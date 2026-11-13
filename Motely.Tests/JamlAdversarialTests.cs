using Motely.Filters;

namespace Motely.Tests;

/// <summary>
/// JAML arrives from the browser build and from other people's files. Out-of-range or malformed
/// input must be a load error, never a process kill, a hang, or a wrapped score.
/// </summary>
public sealed class JamlAdversarialTests
{
    private static void Refused(string yaml)
    {
        Assert.False(JamlConfig.TryLoad(yaml, out _, out var error), "the filter loaded");
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData("must:\n  - luckyMoney: [abc]\n")]
    [InlineData("a: &x [1]\nmust:\n  - joker: Blueprint\n    antes: *x\n")]
    [InlineData("must: 5\n")]
    [InlineData("must:\n  - {}\n")]
    [InlineData("must:\n  - 7\n")]
    [InlineData("must:\n  - joker: Blueprint\n---\nmust:\n  - joker: Blueprint\n")]
    [InlineData("must:\n  - !!python/object joker\n")]
    [InlineData("must:\n  - joker: Blueprint\n    antes: [1e400]\n")]
    [InlineData("must:\n  - joker: Blueprint\n    antes: [0x7FFFFFFFFF]\n")]
    [InlineData("deck: NotADeck\n")]
    [InlineData("name: a\0b\n")]
    public void IsALoadError_NotACrash(string yaml) => Refused(yaml);

    private static void RefusedWith(string yaml, string fragment)
    {
        Assert.False(JamlConfig.TryLoad(yaml, out _, out var error), "the filter loaded");
        Assert.Contains(fragment, error);
    }

    [Theory]
    [InlineData("must:\n  - tag: NegativeTag\n    rolls: [100000]\n")]
    [InlineData("must:\n  - voucher: Hieroglyph\n    antes: [1]\n    rolls: [2147483647]\n")]
    [InlineData("must:\n  - joker: Blueprint\n    antes: [1]\n    sources:\n      shopItems: [2147483647]\n")]
    [InlineData("must:\n  - joker: Blueprint\n    antes: [1]\n    sources:\n      boosterPacks: [1000]\n")]
    [InlineData("must:\n  - joker: Blueprint\n    antes: [-1]\n")]
    [InlineData("must:\n  - tarotCard: TheFool\n    antes: [1]\n    sources:\n      emperor: [5000]\n")]
    [InlineData("must:\n  - luckyMoney: [5000]\n")]
    [InlineData("must:\n  - luckyMoney: 5000\n")]
    public void ANumberOutside0To999_IsRefused(string yaml) => RefusedWith(yaml, "0 to 999");

    [Theory]
    [InlineData("must:\n  - joker: Blueprint\n    antes: [40]\n")]
    [InlineData("must:\n  - and:\n      - joker: Blueprint\n        antes: [40]\n")]
    [InlineData("should:\n  - or:\n      - joker: Blueprint\n        antes: [1, 40]\n")]
    public void AnAnteAbove39_IsRefused(string yaml) => RefusedWith(yaml, "0 to 39");

    [Theory]
    [InlineData("must:\n  - startingDraw: TenOfSpades\n    min: -1\n    antes: [1]\n")]
    [InlineData("must:\n  - joker: Blueprint\n    antes: [1]\n    max: -3\n")]
    [InlineData("must:\n  - or:\n    min: 0\n    clauses:\n      - joker: Blueprint\n        antes: [1]\n")]
    [InlineData("must:\n  - legendaryJoker: Perkeo\n    antes: [1]\n    soulEditionRolls: 5000\n")]
    public void AnOutOfRangeBound_IsRefused(string yaml) => RefusedWith(yaml, "out of range");

    [Fact]
    public void AnAliasBetweenRealKeys_IsRefused() =>
        RefusedWith(
            "must:\n  - !joker\n    joker: Blueprint\n    antes: &x [1]\n  - !joker\n    joker: Brainstorm\n    antes: *x\n",
            "aliases");

    [Fact]
    public void ALoneSurrogate_IsRefused() => Refused("name: a" + (char)0xD800 + "b\n");

    [Fact]
    public void AnAliasChain200Deep_IsRefused()
    {
        var sb = new System.Text.StringBuilder("a0: &a0 [1]\n");
        for (int i = 1; i < 200; i++)
            sb.Append($"a{i}: &a{i} *a{i - 1}\n");
        Refused(sb.ToString());
    }

    [Fact]
    public void AnAliasFanOut_IsRefused()
    {
        var sb = new System.Text.StringBuilder("l0: &l0 [1]\n");
        for (int i = 1; i < 40; i++)
            sb.Append($"l{i}: &l{i} [*l{i - 1}, *l{i - 1}]\n");
        Refused(sb.ToString());
    }

    [Fact]
    public void NestingPast64_IsRefused()
    {
        Refused(new string('[', 70) + new string(']', 70));
    }

    [Fact]
    public void Roll999_Loads()
    {
        Assert.True(JamlConfig.TryLoad("must:\n  - tag: NegativeTag\n    rolls: [999]\n", out _, out var error), error);
    }

    [Fact]
    public void Ante39_Loads()
    {
        Assert.True(JamlConfig.TryLoad("must:\n  - joker: Blueprint\n    antes: [39]\n", out _, out var error), error);
    }

    [Fact]
    public void ToJson_StillExpandsAliases()
    {
        var json = JamlConfig.ToJson("a: &x [1]\nb: *x\n");
        Assert.Contains("\"b\"", json);
    }

    [Fact]
    public void ALongWithList_SearchesToCompletion()
    {
        var jokers = string.Join(", ", Enumerable.Repeat("Perkeo", 20000));
        var config = ProofSearch.LoadOrThrow(
            $"seeds: [ALEEB, UNITTEST, 1YU11111]\nshould:\n  - legendaryJoker: Perkeo\n    antes: [1, 2]\n    with:\n      jokers: [{jokers}]\n");
        var search = MotelySearchBuilder.CreateSettings(config).WithSeedList([.. config.Seeds]).WithThreadCount(1).CreateSearch();
        search.Start();
        search.AwaitCompletion();
        search.Dispose();
    }

    [Fact]
    public void TwoMaxScores_DoNotWrapNegative()
    {
        var config = ProofSearch.LoadOrThrow(
            "should:\n  - voucher: Overstock\n    antes: [1, 2, 3, 4, 5, 6, 7, 8]\n    score: 2147483647\n"
            + "  - voucher: Clearance Sale\n    antes: [1, 2, 3, 4, 5, 6, 7, 8]\n    score: 2147483647\n");
        int best = int.MinValue;
        var search = MotelySearchBuilder.CreateSettings(config)
            .WithSeedList(["ALEEB", "UNITTEST", "1YU11111", "12345678"])
            .WithThreadCount(1)
            .WithScoredResultCallback(r => best = Math.Max(best, r.Score))
            .CreateSearch();
        search.Start();
        search.AwaitCompletion();
        search.Dispose();
        Assert.True(best >= 0, $"best score {best}");
    }
}
