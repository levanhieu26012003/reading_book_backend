
using System.Security.Claims;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResourceServer.Data;
using ResourceServer.DTOs;

namespace ResourceServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAmazonS3 _s3;
        private readonly IConfiguration _configuration;

        // Constructor injecting the ApplicationDbContext
        public BookController(ApplicationDbContext context, IAmazonS3 s3, IConfiguration configuration)
        {
            _context = context;
            _s3 = s3;
            _configuration = configuration;
        }

        [HttpGet("GetBooks")]
        //[Authorize]
        public async Task<IActionResult> GetBooks()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return NotFound();
            }

            var books = await _context.Books
              .Where(b => b.UserId == int.Parse(userId))
              .Select(b => new BookDTO
              {
                  Id = b.Id,
                  Author = b.Author,
                  CoverKey = b.CoverKey,
                  Title = b.Title,
                  FileKey = b.FileKey
              })
              .ToListAsync();
            var bucketName = _configuration["AWS:Bucket"];
            foreach (var b in books)
            {
                
                if (!string.IsNullOrEmpty(b.FileKey))
                {
                    b.FileKey = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
                    {
                        BucketName = bucketName,
                        Key = b.FileKey,
                        Verb = HttpVerb.GET,
                        Expires = DateTime.UtcNow.AddDays(1)
                    });
                }
                if (!string.IsNullOrEmpty(b.CoverKey))
                {
                    b.CoverKey = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
                    {
                        BucketName = bucketName,
                        Key = b.CoverKey,
                        Verb = HttpVerb.GET,
                        Expires = DateTime.UtcNow.AddHours(24)
                    });
                }
            }

            return Ok(books);
        }
    }
}
