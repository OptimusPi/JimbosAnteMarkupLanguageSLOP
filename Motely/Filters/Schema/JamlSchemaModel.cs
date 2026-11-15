namespace Motely.Filters;

/// <summary>How a member's value is written, as the loader's converters and types read it.</summary>
internal enum JamlValueKind
{
    /// <summary>Any scalar, kept as text.</summary>
    Text,

    /// <summary>A list of scalars kept as text.</summary>
    Texts,

    /// <summary>A whole number.</summary>
    Number,

    /// <summary>true or false.</summary>
    Flag,

    /// <summary>One enum name.</summary>
    Name,

    /// <summary>One number or a list of them.</summary>
    Numbers,

    /// <summary>One enum name, a list of them, or an any-word.</summary>
    Names,

    /// <summary>An event clause's own key: one roll, a list of them, or an any-word.</summary>
    EventRolls,

    /// <summary>A playing card: a name, an any-word, or a mapping of its parts.</summary>
    Card,

    /// <summary>A list of clauses.</summary>
    Clauses,

    /// <summary>A mapping with its own members.</summary>
    Block,

    /// <summary>A list of mappings.</summary>
    Blocks,
}

/// <summary>One member a filter may write, with everything the loader holds it to.</summary>
internal sealed record JamlMemberModel(string Name, JamlValueKind Kind, string Description)
{
    /// <summary>The enum whose names the value is (Name, Names, Card).</summary>
    public Type? Enum { get; init; }

    /// <summary>The mapping type the value is (Block, Blocks, Card).</summary>
    public Type? Block { get; init; }

    /// <summary>Whether null may be written.</summary>
    public bool Null { get; init; }

    /// <summary>Whether an any-word may be written.</summary>
    public bool Any { get; init; }

    public int? Min { get; init; }
    public int? Max { get; init; }
    public IReadOnlyList<int> Excluded { get; init; } = [];
    public int MinItems { get; init; }
    public int? MaxItems { get; init; }

    /// <summary>Whether the mapping must write this member.</summary>
    public bool Required { get; init; }

    public bool IsList =>
        Kind is JamlValueKind.Texts or JamlValueKind.Numbers or JamlValueKind.Names or JamlValueKind.EventRolls
            or JamlValueKind.Clauses or JamlValueKind.Blocks;
}

/// <summary>One mapping the loader binds: the filter, a clause, or a block inside a clause.</summary>
internal sealed record JamlTypeModel(
    Type Type,
    string SchemaName,
    string TypeName,
    string Description,
    IReadOnlyList<JamlMemberModel> Members)
{
    /// <summary>The clause keys that select this type, in the binding table's order.</summary>
    public IReadOnlyList<string> Keys { get; init; } = [];

    /// <summary>Groups of members of which at least one must be written (and not as null).</summary>
    public IReadOnlyList<IReadOnlyList<string>> OneOfWritten { get; init; } = [];
}

/// <summary>Everything the three generated files say, derived from the loader.</summary>
internal sealed record JamlSchemaModel(
    JamlTypeModel Filter,
    IReadOnlyList<JamlTypeModel> Blocks,
    IReadOnlyList<JamlTypeModel> Clauses,
    IReadOnlyList<Type> Enums)
{
    /// <summary>Types and members <see cref="JamlSchemaAnnotations"/> has no description for.</summary>
    public IReadOnlyList<string> MissingDescriptions { get; init; } = [];

    /// <summary>Entries of <see cref="JamlSchemaAnnotations"/> that describe nothing a filter can write.</summary>
    public IReadOnlyList<string> OrphanDescriptions { get; init; } = [];

    public IEnumerable<JamlTypeModel> Types => Blocks.Concat(Clauses).Prepend(Filter);

    /// <summary>Each clause key and the clause it selects, in the binding table's order.</summary>
    public IEnumerable<(string Key, JamlTypeModel Clause)> ClauseKeys =>
        JamlClauseBinding.ClauseTypeByKey.Select(pair => (pair.Key, Clauses.First(clause => clause.Type == pair.Value)));
}
