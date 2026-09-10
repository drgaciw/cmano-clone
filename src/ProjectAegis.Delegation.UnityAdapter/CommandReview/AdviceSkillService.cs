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

/// <summary>Invokes a bounded advisory read function and returns the existing auditable skill envelope.</summary>
public static class AdviceSkillService
{
    public static AdviceSkillResult Invoke(string skillId, AdviceFrame advice)
    {
        if (advice is null) throw new ArgumentNullException(nameof(advice));
        if (!AdviceSkillCatalog.TryGet(skillId, out _)) throw new ArgumentOutOfRangeException(nameof(skillId), skillId, "Unknown advisory skill.");
        var invocation = $"{skillId}:{advice.SimTick}:{advice.SimTime.ToString("R", CultureInfo.InvariantCulture)}:{advice.ContactId}";
        var envelope = new SkillEnvelope(SkillLane.Read, skillId, invocation, null, advice.SimTick, advice.SimTime,
            null, null, null, RequiredApproval.None, advice.Evidence, advice.Assumptions, advice.Rationale, null, null,
            new ReplayProvenance(skillId, invocation, advice.SimTick, advice.SimTime, null, null, false));
        return new AdviceSkillResult(envelope, advice);
    }
}
