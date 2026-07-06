using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SubtitleStudio.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private int _currentStep;

    [ObservableProperty]
    private string _currentStepName = "Source";

    [ObservableProperty]
    private string _globalStatus = "Ready";

    [ObservableProperty]
    private bool _isOperationRunning;

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

        TranscribeVm.VideoFilePath = SourceVm.VideoFilePath;
        TranscribeVm.SelectedModelSize = SourceVm.SelectedModelSize;
        TranscribeVm.SelectedSourceLanguage = SourceVm.SelectedSourceLanguage;
        SettingsVm.SelectedWhisperModel = SourceVm.SelectedModelSize;
        TranscribeVm.CheckModelStatus();

        SourceVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SourceViewModel.VideoFilePath))
            {
                TranscribeVm.VideoFilePath = SourceVm.VideoFilePath;
                EditSubtitlesVm.VideoFilePath = SourceVm.VideoFilePath;
            }
            else if (e.PropertyName == nameof(SourceViewModel.SelectedModelSize))
            {
                TranscribeVm.SelectedModelSize = SourceVm.SelectedModelSize;
                SettingsVm.SelectedWhisperModel = SourceVm.SelectedModelSize;
                TranscribeVm.CheckModelStatus();
            }
            else if (e.PropertyName == nameof(SourceViewModel.SelectedSourceLanguage))
                TranscribeVm.SelectedSourceLanguage = SourceVm.SelectedSourceLanguage;
            else if (e.PropertyName == nameof(SourceViewModel.VideoDuration))
                EditSubtitlesVm.VideoDuration = SourceVm.VideoDuration;
            else if (e.PropertyName == nameof(SourceViewModel.IsFfmpegAvailable))
                SettingsVm.IsFfmpegAvailable = SourceVm.IsFfmpegAvailable;
        };

        SettingsVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.SelectedWhisperModel))
            {
                SourceVm.SelectedModelSize = SettingsVm.SelectedWhisperModel;
                TranscribeVm.SelectedModelSize = SettingsVm.SelectedWhisperModel;
                TranscribeVm.CheckModelStatus();
            }
            else if (e.PropertyName == nameof(SettingsViewModel.IsFfmpegAvailable))
                SourceVm.IsFfmpegAvailable = SettingsVm.IsFfmpegAvailable;
        };

        TranscribeVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TranscribeViewModel.SubtitleTrack) && TranscribeVm.SubtitleTrack != null)
            {
                EditSubtitlesVm.SubtitleTrack = TranscribeVm.SubtitleTrack;
                EditSubtitlesVm.VideoFilePath = TranscribeVm.SubtitleTrack.VideoFilePath ?? SourceVm.VideoFilePath;
                TranslateVm.SubtitleTrack = TranscribeVm.SubtitleTrack;
                ExportVm.SubtitleTrack = TranscribeVm.SubtitleTrack;
            }
        };

        WireStatus(SourceVm, () => SourceVm.StatusMessage);
        WireStatus(TranscribeVm, () => TranscribeVm.StatusMessage);
        WireStatus(EditSubtitlesVm, () => EditSubtitlesVm.StatusMessage);
        WireStatus(TranslateVm, () => TranslateVm.StatusMessage);
        WireStatus(ExportVm, () => ExportVm.StatusMessage);
        WireStatus(SettingsVm, () => SettingsVm.StatusMessage);

        WireBusy(SourceVm, nameof(SourceViewModel.IsBusy), nameof(SourceViewModel.IsLoadingVideo));
        WireBusy(TranscribeVm, nameof(TranscribeViewModel.IsTranscribing));
        WireBusy(TranslateVm, nameof(TranslateViewModel.IsTranslating), nameof(TranslateViewModel.IsProofreading),
            nameof(TranslateViewModel.IsDownloadingModel));
        WireBusy(ExportVm, nameof(ExportViewModel.IsExporting));
        WireBusy(SettingsVm, nameof(SettingsViewModel.IsDownloadingWhisper), nameof(SettingsViewModel.IsDownloadingLlm),
            nameof(SettingsViewModel.IsDownloadingFfmpeg));
    }

    partial void OnCurrentStepChanged(int value) => RefreshGlobalStatus();

    [RelayCommand]
    private void NavigateTo(string? stepStr)
    {
        if (int.TryParse(stepStr, out var step) && step is >= 0 and <= 5)
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
            RefreshGlobalStatus();
        }
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStep < 4)
            NavigateTo((CurrentStep + 1).ToString());
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep > 0)
            NavigateTo((CurrentStep - 1).ToString());
    }

    private void WireStatus(ObservableObject vm, Func<string?> getStatus)
    {
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == "StatusMessage")
                RefreshGlobalStatus();
        };
    }

    private void WireBusy(ObservableObject vm, params string[] propertyNames)
    {
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null && propertyNames.Contains(e.PropertyName))
                UpdateOperationRunning();
        };
    }

    private void RefreshGlobalStatus()
    {
        GlobalStatus = CurrentStep switch
        {
            0 => SourceVm.StatusMessage ?? "Ready",
            1 => TranscribeVm.StatusMessage ?? "Ready",
            2 => EditSubtitlesVm.StatusMessage ?? "Ready",
            3 => TranslateVm.StatusMessage ?? "Ready",
            4 => ExportVm.StatusMessage ?? "Ready",
            5 => SettingsVm.StatusMessage ?? "Ready",
            _ => "Ready"
        };
        UpdateOperationRunning();
    }

    private void UpdateOperationRunning()
    {
        IsOperationRunning = SourceVm.IsBusy || SourceVm.IsLoadingVideo || TranscribeVm.IsTranscribing
            || TranslateVm.IsTranslating || TranslateVm.IsProofreading || TranslateVm.IsDownloadingModel
            || ExportVm.IsExporting
            || SettingsVm.IsDownloadingWhisper || SettingsVm.IsDownloadingLlm || SettingsVm.IsDownloadingFfmpeg;
    }
}