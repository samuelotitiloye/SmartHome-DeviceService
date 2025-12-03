using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using DeviceService.Infrastructure.Configuration;

namespace DeviceService.Infrastructure.Cache
{
     /// <summary>
    /// Provides a resilient way to create and manage a shared Redis ConnectionMultiplexer.
    /// Handles retry on startup and lazy initialization.
    /// </summary>
    public class RedisConnectionFactory
    {
        private readonly Lazy<Task<ConnectionMultiplexer>> _lazyConnection;
        private readonly RedisSettings _settings;

        public RedisConnectionFactory(IOptions<RedisSettings> settings)
        {
            _settings = settings.Value;
            _lazyConnection = new Lazy<Task<ConnectionMultiplexer>>(CreateConnectionAsync);
        }

        public async Task<ConnectionMultiplexer> GetConnectionAsync()
        {
            return await _lazyConnection.Value;
        }

        private async Task<ConnectionMultiplexer> CreateConnectionAsync()
        {
            var config = new ConfigurationOptions
            {
                EndPoints = { { _settings.Host, _settings.Port} },
                Password = _settings.Password,
                AbortOnConnectFail = false, //retry allowed
                ConnectRetry = 5,
                ConnectTimeout = 5000,
                SyncTimeout = 5000
            };

            Exception? lastException = null;

            // retry loop
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try 
                {
                    return await ConnectionMultiplexer.ConnectAsync(config);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Console.WriteLine($"Redis connection attempt {attempt}/5 failed: {ex.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); //backoff
                }
            }

            throw new InvalidOperationException(
                "Failed to create Redis connection after 5 retries.", lastException
            );
        }
    }
}