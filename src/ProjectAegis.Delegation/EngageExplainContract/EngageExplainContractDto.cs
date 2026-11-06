namespace ProjectAegis.Delegation.EngageExplainContract;

/// <summary>
/// DRG-215: presentation-facing engagement explanation row for Combat UX Slice B.
/// Populated strictly from combat-event facts; no UI-derived truth.
/// </summary>
public sealed record EngageExplainContractDto(
    string? WhyPermitted,
    string? WhyWithheld,
    string WeaponFamilyId,
    ulong CorrelationId,
    double SimTime)
{
    public static EngageExplainContractDto Empty { get; } =
        new(null, null, string.Empty, 0, 0);
}
