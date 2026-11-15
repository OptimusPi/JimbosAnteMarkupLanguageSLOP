#nullable enable
namespace Motely;

/// <summary>How the CLI finds a filter file from its <c>--jaml</c> argument.</summary>
public static class CliFilterPath
{
    private static readonly string[] Extensions = [".yaml", ".yml", ".jaml"];

    /// <summary>
    /// The argument itself when a file is there; else <c>Filters/&lt;argument&gt;</c> with
    /// <c>.yaml</c>, <c>.yml</c> or <c>.jaml</c>; null when none of them exists.
    /// </summary>
    public static string? Resolve(string filter)
    {
        if (File.Exists(filter))
            return filter;
        foreach (var extension in Extensions)
        {
            var candidate = Path.Combine("Filters", filter + extension);
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }

    /// <summary>The error for an argument <see cref="Resolve"/> found nothing for: every place it looked.</summary>
    public static string NotFound(string filter) =>
        string.IsNullOrWhiteSpace(filter)
            ? "--jaml needs a filter name or a path."
            : $"no filter file for '{filter}': looked for {Path.GetFullPath(filter)} and {Path.GetFullPath(Path.Combine("Filters", filter))} with {string.Join(", ", Extensions)}.";
}
