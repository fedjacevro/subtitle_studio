using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SubtitleStudio.App.Services;
using SubtitleStudio.App.ViewModels;
using SubtitleStudio.App.Views;
using SubtitleStudio.Core.Interfaces;

namespace SubtitleStudio.App;

public partial class App : Application
{
    public static ServiceProvider? ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Services
        services.AddHttpClient<IModelDownloadService, ModelDownloadService>();
        services.AddSingleton<FfmpegService>();
        services.AddSingleton<IVideoProcessingService, VideoProcessingService>();
        services.AddSingleton<ITranscriptionService, TranscriptionService>();
        services.AddSingleton<ITranslationService, TranslationService>();
        services.AddSingleton<ISubtitleExportService, SubtitleExportService>();

        // ViewModels — all singletons to preserve state across tab navigation
        services.AddSingleton<SourceViewModel>();
        services.AddSingleton<TranscribeViewModel>();
        services.AddSingleton<EditSubtitlesViewModel>();
        services.AddSingleton<TranslateViewModel>();
        services.AddSingleton<ExportViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddTransient<SourceView>();
        services.AddTransient<TranscribeView>();
        services.AddTransient<EditSubtitlesView>();
        services.AddTransient<TranslateView>();
        services.AddTransient<ExportView>();
        services.AddTransient<SettingsView>();

        // Main Window
        services.AddSingleton<MainWindow>();

        ServiceProvider = services.BuildServiceProvider();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ServiceProvider?.Dispose();
        base.OnExit(e);
    }
}
