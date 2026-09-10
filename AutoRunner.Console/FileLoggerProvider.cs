using Microsoft.Extensions.Logging;

namespace AutoRunner.ConsoleApp;

internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly StreamWriter _writer;
    private readonly Lock _writeLock = new();

    public FileLoggerProvider(string logsDirectory)
    {
        Directory.CreateDirectory(logsDirectory);

        var logFilePath = Path.Combine(logsDirectory, $"steamgifts-{DateTimeOffset.Now:yyyy-MM-dd}.log");
        _writer = new StreamWriter(
            new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = true
        };
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, this);

    public void Dispose() => _writer.Dispose();

    private void Write(LogLevel logLevel, string categoryName, string message, Exception? exception)
    {
        if (string.IsNullOrWhiteSpace(message) && exception is null)
            return;

        try
        {
            lock (_writeLock)
            {
                _writer.WriteLine($"{DateTimeOffset.Now:O} [{logLevel}] {categoryName}: {message}");

                if (exception is not null)
                    _writer.WriteLine(exception);
            }
        }
        catch (ObjectDisposedException)
        {
            // Logging must not prevent the application from shutting down.
        }
        catch (IOException)
        {
            // Logging must not interrupt giveaway processing if a file becomes unavailable.
        }
    }

    private sealed class FileLogger(string categoryName, FileLoggerProvider provider) : ILogger
    {
        private static readonly IDisposable EmptyScope = new EmptyScopeDisposable();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => EmptyScope;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            provider.Write(logLevel, categoryName, formatter(state, exception), exception);
        }
    }

    private sealed class EmptyScopeDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
