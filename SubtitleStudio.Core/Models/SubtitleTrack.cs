namespace SubtitleStudio.Core.Models;

/// <summary>
/// Represents a complete subtitle track with metadata.
/// </summary>
public class SubtitleTrack
{
    public string SourceLanguage { get; set; } = "auto";
    public string? TargetLanguage { get; set; }
    public string? VideoFilePath { get; set; }
    public string? AudioFilePath { get; set; }
    public List<SubtitleItem> Items { get; set; } = [];

    public SubtitleTrack Clone() => new()
    {
        SourceLanguage = SourceLanguage,
        TargetLanguage = TargetLanguage,
        VideoFilePath = VideoFilePath,
        AudioFilePath = AudioFilePath,
        Items = Items.Select(i => i.Clone()).ToList()
    };
}
