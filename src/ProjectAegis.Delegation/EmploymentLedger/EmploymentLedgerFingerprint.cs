namespace ProjectAegis.Delegation.EmploymentLedger;

using System.Globalization;
using System.Text;

/// <summary>Replay-stable canonical fingerprint for employment ledger snapshots (DRG-224).</summary>
public static class EmploymentLedgerFingerprint
{
    /// <summary>
    /// Same inputs yield the same string. Invariant culture; ordinal ordering; no wall clock.
    /// </summary>
    public static string Compute(EmploymentLedgerSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Rows.Count == 0)
        {
            return "el:empty";
        }

        var builder = new StringBuilder();
        builder.Append("el:r=");
        builder.Append(snapshot.Rows.Count);
        builder.Append('|');
        builder.Append(snapshot.IsFireOrder ? '1' : '0');
        for (var i = 0; i < snapshot.Rows.Count; i++)
        {
            AppendRow(builder, snapshot.Rows[i]);
        }

        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, EmploymentLedgerRow row)
    {
        builder.Append('|');
        builder.Append(row.ShooterId);
        builder.Append(',');
        builder.Append(row.WeaponFamilyId);
        builder.Append(',');
        builder.Append(row.RoundsRemaining);
        builder.Append(',');
        builder.Append(row.SalvoSize);
        builder.Append(',');
        builder.Append(row.LastEmploymentTick);
        builder.Append(',');
        builder.Append(row.WithholdReason ?? string.Empty);
        builder.Append(',');
        builder.Append(row.IsFireOrder ? '1' : '0');
    }
}
