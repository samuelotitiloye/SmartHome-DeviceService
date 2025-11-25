using Serilog.Context;

namespace DeviceService.Api.Middleware
{
    public class LogEnrichmentMiddleware
    {
        private readonly RequestDelegate _next;

        public LogEnrichmentMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.TraceIdentifier;
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var clientIp = context.Connection.RemoteIpAddress?.ToString();

            using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
            using (Serilog.Context.LogContext.PushProperty("UserAgent", userAgent))
            using (Serilog.Context.LogContext.PushProperty("ClientIP", clientIp))
            using (Serilog.Context.LogContext.PushProperty("RequestPath", context.Request.Path))
            using (Serilog.Context.LogContext.PushProperty("RequestMethod", context.Request.Method))
            {
                await _next(context);
            }
        }
    }
}
