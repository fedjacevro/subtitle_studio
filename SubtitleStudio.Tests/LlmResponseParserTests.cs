using SubtitleStudio.Core.Helpers;
using SubtitleStudio.Core.Models;

namespace SubtitleStudio.Tests;

public class LlmResponseParserTests
{
    [Fact]
    public void ParseNumberedLines_AppliesTranslationsByChunkOffset()
    {
        var items = new List<SubtitleItem>
        {
            new() { Index = 1, Text = "One" },
            new() { Index = 2, Text = "Two" },
            new() { Index = 3, Text = "Three" },
            new() { Index = 4, Text = "Four" }
        };

        const string response = """
            [1] Eins
            [2] Zwei
            """;

        var parsed = LlmResponseParser.ParseNumberedLines(response, chunkIdx: 1, chunkSize: 2, items,
            (item, text) => item.SetTranslation("de", text));

        Assert.Equal(2, parsed);
        Assert.Equal("Eins", items[2].GetDisplayTextForLanguage("de", useProofread: false));
        Assert.Equal("Zwei", items[3].GetDisplayTextForLanguage("de", useProofread: false));
    }

    [Fact]
    public void ParseNumberedLines_IgnoresMalformedLines()
    {
        var items = new List<SubtitleItem> { new() { Index = 1, Text = "Hi" } };
        const string response = """
            no marker here
            [x] bad index
            [1] Valid
            """;

        var parsed = LlmResponseParser.ParseNumberedLines(response, 0, 10, items,
            (item, text) => item.SetTranslation("en", text));

        Assert.Equal(1, parsed);
        Assert.Equal("Valid", items[0].GetDisplayTextForLanguage("en", useProofread: false));
    }
}