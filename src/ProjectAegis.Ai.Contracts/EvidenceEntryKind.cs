namespace ProjectAegis.Ai.Contracts;

/// <summary>
/// Evidence row kinds. Mirrors order-log entry kinds by name without referencing Delegation assemblies.
/// </summary>
public enum EvidenceEntryKind
{
    Header = 0,
    AgentDecision = 1,
    PolicyDenial = 2,
    Engagement = 3,
    ControllerChange = 4,
    GroupMemberDetach = 5,
    GroupMemberRejoin = 6,
    MagazineChange = 7,
    ContactChange = 8,
    MissionTransition = 9,
    EventFired = 10,
    EngagementOutcome = 11,
    PlayerOrder = 12,
    PolicyUpdate = 13,
    ModeChange = 14,
    CommsStateChange = 15,
    FuelStateChange = 16,
    FuelBurn = 17,
    PlatformDamageChange = 18,
    OrdnanceStateChange = 19,
    ContactOverride = 20,
}
