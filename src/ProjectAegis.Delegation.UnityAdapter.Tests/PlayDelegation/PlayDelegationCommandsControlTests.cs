namespace ProjectAegis.Delegation.UnityAdapter.Tests.PlayDelegation;

using NUnit.Framework;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.UnityAdapter.PlayDelegation;
using ProjectAegis.Delegation.UnityAdapter.PlayEntry;
using ProjectAegis.Sim.Scenario;

[TestFixture]
public sealed class PlayDelegationCommandsControlTests
{
    [Test]
    public void TryTakeControl_TryReturnToAgent_TryPauseAgentActions_TryResumeAgentActions_work_on_commanded_target()
    {
        var (session, friendly, _) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        var commands = new PlayDelegationCommands(session);
        Assume.That(commands.TryAssignAgent(friendly.Id, "Aggressive", AutonomyLevel.SemiAutonomous, 0).Succeeded, Is.True);
        var agent = (AgentController)friendly.Slot.Active!;

        var paused = commands.TryPauseAgentActions(friendly.Id, simTime: 1.0);
        Assert.That(paused.Succeeded, Is.True, paused.Message);
        Assert.That(agent.IsActionsPaused, Is.True);

        var resumed = commands.TryResumeAgentActions(friendly.Id, simTime: 2.0);
        Assert.That(resumed.Succeeded, Is.True, resumed.Message);
        Assert.That(agent.IsActionsPaused, Is.False);

        var took = commands.TryTakeControl(friendly.Id, simTime: 3.0);
        Assert.That(took.Succeeded, Is.True, took.Message);
        Assert.That(friendly.Slot.Active, Is.InstanceOf<HumanController>());

        var returned = commands.TryReturnToAgent(friendly.Id, simTime: 4.0);
        Assert.That(returned.Succeeded, Is.True, returned.Message);
        Assert.That(friendly.Slot.Active, Is.SameAs(agent));
    }

    [Test]
    public void Control_commands_refuse_replay_viewer_and_uncommanded_target()
    {
        var (session, friendly, opposing) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        var commands = new PlayDelegationCommands(session);
        Assume.That(commands.TryAssignAgent(friendly.Id, "Aggressive", AutonomyLevel.SemiAutonomous, 0).Succeeded, Is.True);

        var uncommanded = commands.TryPauseAgentActions(opposing.Id, simTime: 1.0);
        Assert.That(uncommanded.Succeeded, Is.False);
        Assert.That(uncommanded.ErrorCode, Is.EqualTo(PlayDelegationErrorCodes.TargetNotCommanded));

        session.Bridge!.Orchestrator.AttachReplayViewer = true;
        var replayPause = commands.TryPauseAgentActions(friendly.Id, simTime: 2.0);
        Assert.That(replayPause.Succeeded, Is.False);
        Assert.That(replayPause.ErrorCode, Is.EqualTo(PlayDelegationErrorCodes.ReplayAttached));

        var replayTake = commands.TryTakeControl(friendly.Id, simTime: 3.0);
        Assert.That(replayTake.Succeeded, Is.False);
        Assert.That(replayTake.ErrorCode, Is.EqualTo(PlayDelegationErrorCodes.ReplayAttached));
    }
}
