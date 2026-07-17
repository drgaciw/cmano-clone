namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless selection helper for message log rows (CMD-05 depth).
/// Returns sequenceId + optional unit id for deep-link / unit focus.
/// </summary>
public static class MessageLogPanelSelection
{
    public static MessageLogSelection? SelectRow(MessageLogPanelState state, int index)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (index < 0 || index >= state.Rows.Count)
        {
            return null;
        }

        var row = state.Rows[index];
        return new MessageLogSelection(row.SequenceId, row.UnitId, index);
    }
}

public sealed record MessageLogSelection(ulong SequenceId, string? UnitId, int RowIndex);
