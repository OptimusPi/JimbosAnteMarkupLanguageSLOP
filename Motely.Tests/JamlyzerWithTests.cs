using Motely.Analysis;
using Motely.Filters;

namespace Motely.Tests;

/// <summary>
/// The Jamlyzer's <c>with</c> is a clause's <c>with:</c>: given the same block, what the Jamlyzer
/// shows is what the clause counts. Vouchers build the shop, luck weighs the luck events, held
/// legendaries leave the Soul pool.
/// </summary>
public sealed class JamlyzerWithTests
{
    private static MotelyJamlyzerSeedResult One(
        JamlConfig config,
        MotelyWith? with = null,
        int eventRolls = 0,
        int shopSlots = 0
    ) => Assert.Single(MotelyJamlyzer.Analyze(config, eventRolls, shopSlots, with));

    private static int ShopTarots(MotelyJamlyzerSeedResult result, int ante) =>
        result.Antes.Single(a => a.Ante == ante).ShopItems.Take(8)
            .Count(item => item.TypeCategory == MotelyItemTypeCategory.TarotCard);

    [Theory]
    [InlineData("ALEEB")]
    [InlineData("PIROCKS")]
    [InlineData("UNITTEST")]
    [InlineData("12345678")]
    [InlineData("MOTELY77")]
    [InlineData("PIFREAKS")]
    public void Shop_IsTheShopAClauseWithTheSameVouchersCounts(string seed)
    {
        var config = JamlConfig.FromJaml(
            $"deck: Red\nseeds: [{seed}]\nshould:\n  - tarotCard: any\n    antes: [1]\n"
                + "    sources: { shopItems: [0,1,2,3,4,5,6,7] }\n"
                + "    with: { vouchers: [TarotMerchant, TarotTycoon] }\n"
        );
        var with = new MotelyWith { Vouchers = [MotelyVoucher.TarotMerchant, MotelyVoucher.TarotTycoon] };

        var result = One(config, with);
        Assert.Equal(result.Tally![0], ShopTarots(result, 1));
    }

    [Fact]
    public void Shop_WithNoVouchers_IsTheSeedsOwnShop_AndVouchersChangeIt()
    {
        // PIFREAKS, Red: 3 tarots in the first eight ante-1 slots, 6 under Tarot Tycoon.
        var config = new JamlConfig { Deck = MotelyDeck.Red, Seeds = ["PIFREAKS"] };
        var with = new MotelyWith { Vouchers = [MotelyVoucher.TarotMerchant, MotelyVoucher.TarotTycoon] };

        Assert.Equal(3, ShopTarots(One(config), 1));
        Assert.Equal(3, ShopTarots(One(config, new MotelyWith()), 1));
        Assert.Equal(6, ShopTarots(One(config, with), 1));
    }

    [Fact]
    public void Souls_LeaveThePoolWhenHeld_AndRepeatUnderShowman()
    {
        // BHT11111, Ghost: the raw Joker4 sequence opens Triboulet, Triboulet.
        var config = new JamlConfig { Deck = MotelyDeck.Ghost, Seeds = ["BHT11111"] };
        static MotelyJoker[] FirstTwo(MotelyJamlyzerSeedResult r) =>
            [.. r.LegendaryOrder.Take(2).Select(l => l.Joker)];

        Assert.Equal([MotelyJoker.Triboulet, MotelyJoker.Canio], FirstTwo(One(config)));
        Assert.Equal(
            [MotelyJoker.Triboulet, MotelyJoker.Triboulet],
            FirstTwo(One(config, new MotelyWith { Jokers = [MotelyJoker.Showman] }))
        );
        Assert.DoesNotContain(
            MotelyJoker.Triboulet,
            One(config, new MotelyWith { Jokers = [MotelyJoker.Triboulet] }).LegendaryOrder.Select(l => l.Joker)
        );
    }

    [Fact]
    public void LuckEvents_AreWeighedByLuck_AsALuckClauseCountsThem()
    {
        const int Rolls = 40;
        var config = new JamlConfig { Deck = MotelyDeck.Red, Seeds = ["ALEEB"] };
        config.Should.Add(new LuckyMoneyClause
        {
            Rolls = [.. Enumerable.Range(0, Rolls)],
            With = new MotelyWith { Luck = MotelyLuck.X4 },
        });

        var plain = One(config, eventRolls: Rolls);
        var lucky = One(config, new MotelyWith { Luck = MotelyLuck.X4 }, eventRolls: Rolls);

        Assert.Equal(lucky.Tally![0], lucky.Events.LuckyMoney.Count(hit => hit));
        Assert.True(lucky.Events.LuckyMoney.Count(hit => hit) > plain.Events.LuckyMoney.Count(hit => hit));
        // A business: clause takes no with:, so its rolls stay at base odds.
        Assert.Equal(plain.Events.Business, lucky.Events.Business);
    }

    [Fact]
    public void Resume_ContinuesTheShopTheVouchersBuild()
    {
        var config = new JamlConfig { Deck = MotelyDeck.Red, Seeds = ["PIFREAKS"] };
        var with = new MotelyWith { Vouchers = [MotelyVoucher.MagicTrick, MotelyVoucher.TarotTycoon] };

        var whole = One(config, with, shopSlots: 30);
        var first = One(config, with, shopSlots: 15);
        var second = Assert.Single(MotelyJamlyzer.Analyze(config, first.StreamStates, 0, 15, with));

        for (int i = 0; i < whole.Antes.Count; i++)
            Assert.Equal(
                whole.Antes[i].ShopItems.Select(item => item.Value),
                first.Antes[i].ShopItems.Concat(second.Antes[i].ShopItems).Select(item => item.Value)
            );
    }
}
