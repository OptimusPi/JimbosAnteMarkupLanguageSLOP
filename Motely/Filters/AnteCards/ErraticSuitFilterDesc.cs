using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely.Filters;

[YamlDotNet.Serialization.YamlSerializable]
public sealed partial class ErraticSuitClause: IMotelyClause, IAnteScopedClause
{
    public MotelyStandardcardSuit? ErraticSuit
    {
        get => Suit;
        set => Suit = value ?? default;
    }

    public MotelyStandardcardSuit? ErraticSuits
    {
        get => Suit;
        set => Suit = value ?? default;
    }

    /// <summary><c>ante: 2</c> or <c>ante: [1, 2]</c> is <c>antes:</c>.</summary>
    public int[]? Ante { get => null; set => Antes = value ?? Antes; }

    public string? Label { get; set; }
    public int Min { get; set; } = 1;
    public int? Max { get; set; }
    public int Score { get; set; } = 1;
    public int[] Antes { get; set; } = [1, 2, 3, 4, 5, 6, 7, 8];
    public MotelyStandardcardSuit Suit { get; set; }

    /// <summary>How many of <paramref name="suit"/> a non-Erratic starting deck holds.</summary>
    internal static int FixedDeckCount(MotelyDeck deck, MotelyStandardcardSuit suit) =>
        deck switch
        {
            MotelyDeck.Abandoned => 10,
            MotelyDeck.Checkered => suit is MotelyStandardcardSuit.Spades or MotelyStandardcardSuit.Hearts ? 26 : 0,
            _ => 13,
        };

    public IMotelySeedFilterDesc CreateFilterDesc() => new ErraticSuitFilterDesc(this);
}

public struct ErraticSuitFilterDesc(ErraticSuitClause clause)
    : IMotelySeedFilterDesc<ErraticSuitFilterDesc.ErraticSuitFilter>
{
    private readonly ErraticSuitClause _clause = clause;

    public ErraticSuitFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        ctx.CacheErraticDeckPrngStream();
        return new ErraticSuitFilter(_clause);
    }

    public struct ErraticSuitFilter(ErraticSuitClause clause) : IMotelySeedFilter
    {
        private readonly ErraticSuitClause _clause = clause;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorMask Filter(ref MotelyVectorSearchContext ctx)
        {
            var clause = _clause;
            Vector256<int> count = Vector256<int>.Zero;
            var minVec = Vector256.Create(clause.Min);
            if (ctx.Deck != MotelyDeck.Erratic)
                return Vector256.GreaterThanOrEqual(
                    Vector256.Create(ErraticSuitClause.FixedDeckCount(ctx.Deck, clause.Suit)),
                    minVec
                );
            var stream = ctx.CreateErraticDeckPrngStream(true);
            for (int cardIndex = 0; cardIndex < 52; cardIndex++)
            {
                var card = ctx.GetNextErraticDeckCard(ref stream);
                count += Vector256.ConditionalSelect(
                    VectorEnum256.Equals(card.StandardcardSuit, clause.Suit),
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
