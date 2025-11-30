using System.Threading;
using System.Threading.Tasks;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Services;
using DeviceService.Infrastructure.Repositories;
using DeviceService.Application.Cache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Caching.Distributed;
using Moq;


public class RegisterDeviceTests
{
    private readonly RedisCacheService _cache;

    public RegisterDeviceTests()
    {
        var memoryCache = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions())
        );

        _cache = new RedisCacheService(memoryCache, LoggerFactory.Create(_ => { }).CreateLogger<RedisCacheService>());
    }

    [Fact]
    public async Task Should_Register_Device_Successfully()
    {
        // Arrange
        var repo = new InMemoryDeviceRepository();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<DevicesService>();

        var service = new DevicesService(repo, _cache, logger);

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
