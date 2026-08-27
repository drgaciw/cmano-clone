namespace ProjectAegis.Delegation.EngageExplainContract;

/// <summary>
/// Mirrors <c>CombatEventPhase</c> from DRG-211 (#583) for adapter-free compilation on this SHA.
/// </summary>
public enum EngageExplainCombatEventPhase
{
    IntentAccepted = 1,
    Authorized = 2,
    AuthorizationRefused = 3,
    Firing = 4,
    InFlight = 5,
    TerminalOutcome = 6,
}

/// <summary>
/// Local combat-event fact row mirroring <c>CombatEvent</c> field-for-field (DRG-211 / #583).
/// Presentation-only input — no UI selection, hover, camera, or panel state.
/// </summary>
public sealed record EngageExplainCombatEventInput(
    EngageExplainCombatEventPhase Phase,
    string ShooterId,
    string TargetId,
    string WeaponFamilyId,
    string Outcome,
    ulong CorrelationId,
    double SimTime,
    ulong SimTick,
    string ExplanationRef);

/// <summary>Ordered combat-event facts for one engage-assess leg.</summary>
public sealed record EngageExplainCombatEventSnapshot(IReadOnlyList<EngageExplainCombatEventInput> Events)
{
    public static EngageExplainCombatEventSnapshot Empty { get; } =
        new(Array.Empty<EngageExplainCombatEventInput>());
}
