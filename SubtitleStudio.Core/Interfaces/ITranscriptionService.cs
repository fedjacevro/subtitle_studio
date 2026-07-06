using SubtitleStudio.Core.Models;

namespace SubtitleStudio.Core.Interfaces;

public interface ITranscriptionService
{
    Task<SubtitleTrack> TranscribeAsync(string audioFilePath, string language, WhisperModelSize modelSize,
        IProgress<double>? progress = null, CancellationToken ct = default);
    bool IsModelDownloaded(WhisperModelSize size);
    string GetModelPath(WhisperModelSize size);
    Task DownloadModelAsync(WhisperModelSize size, IProgress<double>? progress = null, CancellationToken ct = default);
}
