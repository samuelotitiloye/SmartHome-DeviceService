using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace DeviceService.Infrastructure.Cache
{
    public class RedisCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisCacheService> _logger;

        private static readonly DistributedCacheEntryOptions DefaultOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T?> GetAsync <T>(string key)
        {
            try
            {
                var cached = await _cache.GetStringAsync(key);

                if (cached == null)
                {
                    _logger.LogDebug("Redis MISS for key: {Key}", key);
                    return default;
                }

                _logger.LogDebug("Redis HIT for key: {Key}", key);
                return JsonSerializer.Deserialize<T>(cached);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis GET failed for key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, int? ttMinutes = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);

                var options = ttMinutes.HasValue ? new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ttMinutes.Value)
                }
                : DefaultOptions;

                await _cache.SetStringAsync(key, json, options);

                _logger.LogDebug("Redis SET for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis SET failed for key: {Key}", key);
            }
        }

        public async Task RemoveAsync (string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
                _logger.LogDebug("Redis REMOVE for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis REMOVE failed for key: {Key}", key);
            }
        }
    }
}