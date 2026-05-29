namespace ProjectAegis.Delegation.Targets;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;

public sealed class UnitTarget : ICommandableTarget
{
    public UnitTarget(TargetId id)
    {
        Id = id;
        Slot = new ControllerSlot();
    }

    public TargetId Id { get; }

    public ControllerSlot Slot { get; }

    public bool IsDetachedFromGroup { get; private set; }

    public void SetDetached(bool detached) => IsDetachedFromGroup = detached;
}
