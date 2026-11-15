namespace Motely.SeedProviders;

public interface IMotelySeedProvider
{
    public long SeedCount { get; }
    public string NextSeed();

    public bool TryNextSeed(out string seed)
    {
        var single = new string[1];
        if (NextSeeds(single) > 0)
        {
            seed = single[0];
            return true;
        }
        seed = string.Empty;
        return false;
    }

    /// <summary>
    /// Batch retrieve multiple seeds in one lock operation - much faster for multi-threaded access.
    /// Fills the provided array with seed strings, returns the number of seeds actually retrieved.
    /// </summary>
    public int NextSeeds(string[] seeds);
}

public sealed class MotelyRandomSeedProvider(int seedCount) : IMotelySeedProvider
{
    // A negative count searched nothing yet reported a provider of "unknown" size.
    public long SeedCount { get; } =
        seedCount >= 0
            ? seedCount
            : throw new ArgumentOutOfRangeException(
                nameof(seedCount),
                seedCount,
                "Random seed count cannot be negative."
            );

    // long, not int: every call past the end still increments, and at seedCount = int.MaxValue
    // an int counter wrapped negative on the very next seed and never read as "done" again —
    // the provider, and the search, ran forever.
    private long _seedsGenerated;

    public bool TryNextSeed(out string seed)
    {
        if (Interlocked.Increment(ref _seedsGenerated) > SeedCount)
        {
            seed = string.Empty;
            return false;
        }

        seed = string.Create(
            MotelyGlobals.MaxSeedLength,
            (object?)null,
            static (buf, _) => Random.Shared.GetItems(MotelyGlobals.SeedDigits, buf)
        );
        return true;
    }

    public string NextSeed() => TryNextSeed(out var seed) ? seed : string.Empty;

    public int NextSeeds(string[] seeds)
    {
        if (seeds is not { Length: > 0 })
            return 0;

        int filled = 0;
        for (int i = 0; i < seeds.Length; i++)
        {
            if (Interlocked.Increment(ref _seedsGenerated) > SeedCount)
                break;

            seeds[i] = string.Create(
                MotelyGlobals.MaxSeedLength,
                (object?)null,
                static (buf, _) => Random.Shared.GetItems(MotelyGlobals.SeedDigits, buf)
            );
            filled++;
        }
        return filled;
    }
}

/// <summary>
/// One aesthetic family's seeds, generated lazily via <see cref="MotelyAesthetics.EnumerateSeeds"/>
/// (palindrome, psychosis, ...).
/// </summary>
public sealed class MotelyAestheticSeedProvider : IMotelySeedProvider
{
    public long SeedCount { get; }

    private readonly IEnumerator<string> _enumerator;
    private readonly object _enumeratorLock = new();

    public MotelyAestheticSeedProvider(MotelyAesthetic aesthetic, char[]? paddingAlphabet = null)
    {
        SeedCount = MotelyAesthetics.GetSeedCount(aesthetic, paddingAlphabet);
        _enumerator = MotelyAesthetics.EnumerateSeeds(aesthetic, paddingAlphabet).GetEnumerator();
    }

    public bool TryNextSeed(out string seed)
    {
        lock (_enumeratorLock)
        {
            if (_enumerator.MoveNext())
            {
                seed = _enumerator.Current;
                return true;
            }
            seed = string.Empty;
            return false;
        }
    }

    public string NextSeed() => TryNextSeed(out var seed) ? seed : string.Empty;

    public int NextSeeds(string[] seeds)
    {
        if (seeds == null || seeds.Length == 0)
            return 0;

        lock (_enumeratorLock)
        {
            int count = 0;
            for (int i = 0; i < seeds.Length; i++)
            {
                if (!_enumerator.MoveNext())
                    break;
                seeds[i] = _enumerator.Current;
                count++;
            }
            return count;
        }
    }
}

/// <summary>
/// Generates repeater patterns directly from their ordinal rather than through a shared iterator.
/// Provider search workers claim independent chunks, so pattern production does not serialize on
/// one enumerator lock before the SIMD search can begin.
/// </summary>
public sealed class MotelyRepeaterSeedProvider : IMotelySeedProvider
{
    // Periods 1 and 2 are period-4 tilings too, so period 4 alone lists each repeater once.
    private const int PatternLength = MotelyGlobals.MaxSeedLength / 2;
    private readonly char[] _alphabet;
    private long _nextIndex;

    public long SeedCount { get; }

    public MotelyRepeaterSeedProvider(char[]? paddingAlphabet = null)
    {
        _alphabet = MotelyAesthetics.AlphabetOrFull(paddingAlphabet);
        long c = _alphabet.Length;
        SeedCount = checked(c * c * c * c);
    }

    public bool TryNextSeed(out string seed)
    {
        long index = Interlocked.Increment(ref _nextIndex) - 1;
        if (index < SeedCount)
        {
            seed = SeedAt(index);
            return true;
        }

        seed = string.Empty;
        return false;
    }

    public string NextSeed() => TryNextSeed(out var seed) ? seed : string.Empty;

    public int NextSeeds(string[] seeds)
    {
        if (seeds is not { Length: > 0 })
            return 0;

        long start = Interlocked.Add(ref _nextIndex, seeds.Length) - seeds.Length;
        if (start >= SeedCount)
            return 0;

        int count = (int)Math.Min(seeds.Length, SeedCount - start);
        for (int i = 0; i < count; i++)
            seeds[i] = SeedAt(start + i);
        return count;
    }

    internal string SeedAt(long index)
    {
        if (index < 0 || index >= SeedCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return string.Create(
            MotelyGlobals.MaxSeedLength,
            (_alphabet, patternIndex: index),
            static (seed, state) =>
            {
                Span<char> pattern = stackalloc char[PatternLength];
                for (int i = PatternLength - 1; i >= 0; i--)
                {
                    pattern[i] = state._alphabet[(int)(state.patternIndex % state._alphabet.Length)];
                    state.patternIndex /= state._alphabet.Length;
                }
                for (int i = 0; i < seed.Length; i++)
                    seed[i] = pattern[i % pattern.Length];
            }
        );
    }
}

public sealed class MotelyKeywordSeedProvider : IMotelySeedProvider
{
    public long SeedCount { get; }

    private readonly IEnumerator<string> _enumerator;
    private readonly object _enumeratorLock = new();

    public MotelyKeywordSeedProvider(IEnumerable<string> keywords, char[]? paddingChars = null)
    {
        SeedCount = MotelyGlobals.GetPaddedSeedCountForKeywordsLong(keywords, paddingChars);
        _enumerator = MotelyGlobals
            .GeneratePaddedSeedsForKeywords(keywords, paddingChars)
            .GetEnumerator();
    }

    public bool TryNextSeed(out string seed)
    {
        lock (_enumeratorLock)
        {
            if (_enumerator.MoveNext())
            {
                seed = _enumerator.Current;
                return true;
            }
            seed = string.Empty;
            return false;
        }
    }

    public string NextSeed() => TryNextSeed(out var seed) ? seed : string.Empty;

    public int NextSeeds(string[] seeds)
    {
        if (seeds == null || seeds.Length == 0)
            return 0;

        lock (_enumeratorLock)
        {
            int count = 0;
            for (int i = 0; i < seeds.Length; i++)
            {
                if (!_enumerator.MoveNext())
                    break;
                seeds[i] = _enumerator.Current;
                count++;
            }
            return count;
        }
    }
}

public sealed class MotelySeedListProvider : IMotelySeedProvider
{
    // Keep seeds as enumerable - don't materialize! Seeds are used in the order provided.
    // For keyword generation, enumerable is lazy and avoids massive allocations.
    private readonly IEnumerator<string> _seedEnumerator;

    // IEnumerator<T> is not thread-safe; lock is intentional.
    private readonly object _enumeratorLock = new();

    public long SeedCount { get; private set; } = -1;

    /// <summary>
    /// True when the seeds are an in-memory collection rather than a lazy sequence. Only then is
    /// the list the whole, bounded workload that the search may run inline on the caller.
    /// </summary>
    internal bool IsMaterialized { get; }

    public MotelySeedListProvider(IEnumerable<string> seeds, long seedCount = -1)
    {
        _seedEnumerator = seeds.GetEnumerator();
        SeedCount = ResolveSeedCount(seeds, seedCount);
        IsMaterialized =
            seeds
                is ICollection<string>
                    or IReadOnlyCollection<string>
                    or System.Collections.ICollection;
    }

    private static long ResolveSeedCount(IEnumerable<string> seeds, long seedCount)
    {
        if (seedCount >= 0)
            return seedCount;

        if (seeds is ICollection<string> collection)
            return collection.Count;

        if (seeds is IReadOnlyCollection<string> readOnlyCollection)
            return readOnlyCollection.Count;

        if (seeds is System.Collections.ICollection nonGenericCollection)
            return nonGenericCollection.Count;

        return -1;
    }

    public bool TryNextSeed(out string seed)
    {
        lock (_enumeratorLock)
        {
            if (_seedEnumerator.MoveNext())
            {
                seed = _seedEnumerator.Current;
                return true;
            }
            seed = string.Empty;
            return false;
        }
    }

    public string NextSeed()
    {
        return TryNextSeed(out var seed) ? seed : string.Empty;
    }

    public int NextSeeds(string[] seeds)
    {
        if (seeds == null || seeds.Length == 0)
            return 0;

        lock (_enumeratorLock)
        {
            int count = 0;
            for (int i = 0; i < seeds.Length; i++)
            {
                if (!_seedEnumerator.MoveNext())
                    break;

                seeds[i] = _seedEnumerator.Current;
                count++;
            }
            return count;
        }
    }

    public void Dispose()
    {
        lock (_enumeratorLock)
        {
            _seedEnumerator?.Dispose();
        }
    }
}

/// <summary>
/// Drains <paramref name="first"/> to exhaustion, then falls through to <paramref name="second"/>.
/// Used to always run a JAML file's saved <c>seeds:</c> list ahead of whatever seed source the
/// search was otherwise configured with.
/// </summary>
public sealed class MotelyChainedSeedProvider(IMotelySeedProvider first, IMotelySeedProvider second)
    : IMotelySeedProvider
{
    private bool _firstExhausted;

    public long SeedCount { get; } =
        first.SeedCount >= 0 && second.SeedCount >= 0
            ? first.SeedCount + second.SeedCount
            : -1;

    public bool TryNextSeed(out string seed)
    {
        if (!_firstExhausted)
        {
            if (first.TryNextSeed(out seed))
                return true;
            _firstExhausted = true;
        }
        return second.TryNextSeed(out seed);
    }

    public string NextSeed() => TryNextSeed(out var seed) ? seed : string.Empty;

    public int NextSeeds(string[] seeds)
    {
        if (seeds == null || seeds.Length == 0)
            return 0;

        int count = 0;
        if (!_firstExhausted)
        {
            count = first.NextSeeds(seeds);
            if (count < seeds.Length)
                _firstExhausted = true;
        }

        if (count < seeds.Length)
        {
            var remaining = new string[seeds.Length - count];
            int gotFromSecond = second.NextSeeds(remaining);
            Array.Copy(remaining, 0, seeds, count, gotFromSecond);
            count += gotFromSecond;
        }

        return count;
    }
}

/// <summary>
/// Optional <see cref="IMotelySeedProvider"/> for <see cref="IAsyncEnumerable{T}"/> sources.
/// Pass to <see cref="MotelySearchSettings{TBaseFilter}.WithProviderSearch"/>; do not use unless you
/// truly need async streaming — prefer <see cref="MotelySeedListProvider"/> / <see cref="MotelySearchSettings{TBaseFilter}.WithSeedGenerator"/>.
/// </summary>
public sealed class MotelyAsyncSeedListProvider : IMotelySeedProvider, IDisposable, IAsyncDisposable
{
    private readonly IAsyncEnumerable<string> _seeds;
    private readonly CancellationToken _cancellationToken;

    private IAsyncEnumerator<string>? _enumerator;
    private string? _currentSeed;
    private readonly object _enumeratorLock = new();
    private bool _disposed;

    public long SeedCount { get; }

    public MotelyAsyncSeedListProvider(
        IAsyncEnumerable<string> seeds,
        long seedCount = -1,
        CancellationToken cancellationToken = default
    )
    {
        _seeds = seeds ?? throw new ArgumentNullException(nameof(seeds));
        SeedCount = seedCount;
        _cancellationToken = cancellationToken;
    }

    private IAsyncEnumerator<string> EnsureEnumerator()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _enumerator ??= _seeds.GetAsyncEnumerator(_cancellationToken);
    }

    private static bool MoveNextSync(IAsyncEnumerator<string> enumerator)
    {
        return enumerator.MoveNextAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public bool TryNextSeed(out string seed)
    {
        lock (_enumeratorLock)
        {
            if (_disposed)
            {
                seed = string.Empty;
                return false;
            }

            var enumerator = EnsureEnumerator();
            if (!MoveNextSync(enumerator))
            {
                seed = string.Empty;
                return false;
            }

            _currentSeed = enumerator.Current;
            seed = _currentSeed;
            return true;
        }
    }

    public string NextSeed() => TryNextSeed(out var seed) ? seed : string.Empty;

    public int NextSeeds(string[] seeds)
    {
        if (seeds is not { Length: > 0 })
            return 0;

        lock (_enumeratorLock)
        {
            if (_disposed)
                return 0;

            var enumerator = EnsureEnumerator();
            int count = 0;
            for (int i = 0; i < seeds.Length; i++)
            {
                if (!MoveNextSync(enumerator))
                    break;
                seeds[i] = enumerator.Current;
                count++;
            }

            return count;
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        IAsyncEnumerator<string>? enumerator;
        lock (_enumeratorLock)
        {
            if (_disposed)
                return;

            _disposed = true;
            enumerator = _enumerator;
            _enumerator = null;
        }

        if (enumerator != null)
            await enumerator.DisposeAsync().ConfigureAwait(false);
    }
}
