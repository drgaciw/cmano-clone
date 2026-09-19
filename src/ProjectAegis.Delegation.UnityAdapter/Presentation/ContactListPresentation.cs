using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Sensors;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Headless contact-list sort/filter for C2 drawer and sensor HUD hosts (ADR-010 §2–3).
/// Operates on read-only <see cref="SensorC2Snapshot"/> rows — no sim mutation.
/// </summary>
public static class ContactListPresentation
{
    /// <summary>Apply chrome sort/filter and return display rows plus a filtered count label.</summary>
    public static ContactListPanelView Apply(SensorC2Snapshot snapshot, ContactListChromeState chrome)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        chrome ??= ContactListChromeState.Default;

        var filtered = Filter(snapshot.Contacts, chrome);
        var sorted = Sort(filtered, chrome.SortKey);
        var rows = new List<SensorC2ContactRow>(sorted.Count);
        foreach (var contact in sorted)
        {
            rows.Add(new SensorC2ContactRow(
                contact.ContactId,
                contact.LifecycleState,
                contact.TargetId,
                FormatContactLine(contact.ContactId, contact.LifecycleState, contact.TargetId)));
        }

        var total = snapshot.Contacts.Count;
        var visible = rows.Count;
        var countLabel = visible == total
            ? $"CONTACTS: {total}"
            : $"CONTACTS: {visible}/{total}";

        return new ContactListPanelView(countLabel, rows);
    }

    private static List<ContactPictureEntry> Filter(
        IReadOnlyList<ContactPictureEntry> contacts,
        ContactListChromeState chrome)
    {
        var result = new List<ContactPictureEntry>(contacts.Count);
        foreach (var contact in contacts)
        {
            if (!MatchesLifecycleFilter(contact, chrome.LifecycleFilter))
            {
                continue;
            }

            if (!MatchesTextFilter(contact, chrome.TextFilter))
            {
                continue;
            }

            result.Add(contact);
        }

        return result;
    }

    private static bool MatchesLifecycleFilter(ContactPictureEntry contact, ContactListLifecycleFilter filter) =>
        filter switch
        {
            ContactListLifecycleFilter.All => true,
            ContactListLifecycleFilter.Classified => string.Equals(
                contact.LifecycleState,
                "Classified",
                StringComparison.Ordinal),
            ContactListLifecycleFilter.Identified => string.Equals(
                contact.LifecycleState,
                "Identified",
                StringComparison.Ordinal),
            ContactListLifecycleFilter.Hostile => HostileContactFilter.IsEngageableHostileTarget(contact.TargetId),
            _ => true,
        };

    private static bool MatchesTextFilter(ContactPictureEntry contact, string? textFilter)
    {
        if (string.IsNullOrWhiteSpace(textFilter))
        {
            return true;
        }

        var needle = textFilter.Trim();
        return contact.ContactId.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || contact.TargetId.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || contact.LifecycleState.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private static List<ContactPictureEntry> Sort(
        List<ContactPictureEntry> contacts,
        ContactListSortKey sortKey)
    {
        contacts.Sort((left, right) => Compare(left, right, sortKey));
        return contacts;
    }

    private static int Compare(ContactPictureEntry left, ContactPictureEntry right, ContactListSortKey sortKey) =>
        sortKey switch
        {
            ContactListSortKey.Lifecycle => string.Compare(
                left.LifecycleState,
                right.LifecycleState,
                StringComparison.Ordinal),
            ContactListSortKey.Age => right.LastSimTick.CompareTo(left.LastSimTick),
            ContactListSortKey.Threat => CompareThreat(left.TargetId, right.TargetId),
            _ => string.Compare(left.ContactId, right.ContactId, StringComparison.Ordinal),
        };

    private static int CompareThreat(string leftTargetId, string rightTargetId)
    {
        var leftHostile = HostileContactFilter.IsEngageableHostileTarget(leftTargetId);
        var rightHostile = HostileContactFilter.IsEngageableHostileTarget(rightTargetId);
        var hostileCompare = rightHostile.CompareTo(leftHostile);
        return hostileCompare != 0
            ? hostileCompare
            : string.Compare(leftTargetId, rightTargetId, StringComparison.Ordinal);
    }

    private static string FormatContactLine(string contactId, string state, string targetId) =>
        $"{contactId}  {state}  → {targetId}";
}

/// <summary>Filtered contact list rows plus count label for panel binding.</summary>
public sealed record ContactListPanelView(string ContactCountLabel, IReadOnlyList<SensorC2ContactRow> ContactRows);
