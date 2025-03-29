using Microsoft.EntityFrameworkCore;
using Entities = CloudDrive.Core.Domain.Entities;

namespace CloudDrive.Infrastructure.Repositories
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<Entities.User> Users { get; set; }
        public DbSet<Entities.File> Files { get; set; }
        public DbSet<Entities.FileVersion> FileVersions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
        }
    }
}
