using System.Diagnostics;
using SubtitleStudio.App.Helpers;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace SubtitleStudio.App.Services;

public class FfmpegService
{
    private readonly ILogger<FfmpegService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public FfmpegService(ILogger<FfmpegService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string GetFfmpegPath()
    {
        var possiblePaths = new[]
        {
            Path.Combine(Constants.GetAppDataPath(), Constants.FfmpegSubfolder, Constants.FfmpegExeName),
            Path.Combine(Constants.GetAppDataPath(), "tools", "ffmpeg", "ffmpeg.exe"),
            "ffmpeg.exe"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;

            // Also check in subdirectories of ffmpeg folder
            var ffmpegDir = Path.GetDirectoryName(path);
            if (ffmpegDir != null && Directory.Exists(ffmpegDir))
            {
                var files = Directory.GetFiles(ffmpegDir, "ffmpeg.exe", SearchOption.AllDirectories);
                if (files.Length > 0)
                    return files[0];
            }
        }

        return "ffmpeg.exe"; // fallback to PATH
    }

    public string GetFfmpegToolsDirectory()
    {
        var dir = Path.Combine(Constants.GetAppDataPath(), Constants.ToolsSubfolder);
        Directory.CreateDirectory(dir);
        return dir;
    }

    public async Task<string> ExtractAudioAsync(string videoFilePath, IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var audioDir = Path.Combine(Constants.GetAppDataPath(), "temp");
        Directory.CreateDirectory(audioDir);
        var audioPath = Path.Combine(audioDir, Constants.TempAudioFileName);

        if (File.Exists(audioPath))
            File.Delete(audioPath);

        var ffmpegPath = GetFfmpegPath();
        _logger.LogInformation("Extracting audio from {Video} to {Audio} using {Ffmpeg}",
            videoFilePath, audioPath, ffmpegPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = $"-i \"{videoFilePath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 \"{audioPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // Read stderr to track progress (FFmpeg outputs progress to stderr)
        var errorOutput = new List<string>();
        string? line;
        while ((line = await process.StandardError.ReadLineAsync(ct)) != null)
        {
            if (line != null)
            {
                errorOutput.Add(line);
                // Parse duration for progress
                if (line.Contains("time="))
                {
                    var timeStr = line.Split("time=")[1].Split(' ')[0];
                    if (TimeSpan.TryParse(timeStr, out var current))
                    {
                        // We can't know total duration easily here, so just report indeterminate
                        progress?.Report(0.5);
                    }
                }
            }
        }

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var errors = string.Join("\n", errorOutput);
            _logger.LogError("FFmpeg extraction failed: {Errors}", errors);
            throw new InvalidOperationException($"FFmpeg audio extraction failed: {errors}");
        }

        progress?.Report(1.0);
        _logger.LogInformation("Audio extraction completed: {Audio}", audioPath);
        return audioPath;
    }

    public async Task BurnSubtitlesAsync(string videoFilePath, string subtitlesFilePath, string outputPath,
        string? fontName = null, int fontSize = 24, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(outputPath))
            File.Delete(outputPath);

        var font = fontName ?? "Arial";
        var ffmpegPath = GetFfmpegPath();
        var arguments = $"-i \"{videoFilePath}\" -vf \"subtitles='{subtitlesFilePath.Replace("'", "'\\''")}':force_style='Fontname={font},Fontsize={fontSize}'\" -c:a copy \"{outputPath}\"";

        _logger.LogInformation("Burning subtitles: ffmpeg {Args}", arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments.Replace("'\\''", "'\\\\''"),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(ct);
            _logger.LogError("FFmpeg burn failed: {Error}", error);
            throw new InvalidOperationException($"Failed to burn subtitles: {error}");
        }

        _logger.LogInformation("Subtitles burned to: {Output}", outputPath);
    }

    public bool IsFfmpegAvailable()
    {
        try
        {
            var path = GetFfmpegPath();
            if (File.Exists(path))
                return true;

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = "-version",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit(5000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task DownloadFfmpegAsync(IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var toolsDir = GetFfmpegToolsDirectory();
        var zipPath = Path.Combine(toolsDir, Constants.FfmpegZipName);

        _logger.LogInformation("Downloading FFmpeg...");

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(10);
        using var response = await client.GetAsync(Constants.FfmpegDownloadUrl,
            HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

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

        await fileStream.DisposeAsync();

        // Extract the zip
        _logger.LogInformation("Extracting FFmpeg archive...");
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, toolsDir, overwriteFiles: true);

        // Find ffmpeg.exe in the extracted folder
        var extractedDirs = Directory.GetDirectories(toolsDir);
        foreach (var dir in extractedDirs)
        {
            var ffmpegFiles = Directory.GetFiles(dir, "ffmpeg.exe", SearchOption.AllDirectories);
            foreach (var ffmpegFile in ffmpegFiles)
            {
                var destDir = Path.Combine(toolsDir, "ffmpeg");
                Directory.CreateDirectory(destDir);
                var destPath = Path.Combine(destDir, "ffmpeg.exe");
                if (!File.Exists(destPath))
                    File.Move(ffmpegFile, destPath);
            }
        }

        // Clean up zip
        File.Delete(zipPath);
        progress?.Report(1.0);
        _logger.LogInformation("FFmpeg download and extraction completed");
    }

    public string? GetVideoDuration(string videoFilePath)
    {
        try
        {
            var ffmpegPath = GetFfmpegPath();
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{videoFilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(5000);

            var durationLine = error.Split('\n')
                .FirstOrDefault(l => l.Trim().StartsWith("Duration:"));
            if (durationLine != null)
            {
                var timeStr = durationLine.Split("Duration:")[1].Split(',')[0].Trim();
                return timeStr;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get video duration");
        }
        return null;
    }
}
