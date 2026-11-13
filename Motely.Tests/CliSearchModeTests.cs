using Motely.Filters;

namespace Motely.Tests;

public sealed class CliSearchModeTests
{
    private static IMotelySearchSettings BaseSettings() =>
        MotelySearchBuilder.CreateSettings(JamlConfig.FromJaml("seeds: []"));

    private static CliSearchMode.Input EmptyInput() =>
        new(
            SeedsArgument: null,
            Replay: false,
            JamlPath: null,
            FilterId: null,
            JamlSeeds: null,
            KeywordInputs: [],
            PaddingCharsOption: null,
            RandomCount: null,
            AestheticName: null,
            StartBatch: null,
            EndBatch: null,
            StartPercent: null,
            StartSeed: null,
            StopSeed: null,
            BatchCharacterCount: null
        );

    [Theory]
    [InlineData("seeds")]
    [InlineData("replay")]
    [InlineData("keyword")]
    [InlineData("random")]
    [InlineData("aesthetic")]
    public void TryApplySearchMode_BatchFlagsWithExplicitSearchMode_RefusesCleanly(string mode)
    {
        var baseInput = mode switch
        {
            "seeds" => EmptyInput() with { SeedsArgument = "11111111" },
            "replay" => EmptyInput() with { Replay = true, JamlPath = "f.yaml", JamlSeeds = ["11111111"] },
            "keyword" => EmptyInput() with { KeywordInputs = ["ALEEB"] },
            "random" => EmptyInput() with { RandomCount = 100 },
            "aesthetic" => EmptyInput() with { AestheticName = "palindrome" },
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };

        CliSearchMode.Input[] conflicting =
        [
            baseInput with { StartBatch = 1 },
            baseInput with { EndBatch = 2 },
            baseInput with { StartPercent = 10 },
            baseInput with { BatchCharacterCount = 3 },
            baseInput with { StartSeed = "11111111" },
            baseInput with { StopSeed = "ZZZZZZZZ" },
        ];

        foreach (var input in conflicting)
        {
            bool ok = CliSearchMode.TryApplySearchMode(BaseSettings(), input, out string? error, out _);
            Assert.False(ok);
            Assert.NotNull(error);
        }
    }

    [Fact]
    public void TryApplySearchMode_SeedIndexCombinedWithBatchFlags_RefusesCleanly()
    {
        var input = EmptyInput() with { StartSeed = "11111111", StartBatch = 0 };
        bool ok = CliSearchMode.TryApplySearchMode(BaseSettings(), input, out string? error, out _);
        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryApplySearchMode_SequentialBatchOptions_Accepted()
    {
        var input = EmptyInput() with
        {
            BatchCharacterCount = 3,
            StartBatch = 1,
            EndBatch = 5,
        };
        bool ok = CliSearchMode.TryApplySearchMode(BaseSettings(), input, out string? error, out var updated);
        Assert.True(ok, error);
        Assert.Null(error);
        var typed = Assert.IsType<MotelySearchSettings<PassthroughFilterDesc.PassthroughFilter>>(updated);
        Assert.Equal(3, typed.SequentialBatchCharacterCount);
        Assert.Equal(1, typed.StartBatchIndex);
        Assert.Equal(5, typed.EndBatchIndex);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("1")]
    [InlineData("AB")]
    [InlineData("9Z")]
    public void TryApplySearchMode_ShortKeywordWithoutPadding_ReturnsGuardError(string keyword)
    {
        var input = EmptyInput() with
        {
            KeywordInputs = [keyword],
            PaddingCharsOption = null,
        };
        bool ok = CliSearchMode.TryApplySearchMode(BaseSettings(), input, out string? error, out _);
        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("keywords that short need --padding", error);
    }

    [Theory]
    [InlineData("A", "123456789")]
    [InlineData("AB", "123")]
    [InlineData("9", "ABC")]
    [InlineData("Z9", "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
    public void TryApplySearchMode_ShortKeywordWithValidPadding_Succeeds(string keyword, string padding)
    {
        var input = EmptyInput() with
        {
            KeywordInputs = [keyword],
            PaddingCharsOption = padding,
        };
        bool ok = CliSearchMode.TryApplySearchMode(BaseSettings(), input, out string? error, out var updated);
        Assert.True(ok, error);
        Assert.Null(error);
        Assert.NotNull(updated);
        var typed = Assert.IsType<MotelySearchSettings<PassthroughFilterDesc.PassthroughFilter>>(updated);
        Assert.NotNull(typed.SeedProvider);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("1234")]
    [InlineData("ALEEB")]
    [InlineData("12345678")]
    public void TryApplySearchMode_LongKeywordWithoutPadding_SucceedsWithDefaultDigits(string keyword)
    {
        var input = EmptyInput() with
        {
            KeywordInputs = [keyword],
            PaddingCharsOption = null,
        };
        bool ok = CliSearchMode.TryApplySearchMode(BaseSettings(), input, out string? error, out var updated);
        Assert.True(ok, error);
        Assert.Null(error);
        Assert.NotNull(updated);
        var typed = Assert.IsType<MotelySearchSettings<PassthroughFilterDesc.PassthroughFilter>>(updated);
        Assert.NotNull(typed.SeedProvider);
    }

    [Fact]
    public void TryApplySearchMode_MultipleKeywordsWithOneShortAndNoPadding_RefusesCleanly()
    {
        var input = EmptyInput() with
        {
            KeywordInputs = ["LONGKEY", "A"],
            PaddingCharsOption = null,
        };
        bool ok = CliSearchMode.TryApplySearchMode(BaseSettings(), input, out string? error, out _);
        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("keywords that short need --padding", error);
    }
}
