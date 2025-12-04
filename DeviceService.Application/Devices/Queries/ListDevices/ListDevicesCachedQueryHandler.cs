using MediatR;
using DeviceService.Application.Common.Models;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Devices.Caching;
using DeviceService.Application.Interfaces;
using DeviceService.Application.Devices.Queries.ListDevices;
using DeviceService.Application.Devices.Models;

public class ListDevicesCachedQueryHandler : IRequestHandler<ListDevicesQuery, PaginatedResult<DeviceDto>>
{
    private readonly IRequestHandler<ListDevicesQuery, PaginatedResult<DeviceDto>> _inner;
    private readonly ICacheService _cache;

    public ListDevicesCachedQueryHandler(IRequestHandler<ListDevicesQuery, PaginatedResult<DeviceDto>> inner, ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<PaginatedResult<DeviceDto>> Handle(ListDevicesQuery request, CancellationToken ct)
    {
        var key = $"devices:{request.Pagination.PageNumber}:{request.Pagination.PageSize}:" +
                  $"{request.Filter.Type}:{request.Filter.Location}:{request.Filter.IsOnline}:" +
                  $"{request.Filter.NameContains}:{request.Filter.MinThresholdWatts}:" +
                  $"{request.Filter.SortBy}:{request.Filter.SortOrder}";

        var cached = await _cache.GetAsync<PaginatedResult<DeviceDto>>(key);
        if (cached != null)
            return cached;

        var result = await _inner.Handle(request, ct);

        await _cache.SetAsync(key, result, 300);

        return result;
    }
}
