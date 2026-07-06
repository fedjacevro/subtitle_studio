using Microsoft.Extensions.Logging;
using SubtitleStudio.Core.Configuration;
using SubtitleStudio.Core.Logging;

namespace SubtitleStudio.Tests;

public class FileLoggerProviderTests
{
    [Fact]
    public void FileLogger_RespectsCategoryLogLevels()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"subtitlestudio-test-{Guid.NewGuid()}.log");
        var levels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Default"] = "Warning",
            ["SubtitleStudio"] = "Debug"
        };

        using var provider = new FileLoggerProvider(logPath, LogLevel.Warning, levels);
        var appLogger = provider.CreateLogger("SubtitleStudio.App.Services.Test");
        var otherLogger = provider.CreateLogger("Other.Category");

        appLogger.LogDebug("debug-visible");
        otherLogger.LogInformation("info-hidden");
        otherLogger.LogWarning("warn-visible");

        var content = File.ReadAllText(logPath);
        Assert.Contains("debug-visible", content);
        Assert.DoesNotContain("info-hidden", content);
        Assert.Contains("warn-visible", content);
    }

    [Fact]
    public void LoggingSetup_SkipsFileProvider_WhenDisabled()
    {
        var settings = new LoggingSettings
        {
            File = new FileLoggingSettings { Enabled = false }
        };

        using var factory = LoggerFactory.Create(logging => LoggingSetup.Configure(logging, settings, "unused.log"));
        factory.CreateLogger("Test").LogInformation("should-not-create-log-file");
        Assert.False(File.Exists("unused.log"));
    }
}