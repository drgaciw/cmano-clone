using NUnit.Framework;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class SensorToShooterPresentationTests
{
    [TestCase(null)]
    [TestCase("")]
    public void Absent_selection_clears_presentation(string? id)
    {
        Assert.That(
            SensorToShooterPresenter.Build(id, Chain(), eligibilityAvailable: true),
            Is.EqualTo(SensorToShooterPresentation.Empty));
    }

    [Test]
    public void Unknown_contact_reports_unknown_chain_without_clearing_selection_context()
    {
        var result = SensorToShooterPresenter.Build("other", Chain(), eligibilityAvailable: true);
        Assert.That(result, Is.Not.EqualTo(SensorToShooterPresentation.Empty));
        Assert.That(result.ContactIdLine, Does.Contain("other"));
        Assert.That(result.StatusLine, Does.Contain("UNKNOWN"));
    }

    [Test]
    public void Complete_chain_lists_all_four_links_without_release_authority()
    {
        var result = SensorToShooterPresenter.Build("c1", Chain(), eligibilityAvailable: true);
        Assert.That(result.StatusLine, Does.Contain("COMPLETE"));
        Assert.That(result.StatusLine, Does.Contain("not release authority"));
        Assert.That(result.TargetIdLine, Does.Contain("target-1"));
        Assert.That(result.ObserverIdLine, Does.Contain("sensor-1"));
        Assert.That(result.SensorLine, Does.Contain("LINKED").And.Contain("sensor-1"));
        Assert.That(result.TrackLine, Does.Contain("LINKED").And.Contain("c1"));
        Assert.That(result.TargetabilityLine, Does.Contain("LINKED"));
        Assert.That(result.ShooterLine, Does.Contain("LINKED").And.Contain("shooter-1"));
        Assert.That(result.Links, Has.Count.EqualTo(4));
        Assert.That(result.NextActionLine, Does.Contain("does not issue fire orders"));
    }

    [TestCase(SensorToShooterBreakCause.LostSensor, "Reacquire")]
    [TestCase(SensorToShooterBreakCause.StaleTrack, "Refresh")]
    [TestCase(SensorToShooterBreakCause.NoFireControl, "fire-control")]
    [TestCase(SensorToShooterBreakCause.NoEligibleShooter, "shooter")]
    [TestCase(SensorToShooterBreakCause.DegradedTrack, "track quality")]
    public void Broken_chain_names_cause_and_next_action(SensorToShooterBreakCause cause, string action)
    {
        var result = SensorToShooterPresenter.Build("c1", Chain(cause), eligibilityAvailable: true);
        Assert.That(result.StatusLine, Does.Contain("BROKEN"));
        Assert.That(result.StatusLine, Does.Contain(SensorToShooterBreakCauseLabels.Format(cause)));
        Assert.That(result.NextActionLine, Does.Contain(action));
    }

    [Test]
    public void Missing_eligibility_fails_closed_without_fabricating_shooter_facts()
    {
        var result = SensorToShooterPresenter.Build("c1", Chain(), eligibilityAvailable: false);
        Assert.That(result.StatusLine, Does.Contain("UNKNOWN"));
        Assert.That(result.ShooterLine, Does.Contain("UNKNOWN"));
        Assert.That(result.NextActionLine, Does.Contain("Obtain current sensor-to-shooter facts"));
    }

    [Test]
    public void Fingerprint_matches_projection_for_replay_stability()
    {
        var snapshot = Chain();
        var chain = snapshot.Chains[0];
        var expected = SensorToShooterProjection.ComputeFingerprint(
            new SensorToShooterSnapshot(new[] { chain }));

        var result = SensorToShooterPresenter.Build("c1", snapshot, eligibilityAvailable: true);
        Assert.That(result.Fingerprint, Is.EqualTo(expected));
        Assert.That(
            SensorToShooterPresenter.Build("c1", snapshot, eligibilityAvailable: true).Fingerprint,
            Is.EqualTo(result.Fingerprint));
    }

    [Test]
    public void Repeated_projection_is_replay_stable()
    {
        var snapshot = Chain(SensorToShooterBreakCause.NoFireControl);
        var first = SensorToShooterPresenter.Build("c1", snapshot, eligibilityAvailable: true);
        var second = SensorToShooterPresenter.Build("c1", snapshot, eligibilityAvailable: true);
        Assert.That(second.Fingerprint, Is.EqualTo(first.Fingerprint));
        Assert.That(second.StatusLine, Is.EqualTo(first.StatusLine));
        Assert.That(second.SensorLine, Is.EqualTo(first.SensorLine));
        Assert.That(second.NextActionLine, Is.EqualTo(first.NextActionLine));
    }

    private static SensorToShooterSnapshot Chain(SensorToShooterBreakCause cause = SensorToShooterBreakCause.None) =>
        new(new[]
        {
            new SensorToShooterChain(
                "c1",
                "target-1",
                "sensor-1",
                cause == SensorToShooterBreakCause.None,
                cause,
                Enum.GetValues<SensorToShooterLinkKind>().Select(kind => new SensorToShooterChainLink(
                    kind,
                    cause == SensorToShooterBreakCause.None,
                    cause,
                    kind == SensorToShooterLinkKind.EligibleShooter ? "shooter-1"
                        : kind == SensorToShooterLinkKind.Track ? "c1"
                        : "sensor-1",
                    "c1",
                    "target-1",
                    "fact")).ToArray()),
        });
}
