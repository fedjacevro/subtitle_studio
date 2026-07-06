namespace SubtitleStudio.App.Helpers;

public static class Constants
{
    public const string AppName = "Subtitle Studio";
    public const string AppDataFolder = "SubtitleStudio";
    public const string WhisperModelsSubfolder = @"models\whisper";
    public const string LlmModelsSubfolder = @"models\llm";
    public const string ToolsSubfolder = @"tools";
    public const string FfmpegSubfolder = @"tools\ffmpeg";
    public const string FfmpegExeName = "ffmpeg.exe";
    public const string TempAudioFileName = "extracted_audio.wav";

    // LLM model info
    public const string LlmModelFileName = "Llama-3.2-3B-Instruct-Q4_K_M.gguf";
    public const string LlmModelDownloadUrl =
        "https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf";

    // FFmpeg download
    public const string FfmpegDownloadUrl =
        "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
    public const string FfmpegZipName = "ffmpeg-release-essentials.zip";

    public static string GetAppDataPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, AppDataFolder);
    }
}
