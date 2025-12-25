using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace JWTAuthServer.Models
{
    [Index(nameof(Email), Name = "IX_Unique_Email", IsUnique = true)]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } // Navigation property for many-to-many relationship with Role

        // Navigation property for refresh tokens
        public ICollection<RefreshToken> RefreshTokens { get; set; }
    }
}