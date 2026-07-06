using SubtitleStudio.Core.Helpers;

namespace SubtitleStudio.Tests;

public class FfmpegPathHelperTests
{
    [Fact]
    public void EscapeSubtitlePath_EscapesWindowsDriveAndQuotes()
    {
        var escaped = FfmpegPathHelper.EscapeSubtitlePath(@"C:\Users\Neko\subs\my file.srt");
        Assert.Equal(@"C\:/Users/Neko/subs/my file.srt", escaped);
    }

    [Fact]
    public void EscapeSubtitlePath_EscapesSingleQuotes()
    {
        var escaped = FfmpegPathHelper.EscapeSubtitlePath(@"D:\it's\test.srt");
        Assert.Contains("'\\''", escaped);
        Assert.DoesNotContain(@"D:\", escaped);
    }
}