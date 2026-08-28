namespace ProjectAegis.Delegation.C2Nodes;

/// <summary>Replay-stable mission-package read model for headless C2 (DRG-213).</summary>
public sealed record MissionPackageSnapshot(
    string ActivePackageId,
    IReadOnlyList<C2NodeElement> Elements,
    IReadOnlyList<MissionPackageMembership> Packages)
{
    public static MissionPackageSnapshot Empty { get; } =
        new(string.Empty, Array.Empty<C2NodeElement>(), Array.Empty<MissionPackageMembership>());
}
