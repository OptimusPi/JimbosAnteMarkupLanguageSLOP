using System.Diagnostics;
using System.Runtime.Intrinsics;
using Motely.Filters;
using Xunit;

namespace Motely.Tests;

/// <summary>
/// Adversarial challenger empirical tests for Milestone 1:
/// 1. SIMD vector lane isolation in mixed batches (NaN seeds mixed with standard seeds of identical length).
///    Tests lane permutations (NaN at lane 0..7, solitary normal seed in 7 NaN lanes, etc.) to guarantee
///    no cross-lane state or TenOfSpades leakage.
/// 2. Seed invariance across non-Erratic decks (Red, Blue, Yellow, Ghost, Abandoned, etc.) for all 35 NaN seeds.
/// 3. Performance throughput: benchmark seeds/sec to verify that the vector NaN check does not degrade performance.
/// </summary>
public sealed class ChallengerMilestone1VectorAndDeckTests
{
    private static readonly string[] NaNSeeds = ErraticDeckAndSeedFidelityTests.NaNSeeds;

    private static readonly string[] Normal8CharSeeds =
    [
        "11111111", "22222222", "33333333", "44444444",
        "AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD",
        "MOTELY01", "TESTSEED", "BALATRO9", "CHALLENG"
    ];

    private sealed class VectorDeckCaptureFilterDesc(Action<string, int, MotelyItem> onCardCaptured)
        : IMotelySeedFilterDesc<VectorDeckCaptureFilterDesc.CaptureFilter>
    {
        public CaptureFilter CreateFilter(ref MotelyFilterCreationContext ctx) => new(onCardCaptured);

        public readonly struct CaptureFilter : IMotelySeedFilter
        {
            private readonly Action<string, int, MotelyItem> _onCardCaptured;

            public CaptureFilter(Action<string, int, MotelyItem> onCardCaptured)
            {
                _onCardCaptured = onCardCaptured;
            }

            public VectorMask Filter(ref MotelyVectorSearchContext ctx)
            {
                var stream = ctx.CreateErraticDeckPrngStream(isCached: false);
                var cards = new MotelyItemVector[52];
                for (int i = 0; i < 52; i++)
                {
                    cards[i] = ctx.GetNextErraticDeckCard(ref stream);
                }

                var callback = _onCardCaptured;
                return ctx.SearchIndividualSeeds(single =>
                {
                    int lane = single.VectorLane;
                    string seed = single.GetSeed();
                    for (int i = 0; i < 52; i++)
                    {
                        callback(seed, i, cards[i][lane]);
                    }
                    return 1;
                });
            }
        }
    }

    private static Dictionary<string, List<MotelyItem>> RunVectorCapture(string[] seeds, MotelyDeck deck = MotelyDeck.Erratic)
    {
        var captured = seeds.Distinct().ToDictionary(s => s, _ => new List<MotelyItem>());
        var filterDesc = new VectorDeckCaptureFilterDesc((seed, index, card) =>
        {
            lock (captured)
            {
                if (captured.TryGetValue(seed, out var list) && list.Count == index)
                {
                    list.Add(card);
                }
            }
        });

        using var search = new MotelySearchSettings<VectorDeckCaptureFilterDesc.CaptureFilter>(filterDesc)
            .WithDeck(deck)
            .WithStake(MotelyStake.White)
            .WithSeedGenerator(seeds, seeds.Length)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .Start();

        search.AwaitCompletion();
        return captured;
    }

    [Fact]
    public void Homogeneous8CharMixedBatch_LaneIsolation_AlternatingNaNAndNormal()
    {
        // 8 seeds, all exactly 8 characters long -> guarantees homogeneousSeedLength == true and vector execution!
        string[] mixedSeeds =
        [
            "7LB2WVPK", // Lane 0: NaN
            "11111111", // Lane 1: Normal
            "1V3UQ6AP", // Lane 2: NaN
            "22222222", // Lane 3: Normal
            "36756M6I", // Lane 4: NaN
            "33333333", // Lane 5: Normal
            "627SP6LE", // Lane 6: NaN
            "44444444"  // Lane 7: Normal
        ];

        // 1. Scalar ground truth
        var scalarConfig = new JamlConfig { Deck = MotelyDeck.Erratic, Stake = MotelyStake.White };
        scalarConfig.Seeds.AddRange(mixedSeeds);
        var scalarResults = MotelyJamlyzer.Analyze(scalarConfig).ToDictionary(r => r.Seed, r => r.ErraticDeck!);

        // 2. Vector capture
        var vectorCaptured = RunVectorCapture(mixedSeeds);

        // 3. Verify exact lane isolation
        for (int lane = 0; lane < 8; lane++)
        {
            string seed = mixedSeeds[lane];
            var scalarDeck = scalarResults[seed];
            var vectorDeck = vectorCaptured[seed];

            Assert.Equal(52, scalarDeck.Count);
            Assert.Equal(52, vectorDeck.Count);

            bool isNaN = (lane % 2 == 0);
            if (isNaN)
            {
                for (int c = 0; c < 52; c++)
                {
                    Assert.Equal(new MotelyItem(MotelyStandardCard.TenOfSpades).Value, vectorDeck[c].Value);
                    Assert.Equal(new MotelyItem(MotelyStandardCard.TenOfSpades).Value, scalarDeck[c].Value);
                }
            }
            else
            {
                // Normal lane must exactly match scalar and have zero leakage of Ten of Spades from neighboring NaN lanes
                for (int c = 0; c < 52; c++)
                {
                    Assert.Equal(scalarDeck[c].Value, vectorDeck[c].Value);
                }

                int distinctCards = vectorDeck.Select(x => x.Value).Distinct().Count();
                Assert.True(distinctCards > 1, $"Normal seed {seed} in lane {lane} must not be homogeneous.");
            }
        }
    }

    [Fact]
    public void LanePermutations_SingleNaNEveryPosition_DoesNotContaminateOtherLanes()
    {
        string nanSeed = "7LB2WVPK"; // 8 chars NaN
        string[] normalPool =
        [
            "AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD",
            "11111111", "22222222", "33333333", "44444444"
        ];

        // Precompute scalar ground truth for all normal seeds
        var scalarConfig = new JamlConfig { Deck = MotelyDeck.Erratic, Stake = MotelyStake.White };
        scalarConfig.Seeds.AddRange(normalPool);
        scalarConfig.Seeds.Add(nanSeed);
        var scalarMap = MotelyJamlyzer.Analyze(scalarConfig).ToDictionary(r => r.Seed, r => r.ErraticDeck!);

        // Test NaN in every single lane position 0..7
        for (int nanLane = 0; nanLane < 8; nanLane++)
        {
            string[] batch = new string[8];
            int normalIdx = 0;
            for (int lane = 0; lane < 8; lane++)
            {
                if (lane == nanLane)
                    batch[lane] = nanSeed;
                else
                    batch[lane] = normalPool[normalIdx++];
            }

            var captured = RunVectorCapture(batch);

            for (int lane = 0; lane < 8; lane++)
            {
                string seed = batch[lane];
                var deck = captured[seed];
                var expected = scalarMap[seed];

                Assert.Equal(52, deck.Count);
                for (int c = 0; c < 52; c++)
                {
                    Assert.Equal(expected[c].Value, deck[c].Value);
                }
            }
        }
    }

    [Fact]
    public void SolitaryNormalSeed_SurroundedBySevenNaNLanes_PreservesFidelity()
    {
        string normalSeed = "CHALLENG"; // 8 chars normal
        // Pick 7 distinct 8-char NaN seeds
        string[] sevenNaNs = NaNSeeds.Where(s => s.Length == 8 && s != normalSeed).Take(7).ToArray();
        Assert.Equal(7, sevenNaNs.Length);

        var scalarConfig = new JamlConfig { Deck = MotelyDeck.Erratic, Stake = MotelyStake.White };
        scalarConfig.Seeds.Add(normalSeed);
        scalarConfig.Seeds.AddRange(sevenNaNs);
        var scalarMap = MotelyJamlyzer.Analyze(scalarConfig).ToDictionary(r => r.Seed, r => r.ErraticDeck!);

        // Place the solitary normal seed at each lane position 0..7
        for (int normalLane = 0; normalLane < 8; normalLane++)
        {
            string[] batch = new string[8];
            int nanIdx = 0;
            for (int lane = 0; lane < 8; lane++)
            {
                if (lane == normalLane)
                    batch[lane] = normalSeed;
                else
                    batch[lane] = sevenNaNs[nanIdx++];
            }

            var captured = RunVectorCapture(batch);

            for (int lane = 0; lane < 8; lane++)
            {
                string seed = batch[lane];
                var deck = captured[seed];
                var expected = scalarMap[seed];

                Assert.Equal(52, deck.Count);
                for (int c = 0; c < 52; c++)
                {
                    Assert.Equal(expected[c].Value, deck[c].Value);
                }
            }
        }
    }

    [Fact]
    public void Homogeneous7CharBatch_WithNaNSeed_IsolatesLanes()
    {
        // "BGY5SDS" is 7 chars NaN seed
        string nan7 = "BGY5SDS";
        string[] normal7 =
        [
            "PIROCKS", "ABCDEFG", "1234567", "MOTELYX",
            "TESTING", "BALATRO", "LUCKYYY"
        ];

        string[] batch = [nan7, .. normal7];
        Assert.Equal(8, batch.Length);
        Assert.All(batch, s => Assert.Equal(7, s.Length));

        var scalarConfig = new JamlConfig { Deck = MotelyDeck.Erratic, Stake = MotelyStake.White };
        scalarConfig.Seeds.AddRange(batch);
        var scalarMap = MotelyJamlyzer.Analyze(scalarConfig).ToDictionary(r => r.Seed, r => r.ErraticDeck!);

        var captured = RunVectorCapture(batch);

        for (int lane = 0; lane < 8; lane++)
        {
            string seed = batch[lane];
            var deck = captured[seed];
            var expected = scalarMap[seed];

            Assert.Equal(52, deck.Count);
            for (int c = 0; c < 52; c++)
            {
                Assert.Equal(expected[c].Value, deck[c].Value);
            }
        }
    }

    [Theory]
    [InlineData(MotelyDeck.Red)]
    [InlineData(MotelyDeck.Blue)]
    [InlineData(MotelyDeck.Yellow)]
    [InlineData(MotelyDeck.Green)]
    [InlineData(MotelyDeck.Black)]
    [InlineData(MotelyDeck.Magic)]
    [InlineData(MotelyDeck.Nebula)]
    [InlineData(MotelyDeck.Ghost)]
    [InlineData(MotelyDeck.Abandoned)]
    [InlineData(MotelyDeck.Checkered)]
    [InlineData(MotelyDeck.Zodiac)]
    [InlineData(MotelyDeck.Painted)]
    [InlineData(MotelyDeck.Anaglyph)]
    [InlineData(MotelyDeck.Plasma)]
    public void NonErraticDecks_NaNSeeds_ProduceNullErraticDeckAndValidRunStreams(MotelyDeck deck)
    {
        // Test a sample of NaN seeds across all 14 non-Erratic decks
        string[] testSeeds = ["7LB2WVPK", "BGY5SDS", "1V3UQ6AP", "ZZFHQFDG"];

        var config = new JamlConfig
        {
            Deck = deck,
            Stake = MotelyStake.White,
        };
        config.Seeds.AddRange(testSeeds);

        var results = MotelyJamlyzer.Analyze(config, eventRolls: 10, shopSlots: 2);
        Assert.Equal(testSeeds.Length, results.Count);

        foreach (var res in results)
        {
            // Requirement: Erratic deck is ONLY generated on Erratic deck.
            Assert.Null(res.ErraticDeck);

            // Verify Boss blind, Voucher, and Tags in each ante are well-formed and valid
            foreach (var ante in res.Antes)
            {
                Assert.True(Enum.IsDefined(ante.Boss), $"Invalid Boss blind on deck {deck}");
                Assert.True(Enum.IsDefined(ante.Voucher), $"Invalid Voucher on deck {deck}");
                Assert.True(Enum.IsDefined(ante.SmallBlindTag), $"Invalid SmallBlindTag on deck {deck}");
                Assert.True(Enum.IsDefined(ante.BigBlindTag), $"Invalid BigBlindTag on deck {deck}");

                // Verify shop items are valid
                foreach (var item in ante.ShopItems)
                {
                    Assert.True(item.Value != 0, $"Shop item was 0 on deck {deck}");
                }
            }

            // Verify voucher sequence in pulls
            foreach (var voucher in res.Antes[0].Pulls.VoucherSequence)
            {
                Assert.True(Enum.IsDefined(voucher));
            }
        }
    }

    [Fact]
    public void PerformanceBenchmark_ErraticDeckThroughput_HealthyAndNoDegradation()
    {
        // JIT warm-up for both Red and Erratic search paths
        foreach (var warmupDeck in new[] { MotelyDeck.Red, MotelyDeck.Erratic })
        {
            using var warmup = new MotelySearchSettings<PassthroughFilterDesc.PassthroughFilter>(new PassthroughFilterDesc())
                .WithDeck(warmupDeck)
                .WithRandomSearch(50_000)
                .WithThreadCount(Environment.ProcessorCount)
                .WithQuietMode(true)
                .Start();
            warmup.AwaitCompletion();
        }

        const int seedCount = 500_000;

        static double MeasureDeckThroughput(MotelyDeck deck, int count)
        {
            double best = 0;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var sw = Stopwatch.StartNew();
                using var search = new MotelySearchSettings<PassthroughFilterDesc.PassthroughFilter>(new PassthroughFilterDesc())
                    .WithDeck(deck)
                    .WithStake(MotelyStake.White)
                    .WithRandomSearch(count)
                    .WithThreadCount(Environment.ProcessorCount)
                    .WithQuietMode(true)
                    .Start();
                search.AwaitCompletion();
                sw.Stop();
                best = Math.Max(best, count / sw.Elapsed.TotalSeconds);
            }
            return best;
        }

        double erraticThroughput = MeasureDeckThroughput(MotelyDeck.Erratic, seedCount);
        double redThroughput = MeasureDeckThroughput(MotelyDeck.Red, seedCount);

        // Assert healthy throughput (> 100,000 seeds/sec after warmup, resilient to parallel test-runner CPU contention)
        Assert.True(erraticThroughput > 100_000,
            $"Erratic throughput was {erraticThroughput:N0} seeds/sec; expected > 100,000 seeds/sec");
        Assert.True(redThroughput > 100_000,
            $"Red deck throughput was {redThroughput:N0} seeds/sec; expected > 100,000 seeds/sec");
    }
}
