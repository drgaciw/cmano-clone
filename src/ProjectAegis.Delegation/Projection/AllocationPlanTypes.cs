namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Policy;

/// <summary>Authoritative weapon identity and display label; ammunition is keyed by shooter and mount, not weapon.</summary>
public sealed record AllocationWeaponFact(ulong ShooterUnitId, ulong MountId, string WeaponId, string WeaponLabel);

/// <summary>Supplied engage preview and policy verdict for one shooter/mount/weapon/target leg. Never inferred from a recommendation.</summary>
public sealed record AllocationEngagementFact(
    ulong ShooterUnitId,
    ulong MountId,
    string WeaponId,
    string TargetId,
    EngagePreview Preview,
    FireAbortReason PolicyAbortReason);

/// <summary>Proposed round count. Repeated legs are summed; zero leaves the leg unallocated.</summary>
public sealed record AllocationRequest(ulong ShooterUnitId, ulong MountId, string WeaponId, string TargetId, int Rounds);

/// <summary>
/// One supplied engagement leg, with combined requested rounds and current preview eligibility.
/// A blocked row carries the existing policy enum, source preview code, or catalog NO_AMMO code.
/// CanFire is a preview over supplied facts, never a weapons-release authorization.
/// </summary>
public sealed record AllocationPlanRow(
    ulong ShooterUnitId,
    ulong MountId,
    string WeaponId,
    string WeaponLabel,
    string TargetId,
    long RequestedRounds,
    bool CanFire,
    FireAbortReason PolicyAbortReason,
    string? AbortCode);

/// <summary>
/// Per-weapon request total with its shared shooter/mount budget. AvailableRounds is the unchanged ledger fact.
/// MagazineRequestedRounds includes all weapons and targets on that mount, including blocked intents.
/// RemainingAfterPlan and Shortfall are hypothetical even for rejected plans; nothing is reserved or consumed.
/// Shared magazine values repeat on each weapon row and must not be summed across weapons.
/// </summary>
public sealed record AllocationWeaponSummary(
    ulong ShooterUnitId,
    ulong MountId,
    string WeaponId,
    string WeaponLabel,
    long RequestedRounds,
    long MagazineRequestedRounds,
    int AvailableRounds,
    long RemainingAfterPlan,
    long Shortfall);

/// <summary>Total requested rounds across a shooter's weapons and targets, including blocked intents.</summary>
public sealed record AllocationShooterTotal(ulong ShooterUnitId, long RequestedRounds);

/// <summary>Total requested rounds against a target across all shooters, including blocked intents.</summary>
public sealed record AllocationTargetTotal(string TargetId, long RequestedRounds);

/// <summary>Immutable preview snapshot. No partial acceptance or ammunition reservation is performed.</summary>
public sealed class AllocationPlanSnapshot
{
    internal AllocationPlanSnapshot(
        bool isValid,
        AllocationPlanRow[] rows,
        AllocationWeaponSummary[] weapons,
        AllocationShooterTotal[] shooters,
        AllocationTargetTotal[] targets)
    {
        IsValid = isValid;
        Rows = Array.AsReadOnly(rows);
        Weapons = Array.AsReadOnly(weapons);
        Shooters = Array.AsReadOnly(shooters);
        Targets = Array.AsReadOnly(targets);
    }

    /// <summary>True when every positive request is eligible and every shared magazine budget fits; an empty plan is valid.</summary>
    public bool IsValid { get; }

    /// <summary>Supplied engagement legs, sorted by shooter, mount, ordinal weapon ID, then ordinal target ID.</summary>
    public IReadOnlyList<AllocationPlanRow> Rows { get; }

    /// <summary>Weapons and their hypothetical shared-magazine remainder, sorted by shooter, mount, then ordinal weapon ID.</summary>
    public IReadOnlyList<AllocationWeaponSummary> Weapons { get; }

    /// <summary>All selected shooters and their requested totals, sorted numerically.</summary>
    public IReadOnlyList<AllocationShooterTotal> Shooters { get; }

    /// <summary>All selected targets and their requested totals, sorted ordinally.</summary>
    public IReadOnlyList<AllocationTargetTotal> Targets { get; }
}
