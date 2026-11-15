using System.Collections;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Motely.Filters;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Motely.Tests;

/// <summary>
/// The JAML grammar, read off the code the loader binds with, and the three files written from it
/// (<c>Motely.Wasm/jaml/</c>: the JSON Schema, the TypeScript declarations and the vocabulary).
///
/// Nothing here lists a key, a member or a name. They come from:
/// <list type="bullet">
/// <item><see cref="JamlClauseBinding.ClauseTypeByKey"/>: every clause key and its class.</item>
/// <item><see cref="MembersOf"/>: YamlDotNet's static type inspector (the Vecc-generated <see cref="JamlYamlContext"/>), the members the deserializer binds.</item>
/// <item><see cref="JamlClauseBinding.Converters"/>: which members read a name, a list, <c>Any</c>.</item>
/// <item><see cref="JamlSchemaRules.All"/>: the value rules, restated from the loader's checks and held to them by <see cref="JamlGrammarTests"/>.</item>
/// <item><see cref="Enum.GetNames(Type)"/> and <see cref="JamlEnumName"/>: the names and their loose spellings.</item>
/// </list>
/// Which member is another spelling of which (<c>ante</c> of <c>antes</c>, <c>and</c> of
/// <c>clauses</c>) is measured: a value is written to one member and the others are read back.
/// </summary>
internal sealed class JamlGrammar
{
    internal const string Regenerate = "MOTELY_WRITE_JAML=1 dotnet test Motely.Tests --filter JamlGrammarTests";

    internal enum Form { Text, Int, Bool, Name, Names, Ints, EventRolls, CardSlot, Object, Objects, Clauses, Texts, NoValue }

    internal sealed class Member(string key, Form form)
    {
        public string Key { get; } = key;
        public Form Form { get; set; } = form;
        public bool Nullable { get; set; }
        public bool Required { get; set; }
        public Type? Enum { get; set; }
        public Type? Object { get; set; }
        public int? Min { get; set; }
        public int? Max { get; set; }
        public int? MinItems { get; set; }

        /// <summary>The rule a schema cannot state (it compares two members), in the loader's words.</summary>
        public string? Note { get; set; }

        /// <summary>Other members this one is another way to write.</summary>
        public List<string> Feeds { get; } = [];
    }

    internal sealed class TypeDef(Type clr, string name)
    {
        public Type Clr { get; } = clr;

        /// <summary>The definition's name in the schema (<c>jokerClause</c>, <c>with</c>).</summary>
        public string Name { get; } = name;
        public string TsName => "Jaml" + char.ToUpperInvariant(Name[0]) + Name[1..];
        public SortedDictionary<string, Member> Members { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// A member that must end up non-empty and starts empty: the document has to write it, or
        /// one of the members that feed it. Null when the type has no such member.
        /// </summary>
        public (string Member, List<string> Feeders, int Min)? NeedsOneOf { get; set; }

        /// <summary>
        /// Members that start long enough and must stay so: when the document does not write the
        /// member itself, a feeder it writes as a list has to hold that many.
        /// </summary>
        public List<(string Member, List<string> Feeders, int Min)> KeepFilled { get; } = [];

        /// <summary>
        /// A number that may not be below another member: while the other is not written it holds
        /// its starting value, so the number may not be below that. (Both written is the part a
        /// schema cannot state; the member's description carries the rule in words.)
        /// </summary>
        public List<(string Member, string Other, int OtherStartsAt)> NotBelowStart { get; } = [];
    }

    private static readonly JamlYamlContext Context = new();

    /// <summary>
    /// The words the loader's converters read as "any" where a name list, a card or a roll list
    /// goes. Restated from their three private tests; <see cref="JamlGrammarTests"/> writes each
    /// word, and words that are not here, to every such member and compares verdicts.
    /// </summary>
    internal static readonly string[] AnyWords = ["any", "~", "null"];

    /// <summary>Blank, or one of <see cref="AnyWords"/> in any case, spaces around it aside.</summary>
    private static readonly string AnyPattern =
        $@"^\s*(?:{string.Join("|", AnyWords.Select(word => string.Concat(word.Select(AnyCase))))})?\s*$";

    private static readonly ITypeInspector Inspector = Context.GetTypeInspector();
    private static readonly ConcurrentDictionary<Type, Dictionary<string, IPropertyDescriptor>> Bound = new();

    /// <summary>
    /// The members the loader binds on <paramref name="type"/>, by the key a document writes (the
    /// alias when there is one, else the camelCase name). Read off YamlDotNet's static type
    /// inspector: the table the deserializer itself binds from.
    /// </summary>
    internal static IReadOnlyDictionary<string, IPropertyDescriptor> MembersOf(Type type) =>
        Bound.GetOrAdd(type, static t =>
        {
            var members = new Dictionary<string, IPropertyDescriptor>(StringComparer.OrdinalIgnoreCase);
            foreach (var member in Inspector.GetProperties(t, null))
            {
                if (!member.CanWrite || member.GetCustomAttribute<YamlIgnoreAttribute>() is not null)
                    continue;
                members[member.GetCustomAttribute<YamlMemberAttribute>()?.Alias ?? CamelCaseNamingConvention.Instance.Apply(member.Name)] = member;
            }
            return members;
        });

    private static string AnyCase(char c) =>
        char.IsLetter(c) ? $"[{char.ToLowerInvariant(c)}{char.ToUpperInvariant(c)}]" : System.Text.RegularExpressions.Regex.Escape(c.ToString());

    public TypeDef Root { get; }
    public List<TypeDef> Objects { get; } = [];
    public List<TypeDef> Clauses { get; } = [];
    public List<Type> Enums { get; } = [];
    public IReadOnlyDictionary<string, Type> ClauseKeys => JamlClauseBinding.ClauseTypeByKey;

    private readonly Dictionary<Type, TypeDef> _defs = [];

    public JamlGrammar()
    {
        Root = Describe(typeof(JamlConfig), isClause: false);
        Objects.Remove(Root);
        foreach (var type in ClauseKeys.Values.Distinct())
            Describe(type, isClause: true);
    }

    public TypeDef DefOf(Type type) => _defs[type];

    // ── the walk ────────────────────────────────────────────────────────────────

    private TypeDef Describe(Type type, bool isClause)
    {
        if (_defs.TryGetValue(type, out var known))
            return known;
        var def = new TypeDef(type, DefName(type));
        _defs[type] = def;
        (isClause ? Clauses : Objects).Add(def);

        var bound = MembersOf(type);
        _describing.Push(type);
        foreach (var (key, descriptor) in bound.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            def.Members[key] = Shape(key, descriptor);
        _describing.Pop();

        MeasureFeeds(def, bound);
        foreach (var member in def.Members.Values)
            ApplyRules(def, member);
        FindListNeeds(def, bound);
        return def;
    }

    private Member Shape(string key, IPropertyDescriptor descriptor)
    {
        var clr = descriptor.Type;
        var under = System.Nullable.GetUnderlyingType(clr) ?? clr;
        var member = new Member(key, Form.Text) { Nullable = TakesNull(descriptor) };
        switch (JamlClauseBinding.Converters.FirstOrDefault(c => c.Accepts(clr)))
        {
            case JamlClauseBinding.IntListConverter:
                member.Form = Form.Ints;
                (member.Min, member.Max) = (0, MotelyGlobals.MaxRollIndex);
                return member;
            case JamlEventRolls.Converter:
                member.Form = Form.EventRolls;
                member.Nullable = true; // the converter reads null as Any
                (member.Min, member.Max) = (0, MotelyGlobals.MaxRollIndex);
                return member;
            case JamlStandardCardSlotConverter:
                member.Form = Form.CardSlot;
                member.Nullable = true; // the converter reads null as Any
                member.Enum = Use(typeof(MotelyStandardCard));
                member.Object = Describe(typeof(JamlStandardCardSlotMapping), isClause: false).Clr;
                return member;
            case { } converter when converter.GetType().IsGenericType
                && converter.GetType().GetGenericTypeDefinition() == typeof(JamlClauseBinding.ScalarOrSequenceConverter<>):
                member.Form = Form.Names;
                member.Enum = Use(clr.GetElementType()!);
                member.Nullable = true; // the converter reads null as Any
                return member;
            case { } unknown:
                throw new InvalidOperationException($"JamlGrammar does not know the converter {unknown.GetType().Name} ({key}): teach Shape what it reads.");
        }

        if (clr == typeof(string))
            member.Form = Form.Text;
        else if (under == typeof(int))
            member.Form = Form.Int;
        else if (under == typeof(bool))
            member.Form = Form.Bool;
        else if (under.IsEnum)
            (member.Form, member.Enum) = (Form.Name, Use(under));
        else if (clr == typeof(List<string>))
            member.Form = Form.Texts;
        else if (clr == typeof(object[]) || clr == typeof(List<object>))
            member.Form = Form.Clauses;
        else if (clr.IsArray && IsOurClass(clr.GetElementType()!))
            (member.Form, member.Object) = (Form.Objects, Describe(clr.GetElementType()!, isClause: false).Clr);
        else if (IsOurClass(clr))
            (member.Form, member.Object) = (Form.Object, Describe(clr, isClause: false).Clr);
        else
            throw new InvalidOperationException($"JamlGrammar does not know the type {clr} ({key}): teach Shape what it reads.");
        return member;
    }

    /// <summary>
    /// Whether the loader takes <c>key: ~</c>: the member's type can hold null and its setter does
    /// not refuse it. (The inspector's AllowNulls is the C# annotation, which the loader does not enforce.)
    /// </summary>
    private bool TakesNull(IPropertyDescriptor descriptor)
    {
        if (descriptor.Type.IsValueType && System.Nullable.GetUnderlyingType(descriptor.Type) is null)
            return false;
        try
        {
            descriptor.Write(Context.GetFactory().Create(_describing.Peek())!, null);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private readonly Stack<Type> _describing = new();

    private static bool IsOurClass(Type type) => type.IsClass && type.Namespace?.StartsWith("Motely", StringComparison.Ordinal) == true;

    private Type Use(Type enumType)
    {
        if (!Enums.Contains(enumType))
            Enums.Add(enumType);
        return enumType;
    }

    private static string DefName(Type type)
    {
        var name = type.Name;
        foreach (var prefix in new[] { "Motely", "Jaml" })
            if (name.StartsWith(prefix, StringComparison.Ordinal))
                name = name[prefix.Length..];
        if (name.EndsWith("Mapping", StringComparison.Ordinal))
            name = name[..^"Mapping".Length];
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    /// <summary>Writes a value to each member of a fresh instance and records which other members changed.</summary>
    private static void MeasureFeeds(TypeDef def, IReadOnlyDictionary<string, IPropertyDescriptor> bound)
    {
        var baseline = Snapshot(Context.GetFactory().Create(def.Clr), bound);
        foreach (var (key, descriptor) in bound)
        {
            if (Sentinel(descriptor.Type) is not { } sentinel)
                continue;
            var instance = Context.GetFactory().Create(def.Clr);
            try
            {
                descriptor.Write(instance, sentinel);
            }
            catch (Exception refused)
            {
                throw new InvalidOperationException($"JamlGrammar could not write a test value to {def.Clr.Name}.{key} ({refused.Message}): teach Sentinel a value its setter takes.", refused);
            }
            var after = Snapshot(instance, bound);
            foreach (var other in bound.Keys)
                if (!other.Equals(key, StringComparison.OrdinalIgnoreCase) && after[other] != baseline[other])
                    def.Members[key].Feeds.Add(other);
        }
    }

    private static Dictionary<string, string> Snapshot(object instance, IReadOnlyDictionary<string, IPropertyDescriptor> bound) =>
        bound.ToDictionary(pair => pair.Key, pair => Render(pair.Value.Read(instance).Value, 0), StringComparer.OrdinalIgnoreCase);

    private static string Render(object? value, int depth) =>
        value switch
        {
            null => "null",
            string text => text,
            IEnumerable list => "[" + string.Join(",", list.Cast<object?>().Select(item => Render(item, depth + 1))) + "]",
            _ when depth < 3 && IsOurClass(value.GetType()) =>
                "{" + string.Join(",", MembersOf(value.GetType()).Select(m => m.Key + ":" + Render(m.Value.Read(value).Value, depth + 1))) + "}",
            _ => value.ToString() ?? "",
        };

    /// <summary>A value unlike any default, of the type a member takes; null when the type has none worth writing.</summary>
    private static object? Sentinel(Type type)
    {
        var under = System.Nullable.GetUnderlyingType(type) ?? type;
        if (under == typeof(int)) return 7;
        if (under == typeof(bool)) return true;
        if (under == typeof(string)) return "§";
        if (under.IsEnum) return Enum.GetValues(under).Cast<object>().Last();
        if (under == typeof(List<string>)) return new List<string> { "§" };
        if (under == typeof(object[])) return new object[] { Context.GetFactory().Create(JamlClauseBinding.ClauseTypeByKey.Values.First())! };
        if (under == typeof(List<object>)) return new List<object> { Context.GetFactory().Create(JamlClauseBinding.ClauseTypeByKey.Values.First())! };
        if (under == typeof(JamlEventRolls)) return new JamlEventRolls { Rolls = [7] };
        if (under.IsArray)
        {
            var element = Sentinel(under.GetElementType()!) ?? Filled(under.GetElementType()!);
            if (element is null) return null;
            var array = Array.CreateInstance(under.GetElementType()!, 1);
            array.SetValue(element, 0);
            return array;
        }
        return under == typeof(JamlStandardCardSlot) ? Filled(under) : null;
    }

    /// <summary>An instance of one of our classes with a sentinel in every member that takes one.</summary>
    private static object? Filled(Type type)
    {
        if (!IsOurClass(type) || !Context.IsKnownType(type) && type != typeof(JamlStandardCardSlot))
            return null;
        if (type == typeof(JamlStandardCardSlot))
            return new JamlStandardCardSlot { Card = Enum.GetValues<MotelyStandardCard>().Last() };
        var instance = Context.GetFactory().Create(type)!;
        foreach (var member in MembersOf(type).Values)
            if (Sentinel(member.Type) is { } value)
                member.Write(instance, value);
        return instance;
    }

    private void ApplyRules(TypeDef def, Member member)
    {
        // A rule on a member covers every member that is another way to write it, except a rule
        // the first member's own setter enforces, or one about the key being written at all.
        var direct = RulesOn(def.Clr, member.Key).ToList();
        var fed = member.Feeds.SelectMany(other => RulesOn(def.Clr, other))
            .Where(rule => !rule.BySetter && rule.Kind is JamlRuleKind.Range or JamlRuleKind.ItemRange or JamlRuleKind.Names);
        foreach (var rule in direct.Concat(fed))
        {
            switch (rule.Kind)
            {
                case JamlRuleKind.Range when member.Form == Form.Int:
                case JamlRuleKind.ItemRange when member.Form is Form.Ints or Form.EventRolls:
                    member.Min = Tighter(member.Min, rule.Min, Math.Max);
                    member.Max = Tighter(member.Max, rule.Max, Math.Min);
                    break;
                case JamlRuleKind.MinItems:
                    member.MinItems = Tighter(member.MinItems, rule.Min, Math.Max);
                    break;
                case JamlRuleKind.Names when member.Form is Form.Name or Form.Names:
                    member.Enum = Use(rule.Names!);
                    break;
                case JamlRuleKind.NotBelow:
                    member.Note = rule.Words;
                    break;
            }
        }
        foreach (var rule in direct)
        {
            if (rule.Kind is JamlRuleKind.NotNull || rule.Kind is JamlRuleKind.MinItems && rule.Min >= 1)
                member.Nullable = false;
            if (rule.Kind is JamlRuleKind.NoValue)
                (member.Form, member.Nullable, member.Note) = (Form.NoValue, false, null); // nothing to compare: it takes no value
            if (rule.Kind is JamlRuleKind.Required)
                member.Required = true;
        }
    }

    private static IEnumerable<JamlRule> RulesOn(Type type, string member) =>
        JamlSchemaRules.All.Where(rule => rule.On.IsAssignableFrom(type) && rule.Member.Equals(member, StringComparison.OrdinalIgnoreCase));

    private static int? Tighter(int? current, int? rule, Func<int, int, int> pick) =>
        current is { } a && rule is { } b ? pick(a, b) : current ?? rule;

    /// <summary>
    /// A list that may not end up short is a rule on where the loader leaves it, whichever key
    /// wrote it last. Read off a fresh instance: a list that starts short has to be written
    /// (<see cref="TypeDef.NeedsOneOf"/>), one that starts long enough has to stay so
    /// (<see cref="TypeDef.KeepFilled"/>).
    /// </summary>
    private static void FindListNeeds(TypeDef def, IReadOnlyDictionary<string, IPropertyDescriptor> bound)
    {
        var fresh = Context.GetFactory().Create(def.Clr);
        foreach (var member in def.Members.Values)
        {
            var rule = RulesOn(def.Clr, member.Key).FirstOrDefault(r => r.Kind is JamlRuleKind.MinItems && !r.BySetter);
            if (rule?.Min is not { } min || bound[member.Key].Read(fresh).Value is not ICollection { } start)
                continue;
            var feeders = def.Members.Values.Where(m => m.Feeds.Contains(member.Key, StringComparer.OrdinalIgnoreCase)).Select(m => m.Key).ToList();
            if (start.Count < min)
                def.NeedsOneOf = def.NeedsOneOf is null ? (member.Key, feeders, min)
                    : throw new InvalidOperationException($"{def.Clr.Name} has two lists that must be written: teach JamlGrammar to state both.");
            else if (feeders.Count > 0)
                def.KeepFilled.Add((member.Key, feeders, min));
        }
        foreach (var rule in JamlSchemaRules.All.Where(r => r.Kind is JamlRuleKind.NotBelow && r.On.IsAssignableFrom(def.Clr)))
            if (def.Members[rule.Member].Form is Form.Int && bound[rule.Other!].Read(fresh).Value is int startsAt)
                def.NotBelowStart.Add((def.Members[rule.Member].Key, def.Members[rule.Other!].Key, startsAt));
    }

    // ── enums ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// The loose spellings the loader reads for a name of <paramref name="enumType"/>, as one
    /// pattern: any case, anything but a letter, digit or comma between the characters. Which
    /// spellings count is asked of <see cref="JamlEnumName.Tolerant"/> itself, for every name and
    /// digit/word alias, bare, with "the" in front and with a leading "the" dropped.
    /// (A letter outside A-Z passes as a gap here; the loader counts it as a letter and refuses.)
    /// </summary>
    internal static string LoosePattern(Type enumType)
    {
        const string gap = "[^A-Za-z0-9,]*";
        static string Spelled(string normalized) => string.Join(gap, normalized.Select(AnyCase));

        var bases = Enum.GetNames(enumType).Select(JamlEnumName.Normalize).Concat(JamlEnumName.DigitWordAliases.Keys).ToList();
        var taken = bases
            .Concat(bases.Select(b => "the" + b))
            .Concat(bases.Where(b => b.StartsWith("the", StringComparison.Ordinal)).Select(b => b[3..]))
            .Where(spelling => spelling.Length > 0 && JamlEnumName.Tolerant(enumType, spelling) is not null)
            .ToHashSet(StringComparer.Ordinal);
        // Written short: a spelling that is also taken with "the" in front sits behind an optional "the".
        var eitherWay = taken.Where(spelling => taken.Contains("the" + spelling)).Order(StringComparer.Ordinal).ToList();
        var asIs = taken.Where(spelling => !eitherWay.Contains(spelling) && !(spelling.StartsWith("the", StringComparison.Ordinal) && eitherWay.Contains(spelling[3..])))
            .Order(StringComparer.Ordinal);

        var ways = new List<string> { $"{gap}(?:{Spelled("the")}{gap})?(?:{string.Join("|", eitherWay.Select(Spelled))}){gap}" };
        ways.AddRange(asIs.Select(spelling => gap + Spelled(spelling) + gap));
        // One letter the loader reads as a name (a rank's J, Q, K, A): asked letter by letter. It
        // takes these with spaces around them only, not with the punctuation a spelled name allows.
        var letters = string.Concat(Enumerable.Range('a', 26).Select(c => ((char)c).ToString())
            .Where(letter => !taken.Contains(letter) && JamlEnumName.Tolerant(enumType, letter) is not null)
            .Select(letter => letter + letter.ToUpperInvariant()));
        if (letters.Length > 0)
            ways.Add($@"\s*[{letters}]\s*");
        if (NumbersOf(enumType).ToList() is { Count: > 0 } numbers)
            ways.Add($@"\s*0*(?:{string.Join("|", numbers)})\s*"); // a number written as text: "4"
        return $"^(?:{string.Join("|", ways)})$";
    }

    private static IEnumerable<int> NumbersOf(Type enumType) =>
        JamlEnumName.NumberIsValue(enumType) ? Enum.GetValues(enumType).Cast<object>().Select(Convert.ToInt32) : [];

    // ── the schema ──────────────────────────────────────────────────────────────

    public string Schema()
    {
        var definitions = new JsonObject { ["enums"] = new JsonObject(Enums.Select(e => KeyValuePair.Create(e.Name, (JsonNode?)EnumSchema(e)))) };
        foreach (var def in Objects.Concat(Clauses))
            definitions[def.Name] = TypeSchema(def);
        definitions["clause"] = new JsonObject
        {
            ["anyOf"] = new JsonArray(ClauseKeys.Select(pair => (JsonNode)new JsonObject
            {
                ["allOf"] = new JsonArray(Ref(DefOf(pair.Value).Name), new JsonObject { ["required"] = new JsonArray(pair.Key) }),
            }).ToArray()),
        };

        var root = new JsonObject
        {
            ["$schema"] = "http://json-schema.org/draft-07/schema#",
            ["title"] = "Motely Filter",
            ["description"] = "A Motely seed filter: the keys, enums, nesting and value rules the loader binds. "
                + $"Generated from the loader by Motely.Tests ({nameof(JamlGrammar)}); do not edit. Regenerate: {Regenerate}",
        };
        foreach (var (key, value) in TypeSchema(Root).ToList())
            root[key] = value?.DeepClone();
        root["definitions"] = definitions;
        return Serialize(root);
    }

    private static JsonObject EnumSchema(Type enumType)
    {
        var forms = new JsonArray(
            new JsonObject { ["type"] = "string", ["enum"] = new JsonArray(Enum.GetNames(enumType).Select(n => (JsonNode)n).ToArray()) },
            new JsonObject { ["type"] = "string", ["pattern"] = LoosePattern(enumType) });
        if (NumbersOf(enumType).ToList() is { Count: > 0 } numbers)
            forms.Add(new JsonObject { ["type"] = "integer", ["enum"] = new JsonArray(numbers.Select(n => (JsonNode)n).ToArray()) });
        return new JsonObject { ["anyOf"] = forms };
    }

    private JsonObject TypeSchema(TypeDef def)
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = new JsonObject(def.Members.Values.Select(m => KeyValuePair.Create(m.Key, (JsonNode?)MemberSchema(m)))),
        };
        if (def.Members.Values.Where(m => m.Required).Select(m => (JsonNode)m.Key).ToArray() is { Length: > 0 } required)
            schema["required"] = new JsonArray(required);
        var conditions = new List<JsonObject>();
        if (def.NeedsOneOf is var (needed, feeders, min))
        {
            // The needed member itself, or a feeder written as a value (not null, not Any) that is long enough.
            var ways = new JsonArray(new JsonObject { ["required"] = new JsonArray(needed) });
            foreach (var feeder in feeders)
                ways.Add(new JsonObject
                {
                    ["properties"] = new JsonObject { [feeder] = new JsonObject { ["type"] = ValueTypes(def.Members[feeder]), ["minItems"] = min } },
                    ["required"] = new JsonArray(feeder),
                });
            conditions.Add(new JsonObject { ["anyOf"] = ways });
        }
        foreach (var (kept, fedBy, least) in def.KeepFilled)
            conditions.Add(new JsonObject
            {
                // The member itself (its own schema holds its length), or no feeder written as a short list.
                ["anyOf"] = new JsonArray(
                    new JsonObject { ["required"] = new JsonArray(kept) },
                    new JsonObject { ["properties"] = new JsonObject(fedBy.Select(f => KeyValuePair.Create(f, (JsonNode?)new JsonObject { ["minItems"] = least }))) }),
            });
        foreach (var (number, other, startsAt) in def.NotBelowStart)
            conditions.Add(new JsonObject
            {
                ["anyOf"] = new JsonArray(
                    new JsonObject { ["required"] = new JsonArray(other) },
                    new JsonObject { ["properties"] = new JsonObject { [number] = new JsonObject { ["minimum"] = startsAt } } }),
            });
        if (conditions.Count == 1)
            schema["anyOf"] = conditions[0]["anyOf"]!.DeepClone();
        else if (conditions.Count > 1)
            schema["allOf"] = new JsonArray(conditions.ToArray<JsonNode>());
        return schema;
    }

    /// <summary>The JSON types a list member's value takes when it is written as a value: not null, not <c>Any</c>.</summary>
    private static JsonNode ValueTypes(Member member) =>
        member.Form is Form.Ints or Form.EventRolls ? new JsonArray("integer", "array") : "array";

    private JsonNode MemberSchema(Member member)
    {
        JsonNode Number() =>
            member.Min is { } only && only == member.Max
                ? new JsonObject { ["const"] = only }
                : Bounded(new JsonObject { ["type"] = "integer" }, member.Min, member.Max);
        JsonObject List(JsonNode items) => WithMinItems(new JsonObject { ["type"] = "array", ["items"] = items }, member.MinItems);
        JsonObject Any() => new() { ["type"] = "string", ["pattern"] = AnyPattern };

        JsonNode core = member.Form switch
        {
            Form.Text => new JsonObject { ["type"] = new JsonArray("string", "number", "boolean") },
            Form.Int => Number(),
            Form.Bool => new JsonObject { ["type"] = "boolean" },
            Form.Name => EnumRef(member.Enum!),
            Form.Names => new JsonObject { ["anyOf"] = new JsonArray(EnumRef(member.Enum!), Any(), List(EnumRef(member.Enum!))) },
            Form.Ints => new JsonObject { ["anyOf"] = new JsonArray(Number(), List(Number())) },
            Form.EventRolls => new JsonObject { ["anyOf"] = new JsonArray(Number(), List(Number()), Any()) },
            Form.CardSlot => new JsonObject { ["anyOf"] = new JsonArray(EnumRef(member.Enum!), Any(), Ref(DefOf(member.Object!).Name)) },
            Form.Object => Ref(DefOf(member.Object!).Name),
            Form.Objects => List(Ref(DefOf(member.Object!).Name)),
            Form.Clauses => List(Ref("clause")),
            Form.Texts => List(new JsonObject { ["type"] = new JsonArray("string", "number", "boolean") }),
            _ => new JsonObject { ["type"] = "null" },
        };
        if (member.Nullable)
            core = new JsonObject { ["anyOf"] = new JsonArray(core, new JsonObject { ["type"] = "null" }) };
        if (member.Note is { } note && core is JsonObject described)
            described["description"] = AlsoChecked + note;
        return core;
    }

    /// <summary>Leads the one kind of rule the schema carries as words: it compares two keys, which a schema cannot.</summary>
    internal const string AlsoChecked = "The loader also refuses: ";

    private static JsonObject Bounded(JsonObject number, int? min, int? max)
    {
        if (min is { } lower) number["minimum"] = lower;
        if (max is { } upper) number["maximum"] = upper;
        return number;
    }

    private static JsonObject WithMinItems(JsonObject list, int? minItems)
    {
        if (minItems is > 0) list["minItems"] = minItems;
        return list;
    }

    private static JsonObject Ref(string definition) => new() { ["$ref"] = "#/definitions/" + definition };

    private static JsonObject EnumRef(Type enumType) => Ref("enums/" + enumType.Name);

    // ── the TypeScript declarations ─────────────────────────────────────────────

    public string TypeScript()
    {
        var ts = new StringBuilder();
        ts.Append("// The filter shapes the engine's loader binds: every key, member, enum name and value rule.\n");
        ts.Append($"// GENERATED from the loader by Motely.Tests ({nameof(JamlGrammar)}); do not edit. Regenerate:\n");
        ts.Append($"//   {Regenerate}\n");
        ts.Append("// A filter is plain JSON: build a JamlFilter, hand JSON.stringify(filter) to JamlConfigLoader.check or\n");
        ts.Append("// fromJaml, and read one back with JSON.parse.\n\n");
        foreach (var enumType in Enums)
        {
            var names = Enum.GetNames(enumType).Select(n => $"\"{n}\"").Concat(NumbersOf(enumType).Select(n => n.ToString()));
            ts.Append($"export type {enumType.Name}Name = {string.Join(" | ", names)};\n");
        }
        ts.Append("\n/** A whole filter document, as the engine loads it from YAML or JSON. */\n");
        Interface(ts, "JamlFilter", Root);
        foreach (var def in Objects.Concat(Clauses))
            Interface(ts, def.TsName, def);
        ts.Append($"export type JamlClause = {string.Join(" | ", ClauseKeys.SelectMany(pair => ClauseArms(pair.Key, DefOf(pair.Value))))};\n");
        return ts.ToString();
    }

    private void Interface(StringBuilder ts, string name, TypeDef def)
    {
        ts.Append($"export interface {name} {{\n");
        foreach (var member in def.Members.Values)
        {
            if (member.Note is { } note)
                ts.Append($"  /** {AlsoChecked}{note} */\n");
            ts.Append($"  {member.Key}{(member.Required ? "" : "?")}: {TsType(member, values: true, empties: true)};\n");
        }
        ts.Append("}\n\n");
    }

    /// <summary>
    /// A clause is its type with its key written. When the type needs one of several members, the
    /// key either carries the value itself or is left empty with the needed member written.
    /// </summary>
    private IEnumerable<string> ClauseArms(string key, TypeDef def)
    {
        if (def.NeedsOneOf is not var (needed, feeders, min) || !feeders.Contains(key, StringComparer.OrdinalIgnoreCase))
        {
            yield return $"{def.TsName} & Required<Pick<{def.TsName}, \"{key}\">>";
            yield break;
        }
        var keyed = def.Members[key];
        yield return $"{def.TsName} & {{ {key}: {TsType(keyed, values: true, empties: false, atLeast: min)} }}";
        if (TsType(keyed, values: false, empties: true) is { Length: > 0 } empty)
            yield return $"{def.TsName} & {{ {key}: {empty}; {needed}: {TsType(def.Members[needed], values: true, empties: false)} }}";
    }

    /// <summary>The member's type: its value forms, its empty forms (<c>"Any"</c>, null), or both.</summary>
    private string TsType(Member member, bool values, bool empties, int? atLeast = null)
    {
        string List(string item) => (atLeast ?? member.MinItems) is > 0 ? $"[{item}, ...{item}[]]" : item.Contains(' ') ? $"({item})[]" : $"{item}[]";
        var number = member.Min is { } only && only == member.Max ? only.ToString() : "number";
        var name = member.Enum is { } e ? e.Name + "Name" : "";
        var (value, any) = member.Form switch
        {
            Form.Text => ("string | number | boolean", false),
            Form.Int => (number, false),
            Form.Bool => ("boolean", false),
            Form.Name => (name, false),
            Form.Names => ($"{name} | {List(name)}", true),
            Form.Ints => ($"{number} | {List(number)}", false),
            Form.EventRolls => ($"{number} | {List(number)}", true),
            Form.CardSlot => ($"{name} | {DefOf(member.Object!).TsName}", true),
            Form.Object => (DefOf(member.Object!).TsName, false),
            Form.Objects => (List(DefOf(member.Object!).TsName), false),
            Form.Clauses => (List("JamlClause"), false),
            Form.Texts => (List("string | number | boolean"), false),
            _ => ("", false),
        };
        var parts = new List<string>();
        if (values && value.Length > 0) parts.Add(value);
        if (empties && any) parts.Add("\"Any\"");
        if (empties && (member.Nullable || member.Form == Form.NoValue)) parts.Add("null");
        return string.Join(" | ", parts);
    }

    // ── the vocabulary ──────────────────────────────────────────────────────────

    /// <summary>The keys, members and names alone, for an editor: no value rules, a fraction of the schema's size.</summary>
    public string Vocabulary()
    {
        JsonObject Members(TypeDef def) => new(def.Members.Values.Select(m =>
        {
            var entry = new JsonObject { ["form"] = m.Form.ToString() };
            if (m.Enum is { } e) entry["enum"] = e.Name;
            if (m.Object is { } o) entry["type"] = DefOf(o).Name;
            if (m.Required) entry["required"] = true;
            return KeyValuePair.Create(m.Key, (JsonNode?)entry);
        }));
        return Serialize(new JsonObject
        {
            ["root"] = Members(Root),
            ["clauses"] = new JsonObject(ClauseKeys.Select(pair => KeyValuePair.Create(pair.Key, (JsonNode?)DefOf(pair.Value).Name))),
            ["types"] = new JsonObject(Objects.Concat(Clauses).Select(def => KeyValuePair.Create(def.Name, (JsonNode?)Members(def)))),
            ["enums"] = new JsonObject(Enums.Select(e =>
                KeyValuePair.Create(e.Name, (JsonNode?)new JsonArray(Enum.GetNames(e).Select(n => (JsonNode)n).ToArray())))),
        });
    }

    private static string Serialize(JsonNode node) =>
        node.ToJsonString(new JsonSerializerOptions { WriteIndented = true, IndentSize = 2, NewLine = "\n", Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n";

    // ── examples, for the tests ─────────────────────────────────────────────────

    /// <summary>One value the member takes, as JSON.</summary>
    public JsonNode? Example(Member member)
    {
        int Within(int wanted) => Math.Clamp(wanted, member.Min ?? int.MinValue, member.Max ?? int.MaxValue);
        JsonNode Object(TypeDef def) => new JsonObject(def.Members.Values.Where(m => m.Required).Select(m => KeyValuePair.Create(m.Key, Example(m))));
        return member.Form switch
        {
            Form.Text => "x",
            Form.Int => Within(1),
            Form.Bool => true,
            Form.Name or Form.Names or Form.CardSlot => Enum.GetNames(member.Enum!)[0],
            Form.Ints or Form.EventRolls => new JsonArray(Within(1)),
            Form.Object => Object(DefOf(member.Object!)),
            Form.Objects => new JsonArray(Object(DefOf(member.Object!))),
            Form.Clauses => new JsonArray(ExampleClause(ClauseKeys.Keys.First())),
            Form.Texts => new JsonArray("AAAA1111"),
            _ => null,
        };
    }

    /// <summary>The smallest clause that loads under <paramref name="key"/>.</summary>
    public JsonObject ExampleClause(string key)
    {
        var def = DefOf(ClauseKeys[key]);
        return new JsonObject { [key] = Example(def.Members[key]) };
    }
}
