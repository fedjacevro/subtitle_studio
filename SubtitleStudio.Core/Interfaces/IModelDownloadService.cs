namespace SubtitleStudio.Core.Interfaces;

public interface IModelDownloadService
{
    Task DownloadFileAsync(string url, string destinationPath, IProgress<double>? progress = null,
        CancellationToken ct = default);
    bool FileExists(string path);
    long GetFileSize(string path);
    string GetModelsDirectory();
    string GetWhisperModelsDirectory();
    string GetLlmModelsDirectory();
}
