using System.Threading;
using System.Threading.Tasks;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Services;
using DeviceService.Infrastructure.Repositories;
using DeviceService.Application.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;

public class RegisterDeviceTests
{
    private readonly Mock<ICacheService> _cacheMock;
    private readonly ILogger<DevicesService> _logger;

    public RegisterDeviceTests()
    {
        _cacheMock = new Mock<ICacheService>();

        // Stub cache operations
        _cacheMock.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _logger = LoggerFactory.Create(_ => { }).CreateLogger<DevicesService>();
    }

    [Fact]
    public async Task Should_Register_Device_Successfully()
    {
        // Arrange
        var repo = new InMemoryDeviceRepository();

        // DevicesService signature: (IDeviceRepository, ILogger, ICacheService)
        var service = new DevicesService(repo, _cacheMock.Object, _logger);

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
        result.Type.Should().Be("SmartPlug");
        result.Location.Should().Be("Living Room");
        result.SerialNumber.Should().Be("SN12345");
        result.IsOnline.Should().BeTrue();
        result.ThresholdWatts.Should().Be(120);

        // Cache invalidation should happen for lists
        _cacheMock.Verify(c => c.RemoveByPatternAsync("devices:*"), Times.Once);
    }
}
