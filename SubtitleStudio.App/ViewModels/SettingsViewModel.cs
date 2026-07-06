using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
using SubtitleStudio.App.Helpers;
using Microsoft.Extensions.Logging;

namespace SubtitleStudio.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ITranscriptionService _transcriptionService;
    private readonly ITranslationService _translationService;
    private readonly IVideoProcessingService _videoService;
    private readonly IModelDownloadService _downloadService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<ModelInfo> _whisperModels = [];

    [ObservableProperty]
    private ObservableCollection<ModelInfo> _llmModels = [];

    [ObservableProperty]
    private bool _isDownloadingWhisper;

    [ObservableProperty]
    private double _whisperDownloadProgress;

    [ObservableProperty]
    private string? _whisperDownloadStatus;

    [ObservableProperty]
    private bool _isDownloadingLlm;

    [ObservableProperty]
    private double _llmDownloadProgress;

    [ObservableProperty]
    private string? _llmDownloadStatus;

    [ObservableProperty]
    private bool _isDownloadingFfmpeg;

    [ObservableProperty]
    private double _ffmpegDownloadProgress;

    [ObservableProperty]
    private string? _ffmpegDownloadStatus;

    [ObservableProperty]
    private bool _isFfmpegAvailable;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private WhisperModelSize _selectedWhisperModel = WhisperModelSize.Small;

    public List<WhisperModelSizeOption> ModelSizes { get; } = WhisperModelSizeOption.GetAll();

    public SettingsViewModel(
        ITranscriptionService transcriptionService,
        ITranslationService translationService,
        IVideoProcessingService videoService,
        IModelDownloadService downloadService,
        ILogger<SettingsViewModel> logger)
    {
        _transcriptionService = transcriptionService;
        _translationService = translationService;
        _videoService = videoService;
        _downloadService = downloadService;
        _logger = logger;
    }

    public async Task RefreshModelStatusAsync()
    {
        WhisperModels.Clear();
        foreach (var option in ModelSizes)
        {
            var size = option.Size;
            var isDownloaded = _transcriptionService.IsModelDownloaded(size);
            var modelPath = _transcriptionService.GetModelPath(size);
            var fileSize = isDownloaded ? _downloadService.GetFileSize(modelPath) : 0;
            WhisperModels.Add(new ModelInfo
            {
                Name = option.DisplayName,
                Size = size,
                IsDownloaded = isDownloaded,
                FileSizeBytes = fileSize,
                FileSizeDisplay = fileSize > 0 ? FormatSize(fileSize) : "Not downloaded"
            });
        }

        LlmModels.Clear();
        var llmPath = Path.Combine(_downloadService.GetLlmModelsDirectory(), Constants.LlmModelFileName);
        var llmDownloaded = _downloadService.FileExists(llmPath);
        LlmModels.Add(new ModelInfo
        {
            Name = "Llama 3.2 3B Instruct Q4_K_M",
            Size = WhisperModelSize.Small,
            IsDownloaded = llmDownloaded,
            FileSizeBytes = llmDownloaded ? _downloadService.GetFileSize(llmPath) : 0,
            FileSizeDisplay = llmDownloaded ? FormatSize(_downloadService.GetFileSize(llmPath)) : "Not downloaded"
        });

        IsFfmpegAvailable = await _videoService.IsFfmpegAvailableAsync();
    }

    [RelayCommand]
    private async Task DownloadWhisperModelAsync()
    {
        IsDownloadingWhisper = true;
        WhisperDownloadStatus = $"Downloading {SelectedWhisperModel.GetDisplayName()}...";
        try
        {
            var progress = new Progress<double>(p =>
            {
                WhisperDownloadProgress = p;
                WhisperDownloadStatus = $"Downloading {SelectedWhisperModel.GetDisplayName()}... {p * 100:F0}%";
            });
            await _transcriptionService.DownloadModelAsync(SelectedWhisperModel, progress);
            WhisperDownloadStatus = $"{SelectedWhisperModel.GetDisplayName()} downloaded!";
            await RefreshModelStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Whisper model download failed");
            WhisperDownloadStatus = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloadingWhisper = false;
        }
    }

    [RelayCommand]
    private async Task DownloadLlmModelAsync()
    {
        IsDownloadingLlm = true;
        LlmDownloadStatus = "Downloading LLM model...";
        try
        {
            var progress = new Progress<double>(p =>
            {
                LlmDownloadProgress = p;
                LlmDownloadStatus = $"Downloading LLM model... {p * 100:F0}%";
            });
            await _translationService.DownloadModelAsync(progress);
            LlmDownloadStatus = "LLM model downloaded!";
            await RefreshModelStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM model download failed");
            LlmDownloadStatus = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloadingLlm = false;
        }
    }

    [RelayCommand]
    private async Task DownloadFfmpegAsync()
    {
        IsDownloadingFfmpeg = true;
        FfmpegDownloadStatus = "Downloading FFmpeg...";
        try
        {
            var progress = new Progress<double>(p =>
            {
                FfmpegDownloadProgress = p;
                FfmpegDownloadStatus = $"Downloading FFmpeg... {p * 100:F0}%";
            });
            await _videoService.DownloadFfmpegAsync(progress);
            IsFfmpegAvailable = true;
            FfmpegDownloadStatus = "FFmpeg downloaded!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg download failed");
            FfmpegDownloadStatus = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloadingFfmpeg = false;
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

public class ModelInfo
{
    public string Name { get; set; } = string.Empty;
    public WhisperModelSize Size { get; set; }
    public bool IsDownloaded { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileSizeDisplay { get; set; } = "Not downloaded";
    public string StatusDisplay => IsDownloaded ? "Downloaded" : "Not downloaded";
}
