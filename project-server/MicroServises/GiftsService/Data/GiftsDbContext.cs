using Microsoft.EntityFrameworkCore;
using GiftsService.Models;

namespace GiftsService.Data
{
    public class GiftsDbContext : DbContext
    {
        public GiftsDbContext(DbContextOptions<GiftsDbContext> options) : base(options) { }

        public DbSet<Gift> Gifts { get; set; }
        public DbSet<Donor> Donors { get; set; }
    }
}