using System.ComponentModel;
using Motely.Filters;

namespace Motely.Analysis;

/// <summary>
/// The window the Jamlyzer reads for one seed: which antes (the JAML's ante scope, see
/// <see cref="MotelyJamlyzer.ComputeAntes"/>), how many rolls per stream, and what the player
/// holds (<see cref="With"/>).
/// </summary>
internal readonly struct MotelyJamlyzerWindow
{
    private static readonly MotelyWith NoWith = new();

    public MotelyJamlyzerWindow(
        int[] antesToAnalyze,
        int eventRolls,
        int shopSlots = 0,
        bool opensAnteZeroSouls = false,
        MotelyWith? with = null
    )
    {
        ThrowIfOutOfRange(eventRolls, shopSlots);
        StartAnte = antesToAnalyze.Length > 0 && antesToAnalyze[0] == 0 ? 0 : 1;
        MaxAnte = antesToAnalyze.Length > 0 ? antesToAnalyze[^1] : 8;
        EventRolls = eventRolls;
        ShopSlots = shopSlots;
        OpensAnteZeroSouls = opensAnteZeroSouls;
        With = with ?? NoWith;
    }

    /// <summary>
    /// What the player holds, the same block a clause takes as <c>with:</c>, and read the same
    /// way: its vouchers build each ante's shop, its luck weighs the luck events, its legendaries
    /// are out of the Soul pool unless Showman is held too. Empty shows the seed as a clause
    /// with no <c>with:</c> matches it.
    /// </summary>
    public readonly MotelyWith With;

    // Ante 0 (the pre-run shop a JAML clause can target with `antes: [0]`) is real data
    // the search already matches on — emit it when scoped instead of silently dropping it.
    public readonly int StartAnte;
    public readonly int MaxAnte;
    public readonly int EventRolls;

    /// <summary>
    /// Whether the walk opens ante 0's Souls. Ante 0 is in the run only after Hieroglyph or
    /// Petroglyph, so its cards are shown and its Souls stay shut unless a legendary clause names
    /// ante 0 (<see cref="MotelyJamlyzer.OpensAnteZeroSouls"/>): opened first, an ante-0 Soul handed
    /// every later Soul the next legendary over, which a run that never plays ante 0 does not get.
    /// </summary>
    public readonly bool OpensAnteZeroSouls;

    // How deep to walk each ante's shop queue, independent of EventRolls. 0 keeps the historical
    // defaults (15 for the pre-run shop and ante 1, 50 beyond). The shop stream never runs dry --
    // GetNextShopItem yields forever -- so this bound is the only thing that ends the walk.
    // It is its own dial because EventRolls sizes eighteen other arrays; hanging shop depth on it
    // makes a 5000-slot shop allocate 5000 entries per pull and shop-source stream as well.
    public readonly int ShopSlots;

    /// <summary>The per-ante shop depth a <c>shopSlots: 0</c> window walks.</summary>
    public static int DefaultShopSlots(int ante) => ante <= 1 ? 15 : 50;

    /// <summary>
    /// The booster packs an ante offers: 4 for ante 1 (its first shop opens after the small blind)
    /// and 6 for every other ante. Ante 0 runs all three of its shops once Hieroglyph and Petroglyph
    /// ease the run back to it, and a clause searches its six packs.
    /// </summary>
    public static int PackCount(int ante) => ante == 1 ? 4 : 6;

    /// <summary>
    /// Both dials arrive from JS unchecked. An out-of-range one throws naming the argument and
    /// the limit.
    /// </summary>
    public static void ThrowIfOutOfRange(int eventRolls, int shopSlots)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(eventRolls);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(eventRolls, MotelyJamlyzer.MaxEventRolls);
        ArgumentOutOfRangeException.ThrowIfNegative(shopSlots);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(shopSlots, MotelyJamlyzer.MaxShopSlots);
    }
}

/// <summary>
/// Jamlyzer's filter: walks every ante's boss/voucher/tags/shop/packs and every relevant PRNG
/// stream for one seed, answering "what does this seed actually contain?". The walk itself is
/// <see cref="MotelyJamlyzerSeedWalk"/>; this desc is how
/// <see cref="MotelyJamlyzer.Analyze(Motely.Filters.JamlConfig, int)"/> runs it standalone,
/// one seed per search, with a resume bag. Unrelated to the CLI's legacy <c>--analyze</c>
/// flag (<see cref="MotelyUnitTestAnalyzer"/>).
/// </summary>
public sealed class MotelyJamlyzerFilterDesc(
    int[] antesToAnalyze,
    int eventRolls = 20,
    MotelyJamlyzerStreamStates? resumeFrom = null,
    int shopSlots = 0,
    bool opensAnteZeroSouls = false,
    MotelyWith? with = null
) : IMotelySeedFilterDesc<MotelyJamlyzerFilterDesc.JamlyzerFilter>
{
    /// <summary>
    /// The last walked seed's breakdown, Score 0 and Tally null (the caller scores). Null until
    /// the filter has run — a seed the engine never handed over (invalid) leaves it null.
    /// </summary>
    public MotelyJamlyzerSeedResult? Result { get; private set; }

    public JamlyzerFilter CreateFilter(ref MotelyFilterCreationContext ctx) => new(this);

    public readonly struct JamlyzerFilter(MotelyJamlyzerFilterDesc filterDesc) : IMotelySeedFilter
    {
        public readonly VectorMask Filter(ref MotelyVectorSearchContext ctx) =>
            ctx.SearchIndividualSeeds(CheckSeed);

        public readonly int CheckSeed(MotelySingleSearchContext ctx)
        {
            filterDesc.Result = MotelyJamlyzerSeedWalk.Walk(
                ref ctx,
                in filterDesc._window,
                filterDesc._resumeFrom
            );
            return 1;
        }
    }

    private readonly MotelyJamlyzerWindow _window =
        new(antesToAnalyze, eventRolls, shopSlots, opensAnteZeroSouls, with);
    private readonly MotelyJamlyzerStreamStates? _resumeFrom = resumeFrom;
}

/// <summary>
/// The Jamlyzer's seed walk: one live seed context in, that seed's whole breakdown out. Pure —
/// every call builds a fresh result and touches no shared state, so it is safe from any worker
/// thread and any SIMD lane. Score is left 0 and Tally null because the caller already knows what
/// the seed scored: the standalone Jamlyzer scores it in the same search, and the analyze
/// provider copies the search's own scored row.
/// </summary>
internal static class MotelyJamlyzerSeedWalk
{
    private ref struct AnteAnalysisState
    {
        public MotelySingleTarotStream ArcanaStream;
        public readonly bool HasArcanaStream => !ArcanaStream.IsNull;
        public MotelySinglePlanetStream CelestialStream;
        public readonly bool HasCelestialStream => !CelestialStream.IsNull;
        public MotelySingleSpectralStream SpectralStream;
        public readonly bool HasSpectralStream => !SpectralStream.IsNull;
        public MotelySingleStandardCardStream StandardStream;
        public readonly bool HasStandardStream => !StandardStream.IsInvalid;
        public MotelySingleJokerStream BuffoonStream;
        public readonly bool HasBuffoonStream => !BuffoonStream.IsNull;
    }

    internal static MotelyJamlyzerSeedResult Walk(
        ref MotelySingleSearchContext ctx,
        in MotelyJamlyzerWindow window,
        MotelyJamlyzerStreamStates? resumeFrom
    )
    {
        int n = window.EventRolls;

        MotelyRunState voucherState = ctx.Deck.GetDefaultRunState();
        MotelySingleBossStream bossStream = ctx.CreateBossStream();

        // What the player holds, on the engine's run state exactly as a clause's own with: puts
        // it there: the vouchers redeemed on the deck's, then luck and Showman (ApplyWith). The
        // shop, the luck events and the Souls below read this one state. The voucher chain above
        // stays the seed's own, as a voucher: clause (which takes no with:) reads it.
        MotelyRunState heldState = ctx.Deck.GetRunState(window.With.Vouchers);
        heldState.ApplyWith(window.With);

        // Ante 0 is walked when the window names it, or when this seed's ante-1 voucher is
        // Hieroglyph/Petroglyph — buying it eases the run back to ante 0, so that round is real.
        int startAnte = window.StartAnte;
        if (startAnte == 1
            && ctx.GetAnteFirstVoucher(1, ctx.Deck.GetDefaultRunState()) is MotelyVoucher.Hieroglyph or MotelyVoucher.Petroglyph)
            startAnte = 0;

        var antes = new MotelyJamlyzerAnteResult[window.MaxAnte - startAnte + 1];
        // Where each ante's streams stand when this window ends: the next window starts there.
        var anteStates = new MotelyJamlyzerAnteStreamStates[antes.Length];

        // The legendary face pool (Joker4) is run-global -- keyed with no ante -- so the Nth Soul the
        // player opens gets draw N however the antes fall. One stream carried across the whole walk
        // records that order once per seed. (The old per-ante LegendaryJokers queues restarted it on
        // every ante, so each ante showed the same global prefix; they are gone.)
        // Every Soul is opened and every legendary kept, so a draw that repeats one already held
        // resamples (Balatro's used_jokers) unless Showman is held. This is the clause walk's own
        // SoulWalk, started from the legendaries in with: as a legendaryJoker clause starts it.
        var souls = new LegendarySoulMatcher.SoulWalk(ctx.CreateRunLegendaryJokerStream());
        souls.HoldFrom(window.With.Jokers);
        List<MotelyJamlyzerLegendary> legendaryOrder = [];

        for (int ante = startAnte; ante <= window.MaxAnte; ante++)
        {
            // Ante 0 is the pre-run shop, reached by ante reduction (Hieroglyph / Petroglyph):
            // its shop, packs and tags are exactly what a JAML `antes: [0]` clause searches.
            // Its boss reads off an isolated stream + run state, because the run state the
            // search chains (voucher tiers, seen-boss pool) opens at ante 1 — sharing it here
            // would shift every ante 1..8 boss and upgrade ante 1's voucher.
            bool isPreRunShop = ante == 0;
            MotelyBossBlind boss;
            if (isPreRunShop)
            {
                MotelyRunState preRunState = ctx.Deck.GetDefaultRunState();
                MotelySingleBossStream preRunBossStream = ctx.CreateBossStream();
                boss = ctx.GetBossForAnte(ref preRunBossStream, ante, preRunState);
            }
            else
                boss = ctx.GetBossForAnte(ref bossStream, ante, voucherState);

            MotelyVoucher voucher = ctx.GetAnteFirstVoucher(ante, voucherState);

            AnteAnalysisState state = new()
            {
                ArcanaStream = default,
                CelestialStream = default,
                SpectralStream = default,
                StandardStream = MotelySingleStandardCardStream.Invalid,
                BuffoonStream = default,
            };

            // Tags
            MotelySingleTagStream tagStream = ctx.CreateTagStream(ante);
            MotelyTag smallTag = ctx.GetNextTag(ref tagStream);
            MotelyTag bigTag = ctx.GetNextTag(ref tagStream);

            // Shop
            // A resumed window creates each stream the usual way and sets it to where the last
            // window left it; nothing is rolled to get there.
            MotelyJamlyzerAnteStreamStates? anteResume = ResumeOf(resumeFrom, ante);

            MotelySingleShopItemStream shopStream = ctx.CreateShopItemStream(ante, heldState);
            if (anteResume is not null)
                shopStream.State = anteResume.Shop;
            int maxSlots =
                window.ShopSlots > 0 ? window.ShopSlots : MotelyJamlyzerWindow.DefaultShopSlots(ante);
            MotelyItem[] shopItems = new MotelyItem[maxSlots];
            for (int i = 0; i < maxSlots; i++)
                shopItems[i] = ctx.GetNextShopItem(ref shopStream);

            // Packs
            var packStream = ctx.CreateBoosterPackStream(ante);
            int maxPacks = MotelyJamlyzerWindow.PackCount(ante);
            MotelyJamlyzerPack[] packs = new MotelyJamlyzerPack[maxPacks];

            // Soul detection rides its own per-ante streams, so the content stream above is untouched:
            // the same arcana/Spectral soul-only streams LegendarySoulMatcher walks, in the same slot
            // order, so a Soul found here is a Soul the scorer finds. The edition is the ante's own
            // edisou{ante}; the face comes from the run-global stream declared before the ante loop.
            MotelySingleTarotStream soulTarotStream = default;
            MotelySingleSpectralStream soulSpectralStream = default;
            bool soulTarotInit = false;
            bool soulSpectralInit = false;
            MotelySingleJokerFixedRarityStream soulEditionStream = default;
            bool soulEditionInit = false;

            for (int i = 0; i < maxPacks; i++)
            {
                MotelyBoosterPack pack = ctx.GetNextBoosterPack(ref packStream);
                MotelyItem[] contents = GetPackContents(ref ctx, ante, pack, ref state).AsArray();
                packs[i] = new(pack, contents);

                bool hasSoul;
                if (pack.GetPackType() == MotelyBoosterPackType.Arcana)
                {
                    if (!soulTarotInit)
                    {
                        soulTarotInit = true;
                        soulTarotStream = ctx.CreateArcanaPackTarotStream(ante, true);
                    }

                    hasSoul = ctx.GetNextArcanaPackHasTheSoul(
                        ref soulTarotStream,
                        pack.GetPackSize()
                    );
                }
                else if (pack.GetPackType() == MotelyBoosterPackType.Spectral)
                {
                    if (!soulSpectralInit)
                    {
                        soulSpectralInit = true;
                        // PerkeoObservatory: ante 1 Spectral uses soulOnly false; ante 2+ uses true.
                        soulSpectralStream = ctx.CreateSpectralPackSpectralStream(
                            ante,
                            soulOnly: ante != 1
                        );
                    }

                    hasSoul = ctx.GetNextSpectralPackHasTheSoul(
                        ref soulSpectralStream,
                        pack.GetPackSize()
                    );
                }
                else
                    continue;

                if (!hasSoul)
                    continue;
                if (ante == 0 && !window.OpensAnteZeroSouls)
                    continue;

                MotelyJoker face = ctx.GetNextLegendaryJoker(
                    ref souls.Faces,
                    souls.Held.AsSpan(0, souls.HeldCount),
                    heldState.ShowmanActive
                );
                souls.Own(face);
                if (!soulEditionInit)
                {
                    soulEditionInit = true;
                    soulEditionStream = ctx.CreateLegendaryJokerStream(
                        ante,
                        MotelyJokerFixedRarityStreamFlags.ExcludeJokerType
                    );
                }

                MotelyItemEdition edition = ctx.GetNextJoker(ref soulEditionStream).Edition;
                legendaryOrder.Add(new(ante, i, face, edition, new MotelyItem(face, edition)));
            }

            // pulls streams — card/joker-activated streams beyond shops/packs.
            var (pulls, pullStates) = CollectPulls(ref ctx, ante, voucherState, n, anteResume?.Pulls);

            // raw shop-source queues, read independently of the resolved shop above
            var (shopStreams, shopSourceStates) = CollectShopStreams(
                ref ctx,
                ante,
                n,
                anteResume?.ShopStreams
            );
            anteStates[ante - startAnte] = new(ante, shopStream.State, pullStates, shopSourceStates);

            // Activate voucher AFTER collecting pulls streams so voucher sequence
            // resampling uses the correct state (pre-activation for this ante).
            // Ante 0 reports its voucher without entering the chain — the search starts the
            // chain at ante 1, so the analyzer reports the same ante-1 voucher it matches.
            if (!isPreRunShop)
                voucherState.ActivateVoucher(voucher);

            antes[ante - startAnte] = new(
                ante,
                boss,
                voucher,
                smallTag,
                bigTag,
                shopItems,
                packs,
                pulls,
                shopStreams
            );
        }

        // The shop advances by the depth actually walked this window, which is ShopSlots when the
        // caller set one and otherwise the per-ante default -- ante 1 walks 15, ante 2+ walks 50.
        var (events, streamStates) = CollectEvents(
            ref ctx,
            n,
            window.ShopSlots,
            resumeFrom,
            (double)heldState.Luck
        );

        MotelyItem[]? erraticDeck = null;
        if (ctx.Deck == MotelyDeck.Erratic)
        {
            var deckStream = ctx.CreateErraticDeckPrngStream(isCached: false);
            erraticDeck = new MotelyItem[52];
            for (int i = 0; i < 52; i++)
                erraticDeck[i] = ctx.GetNextErraticDeckCard(ref deckStream);
        }

        return new(
            ctx.GetSeed(),
            0,
            antes,
            events,
            streamStates with { Antes = anteStates },
            legendaryOrder,
            erraticDeck
        );
    }

    /// <summary>
    /// The state the last window left for <paramref name="ante"/>, or null to start at the seed's
    /// natural start. <see cref="MotelyJamlyzer"/> refuses a bag that lacks an ante the JAML
    /// scopes before the walk runs; ante 0, which only some seeds reach, starts fresh if absent.
    /// </summary>
    private static MotelyJamlyzerAnteStreamStates? ResumeOf(
        MotelyJamlyzerStreamStates? resumeFrom,
        int ante
    )
    {
        if (resumeFrom?.Antes is { } states)
            foreach (var state in states)
                if (state.Ante == ante)
                    return state;
        return null;
    }

    private static (MotelyJamlyzerPulls, MotelyJamlyzerPullStreamStates?) CollectPulls(
        ref MotelySingleSearchContext ctx,
        int ante,
        MotelyRunState voucherState,
        int n,
        MotelyJamlyzerPullStreamStates? resume
    )
    {
        // Nothing to keep: building the streams would hash a dozen PRNG keys per ante to then
        // read none of them. `eventRolls: 0` is sold as the cheap shape, so make it cheap, and
        // hand the streams' positions on untouched.
        if (n == 0)
            return (new([], [], [], [], [], [], [], [], [], []), resume);

        // Joker streams
        var judgementStream = ctx.CreateJudgementJokerStream(ante);
        var wraithStream = ctx.CreateWraithJokerStream(ante);
        var riffRaffStream = ctx.CreateRiffRaffJokerStream(ante);
        var rareTagStream = ctx.CreateRareTagJokerStream(ante);
        var uncommonTagStream = ctx.CreateUncommonTagJokerStream(ante);

        // Tarot streams
        var emperorStream = ctx.CreateEmperorTarotStream(ante);
        var purpleSealStream = ctx.CreatePurpleSealTarotStream(ante);

        // Spectral streams
        var sixthSenseStream = ctx.CreateSixthSenseSpectralStream(ante);
        var seanceStream = ctx.CreateSeanceSpectralStream(ante);

        // Voucher sequence
        var voucherStream = ctx.CreateVoucherStream(ante);

        if (resume is not null)
        {
            judgementStream.State = resume.JudgementJokers;
            wraithStream.State = resume.WraithJokers;
            riffRaffStream.State = resume.RiffRaffJokers;
            rareTagStream.State = resume.RareTagJokers;
            uncommonTagStream.State = resume.UncommonTagJokers;
            emperorStream.State = resume.EmperorTarots;
            purpleSealStream.State = resume.PurpleSealTarots;
            sixthSenseStream.State = resume.SixthSenseSpectrals;
            seanceStream.State = resume.SeanceSpectrals;
            voucherStream.ResampleStream.State = resume.VoucherSequence;
        }

        MotelyItem[] judgement = new MotelyItem[n];
        MotelyItem[] wraith = new MotelyItem[n];
        MotelyItem[] riffRaff = new MotelyItem[n];
        MotelyItem[] rareTag = new MotelyItem[n];
        MotelyItem[] uncommonTag = new MotelyItem[n];
        MotelyItem[] emperor = new MotelyItem[n * 2]; // 2 tarots per use
        MotelyItem[] purpleSeal = new MotelyItem[n];
        MotelyItem[] sixthSense = new MotelyItem[n];
        MotelyItem[] seance = new MotelyItem[n];
        MotelyVoucher[] vouchers = new MotelyVoucher[n];

        for (int i = 0; i < n; i++)
        {
            judgement[i] = ctx.GetNextJoker(ref judgementStream);
            wraith[i] = ctx.GetNextJoker(ref wraithStream);
            riffRaff[i] = ctx.GetNextJoker(ref riffRaffStream);
            rareTag[i] = ctx.GetNextJoker(ref rareTagStream);
            uncommonTag[i] = ctx.GetNextJoker(ref uncommonTagStream);

            var e0 = ctx.GetNextTarot(ref emperorStream);
            emperor[i * 2] = e0;
            emperor[i * 2 + 1] = ctx.GetNextTarot(ref emperorStream, new(e0));

            purpleSeal[i] = ctx.GetNextTarot(ref purpleSealStream);
            sixthSense[i] = ctx.GetNextSpectral(ref sixthSenseStream);
            seance[i] = ctx.GetNextSpectral(ref seanceStream);
            vouchers[i] = ctx.GetNextVoucher(ref voucherStream, voucherState);
        }

        return (
            new(
                judgement,
                wraith,
                emperor,
                purpleSeal,
                sixthSense,
                seance,
                riffRaff,
                rareTag,
                uncommonTag,
                vouchers
            ),
            new(
                judgementStream.State,
                wraithStream.State,
                emperorStream.State,
                purpleSealStream.State,
                sixthSenseStream.State,
                seanceStream.State,
                riffRaffStream.State,
                rareTagStream.State,
                uncommonTagStream.State,
                voucherStream.ResampleStream.State
            )
        );
    }

    private static (MotelyJamlyzerShopStreams, MotelyJamlyzerShopSourceStreamStates?) CollectShopStreams(
        ref MotelySingleSearchContext ctx,
        int ante,
        int n,
        MotelyJamlyzerShopSourceStreamStates? resume
    )
    {
        // Same short-circuit as CollectPulls: no rolls to keep means the seven streams
        // below would be built only to be thrown away.
        if (n == 0)
            return (new([], [], [], [], [], [], []), resume);

        // Shop-source streams share the keys the shop item queue consumes, but each raw
        // queue is read on its own copy of stream state — collecting them here does not
        // perturb the resolved shop above. None depend on voucher run-state.
        var shopJokerStream = ctx.CreateShopJokerStream(ante);
        var commonJokerStream = ctx.CreateCommonShopJokerStream(ante);
        var uncommonJokerStream = ctx.CreateUncommonShopJokerStream(ante);
        var rareJokerStream = ctx.CreateRareShopJokerStream(ante);
        var shopTarotStream = ctx.CreateShopTarotStream(ante);
        var shopPlanetStream = ctx.CreateShopPlanetStream(ante);
        var shopSpectralStream = ctx.CreateShopSpectralStream(ante);

        if (resume is not null)
        {
            shopJokerStream.State = resume.ShopJokers;
            commonJokerStream.State = resume.CommonShopJokers;
            uncommonJokerStream.State = resume.UncommonShopJokers;
            rareJokerStream.State = resume.RareShopJokers;
            shopTarotStream.State = resume.ShopTarots;
            shopPlanetStream.State = resume.ShopPlanets;
            shopSpectralStream.State = resume.ShopSpectrals;
        }

        MotelyItem[] shopJokers = new MotelyItem[n];
        MotelyItem[] commonJokers = new MotelyItem[n];
        MotelyItem[] uncommonJokers = new MotelyItem[n];
        MotelyItem[] rareJokers = new MotelyItem[n];
        MotelyItem[] shopTarots = new MotelyItem[n];
        MotelyItem[] shopPlanets = new MotelyItem[n];
        MotelyItem[] shopSpectrals = new MotelyItem[n];

        for (int i = 0; i < n; i++)
        {
            shopJokers[i] = ctx.GetNextJoker(ref shopJokerStream);
            commonJokers[i] = ctx.GetNextJoker(ref commonJokerStream);
            uncommonJokers[i] = ctx.GetNextJoker(ref uncommonJokerStream);
            rareJokers[i] = ctx.GetNextJoker(ref rareJokerStream);
            shopTarots[i] = ctx.GetNextTarot(ref shopTarotStream);
            shopPlanets[i] = ctx.GetNextPlanet(ref shopPlanetStream);
            shopSpectrals[i] = ctx.GetNextSpectral(ref shopSpectralStream);
        }

        return (
            new(
                shopJokers,
                commonJokers,
                uncommonJokers,
                rareJokers,
                shopTarots,
                shopPlanets,
                shopSpectrals
            ),
            new(
                shopJokerStream.State,
                commonJokerStream.State,
                uncommonJokerStream.State,
                rareJokerStream.State,
                shopTarotStream.State,
                shopPlanetStream.State,
                shopSpectralStream.State
            )
        );
    }

    private static (MotelyJamlyzerEvents, MotelyJamlyzerStreamStates) CollectEvents(
        ref MotelySingleSearchContext ctx,
        int N,
        int shopSlots,
        MotelyJamlyzerStreamStates? resume,
        double luck
    )
    {
        // No resume bag -> each stream starts at the seed's natural start.
        // With a resume bag -> each stream resumes from its saved State double, so the
        // window continues exactly where the previous one stopped (no prefix re-roll).
        var luckyMoney = resume is null
            ? ctx.CreateLuckyCardMoneyStream()
            : ctx.ResumeStream(resume.LuckyMoney);
        var luckyMult = resume is null
            ? ctx.CreateLuckyCardMultStream()
            : ctx.ResumeStream(resume.LuckyMult);
        var wheel = resume is null
            ? ctx.CreateWheelOfFortuneStream()
            : ctx.ResumeStream(resume.WheelOfFortune);
        var cavendish = resume is null
            ? ctx.CreateCavendishPrngStream()
            : ctx.ResumeStream(resume.Cavendish);
        var grosMichel = resume is null
            ? ctx.CreateGrosMichelPrngStream()
            : ctx.ResumeStream(resume.GrosMichel);
        var space = resume is null
            ? ctx.CreateSpacePrngStream()
            : ctx.ResumeStream(resume.Space);
        var business = resume is null
            ? ctx.CreateBusinessPrngStream()
            : ctx.ResumeStream(resume.Business);
        var bloodstone = resume is null
            ? ctx.CreateBloodstonePrngStream()
            : ctx.ResumeStream(resume.Bloodstone);
        var parking = resume is null
            ? ctx.CreateParkingPrngStream()
            : ctx.ResumeStream(resume.Parking);
        var eightBall = resume is null
            ? ctx.CreateEightBallPrngStream()
            : ctx.ResumeStream(resume.EightBall);
        var glass = resume is null
            ? ctx.CreateGlassPrngStream()
            : ctx.ResumeStream(resume.Glass);
        var omenGlobe = resume is null
            ? ctx.CreateOmenGlobePrngStream()
            : ctx.ResumeStream(resume.OmenGlobe);
        var theWheel = resume is null
            ? ctx.CreateTheWheelPrngStream()
            : ctx.ResumeStream(resume.TheWheel);
        var misprint = resume is null
            ? ctx.CreateMisprintPrngStream()
            : ctx.ResumeStream(resume.Misprint);

        bool[] luckyMoneyRolls = new bool[N];
        bool[] luckyMultRolls = new bool[N];
        MotelyItemEdition[] wheelRolls = new MotelyItemEdition[N];
        bool[] cavendishRolls = new bool[N];
        bool[] grosMichelRolls = new bool[N];
        bool[] spaceRolls = new bool[N];
        bool[] businessRolls = new bool[N];
        bool[] bloodstoneRolls = new bool[N];
        bool[] parkingRolls = new bool[N];
        bool[] eightBallRolls = new bool[N];
        bool[] glassRolls = new bool[N];
        bool[] omenGlobeRolls = new bool[N];
        bool[] theWheelRolls = new bool[N];
        int[] misprintRolls = new int[N];

        // Luck weighs the eight events whose clauses take with: { luck } (LuckEventClause), as
        // their scoring walks do. The rest have no with: and stay at base odds.
        for (int i = 0; i < N; i++)
        {
            luckyMoneyRolls[i] = ctx.GetNextLuckyMoney(ref luckyMoney, luck);
            luckyMultRolls[i] = ctx.GetNextLuckyMult(ref luckyMult, luck);
            wheelRolls[i] = ctx.GetNextWheelOfFortune(ref wheel, luck);
            cavendishRolls[i] = ctx.GetNextCavendishExtinct(ref cavendish, luck);
            grosMichelRolls[i] = ctx.GetNextGrosMichelExtinct(ref grosMichel, luck);
            spaceRolls[i] = ctx.GetNextSpaceLevelup(ref space, luck);
            businessRolls[i] = ctx.GetNextBusinessPayout(ref business);
            bloodstoneRolls[i] = ctx.GetNextBloodstoneTrigger(ref bloodstone);
            parkingRolls[i] = ctx.GetNextParkingPayout(ref parking);
            eightBallRolls[i] = ctx.GetNextEightBallTarot(ref eightBall);
            glassRolls[i] = ctx.GetNextGlassDestroy(ref glass, luck);
            omenGlobeRolls[i] = ctx.GetNextOmenGlobeSpectral(ref omenGlobe);
            theWheelRolls[i] = ctx.GetNextWheelStaysFlipped(ref theWheel, luck);
            misprintRolls[i] = ctx.GetNextMisprintMult(ref misprint);
        }

        var events = new MotelyJamlyzerEvents(
            luckyMoneyRolls,
            luckyMultRolls,
            wheelRolls,
            cavendishRolls,
            grosMichelRolls,
            spaceRolls,
            businessRolls,
            bloodstoneRolls,
            parkingRolls,
            eightBallRolls,
            glassRolls,
            omenGlobeRolls,
            theWheelRolls,
            misprintRolls
        );

        // End-of-window state bag — hand straight back as resumeFrom. The doubles are where the
        // event streams stand; the walk adds the per-ante stream states. The three counters only
        // say how far the scroll has gone: N more rolls, and ShopSlots more shop items or one
        // more default-depth window.
        var states = new MotelyJamlyzerStreamStates(
            (resume?.RollOffset ?? 0) + N,
            (resume?.ShopOffset ?? 0) + shopSlots,
            luckyMoney.State,
            luckyMult.State,
            wheel.State,
            cavendish.State,
            grosMichel.State,
            space.State,
            business.State,
            bloodstone.State,
            parking.State,
            eightBall.State,
            glass.State,
            omenGlobe.State,
            theWheel.State,
            misprint.State,
            (resume?.ShopDefaultWindows ?? 0) + (shopSlots > 0 ? 0 : 1)
        );

        return (events, states);
    }

    private static MotelySingleItemSet GetPackContents(
        ref MotelySingleSearchContext ctx,
        int ante,
        MotelyBoosterPack pack,
        ref AnteAnalysisState state
    )
    {
        var packType = pack.GetPackType();
        var packSize = pack.GetPackSize();

        switch (packType)
        {
            case MotelyBoosterPackType.Arcana:
                if (!state.HasArcanaStream)
                    state.ArcanaStream = ctx.CreateArcanaPackTarotStream(ante);
                return ctx.GetNextArcanaPackContents(ref state.ArcanaStream, packSize);

            case MotelyBoosterPackType.Celestial:
                if (!state.HasCelestialStream)
                    state.CelestialStream = ctx.CreateCelestialPackPlanetStream(ante);
                return ctx.GetNextCelestialPackContents(ref state.CelestialStream, packSize);

            case MotelyBoosterPackType.Spectral:
                if (!state.HasSpectralStream)
                    state.SpectralStream = ctx.CreateSpectralPackSpectralStream(ante);
                return ctx.GetNextSpectralPackContents(ref state.SpectralStream, packSize);

            case MotelyBoosterPackType.Buffoon:
                if (!state.HasBuffoonStream)
                    state.BuffoonStream = ctx.CreateBuffoonPackJokerStream(ante);
                return ctx.GetNextBuffoonPackContents(ref state.BuffoonStream, packSize);

            case MotelyBoosterPackType.Standard:
                if (!state.HasStandardStream)
                    state.StandardStream = ctx.CreateStandardPackCardStream(ante);
                return ctx.GetNextStandardPackContents(ref state.StandardStream, packSize);

            default:
                throw new InvalidEnumArgumentException();
        }
    }
}
