using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely.Filters;

[YamlDotNet.Serialization.YamlSerializable]
public sealed partial class ErraticRankClause: IMotelyClause, IAnteScopedClause
{
    public MotelyStandardcardRank? ErraticRank
    {
        get => Rank;
        set => Rank = value ?? default;
    }

    public MotelyStandardcardRank? ErraticRanks
    {
        get => Rank;
        set => Rank = value ?? default;
    }

    /// <summary><c>ante: 2</c> or <c>ante: [1, 2]</c> is <c>antes:</c>.</summary>
    public int[]? Ante { get => null; set => Antes = value ?? Antes; }

    public string? Label { get; set; }
    public int Min { get; set; } = 1;
    public int? Max { get; set; }
    public int Score { get; set; } = 1;
    public int[] Antes { get; set; } = [1, 2, 3, 4, 5, 6, 7, 8];
    public MotelyStandardcardRank Rank { get; set; }

    /// <summary>How many of <paramref name="rank"/> a non-Erratic starting deck holds.</summary>
    internal static int FixedDeckCount(MotelyDeck deck, MotelyStandardcardRank rank) =>
        deck == MotelyDeck.Abandoned
        && rank is MotelyStandardcardRank.Jack or MotelyStandardcardRank.Queen or MotelyStandardcardRank.King
            ? 0
            : 4;

    public IMotelySeedFilterDesc CreateFilterDesc() => new ErraticRankFilterDesc(this);
}

public struct ErraticRankFilterDesc(ErraticRankClause clause)
    : IMotelySeedFilterDesc<ErraticRankFilterDesc.ErraticRankFilter>
{
    private readonly ErraticRankClause _clause = clause;

    public ErraticRankFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        ctx.CacheErraticDeckPrngStream();
        return new ErraticRankFilter(_clause);
    }

    public struct ErraticRankFilter(ErraticRankClause clause) : IMotelySeedFilter
    {
        private readonly ErraticRankClause _clause = clause;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorMask Filter(ref MotelyVectorSearchContext ctx)
        {
            var clause = _clause;
            Vector256<int> count = Vector256<int>.Zero;
            var minVec = Vector256.Create(clause.Min);
            if (ctx.Deck != MotelyDeck.Erratic)
                return Vector256.GreaterThanOrEqual(
                    Vector256.Create(ErraticRankClause.FixedDeckCount(ctx.Deck, clause.Rank)),
                    minVec
                );
            var stream = ctx.CreateErraticDeckPrngStream(true);
            for (int cardIndex = 0; cardIndex < 52; cardIndex++)
            {
                var card = ctx.GetNextErraticDeckCard(ref stream);
                count += Vector256.ConditionalSelect(
                    VectorEnum256.Equals(card.StandardcardRank, clause.Rank),
                    Vector256<int>.One,
                    Vector256<int>.Zero
                );
                if (
                    Vector256.GreaterThanOrEqual(count, minVec).ExtractMostSignificantBits() == 0xFF
                )
                    break;
            }
            return Vector256.GreaterThanOrEqual(count, minVec);
        }
    }
}
