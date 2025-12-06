using Microsoft.Extensions.Logging;

public class TestLogger<T> : ILogger<T>, IDisposable
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => this;

    public void Dispose()
    {
    }

    public bool IsEnabled(LogLevel logLevel) => false;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // No-op. No need to log during tests.
    }
}
