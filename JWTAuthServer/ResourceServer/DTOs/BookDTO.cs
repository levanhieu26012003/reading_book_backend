    namespace ResourceServer.DTOs
{
    public class BookDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string FileKey { get; set; }
        public string CoverKey { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
