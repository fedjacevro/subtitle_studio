using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using SubtitleStudio.App.Helpers;
using SubtitleStudio.Core.Helpers;
using SubtitleStudio.Core.Models;
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

        // Cleanup stale temp audio (best effort)
        try { if (File.Exists(audioPath)) File.Delete(audioPath); } catch { }

        // Also clean stale thumbnails dir occasionally (non-blocking)
        try
        {
            var thumbDir = Path.Combine(audioDir, "thumbnails");
            if (Directory.Exists(thumbDir) && Directory.GetFiles(thumbDir).Length > 50)
                Directory.Delete(thumbDir, true);
        }
        catch { }

        var ffmpegPath = GetFfmpegPath();
        _logger.LogInformation("Extracting audio from {Video} to {Audio} using {Ffmpeg}",
            videoFilePath, audioPath, ffmpegPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(videoFilePath);
        startInfo.ArgumentList.Add("-vn");
        startInfo.ArgumentList.Add("-acodec");
        startInfo.ArgumentList.Add("pcm_s16le");
        startInfo.ArgumentList.Add("-ar");
        startInfo.ArgumentList.Add("16000");
        startInfo.ArgumentList.Add("-ac");
        startInfo.ArgumentList.Add("1");
        startInfo.ArgumentList.Add(audioPath);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // Drain stdout to prevent pipe deadlock (even if not used)
        var stdoutTask = Task.Run(async () =>
        {
            while (await process.StandardOutput.ReadLineAsync(ct) is { } _ ) { /* discard */ }
        }, ct);

        // Read stderr to track progress (FFmpeg outputs progress to stderr)
        var errorOutput = new List<string>();
        string? line;
        while ((line = await process.StandardError.ReadLineAsync(ct)) != null)
        {
            if (line != null)
            {
                errorOutput.Add(line);
                // Parse duration for progress (basic; improved version uses pre-probe)
                if (line.Contains("time="))
                {
                    var timeStr = line.Split("time=")[1].Split(' ')[0].Trim();
                    // Report rough progress; caller maps to 0-0.3
                    progress?.Report(0.5);
                }
            }
        }

        await stdoutTask;
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
        string? fontName = null, int fontSize = 24, string? fontColor = null, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(outputPath))
            File.Delete(outputPath);

        var font = fontName ?? "Arial";
        var color = ToAssColor(fontColor ?? "white");
        var ffmpegPath = GetFfmpegPath();
        var escapedSubs = FfmpegPathHelper.EscapeSubtitlePath(subtitlesFilePath);
        var vfFilter = $"subtitles='{escapedSubs}':force_style='Fontname={font},Fontsize={fontSize},PrimaryColour={color}'";

        _logger.LogInformation("Burning subtitles: ffmpeg vf={Vf}", vfFilter);

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(videoFilePath);
        startInfo.ArgumentList.Add("-vf");
        startInfo.ArgumentList.Add(vfFilter);
        startInfo.ArgumentList.Add("-c:a");
        startInfo.ArgumentList.Add("copy");
        startInfo.ArgumentList.Add(outputPath);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var errorOutput = new List<string>();
        var readStderr = Task.Run(async () =>
        {
            while (await process.StandardError.ReadLineAsync(ct) is { } line)
                errorOutput.Add(line);
        }, ct);

        // Drain stdout to prevent pipe deadlock
        var readStdout = Task.Run(async () =>
        {
            while (await process.StandardOutput.ReadLineAsync(ct) is { } _) { /* discard */ }
        }, ct);

        await process.WaitForExitAsync(ct);
        await Task.WhenAll(readStderr, readStdout);

        if (process.ExitCode != 0)
        {
            var error = string.Join("\n", errorOutput);
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
                FileName = path,
                Arguments = "-version",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;
            // Drain to avoid hang
            _ = process.StandardOutput.ReadToEnd();
            _ = process.StandardError.ReadToEnd();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "FFmpeg availability check failed");
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

        // Clean up zip (best effort)
        try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
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
            _ = process.StandardOutput.ReadToEnd();
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

    public async Task<VideoInfo> ProbeVideoAsync(string videoFilePath, CancellationToken ct = default)
    {
        var info = new VideoInfo
        {
            FilePath = videoFilePath,
            FileName = Path.GetFileName(videoFilePath),
            FileSizeDisplay = FormatFileSize(new FileInfo(videoFilePath).Length)
        };

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
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var output = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        _ = await stdoutTask; // drain to avoid deadlock

        info.Duration = ParseDuration(output);
        if (TimeSpan.TryParse(info.Duration?.Replace(',', '.'), CultureInfo.InvariantCulture, out var ts))
            info.DurationTimeSpan = ts;

        var resolutionMatch = Regex.Match(output, @"(\d{2,5})x(\d{2,5})");
        if (resolutionMatch.Success)
            info.Resolution = $"{resolutionMatch.Groups[1].Value}x{resolutionMatch.Groups[2].Value}";

        var videoCodecMatch = Regex.Match(output, @"Video:\s*(\w+)", RegexOptions.IgnoreCase);
        if (videoCodecMatch.Success)
            info.VideoCodec = videoCodecMatch.Groups[1].Value;

        var audioCodecMatch = Regex.Match(output, @"Audio:\s*(\w+)", RegexOptions.IgnoreCase);
        if (audioCodecMatch.Success)
            info.AudioCodec = audioCodecMatch.Groups[1].Value;

        return info;
    }

    public async Task<string?> ExtractThumbnailAsync(string videoFilePath, CancellationToken ct = default)
    {
        var thumbDir = Path.Combine(Constants.GetAppDataPath(), "temp", "thumbnails");
        Directory.CreateDirectory(thumbDir);
        var thumbPath = Path.Combine(thumbDir,
            $"{Path.GetFileNameWithoutExtension(videoFilePath)}_{Math.Abs(videoFilePath.GetHashCode())}.jpg");

        if (File.Exists(thumbPath))
            return thumbPath;

        var ffmpegPath = GetFfmpegPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = $"-y -ss 00:00:01 -i \"{videoFilePath}\" -vframes 1 -q:v 2 \"{thumbPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        _ = process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        return process.ExitCode == 0 && File.Exists(thumbPath) ? thumbPath : null;
    }

    private static string? ParseDuration(string ffmpegOutput)
    {
        var line = ffmpegOutput.Split('\n')
            .FirstOrDefault(l => l.Trim().StartsWith("Duration:", StringComparison.OrdinalIgnoreCase));
        return line?.Split("Duration:")[1].Split(',')[0].Trim();
    }

    private static string FormatFileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };

    private static string ToAssColor(string colorName) => colorName.Trim().ToLowerInvariant() switch
    {
        "white" => "&H00FFFFFF",
        "yellow" => "&H0000FFFF",
        "black" => "&H00000000",
        "red" => "&H000000FF",
        "green" => "&H0000FF00",
        "blue" => "&H00FF0000",
        "cyan" => "&H00FFFF00",
        _ => "&H00FFFFFF"
    };
}
