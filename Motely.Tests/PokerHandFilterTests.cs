namespace Motely.Tests;

/// <summary>
/// Seed-proof for <c>pokerHand:</c> — list-search only, seeds pinned by CLI --collect.
/// </summary>
public sealed class PokerHandFilterTests
{
    private const string FourOfAKind = """
        name: poker-quads
        deck: Red
        stake: White
        must:
          - pokerHand: FourOfAKind
            antes: [1]
        """;

    private const string StraightFlush = """
        name: poker-sf
        deck: Red
        stake: White
        must:
          - pokerHand: StraightFlush
            antes: [1]
        """;

    private const string PairOrBetter = """
        name: poker-pair-up
        deck: Red
        stake: White
        must:
          - pokerHand: [Pair, TwoPair, ThreeOfAKind, Straight, Flush, FullHouse, FourOfAKind, StraightFlush]
            antes: [1]
        """;

    [Fact]
    public void LoadsAndDiscriminates()
    {
        Assert.True(JamlConfig.TryLoad(FourOfAKind, out var config, out var error), error);
        Assert.NotNull(config);
        var clause = Assert.IsType<PokerHandClause>(config.Must[0]);
        Assert.Equal([MotelyPokerHand.FourOfAKind], clause.PokerHands);
        Assert.Equal([1], clause.Antes);
    }

    [Fact]
    public void FourOfAKind_KnownSeedsMatch()
    {
        ProofSearch.MustMatchAll(FourOfAKind, "5S5", "D7D", "7I7", "KEK", "K5K");
    }

    [Fact]
    public void StraightFlush_KnownSeedsMatch()
    {
        ProofSearch.MustMatchAll(StraightFlush, "4K4", "9", "GGG");
    }

    [Fact]
    public void PairOrBetter_MatchesKnownQuadSeed()
    {
        // Quads imply Pair-or-better list membership for BestScore type.
        ProofSearch.MustMatchAll(PairOrBetter, "5S5");
    }

    [Fact]
    public void BestScore_HigherHandCategoryBeatsHigherRawChipScore()
    {
        static MotelyItem Card(MotelyStandardcardSuit suit, MotelyStandardcardRank rank) =>
            new((MotelyItemType)((int)MotelyItemTypeCategory.Standardcard | (int)suit | (int)rank));

        // 8-card hand holding BOTH an Ace-high Flush (A-K-Q-3-2 of Spades = 284 score)
        // AND a low Full House (2-2-2 + 3-3 = 208 score). Balatro chooses FullHouse.
        Span<MotelyItem> eightCards =
        [
            Card(MotelyStandardcardSuit.Spades, MotelyStandardcardRank.Ace),
            Card(MotelyStandardcardSuit.Spades, MotelyStandardcardRank.King),
            Card(MotelyStandardcardSuit.Spades, MotelyStandardcardRank.Queen),
            Card(MotelyStandardcardSuit.Spades, MotelyStandardcardRank.Two),
            Card(MotelyStandardcardSuit.Spades, MotelyStandardcardRank.Three),
            Card(MotelyStandardcardSuit.Hearts, MotelyStandardcardRank.Two),
            Card(MotelyStandardcardSuit.Clubs, MotelyStandardcardRank.Two),
            Card(MotelyStandardcardSuit.Diamonds, MotelyStandardcardRank.Three),
        ];

        var info = MotelyPokerHandEval.BestScore(eightCards);
        Assert.Equal(MotelyPokerHand.FullHouse, info.Type);
        Assert.Equal(208, info.Score);
    }
}
