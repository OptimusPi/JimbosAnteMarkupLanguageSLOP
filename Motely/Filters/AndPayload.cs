namespace Motely.Filters;

/// <summary>The <c>and</c> clause's score mode and children, for code that reads the clause back.</summary>
public sealed class AndPayload
{
    public LogicScoreMode Mode { get; set; } = LogicScoreMode.Sum;
    public IMotelyClause[] Clauses { get; set; } = [];
    public string? Label { get; set; }
    public int Min { get; set; } = 1;
    public int? Max { get; set; }
    public int Score { get; set; } = 1;
}
