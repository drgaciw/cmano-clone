namespace ProjectAegis.Delegation.Decision;

public enum OrderLogEntryKind
{
    AgentDecision = 0,
    PolicyDenial = 1,
    Engagement = 2,
    ControllerChange = 3,
    GroupMemberDetach = 4,
    GroupMemberRejoin = 5,
    MagazineChange = 6,
    ContactChange = 7,
    MissionTransition = 8,
    EventFired = 9,
    EngagementOutcome = 10,
    PlayerOrder = 11,
    PolicyUpdate = 12,
    ModeChange = 13,
    CommsStateChange = 14,
    FuelStateChange = 15,
    FuelBurn = 16,
}
