using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
using SubtitleStudio.App.Helpers;
using SubtitleStudio.App.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace SubtitleStudio.App.ViewModels;

public partial class ExportViewModel : ObservableObject
{
    private readonly ISubtitleExportService _exportService;
    private readonly IVideoProcessingService _videoService;
    private readonly ProgressDialogService _progressDialog;
    private readonly UserNotificationService _notifications;
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
    private string _fontColor = "white";

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
    public List<string> FontColors { get; } = ["white", "yellow", "black", "red", "green", "blue", "cyan"];

    public ExportViewModel(
        ISubtitleExportService exportService,
        IVideoProcessingService videoService,
        ProgressDialogService progressDialog,
        UserNotificationService notifications,
        ILogger<ExportViewModel> logger)
    {
        _exportService = exportService;
        _videoService = videoService;
        _progressDialog = progressDialog;
        _notifications = notifications;
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

        var extension = Extension();
        var dialog = new SaveFileDialog
        {
            Title = "Export Subtitles",
            Filter = SelectedFormat == ExportFormat.Srt ? "SubRip Subtitle|*.srt" : "WebVTT Subtitle|*.vtt",
            DefaultExt = extension,
            FileName = $"subtitles_{SubtitleTrack.TargetLanguage ?? "original"}{extension}"
        };

        if (dialog.ShowDialog() != true) return;

        IsExporting = true;
        try
        {
            await _progressDialog.RunAsync("Exporting Subtitles", async (progress, ct) =>
            {
                progress.Report(new ProgressReport(0.2, "Writing subtitle file..."));
                var outputPath = dialog.FileName;

                if (!string.IsNullOrEmpty(SubtitleTrack.TargetLanguage))
                {
                    await _exportService.ExportTranslatedAsync(SubtitleTrack, outputPath, SelectedFormat,
                        UseProofread, SubtitleTrack.TargetLanguage);
                }
                else
                {
                    await _exportService.ExportAsync(SubtitleTrack, outputPath, SelectedFormat);
                }

                LastExportPath = outputPath;
                progress.Report(new ProgressReport(0.6, "Subtitle file saved."));

                if (BurnIntoVideo)
                {
                    await BurnInAsync(outputPath, SubtitleTrack.TargetLanguage, progress, ct);
                }

                StatusMessage = BurnIntoVideo ? $"Exported and burned to video." : $"Exported to: {outputPath}";
                progress.Report(new ProgressReport(1, StatusMessage!));
            });
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

        var dialog = new OpenFolderDialog { Title = "Select Output Directory" };
        if (dialog.ShowDialog() != true) return;

        var outputDir = dialog.FolderName;
        var languages = SubtitleTrack.TranslatedLanguageCodes.Count > 0
            ? SubtitleTrack.TranslatedLanguageCodes
            : (SubtitleTrack.TargetLanguage != null ? [SubtitleTrack.TargetLanguage] : []);

        IsExporting = true;
        try
        {
            await _progressDialog.RunAsync("Batch Export", async (progress, ct) =>
            {
                var steps = 1 + languages.Count + (BurnIntoVideo ? languages.Count : 0);
                var step = 0;

                var originalPath = Path.Combine(outputDir, $"subtitles_original{Extension()}");
                await _exportService.ExportAsync(SubtitleTrack, originalPath, SelectedFormat);
                step++;
                progress.Report(new ProgressReport((double)step / steps, "Original exported."));

                foreach (var lang in languages)
                {
                    ct.ThrowIfCancellationRequested();
                    var translatedPath = Path.Combine(outputDir, $"subtitles_{lang}{Extension()}");
                    await _exportService.ExportTranslatedAsync(SubtitleTrack, translatedPath,
                        SelectedFormat, UseProofread, lang);
                    step++;
                    progress.Report(new ProgressReport((double)step / steps, $"Exported {lang}."));

                    if (BurnIntoVideo)
                    {
                        await BurnInAsync(translatedPath, lang, progress, ct, outputDir, step, steps);
                        step++;
                        progress.Report(new ProgressReport((double)step / steps, $"Burned {lang}."));
                    }
                }

                ExportProgress = 1.0;
                StatusMessage = $"Batch export complete: {outputDir}";
                LastExportPath = outputDir;
                progress.Report(new ProgressReport(1, StatusMessage));
            });
        }
        finally
        {
            IsExporting = false;
        }
    }

    private async Task BurnInAsync(string subtitlePath, string? languageCode,
        IProgress<ProgressReport> progress, CancellationToken ct, string? outputDir = null,
        int step = 0, int totalSteps = 1)
    {
        if (SubtitleTrack?.VideoFilePath == null || !File.Exists(SubtitleTrack.VideoFilePath))
        {
            _notifications.ShowError("Burn-in", "Video file path is missing. Re-transcribe from the loaded video.");
            return;
        }

        outputDir ??= Path.GetDirectoryName(subtitlePath) ?? ".";
        var videoName = Path.GetFileNameWithoutExtension(SubtitleTrack.VideoFilePath);
        var langSuffix = string.IsNullOrEmpty(languageCode) ? "subtitled" : languageCode;
        var burnedOutput = Path.Combine(outputDir, $"{videoName}_{langSuffix}.mp4");

        progress.Report(new ProgressReport(totalSteps > 0 ? (double)step / totalSteps : 0.8,
            $"Burning subtitles ({langSuffix})..."));

        await _videoService.BurnSubtitlesAsync(SubtitleTrack.VideoFilePath, subtitlePath,
            burnedOutput, FontName, FontSize, FontColor, ct);
    }

    private string Extension() => SelectedFormat == ExportFormat.Srt ? ".srt" : ".vtt";
}