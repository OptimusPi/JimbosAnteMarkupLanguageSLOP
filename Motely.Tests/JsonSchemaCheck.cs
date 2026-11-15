using System.Text.Json;
using System.Text.RegularExpressions;

namespace Motely.Tests;

/// <summary>
/// The part of JSON Schema draft-07 that <c>motely-filter.schema.json</c> uses, and no more: a
/// keyword it does not know is an error, so the generated schema cannot grow one that these tests
/// then read as "valid".
/// </summary>
internal sealed class JsonSchemaCheck(JsonElement root)
{
    private static readonly HashSet<string> Notes = ["$schema", "title", "description", "$comment", "definitions"];
    private readonly Dictionary<string, Regex> patterns = [];

    public bool IsValid(JsonElement instance) => IsValid(root, instance);

    public bool IsValid(string json)
    {
        using var document = JsonDocument.Parse(json);
        return IsValid(document.RootElement);
    }

    /// <summary>The schema a <c>#/a/b</c> reference names.</summary>
    public JsonElement Resolve(string reference)
    {
        Assert.StartsWith("#/", reference);
        var at = root;
        foreach (var part in reference[2..].Split('/'))
            at = at.GetProperty(part);
        return at;
    }

    public bool IsValid(JsonElement schema, JsonElement instance)
    {
        // In draft-07 a $ref stands alone: what sits beside it is not read.
        if (schema.TryGetProperty("$ref", out var reference))
            return IsValid(Resolve(reference.GetString()!), instance);

        foreach (var keyword in schema.EnumerateObject())
        {
            var value = keyword.Value;
            bool holds = keyword.Name switch
            {
                _ when Notes.Contains(keyword.Name) => true,
                "type" => value.ValueKind == JsonValueKind.Array
                    ? value.EnumerateArray().Any(type => IsType(type.GetString()!, instance))
                    : IsType(value.GetString()!, instance),
                "enum" => value.EnumerateArray().Any(allowed => Same(allowed, instance)),
                "pattern" => instance.ValueKind != JsonValueKind.String || Pattern(value.GetString()!).IsMatch(instance.GetString()!),
                "anyOf" => value.EnumerateArray().Any(option => IsValid(option, instance)),
                "allOf" => value.EnumerateArray().All(option => IsValid(option, instance)),
                "not" => !IsValid(value, instance),
                "minimum" => instance.ValueKind != JsonValueKind.Number || instance.GetDouble() >= value.GetDouble(),
                "maximum" => instance.ValueKind != JsonValueKind.Number || instance.GetDouble() <= value.GetDouble(),
                "minItems" => instance.ValueKind != JsonValueKind.Array || instance.GetArrayLength() >= value.GetInt32(),
                "maxItems" => instance.ValueKind != JsonValueKind.Array || instance.GetArrayLength() <= value.GetInt32(),
                "items" => instance.ValueKind != JsonValueKind.Array || instance.EnumerateArray().All(item => IsValid(value, item)),
                "required" => instance.ValueKind != JsonValueKind.Object
                    || value.EnumerateArray().All(name => instance.TryGetProperty(name.GetString()!, out _)),
                "properties" => instance.ValueKind != JsonValueKind.Object
                    || value.EnumerateObject().All(member =>
                        !instance.TryGetProperty(member.Name, out var written) || IsValid(member.Value, written)),
                "additionalProperties" => value.ValueKind == JsonValueKind.False
                    ? instance.ValueKind != JsonValueKind.Object
                        || instance.EnumerateObject().All(member =>
                            schema.TryGetProperty("properties", out var known) && known.TryGetProperty(member.Name, out _))
                    : throw new NotSupportedException("additionalProperties is only read as false."),
                _ => throw new NotSupportedException($"JsonSchemaCheck does not read the keyword '{keyword.Name}'."),
            };
            if (!holds)
                return false;
        }
        return true;
    }

    private Regex Pattern(string pattern)
    {
        if (!patterns.TryGetValue(pattern, out var regex))
            patterns[pattern] = regex = new Regex(pattern, RegexOptions.ECMAScript);
        return regex;
    }

    private static bool IsType(string type, JsonElement instance) =>
        type switch
        {
            "object" => instance.ValueKind == JsonValueKind.Object,
            "array" => instance.ValueKind == JsonValueKind.Array,
            "string" => instance.ValueKind == JsonValueKind.String,
            "boolean" => instance.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "null" => instance.ValueKind == JsonValueKind.Null,
            "number" => instance.ValueKind == JsonValueKind.Number,
            "integer" => instance.ValueKind == JsonValueKind.Number && instance.GetDouble() % 1 == 0,
            _ => throw new NotSupportedException($"JsonSchemaCheck does not read the type '{type}'."),
        };

    private static bool Same(JsonElement a, JsonElement b) =>
        a.ValueKind == b.ValueKind
        && a.ValueKind switch
        {
            JsonValueKind.String => a.GetString() == b.GetString(),
            JsonValueKind.Number => a.GetDouble() == b.GetDouble(),
            _ => a.GetRawText() == b.GetRawText(),
        };
}
