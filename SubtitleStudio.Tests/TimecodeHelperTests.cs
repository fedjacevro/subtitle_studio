using SubtitleStudio.Core.Helpers;

namespace SubtitleStudio.Tests;

public class TimecodeHelperTests
{
    [Fact]
    public void FormatSrt_UsesCommaSeparator()
    {
        var ts = new TimeSpan(0, 1, 2, 3, 456);
        Assert.Equal("01:02:03,456", TimecodeHelper.FormatSrt(ts));
    }

    [Fact]
    public void TryParseSrt_ParsesCommaFormat()
    {
        Assert.True(TimecodeHelper.TryParseSrt("01:02:03,456", out var ts));
        Assert.Equal(new TimeSpan(0, 1, 2, 3, 456), ts);
    }

    [Fact]
    public void ValidateSubtitleItems_DetectsOverlap()
    {
        var items = new List<(int Index, TimeSpan Start, TimeSpan End)>
        {
            (1, TimeSpan.Zero, TimeSpan.FromSeconds(2)),
            (2, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3))
        };

        var error = TimecodeHelper.ValidateSubtitleItems(items);
        Assert.Contains("overlaps", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateSubtitleItems_AcceptsValidSequence()
    {
        var items = new List<(int Index, TimeSpan Start, TimeSpan End)>
        {
            (1, TimeSpan.Zero, TimeSpan.FromSeconds(2)),
            (2, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4))
        };

        Assert.Null(TimecodeHelper.ValidateSubtitleItems(items));
    }
}