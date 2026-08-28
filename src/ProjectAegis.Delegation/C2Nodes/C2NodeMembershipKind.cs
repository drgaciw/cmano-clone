namespace ProjectAegis.Delegation.C2Nodes;

/// <summary>
/// Distinguishes organic platform capability from mission-package composition so task re-org
/// does not collapse an organic sensor into the package picture.
/// </summary>
public enum C2NodeMembershipKind
{
    Organic = 0,
    Package = 1,
}
