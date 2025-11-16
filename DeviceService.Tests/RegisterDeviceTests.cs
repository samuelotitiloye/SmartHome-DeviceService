using System.Threading;
using System.Threading.Tasks;
using DeviceService.Application.Dto;
using DeviceService.Application.Services;
using DeviceService.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

public class RegisterDeviceTests
{
    [Fact]
    public async Task Should_Register_Device_Successfully()
    {
        var repo = new InMemoryDeviceRepository();
        var service = new DevicesService(repo);

        var dto = new RegisterDeviceDto
        {
            DeviceName = "Lamp",
            Type = "Light",
            Location = "Living Room",
            ThresholdWatts = 50,
            SerialNumber = "XYZ123"
        };

        var result = await service.RegisterDeviceAsync(dto, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Lamp");
        result.SerialNumber.Should().Be("XYZ123");
    }
}
