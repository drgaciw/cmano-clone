namespace ProjectAegis.Delegation.EmploymentLedger;

/// <summary>
/// Read-only magazine / salvo facts for headless employment ledger projection (DRG-224).
/// Fields mirror engage ammo preview inputs without coupling to sim engage context.
/// </summary>
public sealed record EmploymentLedgerMagazineFacts(
    string ShooterId,
    string WeaponFamilyId,
    int RoundsRemaining,
    int SalvoSize,
    ulong LastEmploymentTick = 0);

/// <summary>
/// One presentation-facing magazine / salvo employment row. Sim-clock only — no UI selection,
/// hover, camera, or panel state. Advisory only — never a fire order.
/// </summary>
public sealed record EmploymentLedgerRow(
    string ShooterId,
    string WeaponFamilyId,
    int RoundsRemaining,
    int SalvoSize,
    ulong LastEmploymentTick,
    string? WithholdReason,
    bool IsFireOrder);

/// <summary>Ordered, replay-stable employment ledger for one or more shooter / weapon-family magazines.</summary>
public sealed record EmploymentLedgerSnapshot(
    IReadOnlyList<EmploymentLedgerRow> Rows,
    bool IsFireOrder)
{
    /// <summary>Empty ledger — advisory only, never a fire order.</summary>
    public static EmploymentLedgerSnapshot Empty { get; } =
        new(Array.Empty<EmploymentLedgerRow>(), IsFireOrder: false);
}
