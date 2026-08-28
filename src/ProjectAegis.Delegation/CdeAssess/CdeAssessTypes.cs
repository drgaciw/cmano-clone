namespace ProjectAegis.Delegation.CdeAssess;

using ProjectAegis.Delegation.Projection;

/// <summary>Advisory collateral-risk classification for CDE assess (DRG-220). Not an authorization verdict.</summary>
public enum CdeAssessRiskKind
{
    Low = 1,
    Elevated = 2,
    Withheld = 3,
}

/// <summary>
/// Explicit engage-assess / collateral facts supplied by the caller.
/// Does not enqueue orders, resolve combat, or authorize release.
/// </summary>
public sealed record CdeAssessInput(
    string ShooterId,
    string TargetId,
    string WeaponFamilyId,
    ulong SimTick,
    double SimTime,
    ulong CorrelationId,
    EngagePreview? Preview = null,
    string? RangeClassLabel = null,
    bool CollateralWithheld = false,
    string? CollateralWithholdReason = null);

/// <summary>
/// One presentation-facing collateral/CDE advisory row. Sim-clock only — no UI selection, hover, camera, or panel state.
/// Never carries an authorize/release decision.
/// </summary>
public sealed record CdeAssessRow(
    string ShooterId,
    string TargetId,
    CdeAssessRiskKind RiskKind,
    IReadOnlyList<string> Assumptions,
    string GeometryRangeClass,
    string PolicyConstraintText,
    ulong CorrelationId,
    double SimTime,
    ulong SimTick,
    string? WithholdReason);

/// <summary>Ordered, replay-stable collateral/CDE advisory picture for one or more shooter/target legs.</summary>
public sealed record CdeAssessSnapshot
{
    public IReadOnlyList<CdeAssessRow> Rows { get; }

    public CdeAssessSnapshot(IReadOnlyList<CdeAssessRow> rows)
    {
        Rows = rows.ToArray();
    }

    public static CdeAssessSnapshot Empty { get; } =
        new(Array.Empty<CdeAssessRow>());
}
