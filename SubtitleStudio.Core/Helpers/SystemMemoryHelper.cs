namespace SubtitleStudio.Core.Helpers;

public static class SystemMemoryHelper
{
    public const long RecommendedMinimumBytes = 8L * 1024 * 1024 * 1024;

    public static long GetAvailablePhysicalMemoryBytes()
    {
        var info = GC.GetGCMemoryInfo();
        return (long)info.TotalAvailableMemoryBytes;
    }

    public static bool HasMinimumAvailableMemory(long minimumBytes) =>
        GetAvailablePhysicalMemoryBytes() >= minimumBytes;

    public static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}