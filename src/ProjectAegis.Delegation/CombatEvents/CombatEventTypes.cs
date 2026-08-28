namespace ProjectAegis.Delegation.CombatEvents;

using ProjectAegis.Delegation.Projection;

/// <summary>Replay-stable combat lifecycle phase for Combat UX Slice B (DRG-211).</summary>
public enum CombatEventPhase
{
    IntentAccepted = 1,
    Authorized = 2,
    AuthorizationRefused = 3,
    Firing = 4,
    InFlight = 5,
    TerminalOutcome = 6,
}

/// <summary>
/// Explicit intent / authority / preview facts supplied by the caller.
/// Does not enqueue orders or resolve combat.
/// </summary>
public sealed record CombatEngageAssessInput(
    string ShooterId,
    string TargetId,
    string WeaponFamilyId,
    bool IntentAccepted,
    ulong SimTick,
    double SimTime,
    ulong CorrelationId,
    EngagePreview? Preview = null);

/// <summary>
/// One presentation-facing combat fact row. Sim-clock only — no UI selection, hover, camera, or panel state.
/// </summary>
public sealed record CombatEvent(
    CombatEventPhase Phase,
    string ShooterId,
    string TargetId,
    string WeaponFamilyId,
    string Outcome,
    ulong CorrelationId,
    double SimTime,
    ulong SimTick,
    string ExplanationRef);

/// <summary>Ordered, replay-stable combat-event picture for a single engage-assess leg.</summary>
public sealed record CombatEventSnapshot
{
    public IReadOnlyList<CombatEvent> Events { get; }

    public CombatEventSnapshot(IReadOnlyList<CombatEvent> events)
    {
        Events = events.ToArray();
    }

    public static CombatEventSnapshot Empty { get; } =
        new(Array.Empty<CombatEvent>());
}
