using System.Text.Json;
using SubtitleStudio.Core.Helpers;

namespace SubtitleStudio.Core.Configuration;

public sealed class AppSettings
{
    public LoggingSettings Logging { get; set; } = new();
    public ModelSettings Models { get; set; } = new();

    public static AppSettings Load(string? baseDirectory = null)
    {
        var path = Path.Combine(baseDirectory ?? AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            return new AppSettings();

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var settings = new AppSettings();
            var root = doc.RootElement;

            if (root.TryGetProperty("Logging", out var logging))
                settings.Logging = LoggingSettings.Parse(logging);

            if (root.TryGetProperty("Models", out var models))
            {
                if (models.TryGetProperty("MinimumRamBytes", out var ram))
                    settings.Models.MinimumRamBytes = ram.GetInt64();
                if (models.TryGetProperty("LlmExpectedSha256", out var sha))
                    settings.Models.LlmExpectedSha256 = sha.GetString();
            }

            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }
}

public sealed class ModelSettings
{
    public long MinimumRamBytes { get; set; } = SystemMemoryHelper.RecommendedMinimumBytes;
    public string? LlmExpectedSha256 { get; set; }
}

public sealed class LoggingSettings
{
    public Dictionary<string, string> LogLevel { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Default"] = "Information",
        ["Microsoft"] = "Warning"
    };

    public FileLoggingSettings File { get; set; } = new();

    internal static LoggingSettings Parse(JsonElement element)
    {
        var settings = new LoggingSettings();

        if (element.TryGetProperty("LogLevel", out var levels))
        {
            settings.LogLevel.Clear();
            foreach (var prop in levels.EnumerateObject())
                settings.LogLevel[prop.Name] = prop.Value.GetString() ?? "Information";
        }

        if (element.TryGetProperty("File", out var file))
        {
            if (file.TryGetProperty("Enabled", out var enabled))
                settings.File.Enabled = enabled.GetBoolean();
            if (file.TryGetProperty("MaxFileSizeBytes", out var maxSize))
                settings.File.MaxFileSizeBytes = maxSize.GetInt64();
            if (file.TryGetProperty("MaxRollingFiles", out var maxFiles))
                settings.File.MaxRollingFiles = maxFiles.GetInt32();
        }

        return settings;
    }
}

public sealed class FileLoggingSettings
{
    public bool Enabled { get; set; } = true;
    public long MaxFileSizeBytes { get; set; } = 10_485_760;
    public int MaxRollingFiles { get; set; } = 5;
}