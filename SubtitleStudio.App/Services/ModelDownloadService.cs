using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.App.Helpers;
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
        IProgress<double>? progress = null, CancellationToken ct = default, string? expectedSha256 = null)
    {
        var dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var tempPath = destinationPath + ".tmp";
        var existingBytes = File.Exists(tempPath) ? new FileInfo(tempPath).Length : 0L;

        _logger.LogInformation("Starting download from {Url} to {Path} (resume from {Bytes} bytes)",
            url, destinationPath, existingBytes);

        var (statusCode, contentLength, stream) = await OpenDownloadStreamAsync(url, existingBytes, ct);

        if (statusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            existingBytes = 0;
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            (_, contentLength, stream) = await OpenDownloadStreamAsync(url, 0, ct);
        }
        else if (statusCode == HttpStatusCode.OK && existingBytes > 0)
        {
            existingBytes = 0;
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }

        bool downloadSucceeded = false;
        try
        {
            await using (stream)
            {
                await using var fileStream = new FileStream(tempPath,
                    existingBytes > 0 ? FileMode.Append : FileMode.Create,
                    FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192 * 16];
                long bytesRead = existingBytes;
                var totalBytes = contentLength > 0 ? existingBytes + contentLength : -1L;
                int read;

                while ((read = await stream.ReadAsync(buffer, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                    bytesRead += read;
                    if (totalBytes > 0)
                        progress?.Report((double)bytesRead / totalBytes);
                }
            }

            if (!string.IsNullOrWhiteSpace(expectedSha256))
                await VerifySha256Async(tempPath, expectedSha256);

            if (File.Exists(destinationPath))
                File.Delete(destinationPath);
            File.Move(tempPath, destinationPath);

            downloadSucceeded = true;
            progress?.Report(1.0);
            _logger.LogInformation("Download completed: {Path}", destinationPath);
        }
        finally
        {
            if (!downloadSucceeded && File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* best effort */ }
            }
        }
    }

    private async Task<(HttpStatusCode StatusCode, long ContentLength, Stream Stream)> OpenDownloadStreamAsync(
        string url, long resumeFrom, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (resumeFrom > 0)
            request.Headers.Range = new RangeHeaderValue(resumeFrom, null);

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
            return (response.StatusCode, 0, Stream.Null);

        response.EnsureSuccessStatusCode();
        var contentLength = response.Content.Headers.ContentLength ?? -1L;
        var stream = await response.Content.ReadAsStreamAsync(ct);
        return (response.StatusCode, contentLength, stream);
    }

    private async Task VerifySha256Async(string filePath, string expectedSha256)
    {
        await using var fileStream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(fileStream);
        var actual = Convert.ToHexString(hash);

        if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(filePath);
            throw new InvalidOperationException(
                $"Downloaded file failed SHA256 verification. Expected {expectedSha256}, got {actual}.");
        }

        _logger.LogInformation("SHA256 verification passed for {Path}", filePath);
    }

    public bool FileExists(string path) => File.Exists(path);

    public long GetFileSize(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;
}