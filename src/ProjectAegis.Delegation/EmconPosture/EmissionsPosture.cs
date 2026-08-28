namespace ProjectAegis.Delegation.EmconPosture;

using ProjectAegis.Sim.Policy;

/// <summary>
/// DRG-221: headless EMCON/emissions posture for Combat UX Slice C.
/// Advisory only — never toggles emitters, never authorizes weapons release, never enqueues fire.
/// Distinct from C2 network health (DRG-214) and agent Skills (DRG-194).
/// </summary>
public sealed record EmissionsPosture(
    string UnitId,
    EmconState EmconLevel,
    EmconPostureKind PostureKind,
    bool IsAdvisoryOnly,
    bool IsEmitterToggleAuthorization,
    bool IsWeaponsReleaseAuthorization,
    bool IsFireOrder,
    IReadOnlyList<RadiatingSensor> RadiatingSensors,
    IReadOnlyList<string> Assumptions,
    EmconPostureSilentCause SilentCause,
    string? SilentCauseCode,
    string? SilentCauseLabel,
    string StatusLine)
{
    /// <summary>Empty sentinel when no unit facts are supplied.</summary>
    public static EmissionsPosture Empty { get; } = new(
        UnitId: string.Empty,
        EmconLevel: EmconState.Off,
        PostureKind: EmconPostureKind.AdvisoryEmissionsPosture,
        IsAdvisoryOnly: true,
        IsEmitterToggleAuthorization: false,
        IsWeaponsReleaseAuthorization: false,
        IsFireOrder: false,
        RadiatingSensors: Array.Empty<RadiatingSensor>(),
        Assumptions: Array.Empty<string>(),
        SilentCause: EmconPostureSilentCause.PolicyOff,
        SilentCauseCode: null,
        SilentCauseLabel: null,
        StatusLine: "EMCON: —");
}
