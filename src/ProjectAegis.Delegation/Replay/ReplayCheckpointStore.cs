namespace ProjectAegis.Delegation.Replay;

/// <summary>Append-only checkpoint list emitted at fixed tick intervals.</summary>
public sealed class ReplayCheckpointStore
{
    private readonly List<ReplayCheckpoint> _checkpoints = new();

    public IReadOnlyList<ReplayCheckpoint> Checkpoints => _checkpoints;

    public void Record(ulong simTick, ulong worldHash, string logFingerprint, ulong lastSequenceId)
    {
        if (_checkpoints.Count > 0 && _checkpoints[^1].SimTick >= simTick)
        {
            return;
        }

        _checkpoints.Add(new ReplayCheckpoint(simTick, worldHash, logFingerprint, lastSequenceId));
    }

    public ReplayCheckpoint? FindAtOrBefore(ulong simTick)
    {
        ReplayCheckpoint? best = null;
        foreach (var checkpoint in _checkpoints)
        {
            if (checkpoint.SimTick <= simTick)
            {
                best = checkpoint;
            }
            else
            {
                break;
            }
        }

        return best;
    }
}