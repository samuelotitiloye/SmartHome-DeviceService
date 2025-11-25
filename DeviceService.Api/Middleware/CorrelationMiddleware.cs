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
        string correlationId = context.Request.Headers[CorrelationHeaderName]
            .FirstOrDefault();

        var activity = Activity.Current;

        // Use TraceId if no correlation ID supplied
        if (!string.IsNullOrWhiteSpace(correlationId) && activity != null) 
        {
            correlationId = activity.TraceId.ToString();

            // store for logging
            context.Items[CorrelationHeaderName] = correlationId;

            // Add as span tag
            activity?.SetTag("correlation.id", correlationId);

            context.Response.OnStarting(() => 
            {
                if (!context.Response.Headers.ContainsKey(CorrelationHeaderName))
                    context.Response.Headers.Add(CorrelationHeaderName, correlationId);

                if (activity != null && !context.Response.Headers.ContainsKey(TraceIdHeaderName))
                    context.Response.Headers.Add(TraceIdHeaderName, activity.TraceId.ToString());

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}