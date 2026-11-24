namespace ProjectAegis.Delegation.Skills;

/// <summary>Five required AGD-08 degraded-mode failure conditions.</summary>
public enum DegradedModeCondition
{
    ModelOrSkillUnavailable = 1,
    StaleData = 2,
    NetworkPartition = 3,
    AuthorityWithheld = 4,
    ResourceConflict = 5,
}

/// <summary>Explicit degraded-mode disposition; hidden autonomous fallback is never allowed.</summary>
public enum DegradedModeDisposition
{
    Normal = 0,
    FailClosedWithhold = 1,
    FailClosedManualFallback = 2,
}

/// <summary>Explicit policy rule governing one <see cref="DegradedModeCondition"/>.</summary>
public sealed record DegradedModePolicyRule(
    DegradedModeCondition Condition,
    DegradedModeDisposition Disposition,
    bool AllowHiddenFallback,
    bool RequireOperatorDisclosure,
    string DisclosureCode,
    string PolicyStatement);

/// <summary>Observed runtime condition flags evaluated by <see cref="DegradedModePolicy"/>.</summary>
public sealed record DegradedModeContext(
    bool ModelOrSkillUnavailable = false,
    bool StaleData = false,
    bool NetworkPartition = false,
    bool AuthorityWithheld = false,
    bool ResourceConflict = false,
    string? Detail = null);

/// <summary>Auditable degraded-mode policy evaluation output.</summary>
public sealed record DegradedModeAssessment(
    bool IsDegraded,
    DegradedModeDisposition Disposition,
    bool AllowAutonomousOrSilentFallback,
    bool RequiresOperatorDisclosure,
    IReadOnlyList<DegradedModeCondition> ActiveConditions,
    IReadOnlyList<string> DisclosureCodes,
    string OperatorDisclosure);

/// <summary>
/// Explicit AGD-08 degraded-mode policy contract for Delegation and C2 advisory surfaces.
/// Guarantees fail-closed behavior with zero hidden fallback decision behavior and mandatory operator disclosure.
/// </summary>
public static class DegradedModePolicy
{
    /// <summary>Canonical policy rules across all five required AGD-08 conditions.</summary>
    public static IReadOnlyList<DegradedModePolicyRule> Rules { get; } =
    [
        new(
            DegradedModeCondition.ModelOrSkillUnavailable,
            DegradedModeDisposition.FailClosedManualFallback,
            AllowHiddenFallback: false,
            RequireOperatorDisclosure: true,
            DisclosureCode: "DEGRADED_MODEL_OR_SKILL_UNAVAILABLE",
            PolicyStatement: "Model or skill unavailable: fail closed to explicit manual review; never substitute a hidden model or skill."),
        new(
            DegradedModeCondition.StaleData,
            DegradedModeDisposition.FailClosedWithhold,
            AllowHiddenFallback: false,
            RequireOperatorDisclosure: true,
            DisclosureCode: "DEGRADED_STALE_DATA",
            PolicyStatement: "Stale data or expired proposal: fail closed and require operator refresh; never execute on expired facts."),
        new(
            DegradedModeCondition.NetworkPartition,
            DegradedModeDisposition.FailClosedWithhold,
            AllowHiddenFallback: false,
            RequireOperatorDisclosure: true,
            DisclosureCode: "DEGRADED_NETWORK_PARTITION",
            PolicyStatement: "Network partition or silent comms: fail closed and surface link isolation explicitly."),
        new(
            DegradedModeCondition.AuthorityWithheld,
            DegradedModeDisposition.FailClosedWithhold,
            AllowHiddenFallback: false,
            RequireOperatorDisclosure: true,
            DisclosureCode: "DEGRADED_AUTHORITY_WITHHELD",
            PolicyStatement: "Authority or policy withheld/unavailable: fail closed and withhold release without implicit escalation."),
        new(
            DegradedModeCondition.ResourceConflict,
            DegradedModeDisposition.FailClosedManualFallback,
            AllowHiddenFallback: false,
            RequireOperatorDisclosure: true,
            DisclosureCode: "DEGRADED_RESOURCE_CONFLICT",
            PolicyStatement: "Resource conflict or commitment exclusion: fail closed and disclose competing allocation to the operator."),
    ];

    /// <summary>Evaluates <paramref name="context"/> against <see cref="Rules"/>.</summary>
    public static DegradedModeAssessment Evaluate(DegradedModeContext? context)
    {
        if (context is null
            || (!context.ModelOrSkillUnavailable
                && !context.StaleData
                && !context.NetworkPartition
                && !context.AuthorityWithheld
                && !context.ResourceConflict))
        {
            return new DegradedModeAssessment(
                IsDegraded: false,
                Disposition: DegradedModeDisposition.Normal,
                AllowAutonomousOrSilentFallback: false,
                RequiresOperatorDisclosure: false,
                ActiveConditions: Array.Empty<DegradedModeCondition>(),
                DisclosureCodes: Array.Empty<string>(),
                OperatorDisclosure: "Nominal: no degraded-mode condition active.");
        }

        var active = new List<DegradedModeCondition>(5);
        var codes = new List<string>(5);
        var statements = new List<string>(5);

        void AddIf(bool flag, DegradedModeCondition condition)
        {
            if (!flag)
            {
                return;
            }

            var rule = FindRule(condition);
            active.Add(condition);
            codes.Add(rule.DisclosureCode);
            statements.Add(rule.PolicyStatement);
        }

        AddIf(context.ModelOrSkillUnavailable, DegradedModeCondition.ModelOrSkillUnavailable);
        AddIf(context.StaleData, DegradedModeCondition.StaleData);
        AddIf(context.NetworkPartition, DegradedModeCondition.NetworkPartition);
        AddIf(context.AuthorityWithheld, DegradedModeCondition.AuthorityWithheld);
        AddIf(context.ResourceConflict, DegradedModeCondition.ResourceConflict);

        var disposition = active.Any(c =>
                c is DegradedModeCondition.StaleData
                    or DegradedModeCondition.NetworkPartition
                    or DegradedModeCondition.AuthorityWithheld)
            ? DegradedModeDisposition.FailClosedWithhold
            : DegradedModeDisposition.FailClosedManualFallback;

        var disclosure = string.Join(" ", statements);
        if (!string.IsNullOrWhiteSpace(context.Detail))
        {
            disclosure = $"{disclosure} Detail: {context.Detail!.Trim()}";
        }

        return new DegradedModeAssessment(
            IsDegraded: true,
            Disposition: disposition,
            AllowAutonomousOrSilentFallback: false,
            RequiresOperatorDisclosure: true,
            ActiveConditions: active,
            DisclosureCodes: codes,
            OperatorDisclosure: disclosure);
    }

    private static DegradedModePolicyRule FindRule(DegradedModeCondition condition)
    {
        for (var i = 0; i < Rules.Count; i++)
        {
            if (Rules[i].Condition == condition)
            {
                return Rules[i];
            }
        }

        throw new InvalidOperationException($"Missing DegradedModePolicy rule for {condition}.");
    }
}
