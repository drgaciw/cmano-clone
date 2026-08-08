namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// CMD-26 Phase A: brigade+ ground formations — flat list, no battalion expander.
/// Aggregate strength, ADA contribution, facility status.
/// </summary>
public static class GroundOpsProjection
{
    public static IReadOnlyList<GroundFormationEntry> Project(
        IReadOnlyList<GroundFormationInput>? formations)
    {
        if (formations is null || formations.Count == 0)
        {
            return Array.Empty<GroundFormationEntry>();
        }

        var result = new List<GroundFormationEntry>(formations.Count);
        foreach (var f in formations.OrderBy(x => x.FormationId, StringComparer.Ordinal))
        {
            if (f is null || string.IsNullOrWhiteSpace(f.FormationId))
            {
                continue;
            }

            result.Add(new GroundFormationEntry(
                FormationId: f.FormationId,
                DisplayName: string.IsNullOrWhiteSpace(f.DisplayName) ? f.FormationId : f.DisplayName,
                SideLabel: f.SideLabel ?? "—",
                EchelonLabel: NormalizeEchelon(f.EchelonLabel),
                LocationLabel: f.LocationLabel ?? "—",
                StrengthBand: FormatStrengthBand(f.StrengthFraction),
                AdaContributionLine: FormatAda(f.AdaAssetCount, f.AdaOnlineCount),
                FacilityStatusLine: FormatFacility(f.FacilityKind, f.FacilityDamageFraction),
                IsLeaf: true));
        }

        return result;
    }

    public static GroundOpsAggregate Aggregate(IReadOnlyList<GroundFormationEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return GroundOpsAggregate.Empty;
        }

        return new GroundOpsAggregate(
            FormationCount: entries.Count,
            SummaryLine: $"GROUND: {entries.Count} formation(s) (brigade+)");
    }

    private static string NormalizeEchelon(string? echelon)
    {
        if (string.IsNullOrWhiteSpace(echelon))
        {
            return "Brigade+";
        }

        if (echelon.Contains("battalion", StringComparison.OrdinalIgnoreCase)
            || echelon.Contains("company", StringComparison.OrdinalIgnoreCase))
        {
            return "Brigade+ (charter leaf)";
        }

        return echelon;
    }

    private static string FormatStrengthBand(double fraction)
    {
        var f = Math.Clamp(fraction, 0, 1);
        if (f >= 0.85) return "STRENGTH: GREEN";
        if (f >= 0.50) return "STRENGTH: AMBER";
        if (f > 0) return "STRENGTH: RED";
        return "STRENGTH: —";
    }

    private static string FormatAda(int total, int online)
    {
        if (total <= 0)
        {
            return "ADA: none";
        }

        return $"ADA: {Math.Max(0, online)}/{total} online";
    }

    private static string FormatFacility(string? kind, double damageFraction)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            return "FACILITY: —";
        }

        var d = Math.Clamp(damageFraction, 0, 1);
        var status = d <= 0.05 ? "OPEN" : d < 0.5 ? "DAMAGED" : "CLOSED";
        return $"FACILITY: {kind} ({status})";
    }
}

public sealed record GroundFormationInput(
    string FormationId,
    string? DisplayName,
    string? SideLabel,
    string? EchelonLabel,
    string? LocationLabel,
    double StrengthFraction,
    int AdaAssetCount,
    int AdaOnlineCount,
    string? FacilityKind,
    double FacilityDamageFraction);

public sealed record GroundFormationEntry(
    string FormationId,
    string DisplayName,
    string SideLabel,
    string EchelonLabel,
    string LocationLabel,
    string StrengthBand,
    string AdaContributionLine,
    string FacilityStatusLine,
    bool IsLeaf);

public sealed record GroundOpsAggregate(int FormationCount, string SummaryLine)
{
    public static GroundOpsAggregate Empty { get; } = new(0, "GROUND: no formations");
}
