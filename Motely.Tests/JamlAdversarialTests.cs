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

    [Theory]
    [InlineData("and", "[{ joker: Blueprint }, { joker: Brainstorm }]", "")]
    [InlineData("or", "[{ joker: Blueprint }, { joker: Brainstorm }]", "")]
    [InlineData("and", "~", "    clauses: [{ joker: Blueprint }, { joker: Brainstorm }]\n")]
    [InlineData("or", "~", "    clauses: [{ joker: Blueprint }, { joker: Brainstorm }]\n")]
    public void AndOr_LoadAsAListOrUnderClauses(string key, string value, string rest)
    {
        Assert.True(JamlConfig.TryLoad($"must:\n  - {key}: {value}\n{rest}", out var config, out var error), error);
        Assert.Equal(2, Assert.IsAssignableFrom<LogicClause>(Assert.Single(config!.Must)).Clauses.Length);
    }

    [Theory]
    [InlineData("and")]
    [InlineData("or")]
    public void AndOr_WrittenAsAMapping_IsRefused(string key) =>
        RefusedWith(
            $"must:\n  - {key}: {{ clauses: [{{ joker: Blueprint }}, {{ joker: Brainstorm }}] }}\n",
            "Expected 'SequenceStart', got 'MappingStart'");

    [Theory]
    [InlineData("and", "max: 2", "an and takes no min or max")]
    [InlineData("and", "min: 2", "an and takes no min or max")]
    [InlineData("or", "max: 2", "an or takes no max")]
    [InlineData("or", "min: 0", "an or needs min: 1 or more")]
    public void AndOr_BoundsOutsideTheDeclaredOnes_AreRefused(string key, string bound, string why) =>
        RefusedWith($"must:\n  - {key}: [{{ joker: Blueprint }}, {{ joker: Brainstorm }}]\n    {bound}\n", why);

    [Theory]
    [InlineData("and", "min: 1\n    max: ~")]
    [InlineData("or", "min: 2\n    max: ~")]
    public void AndOr_DeclaredBounds_Load(string key, string bounds)
    {
        string yaml = $"must:\n  - {key}: [{{ joker: Blueprint }}, {{ joker: Brainstorm }}]\n    {bounds}\n";
        Assert.True(JamlConfig.TryLoad(yaml, out _, out var error), error);
    }

    [Theory]
    [InlineData("must:\n  - or: [{ joker: Blueprint }]\n    min: ~\n")]
    [InlineData("must:\n  - joker: Blueprint\n    min: ~\n")]
    [InlineData("must:\n  - joker: Blueprint\n    score: ~\n")]
    [InlineData("must:\n  - joker: Blueprint\n    min:\n")]
    [InlineData("must:\n  - joker: Blueprint\n    min: null\n")]
    [InlineData("must:\n  - joker: Blueprint\n    min: \"\"\n")]
    [InlineData("must:\n  - joker: Blueprint\n    antes: [1, ~]\n")]
    [InlineData("{\"must\":[{\"joker\":\"Blueprint\",\n\"antes\":[1],\n\"min\":null}]}")]
    public void ANullWhereANumberGoes_SaysSo(string yaml) =>
        RefusedWith(yaml, "JAML line 3: a value is missing here. Null and empty are not values.");

    [Theory]
    [InlineData("joker: Blueprint", "shopItems: ~")]
    [InlineData("joker: Blueprint", "boosterPacks:")]
    [InlineData("joker: Blueprint", "judgement: null")]
    [InlineData("tarotCard: TheFool", "emperor: ~")]
    [InlineData("spectralCard: Ankh", "seance: ~")]
    [InlineData("planetCard: Mars", "shopItems: ~")]
    [InlineData("standardCard: AceOfSpades", "boosterPacks: ~")]
    [InlineData("legendaryJoker: Perkeo", "spectralPacks: ~")]
    public void ANullSourceSlot_KeepsItsDefault_AndTheSearchRuns(string clause, string slot)
    {
        // 32.0.1 loaded a null slot as null and threw NullReferenceException when the search
        // built its filter. A null slot is the slot left out, as `ante: ~` is the default antes.
        string[] seeds = ["ALEEB", "PIROCKS", "P1111111", "MOTELY77"];

        List<(string Seed, int Score)> Scores(string yaml)
        {
            var config = ProofSearch.LoadOrThrow(yaml);
            var rows = new List<(string, int)>();
            using var search = MotelySearchBuilder.CreateSettings(config, int.MinValue)
                .WithSeedList(seeds)
                .WithThreadCount(1)
                .WithQuietMode(true)
                .WithScoredResultCallback(result => rows.Add((result.Seed, result.Score)))
                .Start();
            search.AwaitCompletion();
            return rows;
        }

        var withNull = Scores($"should:\n  - {clause}\n    antes: [1, 2, 3]\n    sources:\n      requireMegaPack: false\n      {slot}\n");
        var leftOut = Scores($"should:\n  - {clause}\n    antes: [1, 2, 3]\n    sources:\n      requireMegaPack: false\n");
        Assert.Equal(seeds.Length, leftOut.Count);
        Assert.Equal(leftOut, withNull);
    }

    [Theory]
    [InlineData("must:\n  - joker: Blueprint\n    antes: ~\n", "antes: needs a number or a list of them")]
    [InlineData("must:\n  - voucher: Hieroglyph\n    antes: [1]\n    rolls: ~\n", "rolls: needs a number or a list of them")]
    public void ANullAntesOrRolls_IsRefused(string yaml, string why) => RefusedWith(yaml, why);

    [Theory]
    [InlineData("and", "[]", "")]
    [InlineData("or", "[]", "")]
    [InlineData("or", "~", "")]
    public void AndOr_WithNoClauses_IsRefused(string key, string value, string rest) =>
        RefusedWith($"must:\n  - {key}: {value}\n{rest}", "needs at least one clause");

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

    [Theory]
    [InlineData("id:\nmust:\n  - joker: Blueprint\n    antes: [1]\nseeds:\n")]
    [InlineData("id: ~\nmust:\n  - joker: Blueprint\n    antes: [1]\nseeds: ~\n")]
    public void EmptyIdOrSeedsScalar_NormalizesToNonNullAndAnalyzesCleanly(string yaml)
    {
        var config = ProofSearch.LoadOrThrow(yaml);
        Assert.NotNull(config.Id);
        Assert.NotNull(config.Seeds);
        Assert.Empty(config.Seeds);
        Assert.Empty(Motely.Analysis.MotelyJamlyzer.Analyze(config));
    }
}
