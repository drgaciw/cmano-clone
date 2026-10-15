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
        if (snapshot is null
            || (snapshot.Events.Count == 0 && snapshot.Targetability.Count == 0 && snapshot.Execution.Count == 0))
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

        // Fact segments are appended only when present so event-only fingerprints keep their DRG-211 shape.
        if (snapshot.Targetability.Count > 0)
        {
            builder.Append("|ta=");
            builder.Append(snapshot.Targetability.Count);
            for (var i = 0; i < snapshot.Targetability.Count; i++)
            {
                AppendTargetability(builder, snapshot.Targetability[i]);
            }
        }

        if (snapshot.Execution.Count > 0)
        {
            builder.Append("|ex=");
            builder.Append(snapshot.Execution.Count);
            for (var i = 0; i < snapshot.Execution.Count; i++)
            {
                AppendExecution(builder, snapshot.Execution[i]);
            }
        }

        return builder.ToString();
    }

    private static void AppendTargetability(StringBuilder builder, CombatTargetabilityFact fact)
    {
        builder.Append(';');
        builder.Append(fact.ContactId);
        builder.Append(',');
        builder.Append(fact.TargetId);
        builder.Append(',');
        builder.Append((int)fact.Disposition);
        builder.Append(',');
        builder.Append(fact.WithheldCauseCode);
        builder.Append(',');
        builder.Append((int)fact.Confidence);
        builder.Append(',');
        builder.Append(FormatNullable(fact.SensorToShooterComplete));
        builder.Append(',');
        builder.Append(fact.RoeAllowsEngage ? '1' : '0');
        builder.Append(',');
        builder.Append((int)fact.TargetingDisposition);
        builder.Append(',');
        builder.Append(fact.TargetingReasonCode ?? string.Empty);
        builder.Append(',');
        builder.Append(fact.ShooterId ?? string.Empty);
    }

    private static void AppendExecution(StringBuilder builder, CombatExecutionFact fact)
    {
        builder.Append(';');
        builder.Append(fact.CorrelationId);
        builder.Append(',');
        builder.Append(fact.ShooterId);
        builder.Append(',');
        builder.Append(fact.TargetId);
        builder.Append(',');
        builder.Append(fact.WeaponFamilyId);
        builder.Append(',');
        builder.Append(FormatNullable(fact.HasFireControlTrack));
        builder.Append(',');
        builder.Append(fact.SalvoSize);
        builder.Append(',');
        builder.Append(fact.SimTime.ToString("R", CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(fact.SimTick);
    }

    private static char FormatNullable(bool? value) => value switch
    {
        true => '1',
        false => '0',
        null => '?',
    };

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
