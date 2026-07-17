namespace ProjectAegis.Data.Catalog;

/// <summary>Outcome of evaluating batch CSV rows against <see cref="GauntletOracleExpect"/>.</summary>
public sealed record GauntletOracleEvaluationResult(
    bool Passed,
    IReadOnlyList<string> Failures);
