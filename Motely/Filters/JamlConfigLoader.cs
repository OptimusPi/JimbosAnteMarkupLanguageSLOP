using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.TypeInspectors;

namespace Motely.Filters;

/// <summary>JAML text → <see cref="JamlConfig"/> through YamlDotNet's static (AOT) deserializer. JSON is YAML too.</summary>
public static partial class JamlConfigLoader
{
    private static readonly INamingConvention Naming = CamelCaseNamingConvention.Instance;

    private static readonly IDeserializer Deserializer = BuildDeserializer();

    /// <summary>
    /// The members the deserializer binds, under the names a filter writes: the static context's
    /// own list is the C# property names, so it is read through the naming convention and the
    /// YamlMember aliases the deserializer builder applies.
    /// </summary>
    private static readonly ITypeInspector Inspector = new YamlAttributesTypeInspector(
        new NamingConventionTypeInspector(new JamlYamlContext().GetTypeInspector(), Naming));

    private static readonly ConcurrentDictionary<Type, IPropertyDescriptor[]> Members = new();

    /// <summary>Every member a filter may write on <paramref name="type"/>, by its written name.</summary>
    internal static IReadOnlyList<IPropertyDescriptor> BoundMembers(Type type) =>
        Members.GetOrAdd(type, static bound => [.. Inspector.GetProperties(bound, null).Where(member => member.CanWrite)]);

    internal static IPropertyDescriptor BoundMember(Type type, string name)
    {
        foreach (var member in BoundMembers(type))
            if (member.Name == name)
                return member;
        throw new InvalidOperationException($"{type.Name} binds no member '{name}'.");
    }

    /// <summary>Reads <paramref name="text"/> as one <paramref name="type"/>, as the loader reads it inside a filter.</summary>
    internal static object? Bind(string text, Type type) => Deserializer.Deserialize(new JamlParser(text), type);

    private static IDeserializer BuildDeserializer()
    {
        var builder = new StaticDeserializerBuilder(new JamlYamlContext())
            .WithNamingConvention(Naming)
            .WithCaseInsensitivePropertyMatching()
            .WithDuplicateKeyChecking()
            .WithNodeDeserializer(new JamlEnumNodeDeserializer())
            .WithNodeDeserializer(new ClauseKeyDeserializer());
        foreach (var (key, type) in JamlClauseBinding.ClauseTypeByKey)
            builder = builder.WithTagMapping("!" + key, type);
        return JamlClauseBinding.Converters
            .Aggregate(builder, (current, converter) => current.WithTypeConverter(converter))
            .Build();
    }

    /// <summary>
    /// Loads a filter. A malformed document throws <see cref="JamlFilterException"/> whose message
    /// starts <c>JAML line N:</c> and whose Line/Column point at the offending node.
    /// </summary>
    public static JamlConfig FromJaml(string text)
    {
        try
        {
            var config = Deserializer.Deserialize<JamlConfig?>(new JamlParser(text))
                ?? throw new InvalidOperationException("JAML: the document is empty.");
            config.Id ??= "";
            config.Seeds ??= [];
            Enforce(NoNullSeed, config);
            CheckBounds(config.Must);
            CheckBounds(config.Should);
            CheckBounds(config.MustNot);
            Enforce(ShouldLimit, config);
            return config;
        }
        catch (InvalidCastException)
        {
            // A scalar document (a CSV, a seed list) parses as one string, not a mapping.
            throw new InvalidOperationException("JAML: not a filter (expected keys like name:, must:, should:).");
        }
        catch (YamlException ex)
        {
            var inner = ex;
            while (inner.InnerException is YamlException next)
                inner = next;
            var reason = inner.InnerException is NullReferenceException or IndexOutOfRangeException
                ? "a value is missing here. Null and empty are not values."
                : inner.InnerException?.Message ?? inner.Message;
            var message = $"JAML line {inner.Start.Line}: {reason}";
            throw new JamlFilterException(message, (int)inner.Start.Line, (int)inner.Start.Column, ex);
        }
    }

    /// <summary>
    /// Every rule on a clause that a JSON Schema can express, in the order the loader checks them:
    /// a clause that breaks two rows reports the first. <see cref="CheckBounds"/> enforces the
    /// rows and <see cref="JamlSchemaGenerator"/> emits them. The one row a schema cannot express
    /// (<see cref="JamlRuleKind.NotBelowMin"/>) sits here for its place in the order.
    /// </summary>
    internal static readonly JamlRule[] ClauseRules =
    [
        new(JamlRuleKind.Range, [typeof(IMotelyClause)], "min", "min and max run from 0 up.") { Min = 0 },
        new(JamlRuleKind.Range, [typeof(IMotelyClause)], "max", "min and max run from 0 up.") { Min = 0 },
        new(JamlRuleKind.NotBelowMin, [typeof(IMotelyClause)], "max",
            "max is below min (min defaults to 1), so nothing can match."),
        new(JamlRuleKind.Forbidden, [typeof(AndClause)], "min", "an and takes no min or max: set them on its clauses.")
        {
            Default = 1,
        },
        new(JamlRuleKind.Forbidden, [typeof(AndClause)], "max", "an and takes no min or max: set them on its clauses."),
        new(JamlRuleKind.Forbidden, [typeof(OrClause)], "max", "an or takes no max: set it on its clauses."),
        new(JamlRuleKind.MinItems, [typeof(LogicClause)], "clauses", "clauses: needs at least one clause.") { Min = 1 },
        new(JamlRuleKind.NotNull, [typeof(IAnteScopedClause)], "antes", "antes: needs a number or a list of them."),
        new(JamlRuleKind.NotNull, [typeof(IRollScopedClause)], "rolls", "rolls: needs a number or a list of them."),
        new(JamlRuleKind.NotNull, [typeof(IWithScopedClause)], "with",
            "with: needs a mapping such as { jokers: [Showman] }."),
        new(JamlRuleKind.MinItems,
            [typeof(EventClause), typeof(VoucherClause), typeof(TagClause), typeof(BoosterPackClause)], "rolls",
            "rolls: needs at least one roll.")
        {
            Min = 1,
        },
        new(JamlRuleKind.MinItems, [typeof(BossClause)], "antes", "antes: needs at least one ante.") { Min = 1 },
        new(JamlRuleKind.Excluded, [typeof(BossClause)], "antes", "boss antes run from 1: ante 0 has no boss blind.")
        {
            Value = 0,
        },
        new(JamlRuleKind.Range, [typeof(OrClause)], "min", "an or needs min: 1 or more.") { Min = 1 },
        new(JamlRuleKind.Range, [typeof(IAnteScopedClause)], "antes", $"antes run from 0 to {MotelyGlobals.MaxAnte}.")
        {
            Min = 0,
            Max = MotelyGlobals.MaxAnte,
        },
        // The Soul only ever gives a legendary, so any other joker here could never match. The
        // message stands alone ({0} is the first joker that is none), without the clause prefix.
        new(JamlRuleKind.Names, [typeof(LegendaryJokerClause)], "jokers",
            "JAML: '{0}' is not a legendary joker (legendaryJoker takes Canio, Triboulet, Yorick, Chicot or Perkeo).")
        {
            NamesOf = typeof(MotelyJokerLegendary),
        },
        new(JamlRuleKind.Range, [typeof(LegendaryJokerClause)], "soulEditionRolls",
            $"soulEditionRolls runs from 0 to {MotelyGlobals.MaxRollIndex}.")
        {
            Min = 0,
            Max = MotelyGlobals.MaxRollIndex,
        },
        new(JamlRuleKind.Range, [typeof(LegendarySoulOrderClause)], "maxAnte",
            $"maxAnte runs from 0 to {MotelyGlobals.MaxAnte}.")
        {
            Min = 0,
            Max = MotelyGlobals.MaxAnte,
        },
    ];

    /// <summary>
    /// An unquoted ~ or null in <c>seeds:</c> is YAML's null, not a seed: a replay would
    /// dereference it. Checked before the clauses.
    /// </summary>
    internal static readonly JamlRule NoNullSeed =
        new(JamlRuleKind.NoNullItems, [typeof(JamlConfig)], "seeds",
            "JAML: seeds: holds a null entry (~ or null). Write the seed (quote it if it is the word NULL), or \"\" for the 0-character seed.");

    /// <summary>Checked after the clauses. {0} in the message is the count the document holds.</summary>
    internal static readonly JamlRule ShouldLimit =
        new(JamlRuleKind.MaxItems, [typeof(JamlConfig)], "should",
            $"JAML: should: holds {{0}} clauses; a filter takes at most {MotelyScoredSeedResult.MAX_TALLY_COUNT}.")
        {
            Max = MotelyScoredSeedResult.MAX_TALLY_COUNT,
        };

    /// <summary>The rules on the filter itself, in the order the loader checks them.</summary>
    internal static readonly JamlRule[] FilterRules = [NoNullSeed, ShouldLimit];

    private static void Enforce(JamlRule rule, JamlConfig config)
    {
        if (Breaks(rule, config))
            throw new InvalidOperationException(string.Format(rule.Message, config.Should.Count));
    }

    /// <summary>
    /// Every number in a list (<c>antes</c>, <c>rolls</c>, <c>shopItems</c>, an event's rolls). The
    /// converters check it as they read each number, so the error names the line.
    /// </summary>
    internal static readonly JamlRule ListNumber =
        new(JamlRuleKind.Range, [typeof(int[]), typeof(JamlEventRolls)], "", "a number in a list is out of range.")
        {
            Min = 0,
            Max = MotelyGlobals.MaxRollIndex,
            Site = JamlRuleSite.Binder,
        };

    /// <summary>
    /// Rules the binding code enforces while the document is read. <c>soulOrder:</c> refuses an
    /// entry that names no legendary (<see cref="LegendarySoulOrderClause"/>), and an entry that
    /// names nothing reads as Joker, which is none.
    /// </summary>
    internal static readonly JamlRule[] BinderRules =
    [
        ListNumber,
        new(JamlRuleKind.Names, [typeof(SoulOrderEntry)], "legendary", "a soulOrder entry names a legendary.")
        {
            NamesOf = typeof(MotelyJokerLegendary),
            Site = JamlRuleSite.Binder,
        },
        new(JamlRuleKind.Present, [typeof(SoulOrderEntry)], "legendary", "a soulOrder entry names a legendary.")
        {
            Site = JamlRuleSite.Binder,
        },
    ];

    /// <summary>Whether <paramref name="target"/> breaks <paramref name="rule"/>.</summary>
    internal static bool Breaks(JamlRule rule, object target) =>
        rule.Kind == JamlRuleKind.NotBelowMin
            ? target is IMotelyClause clause && clause.Max < clause.Min
            : rule.BrokenBy(BoundMember(target.GetType(), rule.Member).Read(target).Value);

    private static void CheckBounds(IEnumerable<IMotelyClause> clauses)
    {
        foreach (var clause in clauses)
        {
            var type = clause.GetType();
            foreach (var rule in ClauseRules)
            {
                if (!rule.AppliesTo(type) || !Breaks(rule, clause))
                    continue;
                throw rule.Kind == JamlRuleKind.Names
                    ? new InvalidOperationException(
                        string.Format(rule.Message, rule.FirstOutside(BoundMember(type, rule.Member).Read(clause).Value)))
                    : OutOfRange(clause, rule.Message);
            }
            if (clause is LogicClause logic)
                CheckBounds(logic.Clauses);
        }
    }

    private static InvalidOperationException OutOfRange(IMotelyClause clause, string rule)
    {
        var key = JamlClauseBinding.ClauseTypeByKey.FirstOrDefault(pair => pair.Value == clause.GetType()).Key
            ?? clause.GetType().Name;
        return new InvalidOperationException($"JAML: a {key} clause is out of range: {rule}");
    }

    public static JamlConfig FromFile(string path) => FromJaml(File.ReadAllText(path));

    /// <summary>Every key that starts a JAML filter clause (<c>joker</c>, <c>legendaryJoker</c>, <c>or</c>, …).</summary>
    public static IReadOnlyCollection<string> ClauseKeys => JamlClauseBinding.ClauseKeys;

    /// <summary>Each clause key and the clause type it selects, for editors that reflect the dialect.</summary>
    public static IReadOnlyDictionary<string, Type> ClauseTypes => JamlClauseBinding.ClauseTypeByKey;

    public static bool TryLoad(string text, [NotNullWhen(true)] out JamlConfig? config, out string? error)
    {
        try
        {
            config = FromJaml(text);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            config = null;
            error = ex.Message;
            return false;
        }
    }
}

[YamlStaticContext]
public partial class JamlYamlContext : YamlDotNet.Serialization.StaticContext;
