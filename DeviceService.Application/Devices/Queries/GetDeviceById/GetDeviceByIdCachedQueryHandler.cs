using MediatR;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Devices.Caching;
using DeviceService.Application.Interfaces;
using DeviceService.Application.Devices.Queries.GetDeviceById;

public class GetDeviceByIdCachedQueryHandler 
    : IRequestHandler<GetDeviceByIdQuery, DeviceDto?>
{
    private readonly IRequestHandler<GetDeviceByIdQuery, DeviceDto?> _inner;
    private readonly ICacheService _cache;

    public GetDeviceByIdCachedQueryHandler(
        IRequestHandler<GetDeviceByIdQuery, DeviceDto?> inner,
        ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<DeviceDto?> Handle(GetDeviceByIdQuery request, CancellationToken ct)
    {
        var key = DeviceCacheKeys.DeviceById(request.Id);

        var cached = await _cache.GetAsync<DeviceDto>(key);
        if (cached != null)
            return cached;

        var result = await _inner.Handle(request, ct);

        if (result != null)
            await _cache.SetAsync(key, result, 300);

        return result;
    }
}
