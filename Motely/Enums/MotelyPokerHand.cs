namespace Motely.Enums;

/// <summary>
/// Poker hand categories for starting-hand / shuffle evaluation.
/// Category order, weakest first, as Balatro ranks hands; <see cref="Motely.MotelyPokerHandEval"/> names the highest present.
/// </summary>
public enum MotelyPokerHand
{
    HighCard,
    Pair,
    TwoPair,
    ThreeOfAKind,
    Straight,
    Flush,
    FullHouse,
    FourOfAKind,
    StraightFlush,
}
