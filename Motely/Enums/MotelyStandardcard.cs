using System.Runtime.CompilerServices;

namespace Motely.Enums;

public enum MotelyStandardcardRank
{
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King,
    Ace,
}

public static class MotelyStandardcardRankExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOrdinal(this MotelyStandardcardRank rank)
    {
        return (int)rank;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MotelyStandardcardRank FromOrdinal(int ordinal)
    {
        return (MotelyStandardcardRank)ordinal;
    }
}

public enum MotelyStandardcardSuit
{
    Clubs = 0b00 << MotelyGlobals.StandardcardSuitOffset,
    Diamonds = 0b01 << MotelyGlobals.StandardcardSuitOffset,
    Hearts = 0b10 << MotelyGlobals.StandardcardSuitOffset,
    Spades = 0b11 << MotelyGlobals.StandardcardSuitOffset,
}

public static class MotelyStandardcardSuitExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOrdinal(this MotelyStandardcardSuit suit)
    {
        return (int)suit >> MotelyGlobals.StandardcardSuitOffset;
    }
}

public enum MotelyStandardCard
{
    TwoOfClubs = MotelyStandardcardSuit.Clubs | MotelyStandardcardRank.Two,
    ThreeOfClubs = MotelyStandardcardSuit.Clubs | MotelyStandardcardRank.Three,
    FourOfClubs = MotelyStandardcardSuit.Clubs | MotelyStandardcardRank.Four,
    FiveOfClubs = MotelyStandardcardSuit.Clubs | MotelyStandardcardRank.Five,
    SixOfClubs = MotelyStandardcardSuit.Clubs | MotelyStandardcardRank.Six,
    SevenOfClubs = MotelyStandardcardSuit.Clubs | MotelyStandardcardRank.Seven,
    EightOfClubs = MotelyStandardcardSuit.Clubs | MotelyStandardcardRank.Eight,
    NineOfClubs = MotelyStandardcardSuit.Clubs | MotelyStandardcardRank.Nine,
    TenOfClubs = MotelyStandardcardSuit.Clubs | MotelyStandardcardRank.Ten,
    JackOfClubs = MotelyStandardcardSuit.Clubs | MotelyStandardcardRank.Jack,
    QueenOfClubs = MotelyStandardcardSuit.Clubs | MotelyStandardcardRank.Queen,
    KingOfClubs = MotelyStandardcardSuit.Clubs | MotelyStandardcardRank.King,
    AceOfClubs = MotelyStandardcardSuit.Clubs | MotelyStandardcardRank.Ace,

    TwoOfDiamonds = MotelyStandardcardSuit.Diamonds | MotelyStandardcardRank.Two,
    ThreeOfDiamonds = MotelyStandardcardSuit.Diamonds | MotelyStandardcardRank.Three,
    FourOfDiamonds = MotelyStandardcardSuit.Diamonds | MotelyStandardcardRank.Four,
    FiveOfDiamonds = MotelyStandardcardSuit.Diamonds | MotelyStandardcardRank.Five,
    SixOfDiamonds = MotelyStandardcardSuit.Diamonds | MotelyStandardcardRank.Six,
    SevenOfDiamonds = MotelyStandardcardSuit.Diamonds | MotelyStandardcardRank.Seven,
    EightOfDiamonds = MotelyStandardcardSuit.Diamonds | MotelyStandardcardRank.Eight,
    NineOfDiamonds = MotelyStandardcardSuit.Diamonds | MotelyStandardcardRank.Nine,
    TenOfDiamonds = MotelyStandardcardSuit.Diamonds | MotelyStandardcardRank.Ten,
    JackOfDiamonds = MotelyStandardcardSuit.Diamonds | MotelyStandardcardRank.Jack,
    QueenOfDiamonds = MotelyStandardcardSuit.Diamonds | MotelyStandardcardRank.Queen,
    KingOfDiamonds = MotelyStandardcardSuit.Diamonds | MotelyStandardcardRank.King,
    AceOfDiamonds = MotelyStandardcardSuit.Diamonds | MotelyStandardcardRank.Ace,

    TwoOfHearts = MotelyStandardcardSuit.Hearts | MotelyStandardcardRank.Two,
    ThreeOfHearts = MotelyStandardcardSuit.Hearts | MotelyStandardcardRank.Three,
    FourOfHearts = MotelyStandardcardSuit.Hearts | MotelyStandardcardRank.Four,
    FiveOfHearts = MotelyStandardcardSuit.Hearts | MotelyStandardcardRank.Five,
    SixOfHearts = MotelyStandardcardSuit.Hearts | MotelyStandardcardRank.Six,
    SevenOfHearts = MotelyStandardcardSuit.Hearts | MotelyStandardcardRank.Seven,
    EightOfHearts = MotelyStandardcardSuit.Hearts | MotelyStandardcardRank.Eight,
    NineOfHearts = MotelyStandardcardSuit.Hearts | MotelyStandardcardRank.Nine,
    TenOfHearts = MotelyStandardcardSuit.Hearts | MotelyStandardcardRank.Ten,
    JackOfHearts = MotelyStandardcardSuit.Hearts | MotelyStandardcardRank.Jack,
    QueenOfHearts = MotelyStandardcardSuit.Hearts | MotelyStandardcardRank.Queen,
    KingOfHearts = MotelyStandardcardSuit.Hearts | MotelyStandardcardRank.King,
    AceOfHearts = MotelyStandardcardSuit.Hearts | MotelyStandardcardRank.Ace,

    TwoOfSpades = MotelyStandardcardSuit.Spades | MotelyStandardcardRank.Two,
    ThreeOfSpades = MotelyStandardcardSuit.Spades | MotelyStandardcardRank.Three,
    FourOfSpades = MotelyStandardcardSuit.Spades | MotelyStandardcardRank.Four,
    FiveOfSpades = MotelyStandardcardSuit.Spades | MotelyStandardcardRank.Five,
    SixOfSpades = MotelyStandardcardSuit.Spades | MotelyStandardcardRank.Six,
    SevenOfSpades = MotelyStandardcardSuit.Spades | MotelyStandardcardRank.Seven,
    EightOfSpades = MotelyStandardcardSuit.Spades | MotelyStandardcardRank.Eight,
    NineOfSpades = MotelyStandardcardSuit.Spades | MotelyStandardcardRank.Nine,
    TenOfSpades = MotelyStandardcardSuit.Spades | MotelyStandardcardRank.Ten,
    JackOfSpades = MotelyStandardcardSuit.Spades | MotelyStandardcardRank.Jack,
    QueenOfSpades = MotelyStandardcardSuit.Spades | MotelyStandardcardRank.Queen,
    KingOfSpades = MotelyStandardcardSuit.Spades | MotelyStandardcardRank.King,
    AceOfSpades = MotelyStandardcardSuit.Spades | MotelyStandardcardRank.Ace,
}

public static class MotelyStandardCardExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MotelyStandardcardSuit GetSuit(this MotelyStandardCard card)
    {
        return (MotelyStandardcardSuit)((int)card & MotelyGlobals.StandardcardSuitMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MotelyStandardcardRank GetRank(this MotelyStandardCard card)
    {
        return (MotelyStandardcardRank)((int)card & MotelyGlobals.StandardcardRankMask);
    }

    /// <summary>
    /// Balatro <c>G.P_CARDS</c> iterates ranks in ASCII character order:
    /// <c>'2'..'9', 'A', 'J', 'K', 'Q', 'T'</c> (indices 0..12 within each suit).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAsciiRankOrdinal(this MotelyStandardcardRank rank) =>
        rank switch
        {
            MotelyStandardcardRank.Ace => 8,
            MotelyStandardcardRank.Jack => 9,
            MotelyStandardcardRank.King => 10,
            MotelyStandardcardRank.Queen => 11,
            MotelyStandardcardRank.Ten => 12,
            _ => (int)rank, // Two=0 .. Nine=7
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAsciiIndex(this MotelyStandardCard card) =>
        card.GetSuit().GetOrdinal() * 13 + card.GetRank().GetAsciiRankOrdinal();
}

/// <summary>
/// Canonical Balatro playing-card pools matching <c>G.P_CARDS</c> (<c>game.lua:2340-2373</c>)
/// and <c>Back:apply_to_run</c> (<c>back.lua:249-256</c>) without altering the public
/// <see cref="MotelyStandardcardRank"/> integer ABI (<c>Two=0 .. Ace=12</c>).
/// </summary>
public static class MotelyStandardCardPools
{
    private static readonly MotelyStandardcardRank[] AsciiRanks =
    [
        MotelyStandardcardRank.Two,
        MotelyStandardcardRank.Three,
        MotelyStandardcardRank.Four,
        MotelyStandardcardRank.Five,
        MotelyStandardcardRank.Six,
        MotelyStandardcardRank.Seven,
        MotelyStandardcardRank.Eight,
        MotelyStandardcardRank.Nine,
        MotelyStandardcardRank.Ace,
        MotelyStandardcardRank.Jack,
        MotelyStandardcardRank.King,
        MotelyStandardcardRank.Queen,
        MotelyStandardcardRank.Ten,
    ];

    private static readonly MotelyStandardcardSuit[] AsciiSuits =
    [
        MotelyStandardcardSuit.Clubs,
        MotelyStandardcardSuit.Diamonds,
        MotelyStandardcardSuit.Hearts,
        MotelyStandardcardSuit.Spades,
    ];

    /// <summary>
    /// Balatro's 52-card <c>G.P_CARDS</c> table sorted by key (<c>C_2..C_9, C_A, C_J, C_K, C_Q, C_T, D_..., H_..., S_...</c>).
    /// Used by <c>pseudorandom_element(G.P_CARDS, ...)</c> for Erratic deck, Standard packs, and Magic Trick shop cards.
    /// Note that index 51 is <see cref="MotelyStandardCard.TenOfSpades"/> (<c>S_T</c>).
    /// </summary>
    public static readonly MotelyStandardCard[] PCards = BuildPCards();

    /// <summary>Abandoned Deck starting cards (40 cards: J, Q, K omitted, sorted by <c>s..r</c>).</summary>
    public static readonly MotelyStandardCard[] AbandonedDeckCards = BuildAbandonedDeckCards();

    /// <summary>
    /// Checkered Deck starting cards (52 cards: <c>G.P_CARDS</c> <c>sort_id</c> 1..52 with
    /// <c>Clubs -&gt; Spades</c> and <c>Diamonds -&gt; Hearts</c> mutated in place by <c>Back:apply_to_run</c>).
    /// </summary>
    public static readonly MotelyStandardCard[] CheckeredDeckCards = BuildCheckeredDeckCards();

    private static MotelyStandardCard[] BuildPCards()
    {
        var cards = new MotelyStandardCard[52];
        int idx = 0;
        foreach (var suit in AsciiSuits)
        {
            foreach (var rank in AsciiRanks)
                cards[idx++] = (MotelyStandardCard)((int)suit | (int)rank);
        }
        return cards;
    }

    private static MotelyStandardCard[] BuildAbandonedDeckCards()
    {
        var cards = new MotelyStandardCard[40];
        int idx = 0;
        foreach (var suit in AsciiSuits)
        {
            foreach (var rank in AsciiRanks)
            {
                if (
                    rank
                    is MotelyStandardcardRank.Jack
                        or MotelyStandardcardRank.Queen
                        or MotelyStandardcardRank.King
                )
                    continue;
                cards[idx++] = (MotelyStandardCard)((int)suit | (int)rank);
            }
        }
        return cards;
    }

    private static MotelyStandardCard[] BuildCheckeredDeckCards()
    {
        var cards = new MotelyStandardCard[52];
        for (int i = 0; i < PCards.Length; i++)
        {
            var suit = PCards[i].GetSuit();
            var rank = PCards[i].GetRank();
            if (suit == MotelyStandardcardSuit.Clubs)
                suit = MotelyStandardcardSuit.Spades;
            else if (suit == MotelyStandardcardSuit.Diamonds)
                suit = MotelyStandardcardSuit.Hearts;
            cards[i] = (MotelyStandardCard)((int)suit | (int)rank);
        }
        return cards;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetStartingHandSize(MotelyDeck deck) =>
        deck == MotelyDeck.Painted ? 10 : 8;

    /// <summary>
    /// Builds the pre-shuffle starting deck (<c>sort_id</c> order) for <paramref name="ctx"/>'s deck,
    /// matching <c>Game:start_run</c> (<c>game.lua:2340-2373</c>) and <c>Back:apply_to_run</c>.
    /// </summary>
    public static MotelyItem[] BuildStartingDeck(ref MotelySingleSearchContext ctx)
    {
        switch (ctx.Deck)
        {
            case MotelyDeck.Abandoned:
            {
                var deck = new MotelyItem[AbandonedDeckCards.Length];
                for (int i = 0; i < deck.Length; i++)
                    deck[i] = new(AbandonedDeckCards[i]);
                return deck;
            }

            case MotelyDeck.Checkered:
            {
                var deck = new MotelyItem[CheckeredDeckCards.Length];
                for (int i = 0; i < deck.Length; i++)
                    deck[i] = new(CheckeredDeckCards[i]);
                return deck;
            }

            case MotelyDeck.Erratic:
            {
                var stream = ctx.CreateErraticDeckPrngStream(isCached: false);
                Span<int> keys = stackalloc int[52];
                var deck = new MotelyItem[52];
                for (int i = 0; i < 52; i++)
                {
                    var item = ctx.GetNextErraticDeckCard(ref stream);
                    deck[i] = item;
                    keys[i] = item.StandardcardSuit.GetOrdinal() * 13 + item.StandardcardRank.GetAsciiRankOrdinal();
                }
                keys.Sort(deck.AsSpan());
                return deck;
            }

            default:
            {
                var deck = new MotelyItem[PCards.Length];
                for (int i = 0; i < deck.Length; i++)
                    deck[i] = new(PCards[i]);
                return deck;
            }
        }
    }
}

