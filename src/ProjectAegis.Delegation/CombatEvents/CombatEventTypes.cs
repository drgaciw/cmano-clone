namespace ProjectAegis.Delegation.CombatEvents;

using Projection;
using Skills;
using TargetabilityAccept;

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

/// <summary>
/// DRG-165: Slice A targetability facts copied from an authoritative
/// <see cref="TargetabilityAcceptContactRow"/>. Never recomputed from sensor, envelope, or datalink overlays.
/// </summary>
public sealed record CombatTargetabilityFact(
    string ContactId,
    string TargetId,
    TargetabilityAcceptDisposition Disposition,
    string WithheldCauseCode,
    ContactProvenanceConfidence Confidence,
    bool? SensorToShooterComplete,
    bool RoeAllowsEngage,
    C2AuthorityDisposition TargetingDisposition,
    string? TargetingReasonCode)
{
    /// <summary>Copies the presentation-relevant Slice A facts from one acceptance row.</summary>
    public static CombatTargetabilityFact FromRow(TargetabilityAcceptContactRow row) =>
        new(
            row.ContactId,
            row.TargetId,
            row.Disposition,
            row.WithheldCauseCode,
            row.Provenance?.Confidence ?? ContactProvenanceConfidence.Unknown,
            row.SensorToShooter?.IsComplete,
            row.Authority.Roe.EngageAllowedByRoe,
            row.Authority.Targeting.Disposition,
            row.Authority.Targeting.ReasonCode);
}

/// <summary>
/// DRG-165: execution-time facts recorded on the engagement row when the simulation processed the shot.
/// <see cref="HasFireControlTrack"/> is null when the row predates the enriched log contract.
/// </summary>
public sealed record CombatExecutionFact(
    ulong CorrelationId,
    string ShooterId,
    string TargetId,
    string WeaponFamilyId,
    bool? HasFireControlTrack,
    int SalvoSize,
    double SimTime,
    ulong SimTick);

/// <summary>Ordered, replay-stable combat-event picture for a single engage-assess leg.</summary>
public sealed record CombatEventSnapshot
{
    private readonly IReadOnlyList<CombatTargetabilityFact> _targetability =
        Array.Empty<CombatTargetabilityFact>();

    private readonly IReadOnlyList<CombatExecutionFact> _execution = Array.Empty<CombatExecutionFact>();

    public IReadOnlyList<CombatEvent> Events { get; }

    /// <summary>Slice A targetability facts consumed for the event targets, ordered by contact id.</summary>
    public IReadOnlyList<CombatTargetabilityFact> Targetability
    {
        get => _targetability;
        init => _targetability = Array.AsReadOnly(value.ToArray());
    }

    /// <summary>Execution facts (firing solution, salvo) per correlation, in order-log sequence order.</summary>
    public IReadOnlyList<CombatExecutionFact> Execution
    {
        get => _execution;
        init => _execution = Array.AsReadOnly(value.ToArray());
    }

    public CombatEventSnapshot(IReadOnlyList<CombatEvent> events)
    {
        Events = Array.AsReadOnly(events.ToArray());
    }

    public static CombatEventSnapshot Empty { get; } =
        new(Array.Empty<CombatEvent>());
}
