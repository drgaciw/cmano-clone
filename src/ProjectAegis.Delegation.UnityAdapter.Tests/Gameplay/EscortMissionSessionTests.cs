namespace ProjectAegis.Delegation.UnityAdapter.Tests.Gameplay;

using NUnit.Framework;
using ProjectAegis.Delegation.Gameplay;
using ProjectAegis.Delegation.Watch;
using ProjectAegis.Delegation.UnityAdapter.Gameplay;

[TestFixture]
public sealed class EscortMissionSessionTests
{
    private const double StepSeconds = 1.0 / 60.0;

    [Test]
    public void Starts_in_planning_with_no_seeded_combat_or_movement()
    {
        var session = new EscortMissionSession();
        session.Step(StepSeconds);
        Assert.That(session.SimTime, Is.Zero);
        Assert.That(session.Bridge.Orchestrator.DecisionLog.EngagementOutcomes, Is.Empty);
        Assert.That(session.Presentation.Status, Does.Contain("Planning"));
        Assert.That(session.Presentation.Briefing, Does.Contain("convoy"));
    }

    [Test]
    public void Active_radar_and_delegation_can_protect_convoy_to_exit()
    {
        var session = new EscortMissionSession(42, ShortMission());
        session.Command("alerts-manual");
        session.Command("posture-balanced");
        session.Command("radar-active");
        session.Command("begin");
        Run(session, 30);
        Assert.That(session.IsComplete, Is.True);
        Assert.That(session.Presentation.Succeeded, Is.True);
        Assert.That(session.Presentation.Progress, Is.EqualTo(1).Within(1e-5));
        Assert.That(session.Bridge.Orchestrator.DecisionLog.EngagementOutcomes.Count, Is.GreaterThan(0));
        Assert.That(session.Presentation.Score, Is.GreaterThan(0));
    }

    [Test]
    public void Unprotected_convoy_can_be_destroyed_by_actual_hostile_fire()
    {
        var session = new EscortMissionSession(42, ShortMission() with { ExitDistanceNm = 10 });
        session.Command("alerts-manual");
        session.Command("begin");
        Run(session, 30);
        Assert.That(session.IsComplete, Is.True);
        Assert.That(session.Presentation.Succeeded, Is.False);
        Assert.That(session.Bridge.Orchestrator.DecisionLog.EngagementOutcomes.Any(o => o.VictimTargetId.Value == EscortMissionSession.ConvoyId), Is.True);
    }

    [Test]
    public void Paused_and_terminal_sessions_do_not_advance()
    {
        var session = new EscortMissionSession(42, ShortMission() with { DeadlineSeconds = 1 });
        session.Command("alerts-manual");
        session.Command("begin");
        session.Command("pause");
        session.Step(StepSeconds);
        Assert.That(session.SimTime, Is.Zero);
        session.Command("resume");
        Run(session, 2);
        var terminalTime = session.SimTime;
        var score = session.Presentation.Score;
        Run(session, 2);
        Assert.That(session.SimTime, Is.EqualTo(terminalTime));
        Assert.That(session.Presentation.Score, Is.EqualTo(score));
        Assert.That(session.Presentation.Succeeded, Is.False);
    }

    [Test]
    public void Restart_same_seed_reproduces_outcomes_and_clears_previous_log()
    {
        var session = new EscortMissionSession(42, ShortMission());
        Play(session);
        var first = session.Bridge.Orchestrator.DecisionLog.EngagementOutcomes.Select(o => $"{o.SimTime:R}:{o.OutcomeCode}:{o.PkDraw:R}").ToArray();
        var score = session.Presentation.Score;
        session.Restart();
        Assert.That(session.Bridge.Orchestrator.DecisionLog.EngagementOutcomes, Is.Empty);
        Assert.That(session.SimTime, Is.Zero);
        Play(session);
        Assert.That(session.Bridge.Orchestrator.DecisionLog.EngagementOutcomes.Select(o => $"{o.SimTime:R}:{o.OutcomeCode}:{o.PkDraw:R}"), Is.EqualTo(first));
        Assert.That(session.Presentation.Score, Is.EqualTo(score));
    }

    [Test]
    public void Radar_and_manual_commands_have_visible_authoritative_effects()
    {
        var session = new EscortMissionSession(42, ShortMission());
        session.Command("alerts-manual");
        session.Command("begin");
        Run(session, 0.5);
        Assert.That(session.Snapshot.HasFireControlTrackOnPrimaryContact, Is.False);
        session.Command("radar-active");
        Run(session, 2);
        Assert.That(session.Snapshot.HasFireControlTrackOnPrimaryContact, Is.True);
        Assert.That(session.Command("engage").Accepted, Is.True);
        Run(session, 0.1);
        Assert.That(session.Bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Not.Empty);
        Assert.That(session.Bridge.Orchestrator.DecisionLog.EngagementOutcomes, Is.Not.Empty);
    }

    [Test]
    public void Definition_rejects_invalid_values_and_unknown_commands_do_not_mutate_state()
    {
        Assert.Throws<ArgumentException>((Action)(() => new EscortMissionSession(42, ShortMission() with { ConvoySpeedKnots = double.NaN })));
        var session = new EscortMissionSession();
        Assert.That(session.Command("unknown").Accepted, Is.False);
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => session.Step(double.NaN)));
        Assert.That(session.SimTime, Is.Zero);
    }

    [Test]
    public void Enemy_track_never_becomes_a_blue_track_or_hidden_map_symbol()
    {
        var session = new EscortMissionSession(42, ShortMission());
        Assert.That(session.MapSymbols.Select(s => s.SymbolId), Is.EquivalentTo(new[] { EscortMissionSession.EscortId, EscortMissionSession.ConvoyId }));
        Assert.That(session.Snapshot.TryGetKinematicPose(EscortMissionSession.ThreatId, out _), Is.False);
        session.Command("alerts-manual");
        session.Command("begin");
        Run(session, 5);
        Assert.That(session.Bridge.Orchestrator.DecisionLog.EngagementOutcomes.Any(o => o.ShooterTargetId.Value == EscortMissionSession.ThreatId), Is.True);
        Assert.That(session.Snapshot.HasFireControlTrackOnPrimaryContact, Is.False);
        Assert.That(session.MapSymbols.Any(s => s.SymbolId == EscortMissionSession.ThreatContactId), Is.False);
    }

    [Test]
    public void Reserve_doctrine_limits_actual_ammunition_consumption()
    {
        var definition = ShortMission() with
        {
            FriendlyPk = 0,
            DeadlineSeconds = 100,
            EnemyVisualDetectionSeconds = 100,
            RadarExposureSeconds = 100,
        };
        var cautious = new EscortMissionSession(42, definition);
        var assertive = new EscortMissionSession(42, definition);
        foreach (var mission in new[] { cautious, assertive })
        {
            mission.Command("alerts-manual");
            mission.Command("radar-active");
            mission.Command("begin");
        }
        cautious.Command("posture-cautious");
        assertive.Command("posture-assertive");
        Run(cautious, 5);
        Run(assertive, 5);
        Assert.That(cautious.Presentation.RoundsRemaining, Is.EqualTo(definition.CautiousReserveRounds));
        Assert.That(assertive.Presentation.RoundsRemaining, Is.Zero);
        Assert.That(cautious.Presentation.Score, Is.GreaterThan(assertive.Presentation.Score));
    }

    [Test]
    public void New_contact_auto_pause_finishes_its_atomic_step_without_clock_drift()
    {
        var session = new EscortMissionSession(42, ShortMission());
        session.Command("radar-active");
        session.Command("begin");
        session.Step(StepSeconds);
        Assert.That(session.IsPaused, Is.True);
        Assert.That(session.SimTime, Is.EqualTo(session.Bridge.Session!.Sim.Clock.SimTime).Within(1e-10));
        Assert.That(session.Command("resume").Accepted, Is.False);
        Assert.That(session.Command("acknowledge-alerts").Accepted, Is.True);
        Assert.That(session.IsPaused, Is.True);
        Assert.That(session.Command("resume").Accepted, Is.True);
        session.Step(StepSeconds);
        Assert.That(session.SimTime, Is.EqualTo(2 * StepSeconds).Within(1e-10));
    }

    [Test]
    public void Held_convoy_does_not_move_but_deadline_and_threat_continue()
    {
        var session = new EscortMissionSession(42, ShortMission() with { DeadlineSeconds = 1 });
        session.Command("alerts-manual");
        session.Command("hold");
        session.Command("begin");
        Run(session, 2);
        Assert.That(session.Presentation.Progress, Is.Zero);
        Assert.That(session.IsComplete, Is.True);
        Assert.That(session.Presentation.Status, Does.Contain("Deadline"));
    }

    [TestCase(1)]
    [TestCase(42)]
    [TestCase(123)]
    [TestCase(789)]
    public void Default_data_escort_plan_completes_across_seeds(int seed)
    {
        var session = new EscortMissionSession(seed);
        session.Command("alerts-manual");
        session.Command("posture-balanced");
        session.Command("radar-active");
        session.Command("begin");
        Run(session, 1200);
        Assert.That(session.IsComplete, Is.True);
        Assert.That(session.Presentation.Succeeded, Is.True, $"seed {seed}: {session.Presentation.Debrief}");
        Assert.That(session.SimTime, Is.EqualTo(session.Bridge.Session!.Sim.Clock.SimTime).Within(1e-8));
        Assert.That(session.Presentation.Moments.Count, Is.LessThanOrEqualTo(3));
    }

    [Test]
    public void Identified_identity_persists_when_passive_fire_control_track_is_still_fresh()
    {
        var session = new EscortMissionSession(42, ShortMission());
        session.Command("alerts-manual");
        session.Command("radar-active");
        session.Command("begin");
        Run(session, 1);
        Assert.That(session.Presentation.ContactState, Is.EqualTo("Identified"));
        session.Command("radar-passive");
        Run(session, 0.2);
        Assert.That(session.Presentation.ContactState, Is.EqualTo("Identified"));
        Assert.That(session.Snapshot.HasFireControlTrackOnPrimaryContact, Is.True);
        Assert.That(session.Command("engage").Accepted, Is.True);
        Run(session, 0.1);
        Assert.That(session.Bridge.Orchestrator.DecisionLog.EngagementOutcomes.Any(o => o.ShooterTargetId.Value == EscortMissionSession.EscortId), Is.True);
    }

    [Test]
    public void Unknown_attacker_identity_is_not_revealed_by_player_debrief()
    {
        var session = new EscortMissionSession(42, ShortMission());
        session.Command("alerts-manual");
        session.Command("begin");
        Run(session, 5);
        Assert.That(session.Presentation.ContactState, Is.EqualTo("Unknown"));
        Assert.That(session.Presentation.Moments.Any(m => m.Detail.Contains("unknown")), Is.True);
        Assert.That(session.Presentation.Moments.Any(m => m.Detail.Contains(EscortMissionSession.ThreatId)), Is.False);
        Assert.That(session.Presentation.Debrief.Contains(EscortMissionSession.ThreatId), Is.False);
    }

    [Test]
    public void Identification_log_precedes_first_weapon_release()
    {
        var session = new EscortMissionSession(42, ShortMission());
        session.Command("alerts-manual");
        session.Command("posture-balanced");
        session.Command("radar-active");
        session.Command("begin");
        Run(session, 2);
        var log = session.Bridge.Orchestrator.DecisionLog;
        var identified = log.ContactChanges.First(c => c.NewState == "Identified");
        var fired = log.EngagementOutcomes.First(o => o.ShooterTargetId.Value == EscortMissionSession.EscortId);
        Assert.That(identified.SequenceId, Is.LessThan(fired.SequenceId));
    }

    [Test]
    public void Mission_definition_rejects_off_map_exit_and_threat_positions()
    {
        Assert.Throws<ArgumentException>((Action)(() => new EscortMissionSession(42, ShortMission() with { ExitDistanceNm = 11 })));
        Assert.Throws<ArgumentException>((Action)(() => new EscortMissionSession(42, ShortMission() with { EnemyStartXNm = -1 })));
        var session = new EscortMissionSession();
        Assert.That(session.ExitNormalizedX, Is.EqualTo(0.5).Within(1e-10));
    }

    [Test]
    public void Player_alerts_group_related_contacts_and_acknowledgement_clears_attention()
    {
        var session = new EscortMissionSession();
        var queue = session.Bridge.Session!.WatchQueue;
        queue.Enqueue(new WatchAttentionEvent("related-a", WatchAttentionKind.HostileOrUnknownContact,
            WatchAttentionPriority.High, 0, "contact-a", "raid-1"));
        queue.Enqueue(new WatchAttentionEvent("related-b", WatchAttentionKind.HostileOrUnknownContact,
            WatchAttentionPriority.High, 0, "contact-b", "raid-1"));
        Assert.That(session.Presentation.Alerts.Count, Is.EqualTo(1));
        Assert.That(session.Presentation.Alerts[0], Does.Contain("2 related alerts"));
        Assert.That(session.Command("acknowledge-alerts").Accepted, Is.True);
        Assert.That(session.Presentation.Alerts, Is.Empty);
    }

    private static EscortMissionDefinition ShortMission() => EscortMissionDefinition.LoadDefault() with
    {
        ExitDistanceNm = 0.1,
        ConvoySpeedKnots = 18,
        DeadlineSeconds = 30,
        ActiveTrackSeconds = 0.1,
        PassiveTrackSeconds = 20,
        EnemyVisualDetectionSeconds = 2,
        EnemyReactionSeconds = 4,
        AgentReactionSeconds = 0.25,
        FriendlyPk = 1,
        EnemyPk = 1,
    };

    private static void Play(EscortMissionSession session)
    {
        session.Command("alerts-manual");
        session.Command("posture-balanced");
        session.Command("radar-active");
        session.Command("begin");
        Run(session, 30);
    }

    private static void Run(EscortMissionSession session, double seconds)
    {
        for (var i = 0; i < (int)Math.Ceiling(seconds / StepSeconds); i++) session.Step(StepSeconds);
    }
}
