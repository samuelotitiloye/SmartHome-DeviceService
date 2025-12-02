using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace DeviceService.Api.Extensions
{
    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options => 
            {
                options.AddFixedWindowLimiter("BasicLimiter", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 200;
                    limiterOptions.Window = TimeSpan.FromSeconds(10);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 50;
                });

                // allow health endpoints completely
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });
            
            return services;
        }
    }
}