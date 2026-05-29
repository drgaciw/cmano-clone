namespace ProjectAegis.Delegation.Targets;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;

public sealed class GroupTarget : ICommandableTarget
{
    private readonly List<TargetId> _members = new();

    public GroupTarget(TargetId id)
    {
        Id = id;
        Slot = new ControllerSlot();
    }

    public TargetId Id { get; }

    public ControllerSlot Slot { get; }

    public bool IsDetachedFromGroup => false;

    public bool PendingReplan { get; private set; }

    public IReadOnlyList<TargetId> Members => _members;

    public void AddMember(TargetId memberId) => _members.Add(memberId);

    public void RemoveMember(TargetId memberId) => _members.Remove(memberId);

    public void MarkReplanPending() => PendingReplan = true;

    public void ClearReplanPending() => PendingReplan = false;
}
