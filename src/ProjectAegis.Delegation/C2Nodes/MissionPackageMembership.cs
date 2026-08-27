namespace ProjectAegis.Delegation.C2Nodes;

/// <summary>Sorted membership roll-up for one mission package.</summary>
public sealed record MissionPackageMembership(
    string PackageId,
    string PackageLabel,
    IReadOnlyList<string> ElementIds,
    IReadOnlyList<string> UnitIds);
