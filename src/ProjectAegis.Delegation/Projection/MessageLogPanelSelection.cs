namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless selection helper for message log rows (CMD-05 depth / S105 A2).
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

    public static MessageLogSelection? SelectBySequenceId(MessageLogPanelState state, ulong sequenceId)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        for (var i = 0; i < state.Rows.Count; i++)
        {
            if (state.Rows[i].SequenceId == sequenceId)
            {
                var row = state.Rows[i];
                return new MessageLogSelection(row.SequenceId, row.UnitId, i);
            }
        }

        return null;
    }

    /// <summary>
    /// Unit focus: latest row (highest index) matching unitId. Case-sensitive unit ids.
    /// </summary>
    public static MessageLogSelection? SelectByUnitId(MessageLogPanelState state, string unitId)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (string.IsNullOrEmpty(unitId))
        {
            return null;
        }

        for (var i = state.Rows.Count - 1; i >= 0; i--)
        {
            var row = state.Rows[i];
            if (row.UnitId == unitId)
            {
                return new MessageLogSelection(row.SequenceId, row.UnitId, i);
            }
        }

        return null;
    }

    public static bool TryFindRowIndexBySequenceId(MessageLogPanelState state, ulong sequenceId, out int index)
    {
        var sel = SelectBySequenceId(state, sequenceId);
        if (sel == null)
        {
            index = -1;
            return false;
        }

        index = sel.RowIndex;
        return true;
    }

    public static bool TryFindRowIndexByUnitId(MessageLogPanelState state, string unitId, out int index)
    {
        var sel = SelectByUnitId(state, unitId);
        if (sel == null)
        {
            index = -1;
            return false;
        }

        index = sel.RowIndex;
        return true;
    }
}

public sealed record MessageLogSelection(ulong SequenceId, string? UnitId, int RowIndex);
