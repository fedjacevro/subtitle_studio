using System.Windows;
using SubtitleStudio.App.Helpers;
using SubtitleStudio.App.Views;

namespace SubtitleStudio.App.Services;

public class ProgressDialogService
{
    public async Task<bool> RunAsync(string title, Func<IProgress<ProgressReport>, CancellationToken, Task> work)
    {
        var owner = Application.Current.MainWindow;
        var dialog = new ProgressDialog(title) { Owner = owner };
        var progress = new Progress<ProgressReport>(dialog.Report);
        var success = false;

        dialog.Loaded += async (_, _) =>
        {
            try
            {
                await work(progress, dialog.CancellationToken);
                if (!dialog.CancellationToken.IsCancellationRequested)
                {
                    success = true;
                    dialog.Complete("Done.");
                    await Task.Delay(400);
                    dialog.Dispatcher.Invoke(dialog.Close);
                }
            }
            catch (OperationCanceledException)
            {
                dialog.Dispatcher.Invoke(() =>
                {
                    dialog.Complete("Cancelled.");
                    dialog.Close();
                });
            }
            catch (Exception ex)
            {
                dialog.Dispatcher.Invoke(() =>
                {
                    dialog.Complete($"Failed: {ex.Message}");
                });
                await Task.Delay(1500);
                dialog.Dispatcher.Invoke(dialog.Close);
            }
        };

        dialog.ShowDialog();
        return success && !dialog.WasCancelled;
    }
}