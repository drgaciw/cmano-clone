namespace ProjectAegis.Delegation.C2Nodes;

/// <summary>Package membership context for downstream targetability and authority explanations.</summary>
public sealed record C2NodeMembership(
    string PackageId,
    string PackageLabel,
    C2NodeMembershipKind Kind);
