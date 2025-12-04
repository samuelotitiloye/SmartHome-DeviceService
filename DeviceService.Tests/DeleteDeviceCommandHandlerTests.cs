using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceService.Application.Devices.Commands.DeleteDevice;
using DeviceService.Application.Interfaces;
using DeviceService.Domain.Entities;
using DeviceService.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

public class DeleteDeviceCommandHandlerTests
{
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

        var handler = new DeleteDeviceCommandHandler(repo);
        var command = new DeleteDeviceCommand(device.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        // Confirm device is gone
        (await repo.GetByIdAsync(device.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Should_Return_False_When_Device_Not_Found()
    {
        // Arrange
        var repo = new InMemoryDeviceRepository();
        var handler = new DeleteDeviceCommandHandler(repo);

        var command = new DeleteDeviceCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }
}