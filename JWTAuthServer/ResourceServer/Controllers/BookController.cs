
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceServer.Data;
using ResourceServer.DTOs;

namespace ResourceServer.Controllers
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            Console.WriteLine("==============" + userId);

            if (userId == null) { 
                return NotFound();
            }

            var books = await _context.Books
              .Where(b => b.UserId == int.Parse(userId))
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
