using Microsoft.Extensions.Logging;
using SubtitleStudio.Core.Configuration;

namespace SubtitleStudio.Tests;

public class AppSettingsTests
{
    [Fact]
    public void Load_ReturnsDefaults_WhenFileMissing()
    {
        var settings = AppSettings.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        Assert.Equal("Information", settings.Logging.LogLevel["Default"]);
        Assert.True(settings.Logging.File.Enabled);
    }

    [Fact]
    public void Load_ParsesLoggingAndModels()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        var path = Path.Combine(dir, "appsettings.json");
        File.WriteAllText(path, """
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Warning",
                  "SubtitleStudio": "Debug"
                },
                "File": {
                  "Enabled": false,
                  "MaxFileSizeBytes": 2048,
                  "MaxRollingFiles": 2
                }
              },
              "Models": {
                "MinimumRamBytes": 4096,
                "LlmExpectedSha256": "abc123"
              }
            }
            """);

        var settings = AppSettings.Load(dir);
        Assert.Equal("Warning", settings.Logging.LogLevel["Default"]);
        Assert.Equal("Debug", settings.Logging.LogLevel["SubtitleStudio"]);
        Assert.False(settings.Logging.File.Enabled);
        Assert.Equal(2048, settings.Logging.File.MaxFileSizeBytes);
        Assert.Equal(2, settings.Logging.File.MaxRollingFiles);
        Assert.Equal(4096, settings.Models.MinimumRamBytes);
        Assert.Equal("abc123", settings.Models.LlmExpectedSha256);
    }

    [Fact]
    public void ParseLogLevel_FallsBackToInformation_ForUnknownValue()
    {
        Assert.Equal(LogLevel.Information, LoggingSetup.ParseLogLevel("not-a-level"));
        Assert.Equal(LogLevel.Debug, LoggingSetup.ParseLogLevel("debug"));
    }
}