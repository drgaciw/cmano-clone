namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// CMD-29: contact detail panel fields (belief / inference), distinct from own-unit detail.
/// </summary>
public sealed record ContactDetailEntry(
    string ContactId,
    string TargetId,
    string ClassificationLine,
    string ConfidenceLine,
    string DetectionProvenanceLine,
    string WraLine,
    string BdaLine,
    string StalenessLine,
    string LifecycleState);
