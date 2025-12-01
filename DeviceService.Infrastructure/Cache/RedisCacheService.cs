using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DeviceService.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DeviceService.Infrastructure.Cache
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IDatabase _redisDb;
        private readonly ILogger<RedisCacheService> _logger;

        private static readonly DistributedCacheEntryOptions DefaultOptions =
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

        public RedisCacheService(
            IDistributedCache cache,
            IConnectionMultiplexer muxer,
            ILogger<RedisCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            _redisDb = muxer.GetDatabase();
        }

        // ---------------------------------------------------------
        // BASIC GET
        // ---------------------------------------------------------
        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var json = await _cache.GetStringAsync(key);

                if (json == null)
                {
                    _logger.LogDebug("Redis MISS: {Key}", key);
                    return default;
                }

                _logger.LogDebug("Redis HIT: {Key}", key);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis GET failed for {Key}", key);
                return default;
            }
        }

        // ---------------------------------------------------------
        // BASIC SET
        // ---------------------------------------------------------
        public async Task SetAsync<T>(string key, T value, int? ttlMinutes = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);

                var options = ttlMinutes.HasValue
                    ? new DistributedCacheEntryOptions
                    { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ttlMinutes.Value) }
                    : DefaultOptions;

                await _cache.SetStringAsync(key, json, options);
                _logger.LogDebug("Redis SET: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis SET failed for {Key}", key);
            }
        }

        // ---------------------------------------------------------
        // BASIC REMOVE
        // ---------------------------------------------------------
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
                _logger.LogDebug("Redis REMOVE: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis REMOVE failed for {Key}", key);
            }
        }

        // ---------------------------------------------------------
        // SCAN / DEL
        // ---------------------------------------------------------
        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                _logger.LogInformation("Redis wildcard delete starting. Pattern: {Pattern}", pattern);

                var server = GetServer();
                if (server == null)
                {
                    _logger.LogWarning("Redis server not resolved. Skipping pattern delete.");
                    return;
                }

                int removed = 0;

                foreach (var key in server.Keys(pattern: pattern))
                {
                    await _redisDb.KeyDeleteAsync(key);
                    removed++;
                }

                _logger.LogInformation(
                    "Redis wildcard delete completed. Pattern: {Pattern}, Removed: {Count}",
                    pattern, removed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis wildcard delete failed for pattern: {Pattern}", pattern);
            }
        }

        // ---------------------------------------------------------
        // Resolve Redis Server (for SCAN)
        // ---------------------------------------------------------
        private IServer? GetServer()
        {
            try
            {
                var multiplexer = _redisDb.Multiplexer;
                var endpoints = multiplexer.GetEndPoints();

                if (endpoints.Length == 0)
                    return null;

                return multiplexer.GetServer(endpoints[0]);
            }
            catch
            {
                return null;
            }
        }
    }
}
