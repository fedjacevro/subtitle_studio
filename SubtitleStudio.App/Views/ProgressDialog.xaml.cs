using System.Windows;
using SubtitleStudio.App.Helpers;

namespace SubtitleStudio.App.Views;

public partial class ProgressDialog : Window
{
    private readonly CancellationTokenSource _cts = new();
    private bool _isClosing;

    public CancellationToken CancellationToken => _cts.Token;
    public bool WasCancelled { get; private set; }

    public ProgressDialog(string title)
    {
        InitializeComponent();
        TitleText.Text = title;
        Title = title;
    }

    public void Report(ProgressReport report)
    {
        Dispatcher.Invoke(() =>
        {
            MessageText.Text = report.Message;
            ProgressBar.Value = Math.Clamp(report.Progress, 0, 1);
        });
    }

    public void SetIndeterminate(string message)
    {
        Dispatcher.Invoke(() =>
        {
            MessageText.Text = message;
            ProgressBar.IsIndeterminate = true;
        });
    }

    public void Complete(string message)
    {
        Dispatcher.Invoke(() =>
        {
            MessageText.Text = message;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = 1;
            CancelButton.Content = "Close";
            CancelButton.Click -= CancelButton_Click;
            CancelButton.Click += (_, _) => CloseGracefully();
        });
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (CancelButton.Content?.ToString() == "Close")
        {
            CloseGracefully();
            return;
        }

        WasCancelled = true;
        _cts.Cancel();
        CancelButton.IsEnabled = false;
        MessageText.Text = "Cancelling...";
    }

    private void CloseGracefully()
    {
        if (_isClosing) return;
        _isClosing = true;
        DialogResult = !WasCancelled;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (!_cts.IsCancellationRequested)
            _cts.Cancel();
        _cts.Dispose();
        base.OnClosed(e);
    }
}