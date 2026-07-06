namespace SubtitleStudio.Core.Models;

public class VideoInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? Duration { get; set; }
    public string? Resolution { get; set; }
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public string? FileSizeDisplay { get; set; }
    public string? ThumbnailPath { get; set; }
    public TimeSpan? DurationTimeSpan { get; set; }

    public string Summary
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Duration)) parts.Add($"Duration: {Duration}");
            if (!string.IsNullOrEmpty(Resolution)) parts.Add(Resolution);
            if (!string.IsNullOrEmpty(VideoCodec)) parts.Add(VideoCodec);
            if (!string.IsNullOrEmpty(FileSizeDisplay)) parts.Add(FileSizeDisplay);
            return parts.Count > 0 ? string.Join(" · ", parts) : "No metadata available";
        }
    }
}