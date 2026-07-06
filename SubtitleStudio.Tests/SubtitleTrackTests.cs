using SubtitleStudio.Core.Models;

namespace SubtitleStudio.Tests;

public class SubtitleTrackTests
{
    [Fact]
    public void RegisterTranslatedLanguage_IsCaseInsensitive()
    {
        var track = new SubtitleTrack();
        track.RegisterTranslatedLanguage("de");
        track.RegisterTranslatedLanguage("DE");

        Assert.Single(track.TranslatedLanguageCodes);
        Assert.Equal("de", track.TranslatedLanguageCodes[0]);
    }

    [Fact]
    public void Clone_CopiesItemsAndLanguages()
    {
        var track = new SubtitleTrack
        {
            SourceLanguage = "en",
            TargetLanguage = "de",
            VideoFilePath = "video.mp4"
        };
        var item = new SubtitleItem { Index = 1, Text = "Hi" };
        item.SetTranslation("de", "Hallo");
        track.Items.Add(item);
        track.RegisterTranslatedLanguage("de");

        var clone = track.Clone();
        Assert.Equal("en", clone.SourceLanguage);
        Assert.Equal("de", clone.TargetLanguage);
        Assert.Equal("video.mp4", clone.VideoFilePath);
        Assert.Single(clone.Items);
        Assert.Equal("Hallo", clone.Items[0].GetDisplayTextForLanguage("de", useProofread: false));
        Assert.NotSame(track.Items[0], clone.Items[0]);
    }
}