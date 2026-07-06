using SubtitleStudio.Core.Models;

namespace SubtitleStudio.Core.Interfaces;

public interface ITranslationService
{
    Task<bool> IsModelReadyAsync();
    Task DownloadModelAsync(IProgress<double>? progress = null, CancellationToken ct = default);
    Task TranslateAsync(SubtitleTrack track, string targetLanguage, string script = "Latin",
        IProgress<double>? progress = null, CancellationToken ct = default);
    Task ProofreadAsync(SubtitleTrack track, string targetLanguage,
        IProgress<double>? progress = null, CancellationToken ct = default);
}
