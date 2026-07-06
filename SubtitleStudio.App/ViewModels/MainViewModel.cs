using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SubtitleStudio.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private int _currentStep;

    [ObservableProperty]
    private string _currentStepName = "Source";

    public SourceViewModel SourceVm { get; }
    public TranscribeViewModel TranscribeVm { get; }
    public EditSubtitlesViewModel EditSubtitlesVm { get; }
    public TranslateViewModel TranslateVm { get; }
    public ExportViewModel ExportVm { get; }
    public SettingsViewModel SettingsVm { get; }

    public MainViewModel(
        SourceViewModel sourceVm,
        TranscribeViewModel transcribeVm,
        EditSubtitlesViewModel editSubtitlesVm,
        TranslateViewModel translateVm,
        ExportViewModel exportVm,
        SettingsViewModel settingsVm)
    {
        SourceVm = sourceVm;
        TranscribeVm = transcribeVm;
        EditSubtitlesVm = editSubtitlesVm;
        TranslateVm = translateVm;
        ExportVm = exportVm;
        SettingsVm = settingsVm;

        // Wire up the shared state
        SourceVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SourceViewModel.VideoFilePath))
                TranscribeVm.VideoFilePath = SourceVm.VideoFilePath;
            else if (e.PropertyName == nameof(SourceViewModel.SelectedModelSize))
                TranscribeVm.SelectedModelSize = SourceVm.SelectedModelSize;
        };

        TranscribeVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TranscribeViewModel.SubtitleTrack) && TranscribeVm.SubtitleTrack != null)
            {
                EditSubtitlesVm.SubtitleTrack = TranscribeVm.SubtitleTrack;
                TranslateVm.SubtitleTrack = TranscribeVm.SubtitleTrack;
                ExportVm.SubtitleTrack = TranscribeVm.SubtitleTrack;
            }
        };
    }

    [RelayCommand]
    private void NavigateTo(string? stepStr)
    {
        if (int.TryParse(stepStr, out var step) && step >= 0 && step <= 5)
        {
            CurrentStep = step;
            CurrentStepName = step switch
            {
                0 => "Source",
                1 => "Transcribe",
                2 => "Edit Subtitles",
                3 => "Translate",
                4 => "Export",
                5 => "Settings",
                _ => "Source"
            };
        }
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStep < 5)
            NavigateTo((CurrentStep + 1).ToString());
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep > 0)
            NavigateTo((CurrentStep - 1).ToString());
    }
}
