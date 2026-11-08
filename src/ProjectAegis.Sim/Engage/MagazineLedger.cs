namespace ProjectAegis.Sim.Engage;

/// <summary>Deterministic magazine counts keyed by shooter+mount (MVP).</summary>
public sealed class MagazineLedger
{
    private readonly Dictionary<(ulong Shooter, ulong Mount), int> _rounds = new();
    private readonly Dictionary<(ulong Shooter, ulong Mount), int> _capacity = new();

    public void SetRounds(ulong shooterUnitId, ulong mountId, int rounds)
    {
        var key = (shooterUnitId, mountId);
        var value = Math.Max(0, rounds);
        _rounds[key] = value;
        // Capture initial capacity on first write; never shrink capacity on reload/set.
        if (!_capacity.ContainsKey(key) || value > _capacity[key])
        {
            _capacity[key] = value;
        }
    }

    /// <summary>Sets initial capacity once per shooter+mount (no refill after consumption).</summary>
    public void EnsureInitialRounds(ulong shooterUnitId, ulong mountId, int rounds)
    {
        var key = (shooterUnitId, mountId);
        if (!_rounds.ContainsKey(key))
        {
            var value = Math.Max(0, rounds);
            _rounds[key] = value;
            _capacity[key] = value;
        }
    }

    public int GetRounds(ulong shooterUnitId, ulong mountId) =>
        _rounds.TryGetValue((shooterUnitId, mountId), out var n) ? n : 0;

    /// <summary>
    /// Capacity recorded at first seed/set (does not decrease on consume).
    /// Falls back to remaining when capacity was never recorded.
    /// </summary>
    public int GetCapacity(ulong shooterUnitId, ulong mountId)
    {
        var key = (shooterUnitId, mountId);
        if (_capacity.TryGetValue(key, out var cap))
        {
            return cap;
        }

        return GetRounds(shooterUnitId, mountId);
    }

    /// <summary>
    /// True when this shooter+mount has been seeded or consumed (ledger is authoritative).
    /// Distinguishes tracked-empty (0) from never-seeded (also GetRounds==0).
    /// </summary>
    public bool TryGetRounds(ulong shooterUnitId, ulong mountId, out int rounds) =>
        _rounds.TryGetValue((shooterUnitId, mountId), out rounds);

    public bool TryConsume(ulong shooterUnitId, ulong mountId) =>
        TryConsumeSalvo(shooterUnitId, mountId, 1);

    public bool TryConsumeSalvo(ulong shooterUnitId, ulong mountId, int salvoSize)
    {
        var rounds = Math.Max(1, salvoSize);
        var key = (shooterUnitId, mountId);
        if (!_rounds.TryGetValue(key, out var remaining) || remaining < rounds)
        {
            return false;
        }

        _rounds[key] = remaining - rounds;
        return true;
    }

    /// <summary>
    /// Read-only snapshot of tracked mounts for UI projection (no weapon labels — presentation fills).
    /// Ordered by shooter then mount.
    /// </summary>
    public IReadOnlyList<MagazineLedgerEntry> Snapshot()
    {
        if (_rounds.Count == 0)
        {
            return Array.Empty<MagazineLedgerEntry>();
        }

        var list = new List<MagazineLedgerEntry>(_rounds.Count);
        foreach (var kv in _rounds
                     .OrderBy(k => k.Key.Shooter)
                     .ThenBy(k => k.Key.Mount))
        {
            var remaining = kv.Value;
            var capacity = _capacity.TryGetValue(kv.Key, out var cap)
                ? Math.Max(cap, remaining)
                : remaining;
            list.Add(new MagazineLedgerEntry(kv.Key.Shooter, kv.Key.Mount, remaining, capacity));
        }

        return list;
    }
}

/// <summary>One ledger row for presentation projection (sim-side, no weapon labels).</summary>
public readonly record struct MagazineLedgerEntry(
    ulong ShooterUnitId,
    ulong MountId,
    int Remaining,
    int Capacity);
