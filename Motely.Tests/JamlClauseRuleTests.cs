using Motely.Filters;

namespace Motely.Tests;

/// <summary>
/// What the loader refuses on a loaded clause, written out filter by filter with the message it
/// gives. The rules are rows of <see cref="JamlConfigLoader.ClauseRules"/>; these cases are not
/// read from the table, so a row that changes, goes missing or moves in the order fails here.
/// </summary>
public sealed class JamlClauseRuleTests
{
    private const string Nested = "\n      - joker: Blueprint\n      - joker: Brainstorm";

    [Theory]
    // min and max
    [InlineData("joker", "- joker: Blueprint\n    min: -1", "min and max run from 0 up.")]
    [InlineData("joker", "- joker: Blueprint\n    max: -1", "min and max run from 0 up.")]
    [InlineData("joker", "- joker: Blueprint\n    min: 1\n    max: -1", "min and max run from 0 up.")]
    [InlineData("joker", "- joker: Blueprint\n    min: 3\n    max: 2", "max is below min (min defaults to 1), so nothing can match.")]
    [InlineData("joker", "- joker: Blueprint\n    max: 0", "max is below min (min defaults to 1), so nothing can match.")]
    // and, or
    [InlineData("and", "- and:" + Nested + "\n    min: 2", "an and takes no min or max: set them on its clauses.")]
    [InlineData("and", "- and:" + Nested + "\n    min: 0", "an and takes no min or max: set them on its clauses.")]
    [InlineData("and", "- and:" + Nested + "\n    max: 5", "an and takes no min or max: set them on its clauses.")]
    [InlineData("and", "- and:" + Nested + "\n    min: 2\n    max: 1", "max is below min (min defaults to 1), so nothing can match.")]
    [InlineData("or", "- or:" + Nested + "\n    max: 3", "an or takes no max: set it on its clauses.")]
    [InlineData("or", "- or:" + Nested + "\n    min: 0\n    max: 0", "an or takes no max: set it on its clauses.")]
    [InlineData("or", "- or:" + Nested + "\n    min: 0", "an or needs min: 1 or more.")]
    [InlineData("and", "- and: []", "clauses: needs at least one clause.")]
    [InlineData("or", "- or:\n    mode: Max", "clauses: needs at least one clause.")]
    [InlineData("or", "- or: []\n    min: 0", "clauses: needs at least one clause.")]
    [InlineData("and", "- clauses: []\n    and: ~", "clauses: needs at least one clause.")]
    // antes, rolls, with written as null
    [InlineData("joker", "- joker: Blueprint\n    antes: ~", "antes: needs a number or a list of them.")]
    [InlineData("boss", "- boss: TheHook\n    antes: ~", "antes: needs a number or a list of them.")]
    [InlineData("voucher", "- voucher: Telescope\n    rolls: ~", "rolls: needs a number or a list of them.")]
    [InlineData("pokerHand", "- pokerHand: Flush\n    rolls: ~", "rolls: needs a number or a list of them.")]
    [InlineData("luckyMoney", "- luckyMoney: 1\n    rolls: ~", "rolls: needs a number or a list of them.")]
    [InlineData("joker", "- joker: Blueprint\n    with: ~", "with: needs a mapping such as { jokers: [Showman] }.")]
    [InlineData("luckyMoney", "- luckyMoney: 1\n    with: ~", "with: needs a mapping such as { jokers: [Showman] }.")]
    [InlineData("soulOrder", "- soulOrder: [{ legendary: Perkeo }]\n    with: ~", "with: needs a mapping such as { jokers: [Showman] }.")]
    // lists that need an item
    [InlineData("luckyMoney", "- luckyMoney: any", "rolls: needs at least one roll.")]
    [InlineData("misprintMult", "- misprintMult: []", "rolls: needs at least one roll.")]
    [InlineData("bloodstoneTrigger", "- bloodstoneTrigger: 1\n    rolls: []", "rolls: needs at least one roll.")]
    [InlineData("voucher", "- voucher: Telescope\n    rolls: []", "rolls: needs at least one roll.")]
    [InlineData("tag", "- tag: NegativeTag\n    rolls: []", "rolls: needs at least one roll.")]
    [InlineData("smallBlindTag", "- smallBlindTag: NegativeTag\n    rolls: []", "rolls: needs at least one roll.")]
    [InlineData("boosterPack", "- boosterPack: MegaArcana\n    rolls: []", "rolls: needs at least one roll.")]
    [InlineData("boss", "- boss: TheHook\n    antes: []", "antes: needs at least one ante.")]
    [InlineData("boss", "- boss: TheHook\n    ante: []", "antes: needs at least one ante.")]
    // ranges
    [InlineData("boss", "- boss: TheHook\n    antes: [0]", "boss antes run from 1: ante 0 has no boss blind.")]
    [InlineData("boss", "- boss: TheHook\n    antes: [0, 40]", "boss antes run from 1: ante 0 has no boss blind.")]
    [InlineData("boss", "- boss: TheHook\n    antes: [40]", "antes run from 0 to 39.")]
    [InlineData("joker", "- joker: Blueprint\n    antes: [1, 40]", "antes run from 0 to 39.")]
    [InlineData("joker", "- joker: Blueprint\n    ante: 40", "antes run from 0 to 39.")]
    [InlineData("legendaryJoker", "- legendaryJoker: Perkeo\n    soulEditionRolls: -1", "soulEditionRolls runs from 0 to 999.")]
    [InlineData("legendaryJoker", "- legendaryJoker: Perkeo\n    soulEditionRolls: 1000", "soulEditionRolls runs from 0 to 999.")]
    [InlineData("soulOrder", "- soulOrder: [{ legendary: Perkeo }]\n    maxAnte: -1", "maxAnte runs from 0 to 39.")]
    [InlineData("soulOrder", "- soulOrder: [{ legendary: Perkeo }]\n    maxAnte: 40", "maxAnte runs from 0 to 39.")]
    // a clause inside an or is held to the same rules
    [InlineData("joker", "- or:\n      - joker: Blueprint\n        min: -1", "min and max run from 0 up.")]
    public void AClauseThatBreaksARule_IsRefusedWithItsMessage(string key, string clause, string rule)
    {
        foreach (var list in (string[])["must", "should", "mustNot"])
        {
            Assert.False(JamlConfig.TryLoad($"{list}:\n  {clause}\n", out _, out var error), clause);
            Assert.Equal($"JAML: a {key} clause is out of range: {rule}", error);
        }
    }

    [Theory]
    [InlineData("- joker: Blueprint\n    min: 0")]
    [InlineData("- joker: Blueprint\n    min: 0\n    max: 0")]
    [InlineData("- joker: Blueprint\n    min: 2\n    max: 2")]
    [InlineData("- joker: Blueprint\n    max: ~")]
    [InlineData("- joker: Blueprint\n    antes: [0, 39]")]
    [InlineData("- joker: Blueprint\n    antes: []")]
    [InlineData("- joker: Blueprint\n    ante: ~")]
    [InlineData("- and:" + Nested + "\n    min: 1\n    max: ~")]
    [InlineData("- and: ~\n    clauses:" + Nested)]
    [InlineData("- or:" + Nested + "\n    min: 2")]
    [InlineData("- boss: TheHook\n    antes: [1, 39]")]
    [InlineData("- pokerHand: Flush\n    rolls: []")]
    [InlineData("- legendaryJoker: Perkeo\n    rolls: []")]
    [InlineData("- legendaryJoker: Perkeo\n    rolls: ~")]
    [InlineData("- legendaryJoker: Perkeo\n    soulEditionRolls: 0")]
    [InlineData("- legendaryJoker: Perkeo\n    soulEditionRolls: 999")]
    [InlineData("- soulOrder: [{ legendary: Perkeo }]\n    maxAnte: 0")]
    [InlineData("- soulOrder: [{ legendary: Perkeo }]\n    maxAnte: 39")]
    [InlineData("- luckyMoney: any\n    rolls: [0, 999]")]
    [InlineData("- voucher: any\n    rolls: [0]")]
    public void AClauseInsideEveryRule_Loads(string clause)
    {
        Assert.True(JamlConfig.TryLoad($"must:\n  {clause}\n", out _, out var error), $"{clause}: {error}");
    }

    [Theory]
    [InlineData("- legendaryJoker: Blueprint", "Blueprint")]
    [InlineData("- legendaryJoker: [Perkeo, Showman, Joker]", "Showman")]
    [InlineData("- legendaryJokers: Joker", "Joker")]
    [InlineData("- soulJoker: Joker", "Joker")]
    [InlineData("- legendaryJoker: Perkeo\n    jokers: [Mime]", "Mime")]
    // after the antes range, before soulEditionRolls
    [InlineData("- legendaryJoker: Blueprint\n    soulEditionRolls: 1000", "Blueprint")]
    public void ALegendaryJokerClause_NamingAnotherKindOfJoker_IsRefused(string clause, string name)
    {
        Assert.False(JamlConfig.TryLoad($"must:\n  {clause}\n", out _, out var error));
        Assert.Equal(
            $"JAML: '{name}' is not a legendary joker (legendaryJoker takes Canio, Triboulet, Yorick, Chicot or Perkeo).",
            error);

        Assert.False(JamlConfig.TryLoad("must:\n  - legendaryJoker: Blueprint\n    antes: [40]\n", out _, out error));
        Assert.Equal("JAML: a legendaryJoker clause is out of range: antes run from 0 to 39.", error);
        Assert.True(JamlConfig.TryLoad("must:\n  - legendaryJoker: [Canio, Triboulet, Yorick, Chicot, Perkeo]\n", out _, out error), error);
        Assert.True(JamlConfig.TryLoad("must:\n  - legendaryJoker: any\n  - joker: Perkeo\n", out _, out error), error);
    }

    [Fact]
    public void ANullSeed_IsRefused_AndNullIdOrSeedsLoadsAsEmpty()
    {
        const string clause = "must:\n  - joker: Blueprint\n";
        Assert.False(JamlConfig.TryLoad(clause + "seeds: [ALEEB, ~]\n", out _, out var error));
        Assert.Equal(
            "JAML: seeds: holds a null entry (~ or null). Write the seed (quote it if it is the word NULL), or \"\" for the 0-character seed.",
            error);
        // The seeds are read before the clauses.
        Assert.False(JamlConfig.TryLoad("must:\n  - joker: Blueprint\n    min: -1\nseeds: [~]\n", out _, out error));
        Assert.StartsWith("JAML: seeds: holds a null entry", error);

        Assert.True(JamlConfig.TryLoad(clause + "id: ~\nseeds: ~\n", out var config, out error), error);
        Assert.Equal("", config.Id);
        Assert.Empty(config.Seeds);
    }

    [Fact]
    public void ShouldHoldsAtMost256Clauses()
    {
        string Should(int count) => "should:\n" + string.Concat(Enumerable.Repeat("  - joker: Blueprint\n", count));

        Assert.True(JamlConfig.TryLoad(Should(256), out _, out var error), error);
        Assert.False(JamlConfig.TryLoad(Should(257), out _, out error));
        Assert.Equal("JAML: should: holds 257 clauses; a filter takes at most 256.", error);
        Assert.True(JamlConfig.TryLoad(Should(257).Replace("should:", "must:"), out _, out error), error);
    }

    [Theory]
    [InlineData("- joker: Blueprint\n    antes: [1000]", "JAML line 3: `1000` is out of range: a number in this list runs from 0 to 999.")]
    [InlineData("- joker: Blueprint\n    antes: -1", "JAML line 3: `-1` is out of range: a number in this list runs from 0 to 999.")]
    [InlineData("- luckyMoney: 1000", "JAML line 2: `1000` is out of range: a roll runs from 0 to 999.")]
    [InlineData("- luckyMoney: [0, -1]", "JAML line 2: `-1` is out of range: a roll runs from 0 to 999.")]
    public void ANumberInAListRunsFrom0To999(string clause, string message)
    {
        Assert.False(JamlConfig.TryLoad($"must:\n  {clause}\n", out _, out var error));
        Assert.Equal(message, error);
        Assert.True(JamlConfig.TryLoad("must:\n  - joker: Blueprint\n    sources: { shopItems: [0, 999] }\n", out _, out error), error);
    }

    [Theory]
    [InlineData("any")]
    [InlineData("ANY")]
    [InlineData("~")]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\" Any \"")]
    public void AnAnyWord_ReadsAsEveryNameCardOrNoRolls(string word)
    {
        Assert.True(JamlConfig.TryLoad($"must:\n  - joker: {word}\n", out var names, out var error), error);
        Assert.Empty(((JokerClause)names.Must[0]).Jokers);
        Assert.True(JamlConfig.TryLoad($"must:\n  - standardCard: {word}\n", out var card, out error), error);
        Assert.Null(((StandardCardClause)card.Must[0]).Card);
        Assert.True(JamlConfig.TryLoad($"must:\n  - luckyMoney: {word}\n    rolls: [3]\n", out var rolls, out error), error);
        Assert.Equal([3], ((LuckyMoneyClause)rolls.Must[0]).Rolls);
        Assert.False(JamlConfig.TryLoad($"must:\n  - luckyMoney: {word}\n", out _, out error));
    }
}
