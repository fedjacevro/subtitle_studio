using Microsoft.Extensions.Logging;
using SubtitleStudio.Core.Logging;

namespace SubtitleStudio.Core.Configuration;

public static class LoggingSetup
{
    public static void Configure(ILoggingBuilder builder, LoggingSettings settings, string logFilePath)
    {
        var defaultLevel = ParseLogLevel(settings.LogLevel.GetValueOrDefault("Default", "Information"));
        builder.SetMinimumLevel(defaultLevel);

        foreach (var (category, levelName) in settings.LogLevel)
        {
            if (!string.Equals(category, "Default", StringComparison.OrdinalIgnoreCase))
                builder.AddFilter(category, ParseLogLevel(levelName));
        }

        if (settings.File.Enabled)
        {
            builder.AddProvider(new FileLoggerProvider(
                logFilePath,
                defaultLevel,
                settings.LogLevel,
                settings.File.MaxFileSizeBytes,
                settings.File.MaxRollingFiles));
        }
    }

    public static LogLevel ParseLogLevel(string? name) =>
        Enum.TryParse<LogLevel>(name, ignoreCase: true, out var level) ? level : LogLevel.Information;
}