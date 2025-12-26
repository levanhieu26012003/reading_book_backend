using ResourceServer.Models;
using Microsoft.EntityFrameworkCore;

namespace ResourceServer.Data
{
    public class ApplicationDbContext : DbContext
    {
        // Constructor accepting DbContextOptions and passing them to the base class.
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Book> Books { get; set; }
        public DbSet<Setting> Setting { get; set; }
    }
}