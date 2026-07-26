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

    /// <summary>Append contact + magazine rows projected into the Message Log strip.</summary>
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
    }
}
