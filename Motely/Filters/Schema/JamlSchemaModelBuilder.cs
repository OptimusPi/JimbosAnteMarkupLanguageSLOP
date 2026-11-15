using YamlDotNet.Serialization;

namespace Motely.Filters;

/// <summary>
/// Builds the <see cref="JamlSchemaModel"/> from the code the loader binds with. Clause keys come
/// from <see cref="JamlClauseBinding.ClauseTypeByKey"/>, members from
/// <see cref="JamlConfigLoader.BoundMembers"/>, value forms from the converters in
/// <see cref="JamlClauseBinding.Converters"/>, and limits from the rule tables beside
/// <see cref="JamlConfigLoader.ClauseRules"/>.
///
/// A rule row names one member, but a filter can reach that member under other spellings
/// (<c>ante:</c> for <c>antes:</c>, <c>and:</c> for <c>clauses:</c>, <c>luckyMoney:</c> for
/// <c>rolls:</c>). No list of those is kept here: for each member the builder binds a small
/// document through the loader's own deserializer, with a value just outside the row, and asks
/// <see cref="JamlConfigLoader.Breaks"/> whether the row is broken. A member that can break a row
/// carries the row's limit.
/// </summary>
internal sealed class JamlSchemaModelBuilder
{
    private const string NullWord = "~";

    private readonly Dictionary<Type, JamlTypeModel> blocks = [];
    private readonly List<Type> blockOrder = [];
    private readonly List<Type> enums = [];
    private readonly string sampleClause;
    private readonly List<string> missing = [];
    private readonly Dictionary<(Type, string), Type> narrowed = [];
    private readonly HashSet<(Type, string)> described = [];

    private JamlSchemaModelBuilder()
    {
        var (key, type) = JamlClauseBinding.ClauseTypeByKey.First();
        var member = JamlConfigLoader.BoundMember(type, key);
        sampleClause = $"{{{key}: {Sample(type, member)}}}";
    }

    public static JamlSchemaModel Build()
    {
        var builder = new JamlSchemaModelBuilder();
        var clauses = new List<JamlTypeModel>();
        foreach (var type in JamlClauseBinding.ClauseTypeByKey.Values.Distinct())
        {
            var keys = JamlClauseBinding.ClauseTypeByKey.Where(pair => pair.Value == type).Select(pair => pair.Key).ToArray();
            clauses.Add(builder.BuildType(type, type, JamlConfigLoader.ClauseRules, sorted: true) with { Keys = keys });
        }
        var filter = builder.BuildType(typeof(JamlConfig), typeof(JamlConfig), JamlConfigLoader.FilterRules, sorted: false);
        foreach (var clause in clauses)
        {
            foreach (var key in clause.Keys)
                if (clause.Members.All(member => member.Name != key))
                    throw new InvalidOperationException($"clause key '{key}' is not a member of {clause.Type.Name}.");
        }
        return new JamlSchemaModel(filter, [.. builder.blockOrder.Select(type => builder.blocks[type])], clauses, builder.enums)
        {
            MissingDescriptions = builder.missing,
            OrphanDescriptions =
            [
                .. JamlSchemaAnnotations.Text.Keys
                    .Where(key => !builder.described.Contains(key))
                    .Select(key => JamlSchemaAnnotations.Name(key.Owner, key.Member)),
            ],
        };
    }

    private string Describe(Type type, string member)
    {
        if (JamlSchemaAnnotations.Find(type, member) is { } key)
        {
            described.Add(key);
            return JamlSchemaAnnotations.Text[key];
        }
        missing.Add(JamlSchemaAnnotations.Name(type, member));
        return "";
    }

    /// <summary>The name a definition goes by: the type's name without its Motely or Jaml prefix.</summary>
    internal static string BareName(Type type)
    {
        var name = type.Name;
        foreach (var prefix in (string[])["Motely", "Jaml"])
            if (name.StartsWith(prefix, StringComparison.Ordinal) && name.Length > prefix.Length)
                return name[prefix.Length..];
        return name;
    }

    private JamlTypeModel BuildType(Type type, Type membersFrom, JamlRule[] table, bool sorted)
    {
        var rules = table.Where(rule => rule.Site == JamlRuleSite.Loader && rule.AppliesTo(membersFrom)).ToArray();
        var bound = JamlConfigLoader.BoundMembers(membersFrom);
        foreach (var rule in rules)
            JamlConfigLoader.BoundMember(membersFrom, rule.Member);

        var members = new List<JamlMemberModel>();
        foreach (var member in sorted ? [.. bound.OrderBy(member => member.Name, StringComparer.Ordinal)] : bound)
        {
            if (rules.Any(rule => rule.Kind == JamlRuleKind.Forbidden && rule.Member == member.Name))
                continue;
            members.Add(BuildMember(membersFrom, member, rules));
        }

        var bare = BareName(type);
        return new JamlTypeModel(
            type,
            char.ToLowerInvariant(bare[0]) + bare[1..],
            "Jaml" + bare,
            Describe(type, ""),
            members)
        {
            OneOfWritten = [.. OneOfWritten(membersFrom, members, rules)],
        };
    }

    private JamlMemberModel BuildMember(Type owner, IPropertyDescriptor member, JamlRule[] rules)
    {
        var name = member.Name;
        var kind = Classify(member.Type);
        var description = Describe(owner, name);
        foreach (var rule in rules)
            if (rule.Kind == JamlRuleKind.NotBelowMin && rule.Member == name)
                description += $" The loader also refuses a value below min, which this schema cannot check: \"{rule.Message}\"";

        var readsNullAsAny = kind is JamlValueKind.Names or JamlValueKind.EventRolls or JamlValueKind.Card;
        var model = new JamlMemberModel(name, kind, description)
        {
            Enum = kind switch
            {
                JamlValueKind.Name => Nullable.GetUnderlyingType(member.Type) ?? member.Type,
                JamlValueKind.Names => member.Type.GetElementType(),
                JamlValueKind.Card => typeof(MotelyStandardCard),
                _ => null,
            },
            Block = kind switch
            {
                JamlValueKind.Block => member.Type,
                JamlValueKind.Blocks => member.Type.GetElementType(),
                JamlValueKind.Card => typeof(JamlStandardCardSlot),
                _ => null,
            },
            Null = (member.AllowNulls || readsNullAsAny || ReplacesNull(owner, member)) && Admits(owner, name, NullWord, rules),
            Any = readsNullAsAny && Admits(owner, name, JamlClauseBinding.AnyWords[0], rules),
        };

        foreach (var rule in JamlConfigLoader.BinderRules.Where(rule => rule.AppliesTo(owner) && rule.Member == name))
        {
            model = rule.Kind switch
            {
                JamlRuleKind.Names => model with { Enum = rule.NamesOf },
                JamlRuleKind.Present => model with { Required = true },
                _ => throw new NotSupportedException($"a {rule.Kind} rule is not read as a binder rule."),
            };
        }

        // A member that can break a names rule reads only that rule's names.
        if (kind is JamlValueKind.Name or JamlValueKind.Names)
        {
            foreach (var rule in rules.Where(rule => rule.Kind == JamlRuleKind.Names))
            {
                var allowed = Enum.GetNames(rule.NamesOf!);
                if (Enum.GetNames(model.Enum!).FirstOrDefault(candidate => !allowed.Contains(candidate)) is { } outside
                    && Breaks(owner, name, outside, rule) == true)
                    model = model with { Enum = rule.NamesOf };
            }
        }
        if (model.Enum is { } reads)
            narrowed[(owner, name)] = reads;

        if (model.IsList)
            model = WithItemLimits(owner, member, model, rules);
        if (kind is JamlValueKind.Number or JamlValueKind.Numbers or JamlValueKind.EventRolls)
            model = WithNumberLimits(owner, model, rules);

        if (model.Enum is { } named && !enums.Contains(named))
            enums.Add(named);
        if (model.Block is { } block)
            AddBlock(block);
        return model;
    }

    private JamlMemberModel WithItemLimits(Type owner, IPropertyDescriptor member, JamlMemberModel model, JamlRule[] rules)
    {
        int minItems = Admits(owner, model.Name, "[]", rules) ? 0 : 1;
        int? maxItems = null;
        foreach (var rule in rules)
        {
            if (rule.Kind == JamlRuleKind.MinItems && rule.Min > minItems
                && Breaks(owner, model.Name, ListOf(owner, member, rule.Min.Value - 1), rule) == true)
                minItems = rule.Min.Value;
            if (rule.Kind == JamlRuleKind.MaxItems
                && Breaks(owner, model.Name, ListOf(owner, member, rule.Max!.Value + 1), rule) == true)
                maxItems = rule.Max.Value;
        }
        if (!Admits(owner, model.Name, ListOf(owner, member, Math.Max(minItems, 1)), rules))
            throw new InvalidOperationException($"{owner.Name}.{model.Name}: no list of {Math.Max(minItems, 1)} loads.");
        return model with { MinItems = minItems, MaxItems = maxItems };
    }

    private JamlMemberModel WithNumberLimits(Type owner, JamlMemberModel model, JamlRule[] rules)
    {
        bool list = model.Kind != JamlValueKind.Number;
        int? min = list ? JamlConfigLoader.ListNumber.Min : null;
        int? max = list ? JamlConfigLoader.ListNumber.Max : null;
        var excluded = new List<int>();
        string Written(int number) => list ? $"[{number}]" : $"{number}";

        foreach (var rule in rules)
        {
            if (rule.Kind == JamlRuleKind.Range)
            {
                if (rule.Min is { } low && Breaks(owner, model.Name, Written(low - 1), rule) == true)
                    min = min is { } current ? Math.Max(current, low) : low;
                if (rule.Max is { } high && Breaks(owner, model.Name, Written(high + 1), rule) == true)
                    max = max is { } current ? Math.Min(current, high) : high;
            }
            if (rule.Kind == JamlRuleKind.Excluded && Breaks(owner, model.Name, Written(rule.Value), rule) == true)
                excluded.Add(rule.Value);
        }

        // An excluded number at the low end is a higher minimum.
        while (min is { } floor && excluded.Remove(floor))
            min = floor + 1;
        return model with { Min = min, Max = max, Excluded = excluded };
    }

    /// <summary>
    /// A list rule the type breaks when nothing is written (an <c>and</c> has no clauses, an event
    /// has no rolls) means one of the members that can fill the list must be written.
    /// </summary>
    private IEnumerable<IReadOnlyList<string>> OneOfWritten(Type owner, List<JamlMemberModel> members, JamlRule[] rules)
    {
        foreach (var rule in rules)
        {
            if (rule.Kind != JamlRuleKind.MinItems || !JamlConfigLoader.Breaks(rule, Bind(owner, "{}")!))
                continue;
            var fillers = new List<string>();
            foreach (var member in members.Where(member => member.IsList))
            {
                var written = ListOf(owner, JamlConfigLoader.BoundMember(owner, member.Name), rule.Min!.Value);
                if (Bind(owner, $"{{{member.Name}: {written}}}") is { } bound && !JamlConfigLoader.Breaks(rule, bound))
                    fillers.Add(member.Name);
            }
            if (fillers.Count == 0)
                throw new InvalidOperationException($"{owner.Name}: nothing a filter writes satisfies \"{rule.Message}\"");
            yield return fillers;
        }
    }

    private void AddBlock(Type type)
    {
        if (blocks.ContainsKey(type))
            return;
        // The card converter reads a mapping through its own type, which has no whole-card member.
        var membersFrom = type == typeof(JamlStandardCardSlot) ? typeof(JamlStandardCardSlotMapping) : type;
        blocks[type] = null!;
        var model = BuildType(type, membersFrom, [], sorted: true);
        blocks[type] = model;
        blockOrder.Add(type);
    }

    private static JamlValueKind Classify(Type type)
    {
        var converter = JamlClauseBinding.Converters.FirstOrDefault(candidate => candidate.Accepts(type));
        if (converter is not null)
        {
            return converter switch
            {
                JamlClauseBinding.IntListConverter => JamlValueKind.Numbers,
                JamlEventRolls.Converter => JamlValueKind.EventRolls,
                JamlStandardCardSlotConverter => JamlValueKind.Card,
                _ when converter.GetType().IsGenericType
                    && converter.GetType().GetGenericTypeDefinition() == typeof(JamlClauseBinding.ScalarOrSequenceConverter<>)
                    => JamlValueKind.Names,
                _ => throw new NotSupportedException(
                    $"the schema generator does not know how {converter.GetType().Name} reads a value: teach JamlSchemaModelBuilder.Classify."),
            };
        }

        var bare = Nullable.GetUnderlyingType(type) ?? type;
        if (bare == typeof(string))
            return JamlValueKind.Text;
        if (bare == typeof(int))
            return JamlValueKind.Number;
        if (bare == typeof(bool))
            return JamlValueKind.Flag;
        if (bare.IsEnum)
            return JamlValueKind.Name;
        if (bare == typeof(List<string>))
            return JamlValueKind.Texts;
        if (bare == typeof(List<object>) || bare == typeof(object[]))
            return JamlValueKind.Clauses;
        if (bare.IsArray && bare.GetElementType() is { IsClass: true } element && element != typeof(string))
            return JamlValueKind.Blocks;
        if (bare.IsClass && !bare.IsArray && !bare.IsGenericType)
            return JamlValueKind.Block;
        throw new NotSupportedException(
            $"the schema generator does not know how a {type} member is written: teach JamlSchemaModelBuilder.Classify.");
    }

    // ── documents bound through the loader ──

    private static object? Bind(Type type, string document)
    {
        try
        {
            return JamlConfigLoader.Bind(document, type);
        }
        catch (Exception)
        {
            // The loader refused the document, which is the answer.
            return null;
        }
    }

    /// <summary>
    /// Whether writing <paramref name="written"/> under <paramref name="name"/> breaks
    /// <paramref name="rule"/>, with the rule's own member set to a value that satisfies it first.
    /// Null when the document does not bind at all.
    /// </summary>
    private bool? Breaks(Type owner, string name, string written, JamlRule rule)
    {
        var document = rule.Member == name || rule.Kind is JamlRuleKind.Forbidden or JamlRuleKind.NotBelowMin
            ? $"{{{name}: {written}}}"
            : $"{{{rule.Member}: {Satisfying(owner, rule)}, {name}: {written}}}";
        return Bind(owner, document) is { } bound ? JamlConfigLoader.Breaks(rule, bound) : null;
    }

    /// <summary>Whether the loader binds <paramref name="written"/> under the member and no rule is broken by it.</summary>
    private bool Admits(Type owner, string name, string written, JamlRule[] rules)
    {
        if (Bind(owner, $"{{{name}: {written}}}") is null)
            return false;
        foreach (var rule in rules)
            if (Breaks(owner, name, written, rule) != false)
                return false;
        return true;
    }

    /// <summary>A value for the rule's own member that the rule accepts.</summary>
    private string Satisfying(Type owner, JamlRule rule)
    {
        var member = JamlConfigLoader.BoundMember(owner, rule.Member);
        bool list = Classify(member.Type) != JamlValueKind.Number;
        string Written(int number) => list ? $"[{number}]" : $"{number}";
        var written = rule.Kind switch
        {
            JamlRuleKind.Range => Written(rule.Min ?? rule.Max!.Value),
            JamlRuleKind.Excluded => Written(rule.Value + 1),
            JamlRuleKind.NotNull => Sample(owner, member),
            JamlRuleKind.MinItems => ListOf(owner, member, rule.Min!.Value),
            JamlRuleKind.MaxItems => ListOf(owner, member, Math.Min(1, rule.Max!.Value)),
            JamlRuleKind.NoNullItems => ListOf(owner, member, 1),
            JamlRuleKind.Names => Enum.GetNames(rule.NamesOf!)[0],
            _ => throw new NotSupportedException($"a {rule.Kind} rule has no satisfying value."),
        };
        if (Bind(owner, $"{{{rule.Member}: {written}}}") is not { } bound || JamlConfigLoader.Breaks(rule, bound))
            throw new InvalidOperationException($"{owner.Name}.{rule.Member}: {written} does not satisfy \"{rule.Message}\"");
        return written;
    }

    // ── sample values, each the first thing the member's form allows ──

    private string Sample(Type owner, IPropertyDescriptor member) =>
        Classify(member.Type) switch
        {
            JamlValueKind.Text => "x",
            JamlValueKind.Number => "1",
            JamlValueKind.Flag => "true",
            JamlValueKind.Name => FirstName(owner, member, Nullable.GetUnderlyingType(member.Type) ?? member.Type),
            JamlValueKind.Card => Enum.GetNames<MotelyStandardCard>()[0],
            JamlValueKind.Block => SampleBlock(member.Type),
            _ => ListOf(owner, member, 1),
        };

    private string ListOf(Type owner, IPropertyDescriptor member, int count)
    {
        var item = Classify(member.Type) switch
        {
            JamlValueKind.Texts => "x",
            JamlValueKind.Numbers or JamlValueKind.EventRolls => "1",
            JamlValueKind.Names => FirstName(owner, member, member.Type.GetElementType()!),
            JamlValueKind.Clauses => sampleClause,
            JamlValueKind.Blocks => SampleBlock(member.Type.GetElementType()!),
            var kind => throw new NotSupportedException($"a {kind} member is not a list."),
        };
        return "[" + string.Join(", ", Enumerable.Repeat(item, count)) + "]";
    }

    private string FirstName(Type owner, IPropertyDescriptor member, Type enumType)
    {
        if (narrowed.TryGetValue((owner, member.Name), out var reads))
            return Enum.GetNames(reads)[0];
        foreach (var rule in JamlConfigLoader.BinderRules)
            if (rule.Kind == JamlRuleKind.Names && rule.AppliesTo(owner) && rule.Member == member.Name)
                enumType = rule.NamesOf!;
        return Enum.GetNames(enumType)[0];
    }

    /// <summary>
    /// Whether the loader hands back a value where the member was written as null (a filter's
    /// <c>id: ~</c> loads as "", a <c>sources:</c> list as empty), so null is a way to write it.
    /// </summary>
    private static bool ReplacesNull(Type owner, IPropertyDescriptor member)
    {
        var document = $"{{{member.Name}: {NullWord}}}";
        object? loaded = owner == typeof(JamlConfig)
            ? JamlConfigLoader.TryLoad(document, out var config, out _) ? config : null
            : Bind(owner, document);
        return loaded is not null && member.Read(loaded).Value is not null;
    }

    private string SampleBlock(Type type)
    {
        var written = new List<string>();
        foreach (var rule in JamlConfigLoader.BinderRules)
            if (rule.Kind == JamlRuleKind.Present && rule.AppliesTo(type))
                written.Add($"{rule.Member}: {Sample(type, JamlConfigLoader.BoundMember(type, rule.Member))}");
        return "{" + string.Join(", ", written) + "}";
    }
}
