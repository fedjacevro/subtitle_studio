using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SubtitleStudio.Core.Models;

namespace SubtitleStudio.App.Controls;

public partial class SubtitleTimelineControl : UserControl
{
    public static readonly DependencyProperty SegmentsProperty =
        DependencyProperty.Register(nameof(Segments), typeof(IReadOnlyList<TimelineSegment>),
            typeof(SubtitleTimelineControl), new PropertyMetadata(null, OnVisualChanged));

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(nameof(SelectedIndex), typeof(int),
            typeof(SubtitleTimelineControl), new PropertyMetadata(0, OnVisualChanged));

    public static readonly DependencyProperty PlayheadRatioProperty =
        DependencyProperty.Register(nameof(PlayheadRatio), typeof(double),
            typeof(SubtitleTimelineControl), new PropertyMetadata(0.0, OnVisualChanged));

    private readonly SolidColorBrush _segmentBrush = new(Color.FromRgb(124, 77, 255));
    private readonly SolidColorBrush _selectedBrush = new(Color.FromRgb(179, 136, 255));
    private readonly SolidColorBrush _playheadBrush = new(Colors.White);

    public IReadOnlyList<TimelineSegment>? Segments
    {
        get => (IReadOnlyList<TimelineSegment>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public double PlayheadRatio
    {
        get => (double)GetValue(PlayheadRatioProperty);
        set => SetValue(PlayheadRatioProperty, value);
    }

    public event EventHandler<int>? SegmentClicked;

    public SubtitleTimelineControl()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
        SizeChanged += (_, _) => Redraw();
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SubtitleTimelineControl control)
            control.Redraw();
    }

    private void Redraw()
    {
        TimelineCanvas.Children.Clear();
        var segments = Segments;
        EmptyText.Visibility = segments is not { Count: > 0 } ? Visibility.Visible : Visibility.Collapsed;

        if (segments == null || segments.Count == 0 || TimelineCanvas.ActualWidth <= 0)
            return;

        var width = TimelineCanvas.ActualWidth;
        var height = TimelineCanvas.ActualHeight > 0 ? TimelineCanvas.ActualHeight : 48;
        if (height <= 0) height = 48;
        TimelineCanvas.Height = height;

        foreach (var segment in segments)
        {
            var rect = new Rectangle
            {
                Width = Math.Max(segment.WidthRatio * width, 2),
                Height = height - 8,
                RadiusX = 2,
                RadiusY = 2,
                Fill = segment.Index == SelectedIndex ? _selectedBrush : _segmentBrush,
                Opacity = 0.85,
                ToolTip = $"#{segment.Index}: {segment.Label}",
                Tag = segment.Index
            };

            Canvas.SetLeft(rect, segment.StartRatio * width);
            Canvas.SetTop(rect, 4);
            TimelineCanvas.Children.Add(rect);
        }

        var playheadX = PlayheadRatio * width;
        var line = new Line
        {
            X1 = playheadX,
            X2 = playheadX,
            Y1 = 0,
            Y2 = height,
            Stroke = _playheadBrush,
            StrokeThickness = 2,
            Opacity = 0.9
        };
        TimelineCanvas.Children.Add(line);
    }

    private void TimelineCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Segments == null || Segments.Count == 0 || TimelineCanvas.ActualWidth <= 0)
            return;

        var clickRatio = e.GetPosition(TimelineCanvas).X / TimelineCanvas.ActualWidth;
        var hit = Segments
            .Where(s => clickRatio >= s.StartRatio && clickRatio <= s.StartRatio + s.WidthRatio)
            .OrderBy(s => s.WidthRatio)
            .FirstOrDefault();

        if (hit != null)
            SegmentClicked?.Invoke(this, hit.Index);
    }
}