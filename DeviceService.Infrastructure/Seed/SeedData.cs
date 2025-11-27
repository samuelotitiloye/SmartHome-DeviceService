using DeviceService.Infrastructure.Persistence;
using DeviceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DeviceService.Infrastructure.Seed
{
    public class SeedData
    {
        private readonly DeviceDbContext _db;
        private readonly ILogger<SeedData> _logger;

        public SeedData(DeviceDbContext db, ILogger<SeedData> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            _logger.LogInformation("Checking if database needs seeding...");

            if (await _db.Devices.AnyAsync())
            {
                _logger.LogInformation("Database already contains devices. Skipping seeding.");
                return;
            }

            _logger.LogWarning("No devices found — seeding demo data...");

            var demoDevices = new List<Device>
            {
                new Device(Guid.NewGuid(), "Living Room Thermostat", "thermostat", "online", true, 87, "Living Room", DateTime.UtcNow),
                new Device(Guid.NewGuid(), "Kitchen Smart Light", "light", "offline", false, 100, "Kitchen", DateTime.UtcNow),
                new Device(Guid.NewGuid(), "Basement Humidity Sensor", "sensor", "online", true, 76, "Basement", DateTime.UtcNow),
                new Device(Guid.NewGuid(), "Garage Door Controller", "door-controller", "maintenance", true, 64, "Garage", DateTime.UtcNow),
                new Device(Guid.NewGuid(), "Bedroom Motion Sensor", "sensor", "online", true, 59, "Bedroom", DateTime.UtcNow),
                new Device(Guid.NewGuid(), "Water Leak Detector", "sensor", "offline", false, 42, "Bathroom", DateTime.UtcNow),
                new Device(Guid.NewGuid(), "Outdoor Security Camera", "camera", "online", true, 93, "Front Yard", DateTime.UtcNow)
            };

            _db.Devices.AddRange(demoDevices);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Database seeding complete. {Count} devices inserted.", demoDevices.Count);
        }
    }
}
