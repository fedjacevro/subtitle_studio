using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
using Microsoft.Extensions.Logging;

namespace SubtitleStudio.App.Services;

public class SubtitleExportService : ISubtitleExportService
{
    private readonly ILogger<SubtitleExportService> _logger;

    public SubtitleExportService(ILogger<SubtitleExportService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExportSrtAsync(SubtitleTrack track, string outputPath)
    {
        var content = GenerateSrtContent(track);
        await File.WriteAllTextAsync(outputPath, content, System.Text.Encoding.UTF8);
        _logger.LogInformation("SRT exported to: {Path}", outputPath);
        return outputPath;
    }

    public async Task<string> ExportVttAsync(SubtitleTrack track, string outputPath)
    {
        var content = GenerateVttContent(track);
        await File.WriteAllTextAsync(outputPath, content, System.Text.Encoding.UTF8);
        _logger.LogInformation("VTT exported to: {Path}", outputPath);
        return outputPath;
    }

    public async Task<string> ExportAsync(SubtitleTrack track, string outputPath, ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Srt => await ExportSrtAsync(track, outputPath),
            ExportFormat.Vtt => await ExportVttAsync(track, outputPath),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    public async Task<string> ExportTranslatedAsync(SubtitleTrack track, string outputPath,
        ExportFormat format, bool useProofread = true)
    {
        // Create a copy with translated text via DisplayText
        var translatedTrack = track.Clone();
        foreach (var item in translatedTrack.Items)
        {
            var isTranslated = !string.IsNullOrEmpty(item.TranslatedText);
            if (isTranslated)
            {
                if (useProofread && !string.IsNullOrEmpty(item.ProofreadText))
                    item.Text = item.ProofreadText!;
                else if (!string.IsNullOrEmpty(item.TranslatedText))
                    item.Text = item.TranslatedText;
            }
        }

        return await ExportAsync(translatedTrack, outputPath, format);
    }

    private static string FormatTimeSrt(TimeSpan ts) =>
        $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2},{ts.Milliseconds:D3}";

    private static string FormatTimeVtt(TimeSpan ts) =>
        $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";

    private string GenerateSrtContent(SubtitleTrack track)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var item in track.Items)
        {
            var text = item.DisplayText;
            if (string.IsNullOrWhiteSpace(text))
                continue;

            sb.AppendLine(item.Index.ToString());
            sb.AppendLine($"{FormatTimeSrt(item.StartTime)} --> {FormatTimeSrt(item.EndTime)}");
            sb.AppendLine(text);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private string GenerateVttContent(SubtitleTrack track)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("WEBVTT");
        sb.AppendLine();

        foreach (var item in track.Items)
        {
            var text = item.DisplayText;
            if (string.IsNullOrWhiteSpace(text))
                continue;

            sb.AppendLine($"{FormatTimeVtt(item.StartTime)} --> {FormatTimeVtt(item.EndTime)}");
            sb.AppendLine(text);
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
