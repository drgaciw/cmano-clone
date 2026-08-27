namespace ProjectAegis.Delegation.C2Network;

using ProjectAegis.Delegation.Comms;

/// <summary>
/// Headless fold of comms state, datalink topology, and order-log contacts.
/// Last-known contributors are preserved; live capability is never fabricated.
/// </summary>
public sealed record C2NetworkHealthSnapshot(
    C2NetworkHealthLevel NetworkHealth,
    CommsState CommsState,
    string CommsNodeId,
    IReadOnlyList<C2NetworkLinkHealthEntry> Links,
    IReadOnlyList<C2NetworkContributor> LastKnownContributors,
    IReadOnlyList<C2NetworkLostPath> LostPaths);
