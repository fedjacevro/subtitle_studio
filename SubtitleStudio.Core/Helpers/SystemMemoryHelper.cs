using System.Runtime.InteropServices;

namespace SubtitleStudio.Core.Helpers;

public static class SystemMemoryHelper
{
    public const long RecommendedMinimumBytes = 8L * 1024 * 1024 * 1024;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    public static long GetAvailablePhysicalMemoryBytes()
    {
        try
        {
            var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                return (long)memStatus.ullAvailPhys;
            }
        }
        catch { /* fall through */ }

        // Fallback (less accurate)
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