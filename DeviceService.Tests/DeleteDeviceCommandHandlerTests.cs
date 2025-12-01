using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceService.Application.Interfaces;
using DeviceService.Application.Devices.Commands.DeleteDevice;
using DeviceService.Domain.Entities;
using DeviceService.Infrastructure.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

public class DeleteDeviceCommandHandlerTests
{
    private readonly Mock<ICacheService> _cacheMock;

    public DeleteDeviceCommandHandlerTests()
    {
        _cacheMock = new Mock<ICacheService>();

        // Stub cache methods
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _cacheMock.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Should_Delete_Existing_Device()
    {
        // Arrange
        var repo = new InMemoryDeviceRepository();
        var device = new Device
        {
            Id = Guid.NewGuid(),
            Name = "Lamp",
            Type = "Lighting",
            Location = "Living Room",
            IsOnline = true,
            ThresholdWatts = 50,
            SerialNumber = "ABC123",
            RegisteredAt = DateTime.UtcNow
        };

        await repo.AddAsync(device);

        var handler = new DeleteDeviceCommandHandler(repo, _cacheMock.Object);
        var command = new DeleteDeviceCommand(device.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        // Cache invalidation expectations
        _cacheMock.Verify(c => c.RemoveAsync($"device:{device.Id}"), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPatternAsync("devices:*"), Times.Once);
    }

    [Fact]
    public async Task Should_Return_False_When_Device_Not_Found()
    {
        // Arrange
        var repo = new InMemoryDeviceRepository();
        var handler = new DeleteDeviceCommandHandler(repo, _cacheMock.Object);

        var command = new DeleteDeviceCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        // Ensure NO cache invalidation occurs
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveByPatternAsync(It.IsAny<string>()), Times.Never);
    }
}
