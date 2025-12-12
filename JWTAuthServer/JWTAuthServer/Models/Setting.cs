using System.ComponentModel.DataAnnotations;

namespace JWTAuthServer.Models
{
    public class Setting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FontSize { get; set; }

        [Required]
        [MaxLength(100)]
        public string Color { get; set; }
        [Required]

        public int UserId { get; set; }

        [Required]
        public User User { get; set; }
    }
}
