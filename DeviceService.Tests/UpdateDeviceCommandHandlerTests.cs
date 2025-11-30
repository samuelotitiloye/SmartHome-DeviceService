using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceService.Application.Cache;
using DeviceService.Application.Devices.Commands.UpdateDevice;
using DeviceService.Application.Devices.Dto;
using DeviceService.Infrastructure.Repositories;
using DeviceService.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Caching.Distributed;
using Moq;


public class UpdateDeviceCommandHandlerTests
{
    private readonly RedisCacheService _cache;

    public UpdateDeviceCommandHandlerTests()
    {
        var memoryCache = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions())
        );

        _cache = new RedisCacheService(memoryCache, LoggerFactory.Create(_ => { }).CreateLogger<RedisCacheService>());
    }

    [Fact]
    public async Task Should_Update_Device_Successfully()
    {
        // Arrange
        var repo = new InMemoryDeviceRepository();

        // Seed a device
        var device = new DeviceService.Domain.Entities.Device(
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

        var handler = new UpdateDeviceCommandHandler(repo, _cache);

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
    }
}
