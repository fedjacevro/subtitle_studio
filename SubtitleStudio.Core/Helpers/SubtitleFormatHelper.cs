using SubtitleStudio.Core.Models;

namespace SubtitleStudio.Core.Helpers;

public static class SubtitleFormatHelper
{
    public static string GenerateSrtContent(SubtitleTrack track, Func<SubtitleItem, string>? textSelector = null)
    {
        textSelector ??= item => item.DisplayText;
        var sb = new System.Text.StringBuilder();

        foreach (var item in track.Items)
        {
            var text = textSelector(item);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            sb.AppendLine(item.Index.ToString());
            sb.AppendLine($"{TimecodeHelper.FormatSrt(item.StartTime)} --> {TimecodeHelper.FormatSrt(item.EndTime)}");
            sb.AppendLine(text);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string GenerateVttContent(SubtitleTrack track, Func<SubtitleItem, string>? textSelector = null)
    {
        textSelector ??= item => item.DisplayText;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("WEBVTT");
        sb.AppendLine();

        foreach (var item in track.Items)
        {
            var text = textSelector(item);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            sb.AppendLine($"{TimecodeHelper.FormatVtt(item.StartTime)} --> {TimecodeHelper.FormatVtt(item.EndTime)}");
            sb.AppendLine(text);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string GetTextForLanguage(SubtitleItem item, string? languageCode, bool useProofread)
    {
        if (string.IsNullOrEmpty(languageCode))
            return item.Text;

        if (useProofread && item.TryGetProofread(languageCode, out var proofread))
            return proofread;

        if (item.TryGetTranslation(languageCode, out var translated))
            return translated;

        return item.Text;
    }
}