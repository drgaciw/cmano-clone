namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for <see cref="ContactDetailEntry"/> (CMD-29).
/// Unity hosts map presentation fields onto labels without re-formatting.
/// </summary>
public static class ContactDetailApplyState
{
    public static ContactDetailPresentation Apply(ContactDetailEntry? entry)
    {
        if (entry is null)
        {
            return ContactDetailPresentation.Empty;
        }

        return new ContactDetailPresentation(
            ContactIdLine: $"CONTACT: {entry.ContactId}",
            TargetIdLine: $"TARGET: {entry.TargetId}",
            ClassificationLine: entry.ClassificationLine ?? string.Empty,
            ConfidenceLine: entry.ConfidenceLine ?? string.Empty,
            DetectionProvenanceLine: entry.DetectionProvenanceLine ?? string.Empty,
            WraLine: entry.WraLine ?? string.Empty,
            BdaLine: entry.BdaLine ?? string.Empty,
            StalenessLine: entry.StalenessLine ?? string.Empty,
            LifecycleLine: $"STATE: {entry.LifecycleState}");
    }

    public static ContactDetailPresentation ProjectAndApply(
        string? contactId,
        IReadOnlyList<ContactPictureEntry> contacts,
        ulong currentSimTick,
        string? bdaLifecycleOverride = null)
    {
        if (string.IsNullOrEmpty(contactId))
        {
            return ContactDetailPresentation.Empty;
        }

        var entry = ContactDetailProjection.Project(
            contactId!,
            contacts,
            currentSimTick,
            bdaLifecycleOverride);
        return Apply(entry);
    }
}

/// <summary>Applied contact-detail presentation fields (label text bags).</summary>
public sealed record ContactDetailPresentation(
    string ContactIdLine,
    string TargetIdLine,
    string ClassificationLine,
    string ConfidenceLine,
    string DetectionProvenanceLine,
    string WraLine,
    string BdaLine,
    string StalenessLine,
    string LifecycleLine)
{
    public static ContactDetailPresentation Empty { get; } = new(
        "CONTACT: —",
        "TARGET: —",
        "CLASS: —",
        "CONF: —",
        "Detected by: —",
        "WRA: —",
        "BDA: —",
        "— ticks stale",
        "STATE: —");
}
