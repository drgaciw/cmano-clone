namespace ProjectAegis.Sim.Engage;

/// <summary>Deterministic magazine counts keyed by shooter+mount (MVP).</summary>
public sealed class MagazineLedger
{
    private readonly Dictionary<(ulong Shooter, ulong Mount), int> _rounds = new();

    public void SetRounds(ulong shooterUnitId, ulong mountId, int rounds) =>
        _rounds[(shooterUnitId, mountId)] = rounds;

    /// <summary>Sets initial capacity once per shooter+mount (no refill after consumption).</summary>
    public void EnsureInitialRounds(ulong shooterUnitId, ulong mountId, int rounds)
    {
        var key = (shooterUnitId, mountId);
        if (!_rounds.ContainsKey(key))
        {
            _rounds[key] = rounds;
        }
    }

    public int GetRounds(ulong shooterUnitId, ulong mountId) =>
        _rounds.TryGetValue((shooterUnitId, mountId), out var n) ? n : 0;

    public bool TryConsume(ulong shooterUnitId, ulong mountId)
    {
        var key = (shooterUnitId, mountId);
        if (!_rounds.TryGetValue(key, out var remaining) || remaining <= 0)
        {
            return false;
        }

        _rounds[key] = remaining - 1;
        return true;
    }
}
