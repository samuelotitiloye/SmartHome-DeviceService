using System.Diagnostics;

public class CorrelationMiddleware
{
    private const string CorrelationHeaderName = "X-Correlation-ID";
    private const string TraceIdHeaderName = "X-Trace-ID";

    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var activity = Activity.Current;
        var correlationId = context.Request.Headers[CorrelationHeaderName].FirstOrDefault();

        // If no correlation ID, derive from trace or generate a new one
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            if (activity is not null)
            {
                correlationId = activity.TraceId.ToString();
            }
            else
            {
                correlationId = Guid.NewGuid().ToString();
            }
        }

        // Store for logging
        context.Items[CorrelationHeaderName] = correlationId;

        // Add as span tag
        activity?.SetTag("correlation.id", correlationId);

        context.Response.OnStarting(() =>
        {
            // Use indexer or Append rather than Add (ASP0019)
            context.Response.Headers[CorrelationHeaderName] = correlationId;

            if (activity is not null)
            {
                context.Response.Headers[TraceIdHeaderName] = activity.TraceId.ToString();
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
