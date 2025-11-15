using Microsoft.Extensions.Logging;

public class TestLogger<T> : ILogger<T>, IDisposable
{
    public IDisposable BeginScope<TState>(TState state) => this;
    public void Dispose() { }


    public bool IsEnabled(LogLevel logLevel) => false;

    public void Log<TState>(
        LogLevel logLevel, 
        EventId eventId, 
        TState state, 
        Exception? exception, 
        Func<TState, Exception?, string> formatter)
    {
        //no-op. no need to log during tests
    }
}