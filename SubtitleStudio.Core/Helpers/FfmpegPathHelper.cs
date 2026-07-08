namespace SubtitleStudio.Core.Helpers;

public static class FfmpegPathHelper
{
    public static string EscapeSubtitlePath(string path) =>
        path.Replace("\\", "/")
            .Replace(":", "\\:")
            .Replace("'", "'\\''")
            .Replace(",", "\\,"); // help with filter arg lists
}