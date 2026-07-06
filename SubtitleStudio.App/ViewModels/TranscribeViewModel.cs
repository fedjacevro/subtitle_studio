using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
using SubtitleStudio.App.Helpers;
using SubtitleStudio.App.Services;
using Microsoft.Extensions.Logging;

namespace SubtitleStudio.App.ViewModels;

public partial class TranscribeViewModel : ObservableObject
{
    private readonly ITranscriptionService _transcriptionService;
    private readonly IVideoProcessingService _videoService;
    private readonly ProgressDialogService _progressDialog;
    private readonly UserNotificationService _notifications;
    private readonly ILogger<TranscribeViewModel> _logger;

    [ObservableProperty]
    private string _videoFilePath = string.Empty;

    [ObservableProperty]
    private string _selectedSourceLanguage = "auto";

    [ObservableProperty]
    private SubtitleTrack? _subtitleTrack;

    [ObservableProperty]
    private bool _isTranscribing;

    [ObservableProperty]
    private double _transcriptionProgress;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isModelDownloaded;

    [ObservableProperty]
    private string? _audioFilePath;

    [ObservableProperty]
    private WhisperModelSize _selectedModelSize = WhisperModelSize.Small;

    public TranscribeViewModel(
        ITranscriptionService transcriptionService,
        IVideoProcessingService videoService,
        ProgressDialogService progressDialog,
        UserNotificationService notifications,
        ILogger<TranscribeViewModel> logger)
    {
        _transcriptionService = transcriptionService;
        _videoService = videoService;
        _progressDialog = progressDialog;
        _notifications = notifications;
        _logger = logger;
    }

    [RelayCommand]
    private async Task StartTranscriptionAsync()
    {
        if (string.IsNullOrEmpty(VideoFilePath))
        {
            StatusMessage = "Please select a video file first.";
            _notifications.ShowInfo("Transcription", "Please select a video file on the Source step first.");
            return;
        }

        if (!await _videoService.IsFfmpegAvailableAsync())
        {
            StatusMessage = "FFmpeg is required for audio extraction.";
            _notifications.ShowInfo("Transcription",
                "Download FFmpeg on the Source or Settings step first.");
            return;
        }

        if (!_transcriptionService.IsModelDownloaded(SelectedModelSize))
        {
            StatusMessage = "Whisper model not downloaded.";
            _notifications.ShowInfo("Transcription",
                $"Download the {SelectedModelSize.GetDisplayName()} model on the Source or Settings step first.");
            return;
        }

        IsTranscribing = true;
        _logger.LogInformation("Starting transcription for {Video}", VideoFilePath);

        var success = await _progressDialog.RunAsync("Transcribing Audio", async (progress, ct) =>
        {
            progress.Report(new ProgressReport(0, "Extracting audio..."));
            var extractProgress = new Progress<double>(p =>
            {
                TranscriptionProgress = p * 0.3;
                progress.Report(new ProgressReport(p * 0.3, $"Extracting audio... {p * 100:F0}%"));
            });

            AudioFilePath = await _videoService.ExtractAudioAsync(VideoFilePath, extractProgress, ct);
            progress.Report(new ProgressReport(0.3, "Audio extracted. Transcribing..."));

            var transcribeProgress = new Progress<double>(p =>
            {
                TranscriptionProgress = 0.3 + p * 0.7;
                progress.Report(new ProgressReport(0.3 + p * 0.7, $"Transcribing... {p * 100:F0}%"));
            });

            SubtitleTrack = await _transcriptionService.TranscribeAsync(
                AudioFilePath, SelectedSourceLanguage, SelectedModelSize, transcribeProgress, ct);

            SubtitleTrack.VideoFilePath = VideoFilePath;
            SubtitleTrack.SourceLanguage = SelectedSourceLanguage;
            SubtitleTrack.AudioFilePath = AudioFilePath;

            TranscriptionProgress = 1.0;
            StatusMessage = $"Transcription complete! {SubtitleTrack.Items.Count} segments found.";
            _logger.LogInformation("Transcription complete: {Count} segments", SubtitleTrack.Items.Count);
        });

        if (!success)
            StatusMessage = "Transcription cancelled or failed.";

        IsTranscribing = false;
    }

    public void CheckModelStatus()
    {
        IsModelDownloaded = _transcriptionService.IsModelDownloaded(SelectedModelSize);
    }
}