using DeviceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeviceService.Infrastructure.Persistence
{
    public class DeviceDbContext : DbContext
    {
        public DeviceDbContext(DbContextOptions<DeviceDbContext> options)
            : base(options)
        {
        }

        public DbSet<Device> Devices => Set<Device>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DeviceDbContext).Assembly);
        }
    }
}