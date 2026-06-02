namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Sim.Sensors;

/// <summary>C1 factories: sim + delegation rows → unified <see cref="OrderLogEntry"/>.</summary>
public static class OrderLogEntryFactories
{
    public static OrderLogEntry FromPolicyDenial(PolicyDenialRecord denial, ulong sequenceId = 0) =>
        new(sequenceId, OrderLogEntryKind.PolicyDenial, denial.SimTime, denial);

    public static OrderLogEntry FromEngagement(EngagementRecord engagement, ulong sequenceId = 0) =>
        new(sequenceId, OrderLogEntryKind.Engagement, engagement.SimTime, engagement);

    public static OrderLogEntry FromEngagementOutcome(EngagementOutcomeRecord outcome, ulong sequenceId = 0) =>
        new(sequenceId, OrderLogEntryKind.EngagementOutcome, outcome.SimTime, outcome);

    public static OrderLogEntry FromMagazineChange(MagazineChangeRecord change, ulong sequenceId = 0) =>
        new(sequenceId, OrderLogEntryKind.MagazineChange, change.SimTime, change);

    public static OrderLogEntry FromContactChange(ContactChangeRecord change, ulong sequenceId = 0) =>
        new(sequenceId, OrderLogEntryKind.ContactChange, change.SimTime, change);

    public static OrderLogEntry FromContactTransition(ContactTransition transition, ulong sequenceId = 0) =>
        FromContactChange(
            new ContactChangeRecord(
                sequenceId,
                transition.SimTime,
                transition.SimTick,
                transition.ObserverId,
                transition.ContactId,
                transition.TargetId,
                transition.PreviousState.ToString(),
                transition.NewState.ToString()),
            sequenceId);

    public static OrderLogEntry FromMissionTransition(MissionTransitionRecord transition, ulong sequenceId = 0) =>
        new(sequenceId, OrderLogEntryKind.MissionTransition, transition.SimTime, transition);

    public static OrderLogEntry FromEventFired(EventFiredRecord fired, ulong sequenceId = 0) =>
        new(sequenceId, OrderLogEntryKind.EventFired, fired.SimTime, fired);

    public static OrderLogEntry FromControllerChange(ControllerChangeRecord change, ulong sequenceId = 0) =>
        new(sequenceId, OrderLogEntryKind.ControllerChange, change.SimTime, change);

    public static OrderLogEntry FromGroupMemberDetach(GroupMemberDetachRecord detach, ulong sequenceId = 0) =>
        new(sequenceId, OrderLogEntryKind.GroupMemberDetach, detach.SimTime, detach);

    public static OrderLogEntry FromGroupMemberRejoin(GroupMemberRejoinRecord rejoin, ulong sequenceId = 0) =>
        new(sequenceId, OrderLogEntryKind.GroupMemberRejoin, rejoin.SimTime, rejoin);
}