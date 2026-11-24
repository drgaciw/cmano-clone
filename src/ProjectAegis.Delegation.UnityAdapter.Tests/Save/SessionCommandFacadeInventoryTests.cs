using System.Reflection;
using NUnit.Framework;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Input;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Save;
using ProjectAegis.Delegation.Watch;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Save;

[TestFixture]
public sealed class SessionCommandFacadeInventoryTests
{
    [Test]
    public void Facade_covers_every_entry_in_InputCommandInventory()
    {
        var commandMethods = typeof(SessionCommandFacade)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(m => (Method: m, Attr: m.GetCustomAttribute<SessionCommandAttribute>()))
            .Where(x => x.Attr != null)
            .ToList();

        var coveredKinds = commandMethods.Select(x => x.Attr!.Kind).ToHashSet();
        foreach (var descriptor in InputCommandInventory.All)
        {
            Assert.That(
                coveredKinds.Contains(descriptor.Kind),
                Is.True,
                $"InputCommandInventory entry '{descriptor.CommandId}' ({descriptor.Kind}) is missing from SessionCommandFacade.");
        }

        Assert.That(
            commandMethods.Count,
            Is.EqualTo(InputCommandInventory.All.Count),
            "Every SessionCommandFacade command must have a unique 1:1 InputCommandJournalKind.");

        var nonCommandPublicMethods = typeof(SessionCommandFacade)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && m.GetCustomAttribute<SessionCommandAttribute>() == null)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var methodName in nonCommandPublicMethods)
        {
            Assert.That(
                methodName,
                Is.AnyOf("SyncShadowStateAfterTick", "VerifyNoUnjournaledMutations", "TryCaptureEnvelope"),
                $"Public SessionCommandFacade method '{methodName}' is neither annotated with [SessionCommand] nor in the non-command lifecycle allowlist.");
        }
    }

    [Test]
    public void Denied_calls_do_not_record_journal_rows()
    {
        var (bridge, world, facade) = BuildMixedTestSession(attachReplayViewer: false);
        var emptyDigest = facade.Journal.ComputeDigestSha256();

        // 1. Unknown unit
        Assert.That(facade.TryTakeDirectControl(new EntityKey(999), 1.0), Is.False);
        // 2. Denied resume when pause-class watch event is unresolved
        bridge.Session!.ReportWatchAttention(new WatchAttentionEvent(
            "evt-block-1", WatchAttentionKind.HostileOrUnknownContact, WatchAttentionPriority.Critical, 1, "c1"));
        facade.SyncShadowStateAfterTick();
        Assert.That(facade.TryResumeSim(explicitOverride: false, out _), Is.False);
        // 3. LoopPolicyGate denial during Executing (PlanningOnly / FullAutonomous etc.)
        var agent = new AgentController(
            new AgentId("a-u1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            new SeededRng(42, 1),
            new StubPatrolPolicy(),
            20);
        // Take control -> no active agent -> TryPauseAgentActions denied
        Assert.That(facade.TryPauseAgentActions(new TargetId("u1"), 1.0), Is.False);

        // 4. Replay viewer attached denies all calls
        bridge.AttachReplayViewer = true;
        Assert.That(facade.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Move, 1.0), Is.False);
        Assert.That(facade.TryIssuePlayerCommand(new EntityKey(1), "hold", 1.0, out _), Is.False);
        Assert.That(facade.TryPauseSim(out _), Is.False);
        Assert.That(facade.TrySetTimeAccelerationFactor(2, out _), Is.False);
        Assert.That(facade.TryReclassifyContact("c1", "Hostile", 1.0, out _), Is.False);
        Assert.That(facade.TryDropContact("c1", 1.0, out _), Is.False);
        Assert.That(facade.TryAcknowledgeWatchAttention("evt-block-1"), Is.False);
        Assert.That(facade.TryDismissWatchAttention("evt-block-1"), Is.False);
        Assert.That(facade.TryApplyDoctrineOverride(new TargetId("u1"), "WeaponsTight", 1.0), Is.False);
        Assert.That(facade.TryAssignAgentController(new TargetId("u1"), "a-x", PersonalityCatalog.All[0].Traits, AutonomyLevel.Assisted, 20, null, true, 1.0), Is.False);
        Assert.That(facade.TryRebriefAgent(new TargetId("u1"), "Defensive", 1.0), Is.False);
        _ = agent;
        _ = world;

        Assert.That(facade.Journal.Rows, Is.Empty);
        Assert.That(facade.Journal.ComputeDigestSha256(), Is.EqualTo(emptyDigest));
    }

    [Test]
    public void Accepted_calls_record_exact_ticks_payload_and_sequential_digest()
    {
        ulong observedTick = 3;
        var (bridge, world, facade) = BuildMixedTestSession(attachReplayViewer: false, observedTickProvider: () => observedTick);
        world.AdvanceTo(3.0);

        Assert.That(facade.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Hold, 3.0, RiskLevel.High), Is.True);
        Assert.That(facade.TryIssuePlayerCommand(new EntityKey(1), "move", 3.0, out _), Is.True);
        Assert.That(facade.TrySetTimeAccelerationFactor(4, out _), Is.True);

        var options = bridge.GetAttackMenuOptions("u1", world);
        if (options.Any(o => o.Enabled))
        {
            var opt = options.First(o => o.Enabled);
            Assert.That(facade.TryEnqueueAttackOption(new EntityKey(1), opt.Id, world, out _), Is.True);
        }

        Assert.That(facade.Journal.Rows.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(facade.Journal.Rows[0].ObservedTick, Is.EqualTo(3UL));
        Assert.That(facade.Journal.Rows[0].Arguments.RiskLevel, Is.EqualTo(RiskLevel.High));
        Assert.That(facade.Journal.Rows[1].Arguments.SubCommandId, Is.EqualTo("move"));
        Assert.That(facade.Journal.Rows[2].Arguments.AccelerationFactor, Is.EqualTo(4));

        var digestOriginal = facade.Journal.ComputeDigestSha256();

        // Swapping order changes sequential digest
        var swapped = new SessionInputJournal();
        swapped.Append(
            facade.Journal.Rows[1].ObservedTick,
            facade.Journal.Rows[1].PipelineTick,
            InputCommandJournalKind.IssuePlayerCommand,
            facade.Journal.Rows[1].Arguments);
        swapped.Append(
            facade.Journal.Rows[0].ObservedTick,
            facade.Journal.Rows[0].PipelineTick,
            InputCommandJournalKind.EnqueueHumanOrder,
            facade.Journal.Rows[0].Arguments);
        swapped.Append(
            facade.Journal.Rows[2].ObservedTick,
            facade.Journal.Rows[2].PipelineTick,
            InputCommandJournalKind.SetTimeAccelerationFactor,
            facade.Journal.Rows[2].Arguments);
        for (var i = 3; i < facade.Journal.Rows.Count; i++)
        {
            var r = facade.Journal.Rows[i];
            InputCommandInventory.TryGet(r.CommandKind, out var desc);
            swapped.Append(r.ObservedTick, r.PipelineTick, desc!.Kind, r.Arguments);
        }

        Assert.That(swapped.ComputeDigestSha256(), Is.Not.EqualTo(digestOriginal));

        // Dropping a state-only row changes sequential digest
        var dropped = new SessionInputJournal();
        for (var i = 0; i < facade.Journal.Rows.Count; i++)
        {
            if (i == 2) continue;
            var r = facade.Journal.Rows[i];
            InputCommandInventory.TryGet(r.CommandKind, out var desc);
            dropped.Append(r.ObservedTick, r.PipelineTick, desc!.Kind, r.Arguments);
        }

        Assert.That(dropped.ComputeDigestSha256(), Is.Not.EqualTo(digestOriginal));
    }

    [Test]
    public void Unjournaled_mutation_on_bridge_or_session_fails_save_verification()
    {
        // 1. Direct DecisionLog mutation
        {
            var (bridge, _, facade) = BuildMixedTestSession(false);
            bridge.Orchestrator.DecisionLog.AppendEventFired(new EventFiredRecord(0, 1.0, 1, "rogue", "ROGUE"));
            Assert.That(facade.VerifyNoUnjournaledMutations(out var violation), Is.False);
            Assert.That(violation, Does.Contain("DecisionLog"));
        }

        // 2. Direct PendingApprovalQueue approve outside facade
        {
            var (bridge, _, facade) = BuildMixedTestSession(false);
            bridge.Orchestrator.PendingApprovalQueue.Enqueue(
                new Order(new OrderId(77), new TargetId("u1"), 1.0, OrderKind.Hold, RiskLevel.Low));
            facade.SyncShadowStateAfterTick();
            Assert.That(bridge.Orchestrator.TryApprovePendingOrder(new OrderId(77)), Is.True);
            Assert.That(facade.VerifyNoUnjournaledMutations(out var violation), Is.False);
            Assert.That(violation, Does.Contain("PendingApproval"));
        }

        // 3. Direct PolicySnapshots mutation
        {
            var (bridge, _, facade) = BuildMixedTestSession(false);
            bridge.Orchestrator.PolicySnapshots.Capture(new TargetId("u1"), new EffectivePolicy(RoeLevel.WeaponsTight, 1), 1);
            Assert.That(facade.VerifyNoUnjournaledMutations(out var violation), Is.False);
            Assert.That(violation, Is.Not.Null.And.Not.Empty);
        }

        // 4. Direct WatchQueue mutation
        {
            var (bridge, _, facade) = BuildMixedTestSession(false);
            bridge.Session!.WatchQueue.Enqueue(new WatchAttentionEvent(
                "evt-1", WatchAttentionKind.HostileOrUnknownContact, WatchAttentionPriority.High, 1, "c1"));
            facade.SyncShadowStateAfterTick();
            Assert.That(bridge.Session.WatchQueue.TryAcknowledge("evt-1"), Is.True);
            Assert.That(facade.VerifyNoUnjournaledMutations(out var violation), Is.False);
            Assert.That(violation, Does.Contain("WatchQueue"));
        }

        // 5. Direct SimClock PauseSim / SetTimeAccelerationFactor
        {
            var (bridge, _, facade) = BuildMixedTestSession(false);
            bridge.Session!.PauseSim();
            Assert.That(facade.VerifyNoUnjournaledMutations(out var violation), Is.False);
            Assert.That(violation, Does.Contain("SimClock"));
        }

        // 6. Direct NextEngageSalvoOverride mutation
        {
            var (bridge, _, facade) = BuildMixedTestSession(false);
            bridge.Session!.NextEngageSalvoOverride = 3;
            Assert.That(facade.VerifyNoUnjournaledMutations(out var violation), Is.False);
            Assert.That(violation, Does.Contain("NextEngageSalvoOverride"));
        }

        // 7. Direct AgentController traits / autonomy mutation
        {
            var (bridge, _, facade) = BuildMixedTestSession(false);
            var agent = bridge.Orchestrator.CreateAgent(
                new AgentId("a-u1"),
                PersonalityCatalog.All[0].Traits,
                AutonomyLevel.Assisted);
            bridge.Orchestrator.AssignAgentToTarget(agent, bridge.Registry.Bindings[0].Target, EffectivePolicy.DefaultFree);
            facade.SyncShadowStateAfterTick();
            agent.RebindTraits(PersonalityCatalog.All[1].Traits);
            Assert.That(facade.VerifyNoUnjournaledMutations(out var violation), Is.False);
            Assert.That(violation, Does.Contain("Controller"));
        }

        // 8. Direct AgentExperienceBlob.Metrics / TrustSignals mutation
        {
            var (bridge, _, facade) = BuildMixedTestSession(false);
            var agent = bridge.Orchestrator.CreateAgent(
                new AgentId("a-u1"),
                PersonalityCatalog.All[0].Traits,
                AutonomyLevel.Assisted);
            bridge.Orchestrator.AssignAgentToTarget(agent, bridge.Registry.Bindings[0].Target, EffectivePolicy.DefaultFree);
            facade.SyncShadowStateAfterTick();
            agent.Experience.Metrics["rogue"] = 1.0;
            Assert.That(facade.VerifyNoUnjournaledMutations(out var violation), Is.False);
            Assert.That(violation, Does.Contain("Experience"));
        }
    }

    [Test]
    public void Unjournaled_mutation_followed_by_accepted_facade_command_retains_violation_and_blocks_save_capture()
    {
        var identity = SaveIdentityContext.Create(
            buildId: "build-test",
            scenarioPolicyId: "baltic-patrol",
            catalogSnapshotId: "baltic-fixture",
            catalogReader: InMemoryCatalogReader.BalticPatrolFixture(),
            globalSeed: 42);

        // 1. Direct NextEngageSalvoOverride bypass followed by accepted TryPauseSim and subsequent commands/tick sync
        {
            var (bridge, _, facade) = BuildMixedTestSession(false);
            bridge.Session!.NextEngageSalvoOverride = 3;

            Assert.That(facade.TryPauseSim(out _), Is.True);
            Assert.That(facade.VerifyNoUnjournaledMutations(out var violation), Is.False);
            Assert.That(violation, Does.Contain("NextEngageSalvoOverride"));

            var capture = facade.TryCaptureEnvelope(identity);
            Assert.That(capture.Succeeded, Is.False);
            Assert.That(capture.FailureCode, Is.EqualTo(SaveFailureCode.UnjournaledMutationDetected));
            Assert.That(capture.FailureReason, Does.Contain("NextEngageSalvoOverride"));

            // Subsequent accepted commands or tick sync must not erase the latched violation.
            Assert.That(facade.TrySetTimeAccelerationFactor(2, out _), Is.True);
            facade.SyncShadowStateAfterTick();

            var captureAfterSync = facade.TryCaptureEnvelope(identity);
            Assert.That(captureAfterSync.Succeeded, Is.False);
            Assert.That(captureAfterSync.FailureCode, Is.EqualTo(SaveFailureCode.UnjournaledMutationDetected));
            Assert.That(captureAfterSync.FailureReason, Does.Contain("NextEngageSalvoOverride"));
        }

        // 2. Direct DecisionLog bypass followed by accepted TryEnqueueHumanOrder
        {
            var (bridge, _, facade) = BuildMixedTestSession(false);
            bridge.Orchestrator.DecisionLog.AppendEventFired(new EventFiredRecord(0, 1.0, 1, "rogue", "ROGUE"));

            Assert.That(facade.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Hold, 1.0), Is.True);
            var capture = facade.TryCaptureEnvelope(identity);
            Assert.That(capture.Succeeded, Is.False);
            Assert.That(capture.FailureCode, Is.EqualTo(SaveFailureCode.UnjournaledMutationDetected));
            Assert.That(capture.FailureReason, Does.Contain("DecisionLog"));
        }
    }

    [Test]
    public void WatchQueue_reset_clears_acknowledged_and_dismissed_flags_via_public_api()
    {
        var queue = new WatchAttentionQueue();
        var evtA = new WatchAttentionEvent("e-1", WatchAttentionKind.HostileOrUnknownContact, WatchAttentionPriority.Critical, 2, "c1");
        var evtB = new WatchAttentionEvent("e-2", WatchAttentionKind.OwnSideLossOrDamage, WatchAttentionPriority.High, 3, "u2");
        queue.Enqueue(evtA);
        queue.Enqueue(evtB);
        Assert.That(queue.TryAcknowledge("e-1"), Is.True);
        Assert.That(queue.TryDismiss("e-2"), Is.True);
        Assert.That(queue.HasUnresolvedPauseClass, Is.False);

        SaveResumeDriver.ClearWatchFlags(queue);

        Assert.That(queue.Cards, Has.Count.EqualTo(2));
        Assert.That(queue.Cards[0].EventId, Is.EqualTo("e-1"));
        Assert.That(queue.Cards[0].IsAcknowledged, Is.False);
        Assert.That(queue.Cards[0].IsDismissed, Is.False);
        Assert.That(queue.Cards[1].EventId, Is.EqualTo("e-2"));
        Assert.That(queue.Cards[1].IsAcknowledged, Is.False);
        Assert.That(queue.Cards[1].IsDismissed, Is.False);
        Assert.That(queue.HasUnresolvedPauseClass, Is.True);
    }

    private static (DelegationBridge Bridge, ReplayableSimWorld World, SessionCommandFacade Facade) BuildMixedTestSession(
        bool attachReplayViewer,
        Func<ulong>? observedTickProvider = null)
    {
        var bridge = new DelegationBridge(
            globalSeed: 42,
            mvpEngagement: true,
            scenarioPolicyId: "baltic-patrol",
            catalog: InMemoryCatalogReader.BalticPatrolFixture());
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var h1 = bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            new[] { u1.Target },
            new[] { h1.Target },
            PersonalityCatalog.All[0].Traits);
        bridge.AttachReplayViewer = attachReplayViewer;

        var world = new ReplayableSimWorld();
        world.BindSnapshotState(simTime: 0.0, contactCount: 1, hasFireControlTrack: true, primaryHostileId: new TargetId("hostile-1"));
        var facade = new SessionCommandFacade(bridge, observedTickProvider: observedTickProvider);
        return (bridge, world, facade);
    }
}
