namespace ProjectAegis.Delegation.UnityAdapter.Save;

using Input;

/// <summary>
/// External ordered input journal (ADR-029 / DRG-348 / DRG-349).
/// Records accepted authority-mutating calls with both observed and pipeline ticks
/// and computes a deterministic sequential SHA-256 digest.
/// </summary>
public sealed class SessionInputJournal
{
    private readonly List<SaveJournalRow> _rows = new();

    public IReadOnlyList<SaveJournalRow> Rows => _rows;

    public SaveJournalRow Append(
        ulong observedTick,
        ulong pipelineTick,
        InputCommandJournalKind kind,
        SaveJournalArguments arguments)
    {
        if (arguments is null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }

        string commandId = string.Empty;
        foreach (var entry in InputCommandInventory.All)
        {
            if (entry.Kind == kind)
            {
                commandId = entry.CommandId;
                break;
            }
        }

        if (string.IsNullOrEmpty(commandId))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unregistered InputCommandJournalKind.");
        }

        var row = new SaveJournalRow(
            SequenceOrder: (ulong)_rows.Count,
            ObservedTick: observedTick,
            PipelineTick: pipelineTick,
            CommandKind: commandId,
            Arguments: arguments);
        _rows.Add(row);
        return row;
    }

    public string ComputeDigestSha256() =>
        SaveEnvelopeSerializer.ComputeJournalDigestSha256(_rows);

    public void Clear() => _rows.Clear();
}
