
using JWTAuthServer.Data;
using JWTAuthServer.DTOs;
using JWTAuthServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Reflection.Metadata.BlobBuilder;

namespace JWTAuthServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // Constructor injecting the ApplicationDbContext
        public BookController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetBooks")]
        [Authorize]
        public async Task<IActionResult> GetBooks()
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email);
            if (emailClaim == null)
            {
                return Unauthorized(new { message = "Invalid token: Email claim missing." });
            }

            string userEmail = emailClaim.Value;

            // Retrieve the user from the database, including roles.
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
                
            }

            var books = await _context.Books
              .Where(b => b.UserId == user.Id)
              .Select(b =>new BookDTO
              {
                  Id = b.Id,
                  Author = b.Author,
                  ImageUrl = b.ImageUrl,
                  Title = b.Title,
                  Url = b.Url
              })
              .ToListAsync();

            return Ok(books);
        }
    }
}
