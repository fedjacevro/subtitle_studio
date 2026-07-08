using SubtitleStudio.Core.Interfaces;
using SubtitleStudio.Core.Models;
using SubtitleStudio.Core.Helpers;
using SubtitleStudio.App.Helpers;
using SubtitleStudio.Core.Configuration;
using Microsoft.Extensions.Logging;
using LLama;
using LLama.Common;
using LLama.Sampling;

namespace SubtitleStudio.App.Services;

public class TranslationService : ITranslationService, IDisposable
{
    private readonly IModelDownloadService _downloadService;
    private readonly ILogger<TranslationService> _logger;
    private readonly AppSettings _settings;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private LLamaWeights? _weights;
    private LLamaContext? _context;

    public TranslationService(IModelDownloadService downloadService, ILogger<TranslationService> logger)
    {
        _downloadService = downloadService;
        _logger = logger;
        _settings = AppSettings.Load();
    }

    private string GetModelPath()
    {
        return Path.Combine(_downloadService.GetLlmModelsDirectory(), Constants.LlmModelFileName);
    }

    public async Task<bool> IsModelReadyAsync(CancellationToken ct = default)
    {
        var modelPath = GetModelPath();
        if (!_downloadService.FileExists(modelPath))
            return false;

        if (!SystemMemoryHelper.HasMinimumAvailableMemory(_settings.Models.MinimumRamBytes))
        {
            var available = SystemMemoryHelper.GetAvailablePhysicalMemoryBytes();
            _logger.LogWarning("Insufficient memory for LLM. Available: {Available}, Required: {Required}",
                SystemMemoryHelper.FormatBytes(available), SystemMemoryHelper.FormatBytes(_settings.Models.MinimumRamBytes));
            return false;
        }

        if (_weights != null && _context != null)
            return true;

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_weights != null && _context != null)
                return true;

            // Re-check file (could have been deleted)
            if (!_downloadService.FileExists(modelPath))
                return false;

            var modelParams = new ModelParams(modelPath)
            {
                ContextSize = 2048,
                GpuLayerCount = 0, // CPU-only
                UseMemoryLock = false,
                BatchSize = 512,
            };

            _weights = await Task.Run(() => LLamaWeights.LoadFromFile(modelParams), ct);
            _context = _weights.CreateContext(modelParams);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load LLM model");
            // Clean partial state
            _context?.Dispose();
            _weights?.Dispose();
            _context = null;
            _weights = null;
            return false;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task DownloadModelAsync(IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var modelPath = GetModelPath();
        if (_downloadService.FileExists(modelPath))
        {
            progress?.Report(1.0);
            return;
        }

        _logger.LogInformation("Downloading LLM model...");
        await _downloadService.DownloadFileAsync(Constants.LlmModelDownloadUrl, modelPath, progress, ct,
            string.IsNullOrWhiteSpace(_settings.Models.LlmExpectedSha256) ? null : _settings.Models.LlmExpectedSha256);
    }

    private string GetScriptInstruction(string targetLanguage, string script)
    {
        if (targetLanguage == "sr-Cyrl")
            return "Output the translation in Serbian Cyrillic (ћирилица) script.";
        if (targetLanguage == "sr-Latn")
            return "Output the translation in Serbian Latin script.";
        return string.Empty; // Default script
    }

    public async Task TranslateAsync(SubtitleTrack track, string targetLanguage, string script = "Latin",
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (_weights == null || _context == null)
        {
            if (!await IsModelReadyAsync(ct))
                throw new InvalidOperationException("LLM model is not ready. Please download it first.");
        }

        var sourceLang = track.SourceLanguage == "auto" ? "the original language" : track.SourceLanguage;
        var targetName = TranslationLanguage.GetSupportedLanguages()
            .FirstOrDefault(l => l.Code == targetLanguage)?.NativeName ?? targetLanguage;

        var scriptInstruction = GetScriptInstruction(targetLanguage, script);
        var items = track.Items;
        var chunkSize = 10;
        var totalChunks = (int)Math.Ceiling((double)items.Count / chunkSize);

        for (int chunkIdx = 0; chunkIdx < totalChunks; chunkIdx++)
        {
            ct.ThrowIfCancellationRequested();

            var chunkItems = items.Skip(chunkIdx * chunkSize).Take(chunkSize).ToList();
            var textBlock = string.Join("\n", chunkItems.Select((item, idx) =>
                $"[{idx + 1}] {item.Text}"));

            var prompt = $@"Translate the following subtitles from {sourceLang} to {targetName}. 
Preserve the line breaks and the number of lines exactly. 
Keep the numbered markers like [1], [2], etc. in place.
{scriptInstruction}
Only return the translated text, nothing else.

Source:
{textBlock}";

            _logger.LogInformation("Translating chunk {Chunk}/{Total}", chunkIdx + 1, totalChunks);

            var result = await ExecuteInferenceAsync(prompt, ct);

            var parsed = LlmResponseParser.ParseNumberedLines(result, chunkIdx, chunkSize, items, (item, text) =>
                item.SetTranslation(targetLanguage, text));
            if (parsed < chunkItems.Count)
            {
                _logger.LogWarning(
                    "Translation chunk {Chunk}/{Total}: parsed {Parsed}/{Expected} lines. Some subtitles may be missing.",
                    chunkIdx + 1, totalChunks, parsed, chunkItems.Count);
            }

            progress?.Report((double)(chunkIdx + 1) / totalChunks);
        }

        track.TargetLanguage = targetLanguage;
        track.RegisterTranslatedLanguage(targetLanguage);
        progress?.Report(1.0);
        _logger.LogInformation("Translation completed for {Count} items", items.Count);
    }

    public async Task ProofreadAsync(SubtitleTrack track, string targetLanguage,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (_weights == null || _context == null)
        {
            if (!await IsModelReadyAsync(ct))
                throw new InvalidOperationException("LLM model is not ready. Please download it first.");
        }

        var targetName = TranslationLanguage.GetSupportedLanguages()
            .FirstOrDefault(l => l.Code == targetLanguage)?.NativeName ?? targetLanguage;

        var items = track.Items;
        var chunkSize = 10;
        var totalChunks = (int)Math.Ceiling((double)items.Count / chunkSize);

        for (int chunkIdx = 0; chunkIdx < totalChunks; chunkIdx++)
        {
            ct.ThrowIfCancellationRequested();

            var chunkItems = items.Skip(chunkIdx * chunkSize).Take(chunkSize).ToList();
            var textBlock = string.Join("\n", chunkItems.Select((item, idx) =>
                $"[{idx + 1}] {item.GetDisplayTextForLanguage(targetLanguage, useProofread: false)}"));

            var prompt = $@"Proofread and correct any grammar or spelling mistakes in the following {targetName} subtitles. 
Keep the meaning and line count exactly the same. 
Keep the numbered markers like [1], [2], etc. in place.
Return only the corrected text.

Source:
{textBlock}";

            _logger.LogInformation("Proofreading chunk {Chunk}/{Total}", chunkIdx + 1, totalChunks);

            var result = await ExecuteInferenceAsync(prompt, ct);

            var parsed = LlmResponseParser.ParseNumberedLines(result, chunkIdx, chunkSize, items, (item, text) =>
                item.SetProofread(targetLanguage, text));
            if (parsed < chunkItems.Count)
            {
                _logger.LogWarning(
                    "Proofread chunk {Chunk}/{Total}: parsed {Parsed}/{Expected} lines. Some subtitles may be missing.",
                    chunkIdx + 1, totalChunks, parsed, chunkItems.Count);
            }

            progress?.Report((double)(chunkIdx + 1) / totalChunks);
        }

        progress?.Report(1.0);
        _logger.LogInformation("Proofreading completed for {Count} items", items.Count);
    }

    private async Task<string> ExecuteInferenceAsync(string prompt, CancellationToken ct = default)
    {
        if (_context == null || _weights == null)
            throw new InvalidOperationException("LLM model not loaded");

        ct.ThrowIfCancellationRequested();

        return await Task.Run(async () =>
        {
            var executor = new InteractiveExecutor(_context);
            var session = new ChatSession(executor);
            var inferenceParams = new InferenceParams
            {
                MaxTokens = 2048,
                AntiPrompts = ["User:", "Assistant:", "\n\n"],
                SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.1f }
            };

            session.History.AddMessage(AuthorRole.User, prompt);
            var result = string.Empty;
            await foreach (var text in session.ChatAsync(session.History.Messages.Last(), inferenceParams))
            {
                ct.ThrowIfCancellationRequested();
                result += text;
            }

            return result.Trim();
        }, ct);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _weights?.Dispose();
        _context = null;
        _weights = null;
        _loadLock.Dispose();
    }
}
