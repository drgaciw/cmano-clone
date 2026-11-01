namespace ProjectAegis.Delegation.Projection;

using System.Globalization;
using System.Text;
using Core;

/// <summary>Replay-stable fingerprint for <see cref="ContactCueSnapshot"/> (DRG-271). Not a SimWorldHash.</summary>
public static class ContactCueFingerprint
{
    /// <summary>Same inputs yield the same string. Invariant culture; no wall clock.</summary>
    public static string Compute(ContactCueSnapshot? snapshot)
    {
        if (snapshot is null || (snapshot.Contacts.Count == 0 && snapshot.UnitOrders.Count == 0))
        {
            return "cue:empty";
        }

        var builder = new StringBuilder();
        builder.Append("cue:c=");
        builder.Append(snapshot.Contacts.Count);
        builder.Append(";u=");
        builder.Append(snapshot.UnitOrders.Count);
        for (var i = 0; i < snapshot.Contacts.Count; i++)
        {
            AppendContact(builder, snapshot.Contacts[i]);
        }

        for (var i = 0; i < snapshot.UnitOrders.Count; i++)
        {
            AppendUnit(builder, snapshot.UnitOrders[i]);
        }

        return builder.ToString();
    }

    private static void AppendContact(StringBuilder builder, ContactCueState contact)
    {
        builder.Append('|');
        builder.Append(contact.ContactId);
        builder.Append(',');
        builder.Append((int)contact.Confidence);
        builder.Append(',');
        builder.Append((int)contact.Freshness);
        builder.Append(',');
        AppendUlong(builder, contact.SimAgeTicks);
        builder.Append(',');
        AppendDouble(builder, contact.AgeSimTime);
        builder.Append(',');
        builder.Append((int)contact.Comms);
        builder.Append(',');
        builder.Append(contact.OutOfCommsUnknown ? '1' : '0');
        builder.Append(',');
        builder.Append((int)contact.QualityState);
        builder.Append(',');
        builder.Append((int)contact.Origin);
        builder.Append(',');
        builder.Append(contact.IsDatalinkSourced ? '1' : '0');
        builder.Append(',');
        builder.Append(contact.IsStale ? '1' : '0');
        builder.Append(',');
        builder.Append((int)contact.Position.Kind);
        builder.Append(',');
        AppendDouble(builder, contact.Position.EastMeters);
        builder.Append(',');
        AppendDouble(builder, contact.Position.NorthMeters);
        builder.Append(',');
        AppendUlong(builder, contact.Position.ReportedSimTick);
        builder.Append(',');
        builder.Append(contact.Position.FixtureLabel ?? "-");
        builder.Append(',');
        builder.Append(contact.Position.IsFreshFix ? '1' : '0');
    }

    private static void AppendUnit(StringBuilder builder, UnitOrderQueueState unit)
    {
        builder.Append('|');
        builder.Append(unit.UnitId);
        builder.Append(',');
        builder.Append((int)unit.Disposition);
        builder.Append(',');
        if (unit.Kind is { } kind)
        {
            builder.Append((int)kind);
        }
        else
        {
            builder.Append('-');
        }

        builder.Append(',');
        AppendUlong(builder, unit.IssuedSimTick);
        builder.Append(',');
        AppendUlong(builder, unit.ExecuteSimTick);
        builder.Append(',');
        builder.Append(unit.IsGenuinelyPending ? '1' : '0');
    }

    private static void AppendUlong(StringBuilder builder, ulong? value)
    {
        if (value is { } number)
        {
            builder.Append(number);
            return;
        }

        builder.Append('-');
    }

    private static void AppendDouble(StringBuilder builder, double? value)
    {
        if (value is { } number)
        {
            builder.Append(number.ToString("R", CultureInfo.InvariantCulture));
            return;
        }

        builder.Append('-');
    }
}
