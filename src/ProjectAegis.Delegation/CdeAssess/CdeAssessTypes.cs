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
public sealed record CdeAssessRow
{
    public string ShooterId { get; }
    public string TargetId { get; }
    public string WeaponFamilyId { get; }
    public CdeAssessRiskKind RiskKind { get; }
    public IReadOnlyList<string> Assumptions { get; }
    public string GeometryRangeClass { get; }
    public string PolicyConstraintText { get; }
    public ulong CorrelationId { get; }
    public double SimTime { get; }
    public ulong SimTick { get; }
    public string? WithholdReason { get; }

    public CdeAssessRow(
        string shooterId,
        string targetId,
        string weaponFamilyId,
        CdeAssessRiskKind riskKind,
        IReadOnlyList<string> assumptions,
        string geometryRangeClass,
        string policyConstraintText,
        ulong correlationId,
        double simTime,
        ulong simTick,
        string? withholdReason)
    {
        ShooterId = shooterId;
        TargetId = targetId;
        WeaponFamilyId = weaponFamilyId;
        RiskKind = riskKind;
        // Defensive immutable copy — callers cannot mutate via cast/shared list.
        Assumptions = Array.AsReadOnly(assumptions.ToArray());
        GeometryRangeClass = geometryRangeClass;
        PolicyConstraintText = policyConstraintText;
        CorrelationId = correlationId;
        SimTime = simTime;
        SimTick = simTick;
        WithholdReason = withholdReason;
    }
}

/// <summary>Ordered, replay-stable collateral/CDE advisory picture for one or more shooter/target legs.</summary>
public sealed record CdeAssessSnapshot
{
    public IReadOnlyList<CdeAssessRow> Rows { get; }

    public CdeAssessSnapshot(IReadOnlyList<CdeAssessRow> rows)
    {
        // Fresh row array + AsReadOnly so cast-back cannot replace elements;
        // each row already clones Assumptions in its ctor.
        var copy = new CdeAssessRow[rows.Count];
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            copy[i] = new CdeAssessRow(
                row.ShooterId,
                row.TargetId,
                row.WeaponFamilyId,
                row.RiskKind,
                row.Assumptions,
                row.GeometryRangeClass,
                row.PolicyConstraintText,
                row.CorrelationId,
                row.SimTime,
                row.SimTick,
                row.WithholdReason);
        }

        Rows = Array.AsReadOnly(copy);
    }

    public static CdeAssessSnapshot Empty { get; } =
        new(Array.Empty<CdeAssessRow>());
}
