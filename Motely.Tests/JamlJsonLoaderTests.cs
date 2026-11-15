using Motely.Filters;

namespace Motely.Tests;

public class JamlJsonLoaderTests
{
    /// <summary>The one must clause is a joker clause naming no joker, which the engine reads as the whole category.</summary>
    private static void AssertSingleMustIsAnyJoker(JamlConfig config)
    {
        var clause = Assert.Single(config.Must);
        var joker = Assert.IsType<JokerClause>(clause);
        Assert.Empty(joker.Jokers);
    }

    [Fact]
    public void FromJson_LoadsThroughTheSameLoader()
    {
        var ok = JamlConfig.TryLoad("""
            {
              "name": "json happy",
              "deck": "Erratic",
              "stake": "Gold",
              "must": [{ "joker": "Blueprint" }]
            }
            """, out var config, out var error);
        Assert.True(ok, error);
        Assert.Equal(MotelyDeck.Erratic, config!.Deck);
        Assert.Equal([MotelyJoker.Blueprint], Assert.IsType<JokerClause>(Assert.Single(config.Must)).Jokers);
    }

    [Fact]
    public void TryLoadFromJson_UnknownRootKey_IsRejected()
    {
        var ok = JamlConfig.TryLoad("""{ "name": "x", "boses": [] }""",

            out _,
            out var error
        );

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("boses", error);
    }

    [Fact]
    public void TryLoadFromJaml_UnknownRootKey_IsRejected()
    {
        var ok = JamlConfig.TryLoad("""
            name: test
            deck: Red
            stake: White
            unknownKey: value
            must:
              - joker: Blueprint
            """,
            out _,
            out var error
        );

        Assert.False(ok);
        Assert.Contains("unknownKey", error);
        Assert.Contains("JAML line", error);
    }

    [Fact]
    public void TryLoadFromJaml_UnknownRootKey_IsRejectedWithLine()
    {
        var ok = JamlConfig.TryLoad("""
            name: typo
            deck: Red
            must:
              - joker: Blueprint
            shuold:
              - voucher: Telescope
            """, out _, out var error);
        Assert.False(ok);
        Assert.Contains("JAML line 5", error);
        Assert.Contains("'shuold'", error);

    }

    [Fact]
    public void TryLoadFromJaml_SyntaxError_NamesLine()
    {
        var ok = JamlConfig.TryLoad("""
            name: colon
            deck: Red
            description: keep one: Diet Cola
            must:
              - joker: Blueprint
            """, out _, out var error);
        Assert.False(ok);
        Assert.Contains("JAML line 3:", error);
    }

    [Fact]
    public void FromJson_NullJoker_IsCategoryAny()
    {
        var ok = JamlConfig.TryLoad("""{ "must": [{ "joker": null }] }""", out var config, out var error);
        Assert.True(ok, error);
        AssertSingleMustIsAnyJoker(config!);
    }

    [Fact]
    public void FromJaml_BareJoker_IsCategoryAny()
    {
        var config = JamlConfig.FromJaml("""
            must:
              - joker:
            """);
        AssertSingleMustIsAnyJoker(config);
    }

    [Fact]
    public void FromJaml_FoldedParagraph_LandsOnDescription()
    {
        var config = JamlConfig.FromJaml("""
            name: folded
            description: >
              hello
              world
            must:
              - joker: Any
            """);
        Assert.Equal("hello world\n", config.Description);
        AssertSingleMustIsAnyJoker(config);
    }

    [Fact]
    public void FromJaml_AnyKeyword_IsCategoryAny()
    {
        var config = JamlConfig.FromJaml("""
            must:
              - joker: Any
            """);
        AssertSingleMustIsAnyJoker(config);
    }

    [Fact]
    public void FromJaml_HappyPath_MatchesJson()
    {
        var fromJaml = JamlConfig.FromJaml("""
            name: yaml happy
            deck: red
            stake: white
            must:
              - joker: Blueprint
            """);
        var fromJson = JamlConfig.FromJaml("""{ "name": "yaml happy", "deck": "red", "stake": "white" }""");

        Assert.Equal(fromJson.Deck, fromJaml.Deck);
        Assert.Equal(fromJson.Stake, fromJaml.Stake);
        Assert.IsType<JokerClause>(Assert.Single(fromJaml.Must));
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsWithMessage()
    {
        var ex = Assert.ThrowsAny<InvalidOperationException>(() =>
            JamlConfig.FromJaml("{ not json")
        );
        Assert.NotEmpty(ex.Message);
    }

    [Fact]
    public void FromJaml_InvalidYaml_ThrowsWithMessage()
    {
        var ex = Assert.ThrowsAny<InvalidOperationException>(() =>
            JamlConfig.FromJaml("must: [")
        );
        Assert.NotEmpty(ex.Message);
    }
}
