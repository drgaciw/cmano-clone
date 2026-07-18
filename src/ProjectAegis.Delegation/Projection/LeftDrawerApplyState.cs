namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for left-drawer OOB tab rows (ASSET-007 / S107).
/// Maps bound <see cref="OobTreePanelState"/> into presentation rows hosts can list.
/// </summary>
public static class LeftDrawerApplyState
{
    public static LeftDrawerPresentation Apply(OobTreePanelState? oobState)
    {
        if (oobState is null || oobState.UnitRows is null || oobState.UnitRows.Count == 0)
        {
            return LeftDrawerPresentation.Empty;
        }

        var rows = new List<LeftDrawerRowPresentation>(oobState.UnitRows.Count);
        string? selectedId = null;
        foreach (var row in oobState.UnitRows)
        {
            rows.Add(new LeftDrawerRowPresentation(
                row.UnitId,
                row.DisplayLine ?? string.Empty,
                row.IsAlive,
                row.IsSelected,
                row.StyleClass ?? "oob-row"));
            if (row.IsSelected)
            {
                selectedId = row.UnitId;
            }
        }

        return new LeftDrawerPresentation(rows, selectedId, rows.Count);
    }

    public static LeftDrawerPresentation BindAndApply(
        IReadOnlyList<OobTreeEntry> entries,
        string? selectedUnitId = null)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        return Apply(OobTreePanelBinder.Bind(entries, selectedUnitId));
    }

    public static LeftDrawerPresentation BindAndApply(
        IReadOnlyList<OobTreeEntry> entries,
        string? selectedUnitId,
        IReadOnlyList<string>? graphHighlightIds)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        return Apply(OobTreePanelBinder.Bind(entries, selectedUnitId, graphHighlightIds));
    }
}

public sealed record LeftDrawerRowPresentation(
    string UnitId,
    string DisplayLine,
    bool IsAlive,
    bool IsSelected,
    string StyleClass);

public sealed record LeftDrawerPresentation(
    IReadOnlyList<LeftDrawerRowPresentation> Rows,
    string? SelectedUnitId,
    int RowCount)
{
    public static LeftDrawerPresentation Empty { get; } =
        new(Array.Empty<LeftDrawerRowPresentation>(), null, 0);
}
