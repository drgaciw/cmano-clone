using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class MessageLogNotificationCoordinatorTests
{
    [Test]
    public void Evaluate_processes_each_SequenceId_at_most_once_across_repeated_refreshes()
    {
        var coordinator = new MessageLogNotificationCoordinator();
        var lines = new[]
        {
            new MessageLogLine(10, 5.0, "DAMAGE", "Damage u1: 100% -> 80% (Hit)", "u1"),
            new MessageLogLine(11, 5.0, "MISSION", "Mission patrol-1 -> START"),
        };

        var first = coordinator.Evaluate(lines);
        var second = coordinator.Evaluate(lines);

        Assert.That(first.ShouldPause, Is.True);
        Assert.That(first.ClockAction, Is.EqualTo(ClockStopAction.Pause));
        Assert.That(first.TriggeredSequenceIds, Is.EqualTo(new[] { 10UL }));
        Assert.That(first.NewlyTriggeredAlerts, Has.Count.EqualTo(1));
        Assert.That(first.NewlyTriggeredAlerts[0].SimTime, Is.EqualTo(5.0));
        Assert.That(first.NewlyTriggeredAlerts[0].SubjectId, Is.EqualTo("u1"));
        Assert.That(first.NewlyTriggeredAlerts[0].TriggersDropToRealTime, Is.True);
        Assert.That(coordinator.ActiveBalloons, Has.Count.EqualTo(1));
        Assert.That(coordinator.ActiveBalloons[0].SimTime, Is.EqualTo(5.0));

        Assert.That(second.ShouldPause, Is.False);
        Assert.That(second.ShouldDropToRealTime, Is.False);
        Assert.That(second.ClockAction, Is.EqualTo(ClockStopAction.None));
        Assert.That(second.TriggeredSequenceIds, Is.Empty);
        Assert.That(second.NewlyTriggeredAlerts, Is.Empty);
    }

    [Test]
    public void Evaluate_enforces_Pause_over_DropToRealTime_precedence_in_same_batch()
    {
        var coordinator = new MessageLogNotificationCoordinator();
        var lines = new[]
        {
            new MessageLogLine(20, 6.0, "WEAPON_LAUNCH", "Unit u1 launched engagement 1", "u1"),
            new MessageLogLine(21, 6.0, "DAMAGE", "Damage u1: 80% -> 60% (Hit)", "u1"),
        };

        var eval = coordinator.Evaluate(lines);

        Assert.That(eval.ClockAction, Is.EqualTo(ClockStopAction.Pause));
        Assert.That(eval.ShouldPause, Is.True);
        Assert.That(eval.ShouldDropToRealTime, Is.True);
        Assert.That(eval.TriggeredSequenceIds, Is.EqualTo(new[] { 20UL, 21UL }));
        Assert.That(coordinator.HasUnresolvedPause, Is.True);
    }

    [Test]
    public void TryDismissAlert_for_one_alert_does_not_clear_another_unresolved_pause()
    {
        var coordinator = new MessageLogNotificationCoordinator();
        var lines = new[]
        {
            new MessageLogLine(101, 10.0, "WEAPON_LAUNCH", "Unit u1 launched engagement 1", "u1"),
            new MessageLogLine(102, 10.0, "DAMAGE", "Damage u1: 100% -> 75% (Hit)", "u1"),
            new MessageLogLine(103, 11.0, "CONTACT", "Contact c1 Detected -> Classified", "u1"),
        };

        coordinator.Evaluate(lines);
        Assert.That(coordinator.HasUnresolvedPause, Is.True);
        Assert.That(coordinator.UnresolvedPauseAlerts, Has.Count.EqualTo(2));

        // Dismiss non-pause alert 101: both pause alerts remain unresolved.
        Assert.That(coordinator.TryDismissAlert(101), Is.True);
        Assert.That(coordinator.HasUnresolvedPause, Is.True);
        Assert.That(coordinator.CanResume(explicitOverride: false), Is.False);

        // Dismiss pause alert 102 visually: alert 103 (and 102) keep pause unresolved.
        Assert.That(coordinator.TryDismissAlert(102), Is.True);
        Assert.That(coordinator.HasUnresolvedPause, Is.True);
        Assert.That(coordinator.CanResume(explicitOverride: false), Is.False);

        // Acknowledge 102 only: 103 is STILL unresolved.
        Assert.That(coordinator.TryAcknowledgeAlert(102), Is.True);
        Assert.That(coordinator.HasUnresolvedPause, Is.True);
        Assert.That(coordinator.CanResume(explicitOverride: false), Is.False);

        // Acknowledge 103: now all pause alerts are resolved.
        Assert.That(coordinator.TryAcknowledgeAlert(103), Is.True);
        Assert.That(coordinator.HasUnresolvedPause, Is.False);
        Assert.That(coordinator.CanResume(explicitOverride: false), Is.True);
    }

    [Test]
    public void Routine_coalescing_retains_counts_and_sequence_ids_without_suppressing_pause_alerts()
    {
        var coordinator = new MessageLogNotificationCoordinator();
        var lines = new[]
        {
            new MessageLogLine(1, 1.0, "MISSION", "Mission patrol-1 -> START"),
            new MessageLogLine(2, 2.0, "MISSION", "Mission patrol-1 -> ON_STATION"),
            new MessageLogLine(3, 2.0, "DAMAGE", "Damage u1: 100% -> 70% (Hit)", "u1"),
        };

        var eval = coordinator.Evaluate(lines);

        Assert.That(eval.ShouldPause, Is.True);
        Assert.That(eval.CoalescedRoutines, Is.Not.Empty);
        Assert.That(coordinator.HasUnresolvedPause, Is.True);
        var missionSummary = coordinator.CoalescedSummaries.Single(s => s.Category == "MISSION");
        Assert.That(missionSummary.Count, Is.EqualTo(2));
        Assert.That(missionSummary.SequenceIds, Is.EqualTo(new[] { 1UL, 2UL }));
        Assert.That(missionSummary.LatestSequenceId, Is.EqualTo(2UL));
        Assert.That(missionSummary.LatestText, Does.Contain("ON_STATION"));
    }

    [Test]
    public void Evaluate_under_AttachReplayViewer_suppresses_pause_and_1x_commands_with_zero_mutations()
    {
        var coordinator = new MessageLogNotificationCoordinator();
        var lines = new[]
        {
            new MessageLogLine(55, 8.0, "DAMAGE", "Damage u1: 100% -> 50% (Hit)", "u1"),
            new MessageLogLine(56, 8.0, "COMMS", "Comms n1: Nominal -> Denied (jam)", "n1"),
        };

        var eval = coordinator.Evaluate(lines, isReplayViewerAttached: true);

        Assert.That(eval.ReplaySuppressed, Is.True);
        Assert.That(eval.ClockAction, Is.EqualTo(ClockStopAction.None));
        Assert.That(eval.ShouldPause, Is.False);
        Assert.That(eval.ShouldDropToRealTime, Is.False);
        Assert.That(coordinator.HasUnresolvedPause, Is.False);
        Assert.That(coordinator.ActiveAlerts, Is.Empty);
        Assert.That(coordinator.ActiveBalloons, Is.Empty);
    }

    [TestCase(PlayerInfoModel.DelegationFog)]
    [TestCase(PlayerInfoModel.TieredByAutonomy)]
    public void Evaluate_consuming_ProjectLive_never_leaks_fogged_FullAutonomous_decisions(PlayerInfoModel model)
    {
        var policy = MessageCategoryPolicy.LoadDefault()
            .WithRule("AGENT_DECISION", new MessageCategoryRule(Log: true, Pause: true, MapBalloon: true, DropToRealTime: true));
        var coordinator = new MessageLogNotificationCoordinator(policy);

        var log = new DecisionLog();
        log.Append(new DecisionRecord(
            1.0,
            new AgentId("fogged-full-agent"),
            new TargetId("u1"),
            AutonomyLevel.FullAutonomous,
            OrderKind.Engage,
            Array.Empty<ScoredIntent>(),
            "hidden decision",
            1,
            10,
            0.5,
            SimTick: 1));

        var liveLines = MessageLogProjection.ProjectLive(log, model);
        var eval = coordinator.Evaluate(liveLines);

        Assert.That(liveLines, Is.Empty);
        Assert.That(eval.ShouldPause, Is.False);
        Assert.That(eval.ShouldDropToRealTime, Is.False);
        Assert.That(coordinator.ActiveBalloons, Is.Empty);
    }

    [Test]
    public void PanelBinder_with_policy_Log_false_omits_row_while_coordinator_retains_actions()
    {
        var policy = MessageCategoryPolicy.LoadDefault()
            .WithRule("DAMAGE", new MessageCategoryRule(Log: false, Pause: true, MapBalloon: true, DropToRealTime: true));
        var coordinator = new MessageLogNotificationCoordinator(policy);
        var lines = new[]
        {
            new MessageLogLine(1, 1.0, "DAMAGE", "Damage u1: 100% -> 50% (Hit)", "u1"),
            new MessageLogLine(2, 1.0, "MISSION", "Mission patrol-1 -> START"),
        };

        var eval = coordinator.Evaluate(lines);
        var panel = MessageLogPanelBinder.Bind(lines, policy, MessageLogBindingSource.LiveOrderLogView);

        Assert.That(eval.ShouldPause, Is.True);
        Assert.That(coordinator.ActiveBalloons, Has.Count.EqualTo(1));
        Assert.That(panel.Rows, Has.Count.EqualTo(1));
        Assert.That(panel.Rows[0].Category, Is.EqualTo("MISSION"));
    }

    [Test]
    public void Evaluate_does_not_deduplicate_SequenceId_zero_lines_for_different_units_or_actors()
    {
        var coordinator = new MessageLogNotificationCoordinator();
        var lines = new[]
        {
            new MessageLogLine(0, 12.5, "DAMAGE", "Hull hit", UnitId: "u1", ActorId: "actor-1"),
            new MessageLogLine(0, 12.5, "DAMAGE", "Hull hit", UnitId: "u2", ActorId: "actor-1"),
            new MessageLogLine(0, 12.5, "DAMAGE", "Hull hit", UnitId: "u2", ActorId: "actor-2"),
            new MessageLogLine(0, 12.5, "DAMAGE", "Hull hit", UnitId: "u2", ActorId: "actor-2"),
        };

        var eval = coordinator.Evaluate(lines);

        Assert.That(eval.NewlyTriggeredAlerts, Has.Count.EqualTo(3));
        Assert.That(coordinator.ActiveAlerts, Has.Count.EqualTo(3));
        Assert.That(coordinator.ActiveBalloons, Has.Count.EqualTo(3));
    }

    [Test]
    public void Evaluate_custom_pause_category_before_DAMAGE_or_CONTACT_does_not_mask_PrimaryPauseReason()
    {
        var policy = MessageCategoryPolicy.LoadDefault()
            .WithRule("EVENT", new MessageCategoryRule(Log: true, Pause: true, MapBalloon: false, DropToRealTime: false));

        var damageCoordinator = new MessageLogNotificationCoordinator(policy);
        var damageEval = damageCoordinator.Evaluate(new[]
        {
            new MessageLogLine(1, 5.0, "EVENT", "Custom pause event"),
            new MessageLogLine(2, 5.0, "DAMAGE", "Damage u1: 100% -> 80% (Hit)", "u1"),
        });

        Assert.That(damageEval.ShouldPause, Is.True);
        Assert.That(damageEval.PrimaryPauseReason, Is.EqualTo(ProjectAegis.Delegation.Watch.WatchPauseReason.OwnSideLossOrDamage));

        var contactCoordinator = new MessageLogNotificationCoordinator(policy);
        var contactEval = contactCoordinator.Evaluate(new[]
        {
            new MessageLogLine(3, 6.0, "EVENT", "Custom pause event"),
            new MessageLogLine(4, 6.0, "CONTACT", "Contact c1 Detected -> Classified", "u1"),
        });

        Assert.That(contactEval.ShouldPause, Is.True);
        Assert.That(contactEval.PrimaryPauseReason, Is.EqualTo(ProjectAegis.Delegation.Watch.WatchPauseReason.HostileOrUnknownContact));
    }

    [Test]
    public void AlertBannerProjection_prioritizes_unresolved_pauses_then_latest_active_non_pause_balloon_and_gates_replay()
    {
        var coordinator = new MessageLogNotificationCoordinator();
        coordinator.Evaluate(new[]
        {
            new MessageLogLine(101, 10.0, "WEAPON_LAUNCH", "Unit u1 launched engagement 1", "u1"),
            new MessageLogLine(102, 10.0, "HIT", "Engagement 1 hit hostile-1", "u1"),
            new MessageLogLine(103, 11.0, "DAMAGE", "Damage u1: 100% -> 75% (Hit)", "u1"),
        });

        // While unresolved pause exists, banner prioritizes UnresolvedPauseAlerts.
        var whilePaused = MessageLogAlertBannerProjection.Project(
            coordinator.UnresolvedPauseAlerts,
            coordinator.ActiveAlerts,
            isReplayViewerAttached: false);
        Assert.That(whilePaused, Has.Count.EqualTo(1));
        Assert.That(whilePaused[0].SequenceId, Is.EqualTo(103UL));
        Assert.That(whilePaused[0].Prefix, Is.Not.Empty);
        Assert.That(whilePaused[0].Category, Is.EqualTo("DAMAGE"));
        Assert.That(whilePaused[0].Text, Does.Contain("Damage u1"));
        Assert.That(whilePaused[0].IsPauseAlert, Is.True);
        Assert.That(whilePaused[0].IsDismissed, Is.False);
        Assert.That(whilePaused[0].IsAcknowledged, Is.False);
        Assert.That(whilePaused[0].ShowAck, Is.True);
        Assert.That(whilePaused[0].CanAck, Is.True);
        Assert.That(whilePaused[0].CanDismiss, Is.True);

        // Dismissing non-pause balloon 101 removes only 101 and never clears pause 103.
        Assert.That(coordinator.TryDismissAlert(101), Is.True);
        Assert.That(coordinator.HasUnresolvedPause, Is.True);
        Assert.That(coordinator.ActiveBalloons.Select(b => b.SequenceId), Is.EqualTo(new[] { 102UL, 103UL }));

        // Dismiss pause 103 visually, then acknowledge it; non-pause balloon 102 surfaces with DISMISS.
        Assert.That(coordinator.TryDismissAlert(103), Is.True);
        Assert.That(coordinator.HasUnresolvedPause, Is.True);
        Assert.That(coordinator.TryAcknowledgeAlert(103), Is.True);
        Assert.That(coordinator.HasUnresolvedPause, Is.False);

        var nonPauseBanner = MessageLogAlertBannerProjection.Project(
            coordinator.UnresolvedPauseAlerts,
            coordinator.ActiveAlerts,
            isReplayViewerAttached: false);
        Assert.That(nonPauseBanner, Has.Count.EqualTo(1));
        Assert.That(nonPauseBanner[0].SequenceId, Is.EqualTo(102UL));
        Assert.That(nonPauseBanner[0].ShowAck, Is.False);
        Assert.That(nonPauseBanner[0].CanDismiss, Is.True);

        // Under AttachReplayViewer, ACK and DISMISS are both disabled.
        var replayBanner = MessageLogAlertBannerProjection.Project(
            coordinator.UnresolvedPauseAlerts,
            coordinator.ActiveAlerts,
            isReplayViewerAttached: true);
        Assert.That(replayBanner, Has.Count.EqualTo(1));
        Assert.That(replayBanner[0].CanAck, Is.False);
        Assert.That(replayBanner[0].CanDismiss, Is.False);

        // Dismissing non-pause balloon 102 clears ActiveBalloons and banner.
        Assert.That(coordinator.TryDismissAlert(102), Is.True);
        Assert.That(coordinator.ActiveBalloons, Is.Empty);
        Assert.That(
            MessageLogAlertBannerProjection.Project(
                coordinator.UnresolvedPauseAlerts,
                coordinator.ActiveAlerts,
                isReplayViewerAttached: false),
            Is.Empty);
    }

    [Test]
    public void Coordinator_source_fence_has_no_UnityEngine_SimulationSession_DelegationBridge_or_DecisionLog_writes()
    {
        var repoRoot = FindRepoRoot();
        var policySource = File.ReadAllText(Path.Combine(repoRoot, "src", "ProjectAegis.Delegation", "Projection", "MessageCategoryPolicy.cs"));
        var coordSource = File.ReadAllText(Path.Combine(repoRoot, "src", "ProjectAegis.Delegation", "Projection", "MessageLogNotificationCoordinator.cs"));

        foreach (var source in new[] { policySource, coordSource })
        {
            Assert.That(source, Does.Not.Contain("UnityEngine"));
            Assert.That(source, Does.Not.Contain("SimulationSession"));
            Assert.That(source, Does.Not.Contain("DelegationBridge"));
            Assert.That(source, Does.Not.Contain("DecisionLog"));
            Assert.That(source, Does.Not.Contain("ApplyOrder"));
            Assert.That(source, Does.Not.Contain("ArgumentNullException.ThrowIfNull"));
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repo root.");
    }
}
