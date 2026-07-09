using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// Req 20 P0 T6: mission activate/deactivate eligibility (replay blocks). Pure command resolvers
/// over <see cref="MissionListProjection"/> rows — no DelegationBridge.
/// </summary>
[TestFixture]
public sealed class MissionRuntimeCommandResolverTests
{
    private static IReadOnlyList<MissionListEntry> SampleMissions() =>
    [
        new MissionListEntry("start-exec", 0, "MissionTransition", "Execution"),
        new MissionListEntry("contact-window", 1, "EventFired", "contact_window_open"),
    ];

    [Test]
    public void TryActivate_eligible_mission_produces_activate_intent()
    {
        var command = MissionRuntimeCommandResolver.TryActivate(
            "start-exec",
            SampleMissions(),
            isReplay: false,
            isCurrentlyActive: false,
            out var failure);

        Assert.That(failure, Is.Null);
        Assert.That(command, Is.Not.Null);
        Assert.That(command!.IntentKind, Is.EqualTo(MissionRuntimeCommandResolver.ActivateIntentKind));
        Assert.That(command.MissionId, Is.EqualTo("start-exec"));
    }

    [Test]
    public void TryDeactivate_active_mission_produces_deactivate_intent()
    {
        var command = MissionRuntimeCommandResolver.TryDeactivate(
            "start-exec",
            SampleMissions(),
            isReplay: false,
            isCurrentlyActive: true,
            out var failure);

        Assert.That(failure, Is.Null);
        Assert.That(command, Is.Not.Null);
        Assert.That(command!.IntentKind, Is.EqualTo(MissionRuntimeCommandResolver.DeactivateIntentKind));
        Assert.That(command.MissionId, Is.EqualTo("start-exec"));
    }

    [Test]
    public void TryActivate_replay_blocks()
    {
        var command = MissionRuntimeCommandResolver.TryActivate(
            "start-exec",
            SampleMissions(),
            isReplay: true,
            isCurrentlyActive: false,
            out var failure);

        Assert.That(command, Is.Null);
        Assert.That(failure, Is.EqualTo(MissionRuntimeCommandResolver.FailureReplay));
    }

    [Test]
    public void TryDeactivate_replay_blocks()
    {
        var command = MissionRuntimeCommandResolver.TryDeactivate(
            "start-exec",
            SampleMissions(),
            isReplay: true,
            isCurrentlyActive: true,
            out var failure);

        Assert.That(command, Is.Null);
        Assert.That(failure, Is.EqualTo(MissionRuntimeCommandResolver.FailureReplay));
    }

    [Test]
    public void TryActivate_unknown_mission_fails()
    {
        var command = MissionRuntimeCommandResolver.TryActivate(
            "missing",
            SampleMissions(),
            isReplay: false,
            isCurrentlyActive: false,
            out var failure);

        Assert.That(command, Is.Null);
        Assert.That(failure, Is.EqualTo(MissionRuntimeCommandResolver.FailureUnknownMission));
    }

    [Test]
    public void TryActivate_missing_mission_id_fails()
    {
        var command = MissionRuntimeCommandResolver.TryActivate(
            "  ",
            SampleMissions(),
            isReplay: false,
            isCurrentlyActive: false,
            out var failure);

        Assert.That(command, Is.Null);
        Assert.That(failure, Is.EqualTo(MissionRuntimeCommandResolver.FailureMissingMissionId));
    }

    [Test]
    public void TryActivate_already_active_fails()
    {
        var command = MissionRuntimeCommandResolver.TryActivate(
            "start-exec",
            SampleMissions(),
            isReplay: false,
            isCurrentlyActive: true,
            out var failure);

        Assert.That(command, Is.Null);
        Assert.That(failure, Is.EqualTo(MissionRuntimeCommandResolver.FailureAlreadyActive));
    }

    [Test]
    public void TryDeactivate_not_active_fails()
    {
        var command = MissionRuntimeCommandResolver.TryDeactivate(
            "start-exec",
            SampleMissions(),
            isReplay: false,
            isCurrentlyActive: false,
            out var failure);

        Assert.That(command, Is.Null);
        Assert.That(failure, Is.EqualTo(MissionRuntimeCommandResolver.FailureNotActive));
    }

    [Test]
    public void TryActivate_uses_mission_list_projection_event_ids()
    {
        // Integration with MissionListProjection shape: EventId is the mission target key.
        var rows = SampleMissions();
        Assert.That(rows.Select(r => r.EventId).ToArray(), Does.Contain("contact-window"));

        var command = MissionRuntimeCommandResolver.TryActivate(
            "contact-window",
            rows,
            isReplay: false,
            isCurrentlyActive: false,
            out var failure);

        Assert.That(failure, Is.Null);
        Assert.That(command!.MissionId, Is.EqualTo("contact-window"));
    }
}
