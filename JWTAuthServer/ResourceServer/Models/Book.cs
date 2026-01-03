using System.ComponentModel.DataAnnotations;

namespace ResourceServer.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Author { get; set; }
        [Required]
        public string FileKey { get; set; }
        [Required]
        public string CoverKey { get; set; }
        [Required]
        public int UserId { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
