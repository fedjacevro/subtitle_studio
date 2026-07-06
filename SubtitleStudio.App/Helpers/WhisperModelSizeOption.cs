using SubtitleStudio.Core.Models;

namespace SubtitleStudio.App.Helpers;

/// <summary>
/// Wraps a WhisperModelSize enum value so WPF can bind to DisplayName as a property.
/// </summary>
public class WhisperModelSizeOption
{
    public WhisperModelSize Size { get; init; }
    public string DisplayName { get; init; } = string.Empty;

    public static List<WhisperModelSizeOption> GetAll() =>
    [
        new() { Size = WhisperModelSize.Tiny, DisplayName = WhisperModelSize.Tiny.GetDisplayName() },
        new() { Size = WhisperModelSize.Base, DisplayName = WhisperModelSize.Base.GetDisplayName() },
        new() { Size = WhisperModelSize.Small, DisplayName = WhisperModelSize.Small.GetDisplayName() },
        new() { Size = WhisperModelSize.Medium, DisplayName = WhisperModelSize.Medium.GetDisplayName() },
        new() { Size = WhisperModelSize.LargeV3, DisplayName = WhisperModelSize.LargeV3.GetDisplayName() },
    ];
}
