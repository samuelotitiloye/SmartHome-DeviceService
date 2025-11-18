using System.Threading;
using System.Threading.Tasks;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Services;
using DeviceService.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

public class RegisterDeviceTests
{
    [Fact]
    public async Task Should_Register_Device_Successfully()
    {
        // Arrange
        var repo = new InMemoryDeviceRepository();
        var service = new DevicesService(repo);

        var dto = new RegisterDeviceDto(
            "Device A",
            "SmartPlug",
            "Living Room",
            true,
            120,
            "SN12345"
        );

        // Act
        var result = await service.RegisterDeviceAsync(dto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Device A");
        result.SerialNumber.Should().Be("SN12345");
    }
}
