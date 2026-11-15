namespace Motely.Filters;

/// <summary>
/// A clause list in the document is <c>List&lt;object&gt;</c> because the static generator cannot
/// register <see cref="IMotelyClause"/>. <see cref="ClauseKeyDeserializer"/> has already built the concrete clause.
/// </summary>
internal static class ClauseTags
{
    internal static List<IMotelyClause> Bind(IReadOnlyList<object>? nodes)
    {
        if (nodes is null || nodes.Count == 0)
            return [];

        var clauses = new List<IMotelyClause>(nodes.Count);
        foreach (var node in nodes)
        {
            if (node is not IMotelyClause clause)
            {
                throw new InvalidOperationException(
                    ClauseKeyDeserializer.NeedsKey
                );
            }
            clauses.Add(clause);
        }
        return clauses;
    }
}
