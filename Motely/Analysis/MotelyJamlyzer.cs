
namespace Motely.Analysis;

/// <summary>
/// One seed's Jamlyzer breakdown. <c>Score</c> and <c>Tally</c> are what the JAML's scoring said
/// about the seed — the same numbers the search's scored row carries (Tally is the per-should-clause
/// raw hit counts, in authored order). Tally is null when nothing scored the seed: a config with no
/// must/should clauses, or a search that has no score provider.
/// </summary>
public sealed record MotelyJamlyzerSeedResult(
    string Seed,
    int Score,
    IReadOnlyList<MotelyJamlyzerAnteResult> Antes,
    MotelyJamlyzerEvents Events,
    MotelyJamlyzerStreamStates StreamStates,
    IReadOnlyList<MotelyJamlyzerLegendary> LegendaryOrder,
    IReadOnlyList<MotelyItem>? ErraticDeck = null,
    int[]? Tally = null
);

public static class MotelyItemListExtensions
{
    extension(IReadOnlyList<MotelyItem> items)
    {
        public int Length => items.Count;
    }
}

/// <summary>
/// One Soul the player opens, in run order: the Nth entry is the Nth Soul opened, whatever ante it
/// sits in. Balatro keys the legendary pool <c>Joker4</c> with no ante, so the face is run-global;
/// the edition still comes from the Soul's own ante (<c>edisou{ante}</c>). This is the sequence
/// <c>legendaryJoker:</c> matches against, read once per seed instead of restarted every ante.
/// Every Soul is taken as opened and its legendary kept, so a draw that repeats a legendary already
/// held resamples, as it does in the game without Showman: the order a <c>soulOrder:</c> clause
/// matches under the same <c>with:</c> the Jamlyzer was given (none by default).
/// <c>Item</c> is the joker the Soul yields as an item, edition included — the same shape as every
/// other card the Jamlyzer reports, so a consumer draws it like any shop or pack card.
/// </summary>
public sealed record MotelyJamlyzerLegendary(
    int Ante,
    int PackSlot,
    MotelyJoker Joker,
    MotelyItemEdition Edition,
    MotelyItem Item
);

public sealed record MotelyJamlyzerAnteResult(
    int Ante,
    MotelyBossBlind Boss,
    MotelyVoucher Voucher,
    MotelyTag SmallBlindTag,
    MotelyTag BigBlindTag,
    IReadOnlyList<MotelyItem> ShopItems,
    IReadOnlyList<MotelyJamlyzerPack> Packs,
    MotelyJamlyzerPulls Pulls,
    MotelyJamlyzerShopStreams ShopStreams
);

public sealed record MotelyJamlyzerPack(MotelyBoosterPack Pack, IReadOnlyList<MotelyItem> Items);

/// <summary>
/// Per-ante raw shop-source PRNG queues, read independently of the resolved <c>ShopItems</c>.
/// Each is the sequence that source would yield on its own — e.g. <see cref="ShopTarots"/> is
/// the tarots a shop would surface if every slot rolled a tarot. All arrays have length == eventRolls.
/// </summary>
public sealed record MotelyJamlyzerShopStreams(
    IReadOnlyList<MotelyItem> ShopJokers,
    IReadOnlyList<MotelyItem> CommonShopJokers,
    IReadOnlyList<MotelyItem> UncommonShopJokers,
    IReadOnlyList<MotelyItem> RareShopJokers,
    IReadOnlyList<MotelyItem> ShopTarots,
    IReadOnlyList<MotelyItem> ShopPlanets,
    IReadOnlyList<MotelyItem> ShopSpectrals
);

/// <summary>
/// Per-ante rolls from streams that only fire when activated by a specific card or joker.
/// All arrays have length == eventRolls. EmperorTarots has length == eventRolls * 2 (2 per use).
/// Legendaries are not here: <c>Joker4</c> is run-global, so they live once per seed in
/// <see cref="MotelyJamlyzerSeedResult.LegendaryOrder"/>.
/// </summary>
public sealed record MotelyJamlyzerPulls(
    IReadOnlyList<MotelyItem> JudgementJokers,
    IReadOnlyList<MotelyItem> WraithJokers,
    IReadOnlyList<MotelyItem> EmperorTarots,
    IReadOnlyList<MotelyItem> PurpleSealTarots,
    IReadOnlyList<MotelyItem> SixthSenseSpectrals,
    IReadOnlyList<MotelyItem> SeanceSpectrals,
    IReadOnlyList<MotelyItem> RiffRaffJokers,
    IReadOnlyList<MotelyItem> RareTagJokers,
    IReadOnlyList<MotelyItem> UncommonTagJokers,
    IReadOnlyList<MotelyVoucher> VoucherSequence
);

/// <summary>
/// Resumable state bag for every stream, in AND out. Returned on every result and accepted back as
/// <c>resumeFrom</c> on the next <see cref="MotelyJamlyzer.Analyze(JamlConfig, MotelyJamlyzerStreamStates, int)"/>
/// (or, per seed, <see cref="MotelyJamlyzer.Analyze(JamlConfig, IReadOnlyDictionary{string, MotelyJamlyzerStreamStates}, int)"/>)
/// so the next window continues exactly where this one stopped — no duplicated, no skipped rolls.
/// <para>
/// Every stream's position is a named value here, and a resumed window sets each stream to its
/// value and reads on. Nothing is rolled again to get there, so a page costs the same however
/// deep the scroll is, and the scroll has no end.
/// <list type="bullet">
/// <item><b>Event streams</b> are single PRNG streams: one <c>double</c> each, the fields
/// below.</item>
/// <item><b>Per-ante streams</b> (the shop, the pulls, the raw shop-source queues) are several
/// PRNG streams each; <see cref="Antes"/> holds one typed state per stream per ante.</item>
/// </list>
/// </para>
/// <para>
/// The three counters say how far the scroll has gone and take no part in resuming:
/// <see cref="RollOffset"/> is the rolls read from every roll queue, <see cref="ShopOffset"/> the
/// shop items read by windows that set <c>shopSlots</c>, and <see cref="ShopDefaultWindows"/> the
/// windows that left it at 0 and so read each ante's default depth (15 on antes 0-1, 50 beyond).
/// </para>
/// </summary>
public sealed record MotelyJamlyzerStreamStates(
    int RollOffset,
    int ShopOffset,
    double LuckyMoney,
    double LuckyMult,
    double WheelOfFortune,
    double Cavendish,
    double GrosMichel,
    double Space,
    double Business,
    double Bloodstone,
    double Parking,
    double EightBall,
    double Glass,
    double OmenGlobe,
    double TheWheel,
    double Misprint,
    int ShopDefaultWindows = 0,
    IReadOnlyList<MotelyJamlyzerAnteStreamStates>? Antes = null
)
{
    // Value equality through the ante list, which a record compares by reference on its own.
    public bool Equals(MotelyJamlyzerStreamStates? other) =>
        other is not null
        && RollOffset == other.RollOffset
        && ShopOffset == other.ShopOffset
        && ShopDefaultWindows == other.ShopDefaultWindows
        && LuckyMoney.Equals(other.LuckyMoney)
        && LuckyMult.Equals(other.LuckyMult)
        && WheelOfFortune.Equals(other.WheelOfFortune)
        && Cavendish.Equals(other.Cavendish)
        && GrosMichel.Equals(other.GrosMichel)
        && Space.Equals(other.Space)
        && Business.Equals(other.Business)
        && Bloodstone.Equals(other.Bloodstone)
        && Parking.Equals(other.Parking)
        && EightBall.Equals(other.EightBall)
        && Glass.Equals(other.Glass)
        && OmenGlobe.Equals(other.OmenGlobe)
        && TheWheel.Equals(other.TheWheel)
        && Misprint.Equals(other.Misprint)
        && (Antes is null
            ? other.Antes is null
            : other.Antes is not null && Antes.SequenceEqual(other.Antes));

    public override int GetHashCode() => HashCode.Combine(RollOffset, ShopOffset, ShopDefaultWindows, Misprint);
}

/// <summary>
/// Where one ante's streams stand. <see cref="Pulls"/> and <see cref="ShopStreams"/> are null
/// until a window reads them (<c>eventRolls: 0</c> never does): null is the seed's natural start.
/// </summary>
public sealed record MotelyJamlyzerAnteStreamStates(
    int Ante,
    MotelyShopItemStreamState Shop,
    MotelyJamlyzerPullStreamStates? Pulls,
    MotelyJamlyzerShopSourceStreamStates? ShopStreams
);

/// <summary>The streams behind <see cref="MotelyJamlyzerPulls"/>, one state per queue, same names.</summary>
public sealed record MotelyJamlyzerPullStreamStates(
    MotelyJokerStreamState JudgementJokers,
    MotelyJokerFixedRarityStreamState WraithJokers,
    MotelyTarotStreamState EmperorTarots,
    MotelyTarotStreamState PurpleSealTarots,
    MotelySpectralStreamState SixthSenseSpectrals,
    MotelySpectralStreamState SeanceSpectrals,
    MotelyJokerFixedRarityStreamState RiffRaffJokers,
    MotelyJokerFixedRarityStreamState RareTagJokers,
    MotelyJokerFixedRarityStreamState UncommonTagJokers,
    MotelyResampleStreamState VoucherSequence
);

/// <summary>The streams behind <see cref="MotelyJamlyzerShopStreams"/>, one state per queue, same names.</summary>
public sealed record MotelyJamlyzerShopSourceStreamStates(
    MotelyJokerStreamState ShopJokers,
    MotelyJokerFixedRarityStreamState CommonShopJokers,
    MotelyJokerFixedRarityStreamState UncommonShopJokers,
    MotelyJokerFixedRarityStreamState RareShopJokers,
    MotelyTarotStreamState ShopTarots,
    MotelyPlanetStreamState ShopPlanets,
    MotelySpectralStreamState ShopSpectrals
);

public sealed record MotelyJamlyzerEvents(
    bool[] LuckyMoney,
    bool[] LuckyMult,
    MotelyItemEdition[] WheelOfFortune,
    bool[] Cavendish,
    bool[] GrosMichel,
    bool[] Space,
    bool[] Business,
    bool[] Bloodstone,
    bool[] Parking,
    bool[] EightBall,
    bool[] Glass,
    bool[] OmenGlobe,
    bool[] TheWheel,
    int[] Misprint
);

public static class MotelyJamlyzer
{
    /// <summary>
    /// The most rolls one call reads from each roll queue (<c>eventRolls</c>). It bounds a single
    /// window, so one call cannot allocate gigabytes or pin the browser's only thread; it does
    /// not bound the scroll, which resumes from stream states and can go on as long as it likes.
    /// </summary>
    public const int MaxEventRolls = 10_000;

    /// <summary>The most shop items one call reads per ante (<c>shopSlots</c>); a window bound, as above.</summary>
    public const int MaxShopSlots = 100_000;

    /// <summary>Analyze each seed with every event stream starting at the seed's natural start (0).</summary>
    /// <param name="with">What the player holds, the block a clause takes as <c>with:</c>: its
    /// vouchers build each shop, its luck weighs the luck events, its legendaries are out of the
    /// Soul pool unless Showman is held. Left out, the seed shows as a clause with no
    /// <c>with:</c> matches it.</param>
    public static IReadOnlyList<MotelyJamlyzerSeedResult> Analyze(
        JamlConfig config,
        int eventRolls = 20,
        int shopSlots = 0,
        MotelyWith? with = null
    ) => AnalyzeCore(config, resumeStates: null, eventRolls, shopSlots, with);

    /// <summary>
    /// Analyze each seed, resuming every event stream from <paramref name="resumeFrom"/> — the state
    /// bag handed back by a previous call — so the rolls continue where the last window stopped.
    /// Pass the same <paramref name="with"/> as the window being continued: the shop is replayed
    /// under it.
    /// </summary>
    public static IReadOnlyList<MotelyJamlyzerSeedResult> Analyze(
        JamlConfig config,
        MotelyJamlyzerStreamStates resumeFrom,
        int eventRolls = 20,
        int shopSlots = 0,
        MotelyWith? with = null
    )
    {
        // The bag's 14 event-stream State doubles are positions in a *specific* seed's PRNG. Replaying
        // one bag across a multi-seed config would inject seed[0]'s state into seed[1..], silently
        // corrupting their event rolls. A single bag is inherently a single-seed scroll — to scroll
        // many seeds at once, hand each its own bag via the dictionary overload below.
        if (config.Seeds.Count > 1)
            throw new InvalidOperationException(
                $"Resume (resumeFrom) is single-seed only; config has {config.Seeds.Count} seeds. "
                    + "Scroll one seed at a time, or use the per-seed dictionary overload — the "
                    + "state bag is seed-specific."
            );
        // Nothing to resume, same answer as the fresh overload: no seeds, no rows.
        if (config.Seeds.Count == 0)
            return AnalyzeCore(config, resumeStates: null, eventRolls, shopSlots, with);
        return AnalyzeCore(
            config,
            new Dictionary<string, MotelyJamlyzerStreamStates> { [config.Seeds[0]] = resumeFrom },
            eventRolls,
            shopSlots,
            with
        );
    }

    /// <summary>
    /// Analyze each seed, resuming it from its own state bag in <paramref name="resumeFrom"/> (keyed
    /// by seed). Each <see cref="MotelyJamlyzerSeedResult"/> already carries its <c>Seed</c> and
    /// <c>StreamStates</c>, so the next page is just
    /// <c>prev.ToDictionary(r => r.Seed, r => r.StreamStates)</c>. Seeds absent from the map start
    /// fresh (offset 0), which is how a new seed joins a scroll already in progress. Duplicate seeds
    /// in <paramref name="config"/> collapse to one bag under the shared key.
    /// </summary>
    public static IReadOnlyList<MotelyJamlyzerSeedResult> Analyze(
        JamlConfig config,
        IReadOnlyDictionary<string, MotelyJamlyzerStreamStates> resumeFrom,
        int eventRolls = 20,
        int shopSlots = 0,
        MotelyWith? with = null
    ) => AnalyzeCore(config, resumeFrom, eventRolls, shopSlots, with);

    /// <summary>Every ante the Jamlyzer can walk: the pre-run shop (0) and antes 1..8.</summary>
    public static readonly int[] AllAntes = [0, 1, 2, 3, 4, 5, 6, 7, 8];

    /// <summary>How deep an ante's shop is walked when <c>shopSlots</c> is 0: 15 for the pre-run
    /// shop and ante 1, 50 beyond.</summary>
    /// <param name="ante">The ante.</param>
    /// <returns>The number of shop items the result carries for that ante.</returns>
    public static int DefaultShopSlots(int ante) => MotelyJamlyzerWindow.DefaultShopSlots(ante);

    /// <summary>How many booster packs an ante offers, and so how many the result carries: 4 for
    /// ante 1, 6 for every other ante, ante 0 included. Pack indices in <c>sources:</c> count these.</summary>
    /// <param name="ante">The ante.</param>
    /// <returns>The pack count.</returns>
    public static int PackCount(int ante) => MotelyJamlyzerWindow.PackCount(ante);

    /// <summary>
    /// The Jamlyzer as a rider on a search. Attach the returned desc with
    /// <see cref="IMotelySearchSettings.WithSeedAnalyzeProvider"/> and every seed the search reports
    /// arrives at <paramref name="onAnalyzed"/> as a full <see cref="MotelyJamlyzerSeedResult"/>,
    /// walked on the same context that just filtered and scored it and carrying the search's own
    /// Score and Tally. One pass — a host on the far side of a boundary gets the find and what it
    /// contains together, with no second call to <see cref="Analyze(JamlConfig, int)"/>.
    /// <para>
    /// <paramref name="eventRolls"/> 0 gives the per-ante summary alone — boss, voucher, tags, shop,
    /// packs — with every roll queue empty, the cheap shape for a results table. The callback fires
    /// on the search's worker thread(s).
    /// </para>
    /// </summary>
    public static MotelyJamlyzerRiderDesc CreateRiderDesc(
        JamlConfig config,
        Action<MotelyJamlyzerSeedResult> onAnalyzed,
        int eventRolls = 20,
        int shopSlots = 0,
        MotelyWith? with = null
    ) => new(ComputeAntes(config), onAnalyzed, eventRolls, shopSlots, OpensAnteZeroSouls(config), with);

    private static IReadOnlyList<MotelyJamlyzerSeedResult> AnalyzeCore(
        JamlConfig config,
        IReadOnlyDictionary<string, MotelyJamlyzerStreamStates>? resumeStates,
        int eventRolls,
        int shopSlots,
        MotelyWith? with
    )
    {
        MotelyJamlyzerWindow.ThrowIfOutOfRange(eventRolls, shopSlots);
        var antesToAnalyze = ComputeAntes(config);
        bool opensAnteZeroSouls = OpensAnteZeroSouls(config);
        bool hasScore = config.Must.Count + config.Should.Count > 0;
        var results = new List<MotelyJamlyzerSeedResult>(config.Seeds.Count);

        foreach (var seed in config.Seeds)
        {
            bool validSeed = TryCanonicalizeSeed(seed, out var canonicalSeed);
            string lookupSeed = validSeed ? canonicalSeed! : seed;

            // Each seed resumes from its own bag; one absent from the map starts fresh (offset 0).
            MotelyJamlyzerStreamStates? seedResume =
                resumeStates is not null
                && (
                    resumeStates.TryGetValue(lookupSeed, out var s)
                    || (seed is not null && resumeStates.TryGetValue(seed, out s))
                )
                    ? s
                    : null;
            if (seedResume is not null)
                ThrowIfNotResumable(lookupSeed, seedResume, eventRolls, shopSlots, antesToAnalyze);
            if (!validSeed)
                continue;

            var filterDesc = new MotelyJamlyzerFilterDesc(
                antesToAnalyze,
                eventRolls,
                seedResume,
                shopSlots,
                opensAnteZeroSouls,
                with
            );
            var settings = new MotelySearchSettings<MotelyJamlyzerFilterDesc.JamlyzerFilter>(
                filterDesc
            )
                .WithDeck(config.Deck)
                .WithStake(config.Stake)
                .WithSeedList([canonicalSeed!])
                .WithThreadCount(1);

            int score = 0;
            int[]? tally = null;
            if (hasScore)
            {
                settings = settings
                    .WithSeedScoreProvider(
                        new JamlShouldScoreDesc([.. config.Must], [.. config.Should])
                    )
                    .WithScoredResultCallback(row =>
                    {
                        score = row.Score;
                        tally = row.Tallies;
                    });
            }

            using var search = settings.CreateSearch();
            search.Start();
            search.AwaitCompletion();

            // A seed the engine never handed to the filter (invalid) has no breakdown, so no row.
            if (filterDesc.Result is { } result)
                results.Add(result with { Score = score, Tally = tally });
        }

        return results;
    }

    private static bool TryCanonicalizeSeed(
        string? rawSeed,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? canonicalSeed
    )
    {
        canonicalSeed = null;
        if (rawSeed is null)
            return false;

        ReadOnlySpan<char> span = rawSeed.AsSpan().Trim();
        if (span is "\"\"" or "''")
        {
            canonicalSeed = string.Empty;
            return true;
        }
        if (
            span.Length >= 2
            && ((span[0] == '"' && span[^1] == '"') || (span[0] == '\'' && span[^1] == '\''))
        )
        {
            span = span[1..^1].Trim();
            if (span.IsEmpty)
                return false;
        }
        else if (span.IsEmpty && rawSeed.Length > 0)
        {
            return false;
        }

        if (span.Length > MotelyGlobals.MaxSeedLength)
            return false;

        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];
            if (c is not ((>= '1' and <= '9') or (>= 'A' and <= 'Z') or (>= 'a' and <= 'z')))
                return false;
        }

        canonicalSeed = span.IsEmpty ? string.Empty : span.ToString().ToUpperInvariant();
        return true;
    }

    /// <summary>
    /// A bag only ever comes back from a previous result, but across the JS boundary it is just
    /// an object. A negative or huge offset, or a state outside the PRNG's [0, 1] range, is
    /// refused.
    /// </summary>
    private static void ThrowIfNotResumable(
        string seed,
        MotelyJamlyzerStreamStates resumeFrom,
        int eventRolls,
        int shopSlots,
        int[] antesToAnalyze
    )
    {
        const string Hint = " Pass back the streamStates of a previous result unchanged.";

        // The counters only count. A window may not push one past what an int holds.
        if (resumeFrom.RollOffset < 0 || (long)resumeFrom.RollOffset + eventRolls > int.MaxValue)
            throw new ArgumentOutOfRangeException(
                "resumeFrom",
                $"RollOffset {resumeFrom.RollOffset} + eventRolls {eventRolls} must be 0..{int.MaxValue}."
                    + Hint
            );
        if (
            resumeFrom.ShopOffset < 0
            || resumeFrom.ShopDefaultWindows < 0
            || (long)resumeFrom.ShopOffset + shopSlots > int.MaxValue
            || (long)resumeFrom.ShopDefaultWindows + 1 > int.MaxValue
        )
            throw new ArgumentOutOfRangeException(
                "resumeFrom",
                $"ShopOffset {resumeFrom.ShopOffset} and ShopDefaultWindows {resumeFrom.ShopDefaultWindows} "
                    + $"must each be 0..{int.MaxValue} after this window."
                    + Hint
            );

        // A scroll that has read anything carries the ante streams it read them from.
        bool hasRead = resumeFrom.RollOffset > 0 || resumeFrom.ShopOffset > 0 || resumeFrom.ShopDefaultWindows > 0;
        if (hasRead && resumeFrom.Antes is null)
            throw new ArgumentOutOfRangeException(
                "resumeFrom",
                "Antes is missing, so the shop and pull streams have no position to continue from." + Hint
            );
        foreach (var ante in resumeFrom.Antes ?? [])
            if (ante is null || !IsPosition(ante))
                throw new ArgumentOutOfRangeException(
                    "resumeFrom",
                    $"Antes holds a stream state for ante {ante?.Ante} that is not a PRNG stream state "
                        + "(0..1, or -1 for a stream not created)." + Hint
                );

        // Every ante the JAML scopes is walked on every seed, so its streams are in a real bag.
        // (Ante 0 is also walked on seeds whose ante-1 voucher reaches it; the walk covers that.)
        if (resumeFrom.Antes is { } anteStates)
        {
            var window = new MotelyJamlyzerWindow(antesToAnalyze, eventRolls, shopSlots);
            for (int ante = window.StartAnte; ante <= window.MaxAnte; ante++)
                if (!anteStates.Any(state => state.Ante == ante))
                    throw new ArgumentOutOfRangeException(
                        "resumeFrom",
                        $"Antes has no stream state for ante {ante}, which this filter reads." + Hint
                    );
        }

        ReadOnlySpan<(string Name, string PrngKey, double State)> states =
        [
            (nameof(resumeFrom.LuckyMoney), MotelyPrngKeys.CardLuckyMoney, resumeFrom.LuckyMoney),
            (nameof(resumeFrom.LuckyMult), MotelyPrngKeys.CardLuckyMult, resumeFrom.LuckyMult),
            (nameof(resumeFrom.WheelOfFortune), MotelyPrngKeys.TarotWheelOfFortune, resumeFrom.WheelOfFortune),
            (nameof(resumeFrom.Cavendish), MotelyPrngKeys.JokerCavendish, resumeFrom.Cavendish),
            (nameof(resumeFrom.GrosMichel), MotelyPrngKeys.JokerGrosMichel, resumeFrom.GrosMichel),
            (nameof(resumeFrom.Space), MotelyPrngKeys.JokerSpace, resumeFrom.Space),
            (nameof(resumeFrom.Business), MotelyPrngKeys.JokerBusiness, resumeFrom.Business),
            (nameof(resumeFrom.Bloodstone), MotelyPrngKeys.JokerBloodstone, resumeFrom.Bloodstone),
            (nameof(resumeFrom.Parking), MotelyPrngKeys.JokerParking, resumeFrom.Parking),
            (nameof(resumeFrom.EightBall), MotelyPrngKeys.JokerEightBall, resumeFrom.EightBall),
            (nameof(resumeFrom.Glass), MotelyPrngKeys.CardGlass, resumeFrom.Glass),
            (nameof(resumeFrom.OmenGlobe), MotelyPrngKeys.VoucherOmenGlobe, resumeFrom.OmenGlobe),
            (nameof(resumeFrom.TheWheel), MotelyPrngKeys.BossTheWheel, resumeFrom.TheWheel),
            (nameof(resumeFrom.Misprint), MotelyPrngKeys.JokerMisprint, resumeFrom.Misprint),
        ];
        foreach (var (name, prngKey, state) in states)
        {
            if (double.IsNaN(state))
            {
                if (!IsNaturalNaNStream(seed, prngKey))
                    throw new ArgumentOutOfRangeException(
                        "resumeFrom",
                        $"{name} state {state} is not a PRNG stream state (0..1)." + Hint
                    );
                continue;
            }

            if (state < 0 || state > 1)
                throw new ArgumentOutOfRangeException(
                    "resumeFrom",
                    $"{name} state {state} is not a PRNG stream state (0..1)." + Hint
                );
        }
    }

    // An ante stream's state is real when every double in it is one a PRNG stream can hold: 0..1,
    // -1 (the engine's mark for a stream that is excluded or not created yet), or NaN, which a
    // seed whose key hashes to NaN holds from its first roll on. The event streams above check a
    // NaN against its key; these have too many keys to, so a NaN is taken as natural here.
    private static bool IsPosition(double state) => double.IsNaN(state) || state == -1 || (state >= 0 && state <= 1);

    private static bool IsPosition(MotelyResampleStreamState? s) =>
        s is { Resamples: not null } && IsPosition(s.Initial) && s.Resamples.All(IsPosition);

    private static bool IsPosition(MotelyJokerStreamState? s) =>
        s is not null
        && IsPosition(s.Rarity) && IsPosition(s.Edition) && IsPosition(s.EternalPerishable) && IsPosition(s.Rental)
        && IsPosition(s.Common) && IsPosition(s.Uncommon) && IsPosition(s.Rare)
        && (s.CommonResample is null || IsPosition(s.CommonResample))
        && (s.UncommonResample is null || IsPosition(s.UncommonResample))
        && (s.RareResample is null || IsPosition(s.RareResample));

    private static bool IsPosition(MotelyJokerFixedRarityStreamState? s) =>
        s is not null
        && IsPosition(s.Joker) && IsPosition(s.Edition) && IsPosition(s.EternalPerishable) && IsPosition(s.Rental);

    private static bool IsPosition(MotelyTarotStreamState? s) => s is not null && IsPosition(s.Cards) && IsPosition(s.Soul);

    private static bool IsPosition(MotelyPlanetStreamState? s) => s is not null && IsPosition(s.Cards) && IsPosition(s.BlackHole);

    private static bool IsPosition(MotelySpectralStreamState? s) =>
        s is not null && IsPosition(s.Cards) && IsPosition(s.SoulBlackHole);

    private static bool IsPosition(MotelyJamlyzerAnteStreamStates ante) =>
        ante.Shop is { } shop
        && IsPosition(shop.ItemType) && IsPosition(shop.Jokers) && IsPosition(shop.Tarots)
        && IsPosition(shop.Planets) && IsPosition(shop.Spectrals) && IsPosition(shop.StandardCards)
        && (ante.Pulls is not { } pulls
            || (IsPosition(pulls.JudgementJokers) && IsPosition(pulls.WraithJokers)
                && IsPosition(pulls.EmperorTarots) && IsPosition(pulls.PurpleSealTarots)
                && IsPosition(pulls.SixthSenseSpectrals) && IsPosition(pulls.SeanceSpectrals)
                && IsPosition(pulls.RiffRaffJokers) && IsPosition(pulls.RareTagJokers)
                && IsPosition(pulls.UncommonTagJokers) && IsPosition(pulls.VoucherSequence)))
        && (ante.ShopStreams is not { } sources
            || (IsPosition(sources.ShopJokers) && IsPosition(sources.CommonShopJokers)
                && IsPosition(sources.UncommonShopJokers) && IsPosition(sources.RareShopJokers)
                && IsPosition(sources.ShopTarots) && IsPosition(sources.ShopPlanets)
                && IsPosition(sources.ShopSpectrals)));

    private static bool IsNaturalNaNStream(string rawSeed, string prngKey)
    {
        if (!TryCanonicalizeSeed(rawSeed, out var normalized) || normalized.Length == 0)
            return false;
        string fullKey = prngKey + normalized;
        double num = 1.0;
        for (int i = fullKey.Length - 1; i >= 0; i--)
        {
            double x = 1.1239285023 / num * fullKey[i] * Math.PI + (i + 1) * Math.PI;
            num = x - Math.Floor(x);
        }
        return double.IsNaN(num);
    }

    /// <summary>
    /// The antes the JAML's clauses scope to (sorted, deduplicated), or <see cref="AllAntes"/> when
    /// no clause names one.
    /// </summary>
    public static int[] ComputeAntes(JamlConfig config)
    {
        var set = new SortedSet<int>();
        foreach (var clause in config.Must.Concat(config.Should).Concat(config.MustNot))
            AddAntes(clause, set);
        return set.Count > 0 ? [.. set] : AllAntes;
    }

    private static void AddAntes(IMotelyClause clause, SortedSet<int> set)
    {
        if (clause is IAnteScopedClause scoped)
        {
            set.UnionWith(scoped.Antes);
        }
        else if (clause is LegendarySoulOrderClause soulOrder)
        {
            int maxAnte = soulOrder.MaxAnte > 0 ? soulOrder.MaxAnte : 8;
            bool spansAnyAnte = false;
            foreach (var entry in soulOrder.Entries)
            {
                if (entry.Antes.Length > 0)
                    set.UnionWith(entry.Antes);
                else
                    spansAnyAnte = true;
            }
            if (spansAnyAnte)
            {
                for (int a = 1; a <= maxAnte; a++)
                    set.Add(a);
            }
        }
        else if (clause is LogicClause logic)
        {
            foreach (var child in logic.Clauses)
                AddAntes(child, set);
        }
    }

    /// <summary>
    /// Whether the walk opens ante 0's Souls: only when a legendaryJoker or soulOrder clause names
    /// ante 0. Ante 0 is in the run only after Hieroglyph or Petroglyph, so a walk that opened its
    /// Soul first handed every later Soul the next legendary over (1YU11111: the ante-2 Soul read
    /// Yorick, where a run that never plays ante 0 gets Triboulet) and disagreed with every
    /// legendary clause on antes 1 and up, which opens exactly the Souls it names.
    /// </summary>
    public static bool OpensAnteZeroSouls(JamlConfig config) =>
        config.Must.Concat(config.Should).Concat(config.MustNot).Any(NamesAnteZeroSoul);

    private static bool NamesAnteZeroSoul(IMotelyClause clause) =>
        clause switch
        {
            JokerClause c => Array.IndexOf(c.Antes, 0) >= 0 && (c.Jokers.Length == 0 || c.Jokers.Any(j => ((MotelyJokerRarity)((int)j & MotelyGlobals.JokerRarityMask)) == MotelyJokerRarity.Legendary)),
            LegendaryJokerClause c => Array.IndexOf(c.Antes, 0) >= 0,
            LegendarySoulOrderClause c => Array.Exists(c.Entries, e => Array.IndexOf(e.Antes, 0) >= 0),
            LogicClause c => Array.Exists(c.Clauses, NamesAnteZeroSoul),
            _ => false,
        };
}
