namespace Motely.Filters;

/// <summary>
/// How an <c>or</c> combines arm scores. <see cref="Sum"/> totals every arm that hits;
/// <see cref="Max"/> scores only the best arm. AND match semantics stay min-of-children
/// conjunctions — mode does not reopen sum-of-children AND.
/// </summary>
public enum LogicScoreMode
{
    Sum = 0,
    Max = 1,
}

public abstract class LogicClause : IMotelyClause
{
    public string? Label { get; set; }
    public int Min { get; set; } = 1;
    public int? Max { get; set; }
    public int Score { get; set; } = 1;
    public LogicScoreMode Mode { get; set; } = LogicScoreMode.Sum;

    /// <summary>No antes here: each child clause carries its own.</summary>
    [YamlDotNet.Serialization.YamlIgnore]
    public IMotelyClause[] Clauses { get; set; } = [];

    /// <summary>The document key <c>clauses:</c>.</summary>
    [YamlDotNet.Serialization.YamlMember(Alias = "clauses")]
    public object[]? ClauseNodes
    {
        get => Array.ConvertAll(Clauses, static clause => (object)clause);
        set => Clauses = ClauseTags.Bind(value).ToArray();
    }

    public abstract IMotelySeedFilterDesc CreateFilterDesc();
}

[YamlDotNet.Serialization.YamlSerializable]
public sealed partial class AndClause : LogicClause
{
    /// <summary>The document key <c>and:</c>, holding the child clauses.</summary>
    [YamlDotNet.Serialization.YamlMember(Alias = "and")]
    public object[]? AndNodes { get => null; set => ClauseNodes = value ?? ClauseNodes; }

    [YamlDotNet.Serialization.YamlIgnore]
    public AndPayload And
    {
        get => new() { Mode = Mode, Clauses = Clauses, Label = Label, Min = Min, Max = Max, Score = Score };
        set
        {
            Mode = value.Mode;
            Clauses = value.Clauses;
            Label = value.Label;
            Min = value.Min;
            Max = value.Max;
            Score = value.Score;
        }
    }

    public override IMotelySeedFilterDesc CreateFilterDesc() =>
        new AndFilterDesc([.. Clauses.Select(c => c.CreateFilterDesc())]);
}

[YamlDotNet.Serialization.YamlSerializable]
public sealed partial class OrClause : LogicClause
{
    /// <summary>The document key <c>or:</c>, holding the child clauses.</summary>
    [YamlDotNet.Serialization.YamlMember(Alias = "or")]
    public object[]? OrNodes { get => null; set => ClauseNodes = value ?? ClauseNodes; }

    [YamlDotNet.Serialization.YamlIgnore]
    public OrPayload Or
    {
        get => new() { Mode = Mode, Clauses = Clauses, Label = Label, Min = Min, Max = Max, Score = Score };
        set
        {
            Mode = value.Mode;
            Clauses = value.Clauses;
            Label = value.Label;
            Min = value.Min;
            Max = value.Max;
            Score = value.Score;
        }
    }

    public override IMotelySeedFilterDesc CreateFilterDesc() =>
        new OrFilterDesc([.. Clauses.Select(c => c.CreateFilterDesc())], Min);
}
