namespace ProjectAegis.Delegation.IdentityClass;

using System.Text;

/// <summary>Replay-stable canonical fingerprint for identity classification snapshots (DRG-225).</summary>
public static class IdentityClassFingerprint
{
    /// <summary>
    /// Same inputs yield the same string. Invariant culture; ordinal ordering; no wall clock.
    /// </summary>
    public static string Compute(IdentityClassSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Rows.Count == 0)
        {
            return "ic:empty";
        }

        var builder = new StringBuilder();
        builder.Append("ic:r=");
        builder.Append(snapshot.Rows.Count);
        for (var i = 0; i < snapshot.Rows.Count; i++)
        {
            AppendRow(builder, snapshot.Rows[i]);
        }

        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, IdentityClassRow row)
    {
        builder.Append('|');
        builder.Append(row.ContactId);
        builder.Append(',');
        builder.Append((int)row.Classification);
        builder.Append(',');
        builder.Append(row.ReasonCode);
        builder.Append(',');
        builder.Append((int)row.ConfidenceBand);
        builder.Append(',');
        builder.Append(row.SimTick);
    }
}
