using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Motely.Filters;

namespace Motely.Tests;

/// <summary>
/// <c>Motely.Wasm/jaml</c>'s three files are generated from the filter loader
/// (<see cref="JamlSchemaGenerator"/>). These tests hold the committed files to the generator,
/// and the generator to the loader: what the schema allows loads, what the loader binds is in the
/// schema, and every row of the loader's rule table is refused and accepted by both alike.
/// </summary>
public sealed class JamlSchemaTests
{
    private static readonly IReadOnlyDictionary<string, string> Files = JamlSchemaGenerator.Generate();
    private static readonly JamlSchemaModel Model = JamlSchemaModelBuilder.Build();
    private static readonly JsonElement Schema = JsonDocument.Parse(Files[JamlSchemaGenerator.SchemaFile]).RootElement;
    private static readonly JsonSchemaCheck Check = new(Schema);
    private static readonly JsonSerializerOptions Plain = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static string RepositoryRoot
    {
        get
        {
            var folder = new DirectoryInfo(AppContext.BaseDirectory);
            while (folder is not null && !File.Exists(Path.Join(folder.FullName, "Motely.slnx")))
                folder = folder.Parent;
            return folder?.FullName ?? throw new InvalidOperationException("Motely.slnx is not above the test output.");
        }
    }

    // ── a. drift ──

    [Theory]
    [InlineData(JamlSchemaGenerator.TypesFile)]
    [InlineData(JamlSchemaGenerator.SchemaFile)]
    [InlineData(JamlSchemaGenerator.VocabFile)]
    public void CommittedFile_IsWhatTheGeneratorWrites(string name)
    {
        var path = Path.Join(RepositoryRoot, JamlSchemaGenerator.Directory, name);
        var generated = Files[name];
        Assert.DoesNotContain('\r', generated);
        Assert.EndsWith("\n", generated);
        Assert.True(
            File.Exists(path) && File.ReadAllText(path) == generated,
            $"{JamlSchemaGenerator.Directory}/{name} is not what the loader generates. It is never edited by hand: "
                + $"run `{JamlSchemaGenerator.Command}` from the repository root and commit the result.");
    }

    [Fact]
    public void EveryGeneratedFile_SaysHowToRegenerate()
    {
        Assert.Equal(3, Files.Count);
        foreach (var (name, text) in Files)
        {
            Assert.True(text.Contains(JamlSchemaGenerator.Command), $"{name} does not name the command that writes it.");
            Assert.DoesNotContain("by hand since", text);
        }
    }

    [Fact]
    public void Package_ExportsTheVocabularyBesideTheSchema()
    {
        using var package = JsonDocument.Parse(File.ReadAllText(Path.Join(RepositoryRoot, "Motely.Wasm", "package.json")));
        var exports = package.RootElement.GetProperty("exports");
        Assert.Equal("./jaml/" + JamlSchemaGenerator.SchemaFile, exports.GetProperty("./jaml/schema.json").GetString());
        Assert.Equal("./jaml/" + JamlSchemaGenerator.VocabFile, exports.GetProperty("./jaml/vocab.json").GetString());
    }

    // ── b. truth, forward: what the schema allows, the loader accepts ──

    [Fact]
    public void EveryClauseKeyAndMemberInTheSchema_Loads()
    {
        var failures = new List<string>();
        int tried = 0;
        foreach (var (key, clause) in Model.ClauseKeys)
        {
            var minimal = Filter(MinimalClause(key));
            if (!Loads(minimal, out var error) || !Check.IsValid(Text(minimal)))
                failures.Add($"{Text(minimal)}: {error ?? "the schema refuses its own minimal clause"}");

            foreach (var member in Definition(clause).GetProperty("properties").EnumerateObject())
            {
                var samples = Samples(member.Value).ToList();
                Assert.True(samples.Count > 0, $"{clause.SchemaName}.{member.Name}: the schema gives it no value to write.");
                foreach (var sample in samples)
                {
                    var written = MinimalClause(key);
                    Write(written, member.Name, sample);
                    FillAListTheClauseNeeds(Definition(clause), written);
                    var filter = Filter(written);
                    tried++;
                    if (!Check.IsValid(Text(filter)))
                        failures.Add($"{Text(filter)}: the schema refuses a value it lists");
                    else if (!Loads(filter, out error))
                        failures.Add($"{Text(filter)}: {error}");
                }
            }
        }
        Assert.True(failures.Count == 0, $"the schema allows what the loader refuses:\n{string.Join("\n", failures)}");
        Assert.True(tried > Model.Clauses.Sum(clause => clause.Members.Count));
    }

    [Fact]
    public void EveryFilterMemberInTheSchema_Loads()
    {
        var failures = new List<string>();
        foreach (var member in Schema.GetProperty("properties").EnumerateObject())
        {
            var samples = Samples(member.Value).ToList();
            Assert.NotEmpty(samples);
            foreach (var sample in samples)
            {
                var filter = new JsonObject { [member.Name] = sample?.DeepClone() };
                if (!Check.IsValid(Text(filter)))
                    failures.Add($"{Text(filter)}: the schema refuses a value it lists");
                else if (!Loads(filter, out var error))
                    failures.Add($"{Text(filter)}: {error}");
            }
        }
        Assert.True(failures.Count == 0, $"the schema allows what the loader refuses:\n{string.Join("\n", failures)}");
    }

    [Fact]
    public void EveryLooseSpellingTheSchemaReads_IsANameToTheLoader()
    {
        foreach (var enumType in Model.Enums)
        {
            var pattern = Schema.GetProperty("definitions").GetProperty("enums").GetProperty(enumType.Name)
                .GetProperty("anyOf")[1];
            var spellings = JamlSchemaGenerator.LooseSpellings(enumType);
            Assert.NotEmpty(spellings);
            foreach (var spelling in spellings)
            {
                // "blueprint" as " B-l U_e ..." : any case, anything but letters, digits and commas between.
                var loose = " " + string.Join("", spelling.Select((c, i) =>
                    (i % 2 == 0 ? char.ToUpperInvariant(c) : c) + (i % 3 == 0 ? "-" : i % 3 == 1 ? " " : "_"))) + "!";
                foreach (var written in (string[])[spelling, loose])
                {
                    Assert.True(Check.IsValid(pattern, JsonSerializer.SerializeToElement(written)), $"{enumType.Name}: the schema refuses '{written}'");
                    Assert.True(Enum.IsDefined(enumType, JamlEnumName.Parse(enumType, written)), $"{enumType.Name}: '{written}'");
                }
            }

            foreach (var name in JamlEnumName.ShortNames(enumType))
            {
                Assert.True(Check.IsValid(pattern, JsonSerializer.SerializeToElement($" {name.ToLowerInvariant()} ")));
                Assert.True(Enum.IsDefined(enumType, JamlEnumName.Parse(enumType, $" {name.ToLowerInvariant()} ")));
            }

            var first = Enum.GetNames(enumType)[0];
            foreach (var wrong in (string[])[first + "x", $"{first}, {first}", "", "-", "7"])
            {
                Assert.False(Check.IsValid(pattern, JsonSerializer.SerializeToElement(wrong)), $"{enumType.Name}: the schema reads '{wrong}'");
                Assert.ThrowsAny<FormatException>(() => JamlEnumName.Parse(enumType, wrong));
            }
        }
    }

    [Fact]
    public void EveryAnyWordTheSchemaReads_IsAnyToTheLoader()
    {
        var pattern = JsonSerializer.SerializeToElement(new { type = "string", pattern = JamlSchemaGenerator.AnyPattern() });
        foreach (var word in JamlClauseBinding.AnyWords)
        {
            foreach (var written in (string[])[word, word.ToUpperInvariant()])
            {
                Assert.True(Check.IsValid(pattern, JsonSerializer.SerializeToElement(written)));
                Assert.True(JamlClauseBinding.IsAnyWord(written));
            }
        }
        Assert.False(Check.IsValid(pattern, JsonSerializer.SerializeToElement("anything")));
        Assert.False(JamlClauseBinding.IsAnyWord("anything"));
    }

    [Fact]
    public void EveryTestFilterTheLoaderAccepts_IsValidAgainstTheSchema()
    {
        // The schema may be narrower than the loader in corners (a key written twice over, an
        // and: that writes min: 1), but not for a filter anyone has written here.
        var folder = Path.Join(RepositoryRoot, "Motely.Tests", "JamlFilters");
        var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
            .Where(file => Path.GetExtension(file) is ".yaml" or ".yml" or ".json")
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.NotEmpty(files);
        var refused = files
            .Where(file => JamlConfig.TryLoad(File.ReadAllText(file), out _, out _))
            .Where(file => !Check.IsValid(JamlConfig.ToJson(File.ReadAllText(file))))
            .Select(file => Path.GetRelativePath(folder, file))
            .ToList();
        Assert.True(refused.Count == 0, $"the loader accepts these and the schema refuses them:\n{string.Join("\n", refused)}");
    }

    // ── c. truth, reverse: what the loader binds is in the schema ──

    [Fact]
    public void TheStaticContextAlone_ReportsPropertyNames_NotTheNamesAFilterWrites()
    {
        // Why JamlConfigLoader.BoundMembers reads the context through the naming convention and
        // the YamlMember aliases: on its own it lists the C# properties.
        var raw = new JamlYamlContext().GetTypeInspector().GetProperties(typeof(JamlConfig), null).Select(member => member.Name).ToList();
        Assert.Contains("MustTagged", raw);
        Assert.DoesNotContain("must", raw);

        var bound = JamlConfigLoader.BoundMembers(typeof(JamlConfig)).Select(member => member.Name).ToList();
        Assert.Contains("must", bound);
        Assert.Contains("dateCreated", bound);
        Assert.DoesNotContain("MustTagged", bound);
    }

    [Fact]
    public void EveryMemberTheLoaderBinds_IsInTheSchemaTheTypesAndTheVocabulary()
    {
        using var vocab = JsonDocument.Parse(Files[JamlSchemaGenerator.VocabFile]);
        var types = Files[JamlSchemaGenerator.TypesFile];
        var checkedTypes = new List<Type>();

        foreach (var type in Model.Types)
        {
            // The card converter binds a mapping through its own type; every other type binds as itself.
            var boundAs = type.Type == typeof(JamlStandardCardSlot) ? typeof(JamlStandardCardSlotMapping) : type.Type;
            var forbidden = JamlConfigLoader.ClauseRules
                .Where(rule => rule.Kind == JamlRuleKind.Forbidden && rule.AppliesTo(type.Type))
                .Select(rule => rule.Member)
                .ToHashSet();
            var bound = JamlConfigLoader.BoundMembers(boundAs).Select(member => member.Name).ToList();
            var expected = bound.Where(name => !forbidden.Contains(name)).Order(StringComparer.Ordinal).ToList();
            Assert.All(forbidden, name => Assert.Contains(name, bound));

            var inSchema = Definition(type).GetProperty("properties").EnumerateObject().Select(member => member.Name);
            Assert.Equal(expected, inSchema.Order(StringComparer.Ordinal));

            var listed = ReferenceEquals(type, Model.Filter)
                ? vocab.RootElement.GetProperty("filter")
                : vocab.RootElement.GetProperty(type.Keys.Count > 0 ? "clauses" : "blocks").GetProperty(type.SchemaName);
            Assert.Equal(expected, listed.GetProperty("members").EnumerateObject().Select(member => member.Name).Order(StringComparer.Ordinal));

            var name = ReferenceEquals(type, Model.Filter) ? "JamlFilter" : type.TypeName;
            int start = types.IndexOf($"export interface {name} {{\n", StringComparison.Ordinal);
            Assert.True(start >= 0, $"index.d.ts has no interface {name}.");
            var body = types[start..types.IndexOf("\n}\n", start, StringComparison.Ordinal)];
            var declared = body.Split('\n').Skip(1).Where(line => !line.StartsWith("  /**", StringComparison.Ordinal))
                .Select(line => line.Trim().Split(':')[0].TrimEnd('?'));
            Assert.Equal(expected, declared.Order(StringComparer.Ordinal));
            checkedTypes.Add(type.Type);
        }

        Assert.Contains(typeof(JamlConfig), checkedTypes);
        Assert.All(JamlConfigLoader.ClauseTypes.Values, type => Assert.Contains(type, checkedTypes));
    }

    [Fact]
    public void EveryClauseKeyTheLoaderBinds_IsInTheSchemaTheTypesAndTheVocabulary()
    {
        var keys = JamlConfigLoader.ClauseTypes.Keys.ToList();
        var inSchema = Schema.GetProperty("definitions").GetProperty("clause").GetProperty("anyOf").EnumerateArray()
            .Select(option => option.GetProperty("allOf")[1].GetProperty("required")[0].GetString()!);
        Assert.Equal(keys, inSchema);

        using var vocab = JsonDocument.Parse(Files[JamlSchemaGenerator.VocabFile]);
        Assert.Equal(keys, vocab.RootElement.GetProperty("clauseKeys").EnumerateObject().Select(member => member.Name));
        var union = Files[JamlSchemaGenerator.TypesFile].Split('\n').Single(line => line.StartsWith("export type JamlClause = ", StringComparison.Ordinal));
        Assert.All(keys, key => Assert.True(
            union.Contains($", \"{key}\">>") || union.Contains($"& {{ {key}: "),
            $"JamlClause has no form that requires {key}."));

        foreach (var enumType in Model.Enums)
        {
            Assert.Equal(
                Enum.GetNames(enumType),
                vocab.RootElement.GetProperty("enums").GetProperty(enumType.Name).EnumerateArray().Select(name => name.GetString()));
        }
    }

    [Fact]
    public void AMemberTheLoaderDoesNotBind_IsRefusedByBoth()
    {
        foreach (var (key, clause) in Model.ClauseKeys)
        {
            var written = MinimalClause(key);
            written["notAMember"] = 1;
            Assert.False(Loads(Filter(written), out _), Text(written));
            Assert.False(Check.IsValid(Text(Filter(written))), Text(written));
        }

        // A property the engine computes is no member: the static context lists it, the loader refuses it.
        var computed = JsonNode.Parse("""{"must":[{"joker":"Blueprint","sources":{"hasSpawnSources":true}}]}""")!;
        Assert.False(Loads(computed, out _));
        Assert.False(Check.IsValid(Text(computed)));

        // A card written as a mapping has no whole-card member: that is the scalar form.
        var card = JsonNode.Parse("""{"must":[{"standardCard":{"card":"AceOfSpades"}}]}""")!;
        Assert.False(Loads(card, out _));
        Assert.False(Check.IsValid(Text(card)));
    }

    // ── d. rules: every row is refused and accepted by the loader and the schema alike ──

    [Fact]
    public void EveryClauseRule_IsRefusedAndAcceptedByTheLoaderAndTheSchemaAlike()
    {
        foreach (var rule in JamlConfigLoader.ClauseRules)
        {
            int shown = 0;
            foreach (var clause in Model.Clauses.Where(clause => rule.AppliesTo(clause.Type)))
            {
                var key = clause.Keys[0];
                var properties = Definition(clause).GetProperty("properties");
                bool inSchema = properties.TryGetProperty(rule.Member, out var member);
                if (!inSchema)
                {
                    // Only a forbidden member is left out of a clause it could be written on.
                    Assert.Contains(JamlConfigLoader.ClauseRules, other =>
                        other.Kind == JamlRuleKind.Forbidden && other.Member == rule.Member && other.AppliesTo(clause.Type));
                }

                var breaking = new List<JsonObject>();
                var satisfying = new List<JsonObject>();
                string outside = "";
                JsonObject With(params (string Name, JsonNode? Value)[] members)
                {
                    var written = MinimalClause(key);
                    foreach (var (name, value) in members)
                        written[name] = value?.DeepClone();
                    return written;
                }
                JsonNode Number(int number) => HasListForm(member) ? new JsonArray(number) : JsonValue.Create(number);

                switch (rule.Kind)
                {
                    case JamlRuleKind.Forbidden:
                        Assert.False(inSchema, $"{clause.SchemaName}.{rule.Member} is forbidden, yet the schema has it.");
                        breaking.Add(With((rule.Member, rule.Default is int kept ? kept + 1 : 1)));
                        satisfying.Add(With());
                        break;

                    case JamlRuleKind.NotBelowMin when inSchema && properties.TryGetProperty("min", out _):
                        // The one rule the schema cannot check: it says so where the member is described.
                        Assert.Contains(rule.Message, member.GetProperty("description").GetString());
                        Assert.False(Loads(Filter(With(("min", 2), ("max", 1))), out var reason));
                        Assert.Contains(rule.Message, reason);
                        shown++;
                        satisfying.Add(With(("min", 2), ("max", 2)));
                        break;

                    case JamlRuleKind.Range when inSchema:
                        if (rule.Min is { } low)
                            breaking.Add(With((rule.Member, Number(low - 1))));
                        if (rule.Max is { } high)
                            breaking.Add(With((rule.Member, Number(high + 1))));
                        int inside = new[] { rule.Min, rule.Min + 1, rule.Max, rule.Max - 1 }
                            .OfType<int>()
                            .First(number => !BreaksARuleOn(clause.Type, rule.Member, number));
                        satisfying.Add(With((rule.Member, Number(inside))));
                        break;

                    case JamlRuleKind.Excluded when inSchema:
                        breaking.Add(With((rule.Member, Number(rule.Value))));
                        satisfying.Add(With((rule.Member, Number(rule.Value + 1))));
                        break;

                    case JamlRuleKind.NotNull when inSchema:
                        breaking.Add(With((rule.Member, null)));
                        satisfying.Add(With((rule.Member, Samples(member).First(sample => sample is not null))));
                        break;

                    case JamlRuleKind.Names when inSchema:
                        var allowed = Enum.GetNames(rule.NamesOf!);
                        var declared = JamlConfigLoader.BoundMember(clause.Type, rule.Member).Type.GetElementType()!;
                        outside = Enum.GetNames(declared).First(name => !allowed.Contains(name));
                        breaking.Add(With((rule.Member, outside)));
                        breaking.Add(With((rule.Member, new JsonArray(allowed[0], outside))));
                        satisfying.Add(With((rule.Member, new JsonArray([.. allowed.Select(name => (JsonNode)name)]))));
                        break;

                    case JamlRuleKind.MinItems when inSchema:
                        var item = Samples(ListForm(member).GetProperty("items")).First();
                        breaking.Add(With((rule.Member, ListOf(item, rule.Min!.Value - 1))));
                        satisfying.Add(With((rule.Member, ListOf(item, rule.Min.Value))));
                        break;
                }

                foreach (var written in breaking)
                {
                    var filter = Filter(written);
                    if (Loads(filter, out var error))
                        continue; // this clause cannot break the row: its setter never leaves the value that way
                    Assert.False(Check.IsValid(Text(filter)), $"the loader refuses {Text(filter)} ({error}) and the schema allows it");
                    if (error!.Contains(rule.Kind == JamlRuleKind.Names ? string.Format(rule.Message, outside) : rule.Message))
                        shown++;
                }
                foreach (var written in satisfying)
                {
                    KeepMinAtOrBelowMax(written);
                    var filter = Filter(written);
                    Assert.True(Loads(filter, out var error), $"{Text(filter)}: {error}");
                    Assert.True(Check.IsValid(Text(filter)), $"the loader accepts {Text(filter)} and the schema refuses it");
                }
            }
            Assert.True(shown > 0, $"no filter shows the loader refusing: {rule.Message}");
        }
    }

    [Fact]
    public void EveryFilterRule_IsRefusedAndAcceptedByTheLoaderAndTheSchemaAlike()
    {
        var one = MinimalClause(Model.ClauseKeys.First().Key);
        foreach (var rule in JamlConfigLoader.FilterRules)
        {
            JsonObject good, bad;
            string message;
            switch (rule.Kind)
            {
                case JamlRuleKind.MaxItems:
                    good = new JsonObject { [rule.Member] = ListOf(one, rule.Max!.Value) };
                    bad = new JsonObject { [rule.Member] = ListOf(one, rule.Max.Value + 1) };
                    message = string.Format(rule.Message, rule.Max.Value + 1);
                    break;
                case JamlRuleKind.NoNullItems:
                    var item = Samples(ListForm(Schema.GetProperty("properties").GetProperty(rule.Member)).GetProperty("items")).First();
                    good = new JsonObject { [rule.Member] = ListOf(item, 2) };
                    bad = new JsonObject { [rule.Member] = new JsonArray(item?.DeepClone(), null) };
                    message = rule.Message;
                    break;
                default:
                    throw new NotSupportedException($"no filter shows a {rule.Kind} rule.");
            }

            Assert.True(Loads(good, out var error), error);
            Assert.True(Check.IsValid(Text(good)));
            Assert.False(Loads(bad, out error));
            Assert.Equal(message, error);
            Assert.False(Check.IsValid(Text(bad)));
        }
    }

    [Fact]
    public void TheLegendaryNames_AreTheJokersOfLegendaryRarity()
    {
        // The names rule on legendaryJoker reads MotelyJokerLegendary's names; a joker is a
        // legendary by its rarity bits. The two say the same.
        var byRarity = Enum.GetValues<MotelyJoker>()
            .Where(joker => (MotelyJokerRarity)((int)joker & MotelyGlobals.JokerRarityMask) == MotelyJokerRarity.Legendary)
            .Select(joker => joker.ToString());
        Assert.Equal(Enum.GetNames<MotelyJokerLegendary>().Order(), byRarity.Order());
    }

    [Fact]
    public void EveryBinderRule_IsRefusedAndAcceptedByTheLoaderAndTheSchemaAlike()
    {
        foreach (var rule in JamlConfigLoader.BinderRules)
        {
            int shown = 0;
            void Refused(JsonObject written)
            {
                var filter = Filter(written);
                if (Loads(filter, out _))
                    return; // not every way in is checked as it binds: entries: is not, soulOrder: is
                Assert.False(Check.IsValid(Text(filter)), $"the loader refuses {Text(filter)} and the schema allows it");
                shown++;
            }
            void Accepted(JsonObject written)
            {
                var filter = Filter(written);
                Assert.True(Loads(filter, out var error), $"{Text(filter)}: {error}");
                Assert.True(Check.IsValid(Text(filter)), $"the loader accepts {Text(filter)} and the schema refuses it");
            }

            foreach (var clause in Model.Clauses)
            {
                var key = clause.Keys[0];
                foreach (var member in clause.Members)
                {
                    JsonObject With(JsonNode? value)
                    {
                        var written = MinimalClause(key);
                        written[member.Name] = value;
                        return written;
                    }

                    if (ReferenceEquals(rule, JamlConfigLoader.ListNumber))
                    {
                        // Members no other row narrows: the list's own range is what holds them.
                        if (member.Kind is not (JamlValueKind.Numbers or JamlValueKind.EventRolls)
                            || member.Min != rule.Min || member.Max != rule.Max)
                            continue;
                        Refused(With(new JsonArray(rule.Min!.Value - 1)));
                        Refused(With(new JsonArray(rule.Max!.Value + 1)));
                        Accepted(With(new JsonArray(rule.Min.Value)));
                        Accepted(With(new JsonArray(rule.Max.Value)));
                    }
                    else if (member.Kind == JamlValueKind.Blocks && rule.AppliesTo(member.Block!))
                    {
                        var block = Model.Blocks.Single(candidate => candidate.Type == member.Block);
                        var entry = MinimalObject(Definition(block));
                        var good = entry.DeepClone().AsObject();
                        if (rule.Kind == JamlRuleKind.Names)
                        {
                            var declared = JamlConfigLoader.BoundMember(member.Block!, rule.Member).Type;
                            var allowed = Enum.GetNames(rule.NamesOf!);
                            entry[rule.Member] = Enum.GetNames(declared).First(name => !allowed.Contains(name));
                            good[rule.Member] = allowed[^1];
                        }
                        else
                        {
                            Assert.Equal(JamlRuleKind.Present, rule.Kind);
                            Assert.True(entry.Remove(rule.Member));
                        }
                        Refused(With(new JsonArray(entry)));
                        Accepted(With(new JsonArray(good)));
                    }
                }
            }
            Assert.True(shown > 0, $"no filter shows the loader refusing: {rule.Message}");
        }
    }

    [Fact]
    public void EveryLimitInTheSchema_IsTheLoadersOwn()
    {
        // The schema's minimum, maximum and minItems on a clause member, reached under any
        // spelling (ante:, and:, an event's own key): one step past each, the loader refuses.
        var failures = new List<string>();
        int limits = 0;
        foreach (var (key, clause) in Model.ClauseKeys)
        {
            foreach (var member in Definition(clause).GetProperty("properties").EnumerateObject())
            {
                foreach (var form in Forms(member.Value))
                {
                    bool list = form.TryGetProperty("items", out var items);
                    var number = list ? items : form;
                    var past = new List<JsonNode>();
                    if (number.TryGetProperty("minimum", out var minimum))
                        past.Add(list ? new JsonArray(minimum.GetInt32() - 1) : JsonValue.Create(minimum.GetInt32() - 1));
                    if (number.TryGetProperty("maximum", out var maximum))
                        past.Add(list ? new JsonArray(maximum.GetInt32() + 1) : JsonValue.Create(maximum.GetInt32() + 1));
                    if (list && form.TryGetProperty("minItems", out var minItems))
                        past.Add(ListOf(Samples(items).First(), minItems.GetInt32() - 1));

                    foreach (var value in past)
                    {
                        var written = MinimalClause(key);
                        written[member.Name] = value;
                        limits++;
                        if (Loads(Filter(written), out _))
                            failures.Add($"{Text(written)} loads, past the schema's limit on {clause.SchemaName}.{member.Name}");
                        Assert.False(Check.IsValid(Text(Filter(written))));
                    }
                }
            }
        }
        Assert.True(failures.Count == 0, string.Join("\n", failures));
        Assert.True(limits > 100);
    }

    // ── e. descriptions ──

    [Fact]
    public void EveryTypeAndMember_HasOneDescription_AndNoDescriptionIsLeftOver()
    {
        Assert.True(Model.MissingDescriptions.Count == 0,
            $"JamlSchemaAnnotations describes nothing for: {string.Join(", ", Model.MissingDescriptions)}");
        Assert.True(Model.OrphanDescriptions.Count == 0,
            $"JamlSchemaAnnotations has entries for what no filter writes: {string.Join(", ", Model.OrphanDescriptions)}");
        Assert.All(Model.Types, type => Assert.False(string.IsNullOrWhiteSpace(type.Description)));
        Assert.All(Model.Types.SelectMany(type => type.Members), member => Assert.False(string.IsNullOrWhiteSpace(member.Description)));
    }

    [Fact]
    public void AMissingOrLeftOverDescription_StopsTheGenerator()
    {
        var table = JamlSchemaAnnotations.Text;
        var orphan = (typeof(JamlConfig), "noSuchMember");
        var needed = (typeof(JamlConfig), "deck");
        var kept = table[needed];
        try
        {
            table[orphan] = "Describes nothing.";
            Assert.Equal(["JamlConfig.noSuchMember"], JamlSchemaModelBuilder.Build().OrphanDescriptions);
            Assert.Contains("JamlConfig.noSuchMember", Assert.Throws<InvalidOperationException>(JamlSchemaGenerator.Generate).Message);
            table.Remove(orphan);

            table.Remove(needed);
            Assert.Equal(["JamlConfig.deck"], JamlSchemaModelBuilder.Build().MissingDescriptions);
            Assert.Contains("JamlConfig.deck", Assert.Throws<InvalidOperationException>(JamlSchemaGenerator.Generate).Message);
        }
        finally
        {
            table.Remove(orphan);
            table[needed] = kept;
        }
        Assert.Empty(JamlSchemaModelBuilder.Build().MissingDescriptions);
    }

    // ── building filters from the schema ──

    private static JsonElement Definition(JamlTypeModel type) =>
        ReferenceEquals(type, Model.Filter) ? Schema : Schema.GetProperty("definitions").GetProperty(type.SchemaName);

    private static JsonObject Filter(JsonObject clause) => new() { ["must"] = new JsonArray(clause.DeepClone()) };

    private static string Text(JsonNode node) => node.ToJsonString(Plain);

    private static bool Loads(JsonNode filter, out string? error) => JamlConfig.TryLoad(Text(filter), out _, out error);

    private static JsonArray ListOf(JsonNode? item, int count) =>
        new([.. Enumerable.Range(0, count).Select(_ => item?.DeepClone())]);

    /// <summary>The forms a member may be written in: the options of its anyOf, or itself.</summary>
    private static IEnumerable<JsonElement> Forms(JsonElement member) =>
        member.TryGetProperty("anyOf", out var options) ? options.EnumerateArray() : [member];

    private static JsonElement ListForm(JsonElement member) =>
        Forms(member).First(form => form.TryGetProperty("items", out _));

    private static bool HasListForm(JsonElement member) => Forms(member).Any(form => form.TryGetProperty("items", out _));

    private static bool BreaksARuleOn(Type clause, string member, int number) =>
        JamlConfigLoader.ClauseRules.Any(rule =>
            rule.Member == member && rule.AppliesTo(clause)
            && (rule.Kind is JamlRuleKind.Range or JamlRuleKind.Excluded && rule.BrokenBy(number)
                || rule.Kind == JamlRuleKind.Forbidden));

    /// <summary>
    /// <c>max</c> is not below <c>min</c>, which the schema cannot say and
    /// <see cref="JamlRuleKind.NotBelowMin"/> does: a filter that writes a <c>max</c> below the
    /// default <c>min</c> writes that <c>min</c> too.
    /// </summary>
    private static void KeepMinAtOrBelowMax(JsonObject clause)
    {
        foreach (var rule in JamlConfigLoader.ClauseRules.Where(rule => rule.Kind == JamlRuleKind.NotBelowMin))
        {
            if (clause[rule.Member] is JsonValue max && max.TryGetValue(out int most) && clause["min"] is null)
                clause["min"] = most;
        }
    }

    /// <summary>
    /// A clause whose list must be written under one of several members (an <c>and</c>'s clauses
    /// under <c>and:</c> or <c>clauses:</c>) still needs one of them after its key is written as null.
    /// </summary>
    private static void FillAListTheClauseNeeds(JsonElement definition, JsonObject clause)
    {
        if (!definition.TryGetProperty("anyOf", out var ways))
            return;
        using var written = JsonDocument.Parse(Text(clause));
        if (ways.EnumerateArray().Any(way => Check.IsValid(way, written.RootElement)))
            return;
        var other = ways.EnumerateArray()
            .Select(way => way.GetProperty("required")[0].GetString()!)
            .First(name => clause[name] is null);
        clause[other] = Samples(definition.GetProperty("properties").GetProperty(other)).First(sample => sample is not null)?.DeepClone();
    }

    private static void Write(JsonObject clause, string member, JsonNode? value)
    {
        clause[member] = value?.DeepClone();
        KeepMinAtOrBelowMax(clause);
    }

    /// <summary>The least a clause under <paramref name="key"/> can be: its key, and what the schema requires beside it.</summary>
    private static JsonObject MinimalClause(string key)
    {
        var definition = Definition(Model.ClauseKeys.First(pair => pair.Key == key).Clause);
        var clause = MinimalObject(definition);
        clause[key] = Samples(definition.GetProperty("properties").GetProperty(key)).First()?.DeepClone();
        return clause;
    }

    private static JsonObject MinimalObject(JsonElement definition)
    {
        var written = new JsonObject();
        if (definition.TryGetProperty("required", out var required))
        {
            foreach (var name in required.EnumerateArray())
                written[name.GetString()!] =
                    Samples(definition.GetProperty("properties").GetProperty(name.GetString()!)).First()?.DeepClone();
        }
        return written;
    }

    /// <summary>
    /// Values the schema allows for a member, from each of its forms: both ends of a number's
    /// range, the first and last name of an enum, each any-word, a list at its shortest and
    /// longer, null where allowed, and for a mapping each of its members in turn.
    /// </summary>
    private static IEnumerable<JsonNode?> Samples(JsonElement schema)
    {
        if (schema.TryGetProperty("$ref", out var reference))
        {
            var name = reference.GetString()!;
            if (name == "#/definitions/clause")
            {
                yield return MinimalClause(Model.ClauseKeys.First().Key);
                yield break;
            }
            var target = Check.Resolve(name);
            if (name.StartsWith("#/definitions/enums/", StringComparison.Ordinal))
            {
                foreach (var sample in Samples(target))
                    yield return sample;
                yield break;
            }
            yield return MinimalObject(target);
            foreach (var member in target.GetProperty("properties").EnumerateObject())
            {
                foreach (var sample in Samples(member.Value))
                {
                    var written = MinimalObject(target);
                    written[member.Name] = sample?.DeepClone();
                    yield return written;
                }
            }
            yield break;
        }

        if (schema.TryGetProperty("anyOf", out var options))
        {
            foreach (var option in options.EnumerateArray())
                foreach (var sample in Samples(option))
                    yield return sample;
            yield break;
        }

        var type = schema.GetProperty("type");
        if (type.ValueKind == JsonValueKind.Array)
        {
            yield return "x";
            yield return 5;
            yield return true;
            yield break;
        }
        switch (type.GetString())
        {
            case "null":
                yield return null;
                break;
            case "boolean":
                yield return true;
                yield return false;
                break;
            case "integer" when schema.TryGetProperty("enum", out var numbers):
                yield return numbers[0].GetInt32();
                yield return numbers[numbers.GetArrayLength() - 1].GetInt32();
                break;
            case "integer":
                var excluded = schema.TryGetProperty("not", out var not)
                    ? not.GetProperty("enum").EnumerateArray().Select(number => number.GetInt32()).ToList()
                    : [];
                int low = schema.TryGetProperty("minimum", out var minimum) ? minimum.GetInt32() : 0;
                int high = schema.TryGetProperty("maximum", out var maximum) ? maximum.GetInt32() : low + 7;
                foreach (var number in new[] { low, high }.Distinct().Where(number => !excluded.Contains(number)))
                    yield return number;
                break;
            case "string" when schema.TryGetProperty("enum", out var names):
                yield return names[0].GetString();
                yield return names[names.GetArrayLength() - 1].GetString();
                break;
            case "string" when schema.GetProperty("pattern").GetString() == JamlSchemaGenerator.AnyPattern():
                foreach (var word in JamlClauseBinding.AnyWords)
                {
                    yield return word;
                    yield return word.ToUpperInvariant();
                }
                break;
            case "string":
                // An enum's loose spellings: EveryLooseSpellingTheSchemaReads_IsANameToTheLoader reads them all.
                break;
            case "array":
                var items = Samples(schema.GetProperty("items")).ToList();
                int least = schema.TryGetProperty("minItems", out var minItems) ? minItems.GetInt32() : 0;
                if (least == 0)
                    yield return new JsonArray();
                yield return ListOf(items[0], Math.Max(least, 1));
                yield return ListOf(items[^1], Math.Max(least, 1) + 1);
                break;
            default:
                throw new NotSupportedException($"no sample for the schema type '{type}'.");
        }
    }
}
