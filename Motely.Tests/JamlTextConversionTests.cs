namespace Motely.Tests;

/// <summary>
/// <see cref="JamlConfig.ToJson"/> and <see cref="JamlConfig.ToYaml"/> let a JS app
/// hold every filter as JSON. Pinned on every filter in the test corpus: the JSON loads, and
/// YAML → JSON → YAML → JSON gives the same JSON, so neither direction loses or changes a value.
/// </summary>
public sealed class JamlTextConversionTests
{
    private static readonly string CorpusDir = Path.GetFullPath(
        Path.Join(AppContext.BaseDirectory, "..", "..", "..", "JamlFilters"));

    public static TheoryData<string> CorpusFiles()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory
            .GetFiles(CorpusDir, "*", SearchOption.AllDirectories)
            .Where(f => Path.GetExtension(f) is ".yaml" or ".yml" or ".jaml" or ".json")
            .OrderBy(f => f, StringComparer.Ordinal))
            data.Add(Path.GetRelativePath(CorpusDir, file));
        return data;
    }

    [Theory]
    [MemberData(nameof(CorpusFiles))]
    public void CorpusFilter_RoundTripsThroughJson(string file)
    {
        var yaml = File.ReadAllText(Path.Join(CorpusDir, file));
        Assert.True(JamlConfig.TryLoad(yaml, out _, out var loadError), $"{file}: {loadError}");
        // JSON has no tags. The projection is stable, and it is not a filter.
        var json = JamlConfig.ToJson(yaml);
        Assert.Equal(json, JamlConfig.ToJson(JamlConfig.ToYaml(json)));
    }

    [Fact]
    public void ToJson_TypesUnquotedScalarsAndKeepsQuotedOnesAsStrings()
    {
        var json = JamlConfig.ToJson(
            "name: \"123\"\nid: 123\ncutoff: ~\nmust:\n  - !joker\n    joker: Blueprint\n    antes: [1, 2]\n    score: 1.5\n    edition: 'true'\n");
        Assert.Equal(
            """
            {
              "name": "123",
              "id": 123,
              "cutoff": null,
              "must": [
                {
                  "joker": "Blueprint",
                  "antes": [
                    1,
                    2
                  ],
                  "score": 1.5,
                  "edition": "true"
                }
              ]
            }

            """.ReplaceLineEndings("\n"),
            json);
    }

    [Fact]
    public void ToYaml_WritesBlockClausesAndFlowValueLists()
    {
        var yaml = JamlConfig.ToYaml(
            """{ "name": "Map", "deck": "Ghost", "seeds": ["12345678"], "must": [ { "joker": "Blueprint", "antes": [1], "sources": { "shopItems": [2] } } ] }""");
        Assert.Equal(
            """
            name: Map
            deck: Ghost
            seeds: ["12345678"]
            must:
              - joker: Blueprint
                antes: [1]
                sources:
                  shopItems: [2]

            """.ReplaceLineEndings("\n"),
            yaml);
    }

    [Fact]
    public void ToYaml_KeepsATrailingNewline()
    {
        // A folded or literal block ends its value with "\n"; written plain it would vanish.
        var json = JamlConfig.ToJson("description: >\n  two\n  lines\nmust:\n  - !joker\n    joker: Any\n");
        Assert.Contains("\"two lines\\n\"", json);
        Assert.Equal(json, JamlConfig.ToJson(JamlConfig.ToYaml(json)));
    }

    [Fact]
    public void ToJson_NotYaml_ThrowsWithTheLine()
    {
        var ex = Assert.Throws<JamlFilterException>(() => JamlConfig.ToJson("must:\n  - !joker\n    joker: [Blueprint\n"));
        Assert.True(ex.Line > 0);
    }

    /// <summary>
    /// The loader reads number spellings its own way (<c>010</c> is 8, <c>0x10</c> is 16, and a
    /// seed is its text), so the JSON holds a number only as written and every other spelling as
    /// a string, and the loader makes the same of the JSON as of the YAML — or refuses both.
    /// </summary>
    [Theory]
    [InlineData("cutoff: 5")]
    [InlineData("cutoff: +5")]
    [InlineData("cutoff: 007")]
    [InlineData("cutoff: 010")]
    [InlineData("cutoff: 0x10")]
    [InlineData("cutoff: 0o17")]
    [InlineData("cutoff: .5")]
    [InlineData("cutoff: 5.")]
    [InlineData("cutoff: 1e2")]
    [InlineData("cutoff: 1e400")]
    [InlineData("cutoff: .inf")]
    [InlineData("cutoff: 12345678901234567890")]
    [InlineData("seeds: [99E99, 1E5, 11E11111, 12345678, 1E1E1E1E]")]
    public void ToJson_TheLoaderReadsTheJsonAsItReadsTheYaml(string yaml) =>
        Assert.Equal(Loaded(yaml), Loaded(JamlConfig.ToJson(yaml)));

    [Fact]
    public void ToJson_KeepsANumberAsWritten() =>
        Assert.Equal(
            "{\n  \"a\": 12345678901234567890,\n  \"b\": 1e400,\n  \"c\": 1.10,\n  \"d\": \"010\",\n  \"e\": \"0x10\",\n  \"f\": \"+5\",\n  \"g\": \".inf\"\n}\n",
            JamlConfig.ToJson("a: 12345678901234567890\nb: 1e400\nc: 1.10\nd: 010\ne: 0x10\nf: +5\ng: .inf\n"));

    [Fact]
    public void DeepNesting_IsRefusedAtItsLine()
    {
        // YamlDotNet builds nodes by recursion: 5,000 levels overflowed the stack, and in wasm
        // killed the module. Past 64 levels both readers refuse, as JSON's own reader does.
        var deep = "must:\n  - !joker\n    joker: Joker\n    antes: " + new string('[', 100_000) + new string(']', 100_000) + "\n";
        // ToJson builds the node tree, so the depth cap is what stops it.
        var json = Assert.Throws<JamlFilterException>(() => JamlConfig.ToJson(deep));
        Assert.Equal(4, json.Line);
        Assert.Contains("deeper than 64 levels", json.Message);
        // FromJaml type-checks the number list at the first nested bracket and stops.
        var loaded = Assert.Throws<JamlFilterException>(() => JamlConfig.FromJaml(deep));
        Assert.Equal(4, loaded.Line);
        Assert.Contains("is not a number", loaded.Message);

        var atTheLimit = "x: " + new string('[', 63) + new string(']', 63) + "\n";
        Assert.StartsWith("{", JamlConfig.ToJson(atTheLimit));
        Assert.Throws<JamlFilterException>(() => JamlConfig.ToJson("x: " + new string('[', 64) + new string(']', 64) + "\n"));
    }

    [Fact]
    public void ASecondDocument_IsRefusedAtItsLine()
    {
        // The loader refuses it; reading only the first would turn a refused filter into one that loads.
        foreach (var read in new Func<string, object>[] { JamlConfig.ToJson, JamlConfig.FromJaml })
        {
            var ex = Assert.Throws<JamlFilterException>(() => read("name: a\n---\nname: b\n"));
            Assert.Equal(2, ex.Line);
            Assert.Contains("second document", ex.Message);
        }
        Assert.Equal("{\n  \"name\": \"a\"\n}\n", JamlConfig.ToJson("---\nname: a\n...\n"));
    }

    [Fact]
    public void Aliases_AreExpandedWithinBounds()
    {
        Assert.Equal(
            "{\n  \"a\": [\n    1\n  ],\n  \"b\": [\n    1\n  ]\n}\n",
            JamlConfig.ToJson("a: &x [1]\nb: *x\n"));

        // An alias inside the node it names never ends.
        var cycle = Assert.Throws<JamlFilterException>(() => JamlConfig.ToJson("a: &a [*a]\n"));
        Assert.Contains("deeper than 64 levels", cycle.Message);

        // Each level aliases the last ten times: six levels are ten million values.
        var bomb = new System.Text.StringBuilder("l0: &l0 [x, x, x, x, x, x, x, x, x, x]\n");
        for (int i = 1; i <= 6; i++)
            bomb.Append($"l{i}: &l{i} [").Append(string.Join(", ", Enumerable.Repeat($"*l{i - 1}", 10))).Append("]\n");
        var expand = Assert.Throws<JamlFilterException>(() => JamlConfig.ToJson(bomb.ToString()));
        Assert.Contains("aliases expand the document", expand.Message);
    }

    [Theory]
    [InlineData("? [a, b]\n: 1\n", "a key is a plain value")]
    [InlineData("name: !foo bar\n", "a filter has no tag '!foo'")]
    [InlineData("must: !!seq [{joker: Joker}]\n", "a filter has no tag")]
    public void ToJson_RefusesWhatJsonWouldDrop(string yaml, string message)
    {
        var ex = Assert.Throws<JamlFilterException>(() => JamlConfig.ToJson(yaml));
        Assert.Equal(1, ex.Line);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void ToJson_KeepsTheTagsTheLoaderReads() =>
        Assert.Equal("{\n  \"name\": \"5\",\n  \"cutoff\": 5\n}\n", JamlConfig.ToJson("name: !!str 5\ncutoff: !!int 5\n"));

    [Theory]
    [InlineData("0085")]
    [InlineData("2028")]
    [InlineData("2029")]
    [InlineData("007f")]
    [InlineData("0080")]
    [InlineData("009f")]
    [InlineData("feff")]
    [InlineData("fffe")]
    [InlineData("ffff")]
    public void ToYaml_EscapesWhatYamlCannotHoldRaw(string hex)
    {
        // YAML reads U+0085, U+2028 and U+2029 as line breaks, and prints none of the others.
        var json = JamlConfig.ToJson($"name: \"a\\u{hex}b\"\n");
        var yaml = JamlConfig.ToYaml(json);
        Assert.Equal($"name: \"a\\u{hex}b\"\n", yaml);
        Assert.Equal(json, JamlConfig.ToJson(yaml));
    }

    [Theory]
    [InlineData("{\"name\": \"a\",\n \"name\": \"b\"}", "Duplicate property")]
    [InlineData("{\"name\": \"a\",\n \"description\": \"\\ud800\"}", "surrogate")]
    public void ToYaml_RefusesWithTheLine(string json, string message)
    {
        var ex = Assert.Throws<JamlFilterException>(() => JamlConfig.ToYaml(json));
        Assert.Equal(2, ex.Line);
        Assert.Contains(message, ex.Message);
    }

    /// <summary>What the loader makes of a filter, or that it refuses it.</summary>
    private static string Loaded(string text) =>
        JamlConfig.TryLoad(text, out var config, out _)
            ? $"cutoff={config.Cutoff} seeds={string.Join(",", config.Seeds)} antes="
                + string.Join(";", config.Must.OfType<IAnteScopedClause>().Select(c => string.Join(",", c.Antes)))
            : "refused";
}
