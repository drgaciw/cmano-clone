using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>One bound magazine row with positional remaining/capacity/% props (DRG-261).</summary>
public sealed record MagazineLoadoutRowLabels(
    string DisplayLine,
    int Remaining,
    int Capacity,
    double FillPct,
    string StatusLine);

/// <summary>Label bundle for MagazineLoadout panel hosts (DRG-261 bind path).</summary>
public sealed record MagazineLoadoutPanelLabels(
    string HeaderLine,
    string EmptyStateLine,
    string FeasibilityLine,
    IReadOnlyList<MagazineLoadoutRowLabels> Rows,
    string Fingerprint,
    bool HasMagazineData);

/// <summary>
/// Maps magazine projection rows to panel labels without re-deriving sim facts.
/// Presentation-only — ADR-010 §2–3, ADR-007, ADR-001.
/// </summary>
public static class MagazineLoadoutPanelBinder
{
    /// <summary>Binds immutable presentation rows for UI Toolkit labels and ListView rows.</summary>
    public static MagazineLoadoutPanelLabels Bind(
        MagazineLoadoutPresentation presentation,
        int roundsPerAirframe)
    {
        var rows = new MagazineLoadoutRowLabels[presentation.Rows.Count];
        for (var i = 0; i < presentation.Rows.Count; i++)
        {
            var row = presentation.Rows[i];
            rows[i] = new MagazineLoadoutRowLabels(
                row.DisplayLine,
                row.Remaining,
                row.Capacity,
                row.FillPct,
                row.StatusLine);
        }

        var feasibility = !presentation.HasMagazineData || presentation.Rows.Count == 0
            ? string.Empty
            : MagazineLoadoutApplyState.FormatFeasibilityLine(
                presentation.Aggregate.TotalRemaining,
                roundsPerAirframe);

        return new MagazineLoadoutPanelLabels(
            presentation.HeaderLine,
            presentation.EmptyStateLine,
            feasibility,
            rows,
            MagazineLoadoutPresenter.ComputeFingerprint(presentation),
            presentation.HasMagazineData);
    }
}

/// <summary>Headless magazine loadout presenter for Play Mode live-bind (DRG-261).</summary>
public static class MagazineLoadoutPresenter
{
    /// <summary>Builds presentation from bridge-cached magazine entries.</summary>
    public static MagazineLoadoutPresentation Build(
        IReadOnlyList<MagazineLoadoutEntry>? entries,
        bool hasMagazineData) =>
        MagazineLoadoutApplyState.Apply(entries, hasMagazineData);

    /// <summary>Replay-stable fingerprint for unchanged-frame short-circuit in hosts.</summary>
    public static string ComputeFingerprint(
        IReadOnlyList<MagazineLoadoutEntry>? entries,
        bool hasMagazineData)
    {
        if (!hasMagazineData)
        {
            return "ml:no-data";
        }

        if (entries is null || entries.Count == 0)
        {
            return "ml:empty";
        }

        var builder = new StringBuilder();
        builder.Append("ml:r=");
        builder.Append(entries.Count);
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            builder.Append('|');
            builder.Append(entry.UnitId);
            builder.Append(',');
            builder.Append(entry.MountId);
            builder.Append(',');
            builder.Append(entry.WeaponId);
            builder.Append(',');
            builder.Append(entry.Remaining.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(entry.Capacity.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(entry.FillPct.ToString("0.##", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(entry.StatusLine);
        }

        return builder.ToString();
    }

    /// <summary>Fingerprint from an applied presentation bundle.</summary>
    public static string ComputeFingerprint(MagazineLoadoutPresentation presentation)
    {
        if (!presentation.HasMagazineData)
        {
            return "ml:no-data";
        }

        if (presentation.Rows.Count == 0)
        {
            return "ml:empty";
        }

        var builder = new StringBuilder();
        builder.Append("ml:r=");
        builder.Append(presentation.Rows.Count);
        for (var i = 0; i < presentation.Rows.Count; i++)
        {
            var row = presentation.Rows[i];
            builder.Append('|');
            builder.Append(row.UnitId);
            builder.Append(',');
            builder.Append(row.MountId);
            builder.Append(',');
            builder.Append(row.WeaponId);
            builder.Append(',');
            builder.Append(row.Remaining.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(row.Capacity.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(row.FillPct.ToString("0.##", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(row.StatusLine);
        }

        return builder.ToString();
    }
}
