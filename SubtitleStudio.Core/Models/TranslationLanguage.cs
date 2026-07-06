namespace SubtitleStudio.Core.Models;

public class TranslationLanguage
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;

    public static List<TranslationLanguage> GetSupportedLanguages() =>
    [
        new() { Code = "bs", DisplayName = "Bosnian", NativeName = "Bosanski" },
        new() { Code = "sr-Latn", DisplayName = "Serbian (Latin)", NativeName = "Srpski (latinica)" },
        new() { Code = "sr-Cyrl", DisplayName = "Serbian (Cyrillic)", NativeName = "Српски (ћирилица)" },
        new() { Code = "hr", DisplayName = "Croatian", NativeName = "Hrvatski" },
        new() { Code = "en", DisplayName = "English", NativeName = "Engleski" },
        new() { Code = "de", DisplayName = "German", NativeName = "Deutsch" },
        new() { Code = "es", DisplayName = "Spanish", NativeName = "Español" },
        new() { Code = "it", DisplayName = "Italian", NativeName = "Italiano" },
        new() { Code = "fr", DisplayName = "French", NativeName = "Français" },
    ];

    public static List<string> GetWhisperLanguageCodes() =>
    [
        "auto", "en", "zh", "de", "es", "ru", "ko", "fr", "ja", "pt",
        "tr", "pl", "ca", "nl", "ar", "sv", "it", "id", "hi", "fi",
        "vi", "he", "uk", "el", "ms", "cs", "ro", "da", "hu", "ta",
        "no", "th", "ur", "hr", "bg", "lt", "la", "mi", "ml", "cy",
        "sk", "te", "fa", "lv", "bn", "sr", "az", "sl", "kn", "et",
        "mk", "br", "eu", "is", "hy", "ne", "mn", "bs", "kk", "sq",
        "sw", "gl", "mr", "pa", "si", "km", "sn", "yo", "so", "af",
        "oc", "ka", "be", "tg", "sd", "gu", "am", "yi", "lo", "uz",
        "fo", "ht", "ps", "tk", "nn", "mt", "sa", "lb", "my", "bo",
        "tl", "mg", "as", "tt", "haw", "ln", "ha", "ba", "jw", "su",
    ];
}
