using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using VersOne.Epub;
using PdfSharpCore.Pdf.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResourceServer.Data;


namespace ResourceServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var bucketName = builder.Configuration["AWS:Bucket"];

            builder.Services.AddSingleton<IAmazonS3>(sp =>
            {
      
                return new AmazonS3Client();
            });



            // Add services to the container.
            builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                // This will use the property names as defined in the C# model
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        var httpClient = new HttpClient();
                        var jwks = httpClient.GetStringAsync(builder.Configuration["Jwt:JWKS"]).Result;
                        var keys = new JsonWebKeySet(jwks).Keys;
                        return keys;
                    }
                };
            });

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
              options.UseSqlServer(builder.Configuration.GetConnectionString("EFCoreDBConnection")));

            builder.Services.AddAuthorization(options =>
            {
                // Policy for User role (accessible by User, Editor, Admin)
                options.AddPolicy("UserPolicy", policy =>
                    policy.RequireRole("User", "Editor", "Admin"));
                // Policy for Editor role (accessible by Editor, Admin)
                options.AddPolicy("EditorPolicy", policy =>
                    policy.RequireRole("Editor", "Admin"));
                // Policy for Admin role (accessible by Admin only)
                options.AddPolicy("AdminPolicy", policy =>
                    policy.RequireRole("Admin"));
            });

            var app = builder.Build();
            // Configure the HTTP request pipeline.

            app.MapPost("/api/upload-url", async (HttpRequest req, IAmazonS3 s3) =>
            {
                // Input từ client: fileName, contentType (ví dụ application/pdf hoặc application/epub+zip)
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
                Console.Write(url);
                return Results.Ok(new { key, uploadUrl = url });
            });

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
                string? coverUrl = null;
                if (coverBytes != null)
                {
                    var coverKey = $"covers/{Path.GetFileNameWithoutExtension(req.Key)}_{Guid.NewGuid()}.jpg";
                    var putCover = new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = coverKey,
                        InputStream = new MemoryStream(coverBytes),
                        ContentType = coverContentType ?? "image/jpeg",
                        AutoCloseStream = true
                    };
                    await s3.PutObjectAsync(putCover);

                    var getUrlReq = new GetPreSignedUrlRequest
                    {
                        BucketName = bucketName,
                        Key = coverKey,
                        Verb = HttpVerb.GET,
                        Expires = DateTime.UtcNow.AddHours(24)
                    };
                    coverUrl = s3.GetPreSignedURL(getUrlReq);
                }

                return Results.Ok(new
                {
                    key = req.Key,
                    title,
                    author,
                    coverUrl,
                    contentType
                });
            });

            string MimeFromKey(string key)
            {
                if (key.EndsWith(".epub", StringComparison.OrdinalIgnoreCase)) return "application/epub+zip";
                if (key.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return "application/pdf";
                return "application/octet-stream";
            }


            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();

        }
    }
}
record UploadRequest(string FileName, string ContentType);
record ExtractRequest(string Key);