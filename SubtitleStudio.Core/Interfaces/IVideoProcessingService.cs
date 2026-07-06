namespace SubtitleStudio.Core.Interfaces;

public interface IVideoProcessingService
{
    Task<string> ExtractAudioAsync(string videoFilePath, IProgress<double>? progress = null, CancellationToken ct = default);
    Task BurnSubtitlesAsync(string videoFilePath, string subtitlesFilePath, string outputPath,
        string? fontName = null, int fontSize = 24, CancellationToken ct = default);
    Task<bool> IsFfmpegAvailableAsync();
    Task DownloadFfmpegAsync(IProgress<double>? progress = null, CancellationToken ct = default);
    string GetFfmpegPath();
    string? GetVideoDuration(string videoFilePath);
}
