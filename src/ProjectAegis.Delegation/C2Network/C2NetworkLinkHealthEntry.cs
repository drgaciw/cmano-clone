namespace ProjectAegis.Delegation.C2Network;

/// <summary>Replay-stable link-health row for Combat UX Slice A (headless; DRG-190 renders later).</summary>
public sealed record C2NetworkLinkHealthEntry(
    string FromUnitId,
    string ToUnitId,
    string LinkType,
    C2LinkHealth Health,
    ulong StalenessTicks,
    bool IsLiveCapability,
    IReadOnlyList<string> AffectedContributorUnitIds);
