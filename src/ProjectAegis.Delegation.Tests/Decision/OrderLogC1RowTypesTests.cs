using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

public sealed class OrderLogC1RowTypesTests
{
    [Test]
    public void PlayerOrder_PolicyUpdate_ModeChange_appear_in_fingerprint()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(0, 1, 1, new TargetId("u1"), OrderKind.Engage));
        log.AppendPolicyUpdate(new PolicyUpdateRecord(0, 1, 1, 42, "roe", "WeaponsTight", "WeaponsFree"));
        log.AppendModeChange(new ModeChangeRecord(0, 0, 0, "Planning", "Executing"));

        var fingerprint = log.ComputeFingerprint();
        Assert.That(fingerprint, Does.Contain("PlayerOrder|"));
        Assert.That(fingerprint, Does.Contain("PolicyUpdate|"));
        Assert.That(fingerprint, Does.Contain("ModeChange|"));
    }

    [Test]
    public void BeginExecution_appends_mode_change_row()
    {
        var orchestrator = new DelegationOrchestrator(1);
        orchestrator.BeginExecution();
        Assert.That(orchestrator.DecisionLog.ModeChanges, Has.Count.EqualTo(1));
        Assert.That(orchestrator.DecisionLog.ModeChanges[0].NewMode, Is.EqualTo("Executing"));
    }
}