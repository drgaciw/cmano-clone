namespace ProjectAegis.Delegation.Targets;

using Controllers;
using Core;

public interface ICommandableTarget
{
    TargetId Id { get; }

    ControllerSlot Slot { get; }

    bool IsDetachedFromGroup { get; }
}
