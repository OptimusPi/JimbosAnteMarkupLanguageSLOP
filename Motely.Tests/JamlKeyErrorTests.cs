using Motely.Filters;

namespace Motely.Tests;

/// <summary>
/// A wrong key fails to load (never crashes) with a <c>JAML line N:</c> error that names the
/// offending key.
/// </summary>
public sealed class JamlKeyErrorTests
{
    private static string LoadError(string yaml)
    {
        Assert.False(JamlConfig.TryLoad(yaml, out _, out var error), "the filter loaded");
        return error!;
    }

    [Theory]
    [InlineData("name: x\nmust:\n  - joker: Blueprint\n    luckMoney: [0]\n", "luckMoney")]
    public void UnknownProperty_IsNamed(string yaml, string key)
    {
        var error = LoadError(yaml);
        Assert.StartsWith("JAML line ", error);
        Assert.Contains(key, error);
    }

    [Theory]
    [InlineData("must:\n  - score: 5\n    min: 1\n    luckMoney: [0]\n")]
    [InlineData("must:\n  - zzzzzz: 3\n")]
    [InlineData("{\"must\": [{\"type\": \"Joker\", \"value\": \"OopsAll6s\"}]}")]
    [InlineData("must:\n  - {}\n")]
    public void Clause_NeedsAKey(string yaml)
    {
        var error = LoadError(yaml);
        Assert.Contains("a clause needs a key", error);
    }

    [Fact]
    public void ClauseWrittenAsAList_IsAnError_NotACrash()
    {
        Assert.Contains("a clause needs a key", LoadError("must:\n  - [joker, Blueprint]\n"));
    }

    [Theory]
    [InlineData("name: x\nshould:\n  - Perkeo in ante 1\n", 3)]
    [InlineData("name: x\nmust:\n  - joker: Blueprint\n  - [joker, Brainstorm]\n", 4)]
    [InlineData("name: x\nmust:\n  - joker: Blueprint\n  - zzzzzz: 3\n", 4)]
    [InlineData("name: x\nmust:\n  - or:\n      - joker: Blueprint\n      - Brainstorm\n", 5)]
    public void AClauseWithNoKey_IsRefusedOnItsOwnLine(string yaml, int line) =>
        Assert.StartsWith($"JAML line {line}: a clause needs a key", LoadError(yaml));

    [Theory]
    [InlineData("should:\n  - and:\n    - smallBlindTag: NegativeTag\n      or:\n      - joker: Photograph\n      antes: [3]\n", "or")]
    [InlineData("must:\n  - rareJoker: Any\n    boosterPacks: [0]\n", "boosterPacks")]
    [InlineData("must:\n  - joker: Blueprint\n    antse: [1]\n", "antse")]
    [InlineData("must:\n  - joker: Blueprint\n    sources:\n      shopItem: [1]\n", "shopItem")]
    [InlineData("must:\n  - joker: Blueprint\n    shopItems: [1]\n", "shopItems")]
    [InlineData("nmae: x\n", "nmae")]
    public void UnknownKey_IsAnError_NamingIt(string yaml, string key)
    {
        var error = LoadError(yaml);
        Assert.StartsWith("JAML line ", error);
        Assert.Contains($"'{key}'", error);
    }

    [Theory]
    [InlineData("name: x\nname: y\nmust:\n  - joker: Blueprint\n", 2, "name")]
    [InlineData("must:\n  - joker: Blueprint\nmust:\n  - joker: Brainstorm\n", 3, "must")]
    [InlineData("must:\n  - joker: Blueprint\n    antes: [1]\n    antes: [2]\n", 4, "antes")]
    [InlineData("must:\n  - !joker\n    joker: Blueprint\n    antes: [1]\n    antes: [2]\n", 5, "antes")]
    [InlineData("must:\n  - or:\n      - joker: Blueprint\n        score: 1\n        score: 2\n", 5, "score")]
    [InlineData("must:\n  - joker: Blueprint\n    sources:\n      shopItems: [0]\n      shopItems: [1]\n", 5, "shopItems")]
    [InlineData("{\"must\": [{\"joker\": \"Blueprint\", \"min\": 1, \"min\": 2}]}", 1, "min")]
    public void AKeyWrittenTwice_IsRefusedOnItsLine(string yaml, int line, string key) =>
        Assert.Equal($"JAML line {line}: Encountered duplicate key {key}", LoadError(yaml));

    [Fact]
    public void ASecondClauseKeyTheTypeTakes_IsNotTwoClauses()
    {
        // soulOrder takes boosterPacks as its own key.
        var config = ProofSearch.LoadOrThrow("must:\n  - soulOrder: [{ legendary: Perkeo }]\n    boosterPacks: [0, 1]\n");
        Assert.IsType<LegendarySoulOrderClause>(Assert.Single(config.Must));
    }
}
