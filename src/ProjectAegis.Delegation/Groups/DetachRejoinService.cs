namespace ProjectAegis.Delegation.Groups;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Targets;

public sealed class DetachRejoinService
{
    private readonly OverrideService _overrideService;

    public DetachRejoinService(OverrideService overrideService) =>
        _overrideService = overrideService;

    public void Detach(GroupTarget group, UnitTarget unit)
    {
        unit.SetDetached(true, group.Id);
        group.RemoveMember(unit.Id);
        group.MarkReplanPending();
        _overrideService.TakeDirectControl(unit, new HumanController());
    }

    public void Rejoin(GroupTarget group, UnitTarget unit)
    {
        _overrideService.ReleaseDirectControl(unit);
        unit.SetDetached(false);
        group.AddMember(unit.Id);
        group.MarkReplanPending();
    }
}
