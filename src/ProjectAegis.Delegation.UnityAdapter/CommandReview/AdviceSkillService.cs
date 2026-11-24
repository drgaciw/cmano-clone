namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using System.Globalization;
using Skills;

/// <summary>Stable capability-scoped Slice C advisory function identities.</summary>
public static class AdviceSkillIds
{
    public const string DatalinkAssessment = "c2.advice.datalink.assess";
    public const string ResourceRecommendation = "c2.advice.resource.recommend";
    public const string MissionPackageExplanation = "c2.advice.mission-package.explain";
}

/// <summary>Discoverable read-only advisory functions. No function names a command.</summary>
public static class AdviceSkillCatalog
{
    public static IReadOnlyList<SkillDescriptor> All { get; } =
    [
        new(AdviceSkillIds.DatalinkAssessment, "Assess current data-link evidence", [SkillLane.Read], []),
        new(AdviceSkillIds.ResourceRecommendation, "Recommend current resources", [SkillLane.Read], []),
        new(AdviceSkillIds.MissionPackageExplanation, "Explain current mission package", [SkillLane.Read], []),
    ];

    public static bool TryGet(string skillId, out SkillDescriptor descriptor)
    {
        descriptor = All.FirstOrDefault(d => string.Equals(d.SkillId, skillId, StringComparison.Ordinal))!;
        return descriptor is not null;
    }
}

/// <summary>Auditable advisory policy invocation result with explicit degraded-mode disclosure.</summary>
public sealed record AdviceSkillPolicyResult(
    AdviceSkillResult? Result,
    DegradedModeAssessment Assessment,
    bool IsFireOrder,
    bool IsWeaponsReleaseAuthorization);

/// <summary>Invokes a bounded advisory read function and returns the existing auditable skill envelope.</summary>
public static class AdviceSkillService
{
    public static AdviceSkillResult Invoke(string skillId, AdviceFrame advice)
    {
        if (advice is null) throw new ArgumentNullException(nameof(advice));
        if (!AdviceSkillCatalog.TryGet(skillId, out var descriptor)) throw new ArgumentOutOfRangeException(nameof(skillId), skillId, "Unknown advisory skill.");
        var invocation = $"{skillId}:{advice.SimTick}:{advice.SimTime.ToString("R", CultureInfo.InvariantCulture)}:{advice.ContactId}";
        var envelope = new SkillEnvelope(SkillLane.Read, skillId, invocation, null, advice.SimTick, advice.SimTime,
            null, null, null, RequiredApproval.None, advice.Evidence, advice.Assumptions, advice.Rationale, null, null,
            new ReplayProvenance(skillId, invocation, advice.SimTick, advice.SimTime, null, null, false));
        var validation = SkillEnvelopeValidator.ValidateRead(envelope, descriptor);
        if (!validation.Ok)
        {
            throw new InvalidOperationException($"Advisory skill envelope rejected: {validation.FailureReason}");
        }

        return new AdviceSkillResult(envelope, advice);
    }

    /// <summary>Assesses all 5 AGD-08 degraded-mode conditions from <paramref name="skillId"/>, <paramref name="advice"/>, and optional runtime context.</summary>
    public static DegradedModeAssessment AssessDegradedMode(
        string skillId,
        AdviceFrame advice,
        DegradedModeContext? runtimeContext = null)
    {
        if (advice is null) throw new ArgumentNullException(nameof(advice));

        var skillMissing = string.IsNullOrWhiteSpace(skillId) || !AdviceSkillCatalog.TryGet(skillId, out _);
        var modelUnavailable = skillMissing || advice.Availability is AdviceAvailability.ModelUnavailable or AdviceAvailability.EvidenceUnavailable;
        var stale = advice.Availability == AdviceAvailability.Stale;
        var partitioned = (runtimeContext?.NetworkPartition ?? false)
            || (!string.IsNullOrWhiteSpace(advice.Fallback) && advice.Fallback.IndexOf("reporting link", StringComparison.OrdinalIgnoreCase) >= 0);
        var authorityWithheld = (runtimeContext?.AuthorityWithheld ?? false)
            || advice.Authority?.Targeting.Disposition == C2AuthorityDisposition.Withheld
            || advice.PolicyConstraints.Any(p => p.IndexOf("authority:Withheld", StringComparison.OrdinalIgnoreCase) >= 0);
        var resourceConflict = (runtimeContext?.ResourceConflict ?? false)
            || advice.ResourceCommitments.Count > 0;

        var merged = new DegradedModeContext(
            ModelOrSkillUnavailable: modelUnavailable || (runtimeContext?.ModelOrSkillUnavailable ?? false),
            StaleData: stale || (runtimeContext?.StaleData ?? false),
            NetworkPartition: partitioned,
            AuthorityWithheld: authorityWithheld,
            ResourceConflict: resourceConflict,
            Detail: runtimeContext?.Detail ?? (skillMissing ? $"unknown skill '{skillId}'" : advice.Fallback));

        return DegradedModePolicy.Evaluate(merged);
    }

    /// <summary>
    /// Policy-governed invocation that never throws on unknown skill or degraded frames,
    /// performs zero hidden fallback, and returns explicit operator disclosure.
    /// </summary>
    public static AdviceSkillPolicyResult InvokeWithPolicy(
        string skillId,
        AdviceFrame advice,
        DegradedModeContext? runtimeContext = null)
    {
        if (advice is null) throw new ArgumentNullException(nameof(advice));

        var assessment = AssessDegradedMode(skillId, advice, runtimeContext);
        if (assessment.IsDegraded
            || string.IsNullOrWhiteSpace(skillId)
            || !AdviceSkillCatalog.TryGet(skillId, out _)
            || advice.Availability != AdviceAvailability.Available)
        {
            return new AdviceSkillPolicyResult(
                Result: null,
                Assessment: assessment,
                IsFireOrder: false,
                IsWeaponsReleaseAuthorization: false);
        }

        var result = Invoke(skillId, advice);
        return new AdviceSkillPolicyResult(
            Result: result,
            Assessment: assessment,
            IsFireOrder: false,
            IsWeaponsReleaseAuthorization: false);
    }
}
