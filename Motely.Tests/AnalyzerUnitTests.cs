using System.Text;
using Motely.Analysis;

namespace Motely.Tests;

public sealed class AnalyzerUnitTests
{
    [Theory]
    [InlineData("1234567")]
    [InlineData("12345678")]
    [InlineData("ALEEB")]
    [InlineData("ALEEBOOO")]
    [InlineData("UNITTES")]
    [InlineData("UNITTEST")]
    [InlineData("KK1XD111", MotelyDeck.Ghost, MotelyStake.Black)]
    public async Task TestAnalyzer(string seed, MotelyDeck deck = MotelyDeck.Red, MotelyStake stake = MotelyStake.White)
    {
        await Verify(Format(Analyze(seed, deck, stake)))
            .UseFileName(seed)
            .UseDirectory("seeds");
    }

    [Fact]
    public void TestAnalyzer_ZeroLengthSeed_Succeeds()
    {
        var result = Analyze("");
        var ante1 = result.Antes.Single(a => a.Ante == 1);
        Assert.Equal(MotelyTag.CouponTag, ante1.SmallBlindTag);
        Assert.Equal(MotelyTag.RareTag, ante1.BigBlindTag);
    }

    [Fact]
    public void TestAnalyzer_Ante1_HasFourPacks()
    {
        Assert.Equal(4, Analyze("UNITTEST").Antes.Single(a => a.Ante == 1).Packs.Count);
    }

    private static MotelyJamlyzerSeedResult Analyze(
        string seed,
        MotelyDeck deck = MotelyDeck.Red,
        MotelyStake stake = MotelyStake.White
    )
    {
        var config = new JamlConfig { Deck = deck, Stake = stake };
        config.Seeds.Add(seed);
        return Assert.Single(MotelyJamlyzer.Analyze(config));
    }

    private static string Format(MotelyJamlyzerSeedResult result)
    {
        var sb = new StringBuilder();
        foreach (var ante in result.Antes)
        {
            sb.AppendLine($"==ANTE {ante.Ante}==");
            sb.AppendLine($"Boss: {ante.Boss}");
            sb.AppendLine($"Voucher: {ante.Voucher}");
            sb.AppendLine($"Tags: {ante.SmallBlindTag}, {ante.BigBlindTag}");
            sb.AppendLine("Shop Queue: ");
            for (int i = 0; i < ante.ShopItems.Count; i++)
                sb.AppendLine($"{i + 1}) {ante.ShopItems[i]}");
            sb.AppendLine();
            sb.AppendLine("Packs: ");
            foreach (var pack in ante.Packs)
                sb.AppendLine($"{pack.Pack} - {string.Join(", ", pack.Items)}");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
