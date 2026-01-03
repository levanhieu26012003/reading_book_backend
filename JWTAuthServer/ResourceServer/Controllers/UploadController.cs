using Amazon.S3;
using Amazon.S3.Model;
using PdfSharpCore.Pdf.IO;
using ResourceServer.Data;
using ResourceServer.Models;
using VersOne.Epub;

public static class UploadController
{
    public static void MapUploadEndpoints(this WebApplication app, string bucketName)
    {
        app.MapPost("/api/upload-url", async (HttpRequest req, IAmazonS3 s3) =>
        {
            //Input từ client: fileName, contentType(ví dụ application / pdf hoặc application / epub + zip)
                var form = await req.ReadFromJsonAsync<UploadRequest>();
            if (form is null || string.IsNullOrWhiteSpace(form.FileName) || string.IsNullOrWhiteSpace(form.ContentType))
                return Results.BadRequest("fileName và contentType là bắt buộc");
            var keyPrefix = $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/";
            var key = $"{keyPrefix}{Guid.NewGuid()}_{form.FileName}";
            var presign = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(15),
                ContentType = form.ContentType
            };
            var url = s3.GetPreSignedURL(presign);
            return Results.Ok(new { key, uploadUrl = url });
        });
    }
    
    public static void ExtractMetadata(this WebApplication app, string bucketName) {
        app.MapPost("/api/extract-metadata", async (ExtractRequest req, IAmazonS3 s3) =>
        {
            if (string.IsNullOrWhiteSpace(req.Key))
                return Results.BadRequest("key là bắt buộc");

            var getObj = await s3.GetObjectAsync(bucketName, req.Key);
            using var ms = new MemoryStream();
            await getObj.ResponseStream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            string? title = null;
            string? author = null;
            byte[]? coverBytes = null;
            string? coverContentType = null;

            // Phân nhánh theo content-type hoặc extension
            var contentType = getObj.Headers.ContentType ?? MimeFromKey(req.Key);
            string coverKey = null;

            if (contentType == "application/epub+zip" || req.Key.EndsWith(".epub", StringComparison.OrdinalIgnoreCase))
            {
                using var epubStream = new MemoryStream(bytes);
                var book = EpubReader.ReadBook(epubStream);
                title = book.Title;
                author = (book.AuthorList != null && book.AuthorList.Count > 0) ? string.Join(", ", book.AuthorList) : null;
                coverBytes = book.CoverImage;
                coverContentType = "image/jpeg"; // thường jpg; bạn có thể đoán theo header nếu có
            }
            else if (contentType == "application/pdf" || req.Key.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                using var pdfStream = new MemoryStream(bytes);
                var pdf = PdfReader.Open(pdfStream, PdfDocumentOpenMode.ReadOnly);
                title = pdf.Info.Title;
                author = pdf.Info.Author;

                // Trích cover PDF nâng cao: render trang 1 → ảnh (cần thư viện như Ghostscript.NET hoặc pdfium)
                // Ở đây giữ đơn giản: không trích cover. Bạn có thể thêm sau.
            }

            // Nếu có cover, upload cover lên S3 và tạo pre-signed GET
            if (coverBytes != null)
            {
                coverKey = $"covers/{Path.GetFileNameWithoutExtension(req.Key)}_{Guid.NewGuid()}.jpg";
                var putCover = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = coverKey,
                    InputStream = new MemoryStream(coverBytes),
                    ContentType = coverContentType ?? "image/jpeg",
                    AutoCloseStream = true
                };
                await s3.PutObjectAsync(putCover);
                
            }

            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var bookSave = new Book
            {
                Title = title,
                Author = author,
                FileKey = req.Key,
                CoverKey = coverKey,
                UserId = 2,
                UploadedAt = DateTime.UtcNow
            };
            dbContext.Books.Add(bookSave);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new
            {
                key = req.Key,
                title,
                author,
                coverKey,
                contentType
            });
        });
    }

    private static string MimeFromKey(string key)
    {
        if (key.EndsWith(".epub", StringComparison.OrdinalIgnoreCase)) return "application/epub+zip";
        if (key.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return "application/pdf";
        return new string("application/octet-stream");
    }
}
record ExtractRequest(string Key);
record UploadRequest(string FileName, string ContentType);
