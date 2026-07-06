using SubtitleStudio.Core.Helpers;
using SubtitleStudio.Core.Models;

namespace SubtitleStudio.Tests;

public class TimelineHelperTests
{
    [Fact]
    public void BuildSegments_ProducesProportionalWidths()
    {
        var items = new List<SubtitleItem>
        {
            new() { Index = 1, StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromSeconds(5), Text = "A" },
            new() { Index = 2, StartTime = TimeSpan.FromSeconds(5), EndTime = TimeSpan.FromSeconds(10), Text = "B" }
        };

        var segments = TimelineHelper.BuildSegments(items, TimeSpan.FromSeconds(10));
        Assert.Equal(2, segments.Count);
        Assert.Equal(0.5, segments[0].WidthRatio, 3);
        Assert.Equal(0.5, segments[1].WidthRatio, 3);
    }
}