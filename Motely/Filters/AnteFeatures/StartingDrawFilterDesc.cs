using System.Runtime.CompilerServices;

namespace Motely.Filters;

[YamlDotNet.Serialization.YamlSerializable]
public sealed partial class StartingDrawClause: IMotelyClause, IAnteScopedClause
{
    public MotelyStandardCard? StartingDraw
    {
        get => Card;
        set => Card = value;
    }

    /// <summary><c>ante: 2</c> or <c>ante: [1, 2]</c> is <c>antes:</c>.</summary>
    public int[]? Ante { get => null; set => Antes = value ?? Antes; }

    public string? Label { get; set; }
    public int Min { get; set; } = 1;
    public int? Max { get; set; }
    public int Score { get; set; } = 1;
    public int[] Antes { get; set; } = [1, 2, 3, 4, 5, 6, 7, 8];

    /// <summary>A whole card (<c>startingDraw: AceOfSpades</c>): its rank and suit at once.</summary>
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

    public IMotelySeedFilterDesc CreateFilterDesc() => new StartingDrawFilterDesc(this);
}

public struct StartingDrawFilterDesc(StartingDrawClause clause)
    : IMotelySeedFilterDesc<StartingDrawFilterDesc.StartingDrawFilter>
{
    private readonly StartingDrawClause _clause = clause;

    public StartingDrawFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        return new StartingDrawFilter(_clause);
    }

    public struct StartingDrawFilter(StartingDrawClause clause) : IMotelySeedFilter
    {
        private readonly StartingDrawClause _clause = clause;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorMask Filter(ref MotelyVectorSearchContext ctx)
        {
            // Single match core: same CountStartingDrawOccurrences as should-scoring.
            var clause = _clause;
            return ctx.SearchIndividualSeeds(
                (MotelySingleSearchContext singleCtx) =>
                    ClauseScoring.ClauseMeetsMinForFilter(ref singleCtx, clause) ? 1 : 0
            );
        }
    }
}
