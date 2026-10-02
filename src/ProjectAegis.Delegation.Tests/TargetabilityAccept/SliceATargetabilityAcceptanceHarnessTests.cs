using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.TargetabilityAccept;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.TargetabilityAccept;

/// <summary>
/// DRG-183: Slice A exit gate. Proves Find through Target for a valid path and an
/// approval-required path without firing a weapon.
/// </summary>
[TestFixture]
public sealed class SliceATargetabilityAcceptanceHarnessTests
{
    [Test]
    public void Valid_path_reaches_target_and_approval_path_is_withheld_without_firing()
    {
        var run = SliceATargetabilityAcceptanceHarness.Run();
        var evidence = run.Evidence;

        Assert.That(evidence.ScenarioId, Is.EqualTo("slice-a-targetability-exit"));
        Assert.That(evidence.StoryId, Is.EqualTo("DRG-183"));
        Assert.That(evidence.ReplayScope, Is.EqualTo("slice-a-exit-gate-off-baltic-v2-hash"));
        Assert.That(evidence.EvaluationSimTick, Is.EqualTo(9UL));
        Assert.That(evidence.EngagementCount, Is.Zero);
        Assert.That(evidence.EngagementOutcomeCount, Is.Zero);
        Assert.That(evidence.PlayerOrderCount, Is.Zero);
        Assert.That(evidence.MagazineChangeCount, Is.Zero);
        Assert.That(evidence.OrdnanceStateChangeCount, Is.Zero);
        Assert.That(evidence.ContactChangeCount, Is.EqualTo(6));
        Assert.That(evidence.Paths, Has.Count.EqualTo(2));
        Assert.That(evidence.AcceptanceFingerprint, Does.Not.Contain("17144800277401907079"));
        Assert.That(run.PinnedAcceptanceFingerprint, Is.Not.Empty);

        var valid = evidence.Paths[0];
        AssertPathIdentity(valid, "valid-target", "Valid", "c-valid", "hostile-1");
        Assert.That(valid.Phase, Is.EqualTo(KillChainPhase.Target));
        Assert.That(valid.Loss, Is.EqualTo(nameof(KillChainLossKind.None)));
        Assert.That(valid.PhasesVisited, Is.EqualTo("Find>Fix>Track>Target"));
        Assert.That(valid.TechnicallyTargetable, Is.True);
        Assert.That(valid.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Permitted));
        Assert.That(valid.WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.None));
        Assert.That(valid.EngageVerbDisposition, Is.EqualTo(nameof(C2AuthorityDisposition.Permitted)));
        Assert.That(valid.EngageVerbReason, Is.Empty);
        AssertSilence(valid);

        var approval = evidence.Paths[1];
        AssertPathIdentity(approval, "approval-required", "ApprovalRequired", "c-approval", "hostile-1");
        Assert.That(approval.Phase, Is.EqualTo(KillChainPhase.Target));
        Assert.That(approval.Loss, Is.EqualTo(nameof(KillChainLossKind.None)));
        Assert.That(approval.PhasesVisited, Is.EqualTo("Find>Fix>Track>Target"));
        Assert.That(approval.TechnicallyTargetable, Is.True);
        Assert.That(approval.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
        Assert.That(approval.WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.WeaponsReleaseRequired));
        Assert.That(approval.AuthorityDisposition, Is.EqualTo(nameof(C2AuthorityDisposition.ApprovalRequired)));
        Assert.That(approval.PendingApproval, Is.EqualTo(nameof(RequiredApproval.WeaponsRelease)));
        Assert.That(approval.EngageVerbDisposition, Is.EqualTo(nameof(C2AuthorityDisposition.ApprovalRequired)));
        Assert.That(approval.EngageVerbReason, Is.EqualTo(TargetabilityAcceptCauseCodes.WeaponsReleaseRequired));
        AssertSilence(approval);
    }

    [Test]
    public void Explanations_name_provenance_freshness_authority_roe_and_cause()
    {
        var evidence = SliceATargetabilityAcceptanceHarness.Run().Evidence;
        var valid = evidence.Paths[0];
        AssertProvenance(valid, ContactProvenanceFreshness.Fresh, "High");
        Assert.That(valid.Roe, Is.EqualTo(nameof(RoeLevel.WeaponsFree)));
        Assert.That(valid.RoeLabel, Is.EqualTo("WEAPONS_FREE"));
        Assert.That(valid.EngageAllowedByRoe, Is.True);
        Assert.That(valid.AuthorityDisposition, Is.EqualTo(nameof(C2AuthorityDisposition.Permitted)));
        Assert.That(valid.AuthorityReasonCode, Is.Empty);
        Assert.That(valid.PendingApproval, Is.Empty);
        Assert.That(valid.ChainComplete, Is.True);
        Assert.That(valid.ChainCauseLabel, Is.Empty);
        Assert.That(valid.Explanation, Does.Contain("provenance=observer:u1|target:hostile-1"));
        Assert.That(valid.Explanation, Does.Contain("freshness=Fresh"));
        Assert.That(valid.Explanation, Does.Contain("roe=WeaponsFree"));
        Assert.That(valid.Explanation, Does.Contain("authority=Permitted"));
        Assert.That(valid.Explanation, Does.Contain("disposition=Permitted"));
        Assert.That(valid.Explanation, Does.Contain("cause=None"));
        Assert.That(valid.Explanation, Does.Contain("phases=Find>Fix>Track>Target"));
        Assert.That(valid.Explanation, Does.Not.Contain("APPROVAL_REQUIRED"));
        Assert.That(valid.Explanation, Does.Not.Contain("WEAPONS_RELEASE_REQUIRED"));
        Assert.That(valid.AcceptFingerprint, Does.Not.Contain("APPROVAL_REQUIRED"));
        Assert.That(valid.AcceptFingerprint, Does.Not.Contain("WEAPONS_RELEASE_REQUIRED"));
        Assert.That(valid.AcceptFingerprint, Does.StartWith("tac:"));
        Assert.That(valid.KillChainFingerprint, Does.StartWith("kc:"));

        var approval = evidence.Paths[1];
        AssertProvenance(approval, ContactProvenanceFreshness.Fresh, "High");
        Assert.That(approval.Roe, Is.EqualTo(nameof(RoeLevel.WeaponsFree)));
        Assert.That(approval.RoeLabel, Is.EqualTo("WEAPONS_FREE"));
        Assert.That(approval.EngageAllowedByRoe, Is.True);
        Assert.That(approval.AuthorityReasonCode, Is.EqualTo(TargetabilityAcceptCauseCodes.WeaponsReleaseRequired));
        Assert.That(approval.ChainComplete, Is.True);
        Assert.That(approval.ChainCauseLabel, Is.Empty);
        Assert.That(approval.Explanation, Does.Contain("freshness=Fresh"));
        Assert.That(approval.Explanation, Does.Contain("quality=None"));
        Assert.That(approval.Explanation, Does.Contain("roe=WeaponsFree"));
        Assert.That(approval.Explanation, Does.Contain("roeLabel=WEAPONS_FREE"));
        Assert.That(approval.Explanation, Does.Contain("authority=ApprovalRequired"));
        Assert.That(approval.Explanation, Does.Contain("authorityReason=WEAPONS_RELEASE_REQUIRED"));
        Assert.That(approval.Explanation, Does.Contain("disposition=Withheld"));
        Assert.That(approval.Explanation, Does.Contain("cause=WEAPONS_RELEASE_REQUIRED"));
        Assert.That(approval.Explanation, Does.Contain("engageVerb=ApprovalRequired"));
        Assert.That(approval.AcceptFingerprint, Does.StartWith("tac:"));
        Assert.That(approval.AcceptFingerprint, Is.Not.EqualTo(valid.AcceptFingerprint));
        Assert.That(approval.KillChainFingerprint, Does.StartWith("kc:"));
        Assert.That(approval.KillChainFingerprint, Is.Not.EqualTo(valid.KillChainFingerprint));
    }

    [Test]
    public void Acceptance_fingerprint_matches_fixture_pin_and_is_replay_stable()
    {
        var first = SliceATargetabilityAcceptanceHarness.Run();
        var second = SliceATargetabilityAcceptanceHarness.Run();

        Assert.That(second.Evidence.AcceptanceFingerprint, Is.EqualTo(first.Evidence.AcceptanceFingerprint));
        Assert.That(second.Evidence.Paths[0].Explanation, Is.EqualTo(first.Evidence.Paths[0].Explanation));
        Assert.That(second.Evidence.Paths[1].Explanation, Is.EqualTo(first.Evidence.Paths[1].Explanation));
        Assert.That(second.Evidence.Paths[0].AcceptFingerprint, Is.EqualTo(first.Evidence.Paths[0].AcceptFingerprint));
        Assert.That(second.Evidence.Paths[1].KillChainFingerprint, Is.EqualTo(first.Evidence.Paths[1].KillChainFingerprint));
        Assert.That(first.Evidence.AcceptanceFingerprint, Does.Contain(first.Evidence.Paths[0].Explanation));
        Assert.That(first.Evidence.AcceptanceFingerprint, Does.Contain(first.Evidence.Paths[1].AcceptFingerprint));
        Assert.That(first.Evidence.AcceptanceFingerprint, Does.Contain(first.Evidence.Paths[0].KillChainFingerprint));
        Assert.That(first.PinnedAcceptanceFingerprint, Is.EqualTo(first.Evidence.AcceptanceFingerprint));
        Assert.That(second.PinnedAcceptanceFingerprint, Is.EqualTo(first.PinnedAcceptanceFingerprint));
    }

    private static void AssertPathIdentity(
        SliceATargetabilityPathEvidence path,
        string pathId,
        string kind,
        string contactId,
        string targetId)
    {
        Assert.That(path.PathId, Is.EqualTo(pathId));
        Assert.That(path.Kind, Is.EqualTo(kind));
        Assert.That(path.ContactId, Is.EqualTo(contactId));
        Assert.That(path.TargetId, Is.EqualTo(targetId));
    }

    private static void AssertProvenance(
        SliceATargetabilityPathEvidence path,
        ContactProvenanceFreshness freshness,
        string confidence)
    {
        Assert.That(path.ProvenancePresent, Is.True);
        Assert.That(path.Freshness, Is.EqualTo(freshness));
        Assert.That(path.AgeTicks, Is.Zero);
        Assert.That(path.QualityState, Is.EqualTo(nameof(ContactProvenanceQualityState.None)));
        Assert.That(path.Confidence, Is.EqualTo(confidence));
        Assert.That(path.SourceRef, Is.EqualTo("observer:u1|target:hostile-1"));
    }

    private static void AssertSilence(SliceATargetabilityPathEvidence path)
    {
        Assert.That(path.ContactChangeCount, Is.EqualTo(3));
        Assert.That(path.EngagementCount, Is.Zero);
        Assert.That(path.EngagementOutcomeCount, Is.Zero);
        Assert.That(path.PlayerOrderCount, Is.Zero);
        Assert.That(path.MagazineChangeCount, Is.Zero);
        Assert.That(path.OrdnanceStateChangeCount, Is.Zero);
    }
}
