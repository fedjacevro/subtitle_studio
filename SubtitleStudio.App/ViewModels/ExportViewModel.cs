using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace SubtitleStudio.App.ViewModels;

public partial class ExportViewModel : ObservableObject
{
    private readonly ISubtitleExportService _exportService;
    private readonly IVideoProcessingService _videoService;
    private readonly ILogger<ExportViewModel> _logger;

    [ObservableProperty]
    private SubtitleTrack? _subtitleTrack;

    [ObservableProperty]
    private ExportFormat _selectedFormat = ExportFormat.Srt;

    [ObservableProperty]
    private string _fontName = "Arial";

    [ObservableProperty]
    private int _fontSize = 24;

    [ObservableProperty]
    private bool _burnIntoVideo;

    [ObservableProperty]
    private bool _useProofread = true;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private double _exportProgress;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _lastExportPath;

    public List<ExportFormat> Formats { get; } = [ExportFormat.Srt, ExportFormat.Vtt];

    public ExportViewModel(ISubtitleExportService exportService, IVideoProcessingService videoService,
        ILogger<ExportViewModel> logger)
    {
        _exportService = exportService;
        _videoService = videoService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task ExportSubtitlesAsync()
    {
        if (SubtitleTrack == null || SubtitleTrack.Items.Count == 0)
        {
            StatusMessage = "No subtitles to export.";
            return;
        }

        var extension = SelectedFormat == ExportFormat.Srt ? ".srt" : ".vtt";
        var dialog = new SaveFileDialog
        {
            Title = "Export Subtitles",
            Filter = SelectedFormat == ExportFormat.Srt
                ? "SubRip Subtitle|*.srt"
                : "WebVTT Subtitle|*.vtt",
            DefaultExt = extension,
            FileName = $"subtitles_{SubtitleTrack.TargetLanguage ?? "original"}{extension}"
        };

        if (dialog.ShowDialog() != true) return;

        IsExporting = true;
        ExportProgress = 0;
        StatusMessage = "Exporting...";

        try
        {
            var outputPath = dialog.FileName;

            if (!string.IsNullOrEmpty(SubtitleTrack.TargetLanguage))
            {
                await _exportService.ExportTranslatedAsync(SubtitleTrack, outputPath, SelectedFormat, UseProofread);
            }
            else
            {
                await _exportService.ExportAsync(SubtitleTrack, outputPath, SelectedFormat);
            }

            LastExportPath = outputPath;
            ExportProgress = 1.0;
            StatusMessage = $"Exported to: {outputPath}";

            // Optionally burn into video
            if (BurnIntoVideo && SubtitleTrack.VideoFilePath != null)
            {
                StatusMessage = "Burning subtitles into video...";
                var videoDir = Path.GetDirectoryName(outputPath) ?? ".";
                var videoName = Path.GetFileNameWithoutExtension(SubtitleTrack.VideoFilePath);
                var burnedOutput = Path.Combine(videoDir, $"{videoName}_subtitled.mp4");

                await _videoService.BurnSubtitlesAsync(SubtitleTrack.VideoFilePath, outputPath,
                    burnedOutput, FontName, FontSize);

                StatusMessage = $"Subtitles burned into: {burnedOutput}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed");
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    private async Task ExportAllAsync()
    {
        if (SubtitleTrack == null || SubtitleTrack.Items.Count == 0)
        {
            StatusMessage = "No subtitles to export.";
            return;
        }

        var dialog = new OpenFolderDialog
        {
            Title = "Select Output Directory",
        };

        if (dialog.ShowDialog() != true) return;

        var outputDir = dialog.FolderName;
        IsExporting = true;
        ExportProgress = 0;

        try
        {
            // Export original
            var originalPath = Path.Combine(outputDir, $"subtitles_original{Extension()}");
            await _exportService.ExportAsync(SubtitleTrack, originalPath, SelectedFormat);
            ExportProgress = 0.33;
            StatusMessage = "Original exported.";

            // Export translated if available
            if (!string.IsNullOrEmpty(SubtitleTrack.TargetLanguage))
            {
                var translatedPath = Path.Combine(outputDir,
                    $"subtitles_{SubtitleTrack.TargetLanguage}{Extension()}");
                await _exportService.ExportTranslatedAsync(SubtitleTrack, translatedPath,
                    SelectedFormat, UseProofread);
                ExportProgress = 0.66;
                StatusMessage = "Translated exported.";
            }

            ExportProgress = 1.0;
            StatusMessage = $"All subtitles exported to: {outputDir}";
            LastExportPath = outputDir;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch export failed");
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    private string Extension() => SelectedFormat == ExportFormat.Srt ? ".srt" : ".vtt";
}
