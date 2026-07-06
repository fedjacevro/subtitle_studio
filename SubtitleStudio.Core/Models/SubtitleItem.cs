using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SubtitleStudio.Core.Models;

/// <summary>
/// Represents a single subtitle entry with timing and text.
/// Implements INotifyPropertyChanged for WPF DataGrid editing support.
/// </summary>
public class SubtitleItem : INotifyPropertyChanged
{
    private int _index;
    private TimeSpan _startTime;
    private TimeSpan _endTime;
    private string _text = string.Empty;
    private string? _translatedText;
    private string? _proofreadText;
    private readonly Dictionary<string, string> _translations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _proofreadByLanguage = new(StringComparer.OrdinalIgnoreCase);

    public int Index
    {
        get => _index;
        set => SetProperty(ref _index, value);
    }

    public TimeSpan StartTime
    {
        get => _startTime;
        set
        {
            if (SetProperty(ref _startTime, value))
                OnPropertyChanged(nameof(Duration));
        }
    }

    public TimeSpan EndTime
    {
        get => _endTime;
        set
        {
            if (SetProperty(ref _endTime, value))
                OnPropertyChanged(nameof(Duration));
        }
    }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public string? TranslatedText
    {
        get => _translatedText;
        set => SetProperty(ref _translatedText, value);
    }

    public string? ProofreadText
    {
        get => _proofreadText;
        set => SetProperty(ref _proofreadText, value);
    }

    public TimeSpan Duration => EndTime - StartTime;

    public string DisplayText => ProofreadText ?? TranslatedText ?? Text;

    public IReadOnlyDictionary<string, string> Translations => _translations;

    public IReadOnlyDictionary<string, string> ProofreadByLanguage => _proofreadByLanguage;

    public void SetTranslation(string languageCode, string text)
    {
        _translations[languageCode] = text;
        TranslatedText = text;
        OnPropertyChanged(nameof(DisplayText));
    }

    public void SetProofread(string languageCode, string text)
    {
        _proofreadByLanguage[languageCode] = text;
        ProofreadText = text;
        OnPropertyChanged(nameof(DisplayText));
    }

    public bool TryGetTranslation(string languageCode, out string text) =>
        _translations.TryGetValue(languageCode, out text!);

    public bool TryGetProofread(string languageCode, out string text) =>
        _proofreadByLanguage.TryGetValue(languageCode, out text!);

    public string GetDisplayTextForLanguage(string? languageCode, bool useProofread = true)
    {
        if (string.IsNullOrEmpty(languageCode))
            return Text;

        if (useProofread && TryGetProofread(languageCode, out var proofread))
            return proofread;

        if (TryGetTranslation(languageCode, out var translated))
            return translated;

        return Text;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public SubtitleItem Clone()
    {
        var clone = new SubtitleItem
        {
            Index = Index,
            StartTime = StartTime,
            EndTime = EndTime,
            Text = Text,
            TranslatedText = TranslatedText,
            ProofreadText = ProofreadText
        };
        clone.CopyTranslationsFrom(this);
        return clone;
    }

    public void CopyTranslationsFrom(SubtitleItem source)
    {
        foreach (var (code, text) in source._translations)
            _translations[code] = text;
        foreach (var (code, text) in source._proofreadByLanguage)
            _proofreadByLanguage[code] = text;
    }
}
