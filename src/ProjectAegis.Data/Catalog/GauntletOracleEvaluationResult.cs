namespace ProjectAegis.Data.Catalog;

/// <summary>Outcome of evaluating batch CSV rows against <see cref="GauntletOracleExpect"/>.</summary>
public sealed record GauntletOracleEvaluationResult(
    bool Passed,
    IReadOnlyList<string> Failures,
    IReadOnlyList<string>? Warnings = null)
{
    /// <summary>Strict-key / advisory warnings (never empty when accessed via this property).</summary>
    public IReadOnlyList<string> EffectiveWarnings => Warnings ?? Array.Empty<string>();
}
