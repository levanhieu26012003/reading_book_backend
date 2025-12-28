namespace ResourceServer.DTOs
{
    public class UploadRequest
    {
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
    }

}
