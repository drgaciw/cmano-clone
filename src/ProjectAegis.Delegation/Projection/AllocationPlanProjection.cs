namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;

/// <summary>
/// Pure allocation preview over explicit weapon, engagement, and MagazineLedger.Snapshot facts.
/// Presentation boundary: ADR-010 sections 2–3, ADR-007, ADR-001.
/// </summary>
public static class AllocationPlanProjection
{
    /// <summary>
    /// Combines duplicate intents and validates all requested rounds against shared shooter/mount magazines.
    /// Only supplied engagement legs are projected; missing facts are rejected rather than treated as permission.
    /// Policy denial takes precedence over source preview denial, then shared-magazine NO_AMMO.
    /// All intents count toward hypothetical budgets, including blocked ones. No commands or reservations are created.
    /// </summary>
    /// <exception cref="ArgumentNullException">An input collection is null.</exception>
    /// <exception cref="ArgumentException">Facts conflict, are incomplete, reference unknown identities, or contain invalid counts.</exception>
    public static AllocationPlanSnapshot Build(
        IEnumerable<ulong> shooterUnitIds,
        IEnumerable<string> targetIds,
        IEnumerable<AllocationWeaponFact> weapons,
        IEnumerable<AllocationEngagementFact> engagements,
        IEnumerable<MagazineLedgerEntry> magazines,
        IEnumerable<AllocationRequest> allocations)
    {
        if (shooterUnitIds == null) throw new ArgumentNullException(nameof(shooterUnitIds));
        if (targetIds == null) throw new ArgumentNullException(nameof(targetIds));
        if (weapons == null) throw new ArgumentNullException(nameof(weapons));
        if (engagements == null) throw new ArgumentNullException(nameof(engagements));
        if (magazines == null) throw new ArgumentNullException(nameof(magazines));
        if (allocations == null) throw new ArgumentNullException(nameof(allocations));

        var shooters = new HashSet<ulong>();
        foreach (var shooter in shooterUnitIds)
        {
            if (!shooters.Add(shooter)) throw new ArgumentException("Duplicate shooter.", nameof(shooterUnitIds));
        }

        var targets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var target in targetIds)
        {
            if (string.IsNullOrWhiteSpace(target) || !targets.Add(target))
                throw new ArgumentException("Target IDs must be nonblank and unique.", nameof(targetIds));
        }

        var ledger = new Dictionary<(ulong Shooter, ulong Mount), MagazineLedgerEntry>();
        foreach (var magazine in magazines)
        {
            if (magazine.Remaining < 0 || magazine.Capacity < magazine.Remaining ||
                !ledger.TryAdd((magazine.ShooterUnitId, magazine.MountId), magazine))
                throw new ArgumentException("Magazine facts must be unique with 0 <= remaining <= capacity.", nameof(magazines));
        }

        var weaponFacts = new Dictionary<(ulong Shooter, ulong Mount, string Weapon), AllocationWeaponFact>();
        foreach (var weapon in weapons)
        {
            if (weapon == null || !shooters.Contains(weapon.ShooterUnitId) ||
                string.IsNullOrWhiteSpace(weapon.WeaponId) || string.IsNullOrWhiteSpace(weapon.WeaponLabel))
                throw new ArgumentException("Weapon facts require a selected shooter, ID, and label.", nameof(weapons));
            var key = (weapon.ShooterUnitId, weapon.MountId, weapon.WeaponId);
            if (!weaponFacts.TryAdd(key, weapon))
                throw new ArgumentException("Duplicate weapon fact.", nameof(weapons));
            if (!ledger.ContainsKey((weapon.ShooterUnitId, weapon.MountId)))
                throw new ArgumentException("Every weapon needs an explicit magazine ledger fact.", nameof(magazines));
        }

        var legs = new Dictionary<(ulong Shooter, ulong Mount, string Weapon, string Target), AllocationEngagementFact>();
        foreach (var fact in engagements)
        {
            if (fact == null || fact.Preview == null || !targets.Contains(fact.TargetId) ||
                !weaponFacts.ContainsKey((fact.ShooterUnitId, fact.MountId, fact.WeaponId)))
                throw new ArgumentException("Engagement facts require a known weapon, selected target, and explicit preview.", nameof(engagements));
            if (!Enum.IsDefined(typeof(FireAbortReason), fact.PolicyAbortReason) ||
                (fact.Preview.CanFire ? fact.Preview.AbortPreviewCode != null : string.IsNullOrWhiteSpace(fact.Preview.AbortPreviewCode)))
                throw new ArgumentException("Engagement facts contain contradictory or missing abort information.", nameof(engagements));
            if (!legs.TryAdd((fact.ShooterUnitId, fact.MountId, fact.WeaponId, fact.TargetId), fact))
                throw new ArgumentException("Duplicate engagement fact.", nameof(engagements));
        }

        var requested = new Dictionary<(ulong Shooter, ulong Mount, string Weapon, string Target), long>();
        var mountRequested = new Dictionary<(ulong Shooter, ulong Mount), long>();
        foreach (var allocation in allocations)
        {
            if (allocation == null || allocation.Rounds < 0)
                throw new ArgumentException("Allocations require nonnegative round counts.", nameof(allocations));
            var key = (allocation.ShooterUnitId, allocation.MountId, allocation.WeaponId, allocation.TargetId);
            if (!legs.ContainsKey(key))
                throw new ArgumentException("Every allocation requires matching explicit engagement facts.", nameof(allocations));
            requested.TryGetValue(key, out var legRounds);
            requested[key] = checked(legRounds + allocation.Rounds);
            var mountKey = (allocation.ShooterUnitId, allocation.MountId);
            mountRequested.TryGetValue(mountKey, out var mountRounds);
            mountRequested[mountKey] = checked(mountRounds + allocation.Rounds);
        }

        var rows = legs.Values.OrderBy(f => f.ShooterUnitId).ThenBy(f => f.MountId)
            .ThenBy(f => f.WeaponId, StringComparer.Ordinal).ThenBy(f => f.TargetId, StringComparer.Ordinal)
            .Select(f =>
            {
                var mountKey = (f.ShooterUnitId, f.MountId);
                var available = ledger[mountKey].Remaining;
                mountRequested.TryGetValue(mountKey, out var total);
                requested.TryGetValue((f.ShooterUnitId, f.MountId, f.WeaponId, f.TargetId), out var rounds);
                var hasAmmo = available > 0 && total <= available;
                var abort = f.PolicyAbortReason != FireAbortReason.None ? null
                    : !f.Preview.CanFire ? f.Preview.AbortPreviewCode
                    : !hasAmmo ? AbortReasonCatalog.Engage.NO_AMMO : null;
                return new AllocationPlanRow(f.ShooterUnitId, f.MountId, f.WeaponId,
                    weaponFacts[(f.ShooterUnitId, f.MountId, f.WeaponId)].WeaponLabel, f.TargetId,
                    rounds, f.PolicyAbortReason == FireAbortReason.None && f.Preview.CanFire && hasAmmo,
                    f.PolicyAbortReason, abort);
            }).ToArray();

        var weaponRows = weaponFacts.Values.OrderBy(w => w.ShooterUnitId).ThenBy(w => w.MountId)
            .ThenBy(w => w.WeaponId, StringComparer.Ordinal).Select(w =>
            {
                var mountKey = (w.ShooterUnitId, w.MountId);
                mountRequested.TryGetValue(mountKey, out var total);
                var available = ledger[mountKey].Remaining;
                var weaponTotal = rows.Where(r => r.ShooterUnitId == w.ShooterUnitId && r.MountId == w.MountId && r.WeaponId == w.WeaponId)
                    .Sum(r => r.RequestedRounds);
                return new AllocationWeaponSummary(w.ShooterUnitId, w.MountId, w.WeaponId, w.WeaponLabel,
                    weaponTotal, total, available, Math.Max(0L, available - total), Math.Max(0L, total - available));
            }).ToArray();

        return new AllocationPlanSnapshot(
            rows.All(r => r.RequestedRounds == 0 || r.CanFire), rows, weaponRows,
            shooters.OrderBy(s => s).Select(s => new AllocationShooterTotal(s, rows.Where(r => r.ShooterUnitId == s).Sum(r => r.RequestedRounds))).ToArray(),
            targets.OrderBy(t => t, StringComparer.Ordinal).Select(t => new AllocationTargetTotal(t, rows.Where(r => r.TargetId == t).Sum(r => r.RequestedRounds))).ToArray());
    }
}
