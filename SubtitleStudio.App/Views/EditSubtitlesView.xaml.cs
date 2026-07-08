using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SubtitleStudio.App.ViewModels;

namespace SubtitleStudio.App.Views;

public partial class EditSubtitlesView : UserControl
{
    private EditSubtitlesViewModel? _viewModel;

    public EditSubtitlesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += (_, _) =>
        {
            if (_viewModel != null)
            {
                _viewModel.SeekRequested -= OnSeekRequested;
                Timeline.SegmentClicked -= OnTimelineSegmentClicked;
            }
            try
            {
                if (VideoPlayer != null)
                {
                    VideoPlayer.Stop();
                    VideoPlayer.Close();
                    VideoPlayer.Source = null;
                }
            }
            catch { }
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel = App.ServiceProvider!.GetRequiredService<EditSubtitlesViewModel>();
        DataContext = _viewModel;
        _viewModel.SeekRequested += OnSeekRequested;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(EditSubtitlesViewModel.VideoFilePath))
                LoadVideo(_viewModel.VideoFilePath);
        };

        Timeline.SegmentClicked += OnTimelineSegmentClicked;
        LoadVideo(_viewModel.VideoFilePath);
    }

    private void OnTimelineSegmentClicked(object? sender, int index) =>
        _viewModel?.OnTimelineSegmentClicked(index);

    private void LoadVideo(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return;

        try
        {
            VideoPlayer.Source = new Uri(path);
            VideoPlayer.Play();
            VideoPlayer.Pause();
        }
        catch (Exception)
        {
            // MediaElement may fail on some formats/containers; preview is best-effort
        }
    }

    private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Cancel)
            return;

        Dispatcher.BeginInvoke(() => _viewModel?.OnSubtitleEdited());
    }

    private void OnSeekRequested(TimeSpan position)
    {
        if (VideoPlayer.Source == null)
            return;

        if (VideoPlayer.NaturalDuration.HasTimeSpan)
            VideoPlayer.Position = position;
        else
            VideoPlayer.MediaOpened += SeekOnOpen;

        void SeekOnOpen(object? s, RoutedEventArgs e)
        {
            VideoPlayer.MediaOpened -= SeekOnOpen;
            VideoPlayer.Position = position;
        }
    }
}