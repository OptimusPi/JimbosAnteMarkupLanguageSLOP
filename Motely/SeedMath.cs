using System;
using System.Collections.Generic;

namespace Motely;

/// <summary>
/// Helper for Balatro seed math (Bijective Base-35 conversion)
/// Characters: 123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ (No 0)
///
/// Bijective notation:
/// "" = 0
/// "1" = 1
/// "9" = 9
/// "A" = 10
/// ...
/// "Z" = 35
/// "11" = 36
///
/// Standard Balatro seeds are 8 characters.
/// In the global bijective space, 8-character seeds occupy indices:
/// StartOffset8 = (35^8 - 1) / 34 = 66,231,629,136
/// EndOffset8 = (35^9 - 1) / 34 = 2,318,107,019,761
/// </summary>
public static class SeedMath
{
    private const string CHARS = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly Dictionary<char, int> CharToVal = new();

    static SeedMath()
    {
        for (int i = 0; i < CHARS.Length; i++)
        {
            CharToVal[CHARS[i]] = i;
        }
    }

    /// <summary>
    /// Global Bijective Index. "" = 0, "1" = 1, "11" = 36.
    /// </summary>
    public static long SeedToTotalIndex(string seed)
    {
        seed = seed.ToUpperInvariant();
        long total = 0;
        long multiplier = 1;

        for (int i = seed.Length - 1; i >= 0; i--)
        {
            char c = seed[i];
            if (!CharToVal.TryGetValue(c, out int val))
                throw new ArgumentException($"Invalid seed character: {c}");

            // val is 0-34. Bijective uses 1-35.
            total += (val + 1) * multiplier;
            multiplier *= 35;
        }
        return total;
    }

    public static string TotalIndexToSeed(long index)
    {
        if (index == 0)
            return "";
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        char[] buffer = new char[15];
        int pos = 15;
        long current = index;

        while (current > 0)
        {
            current--; // Bijective shift
            int val = (int)(current % 35);
            buffer[--pos] = CHARS[val];
            current /= 35;
        }

        return new string(buffer, pos, 15 - pos);
    }

    /// <summary>
    /// Motely Search Index: 0-based index relative to the start of the space of a specific length.
    /// e.g. For length 8, SearchIndex 0 corresponds to "11111111".
    /// </summary>
    public static long SeedToSearchIndex(string seed)
    {
        return SeedToTotalIndex(seed) - GetFirstSeedOfLength(seed.Length);
    }

    public static string SearchIndexToSeed(long index, int length)
    {
        return TotalIndexToSeed(index + GetFirstSeedOfLength(length));
    }

    /// <summary>
    /// (35^L - 1) / 34. The first bijective index for a string of length L.
    /// </summary>
    public static long GetFirstSeedOfLength(int length)
    {
        if (length <= 0)
            return 0;
        long sum = 0;
        long pow = 1;
        for (int i = 0; i < length; i++)
        {
            sum += pow;
            if (i < length - 1)
                pow *= 35;
        }
        return sum;
    }

    /// <summary>
    /// Delegates to <see cref="SearchIndexToSeed"/>: <paramref name="index"/> is relative to the start of the length-<paramref name="length"/> space (Balatro seeds are 8 chars).
    /// </summary>
    public static string TotalIndexToSeed(long index, int length) =>
        SearchIndexToSeed(index, length);

    public static long SeedToBatchIndex(string seed, int batchSize)
    {
        ArgumentNullException.ThrowIfNull(seed);
        if (batchSize is < 1 or >= MotelyGlobals.MaxSeedLength)
            throw new ArgumentOutOfRangeException(nameof(batchSize));

        seed = MotelyGlobals.NormalizeSeed(seed);
        if (seed.Length != MotelyGlobals.MaxSeedLength)
            throw new ArgumentException(
                $"Seed '{seed}' must be {MotelyGlobals.MaxSeedLength} characters long.",
                nameof(seed)
            );

        int prefixLen = MotelyGlobals.MaxSeedLength - batchSize;
        if (prefixLen <= 0)
            return 0;

        // The engine writes batch digits into the LAST prefixLen characters, least significant
        // first — MotelySearch.SearchSequentialBatch, _digits[MaxSeedLength - i - 1].
        long index = 0;
        for (int i = seed.Length - 1; i >= batchSize; i--)
        {
            int digit = MotelyGlobals.SeedDigits.IndexOf(seed[i]);
            if (digit < 0)
                throw new ArgumentException($"Invalid seed character: '{seed[i]}'.", nameof(seed));
            index = index * MotelyGlobals.SeedDigits.Length + digit;
        }
        return index;
    }

    public static string BatchIndexToSeedPrefix(long batchIndex, int batchSize)
    {
        int prefixLen = 8 - batchSize;
        if (prefixLen <= 0)
            return "";
        // index is treated as relative
        return SearchIndexToSeed(batchIndex, prefixLen);
    }

    /// <summary>
    /// The lowest seed inside a batch — what <c>--startSeed</c> must be given to resume there.
    /// <para>
    /// The batch digits do not sit at the head of the seed and are not in reading order.
    /// <see cref="BatchIndexToSeedPrefix"/> returns them most-significant-first, while the engine
    /// writes them into the <em>tail</em> most-significant-<em>last</em> —
    /// <c>_digits[MaxSeedLength - i - 1]</c>, so <c>seed[7]</c> carries the high digit and
    /// <c>seed[batchSize]</c> the low one. That is also what <see cref="SeedToBatchIndex"/> reads
    /// back, so this is the only assembly the pair round-trips: batch 409387 at
    /// <c>batchSize = 4</c> is <c>1111S7KA</c>, not <c>AK7S1111</c> — and the latter, read off the
    /// tail, is batch 0, which silently restarts a resumed search from the beginning of the space.
    /// </para>
    /// </summary>
    /// <param name="batchIndex">The batch to resume at, as printed by the search.</param>
    /// <param name="batchSize">Characters varying <em>within</em> a batch — the remaining
    /// <c>8 - batchSize</c> are the batch digits.</param>
    public static string BatchIndexToFirstSeed(long batchIndex, int batchSize)
    {
        if (batchSize is < 1 or > 7)
            throw new ArgumentOutOfRangeException(nameof(batchSize));

        string digits = BatchIndexToSeedPrefix(batchIndex, batchSize);
        if (digits.Length != 8 - batchSize)
            throw new ArgumentOutOfRangeException(
                nameof(batchIndex),
                $"Batch {batchIndex} is outside the {Math.Pow(35, 8 - batchSize):N0} batches of a {batchSize}-character batch."
            );

        char[] seed = new char[8];
        for (int i = 0; i < batchSize; i++)
            seed[i] = MotelyGlobals.SeedDigits[0];
        for (int i = 0; i < digits.Length; i++)
            seed[7 - i] = digits[i];
        return new string(seed);
    }

    /// <summary>
    /// Largest Motely search index for a fixed seed length (inclusive). For length 8 this is 35^8 − 1; for length 0 ("") it is 0.
    /// </summary>
    public static long MaxSearchIndexInclusive(int length)
    {
        if (length < 0)
            return -1;
        long p = 1;
        for (int i = 0; i < length; i++)
            checked
            {
                p *= 35;
            }
        return p - 1;
    }

    /// <summary>
    /// Maps an inclusive Motely search-index range for 8-character seeds to the minimal contiguous
    /// sequential batch range <c>[StartBatchIndex, EndBatchIndexExclusive)</c> covering every seed in
    /// <c>[startSearchIndexInclusive, stopSearchIndexInclusive]</c>.
    /// <para>
    /// <see cref="SeedToSearchIndex"/> is big-endian (<c>seed[0]</c> is MSB, <c>seed[7]</c> is LSB),
    /// whereas <see cref="SeedToBatchIndex"/> / <see cref="BatchIndexToFirstSeed"/> place the
    /// <c>8 - batchCharCount</c> batch digits in the tail reversed (<c>seed[7]</c> is MSB of the batch
    /// index, <c>seed[batchCharCount]</c> is LSB, and <c>seed[0..batchCharCount-1]</c> vary within a batch).
    /// Thus a seed's batch index is the <c>(8 - batchCharCount)</c>-digit base-35 reversal of
    /// <c>searchIndex % 35^(8 - batchCharCount)</c>.
    /// </para>
    /// </summary>
    public static (long StartBatchIndex, long EndBatchIndexExclusive) SearchIndexRangeToBatchRange(
        long startSearchIndexInclusive,
        long stopSearchIndexInclusive,
        int batchCharCount
    )
    {
        if (batchCharCount is < 1 or > 7)
            throw new ArgumentOutOfRangeException(nameof(batchCharCount));

        long maxIdx = MaxSearchIndexInclusive(8);
        if (
            startSearchIndexInclusive < 0
            || stopSearchIndexInclusive < startSearchIndexInclusive
            || stopSearchIndexInclusive > maxIdx
        )
        {
            throw new ArgumentOutOfRangeException(
                $"{nameof(startSearchIndexInclusive)}..{nameof(stopSearchIndexInclusive)}",
                $"Must satisfy 0 <= start <= stop <= {maxIdx}."
            );
        }

        int batchDigits = MotelyGlobals.MaxSeedLength - batchCharCount;
        long totalBatches = 1;
        for (int i = 0; i < batchDigits; i++)
            totalBatches *= 35;

        long seedSpan = stopSearchIndexInclusive - startSearchIndexInclusive + 1;
        if (seedSpan >= totalBatches)
            return (0, totalBatches);

        long lowStart = startSearchIndexInclusive % totalBatches;
        long lowStop = stopSearchIndexInclusive % totalBatches;

        if (lowStart <= lowStop)
        {
            long minBatch = MinReversedBase35(lowStart, lowStop, batchDigits);
            long maxBatch = MaxReversedBase35(lowStart, lowStop, batchDigits);
            return (minBatch, maxBatch + 1);
        }

        // Wrapping past a multiple of totalBatches visits both totalBatches - 1 (all 'Z's) and 0 (all '1's).
        return (0, totalBatches);
    }

    private static long ReverseBase35(long value, int digits)
    {
        long reversed = 0;
        for (int i = 0; i < digits; i++)
        {
            reversed = reversed * 35 + (value % 35);
            value /= 35;
        }
        return reversed;
    }

    private static long MinReversedBase35(long lowInclusive, long highInclusive, int digits)
    {
        long best = Math.Min(
            ReverseBase35(lowInclusive, digits),
            ReverseBase35(highInclusive, digits)
        );
        long pow = 1;
        for (int k = 0; k < digits; k++)
        {
            long candidate = (lowInclusive / pow + 1) * pow;
            if (candidate <= highInclusive)
                best = Math.Min(best, ReverseBase35(candidate, digits));
            pow *= 35;
        }
        return best;
    }

    private static long MaxReversedBase35(long lowInclusive, long highInclusive, int digits)
    {
        long best = Math.Max(
            ReverseBase35(lowInclusive, digits),
            ReverseBase35(highInclusive, digits)
        );
        long pow = 1;
        for (int k = 0; k < digits; k++)
        {
            long prefix = highInclusive / pow;
            if (prefix > 0)
            {
                long candidate = prefix * pow - 1;
                if (candidate >= lowInclusive)
                    best = Math.Max(best, ReverseBase35(candidate, digits));
            }
            pow *= 35;
        }
        return best;
    }
}
