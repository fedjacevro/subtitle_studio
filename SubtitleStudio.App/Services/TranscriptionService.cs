using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
using SubtitleStudio.App.Helpers;
using Microsoft.Extensions.Logging;
using Whisper.net;

namespace SubtitleStudio.App.Services;

public class TranscriptionService : ITranscriptionService
{
    private readonly IModelDownloadService _downloadService;
    private readonly ILogger<TranscriptionService> _logger;

    public TranscriptionService(IModelDownloadService downloadService, ILogger<TranscriptionService> logger)
    {
        _downloadService = downloadService;
        _logger = logger;
    }

    public string GetModelPath(WhisperModelSize size)
    {
        return Path.Combine(_downloadService.GetWhisperModelsDirectory(), size.ToModelName());
    }

    public bool IsModelDownloaded(WhisperModelSize size)
    {
        return _downloadService.FileExists(GetModelPath(size));
    }

    public async Task DownloadModelAsync(WhisperModelSize size, IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var modelPath = GetModelPath(size);
        if (_downloadService.FileExists(modelPath))
        {
            progress?.Report(1.0);
            return;
        }

        _logger.LogInformation("Downloading Whisper model: {Model}", size);
        await _downloadService.DownloadFileAsync(size.GetDownloadUrl(), modelPath, progress, ct);
    }

    public async Task<SubtitleTrack> TranscribeAsync(string audioFilePath, string language, WhisperModelSize modelSize,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var modelPath = GetModelPath(modelSize);
        if (!_downloadService.FileExists(modelPath))
            throw new InvalidOperationException($"Whisper model {modelSize} is not downloaded. Download it first.");

        if (!File.Exists(audioFilePath))
            throw new FileNotFoundException("Audio file not found", audioFilePath);

        var track = new SubtitleTrack
        {
            SourceLanguage = language,
            AudioFilePath = audioFilePath
        };

        _logger.LogInformation("Starting transcription with model {Model}, language: {Lang}", modelSize, language);

        using var whisperFactory = WhisperFactory.FromPath(modelPath);
        using var processor = whisperFactory.CreateBuilder()
            .WithLanguage(language)
            .Build();

        await using var fileStream = File.OpenRead(audioFilePath);

        var segments = new List<(TimeSpan Start, TimeSpan End, string Text)>();
        int segmentCount = 0;

        await foreach (var segment in processor.ProcessAsync(fileStream, ct))
        {
            segments.Add((
                segment.Start,
                segment.End,
                segment.Text.Trim()
            ));
            segmentCount++;
            progress?.Report(0.3 + (segmentCount % 100) * 0.005); // Rough progress during processing
        }

        // Sort by start time and create subtitle items
        var sortedSegments = segments
            .OrderBy(s => s.Start)
            .ThenBy(s => s.End)
            .ToList();

        track.Items = sortedSegments
            .Select((s, i) => new SubtitleItem
            {
                Index = i + 1,
                StartTime = s.Start,
                EndTime = s.End,
                Text = s.Text
            })
            .ToList();

        progress?.Report(1.0);
        _logger.LogInformation("Transcription completed: {Count} segments", track.Items.Count);
        return track;
    }
}
