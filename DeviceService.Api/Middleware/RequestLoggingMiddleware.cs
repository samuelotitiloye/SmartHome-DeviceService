using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CorrelationId.Abstractions;

namespace DeviceService.Api.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly ICorrelationContextAccessor _correlation;

        private const int MaxBodyLengthToLog = 2048;

        // Step 3: Sensitive logging rules
        private static readonly HashSet<string> SensitiveRoutes = new()
        {
            "/api/Auth/login"
        };

        private static readonly HashSet<string> SensitiveFields = new()
        {
            "password", "token", "accessToken", "refreshToken"
        };

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger,
            ICorrelationContextAccessor correlation)
        {
            _next = next;
            _logger = logger;
            _correlation = correlation;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/index.html", StringComparison.OrdinalIgnoreCase) ||
                path.Contains(".png", StringComparison.OrdinalIgnoreCase) ||
                path.Contains(".jpg", StringComparison.OrdinalIgnoreCase) ||
                path.Contains(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                path.Contains(".gif", StringComparison.OrdinalIgnoreCase) ||
                path.Contains(".css", StringComparison.OrdinalIgnoreCase) ||
                path.Contains(".js", StringComparison.OrdinalIgnoreCase) ||
                path.Contains(".ico", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            string correlationId = _correlation.CorrelationContext?.CorrelationId ?? context.TraceIdentifier;

            var request = context.Request;
            string method = request.Method;
            string userAgent = request.Headers["User-Agent"].ToString();
            string endpointName = context.GetEndpoint()?.DisplayName ?? "unknown-endpoint";

            //  Capture request body with filtering
            string requestBody = await CaptureAndFilterRequestBody(request);

            // Capture response body
            var originalResponseBody = context.Response.Body;
            await using var responseBuffer = new MemoryStream();
            context.Response.Body = responseBuffer;

            try
            {
                await _next(context);
                stopwatch.Stop();

                string responseBody = await FilterAndReadResponse(responseBuffer);

                // Build a unified log doc
                var logDocument = new
                {
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = correlationId,
                    Method = method,
                    Path = path,
                    QueryString = request.QueryString.Value,
                    Endpoint = endpointName,
                    StatusCode = context.Response.StatusCode,
                    DurationMs = stopwatch.Elapsed.TotalMilliseconds,
                    UserAgent = userAgent,
                    RequestBody = requestBody,
                    ResponseBody = responseBody
                };

                _logger.LogInformation(
                    "HTTP Request Completed {@request} {@response} {@meta}",
                    new
                    {
                        method = method,
                        path = path,
                        query = request.QueryString.Value,
                        body = requestBody
                    },
                    new
                    {
                        status = context.Response.StatusCode,
                        body = responseBody
                    },
                    new 
                    {
                        correlationId = correlationId,
                        endpoint = endpointName,
                        durationMs = stopwatch.Elapsed.TotalMilliseconds,
                        userAgent = userAgent,
                        timestamp = DateTime.UtcNow
                    }
                );

                // Write back response to client
                responseBuffer.Seek(0, SeekOrigin.Begin);
                await responseBuffer.CopyToAsync(originalResponseBody);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "HTTP {Method} {Path} failed with {StatusCode} in {ElapsedMs} ms. CorrelationId={CorrelationId}, Endpoint={Endpoint}, UserAgent={UserAgent}, RequestBody={RequestBody}",
                    method,
                    path,
                    context.Response?.StatusCode ?? 500,
                    stopwatch.Elapsed.TotalMilliseconds,
                    correlationId,
                    endpointName,
                    userAgent,
                    requestBody
                );

                throw; // normal exception middleware run
            }
            finally
            {
                context.Response.Body = originalResponseBody;
            }
        }

        private async Task<string> CaptureAndFilterRequestBody(HttpRequest request)
        {
            if (!IsBodyReadable(request))
                return string.Empty;

            if (SensitiveRoutes.Contains(request.Path.Value ?? ""))
                return "Skipped - Sensitive Endpoint";

            request.EnableBuffering();
            request.Body.Position = 0;

            string raw = await new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
            request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(raw))
                return "";

            // parse JSON to redact sensitive fields
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(raw);
                if (dict != null)
                {
                    foreach (var field in SensitiveFields)
                    {
                        if (dict.ContainsKey(field))
                            dict[field] = "***REDACTED***";
                    }

                    string sanitized = JsonSerializer.Serialize(dict);

                    if (sanitized.Length > MaxBodyLengthToLog)
                        sanitized = sanitized[..MaxBodyLengthToLog] + "...(truncated)";

                    return sanitized;
                }
            }
            catch
            {
                // fallback to raw body
            }

            return raw.Length > MaxBodyLengthToLog ? raw[..MaxBodyLengthToLog] + "...(truncated)" : raw;
        }

        private async Task<string> FilterAndReadResponse(Stream responseBody)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            string raw = await new StreamReader(responseBody).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(raw))
                return "";

            if (raw.Length > MaxBodyLengthToLog)
                return raw[..MaxBodyLengthToLog] + "...(truncated)";

            return raw;
        }

        private static bool IsBodyReadable(HttpRequest request)
        {
            if (request.ContentLength == null || request.ContentLength == 0)
                return false;

            if (!HttpMethods.IsPost(request.Method) && !HttpMethods.IsPut(request.Method) && !HttpMethods.IsPatch(request.Method))
                return false;

            return (request.ContentType ?? "")
                .Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomRequestLogging(this IApplicationBuilder app)
            => app.UseMiddleware<RequestLoggingMiddleware>();
    }
}