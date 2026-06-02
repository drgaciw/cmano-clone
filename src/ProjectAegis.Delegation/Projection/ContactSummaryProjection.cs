namespace ProjectAegis.Delegation.Projection;

public sealed record ContactSummaryEntry(
    string ContactId,
    string TargetId,
    string LifecycleState,
    string DisplayLine);

public static class ContactSummaryProjection
{
    public static ContactSummaryEntry? Project(
        string contactId,
        IReadOnlyList<ContactPictureEntry> contacts)
    {
        var match = contacts.FirstOrDefault(c => c.ContactId == contactId);
        return match == null
            ? null
            : new ContactSummaryEntry(
                match.ContactId,
                match.TargetId,
                match.LifecycleState,
                $"CONTACT {match.ContactId} → {match.TargetId} ({match.LifecycleState})");
    }
}