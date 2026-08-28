namespace ProjectAegis.Delegation.C2Nodes;

/// <summary>Mission package composition authored outside the tick hotpath.</summary>
public sealed record PackageDefinition(
    string PackageId,
    string Label,
    IReadOnlyList<PackageElementDefinition> Elements);
