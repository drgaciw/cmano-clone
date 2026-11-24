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
            ClassificationLine: entry.ClassificationLine,
            ConfidenceLine: entry.ConfidenceLine,
            DetectionProvenanceLine: entry.DetectionProvenanceLine,
            WraLine: entry.WraLine,
            BdaLine: entry.BdaLine,
            StalenessLine: entry.StalenessLine,
            LifecycleLine: $"STATE: {entry.LifecycleState}",
            OverrideStatusLine: entry.OverrideStatusLine,
            OperatorAffiliation: entry.OperatorAffiliation,
            IsOperatorDropped: entry.IsOperatorDropped);
    }

    public static ContactDetailPresentation ProjectAndApply(
        string? contactId,
        IReadOnlyList<ContactPictureEntry> contacts,
        ulong currentSimTick,
        string? bdaLifecycleOverride = null,
        ContactOverrideEntry? operatorOverride = null)
    {
        if (string.IsNullOrEmpty(contactId))
        {
            return ContactDetailPresentation.Empty;
        }

        var entry = ContactDetailProjection.Project(
            contactId,
            contacts,
            currentSimTick,
            bdaLifecycleOverride,
            operatorOverride);
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
    string LifecycleLine,
    string OverrideStatusLine = "OVERRIDE: —",
    string? OperatorAffiliation = null,
    bool IsOperatorDropped = false)
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
