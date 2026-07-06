using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SubtitleStudio.App.Helpers;
using SubtitleStudio.App.Services;
using SubtitleStudio.Core.Configuration;
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
        var appSettings = AppSettings.Load();
        var logDir = Path.Combine(Constants.GetAppDataPath(), "logs");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "subtitlestudio.log");

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            LoggingSetup.Configure(builder, appSettings.Logging, logPath);
        });

        services.AddSingleton(appSettings);
        services.AddSingleton<DownloadConsentService>();
        services.AddSingleton<UserNotificationService>();
        services.AddSingleton<ProgressDialogService>();

        services.AddHttpClient<IModelDownloadService, ModelDownloadService>();
        services.AddSingleton<FfmpegService>();
        services.AddSingleton<IVideoProcessingService, VideoProcessingService>();
        services.AddSingleton<ITranscriptionService, TranscriptionService>();
        services.AddSingleton<ITranslationService, TranslationService>();
        services.AddSingleton<ISubtitleExportService, SubtitleExportService>();

        services.AddSingleton<SourceViewModel>();
        services.AddSingleton<TranscribeViewModel>();
        services.AddSingleton<EditSubtitlesViewModel>();
        services.AddSingleton<TranslateViewModel>();
        services.AddSingleton<ExportViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        services.AddTransient<SourceView>();
        services.AddTransient<TranscribeView>();
        services.AddTransient<EditSubtitlesView>();
        services.AddTransient<TranslateView>();
        services.AddTransient<ExportView>();
        services.AddTransient<SettingsView>();

        services.AddSingleton<MainWindow>();

        ServiceProvider = services.BuildServiceProvider();

        var logger = ServiceProvider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Subtitle Studio v1.0 starting");

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (ServiceProvider?.GetService<ITranslationService>() is IDisposable disposable)
            disposable.Dispose();

        ServiceProvider?.Dispose();
        base.OnExit(e);
    }
}