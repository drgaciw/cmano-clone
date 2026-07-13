namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

[TestFixture]
public sealed class DoctrineOverrideCommandTests
{
    [Test]
    public void TryApply_changes_policy_and_logs_policy_update_record()
    {
        var orchestrator = CreateOrchestratorWithUnit("u1", EffectivePolicy.DefaultFree);
        var unitId = new TargetId("u1");
        var unitKey = OrderActionMapper.TargetIdToUlong(unitId);

        Assert.That(
            DoctrineOverrideCommand.TryApply(orchestrator, unitId, "WeaponsTight", simTime: 2.0),
            Is.True);

        var effective = orchestrator.ResolveEffectivePolicyForUnit(unitKey);
        Assert.That(effective.Roe, Is.EqualTo(RoeLevel.WeaponsTight));

        var update = orchestrator.DecisionLog.PolicyUpdates
            .Last(u => u.NewValue == nameof(RoeLevel.WeaponsTight));
        Assert.That(update.Field, Is.EqualTo("roe"));
        Assert.That(update.PreviousValue, Is.EqualTo(nameof(RoeLevel.WeaponsFree)));
        Assert.That(update.SimTime, Is.EqualTo(2.0));
        Assert.That(update.PolicySnapshotId, Is.GreaterThan(0));
    }

    [Test]
    public void TryApply_is_idempotent_when_roe_unchanged()
    {
        var orchestrator = CreateOrchestratorWithUnit("u1", new EffectivePolicy(RoeLevel.WeaponsTight));
        var unitId = new TargetId("u1");
        var countBefore = orchestrator.DecisionLog.PolicyUpdates.Count;

        Assert.That(
            DoctrineOverrideCommand.TryApply(orchestrator, unitId, "WeaponsTight", simTime: 3.0),
            Is.False);
        Assert.That(orchestrator.DecisionLog.PolicyUpdates, Has.Count.EqualTo(countBefore));
    }

    [Test]
    public void TryApply_rejects_unknown_roe_label()
    {
        var orchestrator = CreateOrchestratorWithUnit("u1", EffectivePolicy.DefaultFree);
        var unitId = new TargetId("u1");
        var unitKey = OrderActionMapper.TargetIdToUlong(unitId);
        var countBefore = orchestrator.DecisionLog.PolicyUpdates.Count;

        Assert.That(
            DoctrineOverrideCommand.TryApply(orchestrator, unitId, "not-a-roe", simTime: 1.0),
            Is.False);
        Assert.That(
            orchestrator.ResolveEffectivePolicyForUnit(unitKey).Roe,
            Is.EqualTo(RoeLevel.WeaponsFree));
        Assert.That(orchestrator.DecisionLog.PolicyUpdates, Has.Count.EqualTo(countBefore));
    }

    [Test]
    public void TryApply_rejects_null_orchestrator()
    {
        Assert.That(
            DoctrineOverrideCommand.TryApply(null!, new TargetId("u1"), "HoldFire", 0),
            Is.False);
    }

    private static DelegationOrchestrator CreateOrchestratorWithUnit(
        string unitKey,
        EffectivePolicy initialPolicy)
    {
        var orchestrator = new DelegationOrchestrator(42);
        orchestrator.BeginExecution(simTime: 1.0, simTick: 1);

        var unit = new UnitTarget(new TargetId(unitKey));
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous);
        orchestrator.AssignAgentToTarget(agent, unit, initialPolicy);
        orchestrator.Register(unit);

        return orchestrator;
    }
}