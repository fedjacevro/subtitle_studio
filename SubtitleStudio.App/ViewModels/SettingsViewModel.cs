using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
using SubtitleStudio.Core.Helpers;
using SubtitleStudio.App.Helpers;
using SubtitleStudio.App.Services;
using Microsoft.Extensions.Logging;

namespace SubtitleStudio.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ITranscriptionService _transcriptionService;
    private readonly ITranslationService _translationService;
    private readonly IVideoProcessingService _videoService;
    private readonly IModelDownloadService _downloadService;
    private readonly DownloadConsentService _consentService;
    private readonly ProgressDialogService _progressDialog;
    private readonly UserNotificationService _notifications;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<ModelInfo> _whisperModels = [];

    [ObservableProperty]
    private ObservableCollection<ModelInfo> _llmModels = [];

    [ObservableProperty]
    private ModelInfo? _selectedWhisperModelInfo;

    [ObservableProperty]
    private bool _isDownloadingWhisper;

    [ObservableProperty]
    private bool _isDownloadingLlm;

    [ObservableProperty]
    private bool _isDownloadingFfmpeg;

    [ObservableProperty]
    private bool _isFfmpegAvailable;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string _memoryStatus = string.Empty;

    [ObservableProperty]
    private WhisperModelSize _selectedWhisperModel = WhisperModelSize.Small;

    public List<WhisperModelSizeOption> ModelSizes { get; } = WhisperModelSizeOption.GetAll();
    public string ModelsDirectory => _downloadService.GetModelsDirectory();
    public string LogsDirectory => Path.Combine(Constants.GetAppDataPath(), "logs");

    public SettingsViewModel(
        ITranscriptionService transcriptionService,
        ITranslationService translationService,
        IVideoProcessingService videoService,
        IModelDownloadService downloadService,
        DownloadConsentService consentService,
        ProgressDialogService progressDialog,
        UserNotificationService notifications,
        ILogger<SettingsViewModel> logger)
    {
        _transcriptionService = transcriptionService;
        _translationService = translationService;
        _videoService = videoService;
        _downloadService = downloadService;
        _consentService = consentService;
        _progressDialog = progressDialog;
        _notifications = notifications;
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
                FileSizeDisplay = fileSize > 0 ? FormatSize(fileSize) : "Not downloaded",
                FilePath = modelPath
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
            FileSizeDisplay = llmDownloaded ? FormatSize(_downloadService.GetFileSize(llmPath)) : "Not downloaded",
            FilePath = llmPath
        });

        IsFfmpegAvailable = await _videoService.IsFfmpegAvailableAsync();
        var available = SystemMemoryHelper.GetAvailablePhysicalMemoryBytes();
        MemoryStatus =
            $"Available memory: {SystemMemoryHelper.FormatBytes(available)} · Recommended: {SystemMemoryHelper.FormatBytes(SystemMemoryHelper.RecommendedMinimumBytes)}";
        StatusMessage = "Model status refreshed.";
    }

    [RelayCommand]
    private async Task DownloadWhisperModelAsync()
    {
        if (!_consentService.EnsureConsent($"Whisper {SelectedWhisperModel.GetDisplayName()}",
                SelectedWhisperModel.GetApproximateSizeBytes() switch
                {
                    < 200_000_000 => "~75–140 MB",
                    < 600_000_000 => "~460 MB",
                    < 2_000_000_000 => "~1.5 GB",
                    _ => "~2.9 GB"
                }))
            return;

        IsDownloadingWhisper = true;
        try
        {
            await _progressDialog.RunAsync($"Downloading {SelectedWhisperModel.GetDisplayName()}",
                async (progress, ct) =>
                {
                    var inner = new Progress<double>(p =>
                        progress.Report(new ProgressReport(p,
                            $"Downloading {SelectedWhisperModel.GetDisplayName()}... {p * 100:F0}%")));
                    await _transcriptionService.DownloadModelAsync(SelectedWhisperModel, inner, ct);
                    await RefreshModelStatusAsync();
                });
        }
        finally
        {
            IsDownloadingWhisper = false;
        }
    }

    [RelayCommand]
    private async Task DownloadLlmModelAsync()
    {
        if (!_consentService.EnsureConsent("Llama 3.2 3B LLM", "~2 GB"))
            return;

        IsDownloadingLlm = true;
        try
        {
            await _progressDialog.RunAsync("Downloading LLM Model", async (progress, ct) =>
            {
                var inner = new Progress<double>(p =>
                    progress.Report(new ProgressReport(p, $"Downloading LLM model... {p * 100:F0}%")));
                await _translationService.DownloadModelAsync(inner, ct);
                await RefreshModelStatusAsync();
            });
        }
        finally
        {
            IsDownloadingLlm = false;
        }
    }

    [RelayCommand]
    private async Task DownloadFfmpegAsync()
    {
        if (!_consentService.EnsureConsent("FFmpeg", "~80 MB"))
            return;

        IsDownloadingFfmpeg = true;
        try
        {
            await _progressDialog.RunAsync("Downloading FFmpeg", async (progress, ct) =>
            {
                var inner = new Progress<double>(p =>
                    progress.Report(new ProgressReport(p, $"Downloading FFmpeg... {p * 100:F0}%")));
                await _videoService.DownloadFfmpegAsync(inner, ct);
                IsFfmpegAvailable = true;
                StatusMessage = "FFmpeg downloaded.";
            });
        }
        finally
        {
            IsDownloadingFfmpeg = false;
        }
    }

    [RelayCommand]
    private void DeleteSelectedWhisperModel()
    {
        var model = SelectedWhisperModelInfo ?? WhisperModels.FirstOrDefault(m => m.Size == SelectedWhisperModel);
        if (model == null || !model.IsDownloaded || string.IsNullOrEmpty(model.FilePath))
        {
            StatusMessage = "Select a downloaded Whisper model to delete.";
            return;
        }

        if (!_notifications.Confirm("Delete Model",
                $"Delete {model.Name}? It can be downloaded again later."))
            return;

        try
        {
            File.Delete(model.FilePath);
            StatusMessage = $"Deleted {model.Name}.";
            _ = RefreshModelStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete whisper model");
            _notifications.ShowError("Delete Failed", ex.Message);
        }
    }

    [RelayCommand]
    private void DeleteLlmModel()
    {
        var model = LlmModels.FirstOrDefault();
        if (model == null || !model.IsDownloaded || string.IsNullOrEmpty(model.FilePath))
        {
            StatusMessage = "LLM model is not downloaded.";
            return;
        }

        if (!_notifications.Confirm("Delete Model", "Delete the LLM model? It can be downloaded again later."))
            return;

        try
        {
            File.Delete(model.FilePath);
            StatusMessage = "LLM model deleted.";
            _ = RefreshModelStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete LLM model");
            _notifications.ShowError("Delete Failed", ex.Message);
        }
    }

    [RelayCommand]
    private void OpenModelsFolder() => OpenFolder(ModelsDirectory);

    [RelayCommand]
    private void OpenLogsFolder()
    {
        Directory.CreateDirectory(LogsDirectory);
        OpenFolder(LogsDirectory);
    }

    private static void OpenFolder(string path)
    {
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
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
    public string? FilePath { get; set; }
}