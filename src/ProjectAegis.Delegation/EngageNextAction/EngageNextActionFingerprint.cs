namespace ProjectAegis.Delegation.EngageNextAction;

using System.Text;

/// <summary>Replay-stable canonical fingerprint for next-action snapshots (DRG-226).</summary>
public static class EngageNextActionFingerprint
{
    /// <summary>
    /// Same inputs yield the same string. Invariant culture; ordinal ordering; no wall clock.
    /// </summary>
    public static string Compute(EngageNextActionSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Rows.Count == 0)
        {
            return "ena:empty";
        }

        var builder = new StringBuilder();
        builder.Append("ena:r=");
        builder.Append(snapshot.Rows.Count);
        builder.Append('|');
        builder.Append(snapshot.IsFireOrder ? '1' : '0');
        for (var i = 0; i < snapshot.Rows.Count; i++)
        {
            AppendRow(builder, snapshot.Rows[i]);
        }

        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, EngageNextActionRow row)
    {
        builder.Append('|');
        builder.Append(row.ShooterId);
        builder.Append(',');
        builder.Append(row.WeaponFamilyId);
        builder.Append(',');
        builder.Append(row.WithholdReason ?? string.Empty);
        builder.Append(',');
        builder.Append(row.NextActionCode ?? string.Empty);
        builder.Append(',');
        builder.Append(row.IsFireOrder ? '1' : '0');
    }
}
