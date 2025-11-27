using DeviceService.Infrastructure.Persistence;
using DeviceService.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace DeviceService.Tests.Seed
{
    public class SeedDataTests
    {
        private DeviceDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<DeviceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new DeviceDbContext(options);
        }

        [Fact]
        public async Task SeedAsync_Should_Insert_Devices_When_Empty()
        {
            // Arrange
            var db = CreateInMemoryDb();
            var logger = Mock.Of<ILogger<SeedData>>();
            var seeder = new SeedData(db, logger);

            // Act
            await seeder.SeedAsync();

            // Assert
            Assert.True(await db.Devices.CountAsync() > 0);
        }

        [Fact]
        public async Task SeedAsync_Should_Not_Duplicate_When_Rerun()
        {
            // Arrange
            var db = CreateInMemoryDb();
            var logger = Mock.Of<ILogger<SeedData>>();
            var seeder = new SeedData(db, logger);

            // Act
            await seeder.SeedAsync();
            var firstCount = await db.Devices.CountAsync();

            await seeder.SeedAsync();
            var secondCount = await db.Devices.CountAsync();

            // Assert
            Assert.Equal(firstCount, secondCount);
        }
    }
}
