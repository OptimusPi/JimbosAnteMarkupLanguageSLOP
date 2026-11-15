using Motely.Filters;
using Xunit;

namespace Motely.Tests;

public sealed class ChallengerEngineParityTests
{
    [Theory]
    [InlineData("Foil")]
    [InlineData("Holographic")]
    [InlineData("Polychrome")]
    [InlineData("Negative")]
    public async Task ShopJoker_all_editions_have_bit_exact_simd_scalar_parity(string edition)
    {
        string jaml = $"""
            name: ShopJoker_{edition}
            deck: Red
            stake: White
            must:
              - rareJoker: []
                edition: {edition}
                antes: [1, 2]
            """;

        var config = JamlConfig.FromJaml(jaml);

        var simdMatches = await SweepSequentialAsync(config, batchCharCount: 2, startBatch: 0, endBatch: 4);
        var scalarMatches = await SweepScalarAsync(config, batchCharCount: 2, startBatch: 0, endBatch: 4);

        Assert.Equal(scalarMatches, simdMatches);
    }

    [Theory]
    [InlineData("Common")]
    [InlineData("Uncommon")]
    [InlineData("Rare")]
    public async Task ShopJoker_all_rarities_have_bit_exact_simd_scalar_parity(string rarity)
    {
        string tag = rarity switch
        {
            "Common" => "commonJoker",
            "Uncommon" => "uncommonJoker",
            "Rare" => "rareJoker",
            _ => throw new ArgumentOutOfRangeException(nameof(rarity))
        };

        string jaml = $"""
            name: ShopJokerRarity_{rarity}
            deck: Red
            stake: White
            must:
              - {tag}: []
                edition: Foil
                antes: [1]
            """;

        var config = JamlConfig.FromJaml(jaml);

        var simdMatches = await SweepSequentialAsync(config, batchCharCount: 2, startBatch: 0, endBatch: 4);
        var scalarMatches = await SweepScalarAsync(config, batchCharCount: 2, startBatch: 0, endBatch: 4);

        Assert.NotEmpty(simdMatches);
        Assert.Equal(scalarMatches, simdMatches);
    }

    [Theory]
    [InlineData("Foil")]
    [InlineData("Holographic")]
    [InlineData("Polychrome")]
    [InlineData("Negative")]
    public async Task BuffoonJoker_all_editions_have_bit_exact_simd_scalar_parity(string edition)
    {
        string jaml = $"""
            name: BuffoonJoker_{edition}
            deck: Red
            stake: White
            must:
              - uncommonJoker: []
                edition: {edition}
                antes: [1, 2]
                sources:
                  boosterPacks: [0, 1]
            """;

        var config = JamlConfig.FromJaml(jaml);

        var simdMatches = await SweepSequentialAsync(config, batchCharCount: 2, startBatch: 0, endBatch: 4);
        var scalarMatches = await SweepScalarAsync(config, batchCharCount: 2, startBatch: 0, endBatch: 4);

        Assert.Equal(scalarMatches, simdMatches);
    }

    [Fact]
    public async Task High_ante_numbers_ante8_sweep_bit_exact_parity()
    {
        string jaml = """
            name: ShopJoker_Ante8
            deck: Red
            stake: White
            must:
              - uncommonJoker: []
                edition: Foil
                antes: [8]
            """;

        var config = JamlConfig.FromJaml(jaml);

        var simdMatches = await SweepSequentialAsync(config, batchCharCount: 2, startBatch: 0, endBatch: 4);
        var scalarMatches = await SweepScalarAsync(config, batchCharCount: 2, startBatch: 0, endBatch: 4);

        Assert.Equal(scalarMatches, simdMatches);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(16)]
    public async Task High_ante_numbers_ante12_and_16_simd_scalar_parity(int ante)
    {
        string jaml = $"""
            name: ShopJoker_Ante{ante}
            deck: Red
            stake: White
            must:
              - rareJoker: []
                edition: Foil
                antes: [{ante}]
            """;

        var config = JamlConfig.FromJaml(jaml);

        var simdMatches = await SweepSequentialAsync(config, batchCharCount: 2, startBatch: 0, endBatch: 2);
        var scalarMatches = await SweepScalarAsync(config, batchCharCount: 2, startBatch: 0, endBatch: 2);

        Assert.Equal(scalarMatches, simdMatches);
    }

    [Fact]
    public async Task Direct_Empirical_Verification_zero_false_negatives_shop_and_buffoon()
    {
        string jaml = """
            name: DirectZeroFalseNegativeCheck
            deck: Red
            stake: White
            must:
              - rareJoker: []
                edition: Foil
                antes: [1, 2]
            """;

        var config = JamlConfig.FromJaml(jaml);

        var simdMatches = await SweepSequentialAsync(config, batchCharCount: 2, startBatch: 0, endBatch: 16);
        Assert.NotEmpty(simdMatches);

        var scalarMatches = await SweepScalarAsync(config, batchCharCount: 2, startBatch: 0, endBatch: 16);

        var simdSeedSet = new HashSet<string>(simdMatches.Select(m => m.Seed));
        var scalarSeedSet = new HashSet<string>(scalarMatches.Select(m => m.Seed));

        Assert.Empty(scalarSeedSet.Except(simdSeedSet));
        Assert.Empty(simdSeedSet.Except(scalarSeedSet));
        Assert.Equal(scalarMatches, simdMatches);
    }

    private static List<string> EnumerateBatchSeeds(int batchCharCount, long startBatch, long endBatch)
    {
        if (batchCharCount != 2)
            throw new ArgumentOutOfRangeException(nameof(batchCharCount));

        var digits = MotelyGlobals.SeedDigits;
        var seeds = new List<string>((int)(endBatch - startBatch) * digits.Length * digits.Length);
        Span<char> buf = stackalloc char[MotelyGlobals.MaxSeedLength];

        for (long batchIdx = startBatch; batchIdx < endBatch; batchIdx++)
        {
            SeedMath.BatchIndexToFirstSeed(batchIdx, batchCharCount).AsSpan().CopyTo(buf);

            for (int a = 0; a < digits.Length; a++)
            {
                buf[0] = digits[a];
                for (int b = 0; b < digits.Length; b++)
                {
                    buf[1] = digits[b];
                    seeds.Add(new string(buf));
                }
            }
        }

        return seeds;
    }

    private static Task<List<(string Seed, int Score)>> SweepSequentialAsync(
        JamlConfig config,
        int batchCharCount,
        long startBatch,
        long endBatch
    )
    {
        var plan = MotelySearchBuilder.CreatePlan(config, int.MinValue);
        var settings = plan
            .Settings.WithThreadCount(Environment.ProcessorCount)
            .WithBatchCharacterCount(batchCharCount)
            .WithSequentialSearch()
            .WithStartBatchIndex(startBatch)
            .WithEndBatchIndex(endBatch)
            .WithAutoScoreCutoff(false)
            .WithQuietMode(true);
        return RunSearchAsync(settings);
    }

    private static Task<List<(string Seed, int Score)>> SweepScalarAsync(
        JamlConfig config,
        int batchCharCount,
        long startBatch,
        long endBatch
    )
    {
        var seeds = EnumerateBatchSeeds(batchCharCount, startBatch, endBatch);
        var plan = MotelySearchBuilder.CreatePlan(config, int.MinValue);
        var settings = plan
            .Settings.WithSeedList([.. seeds])
            .WithThreadCount(Environment.ProcessorCount)
            .WithAutoScoreCutoff(false)
            .WithQuietMode(true);
        return RunSearchAsync(settings);
    }

    private static async Task<List<(string Seed, int Score)>> RunSearchAsync(IMotelySearchSettings settings)
    {
        var found = new List<(string Seed, int Score)>();

        if (settings.SeedScoreDesc is not null)
        {
            settings = settings.WithScoredResultCallback(r =>
            {
                lock (found)
                {
                    found.Add((r.Seed, r.Score));
                }
            });
        }
        else
        {
            settings = settings.WithSeedMatchCallback(s =>
            {
                lock (found)
                {
                    found.Add((s, 0));
                }
            });
        }

        using var search = settings.Start();
        await search.WaitForCompletionAsync();

        found.Sort((a, b) =>
        {
            int cmp = string.CompareOrdinal(a.Seed, b.Seed);
            return cmp != 0 ? cmp : a.Score.CompareTo(b.Score);
        });
        return found;
    }
}
