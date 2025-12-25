using JWTAuthServer.Models;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthServer.Data
{
    public class ApplicationDbContext : DbContext
    {
        // Constructor accepting DbContextOptions and passing them to the base class.
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Override OnModelCreating to configure entity properties and relationships.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the UserRole entity as a join table for User and Role.
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId }); // Composite primary key.

            //Defines the many-to-many relationship between User and Role.
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // Configure relationships
            // When a User is deleted, their associated refresh tokens are also deleted to maintain data integrity.
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // When a Client is deleted, their associated refresh tokens are also deleted to maintain data integrity.
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.Client)
                .WithMany(c => c.RefreshTokens)
                .HasForeignKey(rt => rt.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed initial data for Roles, Users, Clients, and UserRoles.
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Admin Role" },
                new Role { Id = 2, Name = "Editor", Description = " Editor Role" },
                new Role { Id = 3, Name = "User", Description = "User Role" }
            );

            modelBuilder.Entity<Client>().HasData(
                new Client
                {
                    Id = 1,
                    ClientId = "Mobile",
                    Name = "Mobile Application 1",
                    ClientURL = "https://reading_mobile.com"
                },
                new Client
                {
                    Id = 2,
                    ClientId = "Web",
                    Name = "Web Application 2",
                    ClientURL = "https://reading_web.com"
                }
            );

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Email = "h1@gmail.com",
                    FullName = "Hiếu Lê",
                    Password = "123123",
                }
            );

            modelBuilder.Entity<Setting>().HasData(
               new Setting
               {
                   Id = 1,
                   Color = "Red",
                   FontSize = 13,
                   UserId = 1,
               }
           );
            modelBuilder.Entity<Book>().HasData(
                new Book
                {
                    Id = 1,
                    UserId = 1,
                    Author = "Thích Nhất Hạnh",
                    Title = "An Bình Trên Từng Bước Chân",
                    ImageUrl = "https://image.com",
                    Url = "https://url.com",
                });
        }

        // DbSet representing the Users table.
        public DbSet<User> Users { get; set; }

        // DbSet representing the Roles table.
        public DbSet<Role> Roles { get; set; }

        // DbSet representing the Clients table.
        public DbSet<Client> Clients { get; set; }

        // DbSet representing the UserRoles join table.
        public DbSet<UserRole> UserRoles { get; set; }

        // DbSet representing the SigningKeys table.
        public DbSet<SigningKey> SigningKeys { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Setting> Setting { get; set; }
        // DbSet representing the RefreshTokens table.
        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}