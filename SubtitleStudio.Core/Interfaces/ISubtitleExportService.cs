using SubtitleStudio.Core.Models;

namespace SubtitleStudio.Core.Interfaces;

public interface ISubtitleExportService
{
    Task<string> ExportSrtAsync(SubtitleTrack track, string outputPath);
    Task<string> ExportVttAsync(SubtitleTrack track, string outputPath);
    Task<string> ExportAsync(SubtitleTrack track, string outputPath, ExportFormat format);
    Task<string> ExportTranslatedAsync(SubtitleTrack track, string outputPath, ExportFormat format, bool useProofread = true);
}
