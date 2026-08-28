namespace ProjectAegis.Delegation.CdeAssess;

using System.Globalization;
using System.Text;

/// <summary>Replay-stable canonical fingerprint for collateral/CDE advisory snapshots (DRG-220).</summary>
public static class CdeAssessFingerprint
{
    /// <summary>
    /// Same inputs yield the same string. Invariant culture; ordinal ordering; no wall clock.
    /// String fields are length-prefixed (<c>len:value</c>) so commas/pipes inside ids cannot collide.
    /// </summary>
    public static string Compute(CdeAssessSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Rows.Count == 0)
        {
            return "cde:empty";
        }

        var builder = new StringBuilder();
        builder.Append("cde:r=");
        builder.Append(snapshot.Rows.Count);
        for (var i = 0; i < snapshot.Rows.Count; i++)
        {
            AppendRow(builder, snapshot.Rows[i]);
        }

        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, CdeAssessRow row)
    {
        builder.Append('|');
        builder.Append((int)row.RiskKind);
        builder.Append(',');
        AppendLengthPrefixed(builder, row.ShooterId);
        builder.Append(',');
        AppendLengthPrefixed(builder, row.TargetId);
        builder.Append(',');
        AppendLengthPrefixed(builder, row.WeaponFamilyId);
        builder.Append(',');
        builder.Append(row.CorrelationId);
        builder.Append(',');
        builder.Append(row.SimTime.ToString("R", CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(row.SimTick);
        builder.Append(',');
        AppendLengthPrefixed(builder, row.GeometryRangeClass);
        builder.Append(',');
        AppendLengthPrefixed(builder, row.PolicyConstraintText);
        builder.Append(',');
        AppendLengthPrefixed(builder, row.WithholdReason ?? string.Empty);
        builder.Append(',');
        builder.Append(row.Assumptions.Count);
        for (var i = 0; i < row.Assumptions.Count; i++)
        {
            builder.Append(',');
            AppendLengthPrefixed(builder, row.Assumptions[i]);
        }
    }

    /// <summary>Length-prefixed field encoding — prevents <c>,</c>/<c>|</c> delimiter collisions.</summary>
    private static void AppendLengthPrefixed(StringBuilder builder, string value)
    {
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value);
    }
}
