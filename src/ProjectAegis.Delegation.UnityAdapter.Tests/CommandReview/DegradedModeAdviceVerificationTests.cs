using NUnit.Framework;
using ProjectAegis.Data.Evidence;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

[TestFixture]
public sealed class DegradedModeAdviceVerificationTests
{
    [Test]
    public void DecisionLogDeepLinkBinder_surfaces_policySnapshotId_and_personality()
    {
        var log = new DecisionLog();
        log.Append(OrderLogEntry.FromDecisionRecord(
            new DecisionRecord(
                4.0,
                new AgentId("agent-1"),
                new TargetId("u1"),
                AutonomyLevel.Assisted,
                OrderKind.Engage,
                [new ScoredIntent(OrderKind.Engage, 0.85, RiskLevel.High)],
                "organic lock",
                1.0,
                10.0,
                0.2,
                SimTick: 4,
                PolicySnapshotId: 91UL,
                Personality: "Cautious"),
            simTick: 4));

        var seq = log.ChronologicalEntries()[0].SequenceId;
        var detail = DecisionLogDeepLinkProjection.Inspect(log, seq);
        var bound = DecisionLogDeepLinkBinder.Bind(detail);

        Assert.That(bound.TelemetryLine, Does.Contain("PolicySnap: 91"));
        Assert.That(bound.TelemetryLine, Does.Contain("Personality: Cautious"));
        Assert.That(bound.FieldLines, Has.Some.Contains("PolicySnapshotId: 91"));
        Assert.That(bound.FieldLines, Has.Some.Contains("Personality: Cautious"));
    }

    [Test]
    public void AdviceSkillService_InvokeWithPolicy_discloses_all_five_degraded_conditions_with_zero_hidden_fallback()
    {
        var log = new DecisionLog();
        var fpBefore = log.ComputeFingerprint();

        // 1. Model or skill unavailable (both unknown skillId and ModelUnavailable frame)
        var unknownSkill = AdviceSkillService.InvokeWithPolicy(
            "unknown.skill.id",
            AdviceFrame.Unavailable(1, 1.0, AdviceAvailability.Available, "manual"));
        Assert.That(unknownSkill.Result, Is.Null);
        Assert.That(unknownSkill.Assessment.IsDegraded, Is.True);
        Assert.That(unknownSkill.Assessment.ActiveConditions, Does.Contain(DegradedModeCondition.ModelOrSkillUnavailable));
        Assert.That(unknownSkill.Assessment.AllowAutonomousOrSilentFallback, Is.False);
        Assert.That(unknownSkill.IsFireOrder, Is.False);
        Assert.That(unknownSkill.IsWeaponsReleaseAuthorization, Is.False);

        var modelUnavailable = AdviceSkillService.InvokeWithPolicy(
            AdviceSkillIds.DatalinkAssessment,
            AdviceFrame.Unavailable(1, 1.0, AdviceAvailability.ModelUnavailable, "manual review"));
        Assert.That(modelUnavailable.Assessment.ActiveConditions, Does.Contain(DegradedModeCondition.ModelOrSkillUnavailable));
        Assert.That(modelUnavailable.Assessment.RequiresOperatorDisclosure, Is.True);

        // 2. Stale data
        var stale = AdviceSkillService.InvokeWithPolicy(
            AdviceSkillIds.DatalinkAssessment,
            AdviceFrame.Unavailable(2, 2.0, AdviceAvailability.Stale, "refresh"));
        Assert.That(stale.Assessment.ActiveConditions, Does.Contain(DegradedModeCondition.StaleData));
        Assert.That(stale.Assessment.AllowAutonomousOrSilentFallback, Is.False);

        // 3. Network partition
        var partitioned = AdviceSkillService.InvokeWithPolicy(
            AdviceSkillIds.DatalinkAssessment,
            AdviceFrame.Unavailable(3, 3.0, AdviceAvailability.Stale, "restore link"),
            new DegradedModeContext(NetworkPartition: true, Detail: "SilentComms"));
        Assert.That(partitioned.Assessment.ActiveConditions, Does.Contain(DegradedModeCondition.NetworkPartition));
        Assert.That(partitioned.Assessment.OperatorDisclosure, Does.Contain("SilentComms"));

        // 4. Authority withheld
        var withheldAuthority = new C2AuthorityProjection(
            new(RoeLevel.HoldFire, "HOLD_FIRE", C2AuthorityDisposition.Withheld, "RoeHoldFire", false),
            new(C2AuthorityDisposition.Withheld, "RoeHoldFire", null),
            []);
        var withheldFrame = AdviceFrame.Unavailable(4, 4.0, AdviceAvailability.Available, "hold") with
        {
            Authority = withheldAuthority,
        };
        var withheld = AdviceSkillService.InvokeWithPolicy(AdviceSkillIds.ResourceRecommendation, withheldFrame);
        Assert.That(withheld.Result, Is.Null);
        Assert.That(withheld.Assessment.ActiveConditions, Does.Contain(DegradedModeCondition.AuthorityWithheld));
        Assert.That(withheld.IsWeaponsReleaseAuthorization, Is.False);

        // 5. Resource conflict
        var conflictFrame = AdviceFrame.Unavailable(5, 5.0, AdviceAvailability.Available, "review") with
        {
            ResourceCommitments = ["s2/w2: committed elsewhere: 1"],
        };
        var conflict = AdviceSkillService.InvokeWithPolicy(AdviceSkillIds.ResourceRecommendation, conflictFrame);
        Assert.That(conflict.Result, Is.Null);
        Assert.That(conflict.Assessment.ActiveConditions, Does.Contain(DegradedModeCondition.ResourceConflict));
        Assert.That(conflict.IsFireOrder, Is.False);

        // Available frame paired with degraded runtimeContext flags must always fail closed (Result == null)
        var availableFrame = AdviceFrame.Unavailable(6, 6.0, AdviceAvailability.Available, "healthy");
        var degradedContexts = new[]
        {
            new DegradedModeContext(ModelOrSkillUnavailable: true),
            new DegradedModeContext(StaleData: true),
            new DegradedModeContext(NetworkPartition: true),
            new DegradedModeContext(AuthorityWithheld: true),
            new DegradedModeContext(ResourceConflict: true),
        };
        foreach (var ctx in degradedContexts)
        {
            var res = AdviceSkillService.InvokeWithPolicy(AdviceSkillIds.DatalinkAssessment, availableFrame, ctx);
            Assert.That(res.Assessment.IsDegraded, Is.True);
            Assert.That(res.Result, Is.Null);
        }

        Assert.That(log.ComputeFingerprint(), Is.EqualTo(fpBefore));
    }

    [Test]
    public void Real_baltic_replay_fingerprint_extracts_clean_typed_records_without_index_shifting()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", 4);
        var fp = result.DecisionLog.ComputeFingerprint();
        var extracted = GauntletFingerprintEvidenceExtractor.Extract("baltic-patrol", fp);

        Assert.That(extracted, Is.Not.Empty);
        Assert.That(extracted.All(e => e.HasValidPayload()), Is.True);

        var recommendations = extracted
            .Where(e => e.Kind == MissionThreadEvidenceKind.Recommendation)
            .ToArray();
        Assert.That(recommendations, Is.Not.Empty);
        Assert.That(recommendations.All(r => r.Recommendation!.AgentId != r.Recommendation.ChosenKind), Is.True);
        Assert.That(recommendations.All(r => Enum.TryParse<OrderKind>(r.Recommendation!.ChosenKind, out _)), Is.True);

        var engagements = extracted
            .Where(e => e.AuthoritativeSource == "GauntletCsvFingerprint:Engagement")
            .ToArray();
        Assert.That(engagements, Is.Not.Empty);
        Assert.That(engagements.All(e => e.SimTick > 0 && !string.IsNullOrWhiteSpace(e.Engagement!.ShooterUnitId)), Is.True);
    }

    [Test]
    public void Baltic_patrol_preserves_pinned_world_and_fingerprint_hashes_while_surfacing_provenance()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", 4);

        Assert.That(result.WorldHash, Is.EqualTo(17144800277401907079UL));
        Assert.That(result.DetectionWorldHash, Is.EqualTo(15600UL));
        Assert.That(result.FingerprintSha256, Is.EqualTo("2cc200ecf3e274c24e6691deca2d6a4664d194835b0d66e901796ea47a83b2f2"));

        var decisionEntries = result.DecisionLog.ChronologicalEntries()
            .Where(e => e.Kind == OrderLogEntryKind.AgentDecision)
            .ToArray();
        Assert.That(decisionEntries, Is.Not.Empty);

        foreach (var entry in decisionEntries)
        {
            var detail = DecisionLogDeepLinkProjection.Inspect(result.DecisionLog, entry.SequenceId);
            Assert.That(detail.PolicySnapshotId, Is.Not.Null);
            Assert.That(string.IsNullOrWhiteSpace(detail.Personality), Is.False);
        }
    }
}
