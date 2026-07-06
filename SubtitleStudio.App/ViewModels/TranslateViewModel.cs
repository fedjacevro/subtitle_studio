using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
using SubtitleStudio.Core.Helpers;
using SubtitleStudio.App.Helpers;
using SubtitleStudio.App.Services;
using SubtitleStudio.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace SubtitleStudio.App.ViewModels;

public partial class TranslateViewModel : ObservableObject
{
    private readonly ITranslationService _translationService;
    private readonly DownloadConsentService _consentService;
    private readonly ProgressDialogService _progressDialog;
    private readonly UserNotificationService _notifications;
    private readonly ILogger<TranslateViewModel> _logger;
    private readonly AppSettings _settings;

    [ObservableProperty]
    private SubtitleTrack? _subtitleTrack;

    [ObservableProperty]
    private TranslationLanguage? _selectedTargetLanguage;

    [ObservableProperty]
    private bool _isTranslating;

    [ObservableProperty]
    private bool _isProofreading;

    [ObservableProperty]
    private double _translationProgress;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isLlmModelReady;

    [ObservableProperty]
    private string? _memoryWarning;

    [ObservableProperty]
    private bool _isDownloadingModel;

    [ObservableProperty]
    private bool _translationComplete;

    [ObservableProperty]
    private bool _proofreadingComplete;

    [ObservableProperty]
    private bool _showTranslation;

    public ObservableCollection<SubtitleItem> OriginalItems { get; } = [];
    public ObservableCollection<SubtitleItem> TranslatedItems { get; } = [];
    public ObservableCollection<TranslationLanguageOption> LanguageOptions { get; } = [];

    public List<TranslationLanguage> TargetLanguages { get; } = TranslationLanguage.GetSupportedLanguages();

    public TranslateViewModel(
        ITranslationService translationService,
        DownloadConsentService consentService,
        ProgressDialogService progressDialog,
        UserNotificationService notifications,
        ILogger<TranslateViewModel> logger)
    {
        _translationService = translationService;
        _consentService = consentService;
        _progressDialog = progressDialog;
        _notifications = notifications;
        _logger = logger;
        _settings = AppSettings.Load();

        foreach (var lang in TargetLanguages)
            LanguageOptions.Add(new TranslationLanguageOption { Language = lang });
    }

    partial void OnSubtitleTrackChanged(SubtitleTrack? value)
    {
        if (value == null) return;

        OriginalItems.Clear();
        TranslatedItems.Clear();
        foreach (var item in value.Items)
        {
            OriginalItems.Add(item);
            TranslatedItems.Add(item);
        }

        TranslationComplete = false;
        ProofreadingComplete = false;
        ShowTranslation = false;

        foreach (var option in LanguageOptions)
            option.IsTranslated = value.TranslatedLanguageCodes.Contains(option.Language.Code);
    }

    public async Task InitializeAsync()
    {
        UpdateMemoryWarning();
        IsLlmModelReady = await _translationService.IsModelReadyAsync();
        if (!IsLlmModelReady && MemoryWarning == null)
            StatusMessage = "LLM model not ready. Download it to enable translation.";
    }

    private void UpdateMemoryWarning()
    {
        if (!SystemMemoryHelper.HasMinimumAvailableMemory(_settings.Models.MinimumRamBytes))
        {
            var available = SystemMemoryHelper.GetAvailablePhysicalMemoryBytes();
            MemoryWarning =
                $"Low memory: {SystemMemoryHelper.FormatBytes(available)} available, " +
                $"{SystemMemoryHelper.FormatBytes(_settings.Models.MinimumRamBytes)} recommended for the LLM.";
        }
        else
        {
            MemoryWarning = null;
        }
    }

    [RelayCommand]
    private async Task DownloadModelAsync()
    {
        if (!_consentService.EnsureConsent("Llama 3.2 3B LLM", "~2 GB"))
            return;

        IsDownloadingModel = true;
        try
        {
            await _progressDialog.RunAsync("Downloading LLM Model", async (progress, ct) =>
            {
                var inner = new Progress<double>(p =>
                    progress.Report(new ProgressReport(p, $"Downloading LLM model... {p * 100:F0}%")));
                await _translationService.DownloadModelAsync(inner, ct);
                UpdateMemoryWarning();
                IsLlmModelReady = await _translationService.IsModelReadyAsync();
                StatusMessage = IsLlmModelReady ? "LLM model ready!" : "Model downloaded but could not load. Check memory.";
            });
        }
        finally
        {
            IsDownloadingModel = false;
        }
    }

    [RelayCommand]
    private async Task TranslateAsync()
    {
        if (SubtitleTrack == null || SelectedTargetLanguage == null)
        {
            StatusMessage = "Please select a target language first.";
            return;
        }

        await TranslateLanguageAsync(SelectedTargetLanguage.Code, SelectedTargetLanguage.DisplayName);
    }

    [RelayCommand]
    private async Task TranslateSelectedLanguagesAsync()
    {
        if (SubtitleTrack == null)
        {
            StatusMessage = "No subtitles loaded.";
            return;
        }

        var selected = LanguageOptions.Where(o => o.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "Select at least one target language.";
            return;
        }

        IsTranslating = true;
        try
        {
            await _progressDialog.RunAsync("Translating Subtitles", async (progress, ct) =>
            {
                for (var i = 0; i < selected.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var option = selected[i];
                    var inner = new Progress<double>(p =>
                    {
                        TranslationProgress = (i + p) / selected.Count;
                        progress.Report(new ProgressReport((i + p) / selected.Count,
                            $"Translating to {option.Language.DisplayName} ({i + 1}/{selected.Count})..."));
                    });

                    await _translationService.TranslateAsync(SubtitleTrack, option.Language.Code, "Latin", inner, ct);
                    option.IsTranslated = true;
                }

                TranslationComplete = true;
                ShowTranslation = true;
                StatusMessage = $"Translated to {selected.Count} language(s).";
            });
        }
        finally
        {
            IsTranslating = false;
        }
    }

    private async Task TranslateLanguageAsync(string languageCode, string displayName)
    {
        IsTranslating = true;
        TranslationComplete = false;

        try
        {
            await _progressDialog.RunAsync($"Translating to {displayName}", async (progress, ct) =>
            {
                var inner = new Progress<double>(p =>
                {
                    TranslationProgress = p;
                    progress.Report(new ProgressReport(p, $"Translating... {p * 100:F0}%"));
                });

                await _translationService.TranslateAsync(SubtitleTrack!, languageCode, "Latin", inner, ct);

                var option = LanguageOptions.FirstOrDefault(o => o.Language.Code == languageCode);
                if (option != null)
                    option.IsTranslated = true;

                TranslationComplete = true;
                ShowTranslation = true;
                StatusMessage = $"Translation to {displayName} complete!";
            });
        }
        finally
        {
            IsTranslating = false;
        }
    }

    [RelayCommand]
    private async Task ProofreadAsync()
    {
        if (SubtitleTrack == null || !TranslationComplete)
        {
            StatusMessage = "Please translate first.";
            return;
        }

        IsProofreading = true;
        try
        {
            await _progressDialog.RunAsync("Proofreading", async (progress, ct) =>
            {
                var inner = new Progress<double>(p =>
                {
                    TranslationProgress = p;
                    progress.Report(new ProgressReport(p, $"Proofreading... {p * 100:F0}%"));
                });

                await _translationService.ProofreadAsync(SubtitleTrack,
                    SelectedTargetLanguage?.Code ?? SubtitleTrack.TargetLanguage ?? "en", inner, ct);

                ProofreadingComplete = true;
                StatusMessage = "Proofreading complete!";
            });
        }
        finally
        {
            IsProofreading = false;
        }
    }

    [RelayCommand]
    private void ToggleTranslationView() => ShowTranslation = !ShowTranslation;
}

public partial class TranslationLanguageOption : ObservableObject
{
    public required TranslationLanguage Language { get; init; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isTranslated;

    public string DisplayName => Language.NativeName;
}