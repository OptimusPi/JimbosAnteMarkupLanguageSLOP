namespace Motely.Tests;

/// <summary>What one <see cref="JamlRule"/> says of a member's value.</summary>
internal enum JamlRuleKind
{
    /// <summary>A number from <see cref="JamlRule.Min"/> to <see cref="JamlRule.Max"/> (either end may be open).</summary>
    Range,

    /// <summary>Every number of a list from <see cref="JamlRule.Min"/> to <see cref="JamlRule.Max"/>.</summary>
    ItemRange,

    /// <summary>A list of at least <see cref="JamlRule.Min"/> entries.</summary>
    MinItems,

    /// <summary>The member may not be written as null.</summary>
    NotNull,

    /// <summary>The member takes no value here: only null (or leaving it out) loads.</summary>
    NoValue,

    /// <summary>The number may not be below the member named by <see cref="JamlRule.Other"/>. Two keys at once: a schema cannot say it.</summary>
    NotBelow,

    /// <summary>Every name must also be a name of the enum <see cref="JamlRule.Names"/>.</summary>
    Names,

    /// <summary>The member has to be written.</summary>
    Required,
}

/// <summary>
/// One rule a filter value keeps beyond its type. <see cref="On"/> is the class or interface the
/// rule covers and <see cref="Member"/> the key as a document writes it.
/// </summary>
internal sealed record JamlRule(Type On, string Member, JamlRuleKind Kind)
{
    public int? Min { get; init; }
    public int? Max { get; init; }
    public Type? Names { get; init; }
    public string? Other { get; init; }

    /// <summary>
    /// The member's own setter enforces this as the value binds, so it holds whichever key is
    /// written last. (The other rows are checked once the whole clause is read.)
    /// </summary>
    public bool BySetter { get; init; }

    /// <summary>The loader's own words for the rule, where the schema can only carry it as words.</summary>
    public string? Words { get; init; }
}

/// <summary>
/// The value rules the schema states. They restate what the loader refuses
/// (<c>JamlConfigLoader.CheckBounds</c> and <c>LegendarySoulOrderClause.RequireSoulOrder</c>) as
/// rows a generator can read. The loader does not read this table: it is a second statement of
/// those checks, and <see cref="JamlGrammarTests"/> is what holds it to them. Every row is tried
/// against the loader both ways (a document that breaks it is refused, one that keeps it loads),
/// and every member of every clause is written with a spread of values to find a check the loader
/// makes that no row states. Add a check to the loader and that second test fails until its row is here.
/// </summary>
internal static class JamlSchemaRules
{
    internal static readonly IReadOnlyList<JamlRule> All =
    [
        new(typeof(IMotelyClause), "min", JamlRuleKind.Range) { Min = 0 },
        new(typeof(IMotelyClause), "max", JamlRuleKind.Range) { Min = 0 },
        new(typeof(IMotelyClause), "max", JamlRuleKind.NotBelow) { Other = "min", Words = "max is below min (min defaults to 1), so nothing can match." },
        new(typeof(AndClause), "min", JamlRuleKind.Range) { Min = 1, Max = 1 },
        new(typeof(AndClause), "max", JamlRuleKind.NoValue),
        new(typeof(OrClause), "max", JamlRuleKind.NoValue),
        new(typeof(OrClause), "min", JamlRuleKind.Range) { Min = 1 },
        new(typeof(LogicClause), "clauses", JamlRuleKind.MinItems) { Min = 1 },
        new(typeof(IAnteScopedClause), "antes", JamlRuleKind.NotNull),
        new(typeof(IAnteScopedClause), "antes", JamlRuleKind.ItemRange) { Max = MotelyGlobals.MaxAnte },
        new(typeof(IRollScopedClause), "rolls", JamlRuleKind.NotNull),
        new(typeof(IWithScopedClause), "with", JamlRuleKind.NotNull),
        new(typeof(EventClause), "rolls", JamlRuleKind.MinItems) { Min = 1 },
        new(typeof(VoucherClause), "rolls", JamlRuleKind.MinItems) { Min = 1 },
        new(typeof(TagClause), "rolls", JamlRuleKind.MinItems) { Min = 1 },
        new(typeof(BoosterPackClause), "rolls", JamlRuleKind.MinItems) { Min = 1 },
        new(typeof(BossClause), "antes", JamlRuleKind.MinItems) { Min = 1 },
        new(typeof(BossClause), "antes", JamlRuleKind.ItemRange) { Min = 1 },
        new(typeof(LegendaryJokerClause), "jokers", JamlRuleKind.Names) { Names = typeof(MotelyJokerLegendary) },
        new(typeof(LegendaryJokerClause), "soulEditionRolls", JamlRuleKind.Range) { Min = 0, Max = MotelyGlobals.MaxRollIndex },
        new(typeof(LegendarySoulOrderClause), "maxAnte", JamlRuleKind.Range) { Min = 0, Max = MotelyGlobals.MaxAnte },
        new(typeof(LegendarySoulOrderClause), "entries", JamlRuleKind.MinItems) { Min = 1, BySetter = true },
        new(typeof(LegendarySoulOrderClause), "soulOrder", JamlRuleKind.MinItems) { Min = 1, BySetter = true },
        new(typeof(LegendarySoulOrderClause), "soulOrders", JamlRuleKind.MinItems) { Min = 1, BySetter = true },
        new(typeof(SoulOrderEntry), "legendary", JamlRuleKind.Required) { BySetter = true },
        new(typeof(SoulOrderEntry), "legendary", JamlRuleKind.Names) { Names = typeof(MotelyJokerLegendary), BySetter = true },
    ];
}
