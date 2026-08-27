namespace ProjectAegis.Delegation.AfterAction;

/// <summary>
/// Consume-only mirror of DRG-211 <c>CombatEventPhase</c> from CombatEvents/ (#583).
/// Field-for-field compatible so callers can map <c>CombatEvent</c> rows without reconstruction.
/// </summary>
public enum CombatEventPhaseConsume
{
    IntentAccepted = 1,
    Authorized = 2,
    AuthorizationRefused = 3,
    Firing = 4,
    InFlight = 5,
    TerminalOutcome = 6,
}

/// <summary>
/// Consume-only mirror of DRG-211 <c>CombatEvent</c>. Presentation facts only — no UI-derived truth.
/// </summary>
public sealed record CombatEventRowConsume(
    CombatEventPhaseConsume Phase,
    string ShooterId,
    string TargetId,
    string WeaponFamilyId,
    string Outcome,
    ulong CorrelationId,
    double SimTime,
    ulong SimTick,
    string ExplanationRef);

/// <summary>Consume-only mirror of DRG-211 <c>CombatEventSnapshot</c>.</summary>
public sealed record CombatEventSnapshotConsume(IReadOnlyList<CombatEventRowConsume> Events)
{
    /// <summary>Empty combat-event picture.</summary>
    public static CombatEventSnapshotConsume Empty { get; } =
        new(Array.Empty<CombatEventRowConsume>());
}
