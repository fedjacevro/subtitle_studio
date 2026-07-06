using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.App.Helpers;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace SubtitleStudio.App.Services;

public class ModelDownloadService : IModelDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ModelDownloadService> _logger;

    public ModelDownloadService(HttpClient httpClient, ILogger<ModelDownloadService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public string GetModelsDirectory()
    {
        var dir = Path.Combine(Constants.GetAppDataPath(), "models");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public string GetWhisperModelsDirectory()
    {
        var dir = Path.Combine(GetModelsDirectory(), "whisper");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public string GetLlmModelsDirectory()
    {
        var dir = Path.Combine(GetModelsDirectory(), "llm");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public async Task DownloadFileAsync(string url, string destinationPath,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _logger.LogInformation("Starting download from {Url} to {Path}", url, destinationPath);

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(destinationPath + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192 * 16];
        long bytesRead = 0;
        int read;
        while ((read = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            bytesRead += read;
            if (totalBytes > 0)
                progress?.Report((double)bytesRead / totalBytes);
        }

        await fileStream.FlushAsync(ct);
        await fileStream.DisposeAsync();

        if (File.Exists(destinationPath))
            File.Delete(destinationPath);
        File.Move(destinationPath + ".tmp", destinationPath);

        progress?.Report(1.0);
        _logger.LogInformation("Download completed: {Path}", destinationPath);
    }

    public bool FileExists(string path) => File.Exists(path);

    public long GetFileSize(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;
}
