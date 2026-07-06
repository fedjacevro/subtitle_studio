using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SubtitleStudio.Core.Configuration;

namespace SubtitleStudio.Core.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;
    private readonly long _maxFileSizeBytes;
    private readonly int _maxRollingFiles;
    private readonly LogLevel _defaultLevel;
    private readonly IReadOnlyDictionary<string, string> _categoryLevels;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly object _writeLock = new();

    public FileLoggerProvider(
        string logFilePath,
        LogLevel defaultLevel,
        IReadOnlyDictionary<string, string> categoryLevels,
        long maxFileSizeBytes = 10_485_760,
        int maxRollingFiles = 5)
    {
        _logFilePath = logFilePath;
        _defaultLevel = defaultLevel;
        _categoryLevels = categoryLevels;
        _maxFileSizeBytes = maxFileSizeBytes;
        _maxRollingFiles = maxRollingFiles;
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new FileLogger(name, this));

    public void Dispose() => _loggers.Clear();

    internal bool IsEnabled(string category, LogLevel logLevel)
    {
        var minLevel = _defaultLevel;
        var bestPrefixLength = -1;

        foreach (var (prefix, levelName) in _categoryLevels)
        {
            if (string.Equals(prefix, "Default", StringComparison.OrdinalIgnoreCase))
                continue;

            if (category.StartsWith(prefix, StringComparison.Ordinal) && prefix.Length > bestPrefixLength)
            {
                bestPrefixLength = prefix.Length;
                minLevel = LoggingSetup.ParseLogLevel(levelName);
            }
        }

        return logLevel >= minLevel;
    }

    internal void WriteLog(string message)
    {
        lock (_writeLock)
        {
            RollIfNeeded();
            File.AppendAllText(_logFilePath, message + Environment.NewLine);
        }
    }

    private void RollIfNeeded()
    {
        if (!File.Exists(_logFilePath))
            return;

        if (new FileInfo(_logFilePath).Length < _maxFileSizeBytes)
            return;

        for (var i = _maxRollingFiles - 1; i >= 1; i--)
        {
            var older = $"{_logFilePath}.{i}";
            var newer = i == 1 ? _logFilePath : $"{_logFilePath}.{i - 1}";
            if (File.Exists(newer))
            {
                if (File.Exists(older))
                    File.Delete(older);
                File.Move(newer, older);
            }
        }
    }

    private sealed class FileLogger(string category, FileLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => provider.IsEnabled(category, logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {category}: {message}";
            if (exception != null)
                line += Environment.NewLine + exception;

            provider.WriteLog(line);
        }
    }
}