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

    public SubtitleItem Clone() => new()
    {
        Index = Index,
        StartTime = StartTime,
        EndTime = EndTime,
        Text = Text,
        TranslatedText = TranslatedText,
        ProofreadText = ProofreadText
    };
}
