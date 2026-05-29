namespace ProjectAegis.Delegation.Targets;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;

public interface ICommandableTarget
{
    TargetId Id { get; }

    ControllerSlot Slot { get; }

    bool IsDetachedFromGroup { get; }
}
