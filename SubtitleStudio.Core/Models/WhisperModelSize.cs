namespace SubtitleStudio.Core.Models;

public enum WhisperModelSize
{
    Tiny,
    Base,
    Small,
    Medium,
    LargeV3
}

public static class WhisperModelSizeExtensions
{
    public static string ToModelName(this WhisperModelSize size) => size switch
    {
        WhisperModelSize.Tiny => "ggml-tiny.bin",
        WhisperModelSize.Base => "ggml-base.bin",
        WhisperModelSize.Small => "ggml-small.bin",
        WhisperModelSize.Medium => "ggml-medium.bin",
        WhisperModelSize.LargeV3 => "ggml-large-v3.bin",
        _ => "ggml-small.bin"
    };

    public static string GetDisplayName(this WhisperModelSize size) => size switch
    {
        WhisperModelSize.Tiny => "Tiny (fastest, lowest accuracy)",
        WhisperModelSize.Base => "Base",
        WhisperModelSize.Small => "Small (recommended)",
        WhisperModelSize.Medium => "Medium (better accuracy, slower)",
        WhisperModelSize.LargeV3 => "Large v3 (best accuracy, very slow)",
        _ => "Small (recommended)"
    };

    public static string GetDownloadUrl(this WhisperModelSize size) =>
        $"https://huggingface.co/ggerganov/whisper.cpp/resolve/main/{size.ToModelName()}";

    public static long GetApproximateSizeBytes(this WhisperModelSize size) => size switch
    {
        WhisperModelSize.Tiny => 75_000_000,
        WhisperModelSize.Base => 140_000_000,
        WhisperModelSize.Small => 460_000_000,
        WhisperModelSize.Medium => 1_500_000_000,
        WhisperModelSize.LargeV3 => 2_900_000_000,
        _ => 460_000_000
    };
}
