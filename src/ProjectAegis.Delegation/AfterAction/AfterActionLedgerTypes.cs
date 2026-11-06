namespace ProjectAegis.Delegation.AfterAction;

/// <summary>
/// One replay-linked after-action ledger row. Advisory only — never fire, authorize, or enqueue orders.
/// Fields are copied verbatim from combat-event contract rows; no reconstructed facts.
/// </summary>
public sealed record AfterActionLedgerEntry(
    string ShooterId,
    string TargetId,
    string WeaponFamilyId,
    string Outcome,
    ulong CorrelationId,
    double SimTime,
    ulong SimTick,
    CombatEventPhaseConsume Phase,
    string ExplanationRef);

/// <summary>Ordered, filterable after-action ledger for DRG-171 navigation.</summary>
public sealed record AfterActionLedgerSnapshot(IReadOnlyList<AfterActionLedgerEntry> Entries)
{
    /// <summary>Empty ledger.</summary>
    public static AfterActionLedgerSnapshot Empty { get; } =
        new(Array.Empty<AfterActionLedgerEntry>());
}

/// <summary>Optional filters for ledger rows (platform/shooter, target, weapon family, outcome).</summary>
public sealed record AfterActionLedgerFilter(
    string? ShooterId = null,
    string? TargetId = null,
    string? WeaponFamilyId = null,
    string? Outcome = null);
