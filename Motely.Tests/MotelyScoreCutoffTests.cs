namespace Motely.Tests;

public sealed class MotelyScoreCutoffTests
{
    [Fact]
    public void Auto_FirstSeedAlwaysPasses_ThenDropsBelowRunningMax()
    {
        var cutoff = MotelyScoreCutoff.Auto();

        Assert.True(cutoff.ShouldEmit(3));
        Assert.Equal(3, cutoff.CurrentHigh);
        Assert.True(cutoff.ShouldEmit(3));
        Assert.False(cutoff.ShouldEmit(2));
        Assert.True(cutoff.ShouldEmit(8));
        Assert.Equal(8, cutoff.CurrentHigh);
        Assert.False(cutoff.ShouldEmit(7));
        Assert.True(cutoff.ShouldEmit(8));
    }

    [Fact]
    public void Fixed_EmitsAtOrAboveFloor()
    {
        var cutoff = MotelyScoreCutoff.Fixed(4);

        Assert.False(cutoff.ShouldEmit(3));
        Assert.True(cutoff.ShouldEmit(4));
        Assert.True(cutoff.ShouldEmit(99));
        Assert.False(cutoff.ShouldEmit(0));
    }

    [Fact]
    public void Off_EmitsEverything()
    {
        var cutoff = MotelyScoreCutoff.Off();

        Assert.True(cutoff.ShouldEmit(int.MinValue));
        Assert.True(cutoff.ShouldEmit(0));
        Assert.True(cutoff.ShouldEmit(int.MaxValue));
    }

    [Fact]
    public void TryParse_EmptyAndAuto_AreRunningMax()
    {
        Assert.True(MotelyScoreCutoff.TryParse("", out var empty, out var emptyErr));
        Assert.Null(emptyErr);
        Assert.True(empty.IsAuto);

        Assert.True(MotelyScoreCutoff.TryParse("auto", out var auto, out var autoErr));
        Assert.Null(autoErr);
        Assert.True(auto.IsAuto);
    }

    [Fact]
    public void TryResolve_JamlCutoff_ReplacesAuto()
    {
        Assert.True(MotelyScoreCutoff.TryResolve(null, 12, out var cutoff, out var text, out var err));
        Assert.Null(err);
        Assert.False(cutoff.IsAuto);
        Assert.Equal(12, cutoff.EngineCutoff);
        Assert.Equal("12 (yaml)", text);
    }

    [Fact]
    public void TryResolve_HostText_WinsOverJamlCutoff()
    {
        Assert.True(MotelyScoreCutoff.TryResolve("off", 12, out var off, out var offText, out _));
        Assert.Equal(int.MinValue, off.EngineCutoff);
        Assert.True(off.ShouldEmit(0));
        Assert.Equal("off", offText);

        Assert.True(MotelyScoreCutoff.TryResolve("auto", 12, out var auto, out _, out _));
        Assert.True(auto.IsAuto);

        Assert.False(MotelyScoreCutoff.TryResolve("nope", 12, out _, out _, out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void TryResolve_NothingGiven_IsAuto()
    {
        Assert.True(MotelyScoreCutoff.TryResolve(null, null, out var cutoff, out var text, out _));
        Assert.True(cutoff.IsAuto);
        Assert.Equal("auto", text);
    }

    [Fact]
    public void JamlCutoff_DropsSeedsBelowItInTheEngine()
    {
        const string yaml = """
            should:
              - joker: any
                score: 1
            """;
        var config = Motely.Filters.JamlConfig.FromJaml(yaml);
        long Matches(int engineCutoff)
        {
            using var search = Motely.Filters.MotelySearchBuilder
                .CreateSettings(config, engineCutoff)
                .WithSeedGenerator(["MOTELY77"], 1)
                .WithThreadCount(1)
                .WithQuietMode(true)
                .WithAutoScoreCutoff(false)
                .Start();
            search.AwaitCompletion();
            return search.MatchingSeeds;
        }

        Assert.Equal(1, Matches(int.MinValue));
        Assert.Equal(0, Matches(int.MaxValue));
    }

    [Theory]
    [InlineData("deck: Red\n", "ALEEB")]
    [InlineData("mustNot:\n  - voucher: Hieroglyph\n    antes: [1]\n", "ALEEB")]
    [InlineData("must:\n  - voucher: Telescope\n    antes: [1]\n", "P1111111")]
    [InlineData("must:\n  - voucher: Telescope\n    antes: [1]\n    min: 0\n", "ALEEB")]
    [InlineData("mustNot:\n  - legendaryJoker: Perkeo\n    antes: [1]\n", "ALEEB")]
    public void Cutoff_HoldsWhenNoClauseScores(string yaml, string seed)
    {
        Assert.Equal(1, CutoffMatches(yaml, seed, int.MinValue));
        Assert.Equal(1, CutoffMatches(yaml, seed, 0));
        Assert.Equal(0, CutoffMatches(yaml, seed, 1));

        Assert.Equal(0, CutoffMatches("cutoff: 1\n" + yaml, seed, null));
        Assert.Equal(1, CutoffMatches("cutoff: 1\n" + yaml, seed, int.MinValue));
    }

    private static long CutoffMatches(string yaml, string seed, int? engineCutoff)
    {
        using var search = Motely.Filters.MotelySearchBuilder
            .CreateSettings(Motely.Filters.JamlConfig.FromJaml(yaml), engineCutoff)
            .WithSeedList([seed])
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithAutoScoreCutoff(false)
            .Start();
        search.AwaitCompletion();
        return search.MatchingSeeds;
    }

    [Theory]
    [InlineData("deck: Red\n")]
    [InlineData("must:\n  - voucher: Telescope\n    antes: [1]\n    min: 0\n")]
    public void FloorNoSeedCanReach_ScoresWithoutAllocating(string yaml)
    {
        var settings = Motely.Filters.MotelySearchBuilder.CreateSettings(
            Motely.Filters.JamlConfig.FromJaml(yaml),
            1
        );
        var measured = new AllocationMeasuringScoreDesc(settings.SeedScoreDesc!);

        using var search = settings
            .WithSeedScoreProvider(measured)
            .WithSequentialSearch()
            .WithBatchCharacterCount(3)
            .WithStartBatchIndex(0)
            .WithEndBatchIndex(2)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithAutoScoreCutoff(false)
            .Start();
        search.AwaitCompletion();

        Assert.Equal(0, search.MatchingSeeds);
        Assert.True(measured.MeasuredCalls > 1000, $"{measured.MeasuredCalls} calls measured");
        Assert.Equal(0, measured.Bytes);
    }

    private sealed class AllocationMeasuringScoreDesc(IMotelySeedScoreDesc inner)
        : IMotelySeedScoreDesc,
            IMotelySeedScoreProvider
    {
        private IMotelySeedScoreProvider? _provider;
        private bool _warm;

        public long MeasuredCalls { get; private set; }
        public long Bytes { get; private set; }

        public IMotelySeedScoreProvider CreateScoreProvider(ref MotelyFilterCreationContext ctx)
        {
            _provider = inner.CreateScoreProvider(ref ctx);
            return this;
        }

        public VectorMask Score(
            ref MotelyVectorSearchContext searchContext,
            MotelyScoredSeedResult[] buffer,
            VectorMask baseFilterMask,
            int scoreThreshold = int.MinValue
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            VectorMask scored = _provider!.Score(ref searchContext, buffer, baseFilterMask, scoreThreshold);
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            if (_warm)
            {
                MeasuredCalls++;
                Bytes += bytes;
            }
            _warm = true;
            return scored;
        }
    }

    [Theory]
    [InlineData("deck: Red\n")]
    [InlineData("mustNot:\n  - voucher: Hieroglyph\n    antes: [1]\n")]
    public void WithScoredResultGate_GatesAFilterWithNothingToScore(string yaml)
    {
        var config = Motely.Filters.JamlConfig.FromJaml(yaml);
        string[] seeds = ["ALEEB", "PIROCKS", "P1111111", "MOTELY77"];

        long Matches(Func<int, bool>? gate, List<string> seen)
        {
            using var search = Motely.Filters.MotelySearchBuilder
                .CreateSettings(config, int.MinValue)
                .WithSeedList(seeds)
                .WithThreadCount(1)
                .WithQuietMode(true)
                .WithAutoScoreCutoff(false)
                .WithScoredResultGate(gate)
                .WithSeedMatchCallback(seen.Add)
                .Start();
            search.AwaitCompletion();
            return search.MatchingSeeds;
        }

        var open = new List<string>();
        long ungated = Matches(null, open);
        Assert.True(ungated >= 2, $"{ungated} seeds pass with no gate");
        Assert.Equal(ungated, open.Count);

        var shut = new List<string>();
        Assert.Equal(0, Matches(_ => false, shut));
        Assert.Empty(shut);

        var record = new List<string>();
        Assert.Equal(1, Matches(MotelyScoreCutoff.Max().ShouldEmit, record));
        Assert.Single(record);
    }

    [Fact]
    public void WithScoredResultGate_GatesCallbacksMatchingSeedsAndStopAfter()
    {
        const string yaml = """
            should:
              - joker: any
                antes: [1]
                score: 1
            """;
        var config = Motely.Filters.JamlConfig.FromJaml(yaml);

        var gatedOut = new List<string>();
        using (var rejectedSearch = Motely.Filters.MotelySearchBuilder
            .CreateSettings(config, int.MinValue)
            .WithSeedList(["11111111", "22222222", "33333333"])
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithAutoScoreCutoff(false)
            .WithScoredResultGate(_ => false)
            .WithScoredResultCallback(r => gatedOut.Add(r.Seed))
            .Start())
        {
            rejectedSearch.AwaitCompletion();
            Assert.Empty(gatedOut);
            Assert.Equal(0, rejectedSearch.MatchingSeeds);
        }

        // Only seeds scoring >= 2 pass the gate; StopAfter(1) must wait until a passing seed arrives.
        var kept = new List<string>();
        using (var floorSearch = Motely.Filters.MotelySearchBuilder
            .CreateSettings(config, int.MinValue)
            .WithBatchCharacterCount(2)
            .WithSequentialSearch()
            .WithStartBatchIndex(0)
            .WithEndBatchIndex(1)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithAutoScoreCutoff(false)
            .WithScoredResultGate(score => score >= 2)
            .StopAfter(1)
            .WithScoredResultCallback(r => kept.Add(r.Seed))
            .Start())
        {
            floorSearch.AwaitCompletion();
            Assert.NotEmpty(kept);
            Assert.Equal(kept.Count, floorSearch.MatchingSeeds);
        }

        // Max cutoff wired through WithScoredResultGate only emits strictly increasing records.
        var maxCutoff = MotelyScoreCutoff.Max();
        var records = new List<int>();
        using (var maxSearch = Motely.Filters.MotelySearchBuilder
            .CreateSettings(config, int.MinValue)
            .WithBatchCharacterCount(2)
            .WithSequentialSearch()
            .WithStartBatchIndex(0)
            .WithEndBatchIndex(2)
            .WithThreadCount(Environment.ProcessorCount)
            .WithQuietMode(true)
            .WithAutoScoreCutoff(false)
            .WithScoredResultGate(maxCutoff.ShouldEmit)
            .WithScoredResultCallback(r =>
            {
                lock (records)
                {
                    records.Add(r.Score);
                }
            })
            .Start())
        {
            maxSearch.AwaitCompletion();
            Assert.NotEmpty(records);
            Assert.Equal(records.Count, maxSearch.MatchingSeeds);
        }
    }
}
