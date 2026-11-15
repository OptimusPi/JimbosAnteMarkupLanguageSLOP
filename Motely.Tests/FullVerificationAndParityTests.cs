using System.Runtime.Intrinsics;
using Motely.Analysis;
using Motely.Enums;
using Motely.Filters;
namespace Motely.Tests;

public sealed class FullVerificationAndParityTests
{
    private static readonly string[] NaNSeeds =
    [
        "7LB2WVPK", "BGY5SDS", "1V3UQ6AP", "36756M6I", "3WBV4FSC",
        "627SP6LE", "6WG44S7Z", "81N7QMOM", "9PCOBR2D", "B9A4IA7S",
        "CJFU1DL1", "DHHHTLKL", "ENNGZMF2", "FAH3Y3S9", "HOWRQJVZ",
        "I4N2BX1E", "IU2LKGT8", "N9ER1XLW", "NSPURPAH", "NWMELXAM",
        "O6BRINCY", "O7CMD6K7", "PHLSY9F4", "Q6SEQZLP", "QIB7UA76",
        "RDNCZ58P", "S4DM5V37", "S4TMWWAE", "S793AULP", "SNML7XC8",
        "SS44ACWE", "SSU4L34S", "TO7WNQA5", "XEQH7CP9", "ZZFHQFDG"
    ];

    private static readonly string[] SingleStreamNaNSeeds =
    [
        "S4DM5V37", "S4TMWWAE", "XEQH7CP9"
    ];

    private static readonly string[] SeedsLength0Through8 =
    [
        "",         // 0 chars
        "1",        // 1 char
        "42",       // 2 chars
        "XYZ",      // 3 chars
        "CATS",     // 4 chars
        "ALEEB",    // 5 chars
        "MOTELY",   // 6 chars
        "PIROCKS",  // 7 chars
        "12345678"  // 8 chars
    ];

    [Fact]
    public void Seeds_Lengths0Through8_ExecuteAccurately()
    {
        foreach (var seed in SeedsLength0Through8)
        {
            var yaml = $"deck: Red\nstake: White\nseeds: [\"{seed}\"]\nmust:\n  - joker: Blueprint\n    antes: [1]\n";
            var config = JamlConfig.FromJaml(yaml);
            Assert.NotNull(config);
            var plan = MotelySearchBuilder.CreatePlan(config);
            Assert.NotNull(plan);

            // Execute search over the seed
            using var search = new MotelySearchSettings<PassthroughFilterDesc.PassthroughFilter>(new PassthroughFilterDesc())
                .WithDeck(MotelyDeck.Red)
                .WithStake(MotelyStake.White)
                .WithSeedGenerator([seed], 1)
                .WithThreadCount(1)
                .WithQuietMode(true)
                .Start();

            search.AwaitCompletion();
            Assert.Equal(1, search.MatchingSeeds);
        }
    }

    [Fact]
    public void GlitchedNaNSeeds_35ErraticDeckSeeds_EvaluateTo52TensOfSpades()
    {
        Assert.Equal(35, NaNSeeds.Length);

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

        // Also test stream state is NaN across all 35 seeds
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
    public void GlitchedNaNSeeds_SingleStreamNaNSeeds_PreserveExactIEEE754Behavior()
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

        var config = JamlConfig.FromJaml(jaml);
        config.Seeds.AddRange(SingleStreamNaNSeeds);

        var results = MotelyJamlyzer.Analyze(config);
        Assert.Equal(3, results.Count);

        // All 3 single-stream NaN seeds must find Holographic on one of the first 5 shop items of ante 1
        foreach (var result in results)
        {
            Assert.Contains(SingleStreamNaNSeeds, s => s == result.Seed);
            var ante1 = Assert.Single(result.Antes, a => a.Ante == 1);
            Assert.NotEmpty(ante1.ShopItems);
            Assert.Contains(ante1.ShopItems.Take(5), item => item.Edition == MotelyItemEdition.Holographic);
        }

        // Also verify plan search execution matches all 3 seeds
        var plan = MotelySearchBuilder.CreatePlan(config);
        var matched = new List<string>();
        using var search = plan.Settings
            .WithSeedList(SingleStreamNaNSeeds)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithSeedMatchCallback(line =>
            {
                int comma = line.IndexOf(',');
                matched.Add(comma < 0 ? line : line[..comma]);
            })
            .Start();
        search.AwaitCompletion();
        Assert.Equal(SingleStreamNaNSeeds.OrderBy(x => x), matched.OrderBy(x => x));
    }

    [Fact]
    public void StartingVoucherDecks_MagicNebulaZodiac_ActiveByDefault()
    {
        // 1. Magic Deck: CrystalBall
        var magicState = MotelyDeck.Magic.GetDefaultRunState();
        Assert.True(magicState.IsVoucherActive(MotelyVoucher.CrystalBall));
        Assert.False(magicState.IsVoucherActive(MotelyVoucher.Telescope));

        var magicVec = MotelyDeck.Magic.GetDefaultVectorRunState();
        Assert.Equal(Vector256<int>.AllBitsSet, magicVec.IsVoucherActive(MotelyVoucher.CrystalBall));

        // 2. Nebula Deck: Telescope
        var nebulaState = MotelyDeck.Nebula.GetDefaultRunState();
        Assert.True(nebulaState.IsVoucherActive(MotelyVoucher.Telescope));
        Assert.False(nebulaState.IsVoucherActive(MotelyVoucher.CrystalBall));

        var nebulaVec = MotelyDeck.Nebula.GetDefaultVectorRunState();
        Assert.Equal(Vector256<int>.AllBitsSet, nebulaVec.IsVoucherActive(MotelyVoucher.Telescope));

        // 3. Zodiac Deck: TarotMerchant, PlanetMerchant, Overstock
        var zodiacState = MotelyDeck.Zodiac.GetDefaultRunState();
        Assert.True(zodiacState.IsVoucherActive(MotelyVoucher.TarotMerchant));
        Assert.True(zodiacState.IsVoucherActive(MotelyVoucher.PlanetMerchant));
        Assert.True(zodiacState.IsVoucherActive(MotelyVoucher.Overstock));
        Assert.False(zodiacState.IsVoucherActive(MotelyVoucher.CrystalBall));

        var zodiacVec = MotelyDeck.Zodiac.GetDefaultVectorRunState();
        Assert.Equal(Vector256<int>.AllBitsSet, zodiacVec.IsVoucherActive(MotelyVoucher.TarotMerchant));
        Assert.Equal(Vector256<int>.AllBitsSet, zodiacVec.IsVoucherActive(MotelyVoucher.PlanetMerchant));
        Assert.Equal(Vector256<int>.AllBitsSet, zodiacVec.IsVoucherActive(MotelyVoucher.Overstock));

        // 4. Runtime search test on each deck
        foreach (var deck in new[] { MotelyDeck.Magic, MotelyDeck.Nebula, MotelyDeck.Zodiac })
        {
            var yaml = $"deck: {deck}\nstake: White\nseeds: [\"ALEEB\"]\nmust:\n  - voucher: Blank\n    antes: [1]\n";
            var config = JamlConfig.FromJaml(yaml);
            Assert.Equal(deck, config.Deck);
            var plan = MotelySearchBuilder.CreatePlan(config);
            Assert.NotNull(plan);
        }
    }
}
