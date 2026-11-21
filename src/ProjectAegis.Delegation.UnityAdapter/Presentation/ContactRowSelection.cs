using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>UAT D23: resolves a contact-list row (ListView selection index) to its contact id.</summary>
public static class ContactRowSelection
{
    public static bool TryResolveContactId(IReadOnlyList<SensorC2ContactRow> rows, int index, out string? contactId)
    {
        if (index < 0 || index >= rows.Count)
        {
            contactId = null;
            return false;
        }

        contactId = rows[index].ContactId;
        return true;
    }
}
