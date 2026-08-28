namespace ProjectAegis.Delegation.CdeAssess;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Policy;

/// <summary>
/// DRG-220: folds explicit engage-assess preview facts, optional range class, CDE withhold facts,
/// and shooter-scoped policy denials into a deterministic collateral/CDE advisory snapshot.
/// Presentation-only — never emits authorize/release decisions.
/// </summary>
public static class CdeAssessProjection
{
    public const string AssumptionPreviewInRange = "engage-preview: in-range (CanFire=true)";
    public const string AssumptionPreviewOutOfRange = "engage-preview: out-of-range or blocked (CanFire=false)";
    public const string AssumptionNoPreview = "engage-preview: not supplied";
    public const string AssumptionNoPolicyDenial = "policy: no shooter-scoped engage denial at or after attempt tick";
    public const string AssumptionNoCdeWithhold = "cde: no explicit collateral withhold on assess input";
    public const string AssumptionCdeWithhold = "cde: explicit collateral withhold on assess input";
    public const string AssumptionPolicyDenial = "policy: shooter-scoped engage denial applies to current attempt";

    public const string PolicyConstraintClear = "No policy denial for engage attempt";
    public const string GeometryRangeUnknown = "geometry/range: not supplied";

    /// <summary>
    /// Projects the collateral/CDE advisory row for one shooter/target leg. Always advisory — never authorizes release.
    /// </summary>
    public static CdeAssessSnapshot Project(CdeAssessInput input, DecisionLog? log = null)
    {
        var assumptions = new List<string>(4);
        var geometryRangeClass = BuildGeometryRangeClass(input);
        var policyDenial = FindPolicyDenial(log, input);

        if (input.CollateralWithheld)
        {
            assumptions.Add(AssumptionCdeWithhold);
            AppendPreviewAssumption(input.Preview, assumptions);
            assumptions.Add(policyDenial is not null
                ? AssumptionPolicyDenial
                : AssumptionNoPolicyDenial);

            var reason = input.CollateralWithholdReason ?? "CDE_WITHHOLD";
            return new CdeAssessSnapshot(new[]
            {
                CreateRow(
                    input,
                    CdeAssessRiskKind.Withheld,
                    assumptions,
                    geometryRangeClass,
                    BuildCdeWithholdPolicyText(reason),
                    reason),
            });
        }

        if (policyDenial is not null)
        {
            assumptions.Add(AssumptionNoCdeWithhold);
            AppendPreviewAssumption(input.Preview, assumptions);
            assumptions.Add(AssumptionPolicyDenial);

            var reason = policyDenial.Reason.ToString();
            return new CdeAssessSnapshot(new[]
            {
                CreateRow(
                    input,
                    CdeAssessRiskKind.Withheld,
                    assumptions,
                    geometryRangeClass,
                    BuildPolicyConstraintText(policyDenial),
                    reason),
            });
        }

        assumptions.Add(AssumptionNoCdeWithhold);
        AppendPreviewAssumption(input.Preview, assumptions);
        assumptions.Add(AssumptionNoPolicyDenial);

        if (input.Preview is { CanFire: false })
        {
            var abortCode = input.Preview.AbortPreviewCode ?? "ENGAGE_BLOCKED";
            return new CdeAssessSnapshot(new[]
            {
                CreateRow(
                    input,
                    CdeAssessRiskKind.Elevated,
                    assumptions,
                    geometryRangeClass,
                    PolicyConstraintClear,
                    withholdReason: null),
            });
        }

        return new CdeAssessSnapshot(new[]
        {
            CreateRow(
                input,
                CdeAssessRiskKind.Low,
                assumptions,
                geometryRangeClass,
                PolicyConstraintClear,
                withholdReason: null),
        });
    }

    private static CdeAssessRow CreateRow(
        CdeAssessInput input,
        CdeAssessRiskKind riskKind,
        IReadOnlyList<string> assumptions,
        string geometryRangeClass,
        string policyConstraintText,
        string? withholdReason) =>
        new(
            input.ShooterId,
            input.TargetId,
            riskKind,
            assumptions.ToArray(),
            geometryRangeClass,
            policyConstraintText,
            input.CorrelationId,
            input.SimTime,
            input.SimTick,
            withholdReason);

    private static void AppendPreviewAssumption(EngagePreview? preview, List<string> assumptions)
    {
        if (preview is null)
        {
            assumptions.Add(AssumptionNoPreview);
            return;
        }

        assumptions.Add(preview.CanFire
            ? AssumptionPreviewInRange
            : AssumptionPreviewOutOfRange);
    }

    private static string BuildGeometryRangeClass(CdeAssessInput input)
    {
        var parts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(input.RangeClassLabel))
        {
            parts.Add($"range:{input.RangeClassLabel}");
        }

        if (input.Preview is not null)
        {
            parts.Add(input.Preview.DlzLabel);
            if (!string.IsNullOrWhiteSpace(input.Preview.AbortPreviewCode))
            {
                parts.Add($"abort:{input.Preview.AbortPreviewCode}");
            }
        }

        return parts.Count == 0 ? GeometryRangeUnknown : string.Join(" | ", parts);
    }

    private static string BuildPolicyConstraintText(PolicyDenialRecord denial) =>
        $"Policy denial: {denial.Reason} (tick {denial.SimTick})";

    private static string BuildCdeWithholdPolicyText(string reason) =>
        $"CDE/collateral withhold: {reason}";

    /// <summary>
    /// Policy denials record the commanded unit on <see cref="PolicyDenialRecord.TargetId"/> (see
    /// <c>AgentController</c> / <c>SimulationSession</c>), not the hostile victim id.
    /// </summary>
    private static PolicyDenialRecord? FindPolicyDenial(DecisionLog? log, CdeAssessInput input)
    {
        if (log is null)
        {
            return null;
        }

        PolicyDenialRecord? latest = null;
        for (var i = 0; i < log.PolicyDenials.Count; i++)
        {
            var denial = log.PolicyDenials[i];
            if (denial.AttemptedKind != OrderKind.Engage)
            {
                continue;
            }

            if (!string.Equals(denial.TargetId.Value, input.ShooterId, StringComparison.Ordinal))
            {
                continue;
            }

            if (denial.SimTick < input.SimTick)
            {
                continue;
            }

            latest = denial;
        }

        return latest;
    }
}
