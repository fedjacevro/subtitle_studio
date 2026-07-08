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

        // Fallback: if no numbered lines parsed, try to apply lines sequentially for the chunk (best effort)
        if (parsed == 0)
        {
            var fallbackLines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            int maxForChunk = Math.Min(chunkSize, items.Count - chunkIdx * chunkSize);
            for (int i = 0; i < Math.Min(fallbackLines.Count, maxForChunk); i++)
            {
                var actualIdx = chunkIdx * chunkSize + i;
                if (actualIdx >= items.Count) break;
                var text = fallbackLines[i];
                // Strip any leading markers if present
                if (text.StartsWith('[') && text.Contains(']'))
                    text = text[(text.IndexOf(']') + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    apply(items[actualIdx], text);
                    parsed++;
                }
            }
        }

        return parsed;
    }
}