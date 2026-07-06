using System.Globalization;

namespace SubtitleStudio.Core.Helpers;

public static class TimecodeHelper
{
    public static string FormatSrt(TimeSpan ts) =>
        $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2},{ts.Milliseconds:D3}";

    public static string FormatVtt(TimeSpan ts) =>
        $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";

    public static bool TryParseSrt(string? value, out TimeSpan result)
    {
        result = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim().Replace(',', '.');
        return TimeSpan.TryParse(normalized, CultureInfo.InvariantCulture, out result);
    }

    public static bool IsValidRange(TimeSpan start, TimeSpan end) =>
        start >= TimeSpan.Zero && end > start;

    public static string? ValidateSubtitleItems(IReadOnlyList<(int Index, TimeSpan Start, TimeSpan End)> items)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var (index, start, end) = items[i];
            if (!IsValidRange(start, end))
                return $"Subtitle #{index}: end time must be after start time.";

            if (i > 0)
            {
                var previous = items[i - 1];
                if (start < previous.End)
                    return $"Subtitle #{index}: overlaps with subtitle #{previous.Index}.";
            }
        }

        return null;
    }
}