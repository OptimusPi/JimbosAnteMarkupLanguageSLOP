using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public struct MotelySinglePrngStream(double state)
{
    public static MotelySinglePrngStream Invalid
    {
        get { return new(-1); }
    }

    public double State = state;
    public readonly bool IsInvalid
    {
        get { return State < 0; }
    }
}

public struct MotelySingleResampleStream(MotelySinglePrngStream initialPrngStream, bool isCached)
{
    public static MotelySingleResampleStream Invalid
    {
        get { return new(MotelySinglePrngStream.Invalid, false); }
    }

    // The vector stream's inline array is copied lane by lane into this one (CreateSingleStream),
    // so the two must hold the same number of resamples.
    public const int StackResampleCount = MotelyVectorResampleStream.StackResampleCount;

    [InlineArray(StackResampleCount)]
    public struct MotelyResampleStreams
    {
        public MotelySinglePrngStream PrngStream;
    }

    public MotelySinglePrngStream InitialPrngStream = initialPrngStream;
    public MotelyResampleStreams ResamplePrngStreams;
    public int ResamplePrngStreamInitCount;

    /// <summary>Resamples past the inline array, indexed from <see cref="StackResampleCount"/>.</summary>
    public List<StrongBox<MotelySinglePrngStream>>? HighResamplePrngStreams;
    public bool IsCached = isCached;

    public readonly bool IsInvalid
    {
        get { return InitialPrngStream.IsInvalid; }
    }

    /// <summary>
    /// The stream's position: the first draw's state and every resample stream opened so far.
    /// Setting it on a stream created with the same key continues that stream exactly.
    /// </summary>
    public MotelyResampleStreamState State
    {
        readonly get
        {
            int high = HighResamplePrngStreams?.Count ?? 0;
            var resamples = new double[ResamplePrngStreamInitCount + high];
            for (int i = 0; i < ResamplePrngStreamInitCount; i++)
                resamples[i] = ResamplePrngStreams[i].State;
            for (int i = 0; i < high; i++)
                resamples[ResamplePrngStreamInitCount + i] = HighResamplePrngStreams![i].Value.State;
            return new(InitialPrngStream.State, resamples);
        }
        set
        {
            InitialPrngStream.State = value.Initial;
            int stack = Math.Min(value.Resamples.Count, StackResampleCount);
            for (int i = 0; i < stack; i++)
                ResamplePrngStreams[i] = new(value.Resamples[i]);
            ResamplePrngStreamInitCount = stack;
            HighResamplePrngStreams = null;
            for (int i = stack; i < value.Resamples.Count; i++)
                (HighResamplePrngStreams ??= []).Add(new(new(value.Resamples[i])));
        }
    }
}

public unsafe partial class MotelySingleSearchContext
{
    public readonly int VectorLane;

    private readonly MotelySearchParameters _searchParameters;
    private readonly MotelySearchContextParams _contextParams;

    public MotelyStake Stake
    {
        get { return _searchParameters.Stake; }
    }
    public MotelyDeck Deck
    {
        get { return _searchParameters.Deck; }
    }

    internal MotelySearchParameters SearchParameters
    {
        get { return _searchParameters; }
    }
    internal MotelySearchContextParams SearchContextParams
    {
        get { return _contextParams; }
    }

    private PartialSeedHashCache* SeedHashCache
    {
        get { return _contextParams.SeedHashCache; }
    }
    private int SeedLength
    {
        get { return _contextParams.SeedLength; }
    }
    private int SeedFirstCharactersLength
    {
        get { return _contextParams.SeedFirstCharactersLength; }
    }
    private int SeedLastCharactersLength
    {
        get { return _contextParams.SeedLastCharactersLength; }
    }
    private char* SeedFirstCharacters
    {
        get { return _contextParams.SeedFirstCharacters; }
    }
    private Vector512<double>* SeedLastCharacters
    {
        get { return _contextParams.SeedLastCharacters; }
    }
    private bool IsAdditionalFilter
    {
        get { return _contextParams.IsAdditionalFilter; }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal MotelySingleSearchContext(
        in MotelySearchParameters searchParameters,
        in MotelySearchContextParams contextParams,
        int lane
    )
    {
        _contextParams = contextParams;
        _searchParameters = searchParameters;
        VectorLane = lane;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetSeed()
    {
        return _contextParams.GetSeed(VectorLane);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetSeed(char* output)
    {
        return _contextParams.GetSeed(VectorLane, output);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double PseudoHash(string key, bool isCached = false)
    {
        double partialHash;

        // Same law as the vector context: an additional-filter context never trusts the
        // length-keyed partial-hash cache — it was filled for the base filter's key set.
        // isCached is only a hint; the cache is always asked (see the vector context).
        _ = isCached;
        if (!IsAdditionalFilter && SeedHashCache->HasPartialHash(key.Length))
        {
            partialHash = SeedHashCache->GetPartialHash(key.Length, VectorLane);
        }
        else
        {
            partialHash = InternalPseudoHashSeed(key.Length);
        }

        return InternalPseudoHashKey(key, partialHash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double InternalPseudoHashKey(string key, double partialHash)
    {
        double num = partialHash;

        for (int i = key.Length - 1; i >= 0; i--)
        {
            double x = 1.1239285023 / num * key[i] * Math.PI + (i + 1) * Math.PI;
            num = x - Math.Floor(x);
        }

        return num;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double InternalPseudoHashSeed(int keyLength)
    {
        int seedLastCharacterLength = SeedLastCharactersLength;
        double num = 1;

        // First we do the first characters of the seed which are the same between all vector lanes
        for (int i = SeedFirstCharactersLength - 1; i >= 0; i--)
        {
            double x =
                1.1239285023 / num * SeedFirstCharacters[i] * Math.PI
                + Math.PI * (i + keyLength + seedLastCharacterLength + 1);
            num = x - Math.Floor(x);
        }

        // Then we get the characters for our lane
        for (int i = seedLastCharacterLength - 1; i >= 0; i--)
        {
            double x =
                1.1239285023 / num * SeedLastCharacters[i][VectorLane] * Math.PI
                + Math.PI * (keyLength + i + 1);
            num = x - Math.Floor(x);
        }

        return num;
    }

    private const double PrngRoundingFactor = 1e13;
    private const double PrngMagicNumber = 4503599627370496.0; // 2^52

    // The scalar twin of MotelyVectorSearchContext.IteratePRNG, step for step: multiply, add,
    // take the fraction, round to 13 decimals with one FMA against 2^52, and clear the sign bit
    // (Balatro's math.abs in pseudoseed, which turns -QNaN into +QNaN = 0x7FF8000000000000).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double IteratePRNG(double state)
    {
        state = state * 1.72431234 + 2.134453429141;
        state -= Math.Floor(state);
        state = Math.FusedMultiplyAdd(state, PrngRoundingFactor, PrngMagicNumber) - PrngMagicNumber;
        double res = state / PrngRoundingFactor;
        return BitConverter.UInt64BitsToDouble(
            BitConverter.DoubleToUInt64Bits(res) & 0x7FFFFFFFFFFFFFFFUL
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelySinglePrngStream CreatePrngStream(string key, bool isCached = false)
    {
        return new(PseudoHash(key, isCached));
    }

    /// <summary>
    /// Resume a PRNG stream from a previously-captured <see cref="MotelySinglePrngStream.State"/>.
    /// Every Motely stream is, at bottom, an infinite PRNG stream whose entire position is one
    /// <c>double</c> — so this single method re-seats any of them exactly where it left off (the
    /// key is only ever used to compute the *initial* state in <see cref="CreatePrngStream"/>;
    /// after that, State carries everything forward).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelySinglePrngStream ResumeStream(double state) => new(state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetNextPrngState(ref MotelySinglePrngStream stream)
    {
        Debug.Assert(!stream.IsInvalid, "Invalid stream.");
        return stream.State = IteratePRNG(stream.State);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetNextPseudoSeed(ref MotelySinglePrngStream stream)
    {
        return (GetNextPrngState(ref stream) + SeedHashCache->GetSeedHash(VectorLane)) / 2d;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaRandom GetNextLuaRandom(ref MotelySinglePrngStream stream)
    {
        return new LuaRandom(GetNextPseudoSeed(ref stream));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetNextRandom(ref MotelySinglePrngStream stream)
    {
        return LuaRandom.Random(GetNextPseudoSeed(ref stream));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetNextRandomInt(ref MotelySinglePrngStream stream, int min, int max)
    {
        return LuaRandom.RandInt(GetNextPseudoSeed(ref stream), min, max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetNextRandomElement<T>(ref MotelySinglePrngStream stream, T[] choices)
    {
        return choices[GetNextRandomInt(ref stream, 0, choices.Length)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private MotelySingleResampleStream CreateResampleStream(string key, bool isCached)
    {
        return new(CreatePrngStream(key, isCached), isCached);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private MotelySinglePrngStream CreateResamplePrngStream(string key, int resample, bool isCached)
    {
        // We don't cache resamples >= 8 because they'd use an extra digit
        if (isCached && resample >= 8)
            isCached = false;
        return CreatePrngStream(key + MotelyPrngKeys.Resample + (resample + 2), isCached);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref MotelySinglePrngStream GetResamplePrngStream(
        ref MotelySingleResampleStream resampleStream,
        string key,
        int resample
    )
    {
        if (resample < MotelySingleResampleStream.StackResampleCount)
        {
            ref MotelySinglePrngStream prngStream = ref resampleStream.ResamplePrngStreams[
                resample
            ];

            if (resample == resampleStream.ResamplePrngStreamInitCount)
            {
                ++resampleStream.ResamplePrngStreamInitCount;
                prngStream = CreateResamplePrngStream(key, resample, resampleStream.IsCached);
            }

            return ref prngStream;
        }

        // Past the inline array the streams live on the heap, kept for the stream's whole life:
        // Balatro re-reads a resample stream it has seen before rather than re-seeding it.
        var high = resampleStream.HighResamplePrngStreams ??= [];
        int index = resample - MotelySingleResampleStream.StackResampleCount;
        if (index < high.Count)
            return ref high[index].Value;

        Debug.Assert(index == high.Count, "Resample streams are created in order.");
        var box = new StrongBox<MotelySinglePrngStream>(
            CreateResamplePrngStream(key, resample, resampleStream.IsCached)
        );
        high.Add(box);
        return ref box.Value;
    }
}
