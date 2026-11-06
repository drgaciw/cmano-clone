namespace ProjectAegis.Delegation.TrackCustody;

using System.Globalization;
using System.Text;

/// <summary>Replay-stable canonical fingerprint for track custody snapshots (DRG-222).</summary>
public static class TrackCustodyFingerprint
{
    /// <summary>
    /// Same inputs yield the same string. Invariant culture; ordinal ordering; no wall clock.
    /// </summary>
    public static string Compute(TrackCustodySnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Rows.Count == 0 && snapshot.Entries.Count == 0)
        {
            return "tc:empty";
        }

        var builder = new StringBuilder();
        builder.Append("tc:r=");
        builder.Append(snapshot.Rows.Count);
        for (var i = 0; i < snapshot.Rows.Count; i++)
        {
            AppendRow(builder, snapshot.Rows[i]);
        }

        builder.Append("#e=");
        builder.Append(snapshot.Entries.Count);
        for (var i = 0; i < snapshot.Entries.Count; i++)
        {
            AppendEntry(builder, snapshot.Entries[i]);
        }

        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, TrackCustodyRow row)
    {
        builder.Append('|');
        builder.Append(row.ContactId);
        builder.Append(',');
        builder.Append(row.TargetId);
        builder.Append(',');
        builder.Append(row.ObserverId);
        builder.Append(',');
        builder.Append((int)row.Custody);
        builder.Append(',');
        builder.Append((int)row.Cause);
        builder.Append(',');
        builder.Append(row.LastKnownTick);
        builder.Append(',');
        builder.Append(row.LastKnownSimTime.ToString("R", CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(row.CorrelationSequenceId);
    }

    private static void AppendEntry(StringBuilder builder, TrackCustodyLedgerEntry entry)
    {
        builder.Append('|');
        builder.Append(entry.ContactId);
        builder.Append(',');
        builder.Append(entry.TargetId);
        builder.Append(',');
        builder.Append(entry.ObserverId);
        builder.Append(',');
        builder.Append((int)entry.Custody);
        builder.Append(',');
        builder.Append((int)entry.Cause);
        builder.Append(',');
        builder.Append(entry.SimTick);
        builder.Append(',');
        builder.Append(entry.SimTime.ToString("R", CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(entry.CorrelationSequenceId);
    }
}
