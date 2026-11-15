using System.Runtime.CompilerServices;

namespace Motely.Filters;

[YamlDotNet.Serialization.YamlSerializable]
public sealed partial class StandardCardClause: IMotelyClause, IAnteScopedClause, IWithScopedClause
{
    public JamlStandardCardSlot? StandardCard
    {
        get => null;
        set => ApplyStandardCardSlot(value);
    }

    public JamlStandardCardSlot? StandardCards
    {
        get => null;
        set => ApplyStandardCardSlot(value);
    }

    private void ApplyStandardCardSlot(JamlStandardCardSlot? slot)
    {
        if (slot is null)
            return;
        if (slot.Card is { } card)
            Card = card;
        if (slot.Rank is { } rank)
            Rank = rank;
        if (slot.Suit is { } suit)
            Suit = suit;
        if (slot.Enhancement is { } enhancement)
            Enhancement = enhancement;
        if (slot.Seal is { } seal)
            Seal = seal;
        if (slot.Edition is { } edition)
            Edition = edition;
    }

    /// <summary><c>ante: 2</c> or <c>ante: [1, 2]</c> is <c>antes:</c>.</summary>
    public int[]? Ante { get => null; set => Antes = value ?? Antes; }

    public string? Label { get; set; }
    public int Min { get; set; } = 1;
    public int? Max { get; set; }
    public int Score { get; set; } = 1;
    public int[] Antes { get; set; } = [1, 2, 3, 4, 5, 6, 7, 8];

    /// <summary>A whole card (<c>standardCard: AceOfSpades</c>): its rank and suit at once.</summary>
    public MotelyStandardCard? Card
    {
        get;
        set
        {
            field = value;
            if (value is { } card)
            {
                Rank = card.GetRank();
                Suit = card.GetSuit();
            }
        }
    }
    public MotelyStandardcardRank? Rank { get; set; }
    public MotelyStandardcardSuit? Suit { get; set; }
    public MotelyItemEnhancement? Enhancement { get; set; }
    public MotelyItemSeal? Seal { get; set; }
    public MotelyItemEdition? Edition { get; set; }

    // null = no sources: in JAML → filter DefaultSources at CreateFilter/score (not parse).
    public StandardCardSourceConfig? Sources { get; set; }
    public MotelyWith With { get; set; } = new();

    public IMotelySeedFilterDesc CreateFilterDesc() => new StandardCardFilterDesc(this);
}

public struct StandardCardFilterDesc(StandardCardClause clause)
    : IMotelySeedFilterDesc<StandardCardFilterDesc.StandardCardFilter>
{
    private readonly StandardCardClause _clause = clause;

    /// <summary>
    /// Filter-layer default when Sources is null: every booster-pack slot, Standard packs only.
    /// The shop is left out because its playing-card weight is the Magic Trick weight and no deck
    /// starts with that voucher (<see cref="JamlRarityContext.ShopStandardCardRate"/>), so a
    /// shop-only default matched nothing on every deck. Shop slots need an explicit
    /// <c>sources:</c>. Slot range is the engine's
    /// <see cref="MotelyGlobals.LateAntesMaxPackSlot"/>; scoring clamps ante 1 to its four.
    /// </summary>
    internal static readonly StandardCardSourceConfig DefaultSources = new()
    {
        BoosterPacks = Enumerable.Range(0, MotelyGlobals.LateAntesMaxPackSlot + 1).ToArray(),
    };

    public StandardCardFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        var sources = _clause.Sources ?? DefaultSources;
        foreach (var ante in _clause.Antes)
        {
            if (sources.ShopItems.Length > 0)
                ctx.CacheShopStream(ante);
            if (sources.BoosterPacks.Length > 0)
                ctx.CacheBoosterPackStream(ante);
        }

        return new StandardCardFilter(_clause);
    }

    public struct StandardCardFilter(StandardCardClause clause) : IMotelySeedFilter
    {
        private readonly StandardCardClause _clause = clause;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorMask Filter(ref MotelyVectorSearchContext ctx)
        {
            // Single match core: same PrepareRunState + count as should-scoring.
            var clause = _clause;
            return ctx.SearchIndividualSeeds(
                (MotelySingleSearchContext singleCtx) =>
                    ClauseScoring.ClauseMeetsMinForFilter(ref singleCtx, clause) ? 1 : 0
            );
        }
    }
}

/// <summary>
/// <c>sources:</c> block for <c>standardCard:</c>. Colocated with <see cref="StandardCardFilterDesc"/> (T5).
/// </summary>
[YamlDotNet.Serialization.YamlSerializable]
public sealed partial record StandardCardSourceConfig
{
    /// <summary><c>requireMega:</c> is <c>requireMegaPack:</c>.</summary>
    public bool? RequireMega { get => null; set => RequireMegaPack = value ?? RequireMegaPack; }

    public int[] ShopItems { get; set => field = value ?? []; } = [];
    public int[] BoosterPacks { get; set => field = value ?? []; } = [];

    /// <summary>When true, only Mega-sized Standard packs count (Normal/Jumbo still advance the stream).</summary>
    public bool RequireMegaPack { get; set; }
}
