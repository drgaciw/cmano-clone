namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Traits;

/// <summary>
/// Shared smoke-ORBAT seed for play-mode C2 panels (OOB, Unit Detail, Message Log).
/// Called by Unity <c>SimplePlayModeSimHost</c> and headless projection gates — single shipped entry.
/// </summary>
public static class PlayModeSmokeOrbatSeeder
{
    public const string FriendlyUnitId = "u1";
    public const string HostileUnitId = "hostile-1";
    public const string ContactId = "c1";

    /// <summary>
    /// Registers friendly/hostile units, configures Mixed mode, and seeds contact/magazine log rows.
    /// Idempotent when the registry already has members.
    /// Returns true when the registry is ready (seeded now or already populated); false if bridge is null.
    /// </summary>
    public static bool TrySeed(DelegationBridge? bridge)
    {
        if (bridge == null)
        {
            return false;
        }

        if (bridge.Registry.CollectMemberIds().Count > 0)
        {
            return true;
        }

        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), FriendlyUnitId);
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), HostileUnitId);

        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            friendly: new[] { friendly.Target },
            opposing: new[] { opposing.Target },
            defaultTraits: PersonalityCatalog.All[0].Traits);

        SeedDecisionLog(bridge.Orchestrator.DecisionLog);
        return true;
    }

    /// <summary>
    /// Append contact / magazine plus high-value player-facing rows (policy, mission, event,
    /// agent decision, damage) so Play Mode message log is richer than three seeded lines.
    /// Idempotent when contact or magazine rows already exist.
    /// </summary>
    public static void SeedDecisionLog(DecisionLog? log)
    {
        if (log == null)
        {
            return;
        }

        if (log.ContactChanges.Count > 0 || log.MagazineChanges.Count > 0)
        {
            return;
        }

        log.AppendContactChange(new ContactChangeRecord(
            SequenceId: 0,
            SimTime: 0.0,
            SimTick: 0,
            ObserverId: FriendlyUnitId,
            ContactId: ContactId,
            TargetId: HostileUnitId,
            PreviousState: "Unknown",
            NewState: "Detected"));

        log.AppendContactChange(new ContactChangeRecord(
            SequenceId: 0,
            SimTime: 1.0,
            SimTick: 1,
            ObserverId: FriendlyUnitId,
            ContactId: ContactId,
            TargetId: HostileUnitId,
            PreviousState: "Detected",
            NewState: "Classified"));

        log.AppendMagazineChange(new MagazineChangeRecord(
            SequenceId: 0,
            SimTime: 2.0,
            SimTick: 2,
            ShooterTargetId: new TargetId(FriendlyUnitId),
            MountId: 0,
            Delta: -1,
            ReasonCode: "fire"));

        log.AppendPolicyUpdate(new PolicyUpdateRecord(
            SequenceId: 0,
            SimTime: 2.2,
            SimTick: 3,
            PolicySnapshotId: 1,
            Field: "roe",
            PreviousValue: "WeaponsTight",
            NewValue: "WeaponsFree"));

        log.AppendMissionTransition(new MissionTransitionRecord(
            SequenceId: 0,
            SimTime: 2.3,
            SimTick: 4,
            EventId: "patrol-1",
            PhaseCode: "START"));

        log.AppendEventFired(new EventFiredRecord(
            SequenceId: 0,
            SimTime: 2.4,
            SimTick: 5,
            EventId: "recon-detect",
            EventCode: "DETECTED"));

        log.Append(new DecisionRecord(
            SimTime: 2.5,
            AgentId: new AgentId("a1"),
            TargetId: new TargetId(FriendlyUnitId),
            AutonomyLevel: AutonomyLevel.Assisted,
            ChosenKind: OrderKind.Hold,
            Alternatives: Array.Empty<ScoredIntent>(),
            Rationale: "patrol station-keeping",
            AttentionLoad: 1,
            AttentionBudget: 20,
            RngDraw: 0.1,
            SimTick: 6));

        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            SequenceId: 0,
            SimTime: 2.6,
            SimTick: 7,
            UnitId: new TargetId(HostileUnitId),
            PreviousHpPct: 100,
            NewHpPct: 85,
            ReasonCode: "Hit",
            DamageLevel: 1));
    }

    /// <summary>
    /// Time-gated Play Mode feed: append additional player-facing rows as sim time advances.
    /// Does not call <c>DelegationBridge.Tick</c>. Idempotent per threshold.
    /// </summary>
    public static void AdvanceDecisionLog(DecisionLog? log, double simTime)
    {
        if (log == null)
        {
            return;
        }

        if (simTime >= 5.0 && !HasPolicyField(log, "emcon"))
        {
            log.AppendPolicyUpdate(new PolicyUpdateRecord(
                SequenceId: 0,
                SimTime: 5.0,
                SimTick: 8,
                PolicySnapshotId: 2,
                Field: "emcon",
                PreviousValue: "Active",
                NewValue: "Passive"));
        }

        if (simTime >= 8.0 && !HasMissionPhase(log, "ON_STATION"))
        {
            log.AppendMissionTransition(new MissionTransitionRecord(
                SequenceId: 0,
                SimTime: 8.0,
                SimTick: 9,
                EventId: "patrol-1",
                PhaseCode: "ON_STATION"));
        }

        if (simTime >= 12.0 && !HasEvent(log, "cue-1"))
        {
            log.AppendEventFired(new EventFiredRecord(
                SequenceId: 0,
                SimTime: 12.0,
                SimTick: 10,
                EventId: "cue-1",
                EventCode: "CLASSIFIED"));
        }

        if (simTime >= 15.0 && !HasChosenKind(log, OrderKind.Engage))
        {
            log.Append(new DecisionRecord(
                SimTime: 15.0,
                AgentId: new AgentId("a1"),
                TargetId: new TargetId(FriendlyUnitId),
                AutonomyLevel: AutonomyLevel.Assisted,
                ChosenKind: OrderKind.Engage,
                Alternatives: Array.Empty<ScoredIntent>(),
                Rationale: "classified hostile in envelope",
                AttentionLoad: 4,
                AttentionBudget: 20,
                RngDraw: 0.2,
                SimTick: 11));
        }

        if (simTime >= 20.0 && !HasDamageAtOrBelow(log, 70))
        {
            log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
                SequenceId: 0,
                SimTime: 20.0,
                SimTick: 12,
                UnitId: new TargetId(HostileUnitId),
                PreviousHpPct: 85,
                NewHpPct: 70,
                ReasonCode: "Hit",
                DamageLevel: 2));
        }

        if (simTime >= 30.0 && !HasPolicyField(log, "maxSalvo"))
        {
            log.AppendPolicyUpdate(new PolicyUpdateRecord(
                SequenceId: 0,
                SimTime: 30.0,
                SimTick: 13,
                PolicySnapshotId: 3,
                Field: "maxSalvo",
                PreviousValue: "4",
                NewValue: "2"));
        }
    }

    private static bool HasPolicyField(DecisionLog log, string field) =>
        log.PolicyUpdates.Any(u => string.Equals(u.Field, field, StringComparison.Ordinal));

    private static bool HasMissionPhase(DecisionLog log, string phaseCode) =>
        log.MissionTransitions.Any(m => string.Equals(m.PhaseCode, phaseCode, StringComparison.Ordinal));

    private static bool HasEvent(DecisionLog log, string eventId) =>
        log.EventFired.Any(e => string.Equals(e.EventId, eventId, StringComparison.Ordinal));

    private static bool HasChosenKind(DecisionLog log, OrderKind kind) =>
        log.Records.Any(r => r.ChosenKind == kind);

    private static bool HasDamageAtOrBelow(DecisionLog log, double hpPct) =>
        log.PlatformDamageChanges.Any(d => d.NewHpPct <= hpPct);
}
