using DeviceService.Application.Devices.Commands.RegisterDevice;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Interfaces;
using DeviceService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

public class RegisterDeviceCommandHandlerTests
{
    private readonly Mock<IDeviceRepository> _repoMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<RegisterDeviceCommandHandler>> _loggerMock;
    private readonly RegisterDeviceCommandHandler _handler;

    public RegisterDeviceCommandHandlerTests()
    {
        _repoMock = new Mock<IDeviceRepository>();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<RegisterDeviceCommandHandler>>();

        _handler = new RegisterDeviceCommandHandler(
            _repoMock.Object,
            _cacheMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_AddsDevice_AndInvalidatesCache()
    {
        // Arrange
        var cmd = new RegisterDeviceCommand(
            "Test Device",
            "sensor",
            "Kitchen",
            true,
            50,
            "SN123"
        );

        // repo.AddAsync returns Task, so no setup needed

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert repository was called once
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Device>()), Times.Once);

        // Assert list cache invalidation
        _cacheMock.Verify(c => c.RemoveByPatternAsync("devices:*"), Times.Once);

        // Assert single device cache invalidation (id unknown → ANY)
        _cacheMock.Verify(c => c.RemoveAsync(It.Is<string>(k => k.StartsWith("device:"))), Times.Once);

        // Assert DTO mapping correctness
        Assert.Equal(cmd.Name, result.Name);
        Assert.Equal(cmd.SerialNumber, result.SerialNumber);
        Assert.Equal(cmd.Location, result.Location);
    }
}
