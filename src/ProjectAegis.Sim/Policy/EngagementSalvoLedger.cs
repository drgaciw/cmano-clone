namespace ProjectAegis.Sim.Policy;

/// <summary>
/// Tracks active munitions fired against target tracks within engagement windows (AEGIS-303 / DRG-233).
/// </summary>
public sealed class EngagementSalvoLedger
{
    /// <summary>Default active engagement window duration in sim ticks (10s at 60Hz = 600 ticks).</summary>
    public const ulong DefaultEngagementWindowTicks = 600;

    /// <summary>Record of munitions fired against a target track at a specific sim tick.</summary>
    public readonly record struct SalvoEntry(int Count, ulong SimTick, ulong WindowTicks)
    {
        /// <summary>True when <paramref name="currentSimTick"/> falls within the active engagement window [SimTick, SimTick + WindowTicks).</summary>
        public bool IsActive(ulong currentSimTick)
        {
            if (currentSimTick < SimTick)
            {
                return false;
            }

            if (WindowTicks == 0)
            {
                return true;
            }

            var expiresAt = SimTick <= ulong.MaxValue - WindowTicks
                ? SimTick + WindowTicks
                : ulong.MaxValue;

            return currentSimTick < expiresAt;
        }
    }

    private readonly object _lock = new();
    private readonly Dictionary<ulong, List<SalvoEntry>> _entries = new();
    private readonly ulong _defaultWindowTicks;

    public ulong DefaultWindowTicks => _defaultWindowTicks;

    public EngagementSalvoLedger(ulong defaultWindowTicks = DefaultEngagementWindowTicks)
    {
        _defaultWindowTicks = defaultWindowTicks;
    }

    /// <summary>
    /// Registers fired munitions against a target track.
    /// </summary>
    /// <param name="targetTrackId">The target track ID.</param>
    /// <param name="count">Number of munitions fired (defaults to 1).</param>
    /// <param name="simTick">Sim tick when the munitions were fired.</param>
    /// <param name="windowTicks">Active window ticks for this engagement (null uses ledger default).</param>
    public void RegisterFired(ulong targetTrackId, int count = 1, ulong simTick = 0, ulong? windowTicks = null)
    {
        if (count <= 0)
        {
            return;
        }

        var window = windowTicks ?? _defaultWindowTicks;
        lock (_lock)
        {
            if (!_entries.TryGetValue(targetTrackId, out var list))
            {
                list = new List<SalvoEntry>();
                _entries[targetTrackId] = list;
            }

            list.Add(new SalvoEntry(count, simTick, window));
        }
    }

    /// <summary>
    /// Returns the active salvo count (munitions fired within active window) for a target track at the given tick.
    /// </summary>
    public int GetActiveSalvoCount(ulong targetTrackId, ulong currentSimTick = 0)
    {
        lock (_lock)
        {
            if (!_entries.TryGetValue(targetTrackId, out var list))
            {
                return 0;
            }

            var sum = 0;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].IsActive(currentSimTick))
                {
                    sum += list[i].Count;
                }
            }

            return sum;
        }
    }

    /// <summary>
    /// Alias for <see cref="GetActiveSalvoCount"/>.
    /// </summary>
    public int GetFiredCount(ulong targetTrackId, ulong currentSimTick = 0) =>
        GetActiveSalvoCount(targetTrackId, currentSimTick);

    /// <summary>
    /// Returns total munitions recorded for target track across all windows (including expired).
    /// </summary>
    public int GetTotalFired(ulong targetTrackId)
    {
        lock (_lock)
        {
            if (!_entries.TryGetValue(targetTrackId, out var list))
            {
                return 0;
            }

            var sum = 0;
            for (var i = 0; i < list.Count; i++)
            {
                sum += list[i].Count;
            }

            return sum;
        }
    }

    /// <summary>
    /// Prunes expired salvo entries across all target tracks up to <paramref name="currentSimTick"/>.
    /// Entries with SimTick &gt; currentSimTick (future) are retained.
    /// </summary>
    public void PruneExpired(ulong currentSimTick)
    {
        lock (_lock)
        {
            foreach (var kvp in _entries)
            {
                kvp.Value.RemoveAll(e => currentSimTick >= e.SimTick && !e.IsActive(currentSimTick));
            }
        }
    }

    /// <summary>
    /// Clears all recorded entries.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
    }

    /// <summary>
    /// Clears recorded entries for a specific target track.
    /// </summary>
    public void ClearTarget(ulong targetTrackId)
    {
        lock (_lock)
        {
            _entries.Remove(targetTrackId);
        }
    }

    /// <summary>
    /// Quick indexer to get active salvo count at tick 0.
    /// </summary>
    public int this[ulong targetTrackId] => GetActiveSalvoCount(targetTrackId, 0);
}
