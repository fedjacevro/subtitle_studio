using System.Text;
using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
using SubtitleStudio.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace SubtitleStudio.App.Services;

public class SubtitleExportService : ISubtitleExportService
{
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);
    private readonly ILogger<SubtitleExportService> _logger;

    public SubtitleExportService(ILogger<SubtitleExportService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExportSrtAsync(SubtitleTrack track, string outputPath)
    {
        var content = SubtitleFormatHelper.GenerateSrtContent(track);
        await WriteUtf8WithBomAsync(outputPath, content);
        _logger.LogInformation("SRT exported to: {Path}", outputPath);
        return outputPath;
    }

    public async Task<string> ExportVttAsync(SubtitleTrack track, string outputPath)
    {
        var content = SubtitleFormatHelper.GenerateVttContent(track);
        await WriteUtf8WithBomAsync(outputPath, content);
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
        ExportFormat format, bool useProofread = true, string? languageCode = null)
    {
        languageCode ??= track.TargetLanguage;
        var content = format switch
        {
            ExportFormat.Srt => SubtitleFormatHelper.GenerateSrtContent(track,
                item => SubtitleFormatHelper.GetTextForLanguage(item, languageCode, useProofread)),
            ExportFormat.Vtt => SubtitleFormatHelper.GenerateVttContent(track,
                item => SubtitleFormatHelper.GetTextForLanguage(item, languageCode, useProofread)),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        await WriteUtf8WithBomAsync(outputPath, content);
        _logger.LogInformation("Translated export ({Lang}) to: {Path}", languageCode, outputPath);
        return outputPath;
    }

    private static async Task WriteUtf8WithBomAsync(string outputPath, string content)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(outputPath, content, Utf8WithBom);
    }
}