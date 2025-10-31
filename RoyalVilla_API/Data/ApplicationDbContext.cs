using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RoyalVilla_API.Models;

namespace RoyalVilla_API.Data
{
    public class ApplicationDbContext(DbContextOptions options) : IdentityDbContext(options)
    {
        public DbSet<Villa> Villa { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<VillaAmenities> VillaAmenities { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Villa>().HasData(
                new Villa
                {
                    Id = 1,
                    Name = "Royal Villa",
                    Details = "Luxurious villa with stunning ocean views and private beach access.",
                    Rate = 500.0,
                    Sqft = 2500,
                    Occupancy = 6,
                    ImageUrl = "https://dotnetmasteryimages.blob.core.windows.net/bluevillaimages/villa1.jpg",
                    CreatedDate = new DateTime(2024, 1, 1),
                    UpdatedDate = new DateTime(2024, 1, 1)
                },
                new Villa
                {
                    Id = 2,
                    Name = "Diamond Villa",
                    Details = "Elegant villa with marble interiors and panoramic mountain views.",
                    Rate = 750.0,
                    Sqft = 3200,
                    Occupancy = 8,
                    ImageUrl = "https://dotnetmasteryimages.blob.core.windows.net/bluevillaimages/villa2.jpg",
                    CreatedDate = new DateTime(2024, 1, 15),
                    UpdatedDate = new DateTime(2024, 1, 15)
                },
                new Villa
                {
                    Id = 3,
                    Name = "Pool Villa",
                    Details = "Modern villa featuring an infinity pool and outdoor entertainment area.",
                    Rate = 350.0,
                    Sqft = 1800,
                    Occupancy = 4,
                    ImageUrl = "https://dotnetmasteryimages.blob.core.windows.net/bluevillaimages/villa3.jpg",
                    CreatedDate = new DateTime(2024, 2, 1),
                    UpdatedDate = new DateTime(2024, 2, 1)
                },
                new Villa
                {
                    Id = 4,
                    Name = "Luxury Villa",
                    Details = "Premium villa with spa facilities and concierge services.",
                    Rate = 900.0,
                    Sqft = 4000,
                    Occupancy = 10,
                    ImageUrl = "https://dotnetmasteryimages.blob.core.windows.net/bluevillaimages/villa4.jpg",
                    CreatedDate = new DateTime(2024, 2, 14),
                    UpdatedDate = new DateTime(2024, 2, 14)
                },
                new Villa
                {
                    Id = 5,
                    Name = "Garden Villa",
                    Details = "Charming villa surrounded by tropical gardens and nature trails.",
                    Rate = 275.0,
                    Sqft = 1500,
                    Occupancy = 3,
                    ImageUrl = "https://dotnetmasteryimages.blob.core.windows.net/bluevillaimages/villa5.jpg",
                    CreatedDate = new DateTime(2024, 3, 1),
                    UpdatedDate = new DateTime(2024, 3, 1)
                }
            );
        }
    }
}
