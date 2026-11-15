using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Form = Motely.Tests.JamlGrammar.Form;

namespace Motely.Tests;

/// <summary>
/// Holds <c>Motely.Wasm/jaml/</c> to the loader. The three files there are what
/// <see cref="JamlGrammar"/> writes, and the schema it writes gives the loader's own verdict on
/// every rule row, on every member under a spread of values, and on a set of hand-picked documents.
/// </summary>
public class JamlGrammarTests
{
    private static readonly JamlGrammar Grammar = new();
    private static readonly JsonNode Schema = JsonNode.Parse(Grammar.Schema())!;

    private static readonly string JamlDir =
        Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "..", "..", "..", "..", "Motely.Wasm", "jaml"));

    public static TheoryData<string> Files => new() { "motely-filter.schema.json", "index.d.ts", "vocab.json" };

    [Theory]
    [MemberData(nameof(Files))]
    public void The_committed_file_is_what_the_loader_generates(string file)
    {
        var generated = file switch
        {
            "motely-filter.schema.json" => Grammar.Schema(),
            "index.d.ts" => Grammar.TypeScript(),
            _ => Grammar.Vocabulary(),
        };
        var path = Path.Join(JamlDir, file);
        if (Environment.GetEnvironmentVariable("MOTELY_WRITE_JAML") == "1")
            File.WriteAllText(path, generated);

        Assert.True(File.Exists(path), $"{file} is missing. Write it: {JamlGrammar.Regenerate}");
        var committed = File.ReadAllText(path).Replace("\r\n", "\n");
        Assert.True(committed == generated, $"{file} is not what the loader generates: it was edited by hand, or the loader changed. Write it again: {JamlGrammar.Regenerate}");
    }

    [Fact]
    public void Every_clause_key_loads_at_its_smallest_and_the_schema_takes_it()
    {
        var wrong = new List<string>();
        foreach (var key in Grammar.ClauseKeys.Keys)
        {
            var doc = Must(Grammar.ExampleClause(key));
            if (!Loads(doc, out var error))
                wrong.Add($"{Line(doc)} does not load: {error}");
            if (!SchemaTakes(doc))
                wrong.Add($"{Line(doc)} is refused by the schema");
        }
        Assert.True(wrong.Count == 0, Report(wrong));
    }

    /// <summary>
    /// Each row of <see cref="JamlSchemaRules.All"/>, on every clause it covers: a document that breaks
    /// it, which the loader and the schema both refuse, and one that keeps it, which both take.
    /// </summary>
    [Fact]
    public void Every_rule_is_refused_when_broken_and_taken_when_kept_by_loader_and_schema()
    {
        var wrong = new List<string>();
        var tried = new HashSet<JamlRule>();
        foreach (var rule in JamlSchemaRules.All)
        foreach (var site in Sites().Where(site => rule.On.IsAssignableFrom(site.Def.Clr)))
        {
            var member = site.Def.Members[rule.Member];
            tried.Add(rule);

            foreach (var bad in Breaking(rule, site.Def))
            {
                var doc = site.Build(target => bad(target));
                if (Loads(doc, out var refusal))
                    wrong.Add($"[{rule.Kind} {rule.On.Name}.{rule.Member}] the loader takes {Line(doc)}");
                else if (rule.Words is { } words && member.Form is not Form.NoValue && !refusal!.Contains(words))
                    wrong.Add($"[{rule.Kind} {rule.On.Name}.{rule.Member}] the loader no longer says \"{words}\": it says \"{refusal}\"");
                if (rule.Kind is not JamlRuleKind.NotBelow && SchemaTakes(doc))
                    wrong.Add($"[{rule.Kind} {rule.On.Name}.{rule.Member}] the schema takes {Line(doc)}");
            }

            var kept = site.Build(target =>
            {
                target[member.Key] = Grammar.Example(member);
                if (rule.Other is { } other)
                    target[other] = Grammar.Example(site.Def.Members[other]);
            });
            if (!Loads(kept, out var error))
                wrong.Add($"[{rule.Kind} {rule.On.Name}.{rule.Member}] the loader refuses {Line(kept)}: {error}");
            if (!SchemaTakes(kept))
                wrong.Add($"[{rule.Kind} {rule.On.Name}.{rule.Member}] the schema refuses {Line(kept)}");

            if (rule.Kind is JamlRuleKind.NotBelow && member.Form is not Form.NoValue)
                Assert.Equal(rule.Words, member.Note);
        }
        wrong.AddRange(JamlSchemaRules.All.Where(rule => !tried.Contains(rule)).Select(rule => $"[{rule.Kind} {rule.On.Name}.{rule.Member}] covers nothing a document can reach"));
        Assert.True(wrong.Count == 0, Report(wrong));
    }

    /// <summary>
    /// Every member of the root, of every clause and of every nested mapping, written with each of
    /// a spread of values: the schema takes the document exactly when the loader does.
    /// </summary>
    [Fact]
    public void The_schema_takes_a_member_value_exactly_when_the_loader_does()
    {
        JsonNode?[] Probes(JamlGrammar.Member member) =>
        [
            Grammar.Example(member), null, "Any", " ANY ", "~", "null", "NULL", "none", " ", "x", "", 0, 1, -1, MaxAnte, MaxAnte + 1, MaxRollIndex, MaxRollIndex + 1, 1.5, true,
            .. AsText, new JsonArray(true), new JsonArray(1.5),
            new JsonArray(), new JsonArray((JsonNode?)null), new JsonArray(0), new JsonArray(-1), new JsonArray(MaxAnte + 1),
            new JsonArray(MaxRollIndex + 1), new JsonArray("x"), new JsonArray(new JsonObject()), new JsonObject(), new JsonObject { ["x"] = 1 },
        ];

        var wrong = new List<string>();
        var count = 0;
        foreach (var site in Sites(root: true))
        foreach (var member in site.Def.Members.Values)
        foreach (var probe in Probes(member))
        {
            var doc = site.Build(target => target[member.Key] = probe?.DeepClone());
            var loads = Loads(doc, out var error);
            count++;
            if (member.Form is Form.Int or Form.Ints or Form.EventRolls or Form.Bool && AsText.Any(text => JsonNode.DeepEquals(text, probe)))
            {
                // Where the two part ways by design: see AsText. The schema's side of it is checked here.
                if (SchemaTakes(doc))
                    wrong.Add($"the schema takes {Line(doc)}: a number or boolean written as text");
                continue;
            }
            if (loads != SchemaTakes(doc))
                wrong.Add(loads ? $"the loader takes {Line(doc)}, the schema refuses it" : $"the schema takes {Line(doc)}, the loader refuses it: {error}");
        }
        Assert.True(count > 5000, $"only {count} documents were tried");
        Assert.True(wrong.Count == 0, Report(wrong));
    }

    /// <summary>Documents picked by hand for the corners: two members at once, empty and null forms, nesting.</summary>
    [Theory]
    [InlineData("must:\n  - soulOrder:\n      - antes: [1]")]
    [InlineData("must:\n  - soulOrder:\n      - legendary: Blueprint")]
    [InlineData("must:\n  - soulOrder:\n      - legendary: Perkeo")]
    [InlineData("must:\n  - soulOrder: []")]
    [InlineData("must:\n  - soulOrders: []")]
    [InlineData("must:\n  - soulOrder: ~")]
    [InlineData("must:\n  - soulOrder:\n      - legendary: Perkeo\n    entries: []")]
    [InlineData("must:\n  - luckyMoney: Any")]
    [InlineData("must:\n  - luckyMoney: ~")]
    [InlineData("must:\n  - luckyMoney: []")]
    [InlineData("must:\n  - luckyMoney: Any\n    rolls: [1]")]
    [InlineData("must:\n  - luckyMoney: [1000]")]
    [InlineData("must:\n  - luckyMoney: ~\n    rolls: [1]")]
    [InlineData("must:\n  - luckyMoney: ''\n    rolls: [1]")]
    [InlineData("must:\n  - luckyMoney: 3\n    rolls: [1]")]
    [InlineData("must:\n  - or: ~\n    clauses:\n      - joker: Blueprint")]
    [InlineData("must:\n  - or: ~")]
    [InlineData("must:\n  - joker: t-h-e blueprint")]
    [InlineData("must:\n  - joker: '  ANY '")]
    [InlineData("must:\n  - tarotCard: fool")]
    [InlineData("must:\n  - tarotCard: the the fool")]
    [InlineData("must:\n  - erraticRank: k")]
    [InlineData("must:\n  - erraticRank: z")]
    [InlineData("must:\n  - pokerHand: 4 of a kind")]
    [InlineData("must:\n  - joker: [Blueprint, Any]")]
    [InlineData("must:\n  - joker: Blueprint\n    edition: [Negative, Foil]")]
    [InlineData("must:\n  - standardCard:\n      rank: Ace\n      seal: Red")]
    [InlineData("must:\n  - standardCard: AceOfSpades\n    sources:\n      shopItems: 3")]
    [InlineData("must:\n  - soulOrder:\n      - legendary: Perkeo\n    entries:\n      - {}")]
    [InlineData("must:\n  - soulOrder:\n      - legendary: Perkeo\n    entries:\n      - legendary: Chicot")]
    [InlineData("must:\n  - and:\n      - or:\n          - joker: Blueprint\n          - voucher: Hone\n            rolls: 1000")]
    [InlineData("must:\n  - joker: Blueprint\n    ante: ~")]
    [InlineData("must:\n  - joker: Blueprint\n    antes: ~")]
    [InlineData("must:\n  - joker: Blueprint\n    antes: []")]
    [InlineData("must:\n  - joker: Blueprint\n    min: ~")]
    [InlineData("must:\n  - joker: Blueprint\n    score: -3")]
    [InlineData("must:\n  - joker: Blueprint\n    max: ~")]
    [InlineData("must:\n  - joker: Blueprint\n    label: [1]")]
    [InlineData("must:\n  - joker: Blueprint\n    stickers: []")]
    [InlineData("must:\n  - joker: ~")]
    [InlineData("must:\n  - joker: []")]
    [InlineData("must:\n  - joker: Blueprint\n    sources: ~")]
    [InlineData("must:\n  - joker: Blueprint\n    sources: {}")]
    [InlineData("must:\n  - joker: Blueprint\n    sources:\n      shopItems: [-1]")]
    [InlineData("must:\n  - joker: Blueprint\n    sources:\n      shopItems: [1000]")]
    [InlineData("must:\n  - joker: Blueprint\n    sources:\n      shopItems: []")]
    [InlineData("must:\n  - joker: Blueprint\n    sources:\n      requireMegaPack: ~")]
    [InlineData("must:\n  - joker: Blueprint\n    sources:\n      requireMega: ~")]
    [InlineData("must:\n  - joker: Blueprint\n    sources:\n      hasSpawnSources: true")]
    [InlineData("must:\n  - joker: Blueprint\n    with: ~")]
    [InlineData("must:\n  - joker: Blueprint\n    with: {}")]
    [InlineData("must:\n  - joker: Blueprint\n    with:\n      luck: ~")]
    [InlineData("must:\n  - joker: Blueprint\n    with:\n      jokers: ~")]
    [InlineData("must:\n  - erraticRank: ~")]
    [InlineData("must:\n  - erraticRank: Ace\n    rank: ~")]
    [InlineData("must:\n  - standardCard: ~")]
    [InlineData("must:\n  - standardCard: {}")]
    [InlineData("must:\n  - standardCard:\n      card: AceOfSpades")]
    [InlineData("must:\n  - and: ~\n    clauses:\n      - joker: Blueprint")]
    [InlineData("must:\n  - and:\n      - joker: Blueprint\n    clauses: ~")]
    [InlineData("must:\n  - and:\n      - joker: Blueprint\n    max: 1")]
    [InlineData("must:\n  - or:\n      - joker: Blueprint\n    min: 0")]
    [InlineData("must:\n  - or:\n      - joker: Blueprint\n    mode: ~")]
    [InlineData("must:\n  - legendaryJoker: Perkeo\n    jokers: Blueprint")]
    [InlineData("must:\n  - legendaryJoker: Blueprint")]
    [InlineData("must:\n  - legendaryJoker: Perkeo\n    rolls: []")]
    [InlineData("must:\n  - legendaryJoker: Perkeo\n    soulCardOnly: ~")]
    [InlineData("must:\n  - legendarySoulOrder: x")]
    [InlineData("must:\n  - soulOrder:\n      - legendary: Perkeo\n        antes: [40]")]
    [InlineData("must:\n  - misprintMult: 1\n    mult: -1")]
    [InlineData("must:\n  - voucher: Hone\n    rolls: [1000]")]
    [InlineData("must:\n  - voucher: Hone\n    rolls: []")]
    [InlineData("must:\n  - boss: TheHook\n    ante: 0")]
    [InlineData("must:\n  - boss: TheHook\n    antes: []")]
    [InlineData("must:\n  - boss: TheHook\n    ante: []")]
    [InlineData("must:\n  - boss: TheHook\n    ante: []\n    antes: [2]")]
    [InlineData("must:\n  - boss: TheHook\n    ante: ~")]
    [InlineData("must:\n  - luckyMoney: []\n    rolls: [1]")]
    [InlineData("must:\n  - and: []\n    clauses:\n      - joker: Blueprint")]
    [InlineData("must:\n  - and: []")]
    [InlineData("must:\n  - joker: the  BLUE-print")]
    [InlineData("must:\n  - joker: 8 ball")]
    [InlineData("must:\n  - joker: notajoker")]
    [InlineData("must:\n  - joker: Blueprint\n    with:\n      luck: 4")]
    [InlineData("must:\n  - joker: Blueprint\n    with:\n      luck: 3")]
    [InlineData("must:\n  - joker: Blueprint\n    nope: 1")]
    [InlineData("id: ~")]
    [InlineData("name: [1]")]
    [InlineData("seeds: ~")]
    [InlineData("seeds: [~]")]
    [InlineData("must: ~")]
    [InlineData("deck: ~")]
    [InlineData("deck: ghost deck")]
    [InlineData("stake: ~")]
    [InlineData("cutoff: ~")]
    [InlineData("cutoff: -5")]
    [InlineData("nope: 1")]
    public void The_schema_gives_the_loaders_verdict(string yaml)
    {
        var loads = JamlConfig.TryLoad(yaml, out _, out var error);
        var taken = MiniSchema.Takes(Schema, Schema, JsonNode.Parse(JamlConfig.ToJson(yaml)));
        Assert.True(loads == taken, loads ? "the loader takes it, the schema refuses it" : $"the schema takes it, the loader refuses it: {error}");
    }

    /// <summary>
    /// Every name of every enum, every digit/word alias and every rank letter, spelled a dozen ways
    /// (case, "the" in front once to three times, a dash between every character, a stray letter):
    /// the enum's schema takes the text exactly when the loader's name reader does.
    /// </summary>
    [Fact]
    public void The_schema_takes_a_spelling_exactly_when_the_loader_does()
    {
        var wrong = new List<string>();
        var count = 0;
        foreach (var enumType in Grammar.Enums)
        {
            var schema = Schema["definitions"]!["enums"]![enumType.Name]!;
            var bases = Enum.GetNames(enumType).Concat(JamlEnumName.DigitWordAliases.Keys).Concat(["J", "Q", "K", "A", "Z"])
                .Concat(["x", "the", "1", "3", "4", "04", "+4", "-1", "64", ""]);
            foreach (var word in bases)
            foreach (var text in new[]
            {
                word, word.ToLowerInvariant(), word.ToUpperInvariant(), $" {word} ", "the " + word, "The The " + word, "the the the " + word,
                string.Join("-", word.ToCharArray()), word + "x", "x" + word, word + ",", "_" + word + "!",
                word.StartsWith("The", StringComparison.Ordinal) ? word[3..] : word + " " + word,
            })
            {
                bool loads;
                try { JamlEnumName.Parse(enumType, text); loads = true; }
                catch (FormatException) { loads = false; }
                count++;
                if (loads != MiniSchema.Takes(Schema, schema, JsonNode.Parse(JsonSerializer.Serialize(text))))
                    wrong.Add($"{enumType.Name} '{text}': the loader {(loads ? "takes" : "refuses")} it, the schema does not agree");
            }
        }
        Assert.True(count > 10000, $"only {count} spellings were tried");
        Assert.True(wrong.Count == 0, Report(wrong));
    }

    /// <summary>The filters under Motely.Tests/JamlFilters: the schema takes each one exactly when the loader does.</summary>
    [Fact]
    public void The_schema_gives_the_loaders_verdict_on_every_corpus_filter()
    {
        var dir = Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "..", "..", "..", "JamlFilters"));
        var files = Directory.GetFiles(dir, "*.yaml", SearchOption.AllDirectories);
        Assert.True(files.Length > 100, $"only {files.Length} filters under {dir}");
        var wrong = new List<string>();
        foreach (var file in files)
        {
            var yaml = File.ReadAllText(file);
            var loads = JamlConfig.TryLoad(yaml, out _, out var error);
            if (error is not null && TwoMemberRules.Any(error.Contains))
                continue;
            if (loads != MiniSchema.Takes(Schema, Schema, JsonNode.Parse(JamlConfig.ToJson(yaml))))
                wrong.Add(loads ? $"{Path.GetFileName(file)}: the loader takes it, the schema refuses it" : $"{Path.GetFileName(file)}: the schema takes it, the loader refuses it: {error}");
        }
        Assert.True(wrong.Count == 0, Report(wrong));
    }

    [Fact]
    public void The_vocabulary_lists_what_the_loader_binds()
    {
        var vocab = JsonNode.Parse(Grammar.Vocabulary())!;
        Assert.Equal(JamlClauseBinding.ClauseTypeByKey.Keys.Order(), vocab["clauses"]!.AsObject().Select(pair => pair.Key).Order());
        foreach (var (key, type) in JamlClauseBinding.ClauseTypeByKey)
        {
            var members = vocab["types"]![vocab["clauses"]![key]!.GetValue<string>()]!.AsObject();
            Assert.Equal(JamlGrammar.MembersOf(type).Keys.Order(StringComparer.Ordinal), members.Select(pair => pair.Key).Order(StringComparer.Ordinal));
            Assert.Contains(key, members.Select(pair => pair.Key));
        }
        Assert.Equal(JamlGrammar.MembersOf(typeof(JamlConfig)).Keys.Order(StringComparer.Ordinal), vocab["root"]!.AsObject().Select(pair => pair.Key).Order(StringComparer.Ordinal));
        foreach (var (name, names) in vocab["enums"]!.AsObject())
            Assert.Equal(Enum.GetNames(Grammar.Enums.Single(e => e.Name == name)), names!.AsArray().Select(n => n!.GetValue<string>()));
    }

    /// <summary>
    /// A number or a boolean written as text. The loader reads YAML, where every scalar is text, so
    /// it takes these wherever a number or boolean goes ("010" is 8, "yes" is true). The schema
    /// describes the JSON form, where a number is a number and a boolean a boolean, so there it
    /// refuses them; under every other kind of member the two must still agree on them.
    /// </summary>
    private static readonly JsonNode[] AsText =
    [
        "1", "010", "+5", "-1", "1.5", " 2 ", "0x1F", "1000", "true", "True", "false", "yes", "on",
        new JsonArray("1"), new JsonArray("010"), new JsonArray("+5"), new JsonArray("1000"),
    ];

    private static readonly string[] TwoMemberRules =
        [.. JamlSchemaRules.All.Where(rule => rule.Kind is JamlRuleKind.NotBelow).Select(rule => rule.Words!)];

    // ── documents ───────────────────────────────────────────────────────────────

    /// <summary>A place a mapping of <see cref="Def"/> sits in a document, and how to build that document around it.</summary>
    private sealed record Site(JamlGrammar.TypeDef Def, Func<Action<JsonObject>, JsonObject> Build);

    /// <summary>Every clause under each of its keys, every nested mapping under its first host, and the root when asked.</summary>
    private static IEnumerable<Site> Sites(bool root = false)
    {
        if (root)
            yield return new Site(Grammar.Root, change => { var doc = new JsonObject(); change(doc); return doc; });
        var hosted = new HashSet<Type>();
        foreach (var (key, type) in Grammar.ClauseKeys)
        {
            var def = Grammar.DefOf(type);
            yield return new Site(def, change => { var clause = Grammar.ExampleClause(key); change(clause); return Must(clause); });
            // The other way to write such a clause: its key left empty, the member it needs written out.
            if (def.NeedsOneOf is var (needed, feeders, _) && feeders.Contains(key))
                yield return new Site(def, change =>
                {
                    var clause = new JsonObject { [key] = null, [needed] = Grammar.Example(def.Members[needed]) };
                    change(clause);
                    return Must(clause);
                });
            foreach (var host in def.Members.Values.Where(m => m.Object is not null && hosted.Add(m.Object)))
                yield return new Site(Grammar.DefOf(host.Object!), change =>
                {
                    var clause = Grammar.ExampleClause(key);
                    var nested = Grammar.Example(new JamlGrammar.Member(host.Key, host.Form is Form.Objects ? Form.Object : host.Form is Form.CardSlot ? Form.Object : host.Form) { Object = host.Object })!.AsObject();
                    change(nested);
                    clause[host.Key] = host.Form is Form.Objects ? new JsonArray(nested) : nested;
                    return Must(clause);
                });
        }
    }

    private static IEnumerable<Action<JsonObject>> Breaking(JamlRule rule, JamlGrammar.TypeDef def)
    {
        var key = def.Members[rule.Member].Key;
        switch (rule.Kind)
        {
            case JamlRuleKind.Range:
                if (rule.Min is { } min) yield return target => target[key] = min - 1;
                if (rule.Max is { } max) yield return target => target[key] = max + 1;
                break;
            case JamlRuleKind.ItemRange:
                if (rule.Min is { } least) yield return target => target[key] = new JsonArray(least - 1);
                if (rule.Max is { } most) yield return target => target[key] = new JsonArray(most + 1);
                break;
            case JamlRuleKind.MinItems:
                yield return target => target[key] = new JsonArray();
                break;
            case JamlRuleKind.NotNull:
                yield return target => target[key] = null;
                break;
            case JamlRuleKind.NoValue:
                yield return target => target[key] = 1;
                break;
            case JamlRuleKind.NotBelow:
                yield return target => (target[key], target[rule.Other!]) = (1, 2);
                break;
            case JamlRuleKind.Names:
                var bound = JamlGrammar.MembersOf(def.Clr)[rule.Member].Type;
                var all = Nullable.GetUnderlyingType(bound) ?? bound.GetElementType() ?? bound;
                var outsider = Enum.GetNames(all).First(name => !Enum.IsDefined(rule.Names!, name));
                yield return target => target[key] = outsider;
                break;
            case JamlRuleKind.Required:
                yield return target => target.Remove(key);
                break;
        }
    }

    private static JsonObject Must(JsonObject clause) => new() { ["must"] = new JsonArray(clause) };

    private static bool Loads(JsonObject doc, out string? error) => JamlConfig.TryLoad(doc.ToJsonString(), out _, out error);

    // Read back from text, so a number is a JSON number whatever C# type it was built from.
    private static bool SchemaTakes(JsonObject doc) => MiniSchema.Takes(Schema, Schema, JsonNode.Parse(doc.ToJsonString()));

    private static string Line(JsonNode doc) => doc.ToJsonString();

    private static string Report(List<string> wrong) =>
        wrong.Count == 0 ? "" : $"{wrong.Count} wrong:\n" + string.Join("\n", wrong.Take(80));

    /// <summary>
    /// JSON Schema, for the keywords <see cref="JamlGrammar.Schema"/> writes and no others: a
    /// keyword it does not know throws, so the schema cannot grow past what the tests check.
    /// </summary>
    private static class MiniSchema
    {
        public static bool Takes(JsonNode root, JsonNode schema, JsonNode? value)
        {
            foreach (var (keyword, arg) in schema.AsObject())
            {
                var holds = keyword switch
                {
                    "$schema" or "title" or "description" or "definitions" or "properties" or "items" => true,
                    "$ref" => Takes(root, Resolve(root, arg!.GetValue<string>()), value),
                    "anyOf" => arg!.AsArray().Any(option => Takes(root, option!, value)),
                    "allOf" => arg!.AsArray().All(option => Takes(root, option!, value)),
                    "type" => (arg is JsonArray types ? types.Select(t => t!.GetValue<string>()) : [arg!.GetValue<string>()]).Any(type => Is(type, value)),
                    "enum" => arg!.AsArray().Any(option => JsonNode.DeepEquals(option, value)),
                    "const" => JsonNode.DeepEquals(arg, value),
                    "pattern" => value?.GetValueKind() is not JsonValueKind.String || Regex.IsMatch(value.GetValue<string>(), arg!.GetValue<string>(), RegexOptions.ECMAScript),
                    "minimum" => value?.GetValueKind() is not JsonValueKind.Number || value.GetValue<double>() >= arg!.GetValue<double>(),
                    "maximum" => value?.GetValueKind() is not JsonValueKind.Number || value.GetValue<double>() <= arg!.GetValue<double>(),
                    "minItems" => value is not JsonArray list || list.Count >= arg!.GetValue<int>(),
                    "required" => value is not JsonObject map || arg!.AsArray().All(name => map.ContainsKey(name!.GetValue<string>())),
                    "additionalProperties" => arg!.GetValue<bool>() || value is not JsonObject extra
                        || extra.All(pair => schema["properties"]?.AsObject().ContainsKey(pair.Key) == true),
                    _ => throw new InvalidOperationException($"MiniSchema does not know the keyword '{keyword}'."),
                };
                if (!holds)
                    return false;
            }
            if (value is JsonObject fields && schema["properties"] is JsonObject properties)
                foreach (var (name, field) in fields)
                    if (properties[name] is { } property && !Takes(root, property, field))
                        return false;
            if (value is JsonArray items && schema["items"] is { } each)
                foreach (var item in items)
                    if (!Takes(root, each, item))
                        return false;
            return true;
        }

        private static JsonNode Resolve(JsonNode root, string reference) =>
            reference.TrimStart('#', '/').Split('/').Aggregate(root, (node, part) => node[part] ?? throw new InvalidOperationException($"no {reference}"));

        private static bool Is(string type, JsonNode? value) =>
            type switch
            {
                "null" => value is null,
                "string" => value?.GetValueKind() is JsonValueKind.String,
                "boolean" => value?.GetValueKind() is JsonValueKind.True or JsonValueKind.False,
                "number" => value?.GetValueKind() is JsonValueKind.Number,
                "integer" => value?.GetValueKind() is JsonValueKind.Number && value.GetValue<double>() % 1 == 0,
                "object" => value is JsonObject,
                "array" => value is JsonArray,
                _ => throw new InvalidOperationException($"MiniSchema does not know the type '{type}'."),
            };
    }
}
