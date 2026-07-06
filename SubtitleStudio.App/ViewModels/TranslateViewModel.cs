using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
using Microsoft.Extensions.Logging;

namespace SubtitleStudio.App.ViewModels;

public partial class TranslateViewModel : ObservableObject
{
    private readonly ITranslationService _translationService;
    private readonly ILogger<TranslateViewModel> _logger;

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
    private bool _isDownloadingModel;

    [ObservableProperty]
    private double _modelDownloadProgress;

    [ObservableProperty]
    private bool _translationComplete;

    [ObservableProperty]
    private bool _proofreadingComplete;

    [ObservableProperty]
    private bool _showTranslation;

    public ObservableCollection<SubtitleItem> OriginalItems { get; } = [];
    public ObservableCollection<SubtitleItem> TranslatedItems { get; } = [];

    public List<TranslationLanguage> TargetLanguages { get; } = TranslationLanguage.GetSupportedLanguages();

    public TranslateViewModel(ITranslationService translationService, ILogger<TranslateViewModel> logger)
    {
        _translationService = translationService;
        _logger = logger;
    }

    partial void OnSubtitleTrackChanged(SubtitleTrack? value)
    {
        if (value != null)
        {
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
        }
    }

    public async Task InitializeAsync()
    {
        IsLlmModelReady = await _translationService.IsModelReadyAsync();
    }

    [RelayCommand]
    private async Task DownloadModelAsync()
    {
        IsDownloadingModel = true;
        StatusMessage = "Downloading LLM model...";
        try
        {
            var progress = new Progress<double>(p =>
            {
                ModelDownloadProgress = p;
                StatusMessage = $"Downloading LLM model... {p * 100:F0}%";
            });
            await _translationService.DownloadModelAsync(progress);
            IsLlmModelReady = await _translationService.IsModelReadyAsync();
            StatusMessage = "LLM model ready!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download LLM model");
            StatusMessage = $"Download failed: {ex.Message}";
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

        IsTranslating = true;
        TranslationProgress = 0;
        StatusMessage = $"Translating to {SelectedTargetLanguage.DisplayName}...";
        TranslationComplete = false;

        try
        {
            var progress = new Progress<double>(p =>
            {
                TranslationProgress = p;
                StatusMessage = $"Translating... {p * 100:F0}%";
            });

            await _translationService.TranslateAsync(SubtitleTrack, SelectedTargetLanguage.Code, "Latin", progress);

            TranslationComplete = true;
            ShowTranslation = true;
            StatusMessage = $"Translation to {SelectedTargetLanguage.DisplayName} complete!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed");
            StatusMessage = $"Translation failed: {ex.Message}";
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
        TranslationProgress = 0;
        StatusMessage = "Proofreading...";

        try
        {
            var progress = new Progress<double>(p =>
            {
                TranslationProgress = p;
                StatusMessage = $"Proofreading... {p * 100:F0}%";
            });

            await _translationService.ProofreadAsync(SubtitleTrack,
                SelectedTargetLanguage?.Code ?? "en", progress);

            ProofreadingComplete = true;
            StatusMessage = "Proofreading complete!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Proofreading failed");
            StatusMessage = $"Proofreading failed: {ex.Message}";
        }
        finally
        {
            IsProofreading = false;
        }
    }

    [RelayCommand]
    private void ToggleTranslationView()
    {
        ShowTranslation = !ShowTranslation;
    }
}
