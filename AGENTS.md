# AGENTS.md — Subtitle Studio

## Project Overview
Subtitle Studio is a professional Windows desktop application built with .NET (C#, WPF) that enables users to:
- Load a video file
- Automatically transcribe its audio to timecoded subtitles using a local Whisper AI model (CPU-only)
- Translate the generated subtitles into multiple languages using a local large language model (LLM)
- Proofread/refine translations using the same LLM
- Export the final subtitles as SRT/VTT or burn them directly into the video

All AI models and necessary external tools are downloaded and configured automatically on first use. The application must run on a CPU-only machine with no GPU requirement.

## Technology Stack
- **Framework**: .NET 8 (or .NET 9) – WPF with MVVM pattern
- **Language**: C# 12
- **UI**: Material Design in XAML or modern Fluent style; responsive and professional
- **Audio extraction**: FFmpeg (bundled/downloaded automatically)
- **Speech-to-Text**: Whisper.net (NuGet package – wraps whisper.cpp) using `ggml` models
- **Translation/Proofreading**: LLamaSharp (NuGet package – wraps llama.cpp) using `GGUF` quantised models
- **Packaging**: Windows MSIX or ClickOnce with self-contained deployment
- **Background processing**: `async/await`, `Task.Run` for CPU-bound AI inference to keep UI responsive

## Folder Structure (recommended)
SubtitleStudio/
├── SubtitleStudio.App/ # Main WPF application
│ ├── Models/
│ ├── ViewModels/
│ ├── Views/
│ ├── Services/
│ │ ├── TranscriptionService.cs
│ │ ├── TranslationService.cs
│ │ ├── VideoProcessingService.cs
│ │ ├── ModelDownloadService.cs
│ │ └── FfmpegService.cs
│ ├── Helpers/
│ ├── Resources/
│ └── appsettings.json
├── SubtitleStudio.Core/ # Shared logic, DTOs, interfaces
└── SubtitleStudio.Tests/ # Unit tests

## Core Functionality

### 1. Video Loading & Audio Extraction
- User opens a video file (`.mp4`, `.mkv`, `.avi`, `.mov`, etc.)
- Display video thumbnail or first frame; basic playback control (optional, using `MediaElement`)
- Extract audio track to a 16kHz mono WAV file using FFmpeg:
ffmpeg -i input.mp4 -vn -acodec pcm_s16le -ar 16000 -ac 1 audio.wav
- The extracted WAV is fed to the Whisper model for transcription.

### 2. Transcription with Whisper (CPU-only)
- Use **Whisper.net** (`Whisper.net.Runtime` NuGet) with a `ggml` model.
- Model options (selectable by user or auto-select based on system capability):
- `tiny` (fastest, lowest accuracy) – good for real-time preview
- `base`
- `small` (recommended default for CPU)
- `medium` (better accuracy, slower)
- `large-v3` (optional, but may be too slow on CPU)
- On first run, the required model is automatically downloaded from Hugging Face (`ggerganov/whisper.cpp`) and stored in `%LocalAppData%\SubtitleStudio\models\whisper\`.
- Transcription output: segments with start time, end time, text → saved as `SubtitleTrack` object (list of `SubtitleItem`).
- Support for selecting source language (e.g., Bosnian, English, auto-detect). Whisper can auto-detect language; provide an option to force a language to improve accuracy.

### 3. Subtitle Editing & Timeline
- Display subtitles in a data grid with columns: `#`, `Start`, `End`, `Duration`, `Text`.
- Inline editing of text and timecodes.
- Optional waveform/timeline visualisation for precise alignment (can be simplified to an interactive list for MVP).
- Real-time preview: clicking a subtitle seeks the video to that time (requires FFmpeg-based frame extraction or video player control).

### 4. Translation with Local LLM (CPU-only)
- Use **LLamaSharp** (`LLamaSharp.Backend.Cpu` NuGet) with a suitable `GGUF` quantised model.
- Supported target languages:
- Bosanski (Bosnian)
- Srpski (Serbian – Latin script)
- Srpski (ćirilica) (Serbian – Cyrillic script)
- Hrvatski (Croatian)
- Engleski (English)
- Njemački (German)
- Španski (Spanish)
- Italijanski (Italian)
- Francuski (French)
- The app should detect the original language from Whisper output or let the user specify it.
- Translation pipeline:
1. Concatenate subtitle text blocks (keeping timing information separate) or send chunks of 5–10 subtitles to the LLM.
2. Prompt template (example):
Translate the following subtitles from {sourceLang} to {targetLang}. Preserve the line breaks and number of lines. Only return the translated text, nothing else.

Source:
{textBlock}
3. Parse LLM output back into individual subtitle entries, preserving timing.
4. For Cyrillic script, explicitly instruct the LLM to output Cyrillic.
- Proofreading: after translation, an optional step can re‑feed the translated text to the LLM with a prompt like:
Proofread and correct any grammar or spelling mistakes in the following {targetLang} subtitles. Keep the meaning and line count exactly the same. Return only the corrected text.
- The user can manually edit translations before exporting.

### 5. Export Options
- **Subtitle files**: Export as `.srt` or `.vtt` with proper formatting.
- **Burned-in subtitles**: Use FFmpeg to hardcode subtitles onto the video for the selected language. Command example:
ffmpeg -i input.mp4 -vf "subtitles=subtitles.srt:force_style='Fontsize=24'" output.mp4
The subtitle file should be properly encoded (UTF-8 with BOM) and the style configurable (font, size, color) in the UI.
- Allow batch export: all selected languages at once (e.g., produce one video per language or multiple SRT files).

## AI Model Download & Management
- **Whisper models**: The `Whisper.net` library can download a model directly via `WhisperDownloader`. Integrate that, specifying a custom folder under `%LocalAppData%\SubtitleStudio\models\whisper`. Show a progress bar during download.
- **LLM model** (GGUF): The recommended model for CPU is **Llama 3.2 3B Instruct Q4_K_M** (approx. 2 GB), which handles translation and proofreading well in multiple languages. Alternative: **Phi-3-mini-4k-instruct** or **Mistral-7B-Instruct-v0.2** if performance allows.
- Implementation:
1. On first launch or when user triggers translation, check if model file exists in `%LocalAppData%\SubtitleStudio\models\llm\`.
2. If missing, download from a reliable source (Hugging Face direct link, e.g., `https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf`). Use `HttpClient` with resume support.
3. After download, verify file integrity (optional SHA256) and load it via LLamaSharp.
- All downloads happen in the background with UI progress indication; the application remains usable during download (except the feature requiring the model).
- System requirements: minimum 8 GB RAM recommended; the app should check available memory before loading a large model and warn the user.

## UI Design Guidelines
- **Main window**: Modern, dark theme (professional video tool aesthetic).
- **Workflow steps** presented as a left-side navigation or a tab control:
1. **Source** – Load video, select whisper model size, source language.
2. **Transcribe** – Start transcription, view progress, see raw text.
3. **Edit Subtitles** – Timeline/list editor, search/replace, merge/split lines.
4. **Translate** – Choose target languages, optionally proofread, review side-by-side.
5. **Export** – Format selection, burn-in settings, batch options.
- Use **FontAwesome** or **Segoe MDL2 Assets** for icons.
- Responsive layout: subtitles list takes most space, preview pane can be docked or collapsed.

## Important Implementation Details
- **Whisper.net**:
- Use `WhisperProcessorBuilder` to create processor.
- Process the audio file as a whole or in streaming mode (prefer whole file for accuracy).
- The library returns segments; map them to `SubtitleItem` (Start, End, Text).
- **LLamaSharp**:
- Use `LLamaWeights.LoadFromFile` with `ModelParams` configured for CPU-only inference (no GPU layers).
- Set `ContextSize` to at least 2048 for handling subtitle chunks.
- Infer with `InferenceParams` `MaxTokens` and `Temperature` 0.1 for consistent translation.
- Always dispose the context after use to free memory.
- **FFmpeg**:
- Auto-download a Windows build (e.g., from `gyan.dev`) into `%LocalAppData%\SubtitleStudio\tools\ffmpeg\`.
- Extract the archive (use `System.IO.Compression` for ZIP or 7z) and locate `ffmpeg.exe`.
- Check for its existence before any video processing. Provide a download button in settings.
- **Error handling**: Wrap all model inference in try-catch, show user-friendly messages, log exceptions.
- **Localization**: The application UI itself should be in English; the translated subtitle languages cover the target list.

## Development Milestones
1. **Project scaffold** – WPF with MVVM, basic navigation, dark theme.
2. **Video loading + audio extraction** – Integrate FFmpeg download and extraction, display video info.
3. **Whisper transcription** – Integrate Whisper.net, model download, basic subtitle generation and display.
4. **Subtitle editor** – Editable list, timecode validation, video preview seek.
5. **LLM translation** – Integrate LLamaSharp, model download, translation prompt logic for one language pair.
6. **Proofreading** – Add post-translation refinement step.
7. **Export** – SRT/VTT export; burned-in subtitles via FFmpeg.
8. **Batch & settings** – Multiple language export, font/style configuration, model management UI.
9. **Polish & testing** – Error handling, performance tuning, user feedback.

## Final Notes
- All third-party components (FFmpeg, Whisper model, LLM model) must be downloaded automatically with user consent, never bundled in the installer to keep distribution small.
- The app must handle long-running tasks asynchronously, showing a cancellable progress dialog.
- Log key operations (transcription, translation) into a local log file for debugging.
- Keep the architecture modular so alternative STT or LLM backends can be swapped later.

If you have any questions or need clarification while building, ask immediately.
