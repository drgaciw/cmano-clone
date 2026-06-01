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
}
