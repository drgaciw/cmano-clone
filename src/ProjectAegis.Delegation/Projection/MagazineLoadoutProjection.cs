namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Pure projection of magazine remaining/capacity into loadout rows (CMD-24 / REQ-20).
/// Callers supply tuples — no Unity / orchestrator dependency.
/// </summary>
public static class MagazineLoadoutProjection
{
    public const string StatusOk = "OK";
    public const string StatusLow = "LOW";
    public const string StatusEmpty = "EMPTY";
    public const string MissingLabel = "—";

    /// <summary>Fill fraction at or below this is <see cref="StatusLow"/> (above empty).</summary>
    public const double LowFillThreshold = 0.25;

    /// <summary>
    /// Project magazine rows from loadout tuples.
    /// Items: unitId, platformId, mountId, weaponId, weaponLabel, remaining, capacity.
    /// </summary>
    public static IReadOnlyList<MagazineLoadoutEntry> Project(
        IReadOnlyList<(
            string unitId,
            string platformId,
            string mountId,
            string weaponId,
            string? weaponLabel,
            int remaining,
            int capacity)>? rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return Array.Empty<MagazineLoadoutEntry>();
        }

        var result = new List<MagazineLoadoutEntry>(rows.Count);
        foreach (var row in rows
                     .Where(r => !string.IsNullOrWhiteSpace(r.unitId))
                     .OrderBy(r => r.unitId, StringComparer.Ordinal)
                     .ThenBy(r => r.mountId, StringComparer.Ordinal)
                     .ThenBy(r => r.weaponId, StringComparer.Ordinal))
        {
            result.Add(ToEntry(
                row.unitId,
                row.platformId,
                row.mountId,
                row.weaponId,
                row.weaponLabel,
                row.remaining,
                row.capacity));
        }

        return result;
    }

    /// <summary>
    /// Magazine-constrained loadout feasibility: how many airframes can be fully armed
    /// from current stock given rounds-per-airframe cost.
    /// </summary>
    public static int HowManyAirframesCanArm(int remainingRounds, int roundsPerAirframe)
    {
        if (remainingRounds <= 0 || roundsPerAirframe <= 0)
        {
            return 0;
        }

        return remainingRounds / roundsPerAirframe;
    }

    /// <summary>Fill percentage 0–100 from remaining/capacity (capacity 0 → 0 when empty, else 100).</summary>
    public static double ComputeFillPct(int remaining, int capacity)
    {
        var rem = Math.Max(0, remaining);
        if (capacity <= 0)
        {
            return rem > 0 ? 100.0 : 0.0;
        }

        return Math.Clamp(100.0 * rem / capacity, 0.0, 100.0);
    }

    /// <summary>OK / LOW / EMPTY from remaining and fill fraction.</summary>
    public static string ResolveStatus(int remaining, double fillPct)
    {
        if (remaining <= 0)
        {
            return StatusEmpty;
        }

        if (fillPct / 100.0 <= LowFillThreshold)
        {
            return StatusLow;
        }

        return StatusOk;
    }

    public static MagazineLoadoutAggregate Aggregate(IReadOnlyList<MagazineLoadoutEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return MagazineLoadoutAggregate.Empty;
        }

        var ok = 0;
        var low = 0;
        var empty = 0;
        var totalRemaining = 0;
        var totalCapacity = 0;
        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            totalRemaining += Math.Max(0, e.Remaining);
            totalCapacity += Math.Max(0, e.Capacity);
            if (string.Equals(e.StatusLine, StatusEmpty, StringComparison.Ordinal))
            {
                empty++;
            }
            else if (string.Equals(e.StatusLine, StatusLow, StringComparison.Ordinal))
            {
                low++;
            }
            else
            {
                ok++;
            }
        }

        return new MagazineLoadoutAggregate(
            ok,
            low,
            empty,
            entries.Count,
            totalRemaining,
            totalCapacity,
            FormatSummaryLine(ok, low, empty, entries.Count));
    }

    public static string FormatSummaryLine(int ok, int low, int empty, int total) =>
        $"OK {ok}  LOW {low}  EMPTY {empty}  ·  {total} mounts";

    private static MagazineLoadoutEntry ToEntry(
        string unitId,
        string platformId,
        string mountId,
        string weaponId,
        string? weaponLabel,
        int remaining,
        int capacity)
    {
        var rem = Math.Max(0, remaining);
        var cap = Math.Max(0, capacity);
        var fill = ComputeFillPct(rem, cap);
        var status = ResolveStatus(rem, fill);
        var platform = string.IsNullOrWhiteSpace(platformId) ? MissingLabel : platformId.Trim();
        var mount = string.IsNullOrWhiteSpace(mountId) ? MissingLabel : mountId.Trim();
        var weapon = string.IsNullOrWhiteSpace(weaponId) ? MissingLabel : weaponId.Trim();
        var label = string.IsNullOrWhiteSpace(weaponLabel) ? weapon : weaponLabel.Trim();

        return new MagazineLoadoutEntry(
            UnitId: unitId.Trim(),
            PlatformId: platform,
            MountId: mount,
            WeaponId: weapon,
            WeaponLabel: label,
            Remaining: rem,
            Capacity: cap,
            FillPct: fill,
            StatusLine: status);
    }
}

/// <summary>Aggregate magazine status for panel header (CMD-24).</summary>
public sealed record MagazineLoadoutAggregate(
    int OkCount,
    int LowCount,
    int EmptyCount,
    int TotalCount,
    int TotalRemaining,
    int TotalCapacity,
    string SummaryLine)
{
    public static MagazineLoadoutAggregate Empty { get; } =
        new(0, 0, 0, 0, 0, 0, "OK 0  LOW 0  EMPTY 0  ·  0 mounts");
}
