namespace SubtitleStudio.Core.Models;

public class TimelineSegment
{
    public int Index { get; init; }
    public double StartRatio { get; init; }
    public double WidthRatio { get; init; }
    public string Label { get; init; } = string.Empty;
    public SubtitleItem? Item { get; init; }
}