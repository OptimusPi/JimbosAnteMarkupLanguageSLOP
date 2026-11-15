namespace Motely.Tests;

public sealed class CoverageUtilityTests
{
    [Theory]
    [InlineData("", 0)]
    [InlineData("1", 1)]
    [InlineData("9", 9)]
    [InlineData("A", 10)]
    [InlineData("Z", 35)]
    [InlineData("11", 36)]
    [InlineData("11111111", 66231629136)]
    public void SeedMath_TotalIndexRoundTrips(string seed, long index)
    {
        Assert.Equal(index, SeedMath.SeedToTotalIndex(seed));
        Assert.Equal(seed, SeedMath.TotalIndexToSeed(index));
    }

    [Theory]
    [InlineData("11111111", 0)]
    [InlineData("11111112", 1)]
    [InlineData("1111111Z", 34)]
    [InlineData("11111121", 35)]
    public void SeedMath_SearchIndexRoundTrips(string seed, long index)
    {
        Assert.Equal(index, SeedMath.SeedToSearchIndex(seed));
        Assert.Equal(seed, SeedMath.SearchIndexToSeed(index, seed.Length));
    }

    [Fact]
    public void SeedMath_BatchAndRangeHelpersUseInclusiveSearchIndices()
    {
        Assert.Equal(0, SeedMath.GetFirstSeedOfLength(0));
        Assert.Equal(1, SeedMath.GetFirstSeedOfLength(1));
        Assert.Equal(36, SeedMath.GetFirstSeedOfLength(2));
        Assert.Equal(0, SeedMath.MaxSearchIndexInclusive(0));
        Assert.Equal(35 * 35 - 1, SeedMath.MaxSearchIndexInclusive(2));

        Assert.Equal(0, SeedMath.SeedToBatchIndex("11111111", 3));
        Assert.Equal("11111", SeedMath.BatchIndexToSeedPrefix(0, 3));

        // SearchIndex 0..34 is "11111111".."1111111Z": seed[7] is the MSB of BatchIndex (35^6 at batchCharCount=1).
        long expectedMaxBatch = SeedMath.SeedToBatchIndex("1111111Z", 1);
        Assert.Equal(34L * 35 * 35 * 35 * 35 * 35 * 35, expectedMaxBatch);
        var range = SeedMath.SearchIndexRangeToBatchRange(0, 34, 1);
        Assert.Equal(0, range.StartBatchIndex);
        Assert.Equal(expectedMaxBatch + 1, range.EndBatchIndexExclusive);

        // Full space covers [0, 35^(8 - batchCharCount)).
        var fullRange = SeedMath.SearchIndexRangeToBatchRange(0, SeedMath.MaxSearchIndexInclusive(8), 4);
        Assert.Equal((0L, 35L * 35 * 35 * 35), fullRange);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void SeedMath_SearchIndexRangeToBatchRange_MatchesExactSeedToBatchIndexOracle(int batchCharCount)
    {
        long maxSearchIdx = SeedMath.MaxSearchIndexInclusive(8);
        long totalBatches = MotelyGlobals.SequentialBatchCount(batchCharCount);

        // Sample intervals covering single seeds, digit carries, wrap-arounds modulo totalBatches, and high offsets.
        (long Start, long Stop)[] intervals =
        [
            (0, 0),
            (0, 1),
            (0, 34),
            (1, 33),
            (17, 52),
            (34, 36),
            (1220, 1260),
            (409387, 409420),
            (totalBatches - 5, totalBatches + 5),
            (maxSearchIdx - 40, maxSearchIdx),
            (maxSearchIdx, maxSearchIdx),
        ];

        foreach (var (rawStart, rawStop) in intervals)
        {
            long start = Math.Clamp(rawStart, 0, maxSearchIdx);
            long stop = Math.Clamp(rawStop, start, maxSearchIdx);
            if (stop - start > 500)
                continue;

            long minBatch = long.MaxValue;
            long maxBatch = long.MinValue;
            for (long idx = start; idx <= stop; idx++)
            {
                string seed = SeedMath.SearchIndexToSeed(idx, 8);
                long batch = SeedMath.SeedToBatchIndex(seed, batchCharCount);
                if (batch < minBatch) minBatch = batch;
                if (batch > maxBatch) maxBatch = batch;
            }

            var actual = SeedMath.SearchIndexRangeToBatchRange(start, stop, batchCharCount);
            Assert.Equal(minBatch, actual.StartBatchIndex);
            Assert.Equal(maxBatch + 1, actual.EndBatchIndexExclusive);
        }
    }

    /// <summary>
    /// The two seeds a real interrupted run printed, from the batch it printed beside them. Pinned
    /// as literals because the failure they guard is silent: the old resume seed put the batch
    /// digits at the head, which reads back as batch 0, so resuming restarted the whole sweep.
    /// </summary>
    [Theory]
    [InlineData(409387, 4, "1111S7KA")]
    [InlineData(1449672, 4, "11118FTY")]
    [InlineData(44799, 4, "1111ZK22")]
    [InlineData(0, 4, "11111111")]
    public void SeedMath_BatchIndexToFirstSeed_PutsBatchDigitsInTheTail(
        long batchIndex,
        int batchCharCount,
        string expected
    )
    {
        Assert.Equal(expected, SeedMath.BatchIndexToFirstSeed(batchIndex, batchCharCount));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(7)]
    public void SeedMath_BatchIndexToFirstSeed_RoundTripsThroughSeedToBatchIndex(int batchCharCount)
    {
        long max = (long)Math.Pow(35, 8 - batchCharCount);
        foreach (
            long batch in new[] { 0L, 1L, 34L, 35L, 1234L, max / 2, max - 1 }
                .Where(b => b >= 0 && b < max)
                .Distinct()
        )
        {
            string seed = SeedMath.BatchIndexToFirstSeed(batch, batchCharCount);
            Assert.Equal(8, seed.Length);
            Assert.Equal(batch, SeedMath.SeedToBatchIndex(seed, batchCharCount));
        }
    }

    [Fact]
    public void SeedMath_BatchIndexToFirstSeed_RejectsBatchesOutsideTheSpace()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SeedMath.BatchIndexToFirstSeed(-1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SeedMath.BatchIndexToFirstSeed((long)Math.Pow(35, 4), 4)
        );
        Assert.Throws<ArgumentOutOfRangeException>(() => SeedMath.BatchIndexToFirstSeed(0, 8));
    }

    [Fact]
    public void SeedMath_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentException>(() => SeedMath.SeedToTotalIndex("10"));
        Assert.Throws<ArgumentOutOfRangeException>(() => SeedMath.TotalIndexToSeed(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SeedMath.SearchIndexRangeToBatchRange(0, 1, 0)
        );
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SeedMath.SearchIndexRangeToBatchRange(2, 1, 1)
        );
    }

    [Fact]
    public void KeywordSequences_BasicGeneratorsAreLazyAndDeterministic()
    {
        Assert.Equal("AAA", MotelySeedKeywordSequences.RepeatCharKeywords(3).First());
        Assert.Equal("ZZZ", MotelySeedKeywordSequences.RepeatCharKeywords(3).Last());
        Assert.Equal(26, MotelySeedKeywordSequences.RepeatCharKeywords(3).Count());

        Assert.Equal("1234", MotelySeedKeywordSequences.AscendingDigitLetterKeywords(4).First());
        Assert.Equal("WXYZ", MotelySeedKeywordSequences.AscendingDigitLetterKeywords(4).Last());
        Assert.Equal("ZYXW", MotelySeedKeywordSequences.DescendingDigitLetterKeywords(4).First());
        Assert.Equal("4321", MotelySeedKeywordSequences.DescendingDigitLetterKeywords(4).Last());

        var mirrors = MotelySeedKeywordSequences.MirrorPatternKeywords(2).ToArray();
        Assert.Equal(13 * 13, mirrors.Length);
        Assert.Contains("AA", mirrors);
        Assert.Contains("88", mirrors);
    }

    [Fact]
    public void KeywordSequences_AestheticCountsAndValidationArePinned()
    {
        // Each baked constant must be recomputed from its own keyword array. Comparing the
        // constant to GetAestheticSeedCount(aesthetic) is a tautology for the null-pad case —
        // that call returns the constant — so the live count is the only real pin.
        var baked = string.Join(
            " ",
            MotelySeedKeywordSequences.GrossKeywordAestheticSeedCount,
            MotelySeedKeywordSequences.FunnyKeywordAestheticSeedCount,
            MotelySeedKeywordSequences.BalatroKeywordAestheticSeedCount,
            MotelySeedKeywordSequences.LeetKeywordAestheticSeedCount,
            MotelySeedKeywordSequences.NsfwKeywordAestheticSeedCount
        );
        var live = string.Join(
            " ",
            MotelyGlobals.GetPaddedSeedCountForKeywordsLong(
                MotelySeedKeywordSequences.GrossKeywords
            ),
            MotelyGlobals.GetPaddedSeedCountForKeywordsLong(
                MotelySeedKeywordSequences.FunnyKeywords
            ),
            MotelyGlobals.GetPaddedSeedCountForKeywordsLong(
                MotelySeedKeywordSequences.BalatroKeywords
            ),
            MotelyGlobals.GetPaddedSeedCountForKeywordsLong(MotelySeedKeywordSequences.LeetKeywords),
            MotelyGlobals.GetPaddedSeedCountForKeywordsLong(MotelySeedKeywordSequences.NsfwKeywords)
        );
        Assert.Equal(baked, live);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MotelySeedKeywordSequences.GetAestheticSeedCount(MotelyAesthetic.Palindrome)
        );

        foreach (
            var keywords in new[]
            {
                MotelySeedKeywordSequences.GrossKeywords,
                MotelySeedKeywordSequences.FunnyKeywords,
                MotelySeedKeywordSequences.BalatroKeywords,
                MotelySeedKeywordSequences.NsfwKeywords,
            }
        )
        {
            Assert.NotEmpty(keywords);
            Assert.All(
                keywords,
                keyword =>
                {
                    Assert.InRange(keyword.Length, 1, 8);
                    Assert.All(
                        keyword.ToUpperInvariant(),
                        c => Assert.Contains(c, MotelyGlobals.SeedDigits)
                    );
                }
            );
        }
    }
}
