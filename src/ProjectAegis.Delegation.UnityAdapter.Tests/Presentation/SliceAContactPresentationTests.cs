using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class SliceAContactPresentationTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("other")]
    public void Unknown_selection_clears_all_previous_details(string? id)
    {
        Assert.That(SliceAContactPresenter.Build(id, KillChain(), Provenance(), Chain(), Authority()),
            Is.EqualTo(SliceAContactPresentation.Empty));
    }

    [Test]
    public void Complete_chain_does_not_infer_release_permission()
    {
        var result = SliceAContactPresenter.Build("c1", KillChain(), Provenance(), Chain(), null);
        Assert.That(result.ChainLine, Does.Contain("COMPLETE"));
        Assert.That(result.ChainLine, Does.Contain("shooter-1"));
        Assert.That(result.AuthorityLine, Does.Contain("UNKNOWN"));
        Assert.That(result.NextActionLine, Does.Contain("authority"));
    }

    [Test]
    public void Approval_required_remains_distinct_from_technical_targetability()
    {
        var result = SliceAContactPresenter.Build("c1", KillChain(), Provenance(), Chain(), Authority(C2AuthorityDisposition.ApprovalRequired));
        Assert.That(result.PhaseLine, Does.Contain("Target"));
        Assert.That(result.AuthorityLine, Does.Contain("APPROVAL REQUIRED"));
        Assert.That(result.AuthorityLine, Does.Contain("WeaponsRelease"));
        Assert.That(result.NextActionLine, Does.Contain("approval"));
    }

    [Test]
    public void Provenance_exposes_source_confidence_age_and_comms_without_color()
    {
        var result = SliceAContactPresenter.Build("c1", KillChain(), Provenance(denied: true), Chain(), Authority());
        Assert.That(result.ProvenanceLine, Does.Contain("sensor-1").And.Contain("High").And.Contain("source-ref"));
        Assert.That(result.FreshnessLine, Does.Contain("7 ticks").And.Contain("DENIED"));
        Assert.That(result.NextActionLine, Does.Contain("communications"));
    }

    [TestCase(SensorToShooterBreakCause.LostSensor, "Reacquire")]
    [TestCase(SensorToShooterBreakCause.StaleTrack, "Refresh")]
    [TestCase(SensorToShooterBreakCause.NoFireControl, "fire-control")]
    [TestCase(SensorToShooterBreakCause.NoEligibleShooter, "shooter")]
    [TestCase(SensorToShooterBreakCause.DegradedTrack, "track quality")]
    public void Broken_chain_explains_cause_and_next_action(SensorToShooterBreakCause cause, string action)
    {
        var result = SliceAContactPresenter.Build("c1", KillChain(), Provenance(), Chain(cause), Authority());
        Assert.That(result.ChainLine, Does.Contain(SensorToShooterBreakCauseLabels.Format(cause)));
        Assert.That(result.NextActionLine, Does.Contain(action));
    }

    [Test]
    public void Lost_contact_remains_explainable_without_active_provenance()
    {
        var result = SliceAContactPresenter.Build("c1", KillChain(KillChainLossKind.Lost), null, null, null);
        Assert.That(result.PhaseLine, Does.Contain("Lost"));
        Assert.That(result.NextActionLine, Does.Contain("Reacquire"));
    }

    [Test]
    public void Stale_provenance_takes_priority_over_complete_chain()
    {
        var result = SliceAContactPresenter.Build("c1", KillChain(), Provenance(stale: true), Chain(), Authority());
        Assert.That(result.FreshnessLine, Does.Contain("STALE"));
        Assert.That(result.NextActionLine, Does.Contain("Refresh"));
    }

    [Test]
    public void Repeated_projection_is_replay_stable()
    {
        Assert.That(SliceAContactPresenter.Build("c1", KillChain(), Provenance(), Chain(), Authority()),
            Is.EqualTo(SliceAContactPresenter.Build("c1", KillChain(), Provenance(), Chain(), Authority())));
    }

    private static KillChainContactSnapshot KillChain(KillChainLossKind loss = KillChainLossKind.None) =>
        new(new[] { new KillChainContactState("c1", "target-1", "sensor-1", KillChainPhase.Target, loss,
            true, true, true, true, 1, 1, 2, 2, 2, new ulong[] { 1, 2 }, new[] { "source-ref" }) },
            Array.Empty<KillChainContactTransition>());

    private static ContactProvenanceSnapshot Provenance(bool stale = false, bool denied = false) =>
        new(new[] { new ContactProvenanceState("c1", new("sensor-1", "target-1", "source-ref"),
            ContactProvenanceConfidence.High, stale ? ContactProvenanceFreshness.Stale : ContactProvenanceFreshness.Fresh,
            7, new("Identified", "target-1", 2, 2), denied,
            denied ? ContactProvenanceQualityState.SilentComms : ContactProvenanceQualityState.None) });

    private static SensorToShooterSnapshot Chain(SensorToShooterBreakCause cause = SensorToShooterBreakCause.None) =>
        new(new[] { new SensorToShooterChain("c1", "target-1", "sensor-1", cause == SensorToShooterBreakCause.None,
            cause, Enum.GetValues<SensorToShooterLinkKind>().Select(kind => new SensorToShooterChainLink(kind,
                cause == SensorToShooterBreakCause.None, cause, kind == SensorToShooterLinkKind.EligibleShooter ? "shooter-1" : "sensor-1",
                "c1", "target-1", "fact")).ToArray()) });

    private static C2AuthorityProjection Authority(C2AuthorityDisposition disposition = C2AuthorityDisposition.Withheld) =>
        new(new(RoeLevel.WeaponsTight, "WEAPONS_TIGHT", disposition, "roe-reason", false),
            new(disposition, "authority-reason", disposition == C2AuthorityDisposition.ApprovalRequired ? RequiredApproval.WeaponsRelease : null),
            Array.Empty<C2AuthorityActionState>());
}
