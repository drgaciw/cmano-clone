namespace ProjectAegis.Delegation.C2Nodes;

/// <summary>Authoring-time mission-package element (scenario/fixture input).</summary>
public sealed record PackageElementDefinition(
    string ElementId,
    string PlatformUnitId,
    C2NodeRole Role,
    string CapabilityScope);
