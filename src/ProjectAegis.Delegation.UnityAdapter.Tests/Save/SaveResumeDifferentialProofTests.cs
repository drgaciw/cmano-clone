using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Replay;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Save;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Save;

[TestFixture]
public sealed class SaveResumeDifferentialProofTests
{
    private const double RestoreLatencyCeilingSeconds = 30.0;

    [Test]
    public void Differential_interruption_1_before_pending_approval_and_after_approve_reject()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            // Subcase A: Save BEFORE pending approval, restore, verify PendingApprovalQueue parity, then approve during M
            {
                const string scenarioId = "baltic-patrol";
                const int seed = 42;
                var cont = CreateUnit(scenarioId, seed);
                Assert.That(
                    cont.Facade.TrySetAgentAutonomy(null, new TargetId("u1"), AutonomyLevel.Assisted, 0.0, out var autoErr),
                    Is.True,
                    autoErr.Message);

                StepTicks(cont, 3);
                Assert.That(cont.Bridge.Orchestrator.PendingApprovalQueue.Pending, Has.Count.GreaterThanOrEqualTo(1));

                var capture = cont.Facade.TryCaptureEnvelope(
                    cont.Identity,
                    cont.Stepper.CheckpointStore,
                    cont.Stepper.CombinedWorldHash);
                Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
                var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

                var host = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed));
                var restore = SaveResumeDriver.TryRestore(host, saveBytes);
                Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

                var restored = host.Active;
                SaveDifferentialAssert.AssertPendingApprovalsEqual(cont, restored, "Subcase A at save tick N=3");

                var orderToApprove = cont.Bridge.Orchestrator.PendingApprovalQueue.Pending[0].Order.Id;
                Assert.That(cont.Facade.TryApprovePendingOrder(orderToApprove), Is.True);
                Assert.That(restored.Facade.TryApprovePendingOrder(orderToApprove), Is.True);

                Assert.That(restored.Facade.TryResumeSim(explicitOverride: true, out var resumeErr), Is.True, resumeErr);
                StepTicks(cont, 4);
                StepTicks(restored, 4);

                SaveDifferentialAssert.AssertFullParity(cont, restored, "Subcase A after approve in M");
            }

            // Subcase B: Reject + Approve pending orders immediately BEFORE save, restore, then run M
            {
                const string scenarioId = "baltic-patrol";
                const int seed = 42;
                var cont = CreateUnit(scenarioId, seed);
                Assert.That(
                    cont.Facade.TrySetAgentAutonomy(null, new TargetId("u1"), AutonomyLevel.Assisted, 0.0, out var autoErr),
                    Is.True,
                    autoErr.Message);

                StepTicks(cont, 3);
                var pending = cont.Bridge.Orchestrator.PendingApprovalQueue.Pending;
                Assert.That(pending, Has.Count.GreaterThanOrEqualTo(2));
                Assert.That(cont.Facade.TryRejectPendingOrder(pending[0].Order.Id), Is.True);
                Assert.That(cont.Facade.TryApprovePendingOrder(pending[1].Order.Id), Is.True);

                var capture = cont.Facade.TryCaptureEnvelope(
                    cont.Identity,
                    cont.Stepper.CheckpointStore,
                    cont.Stepper.CombinedWorldHash);
                Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
                var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

                var host = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed));
                var restore = SaveResumeDriver.TryRestore(host, saveBytes);
                Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

                var restored = host.Active;
                SaveDifferentialAssert.AssertPendingApprovalsEqual(cont, restored, "Subcase B at save tick N=3");

                Assert.That(restored.Facade.TryResumeSim(explicitOverride: true, out var resumeErr), Is.True, resumeErr);
                StepTicks(cont, 4);
                StepTicks(restored, 4);

                SaveDifferentialAssert.AssertFullParity(cont, restored, "Subcase B after pre-save reject+approve + M");
            }
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void Differential_interruption_2_delayed_comms_order_in_flight()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            const string scenarioId = "baltic-patrol-comms";
            const int seed = 42;
            var cont = CreateUnit(scenarioId, seed);

            // Tick 1..2: Tick 2 enters CommsState.Degraded (degradedOrderDelayTicks = 2)
            StepTicks(cont, 2);
            Assert.That(cont.Bridge.CurrentCommsState, Is.EqualTo(CommsState.Degraded));

            Assert.That(cont.Facade.TryTakeDirectControl(new EntityKey(1), cont.Stepper.ObservedTick), Is.True);
            Assert.That(
                cont.Facade.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Move, cont.Stepper.ObservedTick, RiskLevel.Low),
                Is.True);

            // Tick 3 (N = 3): delayed Move order is STILL in flight inside HumanController (executeTick = 4)
            StepTicks(cont, 1);
            Assert.That(cont.World.AppliedOrders, Is.Empty, "Delayed Move order must still be in flight at N=3.");

            var capture = cont.Facade.TryCaptureEnvelope(
                cont.Identity,
                cont.Stepper.CheckpointStore,
                cont.Stepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
            var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

            var host = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed));
            var restore = SaveResumeDriver.TryRestore(host, saveBytes);
            Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

            var restored = host.Active;
            Assert.That(restored.World.AppliedOrders, Is.Empty, "Restored unit must keep delayed Move in flight at N=3.");
            Assert.That(restored.Facade.TryResumeSim(explicitOverride: true, out var resumeErr), Is.True, resumeErr);

            // Run M = 4 ticks (tick 4..7): delayed Move dispatches at tick 4
            StepTicks(cont, 4);
            StepTicks(restored, 4);

            Assert.That(cont.World.AppliedOrders.Count(o => o.Kind == OrderKind.Move), Is.EqualTo(1));
            SaveDifferentialAssert.AssertFullParity(cont, restored, "Delayed comms order in flight N=3, M=4");
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void Differential_interruption_3_active_engagement_and_magazine_depletion()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            const string scenarioId = "baltic-patrol";
            const int seed = 42;
            var cont = CreateUnit(scenarioId, seed);

            // N = 2: active engagement + magazine depletion occurs in early ticks
            StepTicks(cont, 2);
            Assert.That(cont.Bridge.Orchestrator.DecisionLog.Engagements, Has.Count.GreaterThanOrEqualTo(1));
            Assert.That(cont.Bridge.Orchestrator.DecisionLog.MagazineChanges, Has.Count.GreaterThanOrEqualTo(1));

            var capture = cont.Facade.TryCaptureEnvelope(
                cont.Identity,
                cont.Stepper.CheckpointStore,
                cont.Stepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
            var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

            var host = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed));
            var restore = SaveResumeDriver.TryRestore(host, saveBytes);
            Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

            var restored = host.Active;
            Assert.That(restored.Facade.TryResumeSim(explicitOverride: true, out var resumeErr), Is.True, resumeErr);

            StepTicks(cont, 4);
            StepTicks(restored, 4);

            SaveDifferentialAssert.AssertFullParity(cont, restored, "Active engagement + magazine depletion N=2, M=4");
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void Differential_time_acceleration_2x_4x_8x_observed_vs_pipeline_tick()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            const string scenarioId = "baltic-patrol";
            const int seed = 77;
            var cont = CreateUnit(scenarioId, seed);

            // Tick 1 at 1x -> PipelineTick = 1
            StepTicks(cont, 1);
            Assert.That(cont.Facade.TrySetTimeAccelerationFactor(2, out var e2), Is.True, e2);

            // Ticks 2..3 at 2x -> PipelineTick = 1 + 2*2 = 5
            StepTicks(cont, 2);
            Assert.That(cont.Facade.TrySetTimeAccelerationFactor(4, out var e4), Is.True, e4);

            // Ticks 4..6 at 4x -> PipelineTick = 5 + 3*4 = 17
            StepTicks(cont, 3);
            Assert.That(cont.Stepper.ObservedTick, Is.EqualTo(6UL));
            Assert.That(cont.Stepper.PipelineTick, Is.EqualTo(17UL));

            var capture = cont.Facade.TryCaptureEnvelope(
                cont.Identity,
                cont.Stepper.CheckpointStore,
                cont.Stepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
            var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

            var host = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed));
            var restore = SaveResumeDriver.TryRestore(host, saveBytes);
            Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

            var restored = host.Active;
            Assert.That(restored.Stepper.ObservedTick, Is.EqualTo(6UL));
            Assert.That(restored.Stepper.PipelineTick, Is.EqualTo(17UL));
            Assert.That(restored.Bridge.Session!.TimeAccelerationFactor, Is.EqualTo(4));
            Assert.That(restored.Facade.TryResumeSim(explicitOverride: true, out var resumeErr), Is.True, resumeErr);

            // Ticks 7..8 at 4x -> PipelineTick = 17 + 2*4 = 25
            StepTicks(cont, 2);
            StepTicks(restored, 2);

            Assert.That(cont.Facade.TrySetTimeAccelerationFactor(8, out var e8), Is.True, e8);
            Assert.That(restored.Facade.TrySetTimeAccelerationFactor(8, out var e8R), Is.True, e8R);

            // Ticks 9..10 at 8x -> PipelineTick = 25 + 2*8 = 41
            StepTicks(cont, 2);
            StepTicks(restored, 2);

            Assert.That(cont.Stepper.ObservedTick, Is.EqualTo(10UL));
            Assert.That(cont.Stepper.PipelineTick, Is.EqualTo(41UL));
            SaveDifferentialAssert.AssertFullParity(cont, restored, "Time acceleration 2x/4x/8x N=6, M=4");
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void Differential_same_tick_multiple_commands_preserve_sequence_order()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            const string scenarioId = "baltic-v3-patrol";
            const int seed = 42;
            var cont = CreateUnit(scenarioId, seed);

            StepTicks(cont, 2);
            Assert.That(cont.Stepper.ObservedTick, Is.EqualTo(2UL));

            // 5 commands at the exact same ObservedTick == 2
            Assert.That(cont.Facade.TryApplyDoctrineOverride(new TargetId("u1"), "WeaponsTight", 2.0), Is.True);
            Assert.That(cont.Facade.TryTakeDirectControl(new EntityKey(2), 2.0), Is.True);
            Assert.That(cont.Facade.TryEnqueueHumanOrder(new EntityKey(2), OrderKind.Move, 2.0, RiskLevel.Low), Is.True);
            Assert.That(cont.Facade.TrySetTimeAccelerationFactor(2, out var accelErr), Is.True, accelErr);
            Assert.That(cont.Facade.TryRebindAgentTraits(new TargetId("u1"), PersonalityCatalog.All[3].Traits), Is.True);

            Assert.That(cont.Facade.Journal.Rows, Has.Count.EqualTo(5));
            for (var i = 0; i < 5; i++)
            {
                Assert.That(cont.Facade.Journal.Rows[i].ObservedTick, Is.EqualTo(2UL));
                Assert.That(cont.Facade.Journal.Rows[i].SequenceOrder, Is.EqualTo((ulong)i));
            }

            StepTicks(cont, 2); // N = 4

            var capture = cont.Facade.TryCaptureEnvelope(
                cont.Identity,
                cont.Stepper.CheckpointStore,
                cont.Stepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
            var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

            var host = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed));
            var restore = SaveResumeDriver.TryRestore(host, saveBytes);
            Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

            var restored = host.Active;
            Assert.That(restored.Facade.TryResumeSim(explicitOverride: true, out var resumeErr), Is.True, resumeErr);

            StepTicks(cont, 4);
            StepTicks(restored, 4);

            SaveDifferentialAssert.AssertFullParity(cont, restored, "Same-tick 5 commands at tick 2, N=4, M=4");
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void Differential_agent_rebind_rebrief_assign_and_doctrine_override()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            const string scenarioId = "baltic-v3-patrol";
            const int seed = 42;
            var cont = CreateUnit(scenarioId, seed);

            StepTicks(cont, 1);

            // ucav-blue starts uncontrolled in baltic-v3-patrol -> AssignAgentController succeeds cleanly
            Assert.That(
                cont.Facade.TryAssignAgentController(
                    new TargetId("ucav-blue"),
                    "a-ucav-blue",
                    PersonalityCatalog.All[0].Traits,
                    AutonomyLevel.SemiAutonomous,
                    20.0,
                    "Aggressive",
                    isFriendly: true,
                    simTime: 1.0),
                Is.True);

            StepTicks(cont, 1);
            Assert.That(cont.Facade.TryRebriefAgent(new TargetId("ucav-blue"), "Defensive", 2.0), Is.True);
            Assert.That(cont.Facade.TryRebindAgentTraits(new TargetId("u1"), PersonalityCatalog.All[2].Traits), Is.True);
            Assert.That(cont.Facade.TryApplyDoctrineOverride(new TargetId("u1"), "WeaponsTight", 2.0), Is.True);

            StepTicks(cont, 3); // N = 5

            var capture = cont.Facade.TryCaptureEnvelope(
                cont.Identity,
                cont.Stepper.CheckpointStore,
                cont.Stepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
            var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

            var host = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed));
            var restore = SaveResumeDriver.TryRestore(host, saveBytes);
            Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

            var restored = host.Active;
            Assert.That(restored.Facade.TryResumeSim(explicitOverride: true, out var resumeErr), Is.True, resumeErr);

            StepTicks(cont, 5);
            StepTicks(restored, 5);

            SaveDifferentialAssert.AssertFullParity(cont, restored, "Assign + Rebrief + Rebind + DoctrineOverride N=5, M=5");
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void Differential_direct_control_handoff_and_pause_resume_agent_actions()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            const string scenarioId = "baltic-patrol";
            const int seed = 42;
            var cont = CreateUnit(scenarioId, seed);

            StepTicks(cont, 1);
            Assert.That(cont.Facade.TryPauseAgentActions(new TargetId("u1"), 1.0), Is.True);

            StepTicks(cont, 1);
            Assert.That(cont.Facade.TryTakeDirectControl(new EntityKey(1), 2.0), Is.True);
            Assert.That(cont.Facade.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Move, 2.0, RiskLevel.Low), Is.True);

            StepTicks(cont, 1); // N = 3

            var capture = cont.Facade.TryCaptureEnvelope(
                cont.Identity,
                cont.Stepper.CheckpointStore,
                cont.Stepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
            var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

            var host = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed));
            var restore = SaveResumeDriver.TryRestore(host, saveBytes);
            Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

            var restored = host.Active;
            var restoredSlot = restored.Bridge.Registry.Bindings.First(b => b.TargetId.Value == "u1").Target.Slot;
            Assert.That(restoredSlot.Active, Is.TypeOf<HumanController>());
            Assert.That(restoredSlot.SuspendedAgent, Is.Not.Null);
            Assert.That(restoredSlot.SuspendedAgent!.IsActionsPaused, Is.True);
            Assert.That(restored.Facade.TryResumeSim(explicitOverride: true, out var resumeErr), Is.True, resumeErr);

            StepTicks(cont, 1);
            StepTicks(restored, 1);

            Assert.That(cont.Facade.TryReleaseDirectControl(new EntityKey(1), 4.0), Is.True);
            Assert.That(restored.Facade.TryReleaseDirectControl(new EntityKey(1), 4.0), Is.True);

            StepTicks(cont, 1);
            StepTicks(restored, 1);

            Assert.That(cont.Facade.TryResumeAgentActions(new TargetId("u1"), 5.0), Is.True);
            Assert.That(restored.Facade.TryResumeAgentActions(new TargetId("u1"), 5.0), Is.True);

            StepTicks(cont, 3);
            StepTicks(restored, 3);

            SaveDifferentialAssert.AssertFullParity(cont, restored, "Direct control handoff + pause/resume agent actions");
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void Differential_pending_NextEngageSalvoOverride_survives_save_and_controls_next_salvo()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            const string scenarioId = "baltic-patrol-salvo";
            const int seed = 42;
            var cont = CreateUnit(scenarioId, seed);

            // Take direct control before tick 1 so autonomous Engage doesn't consume ammo on tick 1
            Assert.That(cont.Facade.TryTakeDirectControl(new EntityKey(1), 0.0), Is.True);
            StepTicks(cont, 2); // c1 detected at tick 1; now at ObservedTick = 2

            Assert.That(
                cont.Facade.TryEnqueueAttackOption(new EntityKey(1), "fire-single", cont.World, out var optErr),
                Is.True,
                optErr);
            Assert.That(cont.Bridge.Session!.NextEngageSalvoOverride, Is.EqualTo(1));

            // Save at N = 2 BEFORE tick 3 drains NextEngageSalvoOverride
            var capture = cont.Facade.TryCaptureEnvelope(
                cont.Identity,
                cont.Stepper.CheckpointStore,
                cont.Stepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
            var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

            var host = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed));
            var restore = SaveResumeDriver.TryRestore(host, saveBytes);
            Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

            var restored = host.Active;
            Assert.That(restored.Bridge.Session!.NextEngageSalvoOverride, Is.EqualTo(1));
            Assert.That(restored.Facade.TryResumeSim(explicitOverride: true, out var resumeErr), Is.True, resumeErr);

            // Tick 3 drains override (salvo = 1 instead of scenario salvoSize = 2)
            StepTicks(cont, 3);
            StepTicks(restored, 3);

            Assert.That(cont.Bridge.Session.NextEngageSalvoOverride, Is.Null);
            Assert.That(cont.Bridge.Orchestrator.DecisionLog.Engagements[0].SalvoSize, Is.EqualTo(1));
            Assert.That(cont.Bridge.Orchestrator.DecisionLog.MagazineChanges[0].Delta, Is.EqualTo(-1));
            SaveDifferentialAssert.AssertFullParity(cont, restored, "Pending NextEngageSalvoOverride fire-single N=2, M=3");
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void Differential_craft_ops_launch_aircraft_launch_boat_and_recover_boat()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            const string scenarioId = "baltic-patrol";
            const int seed = 42;
            var cont = CreateUnit(scenarioId, seed);

            Assert.That(cont.Facade.TryTakeDirectControl(new EntityKey(1), 0.0), Is.True);
            StepTicks(cont, 1);

            Assert.That(cont.Facade.TryIssuePlayerCommand(new EntityKey(1), "launch_aircraft", 1.0, out var airErr), Is.True, airErr);
            Assert.That(cont.Facade.TryIssuePlayerCommand(new EntityKey(1), "launch_boat", 1.0, out var boatErr), Is.True, boatErr);

            StepTicks(cont, 5); // N = 6; BoatOps reaches Waterborne, CraftOpsCounts["u1"] == 2
            Assert.That(cont.World.CraftOpsCounts["u1"], Is.EqualTo(2));

            var capture = cont.Facade.TryCaptureEnvelope(
                cont.Identity,
                cont.Stepper.CheckpointStore,
                cont.Stepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
            var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

            var host = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed));
            var restore = SaveResumeDriver.TryRestore(host, saveBytes);
            Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

            var restored = host.Active;
            Assert.That(restored.World.CraftOpsCounts["u1"], Is.EqualTo(2));
            Assert.That(restored.Facade.TryResumeSim(explicitOverride: true, out var resumeErr), Is.True, resumeErr);

            StepTicks(cont, 1);
            StepTicks(restored, 1);

            Assert.That(cont.Facade.TryIssuePlayerCommand(new EntityKey(1), "recover_boat", 7.0, out var recErr), Is.True, recErr);
            Assert.That(restored.Facade.TryIssuePlayerCommand(new EntityKey(1), "recover_boat", 7.0, out var recErrR), Is.True, recErrR);

            StepTicks(cont, 5); // N + M = 12
            StepTicks(restored, 5);

            Assert.That(cont.World.CraftOpsCounts["u1"], Is.EqualTo(3));
            SaveDifferentialAssert.AssertFullParity(cont, restored, "Craft ops launch_aircraft/launch_boat/recover_boat N=6, M=6");
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void Differential_contact_override_and_play_delegation_personality_autonomy()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            const string scenarioId = "baltic-v3-patrol";
            const int seed = 42;
            var cont = CreateUnit(scenarioId, seed);

            StepTicks(cont, 2);

            var activeContacts = ContactPictureProjection.Project(cont.Bridge.Orchestrator.DecisionLog);
            Assert.That(activeContacts, Has.Count.GreaterThanOrEqualTo(1));
            var contactId = activeContacts[0].ContactId;

            Assert.That(
                cont.Facade.TrySetAgentAutonomy(null, new TargetId("u1"), AutonomyLevel.Assisted, 2.0, out var aRes),
                Is.True,
                aRes.Message);
            Assert.That(
                cont.Facade.TrySetAgentPersonality(null, new TargetId("u1"), "EwSpecialist", 2.0, out var pRes),
                Is.True,
                pRes.Message);
            Assert.That(
                cont.Facade.TryReclassifyContact(contactId, "Friendly", 2.0, out var cRes),
                Is.True,
                cRes.Message);

            StepTicks(cont, 3); // N = 5

            var capture = cont.Facade.TryCaptureEnvelope(
                cont.Identity,
                cont.Stepper.CheckpointStore,
                cont.Stepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
            var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

            var host = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed));
            var restore = SaveResumeDriver.TryRestore(host, saveBytes);
            Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

            var restored = host.Active;
            Assert.That(restored.Facade.TryResumeSim(explicitOverride: true, out var resumeErr), Is.True, resumeErr);

            StepTicks(cont, 1);
            StepTicks(restored, 1);

            Assert.That(cont.Facade.TryDropContact(contactId, 6.0, out var dRes), Is.True, dRes.Message);
            Assert.That(restored.Facade.TryDropContact(contactId, 6.0, out var dResR), Is.True, dResR.Message);

            StepTicks(cont, 4);
            StepTicks(restored, 4);

            SaveDifferentialAssert.AssertFullParity(cont, restored, "Contact override + PlayDelegation personality/autonomy");
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void Differential_watch_auto_pause_gate_ack_dismiss_clearance_and_second_gen_save()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            const string scenarioId = "baltic-v3-patrol";
            const int seed = 42;
            var cont = CreateUnit(scenarioId, seed, reportWatchTransitions: true);

            // Tick 1 detects contacts and auto-pauses WatchQueue
            StepTicks(cont, 1);
            Assert.That(cont.Bridge.Session!.IsSimPaused, Is.True);
            Assert.That(cont.Bridge.Session.WatchQueue.Cards, Has.Count.GreaterThanOrEqualTo(2));
            Assert.That(cont.Facade.TryResumeSim(explicitOverride: false, out _), Is.False);

            var cards = cont.Bridge.Session.WatchQueue.Cards.Select(c => c.EventId).ToArray();
            Assert.That(cont.Facade.TryAcknowledgeWatchAttention(cards[0]), Is.True);
            for (var i = 1; i < cards.Length; i++)
            {
                Assert.That(cont.Facade.TryDismissWatchAttention(cards[i]), Is.True);
            }

            Assert.That(cont.Facade.TryResumeSim(explicitOverride: false, out var resumeOk), Is.True, resumeOk);

            // Step ticks 2..3; resolve any newly emitted watch cards at N=3 so continuous run is unpaused at save
            StepTicks(cont, 2); // N = 3
            var allCardsAtSave = cont.Bridge.Session.WatchQueue.Cards.ToArray();
            var ackedIds = new HashSet<string>(StringComparer.Ordinal) { cards[0] };
            for (var i = 0; i < allCardsAtSave.Length; i++)
            {
                if (ackedIds.Contains(allCardsAtSave[i].EventId))
                {
                    Assert.That(cont.Facade.TryAcknowledgeWatchAttention(allCardsAtSave[i].EventId), Is.True);
                }
                else
                {
                    Assert.That(cont.Facade.TryDismissWatchAttention(allCardsAtSave[i].EventId), Is.True);
                }
            }

            if (cont.Bridge.Session.IsSimPaused)
            {
                Assert.That(cont.Facade.TryResumeSim(explicitOverride: false, out var r2), Is.True, r2);
            }

            var capture = cont.Facade.TryCaptureEnvelope(
                cont.Identity,
                cont.Stepper.CheckpointStore,
                cont.Stepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
            var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

            var host = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed, reportWatchTransitions: true));
            var restore = SaveResumeDriver.TryRestore(
                host,
                saveBytes,
                _ => new SaveCandidateRuntimeArgs(ReportWatchTransitions: true));
            Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

            var restored = host.Active;
            Assert.That(restored.Bridge.Session!.IsSimPaused, Is.True);
            Assert.That(restored.Bridge.Session.WatchQueue.Cards, Has.Count.EqualTo(allCardsAtSave.Length));
            foreach (var card in restored.Bridge.Session.WatchQueue.Cards)
            {
                Assert.That(card.IsAcknowledged, Is.False);
                Assert.That(card.IsDismissed, Is.False);
            }

            Assert.That(
                restored.Facade.TryResumeSim(explicitOverride: false, out _),
                Is.False,
                "WatchAutoPauseGate must block non-override resume until restored watch cards are re-acked/dismissed.");

            for (var i = 0; i < allCardsAtSave.Length; i++)
            {
                if (ackedIds.Contains(allCardsAtSave[i].EventId))
                {
                    Assert.That(restored.Facade.TryAcknowledgeWatchAttention(allCardsAtSave[i].EventId), Is.True);
                }
                else
                {
                    Assert.That(restored.Facade.TryDismissWatchAttention(allCardsAtSave[i].EventId), Is.True);
                }
            }

            Assert.That(restored.Facade.TryResumeSim(explicitOverride: false, out var restoredResumeOk), Is.True, restoredResumeOk);

            StepTicks(cont, 3);
            StepTicks(restored, 3);

            SaveDifferentialAssert.AssertFullParity(cont, restored, "WatchAutoPauseGate 1st-gen restore + re-ack/dismiss + M=3");

            // 2nd-generation save from restored session and restore again
            var gen2Capture = restored.Facade.TryCaptureEnvelope(
                restored.Identity,
                restored.Stepper.CheckpointStore,
                restored.Stepper.CombinedWorldHash);
            Assert.That(gen2Capture.Succeeded, Is.True, gen2Capture.FailureReason);
            var gen2Bytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(gen2Capture.Value!);

            var gen2Restore = SaveResumeDriver.TryRestore(
                host,
                gen2Bytes,
                _ => new SaveCandidateRuntimeArgs(ReportWatchTransitions: true));
            Assert.That(gen2Restore.Succeeded, Is.True, gen2Restore.FailureReason);

            var gen2Unit = host.Active;
            Assert.That(gen2Unit.Bridge.Session!.IsSimPaused, Is.True);
            foreach (var card in gen2Unit.Bridge.Session.WatchQueue.Cards)
            {
                Assert.That(card.IsAcknowledged, Is.False);
                Assert.That(card.IsDismissed, Is.False);
            }

            Assert.That(
                gen2Unit.Facade.TryResumeSim(explicitOverride: false, out _),
                Is.False,
                "WatchAutoPauseGate must block non-override resume until Gen 2 restored watch cards are re-acked/dismissed.");

            foreach (var card in gen2Unit.Bridge.Session.WatchQueue.Cards.ToArray())
            {
                if (ackedIds.Contains(card.EventId))
                {
                    Assert.That(gen2Unit.Facade.TryAcknowledgeWatchAttention(card.EventId), Is.True);
                }
                else
                {
                    Assert.That(gen2Unit.Facade.TryDismissWatchAttention(card.EventId), Is.True);
                }
            }

            Assert.That(gen2Unit.Facade.TryResumeSim(explicitOverride: false, out var gen2ResumeOk), Is.True, gen2ResumeOk);
            StepTicks(cont, 2);
            StepTicks(gen2Unit, 2);

            SaveDifferentialAssert.AssertFullParity(cont, gen2Unit, "WatchAutoPauseGate 2nd-gen save/restore + M2=2");
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void Differential_omitted_state_only_or_unknown_journal_command_fails_and_rolls_back()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            const string scenarioId = "baltic-v3-patrol";
            const int seed = 42;
            var active = CreateUnit(scenarioId, seed);

            StepTicks(active, 1);
            Assert.That(active.Facade.TrySetTimeAccelerationFactor(2, out var accelErr), Is.True, accelErr);
            Assert.That(active.Facade.TryRebindAgentTraits(new TargetId("u1"), PersonalityCatalog.All[2].Traits), Is.True);
            StepTicks(active, 3); // N = 4

            var capture = active.Facade.TryCaptureEnvelope(
                active.Identity,
                active.Stepper.CheckpointStore,
                active.Stepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
            var validBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

            var host = new HeadlessSaveResumeHost(active);
            var beforeUnit = host.Active;
            var beforePublishCount = host.PublishCount;
            var beforeCombinedHash = beforeUnit.Stepper.CombinedWorldHash;
            var beforeOrderLogSha = OrderLogReplayFingerprint.ComputeSha256Hex(beforeUnit.Bridge.Orchestrator.DecisionLog);
            var beforePendingCount = beforeUnit.Bridge.Orchestrator.PendingApprovalQueue.Pending.Count;
            var beforeDecisionCount = beforeUnit.Bridge.Orchestrator.DecisionLog.ChronologicalEntries().Count;
            var beforeUcavBlueSide = BalticV3SideRegistry.GetSideForUnit("ucav-blue");

            // (a) Unknown journal command -> ReplayCommandFailed
            var unknownCmdBytes = ReSignEnvelope(validBytes, root =>
            {
                root["inputJournal"]!.AsObject()["rows"]!.AsArray()[0]!.AsObject()["commandKind"] = "future_unknown_cmd";
            });
            var unknownRes = SaveResumeDriver.TryRestore(host, unknownCmdBytes);
            Assert.That(unknownRes.Succeeded, Is.False);
            Assert.That(unknownRes.FailureCode, Is.EqualTo(SaveFailureCode.ReplayCommandFailed));
            SaveDifferentialAssert.AssertHostUnchanged(
                host, beforeUnit, beforePublishCount, beforeCombinedHash, beforeOrderLogSha, beforePendingCount, beforeDecisionCount, "unknown cmd");
            Assert.That(BalticV3SideRegistry.GetSideForUnit("ucav-blue"), Is.EqualTo(beforeUcavBlueSide));

            // (b) Omitted state-only row with preserved journalDigest -> IntegrityMismatch
            var omittedPreserveDigest = ReSignEnvelope(validBytes, root =>
            {
                var arr = root["inputJournal"]!.AsObject()["rows"]!.AsArray();
                arr.RemoveAt(arr.Count - 1);
            }, preserveJournalDigest: true);
            var omittedDigestRes = SaveResumeDriver.TryRestore(host, omittedPreserveDigest);
            Assert.That(omittedDigestRes.Succeeded, Is.False);
            Assert.That(omittedDigestRes.FailureCode, Is.EqualTo(SaveFailureCode.IntegrityMismatch));
            SaveDifferentialAssert.AssertHostUnchanged(
                host, beforeUnit, beforePublishCount, beforeCombinedHash, beforeOrderLogSha, beforePendingCount, beforeDecisionCount, "omitted digest");

            // (c) Forged re-signed envelope omitting SetTimeAccelerationFactor(2) (with renumbered SequenceOrder) -> ReplayMismatch at Step 7
            var omittedAccelResigned = ReSignEnvelope(validBytes, root =>
            {
                var arr = root["inputJournal"]!.AsObject()["rows"]!.AsArray();
                arr.RemoveAt(0);
                for (var i = 0; i < arr.Count; i++)
                {
                    arr[i]!.AsObject()["sequenceOrder"] = (ulong)i;
                }
            });
            var accelRes = SaveResumeDriver.TryRestore(host, omittedAccelResigned);
            Assert.That(accelRes.Succeeded, Is.False);
            Assert.That(accelRes.FailureCode, Is.EqualTo(SaveFailureCode.ReplayMismatch));
            SaveDifferentialAssert.AssertHostUnchanged(
                host, beforeUnit, beforePublishCount, beforeCombinedHash, beforeOrderLogSha, beforePendingCount, beforeDecisionCount, "omitted accel");
            Assert.That(BalticV3SideRegistry.GetSideForUnit("ucav-blue"), Is.EqualTo(beforeUcavBlueSide));

            // (d) Forged re-signed envelope omitting TryApprovePendingOrder (state-only mutation that gates execution on next tick) -> ReplayMismatch
            {
                var assistedUnit = CreateUnit("baltic-patrol", seed: 42);
                Assert.That(
                    assistedUnit.Facade.TrySetAgentAutonomy(null, new TargetId("u1"), AutonomyLevel.Assisted, 0.0, out _),
                    Is.True);
                StepTicks(assistedUnit, 1);
                var orderId = assistedUnit.Bridge.Orchestrator.PendingApprovalQueue.Pending[0].Order.Id;
                Assert.That(assistedUnit.Facade.TryApprovePendingOrder(orderId), Is.True);
                StepTicks(assistedUnit, 1); // Approved order executes on tick 2

                var assistedCapture = assistedUnit.Facade.TryCaptureEnvelope(
                    assistedUnit.Identity,
                    assistedUnit.Stepper.CheckpointStore,
                    assistedUnit.Stepper.CombinedWorldHash);
                Assert.That(assistedCapture.Succeeded, Is.True, assistedCapture.FailureReason);
                var assistedBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(assistedCapture.Value!);

                var omittedApproveResigned = ReSignEnvelope(assistedBytes, root =>
                {
                    var arr = root["inputJournal"]!.AsObject()["rows"]!.AsArray();
                    arr.RemoveAt(arr.Count - 1); // drop ApprovePendingOrder
                });
                var assistedHost = new HeadlessSaveResumeHost(assistedUnit);
                var assistedBeforeHash = assistedUnit.Stepper.CombinedWorldHash;
                var assistedBeforeLog = OrderLogReplayFingerprint.ComputeSha256Hex(assistedUnit.Bridge.Orchestrator.DecisionLog);
                var assistedBeforePending = assistedUnit.Bridge.Orchestrator.PendingApprovalQueue.Pending.Count;
                var assistedBeforeDecisions = assistedUnit.Bridge.Orchestrator.DecisionLog.ChronologicalEntries().Count;

                var omittedApproveRes = SaveResumeDriver.TryRestore(assistedHost, omittedApproveResigned);
                Assert.That(omittedApproveRes.Succeeded, Is.False);
                Assert.That(omittedApproveRes.FailureCode, Is.EqualTo(SaveFailureCode.ReplayMismatch));
                SaveDifferentialAssert.AssertHostUnchanged(
                    assistedHost,
                    assistedUnit,
                    0,
                    assistedBeforeHash,
                    assistedBeforeLog,
                    assistedBeforePending,
                    assistedBeforeDecisions,
                    "omitted ApprovePendingOrder");
            }
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void Rollback_corrupt_truncated_incompatible_envelope_and_slot_store_leave_active_session_and_slots_unchanged()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        var tempDir = CreateScratchTempDir();
        try
        {
            const string scenarioId = "gauntlet-t1-patrol-a";
            const int seed = 42;
            var dbPath = ResolveBalticCatalogDbPath();
            var dbShaBefore = ComputeFileSha256Hex(dbPath);
            using var catalog = new SqliteCatalogReader(dbPath, layerVersion: "harness-catalog");

            var active = CreateUnit(scenarioId, seed, catalogOverride: catalog);
            StepTicks(active, 4);

            var capture = active.Facade.TryCaptureEnvelope(
                active.Identity,
                active.Stepper.CheckpointStore,
                active.Stepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);

            var store = new SaveSlotStore(tempDir);
            Assert.That(store.Save(SaveSlotStore.Manual1, capture.Value!).Succeeded, Is.True);
            var slotPath = store.TryGetSlotPath(SaveSlotStore.Manual1)!;
            var slotBytesBefore = File.ReadAllBytes(slotPath);
            var validBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

            var host = new HeadlessSaveResumeHost(active);
            var beforeUnit = host.Active;
            var beforePublishCount = host.PublishCount;
            var beforeCombinedHash = beforeUnit.Stepper.CombinedWorldHash;
            var beforeOrderLogSha = OrderLogReplayFingerprint.ComputeSha256Hex(beforeUnit.Bridge.Orchestrator.DecisionLog);
            var beforePendingCount = beforeUnit.Bridge.Orchestrator.PendingApprovalQueue.Pending.Count;
            var beforeDecisionCount = beforeUnit.Bridge.Orchestrator.DecisionLog.ChronologicalEntries().Count;

            Assert.That(BalticV3SideRegistry.GetSideForUnit("k-31-visby-2009"), Is.EqualTo("blue"));
            Assert.That(BalticV3SideRegistry.GetSideForUnit("em-sovremenny-i-pr-956-sarych"), Is.EqualTo("red"));

            var invalidPayloads = new (string Name, byte[] Bytes, SaveFailureCode ExpectedCode)[]
            {
                ("truncated", validBytes.Take(validBytes.Length / 3).ToArray(), SaveFailureCode.TruncatedOrMalformedJson),
                ("wrong_schema", MutateRawJson(validBytes, r => r["identity"]!.AsObject()["schemaVersion"] = 99), SaveFailureCode.UnknownSchemaVersion),
                ("network_field", MutateRawJson(validBytes, r => r["lockstep"] = true), SaveFailureCode.UnsupportedNetworkField),
                ("tampered_hash", MutateRawJson(validBytes, r => r["envelopeHashSha256"] = new string('0', 64)), SaveFailureCode.IntegrityMismatch),
                ("build_mismatch", ReSignEnvelope(validBytes, r => r["identity"]!.AsObject()["buildId"] = "other-build"), SaveFailureCode.BuildMismatch),
                ("catalog_mismatch", ReSignEnvelope(validBytes, r => r["identity"]!.AsObject()["wholeCatalogHash"] = new string('a', 64)), SaveFailureCode.CatalogMismatch),
                ("scenario_mismatch", ReSignEnvelope(validBytes, r => r["identity"]!.AsObject()["scenarioId"] = "other-scenario"), SaveFailureCode.ScenarioMismatch),
                ("policy_mismatch", ReSignEnvelope(validBytes, r => r["identity"]!.AsObject()["policyHashes"]!.AsArray()[0]!.AsObject()["policySha256"] = new string('b', 64)), SaveFailureCode.PolicyMismatch),
                ("seed_mismatch", ReSignEnvelope(validBytes, r => r["identity"]!.AsObject()["globalSeed"] = 9999), SaveFailureCode.SeedMismatch),
            };

            foreach (var (name, bytes, expectedCode) in invalidPayloads)
            {
                var res = SaveResumeDriver.TryRestore(
                    host,
                    bytes,
                    CatalogArgs(catalog));
                Assert.That(res.Succeeded, Is.False, name);
                Assert.That(res.FailureCode, Is.EqualTo(expectedCode), name);
                SaveDifferentialAssert.AssertHostUnchanged(
                    host, beforeUnit, beforePublishCount, beforeCombinedHash, beforeOrderLogSha, beforePendingCount, beforeDecisionCount, name);
                Assert.That(BalticV3SideRegistry.GetSideForUnit("k-31-visby-2009"), Is.EqualTo("blue"), name);
                Assert.That(BalticV3SideRegistry.GetSideForUnit("em-sovremenny-i-pr-956-sarych"), Is.EqualTo("red"), name);
            }

            // Missing slot restore leaves host & existing slot intact
            var missingSlotRes = SaveResumeDriver.TryRestoreFromSlot(
                host,
                store,
                SaveSlotStore.Manual3,
                CatalogArgs(catalog));
            Assert.That(missingSlotRes.Succeeded, Is.False);
            Assert.That(missingSlotRes.FailureCode, Is.EqualTo(SaveFailureCode.SlotNotFound));
            SaveDifferentialAssert.AssertHostUnchanged(
                host, beforeUnit, beforePublishCount, beforeCombinedHash, beforeOrderLogSha, beforePendingCount, beforeDecisionCount, "missing slot");
            Assert.That(File.ReadAllBytes(slotPath), Is.EqualTo(slotBytesBefore));
            Assert.That(ComputeFileSha256Hex(dbPath), Is.EqualTo(dbShaBefore), "Shipped SQLite DB must stay read-only.");
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
            DeleteScratchDir(tempDir);
        }
    }

    [Test]
    public void Differential_real_sqlite_catalog_gauntlet_orbat_parity()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        var tempDir = CreateScratchTempDir();
        try
        {
            const string scenarioId = "gauntlet-t1-patrol-a";
            const int seed = 42;
            var dbPath = ResolveBalticCatalogDbPath();
            var dbShaBefore = ComputeFileSha256Hex(dbPath);
            using var catalog = new SqliteCatalogReader(dbPath, layerVersion: "harness-catalog");

            var cont = CreateUnit(scenarioId, seed, catalogOverride: catalog);
            StepTicks(cont, 6); // N = 6

            var capture = cont.Facade.TryCaptureEnvelope(
                cont.Identity,
                cont.Stepper.CheckpointStore,
                cont.Stepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);

            var store = new SaveSlotStore(tempDir);
            Assert.That(store.Save(SaveSlotStore.Manual2, capture.Value!).Succeeded, Is.True);
            Assert.That(store.Save(SaveSlotStore.Autosave, capture.Value!).Succeeded, Is.True);

            var host = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed, catalogOverride: catalog));
            var restore = SaveResumeDriver.TryRestoreFromSlot(
                host,
                store,
                SaveSlotStore.Manual2,
                CatalogArgs(catalog));
            Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

            var restored = host.Active;
            Assert.That(restored.Facade.TryResumeSim(explicitOverride: true, out var resumeErr), Is.True, resumeErr);

            StepTicks(cont, 4);
            StepTicks(restored, 4);

            SaveDifferentialAssert.AssertFullParity(cont, restored, "Real SQLite gauntlet-t1-patrol-a ORBAT N=6, M=4");
            Assert.That(ComputeFileSha256Hex(dbPath), Is.EqualTo(dbShaBefore), "Shipped SQLite DB must remain 100% read-only.");
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
            DeleteScratchDir(tempDir);
        }
    }

    [Test]
    public void Restore_latency_benchmark_across_tick_depths_and_accelerations_stays_under_30s_ceiling()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            const string scenarioId = "baltic-patrol";
            const int seed = 42;
            const int measuredIterations = 3;
            var matrix = new (int Ticks, int Accel)[]
            {
                (60, 1),
                (180, 1),
                (360, 1),
                (600, 1),
                (60, 4),
                (180, 4),
                (360, 4),
                (600, 4),
            };

            // Warmup JIT before timed cells
            {
                var warmupBytes = BuildBenchmarkEnvelope(scenarioId, seed, ticks: 20, accel: 1);
                var warmupHost = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed));
                var warmupRes = SaveResumeDriver.TryRestore(warmupHost, warmupBytes);
                Assert.That(warmupRes.Succeeded, Is.True, warmupRes.FailureReason);
            }

            var report = new StringBuilder();
            report.AppendLine("| Ticks (Observed) | Accel | PipelineTick | Payload Bytes | Min (ms) | Median (ms) | Max (ms) | Max (s) | Ceiling (<30.0s) |");
            report.AppendLine("|---|---|---|---|---|---|---|---|---|");

            foreach (var (ticks, accel) in matrix)
            {
                var saveBytes = BuildBenchmarkEnvelope(scenarioId, seed, ticks, accel);
                var host = new HeadlessSaveResumeHost(CreateUnit(scenarioId, seed));

                var samplesMs = new double[measuredIterations];
                ulong pipelineTickReached = 0;
                for (var iter = 0; iter < measuredIterations; iter++)
                {
                    var sw = Stopwatch.StartNew();
                    var res = SaveResumeDriver.TryRestore(host, saveBytes);
                    sw.Stop();
                    Assert.That(res.Succeeded, Is.True, $"T={ticks}, {accel}x iter={iter}: {res.FailureReason}");
                    samplesMs[iter] = sw.Elapsed.TotalMilliseconds;
                    pipelineTickReached = host.Active.Stepper.PipelineTick;
                }

                Array.Sort(samplesMs);
                var minMs = samplesMs[0];
                var medianMs = samplesMs[measuredIterations / 2];
                var maxMs = samplesMs[measuredIterations - 1];
                var maxSec = maxMs / 1000.0;

                report.AppendLine(
                    FormattableString.Invariant($"| {ticks} | {accel}x | {pipelineTickReached} | {saveBytes.Length} | {minMs:F2} | {medianMs:F2} | {maxMs:F2} | {maxSec:F4} | PASS |"));

                SaveDifferentialAssert.AssertRestoreLatencyUnderCeiling(
                    maxSec,
                    RestoreLatencyCeilingSeconds,
                    ticks,
                    accel);
            }

            TestContext.WriteLine(report.ToString());
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    private static byte[] BuildBenchmarkEnvelope(string scenarioId, int seed, int ticks, int accel)
    {
        var unit = CreateUnit(scenarioId, seed);
        if (accel > 1)
        {
            Assert.That(unit.Facade.TrySetTimeAccelerationFactor(accel, out var accelErr), Is.True, accelErr);
        }

        StepTicks(unit, 1);
        Assert.That(unit.Facade.TryTakeDirectControl(new EntityKey(1), 1.0), Is.True);
        Assert.That(unit.Facade.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Move, 1.0, RiskLevel.Low), Is.True);
        StepTicks(unit, 1);
        Assert.That(unit.Facade.TryReleaseDirectControl(new EntityKey(1), 2.0), Is.True);
        Assert.That(unit.Facade.TryApplyDoctrineOverride(new TargetId("u1"), "WeaponsTight", 2.0), Is.True);

        if (ticks > 2)
        {
            StepTicks(unit, ticks - 2);
        }

        var capture = unit.Facade.TryCaptureEnvelope(
            unit.Identity,
            unit.Stepper.CheckpointStore,
            unit.Stepper.CombinedWorldHash);
        Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
        return SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);
    }

    private static SaveResumeSessionUnit CreateUnit(
        string scenarioPolicyId,
        int seed,
        bool reportWatchTransitions = false,
        ICatalogReader? catalogOverride = null)
    {
        var world = new ReplayableSimWorld();
        var stepper = BalticReplayStepper.Create(
            seed: seed,
            scenarioPolicyId: scenarioPolicyId,
            mvpEngagement: true,
            catalog: catalogOverride,
            externalOrderSink: world,
            reportWatchTransitions: reportWatchTransitions);
        world.BindInnerSnapshot(stepper.Snapshot, stepper.Bridge.Registry, seed);
        var facade = new SessionCommandFacade(
            stepper.Bridge,
            observedTickProvider: () => stepper.ObservedTick,
            combinedWorldHashProvider: () => stepper.CombinedWorldHash);

        var catalog = catalogOverride ?? CatalogReaderFactory.ResolveForScenario(scenarioPolicyId);
        var identity = SaveIdentityContext.Create(
            buildId: "test-build-1.0",
            scenarioPolicyId: scenarioPolicyId,
            catalogSnapshotId: "snap-test",
            catalogReader: catalog,
            globalSeed: seed);

        return new SaveResumeSessionUnit(world, world, world, stepper.Bridge, stepper, facade, identity);
    }

    private static void StepTicks(SaveResumeSessionUnit unit, int count)
    {
        for (var i = 0; i < count; i++)
        {
            unit.Stepper.StepOneObservedTick();
            unit.World.AdvanceKinematics(1.0);
            unit.Facade.SyncShadowStateAfterTick();
        }
    }

    private static Func<SaveIdentityContext, SaveCandidateRuntimeArgs> CatalogArgs(ICatalogReader catalog) =>
        _ => new SaveCandidateRuntimeArgs(Catalog: catalog);

    private static string ResolveBalticCatalogDbPath()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "assets", "data", "catalog", "baltic_patrol.db");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        throw new FileNotFoundException("assets/data/catalog/baltic_patrol.db");
    }

    private static string ComputeFileSha256Hex(string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string CreateScratchTempDir()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "aegis-save-diff-proof-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(baseDir);
        return baseDir;
    }

    private static void DeleteScratchDir(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private static byte[] MutateRawJson(byte[] validBytes, Action<JsonObject> mutate)
    {
        var root = JsonNode.Parse(validBytes)!.AsObject();
        mutate(root);
        return Encoding.UTF8.GetBytes(root.ToJsonString());
    }

    private static byte[] ReSignEnvelope(byte[] validBytes, Action<JsonObject> mutate, bool preserveJournalDigest = false)
    {
        var root = JsonNode.Parse(validBytes)!.AsObject();
        mutate(root);

        var rows = new List<SaveJournalRow>();
        foreach (var node in root["inputJournal"]!["rows"]!.AsArray())
        {
            var r = node!.AsObject();
            var argsObj = r["arguments"]!.AsObject();
            SaveTraitValues? traits = null;
            if (argsObj["traits"] is JsonObject t)
            {
                traits = new SaveTraitValues(
                    t["aggression"]!.GetValue<double>(),
                    t["riskTolerance"]!.GetValue<double>(),
                    t["reactionDelay"]!.GetValue<double>(),
                    t["errorRate"]!.GetValue<double>(),
                    t["situationalAwareness"]!.GetValue<double>(),
                    t["decisiveness"]!.GetValue<double>());
            }

            rows.Add(new SaveJournalRow(
                r["sequenceOrder"]!.GetValue<ulong>(),
                r["observedTick"]!.GetValue<ulong>(),
                r["pipelineTick"]!.GetValue<ulong>(),
                r["commandKind"]!.GetValue<string>(),
                new SaveJournalArguments(
                    EntityKeyValue: argsObj["entityKeyValue"]?.GetValue<long?>(),
                    TargetId: argsObj["targetId"]?.GetValue<string?>(),
                    AgentId: argsObj["agentId"]?.GetValue<string?>(),
                    OrderKind: argsObj["orderKind"] is JsonValue ok ? (OrderKind)ok.GetValue<int>() : null,
                    RiskLevel: argsObj["riskLevel"] is JsonValue rl ? (RiskLevel)rl.GetValue<int>() : null,
                    OrderId: argsObj["orderId"]?.GetValue<long?>(),
                    SubCommandId: argsObj["subCommandId"]?.GetValue<string?>(),
                    OptionId: argsObj["optionId"]?.GetValue<string?>(),
                    ResolvedSalvo: argsObj["resolvedSalvo"]?.GetValue<int?>(),
                    SimTime: argsObj["simTime"]!.GetValue<double>(),
                    Traits: traits,
                    PresetName: argsObj["presetName"]?.GetValue<string?>(),
                    AutonomyLevel: argsObj["autonomyLevel"] is JsonValue al ? (AutonomyLevel)al.GetValue<int>() : null,
                    AttentionBudget: argsObj["attentionBudget"]?.GetValue<double?>(),
                    IsFriendly: argsObj["isFriendly"]?.GetValue<bool?>(),
                    RoeLevelLabel: argsObj["roeLevelLabel"]?.GetValue<string?>(),
                    ExplicitOverride: argsObj["explicitOverride"]?.GetValue<bool?>(),
                    AccelerationFactor: argsObj["accelerationFactor"]?.GetValue<int?>(),
                    WatchEventId: argsObj["watchEventId"]?.GetValue<string?>(),
                    ContactId: argsObj["contactId"]?.GetValue<string?>(),
                    Affiliation: argsObj["affiliation"]?.GetValue<string?>())));
        }

        if (!preserveJournalDigest)
        {
            root["checksums"]!.AsObject()["journalDigest"] = SaveEnvelopeSerializer.ComputeJournalDigestSha256(rows);
        }

        var idObj = root["identity"]!.AsObject();
        var policies = idObj["policyHashes"]!.AsArray()
            .Select(p => new SavePolicyHashEntry(
                p!["policyId"]!.GetValue<string>(),
                p["policySha256"]!.GetValue<string>()))
            .ToArray();
        var identity = new SaveIdentityBlock(
            idObj["schemaVersion"]!.GetValue<int>(),
            idObj["buildId"]!.GetValue<string>(),
            idObj["scenarioId"]!.GetValue<string>(),
            idObj["scenarioContentHash"]!.GetValue<string>(),
            idObj["catalogSnapshotId"]!.GetValue<string>(),
            idObj["wholeCatalogHash"]!.GetValue<string>(),
            policies,
            idObj["globalSeed"]!.GetValue<int>());

        var posObj = root["position"]!.AsObject();
        var position = new SavePositionBlock(
            posObj["observedTick"]!.GetValue<ulong>(),
            posObj["pipelineTick"]!.GetValue<ulong>(),
            posObj["lastSequenceId"]!.GetValue<ulong>());

        var chkObj = root["checksums"]!.AsObject();
        var checkpoints = chkObj["checkpointRows"]!.AsArray()
            .Select(c => new SaveCheckpointRow(
                c!["simTick"]!.GetValue<ulong>(),
                c["worldHash"]!.GetValue<ulong>(),
                c["logFingerprint"]!.GetValue<string>(),
                c["lastSequenceId"]!.GetValue<ulong>()))
            .ToArray();

        var checksums = new SaveChecksumsBlock(
            chkObj["journalDigest"]!.GetValue<string>(),
            chkObj["orderLogFingerprintSha256"]!.GetValue<string>(),
            chkObj["lastWorldHash"]!.GetValue<ulong>(),
            chkObj["combinedWorldHash"]!.GetValue<ulong>(),
            checkpoints);

        root["envelopeHashSha256"] = SaveEnvelopeSerializer.ComputeEnvelopeHashSha256(
            identity,
            position,
            new SaveInputJournalBlock(rows),
            checksums);

        return Encoding.UTF8.GetBytes(root.ToJsonString());
    }
}
