using System.ComponentModel.DataAnnotations;

namespace JWTAuthServer.DTOs
{
    public class BookDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
    }
}
