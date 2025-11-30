using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceService.Application.Cache;
using DeviceService.Application.Devices.Commands.DeleteDevice;
using DeviceService.Domain.Entities;
using DeviceService.Infrastructure.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Caching.Distributed;
using Moq;



public class DeleteDeviceCommandHandlerTests
{
    private readonly RedisCacheService _cache;

    public DeleteDeviceCommandHandlerTests()
    {
        var memoryCache = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions())
        );

        _cache = new RedisCacheService(memoryCache, LoggerFactory.Create(_ => { }).CreateLogger<RedisCacheService>());
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

        var handler = new DeleteDeviceCommandHandler(repo, _cache);
        var command = new DeleteDeviceCommand(device.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Return_False_When_Device_Not_Found()
    {
        var repo = new InMemoryDeviceRepository();
        var handler = new DeleteDeviceCommandHandler(repo, _cache);

        var command = new DeleteDeviceCommand(Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeFalse();
    }
}
