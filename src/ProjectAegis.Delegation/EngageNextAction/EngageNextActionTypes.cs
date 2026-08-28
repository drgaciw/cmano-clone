namespace ProjectAegis.Delegation.EngageNextAction;

/// <summary>
/// Stable next-corrective-action codes for withheld engagements (DRG-226 / CMB-UI-03).
/// Advisory only — never a fire order.
/// </summary>
public static class EngageNextActionCodes
{
    /// <summary>Magazine empty or insufficient rounds — reload or rearm before re-engaging.</summary>
    public const string ReloadRearm = "RELOAD_REARM";

    /// <summary>Rules-of-engagement or weapons-release gate — seek player or C2 approval.</summary>
    public const string Approval = "APPROVAL";
}

/// <summary>
/// Read-only withhold facts for headless next-action projection (DRG-226).
/// Mirrors employment / explain inputs without coupling to those assemblies.
/// </summary>
public sealed record EngageNextActionInput(
    string ShooterId,
    string WeaponFamilyId,
    string? WithholdReason);

/// <summary>
/// One presentation-facing next-action row. Sim-clock only — no UI selection, hover, camera,
/// or panel state. Advisory only — never a fire order.
/// </summary>
public sealed record EngageNextActionRow(
    string ShooterId,
    string WeaponFamilyId,
    string? WithholdReason,
    string? NextActionCode,
    bool IsFireOrder);

/// <summary>Ordered, replay-stable next-action snapshot for one or more withheld engagements.</summary>
public sealed record EngageNextActionSnapshot(
    IReadOnlyList<EngageNextActionRow> Rows,
    bool IsFireOrder)
{
    /// <summary>Empty snapshot — advisory only, never a fire order.</summary>
    public static EngageNextActionSnapshot Empty { get; } =
        new(Array.Empty<EngageNextActionRow>(), IsFireOrder: false);
}
