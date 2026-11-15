using System.Collections;

namespace Motely.Filters;

/// <summary>What one <see cref="JamlRule"/> row says about a member.</summary>
internal enum JamlRuleKind
{
    /// <summary>A number, or each number of a list, lies in <see cref="JamlRule.Min"/>..<see cref="JamlRule.Max"/>.</summary>
    Range,

    /// <summary>The member is not written as null.</summary>
    NotNull,

    /// <summary>A list holds at least <see cref="JamlRule.Min"/> items.</summary>
    MinItems,

    /// <summary>A list holds at most <see cref="JamlRule.Max"/> items.</summary>
    MaxItems,

    /// <summary>The member is not written on this clause: it keeps <see cref="JamlRule.Default"/>.</summary>
    Forbidden,

    /// <summary>A number, or any number of a list, is not <see cref="JamlRule.Value"/>.</summary>
    Excluded,

    /// <summary>
    /// <c>max</c> is not below <c>min</c>. Two members at once, which a JSON Schema cannot say: the
    /// check is code in <see cref="JamlConfigLoader.Breaks"/>, and the schema carries
    /// <see cref="JamlRule.Message"/> in the member's description instead.
    /// </summary>
    NotBelowMin,

    /// <summary>The name, or each name of a list, is one of <see cref="JamlRule.NamesOf"/>'s names.</summary>
    Names,

    /// <summary>No item of a list is null.</summary>
    NoNullItems,

    /// <summary>The member is written.</summary>
    Present,
}

/// <summary>Where the loader enforces a rule.</summary>
internal enum JamlRuleSite
{
    /// <summary><see cref="JamlConfigLoader"/> checks the row on the loaded filter, in table order.</summary>
    Loader,

    /// <summary>
    /// The code that reads the value enforces it as the document binds, so the error names the
    /// line. The row states the same rule for the schema, and the rule tests hold the two together.
    /// </summary>
    Binder,
}

/// <summary>
/// One rule a JSON Schema can express, on one member of the types in <see cref="Targets"/> (a
/// class, a base class or an interface). <see cref="Member"/> is the name as written in a filter.
/// The loader enforces the rows and the schema generator emits them, so the two cannot drift.
/// </summary>
internal sealed record JamlRule(JamlRuleKind Kind, Type[] Targets, string Member, string Message)
{
    public int? Min { get; init; }
    public int? Max { get; init; }
    public int Value { get; init; }
    public object? Default { get; init; }
    public Type? NamesOf { get; init; }
    public JamlRuleSite Site { get; init; } = JamlRuleSite.Loader;

    public bool AppliesTo(Type type)
    {
        foreach (var target in Targets)
            if (target.IsAssignableFrom(type))
                return true;
        return false;
    }

    /// <summary>Whether <paramref name="value"/>, read from <see cref="Member"/>, breaks this row.</summary>
    public bool BrokenBy(object? value) =>
        Kind switch
        {
            JamlRuleKind.Range => value switch
            {
                int number => OutOfRange(number),
                int[] numbers => Array.Exists(numbers, OutOfRange),
                _ => false,
            },
            JamlRuleKind.NotNull => value is null,
            JamlRuleKind.MinItems => value is ICollection items && items.Count < Min,
            JamlRuleKind.MaxItems => value is ICollection items && items.Count > Max,
            JamlRuleKind.Forbidden => !Equals(value, Default),
            JamlRuleKind.Excluded => value switch
            {
                int number => number == Value,
                int[] numbers => Array.IndexOf(numbers, Value) >= 0,
                _ => false,
            },
            JamlRuleKind.Names => FirstOutside(value) is not null,
            JamlRuleKind.NoNullItems => value is IEnumerable items && items.Cast<object?>().Contains(null),
            _ => throw new InvalidOperationException($"a {Kind} rule is not read from one value."),
        };

    /// <summary>For a <see cref="JamlRuleKind.Names"/> row: the first name in <paramref name="value"/> that is none of <see cref="NamesOf"/>'s.</summary>
    public object? FirstOutside(object? value)
    {
        var allowed = Enum.GetNames(NamesOf!);
        if (value is Array names)
        {
            foreach (var name in names)
                if (Array.IndexOf(allowed, name?.ToString()) < 0)
                    return name;
            return null;
        }
        return value is not null && Array.IndexOf(allowed, value.ToString()) < 0 ? value : null;
    }

    private bool OutOfRange(int number) => number < Min || number > Max;
}
