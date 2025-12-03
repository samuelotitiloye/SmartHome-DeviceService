using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DeviceService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using DeviceService.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace DeviceService.Infrastructure.Cache
{
    public class RedisCacheService : ICacheService
    {
        private readonly RedisConnectionFactory _factory;
        private readonly RedisSettings _settings;

        public RedisCacheService(
           RedisConnectionFactory factory,
           IOptions<RedisSettings> settings)
        {
            _factory = factory;
            _settings = settings.Value;
        }

        // ---------------------------------------------------------
        // BASIC GET
        // ---------------------------------------------------------
        private async Task<IDatabase> GetDatabaseAsync()
        {
            var connection = await _factory.GetConnectionAsync();
            return connection.GetDatabase();
        }

        private IServer GetServer(ConnectionMultiplexer connection)
        {
            // use first configured endpoint
            return connection.GetServer(_settings.Host, _settings.Port);
        }

        private string BuildKey(string key)
        {
            return $"{_settings.InstanceName}{key}";
        }


        public async Task<T?> GetAsync<T>(string key)
        {
                var db = await GetDatabaseAsync();
                var value = await db.StringGetAsync(BuildKey(key));

                if (value.IsNullOrEmpty)
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(value!);
        }           

        // ---------------------------------------------------------
        // BASIC SET
        // ---------------------------------------------------------
        public async Task SetAsync<T>(string key, T value, int? ttlMinutes = null)
        {
                var db = await GetDatabaseAsync();
                var redisKey = BuildKey(key);
                var serialized = JsonSerializer.Serialize(value);

                await db.StringSetAsync(redisKey, serialized); 
                    
                if (ttlMinutes.HasValue)
                {
                    var expiry = TimeSpan.FromMinutes(ttlMinutes.Value);
                    await db.KeyExpireAsync(redisKey, expiry);
                }
        }

        // ---------------------------------------------------------
        // BASIC REMOVE
        // ---------------------------------------------------------
        public async Task RemoveAsync(string key)
        {
            var db = await GetDatabaseAsync();
            await db.KeyDeleteAsync(BuildKey(key));
        }

        // ---------------------------------------------------------
        // SCAN / DEL
        // ---------------------------------------------------------
        public async Task RemoveByPatternAsync(string pattern)
        {
            var connection = await _factory.GetConnectionAsync();
            var server = GetServer(connection);
            var db = connection.GetDatabase();

            var fullPattern = $"{_settings.InstanceName}{pattern}";

            //SCAN: safer than KEYS
            foreach (var key in server.Keys(pattern: fullPattern))
            {
                await db.KeyDeleteAsync(key);
            }
        }
    }
}
