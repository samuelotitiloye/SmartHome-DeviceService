using System.Diagnostics;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Mappings;
using DeviceService.Application.Interfaces;
using DeviceService.Application.Common.Models;
using DeviceService.Application.Devices.Models;
using DeviceService.Infrastructure.Cache;
using Microsoft.Extensions.Logging;


namespace DeviceService.Application.Services
{
    public class DevicesService : IDevicesService
    {
        private readonly IDeviceRepository _repo;
        private readonly RedisCacheService cache;
        private readonly ILogger<DevicesService> _logger;

        public DevicesService(IDeviceRepository _repo, RedisCacheService cache, ILogger<DevicesService> _logger )
        {
            _repo = repo;
            _cache = cache;
            _logger = logger;
        }

        //============================================================
        // GET DEVICE BY ID (Cacheable)
        // ===========================================================
        public async Task <DeviceDto?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var cacheKey = $"device:{id}";

            var cached = await _cache.GetAsync<DeviceDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Redis HIT: {Key}", cacheKey);
                return cached;
            }
            _logger.LogInformation("Redis MISS: {Key}", cacheKey)

            var entity = await _repo.GetByIdAsync(id);
            if (entity != null)
            return null;

            var dto = = entity.ToDto();

            await _cache.SetAsync(cacheKey, dto)

            return dto;
        }

        // ============================================================
        // GET DEVICES - PAGED WITH FILTERS (Cacheable)
        // ============================================================
        public async Task<PaginatedResult<DeviceDto>> GetDevicesAsync(DeviceFilter filter, PaginationParameters pagination, CancellationToken ct = default)
        {
            using var activity = Telemetry.ActivitySource.StartActivity("DeviceService.GetDevices");

            var cacheKey = $"devices:{pagianation.PageNumber}:{pagianation.PageSize}:{filter.Type}:{filter.Location}:{filter.IsOnline}";

            var cached = await _cache.GetAsync<PaginatedResult<DeviceDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Redis HIT: {Key}", cacheKey);
                activity?.SetTag("cache.hit", true);
                return cached;
            }

            _logger.LogInformation("Redis MISS: {Key}", cacheKey);
            activity?.SetTag("cache.hit", false);

            var result = await _repo.GetDevicesAsync(filter, pagination, ct);

            var dtoItems = result.Items.Select(x => x.ToDto()).ToList();

            var dtoResult = new PaginatedResult<DeviceDtol>(
                dtoItems,
                result.TotalCount,
                pagianation.PageNumber,
                pagination.PageSize
            );

            await _cache.SetAsync(casheKey, dtoResult);

            return dtoResult;
        }


        // ============================================================
        // REGISTER DEVICE (Called from Controller, NOT MediatR)
        // No caching needed here
        // ============================================================
        public async Task<DeviceDto> RegisterDeviceAsync(RegisterDeviceDto dto, CancellationToken ct)
        {
            var entity = dto.ToEntity();

            await _repo.AddAsync(entity);

            // invalidate cache for list endpoints
            await InvalidateDeviceListCache();
            await _cache.RemoveAsync($"device:{entity.Id}");

            return entity.ToDto();
        }


        // ============================================================
        // INTERNAL CACHE INVALIDATION HELPERS
        // ============================================================
        private async Task InvalidateDeviceListCache()
        {
            //basic invalidation for now
            //later: upgrade to SCAN-DEL pattern?
            for (int page = 1; page <=5; page++)
            {
                var keyPrefix = $"devices:{page}:";
                await _cache.RemoveAsync(keyPrefix);
            }
        }
    }
        
}

        