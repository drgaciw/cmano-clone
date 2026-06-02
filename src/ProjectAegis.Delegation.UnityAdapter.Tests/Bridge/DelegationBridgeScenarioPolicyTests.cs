namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

[TestFixture]
public sealed class DelegationBridgeScenarioPolicyTests
{
    [Test]
    public void Constructor_with_scenario_id_loads_orchestrator_policy()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "baltic-patrol-mission");
        Assert.That(bridge.Orchestrator.ScenarioPolicy, Is.Not.Null);
        Assert.That(bridge.Orchestrator.ScenarioPolicy!.MissionTimeline, Is.Not.Null);
    }
}