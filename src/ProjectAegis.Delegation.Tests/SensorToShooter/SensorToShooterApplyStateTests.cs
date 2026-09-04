using ProjectAegis.Delegation.SensorToShooter;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.SensorToShooter;

[TestFixture]
public sealed class SensorToShooterApplyStateTests
{
    [Test]
    public void Apply_null_snapshot_and_contact_returns_empty_presentation()
    {
        var applied = SensorToShooterApplyState.Apply(null, null);

        Assert.That(applied, Is.EqualTo(SensorToShooterPresentation.Empty));
        Assert.That(applied.ContactIdLine, Is.EqualTo("CONTACT: —"));
        Assert.That(applied.CompleteLine, Is.EqualTo("CHAIN: —"));
        Assert.That(applied.PrimaryCauseLine, Is.EqualTo("CAUSE: —"));
        Assert.That(applied.SensorLinkLine, Is.EqualTo("SENSOR: —"));
        Assert.That(applied.TrackLinkLine, Is.EqualTo("TRACK: —"));
        Assert.That(applied.TargetabilityLinkLine, Is.EqualTo("TARGETABILITY: —"));
        Assert.That(applied.ShooterLinkLine, Is.EqualTo("SHOOTER: —"));
        Assert.That(applied.ExplainLinkLine, Is.EqualTo("EXPLAIN: —"));
    }

    [Test]
    public void Apply_complete_chain_maps_contact_complete_linked_lines_and_explain()
    {
        var snapshot = new SensorToShooterSnapshot(new[] { CompleteChain("c1") });

        var applied = SensorToShooterApplyState.Apply(snapshot, "c1");

        Assert.That(applied.ContactIdLine, Is.EqualTo("CONTACT: c1"));
        Assert.That(applied.CompleteLine, Is.EqualTo("CHAIN: COMPLETE"));
        Assert.That(applied.PrimaryCauseLine, Is.EqualTo("CAUSE: —"));
        Assert.That(applied.SensorLinkLine, Is.EqualTo("SENSOR: LINKED u1"));
        Assert.That(applied.TrackLinkLine, Is.EqualTo("TRACK: LINKED c1"));
        Assert.That(applied.TargetabilityLinkLine, Is.EqualTo("TARGETABILITY: LINKED c1"));
        Assert.That(applied.ShooterLinkLine, Is.EqualTo("SHOOTER: LINKED u1"));
        Assert.That(applied.ExplainLinkLine, Is.EqualTo("EXPLAIN: engage/c1"));
        Assert.That(applied.SensorLinkLine, Does.Contain("LINKED"));
        Assert.That(applied.TrackLinkLine, Does.Contain("LINKED"));
        Assert.That(applied.TargetabilityLinkLine, Does.Contain("LINKED"));
        Assert.That(applied.ShooterLinkLine, Does.Contain("LINKED"));
    }

    [Test]
    public void Apply_broken_stale_track_maps_chain_broken_and_stale_cause()
    {
        var snapshot = new SensorToShooterSnapshot(new[] { StaleTrackChain("c1") });

        var applied = SensorToShooterApplyState.Apply(snapshot, "c1");

        Assert.That(applied.ContactIdLine, Is.EqualTo("CONTACT: c1"));
        Assert.That(applied.CompleteLine, Is.EqualTo("CHAIN: BROKEN"));
        Assert.That(applied.PrimaryCauseLine, Does.Contain("stale track"));
        Assert.That(applied.TrackLinkLine, Does.Contain("BROKEN"));
        Assert.That(applied.TrackLinkLine, Does.Contain("stale track"));
        Assert.That(applied.ExplainLinkLine, Is.EqualTo("EXPLAIN: engage/c1"));
    }

    [Test]
    public void Apply_contact_not_in_snapshot_returns_empty()
    {
        var snapshot = new SensorToShooterSnapshot(new[] { CompleteChain("c1") });

        var applied = SensorToShooterApplyState.Apply(snapshot, "missing");

        Assert.That(applied, Is.EqualTo(SensorToShooterPresentation.Empty));
    }

    [Test]
    public void Apply_presentation_lines_contain_no_hex_color()
    {
        var snapshot = new SensorToShooterSnapshot(new[] { StaleTrackChain("c-qos") });

        var applied = SensorToShooterApplyState.Apply(snapshot, "c-qos");

        foreach (var line in AllLines(applied))
        {
            Assert.That(line, Does.Not.Match("#[0-9A-Fa-f]{3,8}"));
        }
    }

    [Test]
    public void Apply_explain_deep_link_is_exactly_engage_contact_id()
    {
        var snapshot = new SensorToShooterSnapshot(new[] { CompleteChain("hostile-track") });

        var applied = SensorToShooterApplyState.Apply(snapshot, "hostile-track");

        Assert.That(applied.ExplainLinkLine, Is.EqualTo("EXPLAIN: engage/hostile-track"));
    }

    private static SensorToShooterChain CompleteChain(string contactId)
    {
        var links = new[]
        {
            Linked(SensorToShooterLinkKind.Sensor, "u1", contactId),
            Linked(SensorToShooterLinkKind.Track, contactId, contactId),
            Linked(SensorToShooterLinkKind.Targetability, contactId, contactId),
            Linked(SensorToShooterLinkKind.EligibleShooter, "u1", contactId),
        };
        return new SensorToShooterChain(
            contactId,
            "hostile-1",
            "u1",
            IsComplete: true,
            PrimaryBreakCause: SensorToShooterBreakCause.None,
            links);
    }

    private static SensorToShooterChain StaleTrackChain(string contactId)
    {
        var links = new[]
        {
            Linked(SensorToShooterLinkKind.Sensor, "u1", contactId),
            Broken(SensorToShooterLinkKind.Track, SensorToShooterBreakCause.StaleTrack, contactId, contactId),
            Broken(SensorToShooterLinkKind.Targetability, SensorToShooterBreakCause.StaleTrack, contactId, contactId),
            Broken(SensorToShooterLinkKind.EligibleShooter, SensorToShooterBreakCause.StaleTrack, null, contactId),
        };
        return new SensorToShooterChain(
            contactId,
            "hostile-1",
            "u1",
            IsComplete: false,
            PrimaryBreakCause: SensorToShooterBreakCause.StaleTrack,
            links);
    }

    private static SensorToShooterChainLink Linked(
        SensorToShooterLinkKind kind,
        string? unitId,
        string contactId) =>
        new(
            kind,
            IsLinked: true,
            BreakCause: SensorToShooterBreakCause.None,
            UnitId: unitId,
            ContactId: contactId,
            TargetId: "hostile-1",
            Detail: null);

    private static SensorToShooterChainLink Broken(
        SensorToShooterLinkKind kind,
        SensorToShooterBreakCause cause,
        string? unitId,
        string contactId) =>
        new(
            kind,
            IsLinked: false,
            BreakCause: cause,
            UnitId: unitId,
            ContactId: contactId,
            TargetId: "hostile-1",
            Detail: SensorToShooterBreakCauseLabels.Format(cause));

    private static IEnumerable<string> AllLines(SensorToShooterPresentation applied)
    {
        yield return applied.ContactIdLine;
        yield return applied.CompleteLine;
        yield return applied.PrimaryCauseLine;
        yield return applied.SensorLinkLine;
        yield return applied.TrackLinkLine;
        yield return applied.TargetabilityLinkLine;
        yield return applied.ShooterLinkLine;
        yield return applied.ExplainLinkLine;
    }
}
