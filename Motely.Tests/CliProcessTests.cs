using System.Diagnostics;

namespace Motely.Tests;

/// <summary>
/// Motely.CLI run as its own process, the way a person runs it: what it prints and how it ends.
/// </summary>
public sealed class CliProcessTests : IDisposable
{
    private readonly string _home = Directory.CreateTempSubdirectory("motely-cli-").FullName;

    public void Dispose() => Directory.Delete(_home, recursive: true);

    /// <summary>
    /// The <c>dotnet</c> host that runs Motely.CLI.dll. Under <c>dotnet test</c> the process can be testhost.exe,
    /// which has no runtimeconfig for the CLI and cannot run another assembly, so it is not used.
    /// </summary>
    private static string DotnetHost()
    {
        var current = Environment.ProcessPath;
        if (current is not null
            && string.Equals(Path.GetFileNameWithoutExtension(current), "dotnet", StringComparison.OrdinalIgnoreCase))
            return current;
        var fromSdk = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        return string.IsNullOrEmpty(fromSdk) ? "dotnet" : fromSdk;
    }

    private (int ExitCode, string Out, string Error) Run(params string[] args)
    {
        var start = new ProcessStartInfo(DotnetHost())
        {
            WorkingDirectory = _home,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "Motely.CLI.dll"));
        foreach (var arg in args)
            start.ArgumentList.Add(arg);

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(60)))
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail($"motely {string.Join(' ', args)} was still running after 60 seconds.");
        }
        return (process.ExitCode, output.Result, error.Result);
    }

    private string Filter(string name, string yaml)
    {
        var path = Path.Combine(_home, name);
        File.WriteAllText(path, yaml);
        return path;
    }

    /// <summary>The <c>SEED, score</c> rows of the CSV the CLI prints, without its <c>seed,score</c> header.</summary>
    private static string[] SeedRows(string output) =>
        [.. output.Split('\n').Where(line => line.Contains(',') && !line.StartsWith("seed,", StringComparison.Ordinal))];

    [Theory]
    [InlineData("nope")]
    [InlineData("nope.yaml")]
    [InlineData("nope.x.y")]
    [InlineData("Filters/nope")]
    public void AMissingFilter_NamesEveryPlaceItLooked(string argument)
    {
        var (exitCode, _, error) = Run("--jaml", argument, "--seeds", "ALEEB");

        Assert.Equal(1, exitCode);
        Assert.Contains($"no filter file for '{argument}'", error);
        Assert.Contains(Path.GetFullPath(Path.Combine(_home, argument)), error);
        Assert.Contains(Path.GetFullPath(Path.Combine(_home, "Filters", argument)), error);
        Assert.DoesNotContain(".yaml.yaml", error);
    }

    [Fact]
    public void ABareName_IsFoundUnderFilters_WithADotInIt()
    {
        Directory.CreateDirectory(Path.Combine(_home, "Filters"));
        File.WriteAllText(Path.Combine(_home, "Filters", "v1.2.yaml"), "deck: Red\n");

        var (exitCode, output, error) = Run("--jaml", "v1.2", "--seeds", "ALEEB", "--cutoff", "off", "-q");

        Assert.True(exitCode == 0, error);
        Assert.Single(SeedRows(output));
    }

    [Theory]
    [InlineData("deck: Red\n")]
    [InlineData("mustNot:\n  - voucher: Hieroglyph\n    antes: [1]\n")]
    public void CutoffMax_OnAFilterWithNothingToScore_PrintsOneRow(string yaml)
    {
        var filter = Filter("nothing.yaml", yaml);

        var (exitCode, output, error) = Run("--jaml", filter, "--seeds", "ALEEB,PIROCKS,P1111111,MOTELY77", "--cutoff", "max", "-q");

        Assert.True(exitCode == 0, error);
        Assert.Single(SeedRows(output));
    }

    [Fact]
    public void CutoffMax_WithCollect_OnAFilterWithNothingToScore_Stops()
    {
        // 32.0.1 never reached the second row --collect was waiting for, and swept every seed.
        var filter = Filter("nothing.yaml", "deck: Red\n");

        var (exitCode, output, error) = Run("--jaml", filter, "--cutoff", "max", "--collect", "3", "-q");

        Assert.True(exitCode == 0, error);
        Assert.Single(SeedRows(output));
        Assert.Contains("the first match is the only row", error);
    }

    [Fact]
    public void ANullSourceSlot_Runs_WithNoStackTrace()
    {
        var filter = Filter("null-slot.yaml", "should:\n  - joker: Blueprint\n    sources:\n      shopItems: ~\n");

        var (exitCode, output, error) = Run("--jaml", filter, "--seeds", "ALEEB,PIROCKS", "--cutoff", "off", "-q");

        Assert.True(exitCode == 0, error);
        Assert.Equal(2, SeedRows(output).Length);
        Assert.DoesNotContain("NullReferenceException", error);
    }
}
