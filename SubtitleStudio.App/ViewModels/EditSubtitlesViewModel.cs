using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubtitleStudio.Core.Models;
using SubtitleStudio.Core.Helpers;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace SubtitleStudio.App.ViewModels;

public partial class EditSubtitlesViewModel : ObservableObject
{
    private readonly ILogger<EditSubtitlesViewModel> _logger;

    [ObservableProperty]
    private SubtitleTrack? _subtitleTrack;

    [ObservableProperty]
    private ObservableCollection<SubtitleItem> _subtitleItems = [];

    [ObservableProperty]
    private SubtitleItem? _selectedItem;

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private string? _replaceText;

    [ObservableProperty]
    private string? _videoFilePath;

    [ObservableProperty]
    private string? _videoDuration;

    [ObservableProperty]
    private bool _isPreviewExpanded = true;

    [ObservableProperty]
    private string _previewToggleLabel = "Hide Preview";

    [ObservableProperty]
    private int _selectedTimelineIndex;

    [ObservableProperty]
    private double _playheadRatio;

    [ObservableProperty]
    private List<TimelineSegment> _timelineSegments = [];

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _validationMessage;

    public event Action<TimeSpan>? SeekRequested;

    public EditSubtitlesViewModel(ILogger<EditSubtitlesViewModel> logger)
    {
        _logger = logger;
    }

    partial void OnSubtitleTrackChanged(SubtitleTrack? value)
    {
        if (value != null)
        {
            SubtitleItems = new ObservableCollection<SubtitleItem>(value.Items);
            VideoFilePath ??= value.VideoFilePath;
            StatusMessage = $"{SubtitleItems.Count} subtitles loaded for editing.";
            ValidateTimecodes();
            RefreshTimeline();
        }
    }

    partial void OnSelectedItemChanged(SubtitleItem? value)
    {
        if (value == null) return;
        SelectedTimelineIndex = value.Index;
        SeekRequested?.Invoke(value.StartTime);
        UpdatePlayhead(value.StartTime);
    }

    partial void OnIsPreviewExpandedChanged(bool value) =>
        PreviewToggleLabel = value ? "Hide Preview" : "Show Preview";

    partial void OnVideoDurationChanged(string? value) => RefreshTimeline();

    [RelayCommand]
    private void TogglePreview() => IsPreviewExpanded = !IsPreviewExpanded;

    public void OnTimelineSegmentClicked(int index)
    {
        var item = SubtitleItems.FirstOrDefault(i => i.Index == index)
            ?? SubtitleTrack?.Items.FirstOrDefault(i => i.Index == index);
        if (item == null) return;
        SelectedItem = item;
        SeekRequested?.Invoke(item.StartTime);
        UpdatePlayhead(item.StartTime);
    }

    private void RefreshTimeline()
    {
        if (SubtitleTrack == null)
        {
            TimelineSegments = [];
            return;
        }

        TimeSpan? videoDuration = null;
        if (!string.IsNullOrEmpty(VideoDuration) &&
            TimeSpan.TryParse(VideoDuration.Replace(',', '.'), CultureInfo.InvariantCulture, out var vd))
            videoDuration = vd;

        var total = TimelineHelper.GetTotalDuration(SubtitleTrack.Items, videoDuration);
        TimelineSegments = TimelineHelper.BuildSegments(SubtitleTrack.Items, total);
    }

    private void UpdatePlayhead(TimeSpan position)
    {
        if (SubtitleTrack == null) return;
        TimeSpan? videoDuration = null;
        if (!string.IsNullOrEmpty(VideoDuration) &&
            TimeSpan.TryParse(VideoDuration.Replace(',', '.'), CultureInfo.InvariantCulture, out var vd))
            videoDuration = vd;
        var total = TimelineHelper.GetTotalDuration(SubtitleTrack.Items, videoDuration);
        PlayheadRatio = TimelineHelper.GetPlayheadRatio(position, total);
    }

    partial void OnSearchTextChanged(string? value) => RefreshSubtitleItemsView();

    public void OnSubtitleEdited()
    {
        ValidateTimecodes();
        RefreshTimeline();
    }

    private void RefreshSubtitleItemsView()
    {
        if (SubtitleTrack == null) return;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SubtitleItems = new ObservableCollection<SubtitleItem>(SubtitleTrack.Items);
            StatusMessage = $"{SubtitleTrack.Items.Count} subtitles loaded for editing.";
        }
        else
        {
            var filtered = SubtitleTrack.Items
                .Where(i => i.Text.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            SubtitleItems = new ObservableCollection<SubtitleItem>(filtered);
            StatusMessage = $"{filtered.Count} results found.";
        }
    }

    private int FindTrackIndex(SubtitleItem item)
    {
        if (SubtitleTrack == null) return -1;

        var idx = SubtitleTrack.Items.IndexOf(item);
        if (idx >= 0) return idx;

        return SubtitleTrack.Items.FindIndex(i => i.Index == item.Index);
    }

    private void ReindexTrack()
    {
        if (SubtitleTrack == null) return;

        for (var i = 0; i < SubtitleTrack.Items.Count; i++)
            SubtitleTrack.Items[i].Index = i + 1;
    }

    [RelayCommand]
    private void ReplaceAll()
    {
        if (SubtitleTrack == null || string.IsNullOrWhiteSpace(SearchText))
        {
            StatusMessage = "Enter search text to replace.";
            return;
        }

        var count = 0;
        foreach (var item in SubtitleTrack.Items)
        {
            if (item.Text.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            {
                item.Text = item.Text.Replace(SearchText, ReplaceText ?? string.Empty,
                    StringComparison.OrdinalIgnoreCase);
                count++;
            }
        }

        RefreshSubtitleItemsView();
        StatusMessage = $"Replaced text in {count} subtitle(s).";
        _logger.LogInformation("Search/replace applied to {Count} subtitles", count);
        ValidateTimecodes();
    }

    [RelayCommand]
    private void ValidateTimecodes()
    {
        if (SubtitleTrack == null)
        {
            ValidationMessage = null;
            return;
        }

        var items = SubtitleTrack.Items
            .Select(i => (i.Index, i.StartTime, i.EndTime))
            .ToList();
        ValidationMessage = TimecodeHelper.ValidateSubtitleItems(items);
        if (ValidationMessage != null)
            StatusMessage = ValidationMessage;
    }

    [RelayCommand]
    private void SeekToSelected()
    {
        if (SelectedItem != null)
            SeekRequested?.Invoke(SelectedItem.StartTime);
    }

    [RelayCommand]
    private void MergeSubtitles()
    {
        if (SelectedItem == null || SubtitleTrack == null) return;

        var trackIndex = FindTrackIndex(SelectedItem);
        if (trackIndex < 0 || trackIndex >= SubtitleTrack.Items.Count - 1) return;

        var current = SubtitleTrack.Items[trackIndex];
        var next = SubtitleTrack.Items[trackIndex + 1];

        current.Text = $"{current.Text} {next.Text}";
        current.EndTime = next.EndTime;

        SubtitleTrack.Items.RemoveAt(trackIndex + 1);
        ReindexTrack();
        RefreshSubtitleItemsView();
        SelectedItem = current;
        StatusMessage = "Subtitles merged.";
        ValidateTimecodes();
        RefreshTimeline();
    }

    [RelayCommand]
    private void SplitSubtitle()
    {
        if (SelectedItem == null || SubtitleTrack == null) return;

        var trackIndex = FindTrackIndex(SelectedItem);
        if (trackIndex < 0) return;

        var item = SubtitleTrack.Items[trackIndex];
        var midPoint = item.Text.Length / 2;
        var spaceIdx = item.Text.IndexOf(' ', midPoint);
        if (spaceIdx < 0) spaceIdx = midPoint;

        var firstPart = item.Text[..spaceIdx].Trim();
        var secondPart = item.Text[spaceIdx..].Trim();
        if (string.IsNullOrEmpty(firstPart) || string.IsNullOrEmpty(secondPart)) return;

        var originalEnd = item.EndTime;
        var midTime = item.StartTime + (originalEnd - item.StartTime) / 2;

        item.Text = firstPart;
        item.EndTime = midTime;

        var newItem = new SubtitleItem
        {
            Index = item.Index + 1,
            StartTime = midTime,
            EndTime = originalEnd,
            Text = secondPart
        };

        SubtitleTrack.Items.Insert(trackIndex + 1, newItem);
        ReindexTrack();
        RefreshSubtitleItemsView();
        SelectedItem = item;
        StatusMessage = "Subtitle split.";
        ValidateTimecodes();
        RefreshTimeline();
    }

    [RelayCommand]
    private void DeleteSubtitle()
    {
        if (SelectedItem == null || SubtitleTrack == null) return;

        var trackIndex = FindTrackIndex(SelectedItem);
        if (trackIndex < 0) return;

        SubtitleTrack.Items.RemoveAt(trackIndex);
        ReindexTrack();
        RefreshSubtitleItemsView();
        StatusMessage = "Subtitle deleted.";
        ValidateTimecodes();
        RefreshTimeline();
    }

    [RelayCommand]
    private void AddSubtitle()
    {
        if (SubtitleTrack == null) return;

        var newItem = new SubtitleItem
        {
            Index = SubtitleTrack.Items.Count + 1,
            StartTime = TimeSpan.Zero,
            EndTime = TimeSpan.FromSeconds(3),
            Text = "New subtitle"
        };

        SubtitleTrack.Items.Add(newItem);
        ReindexTrack();
        RefreshSubtitleItemsView();
        SelectedItem = newItem;
        StatusMessage = "New subtitle added.";
        ValidateTimecodes();
        RefreshTimeline();
    }
}