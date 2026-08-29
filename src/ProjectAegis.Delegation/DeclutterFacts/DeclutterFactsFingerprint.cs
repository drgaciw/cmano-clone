namespace ProjectAegis.Delegation.DeclutterFacts;

using System.Globalization;
using System.Text;

/// <summary>Replay-stable canonical fingerprint for declutter aggregation snapshots (DRG-230).</summary>
public static class DeclutterFactsFingerprint
{
    /// <summary>
    /// Same inputs yield the same string. Invariant culture; ordinal ordering; no wall clock.
    /// </summary>
    public static string Compute(DeclutterFactsSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Rows.Count == 0)
        {
            return "df:empty";
        }

        var builder = new StringBuilder();
        builder.Append("df:r=");
        builder.Append(snapshot.Rows.Count);
        builder.Append('|');
        builder.Append(snapshot.IsFireOrder ? '1' : '0');
        for (var i = 0; i < snapshot.Rows.Count; i++)
        {
            AppendRow(builder, snapshot.Rows[i]);
        }

        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, DeclutterFactsRow row)
    {
        builder.Append('|');
        builder.Append(row.WeaponFamilyId);
        builder.Append(',');
        builder.Append(row.Count.ToString(CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(row.ZoomBandToken);
        builder.Append(',');
        builder.Append(row.IsFireOrder ? '1' : '0');
    }
}
