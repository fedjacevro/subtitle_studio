using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
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
        string? fontName = null, int fontSize = 24, string? fontColor = null, CancellationToken ct = default)
        => _ffmpeg.BurnSubtitlesAsync(videoFilePath, subtitlesFilePath, outputPath, fontName, fontSize, fontColor, ct);

    public Task<bool> IsFfmpegAvailableAsync()
        => Task.FromResult(_ffmpeg.IsFfmpegAvailable());

    public Task DownloadFfmpegAsync(IProgress<double>? progress = null, CancellationToken ct = default)
        => _ffmpeg.DownloadFfmpegAsync(progress, ct);

    public Task<VideoInfo> GetVideoInfoAsync(string videoFilePath, CancellationToken ct = default)
        => _ffmpeg.ProbeVideoAsync(videoFilePath, ct);

    public Task<string?> ExtractThumbnailAsync(string videoFilePath, CancellationToken ct = default)
        => _ffmpeg.ExtractThumbnailAsync(videoFilePath, ct);

    public string GetFfmpegPath() => _ffmpeg.GetFfmpegPath();

    public string? GetVideoDuration(string videoFilePath)
        => _ffmpeg.GetVideoDuration(videoFilePath);
}