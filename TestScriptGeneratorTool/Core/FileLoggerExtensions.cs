using Microsoft.Extensions.Logging;
using System.IO;

namespace TestScriptGeneratorTool.Core
{
    /// <summary>
    /// Extension methods for adding file logging to ILoggingBuilder.
    /// </summary>
    public static class FileLoggerExtensions
    {
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string filePath, LogLevel minimumLevel = LogLevel.Information)
        {
            builder.AddProvider(new FileLoggerProvider(filePath, minimumLevel));
            return builder;
        }
    }

    /// <summary>
    /// File logger provider for writing logs to a file.
    /// </summary>
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;
        private readonly LogLevel _minimumLevel;
        private readonly object _lock = new object();

        public FileLoggerProvider(string filePath, LogLevel minimumLevel)
        {
            _filePath = filePath;
            _minimumLevel = minimumLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(_filePath, categoryName, _minimumLevel, _lock);
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// File logger implementation.
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _filePath;
        private readonly string _categoryName;
        private readonly LogLevel _minimumLevel;
        private readonly object _lock;

        public FileLogger(string filePath, string categoryName, LogLevel minimumLevel, object lockObj)
        {
            _filePath = filePath;
            _categoryName = categoryName;
            _minimumLevel = minimumLevel;
            _lock = lockObj;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return new NoOpScope();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _minimumLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            try
            {
                lock (_lock)
                {
                    var message = formatter(state, exception);
                    var logEntry = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel.ToString().ToUpper()}] [{_categoryName}] {message}";

                    if (exception != null)
                    {
                        logEntry += Environment.NewLine + exception.ToString();
                    }

                    File.AppendAllText(_filePath, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // Silently fail if file writing fails to avoid disrupting the application
            }
        }
    }

    /// <summary>
    /// No-op scope for the file logger.
    /// </summary>
    public class NoOpScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
