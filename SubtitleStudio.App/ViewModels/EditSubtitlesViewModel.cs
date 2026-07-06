using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubtitleStudio.Core.Models;
using Microsoft.Extensions.Logging;

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
    private string? _statusMessage;

    public EditSubtitlesViewModel(ILogger<EditSubtitlesViewModel> logger)
    {
        _logger = logger;
    }

    partial void OnSubtitleTrackChanged(SubtitleTrack? value)
    {
        if (value != null)
        {
            SubtitleItems = new ObservableCollection<SubtitleItem>(value.Items);
            StatusMessage = $"{SubtitleItems.Count} subtitles loaded for editing.";
        }
    }

    partial void OnSearchTextChanged(string? value)
    {
        if (SubtitleTrack == null || string.IsNullOrWhiteSpace(value))
        {
            if (SubtitleTrack != null)
                SubtitleItems = new ObservableCollection<SubtitleItem>(SubtitleTrack.Items);
            return;
        }

        var filtered = SubtitleTrack.Items
            .Where(i => i.Text.Contains(value, StringComparison.OrdinalIgnoreCase))
            .ToList();
        SubtitleItems = new ObservableCollection<SubtitleItem>(filtered);
        StatusMessage = $"{filtered.Count} results found.";
    }

    [RelayCommand]
    private void MergeSubtitles()
    {
        if (SelectedItem == null) return;

        var index = SubtitleItems.IndexOf(SelectedItem);
        if (index < 0 || index >= SubtitleItems.Count - 1) return;

        var current = SubtitleItems[index];
        var next = SubtitleItems[index + 1];

        current.Text = $"{current.Text} {next.Text}";
        current.EndTime = next.EndTime;

        SubtitleItems.RemoveAt(index + 1);
        if (SubtitleTrack != null)
        {
            SubtitleTrack.Items = [..SubtitleItems];
            // Re-index
            for (int i = 0; i < SubtitleTrack.Items.Count; i++)
                SubtitleTrack.Items[i].Index = i + 1;
        }

        StatusMessage = "Subtitles merged.";
    }

    [RelayCommand]
    private void SplitSubtitle()
    {
        if (SelectedItem == null) return;

        var index = SubtitleItems.IndexOf(SelectedItem);
        if (index < 0) return;

        var item = SelectedItem;
        var midPoint = item.Text.Length / 2;
        var spaceIdx = item.Text.IndexOf(' ', midPoint);
        if (spaceIdx < 0) spaceIdx = midPoint;

        var firstPart = item.Text[..spaceIdx].Trim();
        var secondPart = item.Text[spaceIdx..].Trim();
        if (string.IsNullOrEmpty(firstPart) || string.IsNullOrEmpty(secondPart)) return;

        var midTime = item.StartTime + (item.EndTime - item.StartTime) / 2;

        item.Text = firstPart;
        item.EndTime = midTime;

        var newItem = new SubtitleItem
        {
            Index = item.Index + 1,
            StartTime = midTime,
            EndTime = item.EndTime,
            Text = secondPart
        };

        SubtitleItems.Insert(index + 1, newItem);

        if (SubtitleTrack != null)
        {
            SubtitleTrack.Items = [..SubtitleItems];
            // Re-index
            for (int i = 0; i < SubtitleTrack.Items.Count; i++)
                SubtitleTrack.Items[i].Index = i + 1;
        }

        StatusMessage = "Subtitle split.";
    }

    [RelayCommand]
    private void DeleteSubtitle()
    {
        if (SelectedItem == null) return;

        SubtitleItems.Remove(SelectedItem);
        if (SubtitleTrack != null)
        {
            SubtitleTrack.Items = [..SubtitleItems];
            for (int i = 0; i < SubtitleTrack.Items.Count; i++)
                SubtitleTrack.Items[i].Index = i + 1;
        }

        StatusMessage = "Subtitle deleted.";
    }

    [RelayCommand]
    private void AddSubtitle()
    {
        var newItem = new SubtitleItem
        {
            Index = (SubtitleTrack?.Items.Count ?? 0) + 1,
            StartTime = TimeSpan.Zero,
            EndTime = TimeSpan.FromSeconds(3),
            Text = "New subtitle"
        };

        SubtitleItems.Add(newItem);
        if (SubtitleTrack != null)
        {
            SubtitleTrack.Items = [..SubtitleItems];
        }

        StatusMessage = "New subtitle added.";
    }
}
