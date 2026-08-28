namespace ProjectAegis.Delegation.Projection;

using System.Globalization;
using System.Text;

/// <summary>Replay-stable canonical fingerprint for contact provenance snapshots (DRG-206).</summary>
public static class ContactProvenanceFingerprint
{
    /// <summary>
    /// Same inputs yield the same string. Invariant culture; ordinal ordering; no wall clock.
    /// </summary>
    public static string Compute(ContactProvenanceSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Contacts.Count == 0)
        {
            return "cp:empty";
        }

        var builder = new StringBuilder();
        builder.Append("cp:c=");
        builder.Append(snapshot.Contacts.Count);
        for (var i = 0; i < snapshot.Contacts.Count; i++)
        {
            AppendContact(builder, snapshot.Contacts[i]);
        }

        return builder.ToString();
    }

    private static void AppendContact(StringBuilder builder, ContactProvenanceState contact)
    {
        builder.Append('|');
        builder.Append(contact.ContactId);
        builder.Append(',');
        builder.Append(contact.Source.ObserverId);
        builder.Append(',');
        builder.Append(contact.Source.TargetId);
        builder.Append(',');
        builder.Append(contact.Source.SourceRef);
        builder.Append(',');
        builder.Append((int)contact.Confidence);
        builder.Append(',');
        builder.Append((int)contact.Freshness);
        builder.Append(',');
        builder.Append(contact.AgeTicks);
        builder.Append(',');
        builder.Append(contact.LastKnown.LifecycleState);
        builder.Append(',');
        builder.Append(contact.LastKnown.TargetId);
        builder.Append(',');
        builder.Append(contact.LastKnown.LastSimTick);
        builder.Append(',');
        builder.Append(contact.LastKnown.LastSimTime.ToString("R", CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(contact.OutOfCommsUnknown ? '1' : '0');
        builder.Append(',');
        builder.Append((int)contact.QualityState);
    }
}
