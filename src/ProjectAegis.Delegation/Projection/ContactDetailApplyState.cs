namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Comms;

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
            LifecycleLine: $"STATE: {entry.LifecycleState}",
            SourceLine: entry.SourceLine ?? string.Empty,
            CommsLine: entry.CommsLine ?? string.Empty,
            LastKnownLine: entry.LastKnownLine ?? string.Empty,
            ExplainLinkLine: entry.ExplainLinkLine ?? string.Empty);
    }

    public static ContactDetailPresentation ProjectAndApply(
        string? contactId,
        IReadOnlyList<ContactPictureEntry> contacts,
        ulong currentSimTick,
        string? bdaLifecycleOverride = null,
        string? sourceKind = null,
        bool outOfComms = false,
        string? lastKnownState = null)
    {
        if (string.IsNullOrEmpty(contactId))
        {
            return ContactDetailPresentation.Empty;
        }

        var entry = ContactDetailProjection.Project(
            contactId!,
            contacts,
            currentSimTick,
            bdaLifecycleOverride,
            sourceKind,
            outOfComms,
            lastKnownState);
        return Apply(entry);
    }

    /// <summary>
    /// Contact-quality out-of-comms cue: C2 network Denied means the contact picture
    /// cannot be trusted as live (DRG-180). Degraded stays "COMMS: up".
    /// </summary>
    public static bool OutOfCommsFromNetwork(CommsStateSnapshot? comms) =>
        comms is { State: CommsState.Denied };
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
    string SourceLine,
    string CommsLine,
    string LastKnownLine,
    string ExplainLinkLine)
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
        "STATE: —",
        "SOURCE: —",
        "COMMS: —",
        "LAST KNOWN: —",
        "EXPLAIN: —");
}
