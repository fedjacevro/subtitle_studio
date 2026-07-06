using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
using SubtitleStudio.App.Helpers;
using SubtitleStudio.App.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace SubtitleStudio.App.ViewModels;

public partial class SourceViewModel : ObservableObject
{
    private readonly IVideoProcessingService _videoService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly DownloadConsentService _consentService;
    private readonly ProgressDialogService _progressDialog;
    private readonly UserNotificationService _notifications;
    private readonly ILogger<SourceViewModel> _logger;

    [ObservableProperty]
    private string _videoFilePath = string.Empty;

    [ObservableProperty]
    private string _videoFileName = "No file selected";

    [ObservableProperty]
    private string? _videoDuration;

    [ObservableProperty]
    private string? _videoSummary;

    [ObservableProperty]
    private string? _thumbnailPath;

    [ObservableProperty]
    private WhisperModelSize _selectedModelSize = WhisperModelSize.Small;

    [ObservableProperty]
    private string _selectedSourceLanguage = "auto";

    [ObservableProperty]
    private bool _isFfmpegAvailable;

    [ObservableProperty]
    private bool _isLoadingVideo;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isBusy;

    public List<WhisperModelSizeOption> ModelSizes { get; } = WhisperModelSizeOption.GetAll();
    public List<string> SourceLanguages { get; } = TranslationLanguage.GetWhisperLanguageCodes();

    public SourceViewModel(
        IVideoProcessingService videoService,
        ITranscriptionService transcriptionService,
        DownloadConsentService consentService,
        ProgressDialogService progressDialog,
        UserNotificationService notifications,
        ILogger<SourceViewModel> logger)
    {
        _videoService = videoService;
        _transcriptionService = transcriptionService;
        _consentService = consentService;
        _progressDialog = progressDialog;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        IsFfmpegAvailable = await _videoService.IsFfmpegAvailableAsync();
        StatusMessage = IsFfmpegAvailable
            ? "Ready — select a video to begin."
            : "FFmpeg is required. Download it to continue.";
    }

    [RelayCommand]
    private async Task SelectVideoAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Video File",
            Filter = "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm|All Files|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        VideoFilePath = dialog.FileName;
        VideoFileName = Path.GetFileName(dialog.FileName);
        IsLoadingVideo = true;
        StatusMessage = "Loading video metadata...";

        try
        {
            var info = await _videoService.GetVideoInfoAsync(VideoFilePath);
            VideoDuration = info.Duration;
            VideoSummary = info.Summary;

            if (IsFfmpegAvailable)
            {
                ThumbnailPath = await _videoService.ExtractThumbnailAsync(VideoFilePath);
            }

            StatusMessage = $"Loaded: {VideoFileName}";
            _logger.LogInformation("Video selected: {Path} — {Summary}", VideoFilePath, VideoSummary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load video metadata");
            VideoDuration = _videoService.GetVideoDuration(VideoFilePath);
            StatusMessage = $"Loaded: {VideoFileName} (metadata partial)";
            _notifications.ShowError("Video Info", $"Could not read full video metadata: {ex.Message}");
        }
        finally
        {
            IsLoadingVideo = false;
        }
    }

    [RelayCommand]
    private async Task DownloadFfmpegAsync()
    {
        if (!_consentService.EnsureConsent("FFmpeg", "~80 MB"))
            return;

        await _progressDialog.RunAsync("Downloading FFmpeg", async (progress, ct) =>
        {
            var inner = new Progress<double>(p =>
                progress.Report(new ProgressReport(p, $"Downloading FFmpeg... {p * 100:F0}%")));
            await _videoService.DownloadFfmpegAsync(inner, ct);
            IsFfmpegAvailable = await _videoService.IsFfmpegAvailableAsync();
            StatusMessage = "FFmpeg downloaded successfully!";
        });
    }

    [RelayCommand]
    private async Task DownloadWhisperModelAsync()
    {
        if (_transcriptionService.IsModelDownloaded(SelectedModelSize))
        {
            StatusMessage = $"Model {SelectedModelSize.GetDisplayName()} is already downloaded.";
            return;
        }

        if (!_consentService.EnsureConsent($"Whisper {SelectedModelSize.GetDisplayName()}",
                SelectedModelSize.GetApproximateSizeBytes() switch
                {
                    < 200_000_000 => "~75–140 MB",
                    < 600_000_000 => "~460 MB",
                    < 2_000_000_000 => "~1.5 GB",
                    _ => "~2.9 GB"
                }))
            return;

        IsBusy = true;
        try
        {
            await _progressDialog.RunAsync($"Downloading {SelectedModelSize.GetDisplayName()}",
                async (progress, ct) =>
                {
                    var inner = new Progress<double>(p =>
                        progress.Report(new ProgressReport(p,
                            $"Downloading {SelectedModelSize.GetDisplayName()}... {p * 100:F0}%")));
                    await _transcriptionService.DownloadModelAsync(SelectedModelSize, inner, ct);
                    StatusMessage = $"Model {SelectedModelSize.GetDisplayName()} downloaded!";
                });
        }
        finally
        {
            IsBusy = false;
        }
    }
}