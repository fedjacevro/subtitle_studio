using SubtitleStudio.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace SubtitleStudio.App.Services;

public class VideoProcessingService : IVideoProcessingService
{
    private readonly FfmpegService _ffmpeg;
    private readonly ILogger<VideoProcessingService> _logger;

    public VideoProcessingService(FfmpegService ffmpeg, ILogger<VideoProcessingService> logger)
    {
        _ffmpeg = ffmpeg;
        _logger = logger;
    }

    public Task<string> ExtractAudioAsync(string videoFilePath, IProgress<double>? progress = null,
        CancellationToken ct = default)
        => _ffmpeg.ExtractAudioAsync(videoFilePath, progress, ct);

    public Task BurnSubtitlesAsync(string videoFilePath, string subtitlesFilePath, string outputPath,
        string? fontName = null, int fontSize = 24, CancellationToken ct = default)
        => _ffmpeg.BurnSubtitlesAsync(videoFilePath, subtitlesFilePath, outputPath, fontName, fontSize, ct);

    public Task<bool> IsFfmpegAvailableAsync()
        => Task.FromResult(_ffmpeg.IsFfmpegAvailable());

    public Task DownloadFfmpegAsync(IProgress<double>? progress = null, CancellationToken ct = default)
        => _ffmpeg.DownloadFfmpegAsync(progress, ct);

    public string GetFfmpegPath() => _ffmpeg.GetFfmpegPath();

    public string? GetVideoDuration(string videoFilePath)
        => _ffmpeg.GetVideoDuration(videoFilePath);
}
