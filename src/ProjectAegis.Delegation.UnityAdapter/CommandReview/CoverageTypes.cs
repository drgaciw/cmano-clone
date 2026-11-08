namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

/// <summary>Authored evidence that one package element owns a named coverage responsibility.</summary>
public sealed record CoverageFact(
    string ElementId,
    string CoverageId,
    string Label,
    CoverageAreaGeometry? Geometry = null);

/// <summary>A point in the normalized map frame; both axes are expected in the inclusive [0,1] range.</summary>
public readonly record struct CoverageAreaPoint(double NormalizedX, double NormalizedY);

/// <summary>
/// Authoritative normalized polygon/sector boundary and its provenance. Consumers may scale the
/// normalized points to the current map viewport; this type does not infer range or geodesy.
/// </summary>
public sealed record CoverageAreaGeometry(
    IReadOnlyList<CoverageAreaPoint> Boundary,
    string SourceRef);

/// <summary>Truth state for a named coverage responsibility.</summary>
public enum CoverageStatus
{
    /// <summary>No authored coverage evidence is available.</summary>
    Unknown = 0,

    /// <summary>The explicitly identified contributor is available.</summary>
    Covered = 1,

    /// <summary>The explicitly identified contributor is unavailable or detached.</summary>
    Gap = 2,
}

/// <summary>
/// Coverage result derived only from an explicit <see cref="CoverageFact"/>.
/// Geometry is present only when supplied by an authored, source-labelled spatial fact.
/// </summary>
public sealed record CoverageAssessment(
    CoverageStatus Status,
    string CoverageId,
    string Label,
    CoverageAreaGeometry? Geometry,
    string? ReasonCode)
{
    /// <summary>Named unknown sentinel; it never implies a radius or area.</summary>
    public static CoverageAssessment Unknown { get; } = new(
        CoverageStatus.Unknown,
        string.Empty,
        "UNKNOWN",
        Geometry: null,
        ReasonCode: "COVERAGE_FACT_MISSING");
}
