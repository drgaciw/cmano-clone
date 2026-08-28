namespace ProjectAegis.Delegation.CombatEvents;

using System.Globalization;
using System.Text;

/// <summary>Replay-stable canonical fingerprint for combat-event snapshots (DRG-211).</summary>
public static class CombatEventFingerprint
{
    /// <summary>
    /// Same inputs yield the same string. Invariant culture; ordinal ordering; no wall clock.
    /// </summary>
    public static string Compute(CombatEventSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Events.Count == 0)
        {
            return "ce:empty";
        }

        var builder = new StringBuilder();
        builder.Append("ce:e=");
        builder.Append(snapshot.Events.Count);
        for (var i = 0; i < snapshot.Events.Count; i++)
        {
            AppendEvent(builder, snapshot.Events[i]);
        }

        return builder.ToString();
    }

    private static void AppendEvent(StringBuilder builder, CombatEvent evt)
    {
        builder.Append('|');
        builder.Append((int)evt.Phase);
        builder.Append(',');
        builder.Append(evt.ShooterId);
        builder.Append(',');
        builder.Append(evt.TargetId);
        builder.Append(',');
        builder.Append(evt.WeaponFamilyId);
        builder.Append(',');
        builder.Append(evt.Outcome);
        builder.Append(',');
        builder.Append(evt.CorrelationId);
        builder.Append(',');
        builder.Append(evt.SimTime.ToString("R", CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(evt.SimTick);
        builder.Append(',');
        builder.Append(evt.ExplanationRef);
    }
}
