using DeviceService.Application.Devices.Commands.UpdateDevice;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Interfaces;
using DeviceService.Domain.Entities;
using Moq;

public class UpdateDeviceCommandHandlerTests
{
    private readonly Mock<IDeviceRepository> _repoMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly UpdateDeviceCommandHandler _handler;

    public UpdateDeviceCommandHandlerTests()
    {
        _repoMock = new Mock<IDeviceRepository>();
        _cacheMock = new Mock<ICacheService>();

        _handler = new UpdateDeviceCommandHandler(
            _repoMock.Object,
            _cacheMock.Object
        );
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenDeviceNotFound()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Device?)null);

        var cmd = new UpdateDeviceCommand(
            Guid.NewGuid(),
            "NewName",
            "sensor",
            "Kitchen",
            true,
            75,
            "SN123"
        );

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Device>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveByPatternAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UpdatesDevice_AndInvalidatesCache()
    {
        // Arrange
        var id = Guid.NewGuid();

        var existingDevice = new Device
        {
            Id = id,
            Name = "Old",
            Type = "sensor",
            Location = "Basement",
            IsOnline = false,
            ThresholdWatts = 40,
            SerialNumber = "ABC",
            RegisteredAt = DateTime.UtcNow
        };

        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existingDevice);

        var cmd = new UpdateDeviceCommand(
            id,
            "NewName",
            "sensor",
            "Kitchen",
            true,
            90,
            "XYZ"
        );

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert updated fields
        Assert.NotNull(result);
        Assert.Equal("NewName", result.Name);
        Assert.Equal("Kitchen", result.Location);
        Assert.Equal(90, result.ThresholdWatts);
        Assert.Equal("XYZ", result.SerialNumber);

        // Assert repo update called
        _repoMock.Verify(r => r.UpdateAsync(existingDevice), Times.Once);

        // Cache invalidations
        _cacheMock.Verify(c => c.RemoveAsync($"device:{id}"), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPatternAsync("devices:*"), Times.Once);
    }
}
