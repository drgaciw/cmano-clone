namespace ProjectAegis.Delegation.Projection;

using System.Collections.ObjectModel;
using Decision;

/// <summary>
/// Read-only operator override state per contact (DRG-272 / C2-22), folded chronologically
/// from <see cref="DecisionLog.ContactOverrides"/> without mutating sensor contact truth.
/// </summary>
public sealed record ContactOverrideEntry(
    string ContactId,
    string? AffiliationOverride,
    bool IsDropped,
    ulong LastSimTick,
    ulong LastSequenceId)
{
    public string StatusLabel =>
        IsDropped
            ? "OVERRIDE: DROPPED (operator)"
            : !string.IsNullOrEmpty(AffiliationOverride)
                ? $"OVERRIDE: RECLASSIFIED → {AffiliationOverride}"
                : "OVERRIDE: —";
}

/// <summary>Snapshot of all active operator contact overrides.</summary>
public sealed record ContactOverrideSnapshot(IReadOnlyList<ContactOverrideEntry> Overrides)
{
    public static ContactOverrideSnapshot Empty { get; } = new(Array.Empty<ContactOverrideEntry>());

    public ContactOverrideEntry? Find(string? contactId)
    {
        if (string.IsNullOrEmpty(contactId))
        {
            return null;
        }

        for (var i = 0; i < Overrides.Count; i++)
        {
            if (string.Equals(Overrides[i].ContactId, contactId, StringComparison.Ordinal))
            {
                return Overrides[i];
            }
        }

        return null;
    }

    public IReadOnlyDictionary<string, string> ToAffiliationMap()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in Overrides)
        {
            if (!entry.IsDropped && !string.IsNullOrEmpty(entry.AffiliationOverride))
            {
                map[entry.ContactId] = entry.AffiliationOverride!;
            }
        }

        return new ReadOnlyDictionary<string, string>(map);
    }

    public IReadOnlyCollection<string> ToDroppedContactIds()
    {
        var dropped = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in Overrides)
        {
            if (entry.IsDropped)
            {
                dropped.Add(entry.ContactId);
            }
        }

        return dropped;
    }
}

/// <summary>Pure projection folding <see cref="ContactOverrideRecord"/> rows into <see cref="ContactOverrideSnapshot"/>.</summary>
public static class ContactOverrideProjection
{
    public static ContactOverrideSnapshot Project(DecisionLog? log) =>
        log is null ? ContactOverrideSnapshot.Empty : Project(log.ContactOverrides);

    public static ContactOverrideSnapshot Project(IReadOnlyList<ContactOverrideRecord>? records)
    {
        if (records is null || records.Count == 0)
        {
            return ContactOverrideSnapshot.Empty;
        }

        var folded = new Dictionary<string, ContactOverrideEntry>(StringComparer.Ordinal);
        foreach (var record in records.OrderBy(r => r.SimTick).ThenBy(r => r.SequenceId))
        {
            if (string.IsNullOrEmpty(record.ContactId))
            {
                continue;
            }

            folded.TryGetValue(record.ContactId, out var existing);
            folded[record.ContactId] = record.Action switch
            {
                ContactOverrideAction.Drop => new ContactOverrideEntry(
                    record.ContactId,
                    existing?.AffiliationOverride,
                    IsDropped: true,
                    record.SimTick,
                    record.SequenceId),
                ContactOverrideAction.Reclassify => new ContactOverrideEntry(
                    record.ContactId,
                    MilitarySymbology.NormalizeAffiliation(record.NewValue),
                    IsDropped: false,
                    record.SimTick,
                    record.SequenceId),
                _ => existing ?? new ContactOverrideEntry(
                    record.ContactId, null, false, record.SimTick, record.SequenceId),
            };
        }

        var ordered = folded.Values
            .OrderBy(e => e.ContactId, StringComparer.Ordinal)
            .ToArray();
        return ordered.Length == 0
            ? ContactOverrideSnapshot.Empty
            : new ContactOverrideSnapshot(Array.AsReadOnly(ordered));
    }
}
