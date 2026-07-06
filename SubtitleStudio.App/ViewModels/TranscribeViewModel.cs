using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
using Microsoft.Extensions.Logging;

namespace SubtitleStudio.App.ViewModels;

public partial class TranscribeViewModel : ObservableObject
{
    private readonly ITranscriptionService _transcriptionService;
    private readonly IVideoProcessingService _videoService;
    private readonly ILogger<TranscribeViewModel> _logger;

    [ObservableProperty]
    private string _videoFilePath = string.Empty;

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

    public TranscribeViewModel(ITranscriptionService transcriptionService, IVideoProcessingService videoService,
        ILogger<TranscribeViewModel> logger)
    {
        _transcriptionService = transcriptionService;
        _videoService = videoService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task StartTranscriptionAsync()
    {
        if (string.IsNullOrEmpty(VideoFilePath))
        {
            StatusMessage = "Please select a video file first.";
            return;
        }

        IsTranscribing = true;
        TranscriptionProgress = 0;
        StatusMessage = "Extracting audio...";
        _logger.LogInformation("Starting transcription process");

        try
        {
            // Step 1: Extract audio
            var extractProgress = new Progress<double>(p => TranscriptionProgress = p * 0.3);
            AudioFilePath = await _videoService.ExtractAudioAsync(VideoFilePath, extractProgress);
            StatusMessage = "Audio extracted. Transcribing...";

            // Step 2: Transcribe — use the model size selected in SourceView
            var transcribeProgress = new Progress<double>(p =>
            {
                TranscriptionProgress = 0.3 + p * 0.7;
                StatusMessage = $"Transcribing... {p * 100:F0}%";
            });

            SubtitleTrack = await _transcriptionService.TranscribeAsync(
                AudioFilePath, "auto", SelectedModelSize, transcribeProgress);

            TranscriptionProgress = 1.0;
            StatusMessage = $"Transcription complete! {SubtitleTrack.Items.Count} segments found.";
            _logger.LogInformation("Transcription complete: {Count} segments", SubtitleTrack.Items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed");
            StatusMessage = $"Transcription failed: {ex.Message}";
        }
        finally
        {
            IsTranscribing = false;
        }
    }

    public void CheckModelStatus()
    {
        IsModelDownloaded = _transcriptionService.IsModelDownloaded(SelectedModelSize);
    }
}
