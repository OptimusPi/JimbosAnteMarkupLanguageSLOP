using System.Runtime.Intrinsics;
using Motely.Filters;
using Xunit;

namespace Motely.Tests;

public class AdversarialMilestone1ChallengerTests
{
    private static readonly string[] All35NaNSeeds =
    [
        "7LB2WVPK", "BGY5SDS", "1V3UQ6AP", "36756M6I", "3WBV4FSC",
        "627SP6LE", "6WG44S7Z", "81N7QMOM", "9PCOBR2D", "B9A4IA7S",
        "CJFU1DL1", "DHHHTLKL", "ENNGZMF2", "FAH3Y3S9", "HOWRQJVZ",
        "I4N2BX1E", "IU2LKGT8", "N9ER1XLW", "NSPURPAH", "NWMELXAM",
        "O6BRINCY", "O7CMD6K7", "PHLSY9F4", "Q6SEQZLP", "QIB7UA76",
        "RDNCZ58P", "S4DM5V37", "S4TMWWAE", "S793AULP", "SNML7XC8",
        "SS44ACWE", "SSU4L34S", "TO7WNQA5", "XEQH7CP9", "ZZFHQFDG"
    ];

    private static readonly string[] NormalBaselineSeeds =
    [
        "11111111", "22222222", "33333333", "44444444",
        "AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD"
    ];

    private static readonly string[] VariableLengthSeeds =
    [
        "1", "A", "9", "Z",                 // 1 char
        "11", "AB", "42", "ZZ",             // 2 chars
        "123", "XYZ", "FOO", "BAR",         // 3 chars
        "CATS", "DEAD", "BEEF", "9999",     // 4 chars
        "ALEEB", "BALAT", "RODEC", "11111", // 5 chars
        "MOTELY", "SPADES", "HEARTS",       // 6 chars
        "PIROCKS", "BALATRO", "ERRATIC",    // 7 chars
        "12345678", "ZZZZZZZZ", "AAAAAAAA"  // 8 chars
    ];

    #region 1. All 35 NaN Seeds Empirical Challenge

    [Theory]
    [MemberData(nameof(GetAll35NaNSeedsData))]
    public void Scalar_EachOf35NaNSeeds_ProducesAllTenOfSpades(string seed)
    {
        using var search = new MotelySearchSettings<PassthroughFilterDesc.PassthroughFilter>(new PassthroughFilterDesc())
            .WithDeck(MotelyDeck.Erratic)
            .WithStake(MotelyStake.White)
            .WithSeedGenerator([seed], 1)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithJimmolate(ctx =>
            {
                var stream = ctx.CreateErraticDeckPrngStream(isCached: false);
                Assert.True(double.IsNaN(stream.State), $"Seed {seed} PRNG stream state MUST be NaN.");

                // Check standard 52 cards
                for (int i = 0; i < 52; i++)
                {
                    var card = ctx.GetNextErraticDeckCard(ref stream);
                    Assert.Equal(MotelyStandardcardRank.Ten, card.StandardcardRank);
                    Assert.Equal(MotelyStandardcardSuit.Spades, card.StandardcardSuit);
                    Assert.Equal(new MotelyItem(MotelyStandardCard.TenOfSpades).Value, card.Value);
                }

                // Stress beyond 52: should remain stable and continue returning 10_S
                for (int i = 52; i < 100; i++)
                {
                    var extraCard = ctx.GetNextErraticDeckCard(ref stream);
                    Assert.Equal(new MotelyItem(MotelyStandardCard.TenOfSpades).Value, extraCard.Value);
                }

                return 1;
            })
            .Start();

        search.AwaitCompletion();
        Assert.Equal(1, search.MatchingSeeds);
    }

    [Fact]
    public void Vector_All35NaNSeeds_ProduceAllTenOfSpades_InPaddedBatches()
    {
        for (int chunkStart = 0; chunkStart < All35NaNSeeds.Length; chunkStart += 8)
        {
            var chunk = All35NaNSeeds.Skip(chunkStart).Take(8).ToArray();
            var paddedChunk = chunk.Length == 8 ? chunk : [.. chunk, .. Enumerable.Repeat(All35NaNSeeds[0], 8 - chunk.Length)];

            var captured = paddedChunk.Distinct().ToDictionary(s => s, _ => new List<MotelyItem>());

            var filterDesc = new ErraticCaptureFilterDesc((seed, index, card) =>
            {
                lock (captured)
                {
                    if (captured.TryGetValue(seed, out var list) && list.Count == index)
                    {
                        list.Add(card);
                    }
                }
            });

            using var search = new MotelySearchSettings<ErraticCaptureFilterDesc.CardCaptureFilter>(filterDesc)
                .WithDeck(MotelyDeck.Erratic)
                .WithStake(MotelyStake.White)
                .WithSeedGenerator(paddedChunk, paddedChunk.Length)
                .WithThreadCount(1)
                .WithQuietMode(true)
                .Start();

            search.AwaitCompletion();

            foreach (var s in chunk)
            {
                Assert.True(captured.TryGetValue(s, out var deck));
                Assert.Equal(52, deck.Count);
                foreach (var card in deck)
                {
                    Assert.Equal(MotelyStandardcardRank.Ten, card.StandardcardRank);
                    Assert.Equal(MotelyStandardcardSuit.Spades, card.StandardcardSuit);
                }
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Vector_SingleNaNLane_DoesNotCorruptNormalLanes(int nanLaneIndex)
    {
        string nanSeed = "7LB2WVPK";
        string[] batch = (string[])NormalBaselineSeeds.Clone();
        batch[nanLaneIndex] = nanSeed;

        // Obtain scalar ground-truth for all 8 seeds
        var scalarGroundTruth = new Dictionary<string, IReadOnlyList<MotelyItem>>();
        var scalarConfig = new JamlConfig { Deck = MotelyDeck.Erratic, Stake = MotelyStake.White };
        scalarConfig.Seeds.AddRange(batch);
        foreach (var r in MotelyJamlyzer.Analyze(scalarConfig))
        {
            scalarGroundTruth[r.Seed] = r.ErraticDeck!;
        }

        // Run vector batch
        var vectorCaptured = new Dictionary<int, List<MotelyItem>>();
        for (int l = 0; l < 8; l++) vectorCaptured[l] = [];

        var filterDesc = new LaneCaptureFilterDesc((lane, index, card) =>
        {
            lock (vectorCaptured)
            {
                if (vectorCaptured[lane].Count == index)
                    vectorCaptured[lane].Add(card);
            }
        });

        using var search = new MotelySearchSettings<LaneCaptureFilterDesc.LaneFilter>(filterDesc)
            .WithDeck(MotelyDeck.Erratic)
            .WithStake(MotelyStake.White)
            .WithSeedGenerator(batch, batch.Length)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .Start();

        search.AwaitCompletion();

        // Validate each lane
        for (int lane = 0; lane < 8; lane++)
        {
            string seed = batch[lane];
            var expectedCards = scalarGroundTruth[seed];
            var actualCards = vectorCaptured[lane];

            Assert.Equal(52, actualCards.Count);

            if (lane == nanLaneIndex)
            {
                // NaN lane must be all 10_S
                Assert.All(actualCards, c =>
                {
                    Assert.Equal(MotelyStandardcardRank.Ten, c.StandardcardRank);
                    Assert.Equal(MotelyStandardcardSuit.Spades, c.StandardcardSuit);
                });
            }
            else
            {
                // Normal lane must exactly match scalar execution
                for (int i = 0; i < 52; i++)
                {
                    Assert.Equal(expectedCards[i].Value, actualCards[i].Value);
                }
            }
        }
    }

    #endregion

    #region 2. Variable Length Seeds Stress Tests

    [Theory]
    [MemberData(nameof(GetVariableLengthSeedsData))]
    public void VariableLengthSeeds_ScalarAndVector_BitExactParity(string seed)
    {
        // Scalar
        var scalarConfig = new JamlConfig { Deck = MotelyDeck.Erratic, Stake = MotelyStake.White };
        scalarConfig.Seeds.Add(seed);
        var scalarResult = Assert.Single(MotelyJamlyzer.Analyze(scalarConfig));
        Assert.NotNull(scalarResult.ErraticDeck);
        Assert.Equal(52, scalarResult.ErraticDeck.Count);

        // Vector: fill batch of 8 with this seed
        string[] batch = Enumerable.Repeat(seed, 8).ToArray();
        var vectorCards = new List<MotelyItem>();

        var filterDesc = new LaneCaptureFilterDesc((lane, index, card) =>
        {
            if (lane == 0)
            {
                lock (vectorCards)
                {
                    if (vectorCards.Count == index) vectorCards.Add(card);
                }
            }
        });

        using var search = new MotelySearchSettings<LaneCaptureFilterDesc.LaneFilter>(filterDesc)
            .WithDeck(MotelyDeck.Erratic)
            .WithStake(MotelyStake.White)
            .WithSeedGenerator(batch, batch.Length)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .Start();

        search.AwaitCompletion();

        Assert.Equal(52, vectorCards.Count);
        for (int i = 0; i < 52; i++)
        {
            Assert.Equal(scalarResult.ErraticDeck[i].Value, vectorCards[i].Value);
        }
    }

    [Fact]
    public void MixedLengthBatch_InVectorEngine_ExecutesWithoutError()
    {
        string[] mixedLengths = ["1", "AB", "123", "CATS", "ALEEB", "MOTELY", "PIROCKS", "12345678"];

        var scalarConfig = new JamlConfig { Deck = MotelyDeck.Erratic, Stake = MotelyStake.White };
        scalarConfig.Seeds.AddRange(mixedLengths);
        var scalarResults = MotelyJamlyzer.Analyze(scalarConfig).ToDictionary(r => r.Seed, r => r.ErraticDeck!);

        var captured = mixedLengths.ToDictionary(s => s, _ => new List<MotelyItem>());

        var filterDesc = new ErraticCaptureFilterDesc((seed, index, card) =>
        {
            lock (captured)
            {
                if (captured.TryGetValue(seed, out var list) && list.Count == index)
                    list.Add(card);
            }
        });

        using var search = new MotelySearchSettings<ErraticCaptureFilterDesc.CardCaptureFilter>(filterDesc)
            .WithDeck(MotelyDeck.Erratic)
            .WithStake(MotelyStake.White)
            .WithSeedGenerator(mixedLengths, mixedLengths.Length)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .Start();

        search.AwaitCompletion();

        foreach (var seed in mixedLengths)
        {
            var scalarDeck = scalarResults[seed];
            var searchDeck = captured[seed];

            Assert.Equal(52, searchDeck.Count);
            for (int c = 0; c < 52; c++)
            {
                Assert.Equal(scalarDeck[c].Value, searchDeck[c].Value);
            }
        }
    }

    #endregion

    #region Helpers & Data Providers

    public static TheoryData<string> GetAll35NaNSeedsData()
    {
        var data = new TheoryData<string>();
        foreach (var s in All35NaNSeeds) data.Add(s);
        return data;
    }

    public static TheoryData<string> GetVariableLengthSeedsData()
    {
        var data = new TheoryData<string>();
        foreach (var s in VariableLengthSeeds) data.Add(s);
        return data;
    }

    private sealed class ErraticCaptureFilterDesc(Action<string, int, MotelyItem> recordCard)
        : IMotelySeedFilterDesc<ErraticCaptureFilterDesc.CardCaptureFilter>
    {
        public CardCaptureFilter CreateFilter(ref MotelyFilterCreationContext ctx) => new(recordCard);

        public readonly struct CardCaptureFilter : IMotelySeedFilter
        {
            private readonly Action<string, int, MotelyItem> _recordCard;

            public CardCaptureFilter(Action<string, int, MotelyItem> recordCard)
            {
                _recordCard = recordCard;
            }

            public VectorMask Filter(ref MotelyVectorSearchContext ctx)
            {
                var stream = ctx.CreateErraticDeckPrngStream(isCached: false);
                var cards = new MotelyItemVector[52];
                for (int i = 0; i < 52; i++)
                {
                    cards[i] = ctx.GetNextErraticDeckCard(ref stream);
                }

                var recorder = _recordCard;
                return ctx.SearchIndividualSeeds(single =>
                {
                    int lane = single.VectorLane;
                    string seed = single.GetSeed();
                    for (int i = 0; i < 52; i++)
                    {
                        recorder(seed, i, cards[i][lane]);
                    }
                    return 1;
                });
            }
        }
    }

    private sealed class LaneCaptureFilterDesc(Action<int, int, MotelyItem> recordLaneCard)
        : IMotelySeedFilterDesc<LaneCaptureFilterDesc.LaneFilter>
    {
        public LaneFilter CreateFilter(ref MotelyFilterCreationContext ctx) => new(recordLaneCard);

        public readonly struct LaneFilter : IMotelySeedFilter
        {
            private readonly Action<int, int, MotelyItem> _recordLaneCard;

            public LaneFilter(Action<int, int, MotelyItem> recordLaneCard)
            {
                _recordLaneCard = recordLaneCard;
            }

            public VectorMask Filter(ref MotelyVectorSearchContext ctx)
            {
                var stream = ctx.CreateErraticDeckPrngStream(isCached: false);
                var cards = new MotelyItemVector[52];
                for (int i = 0; i < 52; i++)
                {
                    cards[i] = ctx.GetNextErraticDeckCard(ref stream);
                }

                var recorder = _recordLaneCard;
                return ctx.SearchIndividualSeeds(single =>
                {
                    int lane = single.VectorLane;
                    for (int i = 0; i < 52; i++)
                    {
                        recorder(lane, i, cards[i][lane]);
                    }
                    return 1;
                });
            }
        }
    }

    #endregion
}
