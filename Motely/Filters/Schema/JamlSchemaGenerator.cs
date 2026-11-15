using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Motely.Filters;

/// <summary>
/// Writes the three files that describe a filter to the world outside the engine, from the
/// <see cref="JamlSchemaModel"/>: TypeScript types, a JSON Schema, and a small vocabulary for
/// editors. Nothing here names a clause key, a member or an enum value; a description lives in
/// <see cref="JamlSchemaAnnotations"/>. Every file ends its lines with <c>\n</c> on every OS.
/// </summary>
internal static class JamlSchemaGenerator
{
    /// <summary>The command that writes the files, run from the repository root.</summary>
    public const string Command = "dotnet run --project Motely.CLI -- schema";

    /// <summary>Where the files live, from the repository root.</summary>
    public const string Directory = "Motely.Wasm/jaml";

    public const string TypesFile = "index.d.ts";
    public const string SchemaFile = "motely-filter.schema.json";
    public const string VocabFile = "vocab.json";

    private const string SchemaRoot = "#/definitions/";

    /// <summary>What a name may hold between its letters and digits: anything but those and a comma.</summary>
    private const string Separator = "[^A-Za-z0-9,]";

    /// <summary>Each file's name and its whole text.</summary>
    public static IReadOnlyDictionary<string, string> Generate()
    {
        var model = JamlSchemaModelBuilder.Build();
        if (model.MissingDescriptions.Count > 0 || model.OrphanDescriptions.Count > 0)
            throw new InvalidOperationException(
                "JamlSchemaAnnotations is out of step with the loader."
                + (model.MissingDescriptions.Count > 0 ? $" No description for: {string.Join(", ", model.MissingDescriptions)}." : "")
                + (model.OrphanDescriptions.Count > 0 ? $" Describes nothing a filter writes: {string.Join(", ", model.OrphanDescriptions)}." : ""));

        return new Dictionary<string, string>
        {
            [TypesFile] = Types(model),
            [SchemaFile] = Schema(model),
            [VocabFile] = Vocab(model),
        };
    }

    // ── names ──

    private static string EnumTypeName(Type enumType) => enumType.Name + "Name";

    private static string AnyLiteral
    {
        get
        {
            var word = JamlClauseBinding.AnyWords[0];
            return char.ToUpperInvariant(word[0]) + word[1..];
        }
    }

    private static string TypeNameOf(JamlSchemaModel model, Type block) =>
        model.Blocks.First(candidate => candidate.Type == block).TypeName;

    private static string SchemaNameOf(JamlSchemaModel model, Type block) =>
        model.Blocks.First(candidate => candidate.Type == block).SchemaName;

    /// <summary>The numbers an enum also reads as its values (<c>luck: 4</c>), or none.</summary>
    private static IEnumerable<int> NumberValues(Type enumType) =>
        JamlEnumName.NumberIsValue(enumType)
            ? Enum.GetNames(enumType).Select(name => Convert.ToInt32(Enum.Parse(enumType, name)))
            : [];

    /// <summary>
    /// Every loose spelling of <paramref name="enumType"/>'s names the loader reads, as letters
    /// and digits in lower case: each name, with and without a leading "the", and the digit and
    /// word spellings. The loader's own <see cref="JamlEnumName.Tolerant"/> decides which stay.
    /// </summary>
    internal static IReadOnlyList<string> LooseSpellings(Type enumType)
    {
        var spellings = new List<string>();
        var offered = new HashSet<string>(StringComparer.Ordinal);
        void Offer(string spelling)
        {
            if (spelling.Length > 0 && offered.Add(spelling) && JamlEnumName.Tolerant(enumType, spelling) is not null)
                spellings.Add(spelling);
        }

        foreach (var name in Enum.GetNames(enumType).Select(JamlEnumName.Normalize).Concat(JamlEnumName.DigitWordAliases.Keys))
        {
            Offer(name);
            Offer("the" + name);
            if (name.StartsWith("the", StringComparison.Ordinal))
                Offer(name[3..]);
        }
        return spellings;
    }

    /// <summary>The pattern that reads every loose spelling of the enum's names, and its short names.</summary>
    internal static string LoosePattern(Type enumType)
    {
        var pattern = new StringBuilder("^(?:");
        var shortNames = JamlEnumName.ShortNames(enumType);
        if (shortNames.Count > 0)
            pattern.Append(@"\s*(?:").AppendJoin('|', shortNames.Select(name => Letters(name, ""))).Append(@")\s*|");
        // A spelling the loader reads with and without a leading "the" is written once, behind an
        // optional "the"; the rest are written as they are.
        var spellings = LooseSpellings(enumType);
        var all = spellings.ToHashSet(StringComparer.Ordinal);
        var withThe = spellings.Where(spelling => all.Contains("the" + spelling)).ToList();
        var covered = withThe.Select(spelling => "the" + spelling).ToHashSet(StringComparer.Ordinal);
        var plain = spellings.Where(spelling => !covered.Contains(spelling) && !withThe.Contains(spelling)).ToList();
        string Alternatives(IEnumerable<string> group) =>
            string.Join('|', group.Select(spelling => Letters(spelling, Separator + "*")));

        var forms = new List<string>();
        if (withThe.Count > 0)
            forms.Add($"(?:{Letters("the", Separator + "*")}{Separator}*)?(?:{Alternatives(withThe)})");
        if (plain.Count > 0)
            forms.Add(Alternatives(plain));
        pattern.Append(Separator).Append("*(?:").AppendJoin('|', forms).Append(')').Append(Separator).Append("*)$");
        return pattern.ToString();
    }

    /// <summary>The pattern that reads an any-word in any case.</summary>
    internal static string AnyPattern() =>
        "^(?:" + string.Join('|', JamlClauseBinding.AnyWords.Select(word => Letters(word, ""))) + ")$";

    /// <summary>Each letter in either case, each other character as itself, with <paramref name="between"/> between them.</summary>
    private static string Letters(string text, string between) =>
        string.Join(between, text.Select(c =>
            char.IsAsciiLetter(c) ? $"[{char.ToLowerInvariant(c)}{char.ToUpperInvariant(c)}]"
            : char.IsAsciiDigit(c) || c == '~' ? c.ToString()
            : throw new NotSupportedException($"'{c}' in \"{text}\" has no pattern form.")));

    // ── index.d.ts ──

    private static string Types(JamlSchemaModel model)
    {
        var text = new StringBuilder();
        text.Append("// The filter shapes the engine's loader binds. Generated from the loader by\n");
        text.Append($"//   {Command}\n");
        text.Append("// Do not edit: change the clause classes in Motely/Filters (and the descriptions in\n");
        text.Append("// Motely/Filters/Schema/JamlSchemaAnnotations.cs), then run the command. A filter is plain JSON: build a\n");
        text.Append("// JamlFilter, hand JSON.stringify(filter) to JamlConfigLoader.check or fromJaml, and read one back with JSON.parse.\n\n");

        foreach (var enumType in model.Enums)
        {
            var values = Enum.GetNames(enumType).Select(Quote).Concat(NumberValues(enumType).Select(number => number.ToString()));
            text.Append($"export type {EnumTypeName(enumType)} = {string.Join(" | ", values)};\n");
        }

        foreach (var type in model.Types)
        {
            text.Append('\n');
            Doc(text, "", type.Description);
            text.Append($"export interface {TypeNameOf(type, model)} {{\n");
            foreach (var member in type.Members)
            {
                Doc(text, "  ", member.Description);
                text.Append($"  {member.Name}{(member.Required ? "" : "?")}: {TypeOf(model, member)};\n");
            }
            text.Append("}\n");
        }

        text.Append("\n/** One clause: the member that names its kind is required. */\n");
        text.Append("export type JamlClause = ");
        text.AppendJoin(" | ", model.ClauseKeys.SelectMany(pair => ClauseForms(model, pair.Key, pair.Clause)));
        text.Append(";\n");
        return text.ToString();
    }

    /// <summary>
    /// A clause type with its key required. Where the key may be null and the clause's list must
    /// still be written (<c>and: ~</c> beside <c>clauses:</c>), that is two forms: the key as the
    /// list, or the key as null with the list under another member.
    /// </summary>
    private static IEnumerable<string> ClauseForms(JamlSchemaModel model, string key, JamlTypeModel clause)
    {
        var member = clause.Members.First(candidate => candidate.Name == key);
        var group = clause.OneOfWritten.FirstOrDefault(names => names.Contains(key));
        if (clause.OneOfWritten.Count > (group is null ? 0 : 1) || member.Any && group is not null)
            throw new NotSupportedException($"{clause.Type.Name}: no TypeScript form for the lists it must write.");
        if (group is null || !member.Null)
        {
            yield return $"{clause.TypeName} & Required<Pick<{clause.TypeName}, {Quote(key)}>>";
            yield break;
        }

        string Written(JamlMemberModel list) => TypeOf(model, list with { Null = false, Any = false });
        yield return $"{clause.TypeName} & {{ {key}: {Written(member)} }}";
        foreach (var other in group.Where(name => name != key))
            yield return $"{clause.TypeName} & {{ {key}: null; {other}: {Written(clause.Members.First(candidate => candidate.Name == other))} }}";
    }

    /// <summary>The filter itself is JamlFilter to its users, whatever the class is called.</summary>
    private static string TypeNameOf(JamlTypeModel type, JamlSchemaModel model) =>
        ReferenceEquals(type, model.Filter) ? "JamlFilter" : type.TypeName;

    private static void Doc(StringBuilder text, string indent, string description) =>
        text.Append($"{indent}/** {description.Replace("*/", "* /")} */\n");

    private static string Quote(string value) => $"\"{JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(value)}\"";

    private static string TypeOf(JamlSchemaModel model, JamlMemberModel member)
    {
        const string text = "string | number | boolean";
        var named = member.Enum is { } enumType ? EnumTypeName(enumType) : "";
        string[] any = member.Any ? [Quote(AnyLiteral)] : [];
        string[] options = member.Kind switch
        {
            JamlValueKind.Text => [text],
            JamlValueKind.Texts => [$"({text})[]"],
            JamlValueKind.Number => ["number"],
            JamlValueKind.Flag => ["boolean"],
            JamlValueKind.Name => [named],
            JamlValueKind.Numbers or JamlValueKind.EventRolls => ["number", "number[]", .. any],
            JamlValueKind.Names => [named, .. any, $"{named}[]"],
            JamlValueKind.Card => [named, .. any, TypeNameOf(model, member.Block!)],
            // A list that needs a clause is a tuple with one, so an empty literal does not type.
            JamlValueKind.Clauses => [member.MinItems > 0 ? "[JamlClause, ...JamlClause[]]" : "JamlClause[]"],
            JamlValueKind.Block => [TypeNameOf(model, member.Block!)],
            JamlValueKind.Blocks => [$"{TypeNameOf(model, member.Block!)}[]"],
            _ => throw new NotSupportedException($"no TypeScript form for a {member.Kind} member."),
        };
        return string.Join(" | ", member.Null ? [.. options, "null"] : options);
    }

    // ── motely-filter.schema.json ──

    private static string Schema(JamlSchemaModel model) =>
        Json(json =>
        {
            json.WriteStartObject();
            json.WriteString("$schema", "http://json-schema.org/draft-07/schema#");
            json.WriteString("title", "Motely Filter");
            json.WriteString("description", model.Filter.Description);
            json.WriteString("$comment", GeneratedNote);
            WriteMapping(json, model, model.Filter, describe: false);

            json.WriteStartObject("definitions");
            json.WriteStartObject("enums");
            foreach (var enumType in model.Enums)
            {
                json.WriteStartObject(enumType.Name);
                json.WriteStartArray("anyOf");
                json.WriteStartObject();
                json.WriteString("type", "string");
                json.WriteStartArray("enum");
                foreach (var name in Enum.GetNames(enumType))
                    json.WriteStringValue(name);
                json.WriteEndArray();
                json.WriteEndObject();
                json.WriteStartObject();
                json.WriteString("type", "string");
                json.WriteString("pattern", LoosePattern(enumType));
                json.WriteEndObject();
                if (NumberValues(enumType).ToArray() is { Length: > 0 } numbers)
                {
                    json.WriteStartObject();
                    json.WriteString("type", "integer");
                    json.WriteStartArray("enum");
                    foreach (var number in numbers)
                        json.WriteNumberValue(number);
                    json.WriteEndArray();
                    json.WriteEndObject();
                }
                json.WriteEndArray();
                json.WriteEndObject();
            }
            json.WriteEndObject();

            foreach (var type in model.Blocks.Concat(model.Clauses))
            {
                json.WriteStartObject(type.SchemaName);
                WriteMapping(json, model, type, describe: true);
                json.WriteEndObject();
            }

            json.WriteStartObject("clause");
            json.WriteStartArray("anyOf");
            foreach (var (key, clause) in model.ClauseKeys)
            {
                json.WriteStartObject();
                json.WriteStartArray("allOf");
                json.WriteStartObject();
                json.WriteString("$ref", SchemaRoot + clause.SchemaName);
                json.WriteEndObject();
                json.WriteStartObject();
                json.WriteStartArray("required");
                json.WriteStringValue(key);
                json.WriteEndArray();
                json.WriteEndObject();
                json.WriteEndArray();
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteEndObject();

            json.WriteEndObject();
            json.WriteEndObject();
        });

    private static string GeneratedNote =>
        $"Generated from the engine's filter loader by `{Command}`. Do not edit: change the clause classes in Motely/Filters, then run the command.";

    private static void WriteMapping(Utf8JsonWriter json, JamlSchemaModel model, JamlTypeModel type, bool describe)
    {
        if (describe)
            json.WriteString("description", type.Description);
        json.WriteString("type", "object");
        json.WriteBoolean("additionalProperties", false);
        json.WriteStartObject("properties");
        foreach (var member in type.Members)
        {
            json.WriteStartObject(member.Name);
            json.WriteString("description", member.Description);
            var forms = Forms(model, member);
            if (forms.Count == 1)
            {
                forms[0](json);
            }
            else
            {
                json.WriteStartArray("anyOf");
                foreach (var form in forms)
                {
                    json.WriteStartObject();
                    form(json);
                    json.WriteEndObject();
                }
                json.WriteEndArray();
            }
            json.WriteEndObject();
        }
        json.WriteEndObject();

        if (type.Members.Where(member => member.Required).Select(member => member.Name).ToArray() is { Length: > 0 } required)
        {
            json.WriteStartArray("required");
            foreach (var name in required)
                json.WriteStringValue(name);
            json.WriteEndArray();
        }

        if (type.OneOfWritten.Count > 1)
            throw new NotSupportedException($"{type.Type.Name} has more than one list that must be written.");
        foreach (var group in type.OneOfWritten)
        {
            json.WriteStartArray("anyOf");
            foreach (var name in group)
            {
                var member = type.Members.First(candidate => candidate.Name == name);
                json.WriteStartObject();
                if (member.Null || member.Any)
                {
                    // Written as null the member fills nothing, so it has to be the list.
                    json.WriteStartObject("properties");
                    json.WriteStartObject(name);
                    json.WriteString("type", "array");
                    json.WriteEndObject();
                    json.WriteEndObject();
                }
                json.WriteStartArray("required");
                json.WriteStringValue(name);
                json.WriteEndArray();
                json.WriteEndObject();
            }
            json.WriteEndArray();
        }
    }

    /// <summary>The forms a member's value may have, each written into an open object.</summary>
    private static List<Action<Utf8JsonWriter>> Forms(JamlSchemaModel model, JamlMemberModel member)
    {
        void Text(Utf8JsonWriter json)
        {
            json.WriteStartArray("type");
            json.WriteStringValue("string");
            json.WriteStringValue("number");
            json.WriteStringValue("boolean");
            json.WriteEndArray();
        }
        void Number(Utf8JsonWriter json)
        {
            json.WriteString("type", "integer");
            if (member.Min is { } min)
                json.WriteNumber("minimum", min);
            if (member.Max is { } max)
                json.WriteNumber("maximum", max);
            if (member.Excluded.Count > 0)
            {
                json.WriteStartObject("not");
                json.WriteStartArray("enum");
                foreach (var number in member.Excluded)
                    json.WriteNumberValue(number);
                json.WriteEndArray();
                json.WriteEndObject();
            }
        }
        void Named(Utf8JsonWriter json) => json.WriteString("$ref", SchemaRoot + "enums/" + member.Enum!.Name);
        void Block(Utf8JsonWriter json) => json.WriteString("$ref", SchemaRoot + SchemaNameOf(model, member.Block!));
        void Clause(Utf8JsonWriter json) => json.WriteString("$ref", SchemaRoot + "clause");
        void Any(Utf8JsonWriter json)
        {
            json.WriteString("type", "string");
            json.WriteString("pattern", AnyPattern());
        }
        Action<Utf8JsonWriter> ListOf(Action<Utf8JsonWriter> item) => json =>
        {
            json.WriteString("type", "array");
            json.WriteStartObject("items");
            item(json);
            json.WriteEndObject();
            if (member.MinItems > 0)
                json.WriteNumber("minItems", member.MinItems);
            if (member.MaxItems is { } maxItems)
                json.WriteNumber("maxItems", maxItems);
        };

        Action<Utf8JsonWriter>[] any = member.Any ? [Any] : [];
        // One number stands for a list of one, so it is a form only where a list of one is allowed.
        Action<Utf8JsonWriter>[] one = member.MinItems <= 1 ? [Number] : [];
        List<Action<Utf8JsonWriter>> forms = member.Kind switch
        {
            JamlValueKind.Text => [Text],
            JamlValueKind.Texts => [ListOf(Text)],
            JamlValueKind.Number => [Number],
            JamlValueKind.Flag => [json => json.WriteString("type", "boolean")],
            JamlValueKind.Name => [Named],
            JamlValueKind.Numbers or JamlValueKind.EventRolls => [.. one, ListOf(Number), .. any],
            JamlValueKind.Names => [Named, .. any, ListOf(Named)],
            JamlValueKind.Card => [Named, .. any, Block],
            JamlValueKind.Clauses => [ListOf(Clause)],
            JamlValueKind.Block => [Block],
            JamlValueKind.Blocks => [ListOf(Block)],
            _ => throw new NotSupportedException($"no schema form for a {member.Kind} member."),
        };
        if (member.Null)
            forms.Add(json => json.WriteString("type", "null"));
        return forms;
    }

    // ── vocab.json ──

    private static string Vocab(JamlSchemaModel model) =>
        Json(json =>
        {
            json.WriteStartObject();
            json.WriteString("$comment", GeneratedNote);

            json.WriteStartObject("filter");
            WriteMembers(json, model, model.Filter);
            json.WriteEndObject();

            json.WriteStartObject("clauseKeys");
            foreach (var (key, clause) in model.ClauseKeys)
                json.WriteString(key, clause.SchemaName);
            json.WriteEndObject();

            json.WriteStartObject("clauses");
            foreach (var clause in model.Clauses)
            {
                json.WriteStartObject(clause.SchemaName);
                json.WriteStartArray("keys");
                foreach (var key in clause.Keys)
                    json.WriteStringValue(key);
                json.WriteEndArray();
                WriteMembers(json, model, clause);
                json.WriteEndObject();
            }
            json.WriteEndObject();

            json.WriteStartObject("blocks");
            foreach (var block in model.Blocks)
            {
                json.WriteStartObject(block.SchemaName);
                WriteMembers(json, model, block);
                json.WriteEndObject();
            }
            json.WriteEndObject();

            json.WriteStartArray("anyWords");
            foreach (var word in JamlClauseBinding.AnyWords)
                json.WriteStringValue(word);
            json.WriteEndArray();

            json.WriteStartObject("enums");
            foreach (var enumType in model.Enums)
            {
                json.WriteStartArray(enumType.Name);
                foreach (var name in Enum.GetNames(enumType))
                    json.WriteStringValue(name);
                json.WriteEndArray();
            }
            json.WriteEndObject();
            json.WriteEndObject();
        });

    /// <summary>
    /// Each member with how its value is written: the kind, then after a colon the enum whose
    /// names it reads or the block it holds (<c>names:MotelyJoker</c>, <c>block:with</c>).
    /// </summary>
    private static void WriteMembers(Utf8JsonWriter json, JamlSchemaModel model, JamlTypeModel type)
    {
        json.WriteStartObject("members");
        foreach (var member in type.Members)
        {
            var kind = member.Kind.ToString();
            kind = char.ToLowerInvariant(kind[0]) + kind[1..];
            if (member.Enum is { } enumType)
                kind += ":" + enumType.Name;
            else if (member.Block is { } block)
                kind += ":" + SchemaNameOf(model, block);
            json.WriteString(member.Name, kind);
        }
        json.WriteEndObject();
    }

    private static string Json(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        var options = new JsonWriterOptions
        {
            Indented = true,
            NewLine = "\n",
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        using (var json = new Utf8JsonWriter(stream, options))
            write(json);
        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }
}
