using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
using SubtitleStudio.App.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace SubtitleStudio.App.ViewModels;

public partial class SourceViewModel : ObservableObject
{
    private readonly IVideoProcessingService _videoService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ILogger<SourceViewModel> _logger;

    [ObservableProperty]
    private string _videoFilePath = string.Empty;

    [ObservableProperty]
    private string _videoFileName = "No file selected";

    [ObservableProperty]
    private string? _videoDuration;

    [ObservableProperty]
    private WhisperModelSize _selectedModelSize = WhisperModelSize.Small;

    [ObservableProperty]
    private string _selectedSourceLanguage = "auto";

    [ObservableProperty]
    private bool _isFfmpegAvailable;

    [ObservableProperty]
    private bool _isDownloadingFfmpeg;

    [ObservableProperty]
    private double _ffmpegDownloadProgress;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isBusy;

    public List<WhisperModelSizeOption> ModelSizes { get; } = WhisperModelSizeOption.GetAll();

    public List<string> SourceLanguages { get; } = TranslationLanguage.GetWhisperLanguageCodes();

    public SourceViewModel(IVideoProcessingService videoService, ITranscriptionService transcriptionService,
        ILogger<SourceViewModel> logger)
    {
        _videoService = videoService;
        _transcriptionService = transcriptionService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        IsFfmpegAvailable = await _videoService.IsFfmpegAvailableAsync();
    }

    [RelayCommand]
    private async Task SelectVideoAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Video File",
            Filter = "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            VideoFilePath = dialog.FileName;
            VideoFileName = Path.GetFileName(dialog.FileName);
            StatusMessage = $"Loaded: {VideoFileName}";
            _logger.LogInformation("Video selected: {Path}", VideoFilePath);

            // Get video duration
            VideoDuration = _videoService.GetVideoDuration(VideoFilePath);
        }
    }

    [RelayCommand]
    private async Task DownloadFfmpegAsync()
    {
        IsDownloadingFfmpeg = true;
        StatusMessage = "Downloading FFmpeg...";
        try
        {
            var progress = new Progress<double>(p =>
            {
                FfmpegDownloadProgress = p;
                StatusMessage = $"Downloading FFmpeg... {p * 100:F0}%";
            });
            await _videoService.DownloadFfmpegAsync(progress);
            IsFfmpegAvailable = await _videoService.IsFfmpegAvailableAsync();
            StatusMessage = "FFmpeg downloaded successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download FFmpeg");
            StatusMessage = $"Failed to download FFmpeg: {ex.Message}";
        }
        finally
        {
            IsDownloadingFfmpeg = false;
        }
    }

    [RelayCommand]
    private async Task DownloadWhisperModelAsync()
    {
        if (_transcriptionService.IsModelDownloaded(SelectedModelSize))
        {
            StatusMessage = $"Model {SelectedModelSize.GetDisplayName()} is already downloaded.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Downloading {SelectedModelSize.GetDisplayName()}...";
        try
        {
            var progress = new Progress<double>(p =>
            {
                StatusMessage = $"Downloading {SelectedModelSize.GetDisplayName()}... {p * 100:F0}%";
            });
            await _transcriptionService.DownloadModelAsync(SelectedModelSize, progress);
            StatusMessage = $"Model {SelectedModelSize.GetDisplayName()} downloaded!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download Whisper model");
            StatusMessage = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
