using Motely.Analysis;
using Motely.Filters;

namespace Motely.Tests;

/// <summary>
/// <c>with: { vouchers: [...] }</c> on a shop-reading clause builds that clause's shop from the
/// deck's vouchers plus the listed ones, through the engine's own
/// CreateShopItemStream(ante, runState). The expected shops come from that same engine call.
/// </summary>
public sealed class JamlWithVouchersTests
{
    private const int Slots = 8;

    /// <summary>Tarots in the first eight ante-1 shop slots, straight from the engine.</summary>
    private static int EngineShopTarots(string seed, MotelyDeck deck, params MotelyVoucher[] held)
    {
        int tarots = 0;
        using var search = new MotelySearchSettings<PassthroughFilterDesc.PassthroughFilter>(new PassthroughFilterDesc())
            .WithDeck(deck)
            .WithStake(MotelyStake.White)
            .WithSeedList([seed])
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithJimmolate((MotelySingleSearchContext ctx) =>
            {
                var state = deck.GetDefaultRunState();
                foreach (var v in held)
                    state.ActivateVoucher(v);
                var shop = ctx.CreateShopItemStream(1, state);
                for (int i = 0; i < Slots; i++)
                    if (ctx.GetNextShopItem(ref shop).TypeCategory == MotelyItemTypeCategory.TarotCard)
                        tarots++;
                return 1;
            })
            .Start();
        search.AwaitCompletion();
        return tarots;
    }

    private static int JamlTally(string seed, string with)
    {
        var config = JamlConfig.FromJaml(
            $"deck: Red\nseeds: [{seed}]\nshould:\n  - tarotCard: any\n    antes: [1]\n    sources: {{ shopItems: [0,1,2,3,4,5,6,7] }}\n{with}"
        );
        return Assert.Single(MotelyJamlyzer.Analyze(config, eventRolls: 0)).Tally![0];
    }

    [Theory]
    [InlineData("ALEEB")]
    [InlineData("PIROCKS")]
    [InlineData("UNITTEST")]
    [InlineData("12345678")]
    public void TarotClause_CountsTheShopItsVouchersBuild(string seed)
    {
        Assert.Equal(EngineShopTarots(seed, MotelyDeck.Red), JamlTally(seed, ""));
        Assert.Equal(
            EngineShopTarots(seed, MotelyDeck.Red, MotelyVoucher.TarotMerchant),
            JamlTally(seed, "    with: { vouchers: [TarotMerchant] }\n")
        );
        Assert.Equal(
            EngineShopTarots(seed, MotelyDeck.Red, MotelyVoucher.TarotMerchant, MotelyVoucher.TarotTycoon),
            JamlTally(seed, "    with: { vouchers: [TarotMerchant, TarotTycoon] }\n")
        );
    }

    [Fact]
    public void Vouchers_ChangeWhatTheShopHolds_OnSomeSeed()
    {
        // Guards the test above against passing because the voucher did nothing.
        string[] seeds = ["ALEEB", "PIROCKS", "UNITTEST", "12345678"];
        Assert.Contains(seeds, s =>
            EngineShopTarots(s, MotelyDeck.Red) != EngineShopTarots(s, MotelyDeck.Red, MotelyVoucher.TarotTycoon));
    }

    [Fact]
    public void DeckRunState_HoldsTheDecksVouchersPlusTheHeldOnes()
    {
        var red = MotelyDeck.Red.GetRunState([MotelyVoucher.MagicTrick]);
        Assert.True(red.IsVoucherActive(MotelyVoucher.MagicTrick));
        Assert.False(red.IsVoucherActive(MotelyVoucher.TarotMerchant));

        var zodiac = MotelyDeck.Zodiac.GetRunState([MotelyVoucher.MagicTrick]);
        Assert.True(zodiac.IsVoucherActive(MotelyVoucher.TarotMerchant));
        Assert.True(zodiac.IsVoucherActive(MotelyVoucher.MagicTrick));

        Assert.Equal(
            MotelyDeck.Red.GetDefaultRunState().VoucherBitfield,
            MotelyDeck.Red.GetRunState([]).VoucherBitfield
        );
    }

    // Eight 8-char seeds fill every lane of one vector batch.
    private static readonly string[] LaneSeeds =
    [
        "ALEEBOOO", "UNITTEST", "KK1XD111", "MOTELY77",
        "PIFREAKS", "BALATROO", "AAAAAAAA", "11111111",
    ];

    /// <summary>
    /// Rolls ante 1's shop on the SIMD context and again per seed on the scalar one, both from the
    /// same run state, and passes the lanes where every slot agrees.
    /// </summary>
    private sealed class ShopParityDesc(MotelyVoucher[] held, List<string> mismatches)
        : IMotelySeedFilterDesc<ShopParityDesc.ShopParityFilter>
    {
        public ShopParityFilter CreateFilter(ref MotelyFilterCreationContext ctx) =>
            new(ctx.Deck.GetRunState(held), mismatches);

        public readonly struct ShopParityFilter(MotelyRunState shopState, List<string> mismatches)
            : IMotelySeedFilter
        {
            private readonly MotelyRunState _shopState = shopState;
            private readonly List<string> _mismatches = mismatches;

            public VectorMask Filter(ref MotelyVectorSearchContext ctx)
            {
                var state = _shopState;
                var sink = _mismatches;

                var vector = new int[Slots][];
                var vectorShop = ctx.CreateShopItemStream(1, state);
                for (int slot = 0; slot < Slots; slot++)
                {
                    var item = ctx.GetNextShopItem(ref vectorShop);
                    vector[slot] = new int[MotelyItemVector.Count];
                    for (int lane = 0; lane < MotelyItemVector.Count; lane++)
                        vector[slot][lane] = item[lane].Value;
                }

                return ctx.SearchIndividualSeeds(single =>
                {
                    bool same = true;
                    var scalarShop = single.CreateShopItemStream(1, state);
                    for (int slot = 0; slot < Slots; slot++)
                    {
                        int scalar = single.GetNextShopItem(ref scalarShop).Value;
                        int simd = vector[slot][single.VectorLane];
                        if (simd == scalar)
                            continue;
                        same = false;
                        lock (sink)
                            sink.Add($"{single.GetSeed()} slot {slot}: simd={simd:X8} scalar={scalar:X8}");
                    }
                    return same ? 1 : 0;
                });
            }
        }
    }

    [Theory]
    [InlineData(MotelyDeck.Red, new MotelyVoucher[0])]
    [InlineData(MotelyDeck.Red, new[] { MotelyVoucher.TarotMerchant })]
    [InlineData(MotelyDeck.Red, new[] { MotelyVoucher.TarotMerchant, MotelyVoucher.TarotTycoon })]
    [InlineData(MotelyDeck.Red, new[] { MotelyVoucher.PlanetMerchant, MotelyVoucher.PlanetTycoon })]
    [InlineData(MotelyDeck.Red, new[] { MotelyVoucher.MagicTrick })]
    [InlineData(MotelyDeck.Ghost, new[] { MotelyVoucher.MagicTrick, MotelyVoucher.TarotTycoon })]
    [InlineData(MotelyDeck.Zodiac, new[] { MotelyVoucher.MagicTrick })]
    public void SimdShop_RollsTheSameShopAsScalar_UnderHeldVouchers(MotelyDeck deck, MotelyVoucher[] held)
    {
        var mismatches = new List<string>();
        using var search = new MotelySearchSettings<ShopParityDesc.ShopParityFilter>(
            new ShopParityDesc(held, mismatches)
        )
            .WithDeck(deck)
            .WithStake(MotelyStake.White)
            .WithSeedGenerator(LaneSeeds, LaneSeeds.Length)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .Start();
        search.AwaitCompletion();

        Assert.True(mismatches.Count == 0, string.Join("\n", mismatches.Take(40)));
        Assert.Equal(LaneSeeds.Length, search.MatchingSeeds);
    }

    /// <summary>
    /// The same clause as a <c>must</c> (the SIMD filter, then its confirm) and as a
    /// <c>should</c> (the scoring count): the seeds the must keeps are the seeds whose count
    /// reaches <c>min</c>.
    /// </summary>
    [Theory]
    [InlineData("", 2, 2)]
    [InlineData("    with: { vouchers: [TarotMerchant] }\n", 2, 3)]
    [InlineData("    with: { vouchers: [TarotMerchant, TarotTycoon] }\n", 3, 7)]
    public void MustAndShould_AgreeOnTheShopTheVouchersBuild(string with, int min, int seedsKept)
    {
        string clause =
            $"  - tarotCard: any\n    antes: [1]\n    min: {min}\n    sources: {{ shopItems: [0,1,2,3,4,5,6,7] }}\n{with}";

        var counts = new Dictionary<string, int>();
        using (var scored = MotelySearchBuilder
            .CreateSettings(JamlConfig.FromJaml($"deck: Red\nshould:\n{clause}"))
            .WithSeedGenerator(LaneSeeds, LaneSeeds.Length)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithScoredResultCallback(r => counts[r.Seed] = r.GetTally(0))
            .Start())
            scored.AwaitCompletion();
        Assert.Equal(LaneSeeds.Length, counts.Count);

        var kept = new HashSet<string>();
        using (var filtered = MotelySearchBuilder
            .CreateSettings(JamlConfig.FromJaml($"deck: Red\nmust:\n{clause}"))
            .WithSeedGenerator(LaneSeeds, LaneSeeds.Length)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithSeedMatchCallback(s => kept.Add(s))
            .Start())
            filtered.AwaitCompletion();

        var expected = counts.Where(c => c.Value >= min).Select(c => c.Key).ToHashSet();
        Assert.Equal(expected.Order(), kept.Order());
        // Some of the eight, never all or none: the vouchers decide which.
        Assert.Equal(seedsKept, kept.Count);
    }

    // 256 eight-character seeds: 32 full vector batches.
    private static readonly string[] ManySeeds =
    [
        .. Enumerable.Range(0, 256).Select(i =>
        {
            const string digits = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return $"WITH{digits[i / 35 % 35]}{digits[i % 35]}{digits[i * 7 % 35]}{digits[i * 11 % 35]}";
        }),
    ];

    private static Dictionary<string, int> ShouldTallies(string deck, string clause)
    {
        var counts = new Dictionary<string, int>();
        using var search = MotelySearchBuilder
            .CreateSettings(JamlConfig.FromJaml($"deck: {deck}\nshould:\n{clause}"))
            .WithSeedGenerator(ManySeeds, ManySeeds.Length)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithScoredResultCallback(r => counts[r.Seed] = r.GetTally(0))
            .Start();
        search.AwaitCompletion();
        Assert.Equal(ManySeeds.Length, counts.Count);
        return counts;
    }

    /// <summary>
    /// Every clause whose SIMD filter rolls the shop, under vouchers that move its cards: the
    /// seeds the <c>must</c> keeps (SIMD filter first) are exactly the seeds the scoring count
    /// says reach <c>min</c>. A filter rolling the wrong shop drops seeds scoring would keep.
    /// </summary>
    [Theory]
    [InlineData("Red", "tarotCard: any", "TarotMerchant, TarotTycoon")]
    [InlineData("Red", "planetCard: any", "PlanetMerchant, PlanetTycoon")]
    [InlineData("Red", "commonJoker: any", "TarotMerchant, TarotTycoon")]
    [InlineData("Red", "uncommonJoker: any", "PlanetMerchant, PlanetTycoon")]
    [InlineData("Red", "rareJoker: any", "TarotMerchant, TarotTycoon")]
    [InlineData(
        "Red",
        "joker: [Joker, JollyJoker, ZanyJoker, MadJoker, CrazyJoker, DrollJoker, HalfJoker, Banner, MysticSummit, GreenJoker, Splash, AbstractJoker]",
        "PlanetMerchant, PlanetTycoon"
    )]
    [InlineData("Ghost", "spectralCard: any", "TarotMerchant, TarotTycoon")]
    public void EveryShopFilter_KeepsTheSeedsItsVouchersShopScores(string deck, string key, string vouchers)
    {
        string body = $"    antes: [1, 2]\n    sources: {{ shopItems: [0,1,2,3,4,5,6,7] }}\n";
        string with = $"    with: {{ vouchers: [{vouchers}] }}\n";

        var bare = ShouldTallies(deck, $"  - {key}\n{body}");
        var held = ShouldTallies(deck, $"  - {key}\n{body}{with}");
        Assert.Contains(ManySeeds, s => bare[s] != held[s]);

        int min = Math.Max(1, held.Values.Order().ElementAt(held.Count / 2));
        var expected = held.Where(c => c.Value >= min).Select(c => c.Key).ToHashSet();
        Assert.NotEmpty(expected);

        var kept = new HashSet<string>();
        using (var filtered = MotelySearchBuilder
            .CreateSettings(JamlConfig.FromJaml($"deck: {deck}\nmust:\n  - {key}\n    min: {min}\n{body}{with}"))
            .WithSeedGenerator(ManySeeds, ManySeeds.Length)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithSeedMatchCallback(s => kept.Add(s))
            .Start())
            filtered.AwaitCompletion();

        Assert.Equal(expected.Order(), kept.Order());
    }

    [Fact]
    public void With_WrittenAsNull_FailsToLoad()
    {
        // jaml/index.d.ts and the schema type `with` as a mapping only, never null.
        Assert.False(JamlConfig.TryLoad("must:\n  - joker: Blueprint\n    with: ~\n", out _, out var error));
        Assert.Contains("with: needs a mapping", error);
    }

    [Fact]
    public void UnknownVoucherName_FailsToLoad()
    {
        Assert.False(JamlConfig.TryLoad(
            "must:\n  - joker: Blueprint\n    with: { vouchers: [NotAVoucher] }\n", out _, out var error));
        Assert.Contains("NotAVoucher", error);
    }
}
