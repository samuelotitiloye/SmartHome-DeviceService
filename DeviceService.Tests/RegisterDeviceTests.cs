using Xunit;
using DeviceService.Application.Dto;
using DeviceService.Application.Services;
using DeviceService.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

public class RegisterDeviceTests
{
    [Fact]
    public async Task RegisterDevice_Should_CreateDeviceWithId()
    {
        // Arrange
        var repo = new InMemoryDeviceRepository();
        var logger = new NullLogger<DevicesService>();
        var service = new DevicesService(repo, logger);

        var dto = new RegisterDeviceDto
        {
            DeviceName = "Test Lamp",
            Type = "Light",
            Location = "Office",
            ThresholdWatts = 60,
            SerialNumber = "TL-001"
        };

        // Act
        var result = await service.RegisterDeviceAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Test Lamp", result.DeviceName);
    }
}
