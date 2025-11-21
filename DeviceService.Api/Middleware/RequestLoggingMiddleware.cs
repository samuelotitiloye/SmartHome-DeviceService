using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CorrelationId;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CorrelationId;
using CorrelationId.Abstractions;
using System.Text.Json;


namespace DeviceService.Api.Middleware
{
    public class RequestLoggingMiddleware
    {
        private const int MaxBodyLengthToLog = 2048; //bytes/characters to avoid huge logs

        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly ICorrelationContextAccessor _correlationContextAccessor;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, ICorrelationContextAccessor correlationContextAccessor)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _correlationContextAccessor = correlationContextAccessor ?? throw new ArgumentNullException(nameof(correlationContextAccessor));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var stopwatch = Stopwatch.StartNew();

            var correlationId = _correlationContextAccessor.CorrelationContext?.CorrelationId ?? context.TraceIdentifier;

            var request = context.Request;
            var method = request.Method;
            var path = request.Path.Value ?? "";
            var userAgent = request.Headers["User-Agent"].ToString();
            var endpointName = context.GetEndpoint()?.DisplayName ?? "unknown-endpoint";

            string? requestBody = null;

            //capture request body for JSON POST/PUT/PATCH 
            if (IsBodyReadable(request))
            {
                request.EnableBuffering();

                using var reader = new StreamReader(
                    request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);

                requestBody = PrettyPrintJson(await reader.ReadToEndAsync());

                if (!string.IsNullOrWhiteSpace(requestBody) && requestBody.Length > MaxBodyLengthToLog)
                {
                    requestBody = requestBody.Substring(0, MaxBodyLengthToLog) + "...(truncated)";
                }

                request.Body.Position = 0;
            }

            var originalResponseBodyStream = context.Response.Body;
            await using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);

                stopwatch.Stop();

                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseText = PrettyPrintJson(await new StreamReader(context.Response.Body).ReadToEndAsync());
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                if (!string.IsNullOrWhiteSpace(responseText) && responseText.Length > MaxBodyLengthToLog)
                {
                    responseText = responseText.Substring(0, MaxBodyLengthToLog) + "...(truncated)";
                }

                var statusCode = context.Response.StatusCode;

                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms. CorrelationId={CorrelationId}, Endpoint={Endpoint}, UserAgent={UserAgent}, RequestBody={RequestBody}, ResponseBody={ResponseBody}",
                    method,
                    path,
                    statusCode,
                    stopwatch.Elapsed.TotalMilliseconds,
                    correlationId,
                    endpointName,
                    userAgent,
                    requestBody,
                    responseText);

                await responseBodyStream.CopyToAsync(originalResponseBodyStream);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var statusCode = context.Response?.StatusCode ?? StatusCodes.Status500InternalServerError;

                _logger.LogError(
                    ex,
                    "HTTP {Method} {Path} failed with {StatusCode} in {ElapsedMs} ms. CorrelationId={CorrelationId}, Endpoint={Endpoint}, UserAgent={UserAgent}, RequestBody={RequestBody}",
                    method,
                    path,
                    statusCode,
                    stopwatch.Elapsed.TotalMilliseconds,
                    correlationId,
                    endpointName,
                    userAgent,
                    requestBody);

                // rethrow so normal error pipeline (DeveloperExceptionPage) still runs
                throw;
            }
            finally 
            {
                context.Response.Body = originalResponseBodyStream;
            }
        }

        private static string PrettyPrintJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json;
        
            try
            {
                using var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch
            {
                return json;
            }
        }

        private static bool IsBodyReadable(HttpRequest request)
        {
            if (request.ContentLength == null || request.ContentLength == 0)
                return false;

            if (!HttpMethods.IsPost(request.Method) && !HttpMethods.IsPut(request.Method) && !HttpMethods.IsPatch(request.Method))
                return false;

            var contentType = request.ContentType ?? "";
            return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }
    }

    // extension for clean program.cs
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomRequestLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestLoggingMiddleware>();
        }
    } 
}