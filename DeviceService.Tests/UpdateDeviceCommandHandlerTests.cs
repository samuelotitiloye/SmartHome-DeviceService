using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceService.Application.Interfaces;
using DeviceService.Application.Devices.Commands.UpdateDevice;
using DeviceService.Application.Devices.Dto;
using DeviceService.Domain.Entities;
using DeviceService.Infrastructure.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

public class UpdateDeviceCommandHandlerTests
{
    private readonly Mock<ICacheService> _cacheMock;

    public UpdateDeviceCommandHandlerTests()
    {
        _cacheMock = new Mock<ICacheService>();

        // Setup defaults
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _cacheMock.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Should_Update_Device_Successfully()
    {
        // Arrange
        var repo = new InMemoryDeviceRepository();

        var device = new Device(
            Guid.NewGuid(),
            "Old Name",
            "Old Type",
            "Old Location",
            false,
            10,
            "OLD123",
            DateTime.UtcNow
        );

        await repo.AddAsync(device);

        var handler = new UpdateDeviceCommandHandler(repo, _cacheMock.Object);

        var command = new UpdateDeviceCommand(
            device.Id,
            "New Name",
            "SmartPlug",
            "Kitchen",
            true,
            200,
            "XYZ789"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("New Name");
        result.Type.Should().Be("SmartPlug");
        result.Location.Should().Be("Kitchen");
        result.IsOnline.Should().BeTrue();
        result.SerialNumber.Should().Be("XYZ789");
        result.ThresholdWatts.Should().Be(200);

        // Verify that cache invalidation happened
        _cacheMock.Verify(c => c.RemoveAsync($"device:{device.Id}"), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPatternAsync("devices:*"), Times.Once);
    }
}