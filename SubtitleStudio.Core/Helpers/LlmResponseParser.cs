using SubtitleStudio.Core.Models;

namespace SubtitleStudio.Core.Helpers;

public static class LlmResponseParser
{
    public static int ParseNumberedLines(
        string result,
        int chunkIdx,
        int chunkSize,
        IList<SubtitleItem> items,
        Action<SubtitleItem, string> apply)
    {
        var parsed = 0;
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (!trimmedLine.StartsWith('[') || !trimmedLine.Contains(']'))
                continue;

            var closeBracket = trimmedLine.IndexOf(']');
            if (!int.TryParse(trimmedLine.Substring(1, closeBracket - 1), out var idx))
                continue;

            var actualIdx = chunkIdx * chunkSize + idx - 1;
            if (actualIdx < 0 || actualIdx >= items.Count)
                continue;

            var text = trimmedLine[(closeBracket + 1)..].Trim();
            if (string.IsNullOrEmpty(text))
                continue;

            apply(items[actualIdx], text);
            parsed++;
        }

        return parsed;
    }
}