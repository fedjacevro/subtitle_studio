using SubtitleStudio.Core.Models;

namespace SubtitleStudio.Tests;

public class SubtitleItemTests
{
    [Fact]
    public void GetDisplayTextForLanguage_PrefersProofreadOverTranslation()
    {
        var item = new SubtitleItem { Text = "Hello" };
        item.SetTranslation("de", "Hallo");
        item.SetProofread("de", "Hallo!");

        Assert.Equal("Hallo!", item.GetDisplayTextForLanguage("de"));
        Assert.Equal("Hallo", item.GetDisplayTextForLanguage("de", useProofread: false));
    }

    [Fact]
    public void GetDisplayTextForLanguage_FallsBackToSourceText()
    {
        var item = new SubtitleItem { Text = "Hello" };
        Assert.Equal("Hello", item.GetDisplayTextForLanguage("fr"));
        Assert.Equal("Hello", item.GetDisplayTextForLanguage(null));
    }

    [Fact]
    public void Clone_CopiesTranslations()
    {
        var item = new SubtitleItem { Text = "Hi", Index = 1 };
        item.SetTranslation("bs", "Zdravo");
        item.SetProofread("bs", "Zdravo!");

        var clone = item.Clone();
        Assert.Equal("Zdravo!", clone.GetDisplayTextForLanguage("bs"));
        Assert.NotSame(item, clone);
    }

    [Fact]
    public void Duration_UpdatesWhenEndTimeChanges()
    {
        var item = new SubtitleItem
        {
            StartTime = TimeSpan.Zero,
            EndTime = TimeSpan.FromSeconds(2)
        };

        Assert.Equal(TimeSpan.FromSeconds(2), item.Duration);
        item.EndTime = TimeSpan.FromSeconds(5);
        Assert.Equal(TimeSpan.FromSeconds(5), item.Duration);
    }
}