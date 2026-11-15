using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Motely.Filters;

/// <summary>JAML text → <see cref="JamlConfig"/> through YamlDotNet's static (AOT) deserializer. JSON is YAML too.</summary>
public static partial class JamlConfigLoader
{
    private static readonly IDeserializer Deserializer = BuildDeserializer();

    private static IDeserializer BuildDeserializer()
    {
        var builder = new StaticDeserializerBuilder(new JamlYamlContext())
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
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
            CheckBounds(config.Must);
            CheckBounds(config.Should);
            CheckBounds(config.MustNot);
            if (config.Should.Count > MotelyScoredSeedResult.MAX_TALLY_COUNT)
                throw new InvalidOperationException(
                    $"JAML: should: holds {config.Should.Count} clauses; a filter takes at most {MotelyScoredSeedResult.MAX_TALLY_COUNT}.");
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
            var reason = inner.InnerException is NullReferenceException
                ? "this key needs a value; ~ is not one."
                : inner.InnerException?.Message ?? inner.Message;
            var message = $"JAML line {inner.Start.Line}: {reason}";
            throw new JamlFilterException(message, (int)inner.Start.Line, (int)inner.Start.Column, ex);
        }
    }

    private static void CheckBounds(IEnumerable<IMotelyClause> clauses)
    {
        foreach (var clause in clauses)
        {
            if (clause.Min < 0 || clause.Max < 0)
                throw OutOfRange(clause, "min and max run from 0 up.");
            if (clause.Max < clause.Min)
                throw OutOfRange(clause, "max is below min (min defaults to 1), so nothing can match.");
            if (clause is AndClause && (clause.Min != 1 || clause.Max is not null))
                throw OutOfRange(clause, "an and takes no min or max: set them on its clauses.");
            if (clause is OrClause { Max: not null })
                throw OutOfRange(clause, "an or takes no max: set it on its clauses.");
            if (clause is LogicClause { Clauses.Length: 0 })
                throw OutOfRange(clause, "clauses: needs at least one clause.");
            if (clause is IAnteScopedClause { Antes: null })
                throw OutOfRange(clause, "antes: needs a number or a list of them.");
            if (clause is IRollScopedClause { Rolls: null })
                throw OutOfRange(clause, "rolls: needs a number or a list of them.");
            if (clause is IWithScopedClause { With: null })
                throw OutOfRange(clause, "with: needs a mapping such as { jokers: [Showman] }.");
            if (clause is EventClause or VoucherClause or TagClause or BoosterPackClause
                && ((IRollScopedClause)clause).Rolls.Length == 0)
                throw OutOfRange(clause, "rolls: needs at least one roll.");
            if (clause is BossClause { Antes.Length: 0 })
                throw OutOfRange(clause, "antes: needs at least one ante.");
            if (clause is BossClause { Antes: { } bossAntes } && bossAntes.Contains(0))
                throw OutOfRange(clause, "boss antes run from 1: ante 0 has no boss blind.");
            if (clause is OrClause { Min: < 1 })
                throw OutOfRange(clause, "an or needs min: 1 or more.");
            if (clause is IAnteScopedClause { Antes: { } antes } && antes.Any(ante => ante > MotelyGlobals.MaxAnte))
                throw OutOfRange(clause, $"antes run from 0 to {MotelyGlobals.MaxAnte}.");
            if (clause is LegendaryJokerClause { SoulEditionRolls: < 0 or > MotelyGlobals.MaxRollIndex })
                throw OutOfRange(clause, $"soulEditionRolls runs from 0 to {MotelyGlobals.MaxRollIndex}.");
            if (clause is LegendarySoulOrderClause { MaxAnte: < 0 or > MotelyGlobals.MaxAnte })
                throw OutOfRange(clause, $"maxAnte runs from 0 to {MotelyGlobals.MaxAnte}.");
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
