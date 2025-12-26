using System.ComponentModel.DataAnnotations;

namespace ResourceServer.Models
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

    }  
}
