using System.Text;
using Motely.Analysis;
using Motely.Filters;

namespace Motely.Tests;

/// <summary>
/// End-to-end parity and security regression tests verifying:
/// - Balatro lua51.dll G.P_CARDS ASCII playing-card ordering across Erratic, StandardCard, StartingDraw, and PokerHand.
/// - Balatro lua51.dll NaN pseudohash parity (+QNaN math.abs sign clearing, x - floor(x), S4DM5V37 / S4TMWWAE / XEQH7CP9 / 7LB2WVPK).
/// - MotelyJamlyzer ErraticDeck list serialization and natural-NaN stream resume validation.
/// - Starting deck vouchers (Magic, Nebula, Zodiac) and deck compositions (Abandoned, Checkered, Painted, Erratic) across Scalar, SIMD, and Jamlyzer.
/// - Hybrid joker spawn source fallback (shop + judgement) parity between SIMD and Scalar.
/// - Digit-leading JAML enum names ("8 Ball", "4 Fingers", "6th Sense") in JamlConfig and JamlShorthand.
/// </summary>
public sealed class AuditRegressionParityTests
{
    private static List<string> SearchSeedList(string jaml, params string[] seeds)
    {
        var config = JamlConfig.FromJaml(jaml);
        var plan = MotelySearchBuilder.CreatePlan(config);
        var matched = new List<string>();
        using var search = plan.Settings
            .WithSeedList(seeds)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithSeedMatchCallback(line =>
            {
                int comma = line.IndexOf(',');
                matched.Add(comma < 0 ? line : line[..comma]);
            })
            .Start();
        search.AwaitCompletion();
        return matched;
    }

    private static List<string> SearchSequentialSingleBatch(string jaml, string targetSeed)
    {
        const int batchChars = 1;
        long batchIdx = SeedMath.SeedToBatchIndex(targetSeed, batchChars);
        var config = JamlConfig.FromJaml(jaml);
        var plan = MotelySearchBuilder.CreatePlan(config);
        var matched = new List<string>();
        using var search = plan.Settings
            .WithBatchCharacterCount(batchChars)
            .WithSequentialSearch()
            .WithStartBatchIndex(batchIdx)
            .WithEndBatchIndex(batchIdx + 1)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithSeedMatchCallback(line =>
            {
                int comma = line.IndexOf(',');
                matched.Add(comma < 0 ? line : line[..comma]);
            })
            .Start();
        search.AwaitCompletion();
        return matched;
    }

    [Fact]
    public void PCards_AsciiRankOrder_MatchesShippedBalatroLua51()
    {
        // Verified directly against Balatro lua51.dll:
        // 11OK37SD on Erratic has 21 Aces and 3 Tens (previously swapped 21 Tens / 3 Aces).
        // 1UQ8D22L on Erratic has 21 Tens and 0 Aces.
        // ALEEB on Erratic has card[1] == KingOfSpades (previously QueenOfSpades).
        var cfg = JamlConfig.FromJaml("deck: Erratic\nseeds: [11OK37SD, 1UQ8D22L, ALEEB]\n");
        var results = MotelyJamlyzer.Analyze(cfg, eventRolls: 5);
        Assert.Equal(3, results.Count);

        var r1 = results[0].ErraticDeck!;
        Assert.Equal(52, r1.Count);
        Assert.Equal(21, r1.Count(c => c.StandardcardRank == MotelyStandardcardRank.Ace));
        Assert.Equal(3, r1.Count(c => c.StandardcardRank == MotelyStandardcardRank.Ten));

        var r2 = results[1].ErraticDeck!;
        Assert.Equal(52, r2.Count);
        Assert.Equal(21, r2.Count(c => c.StandardcardRank == MotelyStandardcardRank.Ten));
        Assert.Equal(0, r2.Count(c => c.StandardcardRank == MotelyStandardcardRank.Ace));

        var rAleeb = results[2].ErraticDeck!;
        Assert.Equal(52, rAleeb.Count);
        Assert.Equal(MotelyStandardcardRank.King, rAleeb[1].StandardcardRank);
        Assert.Equal(MotelyStandardcardSuit.Spades, rAleeb[1].StandardcardSuit);
    }

    [Fact]
    public void NaNPseudohash_HolographicShopJoker_MatchesScalarSimdAndJamlyzer()
    {
        // Verified directly against Balatro lua51.dll:
        // pseudohash("edisho1S4DM5V37") == NaN -> pseudoseed -> 0.99249255698284466 -> Holographic on the first shop joker!
        const string jaml = """
            deck: Red
            stake: White
            must:
              - joker: Any
                edition: Holographic
                sources:
                  shopItems: [0, 1, 2, 3, 4]
                antes: [1]
            """;

        string[] nanShopSeeds = ["S4DM5V37", "S4TMWWAE", "XEQH7CP9"];
        var scalarMatches = SearchSeedList(jaml, nanShopSeeds);
        Assert.Equal(nanShopSeeds.OrderBy(x => x), scalarMatches.OrderBy(x => x));

        foreach (string seed in nanShopSeeds)
        {
            var simdMatches = SearchSequentialSingleBatch(jaml, seed);
            Assert.Contains(seed, simdMatches);
        }
    }

    [Fact]
    public void Jamlyzer_ResumeFromNaturalNaNStream_Succeeds_WhileCorruptNaN_Throws()
    {
        // S4DM5V37 produces a genuine Balatro NaN on pseudohash("parkingS4DM5V37").
        var cfg = JamlConfig.FromJaml("deck: Red\nseeds: [S4DM5V37]\n");
        var window1 = MotelyJamlyzer.Analyze(cfg, eventRolls: 5);
        var bag = Assert.Single(window1).StreamStates;
        Assert.True(double.IsNaN(bag.Parking));

        // Window 2 resume with the natural NaN stream state must succeed!
        var window2 = MotelyJamlyzer.Analyze(cfg, bag, eventRolls: 5);
        Assert.Single(window2);

        // Corrupt NaN on a seed whose stream is NOT naturally NaN must still throw ArgumentOutOfRangeException.
        var normalCfg = JamlConfig.FromJaml("deck: Red\nseeds: [UNITTEST]\n");
        var normalBag = Assert.Single(MotelyJamlyzer.Analyze(normalCfg, eventRolls: 5)).StreamStates;
        var corruptBag = normalBag with { Misprint = double.NaN };
        Assert.Throws<ArgumentOutOfRangeException>(() => MotelyJamlyzer.Analyze(normalCfg, corruptBag, eventRolls: 5));
    }

    [Fact]
    public void ErraticNaNDeck_7LB2WVPK_Produces52TenOfSpades_AndMatchesStartingDrawAndPokerHand()
    {
        // pseudohash("erratic7LB2WVPK") == NaN -> +QNaN in Balatro -> index 51 (S_T = TenOfSpades) x 52.
        var cfg = JamlConfig.FromJaml("deck: Erratic\nseeds: [7LB2WVPK]\n");
        var res = Assert.Single(MotelyJamlyzer.Analyze(cfg, eventRolls: 5));
        Assert.NotNull(res.ErraticDeck);
        Assert.Equal(52, res.ErraticDeck!.Count);
        Assert.All(res.ErraticDeck, item =>
        {
            Assert.Equal(MotelyStandardcardRank.Ten, item.StandardcardRank);
            Assert.Equal(MotelyStandardcardSuit.Spades, item.StandardcardSuit);
        });

        const string drawJaml = """
            deck: Erratic
            must:
              - startingDraw: TenOfSpades
                min: 8
                antes: [1]
              - pokerHand: FourOfAKind
                antes: [1]
            """;
        var matches = SearchSeedList(drawJaml, "7LB2WVPK");
        Assert.Single(matches, "7LB2WVPK");
    }

    [Fact]
    public void DeckCompositions_AbandonedCheckeredAndPainted_RespectedInStartingDraw()
    {
        // Abandoned deck has 0 face cards across all seeds.
        const string abandonedFaceJaml = """
            deck: Abandoned
            must:
              - or:
                  - startingDraw:
                    rank: Jack
                    antes: [1]
                  - startingDraw:
                    rank: Queen
                    antes: [1]
                  - startingDraw:
                    rank: King
                    antes: [1]
            """;
        Assert.Empty(SearchSeedList(abandonedFaceJaml, "ALEEB", "12345678", "S4DM5V37", "PIROCKS"));

        // Checkered deck has only Spades and Hearts (0 Clubs, 0 Diamonds).
        const string checkeredMinorSuitsJaml = """
            deck: Checkered
            must:
              - or:
                  - startingDraw:
                    suit: Clubs
                    antes: [1]
                  - startingDraw:
                    suit: Diamonds
                    antes: [1]
            """;
        Assert.Empty(SearchSeedList(checkeredMinorSuitsJaml, "ALEEB", "12345678", "S4DM5V37", "PIROCKS"));

        // Painted deck has +2 hand size (10 cards drawn at start of Ante 1 Small Blind!).
        // Red deck only draws 8 cards, so min: 9 is impossible on Red, whereas Painted draws 10 cards.
        const string nineHeartsOrSpadesCheckeredPainted = """
            deck: Painted
            must:
              - or:
                  - startingDraw:
                    suit: Hearts
                    min: 9
                    antes: [1]
                  - startingDraw:
                    suit: Spades
                    min: 9
                    antes: [1]
                  - startingDraw:
                    suit: Clubs
                    min: 9
                    antes: [1]
                  - startingDraw:
                    suit: Diamonds
                    min: 9
                    antes: [1]
            """;
        const string impossibleOnRed = """
            deck: Red
            must:
              - startingDraw:
                suit: Hearts
                min: 9
                antes: [1]
            """;
        Assert.Empty(SearchSeedList(impossibleOnRed, "ALEEB", "12345678", "99999999"));
        var paintedPlan = MotelySearchBuilder.CreatePlan(JamlConfig.FromJaml(nineHeartsOrSpadesCheckeredPainted));
        Assert.NotNull(paintedPlan);
    }

    [Fact]
    public void StartingDeckVouchers_NebulaMagicZodiac_UnlockTier2InAnte1AcrossScalarSimdAndJamlyzer()
    {
        // Nebula starts with Telescope -> Observatory is unlocked in Ante 1!
        const string nebulaObsJaml = """
            deck: Nebula
            stake: White
            must:
              - voucher: Observatory
                antes: [1]
            """;
        var config = JamlConfig.FromJaml(nebulaObsJaml);

        // Find at least one seed in batch 0 on Nebula with Ante 1 Observatory via SIMD and verify Scalar + Jamlyzer agree.
        var plan = MotelySearchBuilder.CreatePlan(config);
        var simdHits = new List<string>();
        using (var s = plan.Settings
            .WithBatchCharacterCount(2)
            .WithSequentialSearch()
            .WithStartBatchIndex(0)
            .WithEndBatchIndex(1)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithSeedMatchCallback(line => simdHits.Add(line.Split(',')[0]))
            .Start())
        {
            s.AwaitCompletion();
        }

        Assert.NotEmpty(simdHits);
        string hitSeed = simdHits[0];

        // Scalar verification
        Assert.Contains(hitSeed, SearchSeedList(nebulaObsJaml, hitSeed));

        // Jamlyzer verification
        var jamlyzed = Assert.Single(
            MotelyJamlyzer.Analyze(JamlConfig.FromJaml($"deck: Nebula\nseeds: [{hitSeed}]\n"), eventRolls: 5)
        );
        Assert.Equal(MotelyVoucher.Observatory, jamlyzed.Antes.Single(a => a.Ante == 1).Voucher);

        // Red deck on the SAME seed must NOT roll Observatory in Ante 1 (Telescope not redeemed yet)
        var redJamlyzed = Assert.Single(
            MotelyJamlyzer.Analyze(JamlConfig.FromJaml($"deck: Red\nseeds: [{hitSeed}]\n"), eventRolls: 5)
        );
        Assert.NotEqual(MotelyVoucher.Observatory, redJamlyzed.Antes.Single(a => a.Ante == 1).Voucher);
    }

    [Fact]
    public void HybridJokerSources_SimdAndScalarParity_DoesNotDropJudgementSeeds()
    {
        const string hybridJaml = """
            deck: Red
            must:
              - joker: Blueprint
                sources:
                  shopItems: [0, 1]
                  judgement: [0]
                antes: [1]
            """;
        var config = JamlConfig.FromJaml(hybridJaml);
        var plan = MotelySearchBuilder.CreatePlan(config);
        var simdHits = new List<string>();
        using (var s = plan.Settings
            .WithBatchCharacterCount(3)
            .WithSequentialSearch()
            .WithStartBatchIndex(0)
            .WithEndBatchIndex(1)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithSeedMatchCallback(line => simdHits.Add(line.Split(',')[0]))
            .Start())
        {
            s.AwaitCompletion();
        }

        Assert.NotEmpty(simdHits);
        var scalarHits = SearchSeedList(hybridJaml, [.. simdHits]);
        Assert.Equal(simdHits.OrderBy(x => x), scalarHits.OrderBy(x => x));
    }

    [Theory]
    [InlineData("8 Ball", MotelyJoker.EightBall)]
    [InlineData("8_Ball", MotelyJoker.EightBall)]
    [InlineData("EightBall", MotelyJoker.EightBall)]
    [InlineData("4 Fingers", MotelyJoker.FourFingers)]
    [InlineData("6th Sense", MotelyJoker.SixthSense)]
    public void DigitLeadingJokerNames_ParseInJamlLoader(string input, MotelyJoker expected)
    {
        var cfg = JamlConfig.FromJaml($"must:\n  - joker: \"{input}\"\n    antes: [1]\n");
        var mappedClause = Assert.IsType<JokerClause>(Assert.Single(cfg.Must));
        Assert.Equal([expected], mappedClause.Jokers);
    }

    [Fact]
    public async Task DisposeFromInsideCallback_MultiThreadedStartupRaceStress_NeverCorruptsHeapOrThrows()
    {
        for (int iter = 0; iter < 40; iter++)
        {
            int threads = (iter % 2 == 0) ? 4 : 8;
            int triggerAt = (iter % 3) + 1;
            int matches = 0;
            IMotelySearch? search = null;
            search = new MotelySearchSettings<PassthroughFilterDesc.PassthroughFilter>(new PassthroughFilterDesc())
                .WithSequentialSearch()
                .WithBatchCharacterCount(3)
                .WithThreadCount(threads)
                .StopAfter(10)
                .WithQuietMode(true)
                .WithJimmolate(_ => 1)
                .WithSeedMatchCallback(_ =>
                {
                    if (Interlocked.Increment(ref matches) == triggerAt)
                        search!.Dispose();
                })
                .CreateSearch();

            await search.RunSearchAsync().WaitAsync(TimeSpan.FromSeconds(15));
            search.Dispose();
        }
    }

    [Fact]
    public void EmptySeed_ZeroLengthParity_MatchesShippedBalatroLua51()
    {
        // Ground truth directly from D:\SteamLibrary\steamapps\common\Balatro\lua51.dll for seed = "":
        // hashed_seed = pseudohash("") = 1.0
        // pseudohash("erratic") = 0.45752552206801056
        // 52-card Erratic Deck starting deck:
        string[] luaErraticCodes =
        [
            "H_4","S_T","S_6","H_J","D_J","D_8","D_6","S_8","D_T","C_K",
            "D_3","C_4","H_2","S_K","H_8","C_8","H_8","C_4","C_8","D_3",
            "H_3","H_9","H_2","S_T","H_4","H_A","C_9","C_6","H_9","S_9",
            "S_4","D_7","C_4","H_K","S_7","C_6","C_5","S_7","C_2","S_6",
            "C_Q","S_T","H_T","C_5","S_2","H_2","C_8","S_9","C_2","H_Q",
            "S_A","S_5"
        ];

        static string ToLuaCode(MotelyItem item)
        {
            char suit = item.StandardcardSuit switch
            {
                MotelyStandardcardSuit.Clubs => 'C',
                MotelyStandardcardSuit.Diamonds => 'D',
                MotelyStandardcardSuit.Hearts => 'H',
                MotelyStandardcardSuit.Spades => 'S',
                _ => '?',
            };
            char rank = item.StandardcardRank switch
            {
                MotelyStandardcardRank.Two => '2',
                MotelyStandardcardRank.Three => '3',
                MotelyStandardcardRank.Four => '4',
                MotelyStandardcardRank.Five => '5',
                MotelyStandardcardRank.Six => '6',
                MotelyStandardcardRank.Seven => '7',
                MotelyStandardcardRank.Eight => '8',
                MotelyStandardcardRank.Nine => '9',
                MotelyStandardcardRank.Ten => 'T',
                MotelyStandardcardRank.Jack => 'J',
                MotelyStandardcardRank.Queen => 'Q',
                MotelyStandardcardRank.King => 'K',
                MotelyStandardcardRank.Ace => 'A',
                _ => '?',
            };
            return $"{suit}_{rank}";
        }

        // 1. MotelyJamlyzer on Erratic Deck with seeds: [""]
        var cfg = JamlConfig.FromJaml("deck: Erratic\nstake: White\nseeds:\n  - \"\"\n");
        Assert.Equal([""], cfg.Seeds);
        var jamResults = MotelyJamlyzer.Analyze(cfg, eventRolls: 5);
        var emptyResult = Assert.Single(jamResults);
        Assert.Equal("", emptyResult.Seed);
        Assert.NotNull(emptyResult.ErraticDeck);
        Assert.Equal(52, emptyResult.ErraticDeck!.Count);
        Assert.Equal(luaErraticCodes, emptyResult.ErraticDeck.Select(ToLuaCode).ToArray());

        // 2. Mixed-length WithSeedList(["", "   ", "ALEEB", "12345678"]) with SIMD additional filters + scoring:
        //    "" matches CouponTag in ante 1 (verified directly against lua51.dll Tag1); whitespace-only "   " is dropped cleanly.
        const string couponFilter = """
            deck: Red
            stake: White
            must:
              - tag: CouponTag
                antes: [1]
            should:
              - boss: any
                antes: [1]
                score: 5
            """;
        var couponMatches = SearchSeedList(couponFilter, ["", "   ", "ALEEB", "12345678"]);
        Assert.Contains("", couponMatches);
        Assert.DoesNotContain("   ", couponMatches);
    }

    [Fact]
    public void EmptySeed_ProviderChainingCliAndMcp_RoundTripCleanly()
    {
        // 1. MotelyChainedSeedProvider: "" in first provider must NOT prematurely exhaust first!
        var first = new Motely.SeedProviders.MotelySeedListProvider(["", "A"]);
        var second = new Motely.SeedProviders.MotelySeedListProvider(["B"]);
        var chained = new Motely.SeedProviders.MotelyChainedSeedProvider(first, second);

        Assert.True(chained.TryNextSeed(out var s0));
        Assert.Equal("", s0);
        Assert.True(chained.TryNextSeed(out var s1));
        Assert.Equal("A", s1);
        Assert.True(chained.TryNextSeed(out var s2));
        Assert.Equal("B", s2);
        Assert.False(chained.TryNextSeed(out _));

        // 2. MotelyTopSeedSink round-trip of "" in YAML seeds: block
        Assert.True(
            MotelyTopSeedSink.TryRewriteAndValidate("deck: Red\nstake: White\n", ["", "ALEEB"], out var rewritten, out var err),
            err
        );
        Assert.Contains("  - \"\"", rewritten);
        var reloaded = JamlConfig.FromJaml(rewritten);
        Assert.Equal(["", "ALEEB"], reloaded.Seeds);

        // 3. CliSearchMode: explicit "" inline works; blank lines in seed files are ignored while quoted "" works.
        Assert.True(CliSearchMode.TryResolveSeeds("", out var inlineEmpty, out var inlineErr), inlineErr);
        Assert.Equal([""], inlineEmpty);
        Assert.False(CliSearchMode.TryResolveSeeds("   ", out _, out _));

        string tempSeedFile = Path.Combine(Path.GetTempPath(), $"motely-seeds-{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllText(tempSeedFile, "seed,score\n\n   \nALEEB,10\n\"\",5\n\n12345678,3\n");
            Assert.True(CliSearchMode.TryResolveSeeds(tempSeedFile, out var fileSeeds, out var fileErr), fileErr);
            Assert.Equal(["ALEEB", "", "12345678"], fileSeeds);
        }
        finally
        {
            if (File.Exists(tempSeedFile))
                File.Delete(tempSeedFile);
        }

        // 4. MotelyJamlyzer on ""
        var empty = Assert.Single(MotelyJamlyzer.Analyze(new JamlConfig { Seeds = [""] }));
        Assert.Equal("", empty.Seed);
        Assert.Equal(MotelyTag.CouponTag, empty.Antes.Single(a => a.Ante == 1).SmallBlindTag);
    }

    [Fact]
    public void MotelyJamlyzer_ResumeAndSearchAgent_HandleNonCanonicalQuotedEmptyAndGlitchedSeeds()
    {
        // Dictionary resume keyed by result.Seed (documented pattern) with lowercase, quoted empty, and quoted NaN seeds
        var config = new JamlConfig
        {
            Deck = MotelyDeck.Red,
            Stake = MotelyStake.White,
            Seeds = ["aleeb", "\"\"", " \"s4dm5v37\" "],
        };

        var page1 = MotelyJamlyzer.Analyze(config, eventRolls: 10);
        Assert.Equal(["ALEEB", "", "S4DM5V37"], page1.Select(r => r.Seed));
        Assert.All(page1, r => Assert.Equal(10, r.StreamStates.RollOffset));
        Assert.True(double.IsNaN(page1[2].StreamStates.Parking));

        var resumeMap = page1.ToDictionary(r => r.Seed, r => r.StreamStates);
        var page2 = MotelyJamlyzer.Analyze(config, resumeMap, eventRolls: 10);
        Assert.Equal(["ALEEB", "", "S4DM5V37"], page2.Select(r => r.Seed));
        Assert.All(page2, r => Assert.Equal(20, r.StreamStates.RollOffset));

        // Single-bag resume overload on quoted NaN seed
        var quotedNanCfg = new JamlConfig
        {
            Deck = MotelyDeck.Red,
            Stake = MotelyStake.White,
            Seeds = ["\"S4DM5V37\""],
        };
        var nanResumed = Assert.Single(
            MotelyJamlyzer.Analyze(quotedNanCfg, page1[2].StreamStates, eventRolls: 5)
        );
        Assert.Equal(15, nanResumed.StreamStates.RollOffset);
    }
}

