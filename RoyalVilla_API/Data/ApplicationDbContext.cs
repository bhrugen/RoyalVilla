using Microsoft.EntityFrameworkCore;
using RoyalVilla_API.Models;

namespace RoyalVilla_API.Data
{
    public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Villa> Villa{ get; set; }
    }
}
