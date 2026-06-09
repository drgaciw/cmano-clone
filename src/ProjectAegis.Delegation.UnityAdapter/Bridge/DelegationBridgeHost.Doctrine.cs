namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

public sealed partial class DelegationBridgeHost
{
    public DoctrineInheritanceEntry? LastDoctrineInheritance { get; private set; }

    public bool TrySetDoctrineOverride(string roeLevelLabel)
    {
        if (Bridge == null || string.IsNullOrEmpty(_selectedUnitId))
        {
            return false;
        }

        var unitId = new TargetId(_selectedUnitId);
        var simTime = Bridge.Phase == SimulationPhase.Executing ? _simTime : 0;

        var result = DoctrineOverrideCommand.TryApply(Bridge.Orchestrator, unitId, roeLevelLabel, simTime);

        if (result)
        {
            RefreshDoctrineInheritance();
        }

        return result;
    }

    public void RefreshDoctrineInheritance()
    {
        if (Bridge == null || string.IsNullOrEmpty(_selectedUnitId))
        {
            LastDoctrineInheritance = null;
            return;
        }

        var unitId = new TargetId(_selectedUnitId);
        var policy = Bridge.Orchestrator.ScenarioPolicy;
        LastDoctrineInheritance = DoctrineInheritanceProjection.ProjectUnit(unitId, policy, isFriendly: true);
    }
}
