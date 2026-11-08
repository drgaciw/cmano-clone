namespace ProjectAegis.Delegation.Projection;

using System.Text;

/// <summary>Replay-stable canonical fingerprint for weapon-contact ambiguity snapshots (DRG-411).</summary>
public static class WeaponContactAmbiguityFingerprint
{
    /// <summary>
    /// Same inputs yield the same string. Invariant culture; ordinal ordering; no wall clock.
    /// </summary>
    public static string Compute(WeaponContactAmbiguitySnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Rows.Count == 0)
        {
            return "wca:empty";
        }

        var builder = new StringBuilder();
        builder.Append("wca:n=");
        builder.Append(snapshot.Rows.Count);
        for (var i = 0; i < snapshot.Rows.Count; i++)
        {
            AppendRow(builder, snapshot.Rows[i]);
        }

        return builder.ToString();
    }

    internal static string RowKey(WeaponContactAmbiguityRow row)
    {
        var builder = new StringBuilder();
        AppendRow(builder, row);
        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, WeaponContactAmbiguityRow row)
    {
        builder.Append('|');
        builder.Append(row.WeaponId);
        builder.Append(',');
        builder.Append(row.ContactId);
        builder.Append(',');
        AppendMeasurement(builder, row.DownrangeAmbiguity);
        builder.Append(',');
        AppendMeasurement(builder, row.CrossRangeAmbiguity);
        builder.Append(',');
        AppendMeasurement(builder, row.WeaponTolerance);
        builder.Append(',');
        builder.Append((int)row.Doctrine);
        builder.Append(',');
        builder.Append((int)row.Verdict);
        builder.Append(',');
        builder.Append(row.RefusalText ?? "-");
    }

    private static void AppendMeasurement(StringBuilder builder, MeasuredNauticalMiles measurement)
    {
        if (!measurement.TryGetNauticalMiles(out var value))
        {
            builder.Append('U');
            return;
        }

        builder.Append('K');
        builder.Append(WeaponContactAmbiguityProjection.FormatNauticalMiles(value));
    }
}
