namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>Pure Mission Board list/filter over canonical scenario document (AME-3.4).</summary>
public static class MissionBoardQuery
{
    /// <summary>
    /// Lists missions as board rows, optionally filtered by type/side/status, sorted by id (ordinal).
    /// </summary>
    /// <param name="document">Canonical scenario document.</param>
    /// <param name="typeFilter">Null = all; case-insensitive type match.</param>
    /// <param name="sideFilter">Null = all; match row.SideId.</param>
    /// <param name="statusFilter">Null = all; Assigned|Unassigned.</param>
    public static IReadOnlyList<MissionBoardRow> List(
        ScenarioDocumentDto document,
        string? typeFilter = null,
        string? sideFilter = null,
        string? statusFilter = null)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var unitSide = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in document.Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>())
        {
            if (!string.IsNullOrWhiteSpace(u.Id))
            {
                unitSide[u.Id] = u.SideId ?? "";
            }
        }

        IEnumerable<MissionBoardRow> rows = document.Missions.Select(m => ToRow(m, unitSide));

        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            rows = rows.Where(r => string.Equals(r.Type, typeFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(sideFilter))
        {
            rows = rows.Where(r => string.Equals(r.SideId, sideFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            rows = rows.Where(r => string.Equals(r.Status, statusFilter, StringComparison.OrdinalIgnoreCase));
        }

        return rows.OrderBy(r => r.Id, StringComparer.Ordinal).ToArray();
    }

    private static MissionBoardRow ToRow(ScenarioMissionDto m, IReadOnlyDictionary<string, string> unitSide)
    {
        var units = m.AssignedUnitIds ?? Array.Empty<string>();
        string? side = null;
        foreach (var id in units)
        {
            if (unitSide.TryGetValue(id, out var s) && !string.IsNullOrEmpty(s))
            {
                side = s;
                break;
            }
        }

        var status = units.Count > 0 ? "Assigned" : "Unassigned";
        var type = m.Type ?? "";
        return new MissionBoardRow
        {
            Id = m.Id,
            Type = type,
            SideId = side,
            Status = status,
            UnitCount = units.Count,
            SummaryLine = $"{type} {m.Id} | units={units.Count} | {status}",
        };
    }
}
