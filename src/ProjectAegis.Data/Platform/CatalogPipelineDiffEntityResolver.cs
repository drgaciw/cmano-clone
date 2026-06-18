namespace ProjectAegis.Data.Platform;

/// <summary>S29-10: resolve catalog entity ids touched by an import/approve diff.</summary>
public static class CatalogPipelineDiffEntityResolver
{
    private static readonly string[] PlatformIdSheets =
    {
        "Sensors", "Mounts", "Loadouts", "Magazines", "Comms",
        "Mobility", "Signatures", "Emcon", "Platforms",
    };

    public static IReadOnlyList<string> ResolveFromImportPlan(PlatformImportPlan plan, PlatformWorkbook edited)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        if (edited is null) throw new ArgumentNullException(nameof(edited));
        if (!plan.HasChanges)
        {
            return [];
        }

        var ids = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var change in plan.SupportedChanges)
        {
            if (change.RowIndex < 0)
            {
                continue;
            }

            if (!PlatformIdSheets.Contains(change.Sheet, StringComparer.Ordinal))
            {
                continue;
            }

            var sheet = edited.FindSheet(change.Sheet);
            if (sheet is null || change.RowIndex >= sheet.Rows.Count)
            {
                continue;
            }

            var platformId = ReadPlatformId(sheet, change.RowIndex);
            if (!string.IsNullOrWhiteSpace(platformId))
            {
                ids.Add(platformId);
            }
        }

        return ids.ToArray();
    }

    public static IReadOnlyList<string> ResolveFromPlatformIds(IEnumerable<string> platformIds)
    {
        if (platformIds is null)
        {
            return [];
        }

        return platformIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ReadPlatformId(PlatformWorkbookSheet sheet, int rowIndex)
    {
        var col = HeaderIndex(sheet);
        if (!col.TryGetValue("PlatformId", out var platformCol) || platformCol >= sheet.Rows[rowIndex].Count)
        {
            return string.Empty;
        }

        return sheet.Rows[rowIndex][platformCol];
    }

    private static Dictionary<string, int> HeaderIndex(PlatformWorkbookSheet sheet)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < sheet.Header.Count; i++)
        {
            map[sheet.Header[i]] = i;
        }

        return map;
    }
}