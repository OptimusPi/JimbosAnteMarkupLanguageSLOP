using Motely.Filters;

namespace Motely.Tests;

/// <summary>
/// A name in a mapping reads as loosely as in shorthand: any case, spaces or punctuation, and a
/// leading "The" either way — when exactly one name fits. A name that belongs to another kind of
/// thing fails with the key it goes under.
/// </summary>
public sealed class JamlTolerantNameTests
{
    private static string LoadError(string yaml)
    {
        Assert.False(JamlConfig.TryLoad(yaml, out _, out var error), "the filter loaded");
        return error!;
    }

    [Theory]
    [InlineData("voucher: Reroll Surplus", "voucher: RerollSurplus")]
    [InlineData("boss: The Hook", "boss: TheHook")]
    [InlineData("boss: the_hook", "boss: TheHook")]
    [InlineData("boss: Hook", "boss: TheHook")]
    [InlineData("tarotCard: Fool", "tarotCard: TheFool")]
    [InlineData("joker: The Luchador", "joker: Luchador")]
    [InlineData("joker: [blue print, Invisible Joker, oops all 6s]", "joker: [Blueprint, InvisibleJoker, OopsAll6s]")]
    [InlineData("tag: Charm Tag", "tag: CharmTag")]
    [InlineData("spectralCard: Black Hole", "spectralCard: BlackHole")]
    [InlineData("standardCard:\n      rank: K\n      suit: Hearts", "standardCard:\n      rank: King\n      suit: Hearts")]
    [InlineData("standardCard:\n      rank: q", "standardCard:\n      rank: Queen")]
    [InlineData("erraticRank: A", "erraticRank: Ace")]
    [InlineData("erraticRank: J", "erraticRank: Jack")]
    public void LooseName_LoadsTheSameClauseAsTheExactName(string loose, string exact)
    {
        Assert.Equal(
            TwinFilters.AllFields(ProofSearch.LoadOrThrow(Clause(exact))),
            TwinFilters.AllFields(ProofSearch.LoadOrThrow(Clause(loose))));
    }

    [Fact]
    public void LooseNames_FindExactlyWhatExactNamesFind()
    {
        TwinFilters.AssertSame(
            """
            should:
              - !voucher
                voucher: RerollSurplus
                antes: [1, 2]
              - !boss
                boss: TheHook
                antes: [1, 2, 3]
              - !tarotCard
                tarotCard: TheFool
                antes: [1, 2]
              - !joker
                joker: [Luchador, InvisibleJoker]
                antes: [1, 2]
              - !standardCard
                standardCard:
                  rank: King
                antes: [1, 2]
            """,
            """
            should:
              - !voucher
                voucher: Reroll Surplus
                antes: [1, 2]
              - !boss
                boss: the hook
                antes: [1, 2, 3]
              - !tarotCard
                tarotCard: Fool
                antes: [1, 2]
              - !joker
                joker: [The Luchador, invisible joker]
                antes: [1, 2]
              - !standardCard
                standardCard:
                  rank: K
                antes: [1, 2]
            """);
    }

    [Theory]
    [InlineData("standardCard: Certificate", "Certificate")]
    [InlineData("commonJoker: Showman", "Showman")]
    [InlineData("boss: The Luchador", "The Luchador")]
    [InlineData("joker: The Fool", "The Fool")]
    [InlineData("rareJoker: Perkeo", "Perkeo")]
    [InlineData("joker: Blueprnt", "Blueprnt")]
    [InlineData("voucher: Telescop", "Telescop")]
    public void NameOfTheWrongKind_IsRefusedOnItsLine(string clause, string name)
    {
        Assert.StartsWith($"JAML line 3: '{name}' is not a", LoadError(Clause(clause)));
    }

    [Fact]
    public void ANumberIsStillNoRank()
    {
        // Letters are the card's own; a number stays refused (Enum.Parse read 2 as Four).
        Assert.StartsWith("JAML line 3: '2' is not a name", LoadError("must:\n  - !erraticRank\n    erraticRank: 2\n"));
    }

    private static string Clause(string body)
    {
        var key = body.Split(':', '\n')[0].Trim();
        return $"must:\n  - !{key}\n    {body}\n";
    }
}
