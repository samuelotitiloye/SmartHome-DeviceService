using DeviceService.Application.Interfaces;
using DeviceService.Application.Devices.Caching;


public class DeviceCacheInvalidator
{
    private readonly ICacheService _cache;

    public DeviceCacheInvalidator(ICacheService cache)
    {
        _cache = cache;
    }

    public Task RemoveForDevice(Guid id) =>
        _cache.RemoveAsync(DeviceCacheKeys.DeviceById(id));

    public Task RemoveList() =>
        _cache.RemoveAsync(DeviceCacheKeys.DeviceList);
}