using SubtitleStudio.Core.Models;

namespace SubtitleStudio.Core.Helpers;

public static class TimelineHelper
{
    public static TimeSpan GetTotalDuration(IReadOnlyList<SubtitleItem> items, TimeSpan? videoDuration = null)
    {
        if (items.Count == 0)
            return videoDuration ?? TimeSpan.FromMinutes(5);

        var maxEnd = items.Max(i => i.EndTime);
        if (videoDuration.HasValue && videoDuration.Value > maxEnd)
            return videoDuration.Value;

        return maxEnd + TimeSpan.FromSeconds(1);
    }

    public static List<TimelineSegment> BuildSegments(IReadOnlyList<SubtitleItem> items, TimeSpan totalDuration)
    {
        if (items.Count == 0 || totalDuration <= TimeSpan.Zero)
            return [];

        var totalSeconds = totalDuration.TotalSeconds;
        return items.Select(item =>
        {
            var start = item.StartTime.TotalSeconds / totalSeconds;
            var width = Math.Max((item.EndTime - item.StartTime).TotalSeconds / totalSeconds, 0.002);
            return new TimelineSegment
            {
                Index = item.Index,
                StartRatio = start,
                WidthRatio = width,
                Label = item.Text.Length > 30 ? item.Text[..30] + "…" : item.Text,
                Item = item
            };
        }).ToList();
    }

    public static double GetPlayheadRatio(TimeSpan position, TimeSpan totalDuration) =>
        totalDuration <= TimeSpan.Zero ? 0 : Math.Clamp(position.TotalSeconds / totalDuration.TotalSeconds, 0, 1);
}