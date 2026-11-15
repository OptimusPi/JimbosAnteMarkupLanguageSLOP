using System.Runtime.Intrinsics;
using Motely.Filters;

namespace Motely.Tests;

/// <summary>
/// Validates exact Balatro game RNG fidelity across:
/// 1. Seeds of lengths 1 through 8 characters.
/// 2. All 35 seeds where pseudohash("erratic" + seed) evaluates to NaN, guaranteeing that
///    the Erratic deck deterministically defaults to 52 Tens of Spades (10_S, S_T in Lua).
/// 3. Scalar and SIMD vector engine parity across mixed batches of NaN and normal seeds.
/// </summary>
public class ErraticDeckAndSeedFidelityTests
{
    /// <summary>
    /// The complete set of 35 seeds where Balatro's pseudohash("erratic" + seed) evaluates to NaN.
    /// Ground truth in Balatro Lua: pseudorandom_element sorts keys alphabetically ("C_2".."S_T"),
    /// and upon receiving NaN PRNG state falls back to keys[#keys] = "S_T" (Ten of Spades).
    /// </summary>
    public static readonly string[] NaNSeeds =
    [
        "7LB2WVPK", "BGY5SDS", "1V3UQ6AP", "36756M6I", "3WBV4FSC",
        "627SP6LE", "6WG44S7Z", "81N7QMOM", "9PCOBR2D", "B9A4IA7S",
        "CJFU1DL1", "DHHHTLKL", "ENNGZMF2", "FAH3Y3S9", "HOWRQJVZ",
        "I4N2BX1E", "IU2LKGT8", "N9ER1XLW", "NSPURPAH", "NWMELXAM",
        "O6BRINCY", "O7CMD6K7", "PHLSY9F4", "Q6SEQZLP", "QIB7UA76",
        "RDNCZ58P", "S4DM5V37", "S4TMWWAE", "S793AULP", "SNML7XC8",
        "SS44ACWE", "SSU4L34S", "TO7WNQA5", "XEQH7CP9", "ZZFHQFDG"
    ];

    public static readonly string[] VariableLengthSeeds =
    [
        "1",        // 1 char
        "A",        // 1 char
        "42",       // 2 chars
        "AB",       // 2 chars
        "123",      // 3 chars
        "XYZ",      // 3 chars
        "CATS",     // 4 chars
        "ALEEB",    // 5 chars
        "MOTELY",   // 6 chars
        "PIROCKS",  // 7 chars
        "12345678"  // 8 chars
    ];

    [Fact]
    public void Exactly35Seeds_EvaluateErraticPseudohashToNaN()
    {
        Assert.Equal(35, NaNSeeds.Length);
        foreach (var seed in NaNSeeds)
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
                    Assert.True(double.IsNaN(stream.State), $"Seed {seed} erratic stream state should be NaN.");
                    return 1;
                })
                .Start();
            search.AwaitCompletion();
            Assert.Equal(1, search.MatchingSeeds);
        }
    }

    [Fact]
    public void All35NaNSeeds_Produce52TensOfSpades_InScalarJamlyzer()
    {
        var config = new JamlConfig
        {
            Deck = MotelyDeck.Erratic,
            Stake = MotelyStake.White,
        };
        config.Seeds.AddRange(NaNSeeds);

        var results = MotelyJamlyzer.Analyze(config);
        Assert.Equal(35, results.Count);

        foreach (var result in results)
        {
            Assert.NotNull(result.ErraticDeck);
            Assert.Equal(52, result.ErraticDeck.Count);

            foreach (var card in result.ErraticDeck)
            {
                Assert.Equal(MotelyStandardcardRank.Ten, card.StandardcardRank);
                Assert.Equal(MotelyStandardcardSuit.Spades, card.StandardcardSuit);
                Assert.Equal(new MotelyItem(MotelyStandardCard.TenOfSpades).Value, card.Value);
            }
        }
    }

    private sealed class ErraticCardCaptureFilterDesc(Action<string, int, MotelyItem> recordVectorCard)
        : IMotelySeedFilterDesc<ErraticCardCaptureFilterDesc.CardCaptureFilter>
    {
        public CardCaptureFilter CreateFilter(ref MotelyFilterCreationContext ctx) =>
            new(recordVectorCard);

        public readonly struct CardCaptureFilter : IMotelySeedFilter
        {
            private readonly Action<string, int, MotelyItem> _recordVectorCard;

            public CardCaptureFilter(Action<string, int, MotelyItem> recordVectorCard)
            {
                _recordVectorCard = recordVectorCard;
            }

            public VectorMask Filter(ref MotelyVectorSearchContext ctx)
            {
                var stream = ctx.CreateErraticDeckPrngStream(isCached: false);
                var cards = new MotelyItemVector[52];
                for (int i = 0; i < 52; i++)
                {
                    cards[i] = ctx.GetNextErraticDeckCard(ref stream);
                }

                var recorder = _recordVectorCard;
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

    [Fact]
    public void All35NaNSeeds_Produce52TensOfSpades_InVectorEngine()
    {
        // Test in chunks of 8 (vector batch width)
        for (int chunkStart = 0; chunkStart < NaNSeeds.Length; chunkStart += 8)
        {
            var chunk = NaNSeeds.Skip(chunkStart).Take(8).ToArray();
            // Pad to 8 if necessary using first NaN seed
            var paddedChunk = chunk.Length == 8 ? chunk : [.. chunk, .. Enumerable.Repeat(NaNSeeds[0], 8 - chunk.Length)];

            var captured = new Dictionary<string, List<MotelyItem>>();
            foreach (var s in paddedChunk)
                captured[s] = [];

            var filterDesc = new ErraticCardCaptureFilterDesc((seed, index, card) =>
            {
                lock (captured)
                {
                    if (captured.TryGetValue(seed, out var list))
                    {
                        if (list.Count == index)
                            list.Add(card);
                    }
                }
            });

            using var search = new MotelySearchSettings<ErraticCardCaptureFilterDesc.CardCaptureFilter>(filterDesc)
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

    [Fact]
    public void MixedVectorBatch_PreservesNaNFallbackAndNormalSeedParity()
    {
        // 4 NaN seeds and 4 normal seeds interleaved in one 8-lane SIMD vector
        string[] mixedSeeds =
        [
            "7LB2WVPK",   // NaN -> 52 Tens of Spades
            "ALEEB",      // Normal
            "BGY5SDS",    // NaN -> 52 Tens of Spades
            "PIROCKS",    // Normal
            "1V3UQ6AP",   // NaN -> 52 Tens of Spades
            "UNITTEST",   // Normal
            "36756M6I",   // NaN -> 52 Tens of Spades
            "11111111"    // Normal
        ];

        // 1. Get scalar ground truth for each seed
        var scalarConfig = new JamlConfig
        {
            Deck = MotelyDeck.Erratic,
            Stake = MotelyStake.White,
        };
        scalarConfig.Seeds.AddRange(mixedSeeds);
        var scalarResults = MotelyJamlyzer.Analyze(scalarConfig).ToDictionary(r => r.Seed, r => r.ErraticDeck!);

        // 2. Capture vector output for the mixed batch
        var vectorCaptured = mixedSeeds.ToDictionary(s => s, _ => new List<MotelyItem>());
        var filterDesc = new ErraticCardCaptureFilterDesc((seed, index, card) =>
        {
            lock (vectorCaptured)
            {
                if (vectorCaptured.TryGetValue(seed, out var list) && list.Count == index)
                    list.Add(card);
            }
        });

        using var search = new MotelySearchSettings<ErraticCardCaptureFilterDesc.CardCaptureFilter>(filterDesc)
            .WithDeck(MotelyDeck.Erratic)
            .WithStake(MotelyStake.White)
            .WithSeedGenerator(mixedSeeds, mixedSeeds.Length)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .Start();

        search.AwaitCompletion();

        // 3. Verify lane by lane
        for (int i = 0; i < mixedSeeds.Length; i++)
        {
            string seed = mixedSeeds[i];
            var scalarDeck = scalarResults[seed];
            var vectorDeck = vectorCaptured[seed];

            Assert.Equal(52, scalarDeck.Count);
            Assert.Equal(52, vectorDeck.Count);

            bool isNaN = i % 2 == 0;
            if (isNaN)
            {
                // NaN seed: all 52 cards are Ten of Spades in both scalar and vector
                for (int c = 0; c < 52; c++)
                {
                    Assert.Equal(new MotelyItem(MotelyStandardCard.TenOfSpades).Value, vectorDeck[c].Value);
                    Assert.Equal(new MotelyItem(MotelyStandardCard.TenOfSpades).Value, scalarDeck[c].Value);
                }
            }
            else
            {
                // Normal seed: vector and scalar must be 100% identical card-for-card
                for (int c = 0; c < 52; c++)
                {
                    Assert.Equal(scalarDeck[c].Value, vectorDeck[c].Value);
                }

                // Normal seed must NOT be all Tens of Spades
                var distinctCards = vectorDeck.Select(x => x.Value).Distinct().Count();
                Assert.True(distinctCards > 1, $"Normal seed {seed} should have varied card composition.");
            }
        }
    }

    [Theory]
    [InlineData("1")]
    [InlineData("A")]
    [InlineData("42")]
    [InlineData("AB")]
    [InlineData("123")]
    [InlineData("XYZ")]
    [InlineData("CATS")]
    [InlineData("ALEEB")]
    [InlineData("MOTELY")]
    [InlineData("PIROCKS")]
    [InlineData("12345678")]
    public void VariableLengthSeeds_1To8Chars_ProduceValidErraticDecks(string seed)
    {
        var config = new JamlConfig
        {
            Deck = MotelyDeck.Erratic,
            Stake = MotelyStake.White,
        };
        config.Seeds.Add(seed);

        var results = MotelyJamlyzer.Analyze(config);
        var result = Assert.Single(results);
        Assert.NotNull(result.ErraticDeck);
        Assert.Equal(52, result.ErraticDeck.Count);

        // Every card has valid rank and suit
        foreach (var card in result.ErraticDeck)
        {
            Assert.True(Enum.IsDefined(card.StandardcardRank));
            Assert.True(Enum.IsDefined(card.StandardcardSuit));
            Assert.Equal(MotelyItemTypeCategory.Standardcard, card.TypeCategory);
        }

        // Varied composition for normal seeds
        var distinctCards = result.ErraticDeck.Select(x => x.Value).Distinct().Count();
        Assert.True(distinctCards > 1, $"Seed '{seed}' should produce varied erratic deck cards.");
    }

    [Fact]
    public void ErraticClauses_MatchCorrectlyOnNaNDeck()
    {
        var config = new JamlConfig
        {
            Deck = MotelyDeck.Erratic,
            Stake = MotelyStake.White,
        };
        config.Seeds.Add("7LB2WVPK");
        config.Must.Add(new ErraticRankClause { Rank = MotelyStandardcardRank.Ten, Antes = [1] });
        config.Must.Add(new ErraticSuitClause { Suit = MotelyStandardcardSuit.Spades, Antes = [1] });

        var results = MotelyJamlyzer.Analyze(config);
        var result = Assert.Single(results);
        Assert.NotNull(result.ErraticDeck);
        Assert.All(result.ErraticDeck, c =>
        {
            Assert.Equal(MotelyStandardcardRank.Ten, c.StandardcardRank);
            Assert.Equal(MotelyStandardcardSuit.Spades, c.StandardcardSuit);
        });
    }
}
