namespace SubtitleStudio.Core.Models;

public enum ExportFormat
{
    Srt,
    Vtt
}

public class ExportSettings
{
    public ExportFormat Format { get; set; } = ExportFormat.Srt;
    public string FontName { get; set; } = "Arial";
    public int FontSize { get; set; } = 24;
    public string FontColor { get; set; } = "white";
    public bool BurnIntoVideo { get; set; }
    public string? OutputDirectory { get; set; }
    public List<string> SelectedLanguages { get; set; } = [];
}
