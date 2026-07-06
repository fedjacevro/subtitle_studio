using SubtitleStudio.Core.Helpers;
using SubtitleStudio.Core.Models;

namespace SubtitleStudio.Tests;

public class SubtitleFormatHelperTests
{
    [Fact]
    public void GenerateSrtContent_IncludesIndexAndTimecodes()
    {
        var track = new SubtitleTrack
        {
            Items =
            [
                new SubtitleItem
                {
                    Index = 1,
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.FromSeconds(2),
                    Text = "Hello"
                }
            ]
        };

        var content = SubtitleFormatHelper.GenerateSrtContent(track);
        Assert.Contains("1", content);
        Assert.Contains("00:00:00,000 --> 00:00:02,000", content);
        Assert.Contains("Hello", content);
    }

    [Fact]
    public void GenerateVttContent_IncludesHeaderAndDotSeparator()
    {
        var track = new SubtitleTrack
        {
            Items =
            [
                new SubtitleItem
                {
                    Index = 1,
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.FromSeconds(1.5),
                    Text = "Test"
                }
            ]
        };

        var content = SubtitleFormatHelper.GenerateVttContent(track);
        Assert.StartsWith("WEBVTT", content);
        Assert.Contains("00:00:00.000 --> 00:00:01.500", content);
        Assert.Contains("Test", content);
    }

    [Fact]
    public void GetTextForLanguage_PrefersProofreadTranslation()
    {
        var item = new SubtitleItem { Text = "Hi" };
        item.SetTranslation("de", "Hallo");
        item.SetProofread("de", "Hallo!");

        var text = SubtitleFormatHelper.GetTextForLanguage(item, "de", useProofread: true);
        Assert.Equal("Hallo!", text);
    }
}