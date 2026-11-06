namespace ProjectAegis.Delegation.EscalationGate;

using System.Globalization;
using System.Text;

/// <summary>Replay-stable canonical fingerprint for escalation gate snapshots (DRG-228).</summary>
public static class EscalationGateFingerprint
{
  /// <summary>
  /// Same inputs yield the same string. Invariant culture; ordinal ordering; no wall clock.
  /// </summary>
  public static string Compute(EscalationGateSnapshot? snapshot)
  {
    if (snapshot is null || snapshot.Rows.Count == 0)
    {
      return "eg:empty";
    }

    var builder = new StringBuilder();
    builder.Append("eg:r=");
    builder.Append(snapshot.Rows.Count);
    builder.Append('|');
    builder.Append(snapshot.IsOrder ? '1' : '0');
    for (var i = 0; i < snapshot.Rows.Count; i++)
    {
      AppendRow(builder, snapshot.Rows[i]);
    }

    return builder.ToString();
  }

  private static void AppendRow(StringBuilder builder, EscalationGateRow row)
  {
    builder.Append('|');
    builder.Append(row.ContactOrOrderId);
    builder.Append(',');
    builder.Append(row.GateCode);
    builder.Append(',');
    builder.Append(((int)row.RequiredAuthority).ToString(CultureInfo.InvariantCulture));
    builder.Append(',');
    builder.Append(row.ReasonCode);
    builder.Append(',');
    builder.Append(row.IsOrder ? '1' : '0');
  }
}
