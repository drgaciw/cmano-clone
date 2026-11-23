namespace ProjectAegis.Delegation.UnityAdapter.Tests.Save;

using System.Text;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Input;
using ProjectAegis.Delegation.Replay;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Save;
using ProjectAegis.Delegation.Watch;
using ProjectAegis.Sim.Scenario;

[TestFixture]
public sealed class SaveResumeDriverTests
{
    [Test]
    public void Restore_rejects_mismatched_build_catalog_scenario_policy_or_seed_without_mutating_active_host()
    {
        var (host, validBytes) = BuildActiveHostAndSaveAtTick(ticks: 3, scenarioPolicyId: "baltic-patrol", seed: 42);
        var beforeUnit = host.Active;
        var beforePublishCount = host.PublishCount;
        var beforeWorldHash = beforeUnit.Stepper.CombinedWorldHash;
        var beforeFingerprint = OrderLogReplayFingerprint.ComputeSha256Hex(beforeUnit.Bridge.Orchestrator.DecisionLog);

        var mismatches = new[]
        {
            ("buildId", SaveFailureCode.BuildMismatch, (Action<JsonObject>)(id => id["buildId"] = "other-build")),
            ("scenarioId", SaveFailureCode.ScenarioMismatch, id => id["scenarioId"] = "other-scenario"),
            ("scenarioContentHash", SaveFailureCode.ScenarioMismatch, id => id["scenarioContentHash"] = new string('9', 64)),
            ("catalogSnapshotId", SaveFailureCode.CatalogMismatch, id => id["catalogSnapshotId"] = "other-snap"),
            ("wholeCatalogHash", SaveFailureCode.CatalogMismatch, id => id["wholeCatalogHash"] = new string('8', 64)),
            ("policyHashes", SaveFailureCode.PolicyMismatch, id => id["policyHashes"]!.AsArray()[0]!.AsObject()["policySha256"] = new string('7', 64)),
            ("globalSeed", SaveFailureCode.SeedMismatch, id => id["globalSeed"] = 9999),
        };

        var constructCount = 0;
        foreach (var (name, expectedCode, mutateIdentity) in mismatches)
        {
            var modified = ReSignEnvelope(validBytes, root => mutateIdentity(root["identity"]!.AsObject()));
            var result = SaveResumeDriver.TryRestore(
                host,
                modified,
                _ =>
                {
                    constructCount++;
                    return new SaveCandidateRuntimeArgs();
                });

            Assert.That(result.Succeeded, Is.False, name);
            Assert.That(result.FailureCode, Is.EqualTo(expectedCode), name);
            Assert.That(constructCount, Is.EqualTo(0), $"Candidate must NOT be constructed for {name}.");
            Assert.That(ReferenceEquals(host.Active, beforeUnit), Is.True, name);
            Assert.That(host.PublishCount, Is.EqualTo(beforePublishCount), name);
            Assert.That(host.Active.Stepper.CombinedWorldHash, Is.EqualTo(beforeWorldHash), name);
            Assert.That(OrderLogReplayFingerprint.ComputeSha256Hex(host.Active.Bridge.Orchestrator.DecisionLog), Is.EqualTo(beforeFingerprint), name);
        }
    }

    [Test]
    public void Restore_rejects_empty_catalog_snapshot_hash_without_wildcard()
    {
        var (host, validBytes) = BuildActiveHostAndSaveAtTick(ticks: 2, scenarioPolicyId: "baltic-patrol", seed: 42);
        var modified = ReSignEnvelope(validBytes, root =>
        {
            root["identity"]!.AsObject()["wholeCatalogHash"] = "";
        });

        var constructed = false;
        var result = SaveResumeDriver.TryRestore(
            host,
            modified,
            _ =>
            {
                constructed = true;
                return new SaveCandidateRuntimeArgs();
            });

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo(SaveFailureCode.CatalogMismatch));
        Assert.That(constructed, Is.False);
    }

    [Test]
    public void Restore_rejects_checkpoint_mismatch_and_discards_candidate_leaving_active_host_and_side_registry_untouched()
    {
        // Active host runs baltic-v3-patrol (which populates BalticV3SideRegistry!)
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            var (host, validBytes) = BuildActiveHostAndSaveAtTick(ticks: 4, scenarioPolicyId: "baltic-v3-patrol", seed: 42);
            var activeSideForUcavBlue = BalticV3SideRegistry.GetSideForUnit("ucav-blue");
            var activeSideForUcavRed = BalticV3SideRegistry.GetSideForUnit("ucav-red");
            var beforeUnit = host.Active;

            var tampered = ReSignEnvelope(validBytes, root =>
            {
                root["checksums"]!.AsObject()["lastWorldHash"] = 12345UL;
            });

            var result = SaveResumeDriver.TryRestore(host, tampered, _ => new SaveCandidateRuntimeArgs());
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureCode, Is.EqualTo(SaveFailureCode.ReplayMismatch));
            Assert.That(ReferenceEquals(host.Active, beforeUnit), Is.True);
            Assert.That(BalticV3SideRegistry.GetSideForUnit("ucav-blue"), Is.EqualTo(activeSideForUcavBlue));
            Assert.That(BalticV3SideRegistry.GetSideForUnit("ucav-red"), Is.EqualTo(activeSideForUcavRed));
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void Restore_rejects_omitted_state_only_row_even_when_world_hash_and_log_fingerprint_match()
    {
        // Execute a state-only mutation (TryRebindAgentTraits) immediately before save
        var (host, _) = BuildActiveHostAndSaveAtTick(
            ticks: 2,
            scenarioPolicyId: "baltic-patrol",
            seed: 42,
            beforeSaveAction: unit =>
            {
                // Make sure u1 has an agent with Assisted autonomy or PlanningOnly-compatible edit
                var u1 = unit.Bridge.Registry.Bindings.First(b => b.TargetId.Value == "u1");
                if (u1.Target.Slot.Active is Controllers.AgentController agent)
                {
                    agent.Autonomy = AutonomyLevel.Assisted;
                    unit.Facade.SyncShadowStateAfterTick();
                    Assert.That(unit.Facade.TryRebindAgentTraits(new TargetId("u1"), PersonalityCatalog.All[2].Traits), Is.True);
                }
            });

        // Capture a valid envelope, then tamper only by dropping the state-only row and recomputing digest/envelope hash
        // to simulate an omitted row where world hash & order-log fingerprint still match!
        var validEnv = host.Active.Facade.TryCaptureEnvelope(host.Active.Identity, host.Active.Stepper.CheckpointStore, host.Active.Stepper.CombinedWorldHash);
        Assert.That(validEnv.Succeeded, Is.True);
        var validBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(validEnv.Value!);

        // Case A: Digest kept from original, row dropped -> pre-build integrity rejects
        var rootKeepDigest = JsonNode.Parse(validBytes)!.AsObject();
        rootKeepDigest["inputJournal"]!.AsObject()["rows"]!.AsArray().Clear();
        var resKeep = SaveResumeDriver.TryRestore(host, Encoding.UTF8.GetBytes(rootKeepDigest.ToJsonString()), _ => new SaveCandidateRuntimeArgs());
        Assert.That(resKeep.Succeeded, Is.False);

        // Case B: Re-signed envelope with the state-only row dropped at save, while original active host had rebind applied
        // Notice: even if re-signed with 0 rows and same LastWorldHash & OrderLogFingerprintSha256,
        // the candidate's replayed traits differ from original, OR if digest mismatches, restore fails!
        // Here we also test dropping an already-recorded state-only row from the re-signed envelope against expected digest:
        var forgedBytes = ReSignEnvelope(validBytes, root =>
        {
            // Keep original JournalDigest so Step 2 or Step 7 catches mismatch when rows omit the state-only row!
            root["inputJournal"]!.AsObject()["rows"]!.AsArray().Clear();
        }, preserveJournalDigest: true);

        var resForged = SaveResumeDriver.TryRestore(host, forgedBytes, _ => new SaveCandidateRuntimeArgs());
        Assert.That(resForged.Succeeded, Is.False);
        Assert.That(resForged.FailureCode, Is.EqualTo(SaveFailureCode.IntegrityMismatch));
    }

    [Test]
    public void Restore_clears_watch_ack_and_dismiss_flags_pauses_sim_and_atomically_swaps_world_snapshot_sink_bridge()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            var (host, _) = BuildActiveHostAndSaveAtTick(
                ticks: 3,
                scenarioPolicyId: "baltic-patrol",
                seed: 42,
                reportWatchTransitions: true,
                beforeSaveAction: unit =>
                {
                    Assert.That(unit.Bridge.Session!.WatchQueue.Cards, Has.Count.GreaterThanOrEqualTo(1));
                    var cardId = unit.Bridge.Session.WatchQueue.Cards[0].EventId;
                    Assert.That(unit.Facade.TryAcknowledgeWatchAttention(cardId), Is.True);
                    Assert.That(unit.Facade.TryResumeSim(explicitOverride: true, out _), Is.True);
                });

            var capture = host.Active.Facade.TryCaptureEnvelope(
                host.Active.Identity,
                host.Active.Stepper.CheckpointStore,
                host.Active.Stepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
            var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

            var oldUnit = host.Active;
            var oldWorld = oldUnit.World;
            var oldSnapshot = oldUnit.Snapshot;
            var oldSink = oldUnit.Sink;
            var oldBridge = oldUnit.Bridge;

            var restore = SaveResumeDriver.TryRestore(
                host,
                saveBytes,
                _ => new SaveCandidateRuntimeArgs(ReportWatchTransitions: true));
            Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

            var restored = host.Active;
            Assert.That(ReferenceEquals(restored, oldUnit), Is.False);
            Assert.That(ReferenceEquals(restored.World, oldWorld), Is.False);
            Assert.That(ReferenceEquals(restored.Snapshot, oldSnapshot), Is.False);
            Assert.That(ReferenceEquals(restored.Sink, oldSink), Is.False);
            Assert.That(ReferenceEquals(restored.Bridge, oldBridge), Is.False);
            Assert.That(restored.Bridge.AttachReplayViewer, Is.False);
            Assert.That(restored.Bridge.Session!.IsSimPaused, Is.True, "Restored session must be paused-on-load.");
            Assert.That(restored.Bridge.Session.WatchQueue.Cards, Has.Count.EqualTo(1));
            Assert.That(restored.Bridge.Session.WatchQueue.Cards[0].IsAcknowledged, Is.False);
            Assert.That(restored.Bridge.Session.WatchQueue.Cards[0].IsDismissed, Is.False);
            Assert.That(host.PublishCount, Is.EqualTo(1));
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void ReplayableSimWorld_applies_and_tracks_order_sink_side_effects_not_noop()
    {
        var world = new ReplayableSimWorld();
        world.SeedUnitPose("u1", layoutSeed: 42, hostile: false);
        Assert.That(world.TryGetKinematicPose("u1", out var initialPose), Is.True);

        world.ApplyOrder(new EntityKey(1), new Order(new OrderId(1), new TargetId("u1"), 1.0, OrderKind.Move, RiskLevel.Low));
        world.ApplyOrder(new EntityKey(1), new Order(new OrderId(2), new TargetId("u1"), 1.0, OrderKind.SetEmcon, RiskLevel.Low));
        world.AdvanceKinematics(300.0);

        Assert.That(world.AppliedOrders, Has.Count.EqualTo(2));
        Assert.That(world.GetPlottedCourse("u1"), Is.Not.Null);
        Assert.That(world.TryGetKinematicPose("u1", out var movedPose), Is.True);
        Assert.That(
            movedPose.NormalizedX != initialPose.NormalizedX || movedPose.NormalizedY != initialPose.NormalizedY,
            Is.True,
            "Move order + AdvanceKinematics must move the unit pose.");
        Assert.That(world.EmconPostures["u1"], Is.EqualTo("active"));
        Assert.That(world.ComputeSideEffectsDigestSha256(), Has.Length.EqualTo(64));
    }

    [Test]
    public void Restore_and_resume_with_non_engage_orders_move_hold_set_emcon_matches_continuous_run()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            const string scenarioId = "baltic-patrol";
            const int seed = 42;
            const int mTicks = 4;

            // Continuous N + M run (N = 6, M = 4 -> total 10 ticks)
            var contWorld = new ReplayableSimWorld();
            var contStepper = BalticReplayStepper.Create(
                seed: seed,
                scenarioPolicyId: scenarioId,
                mvpEngagement: true,
                externalOrderSink: contWorld,
                reportWatchTransitions: false);
            contWorld.BindInnerSnapshot(contStepper.Snapshot, contStepper.Bridge.Registry, seed);
            var contFacade = new SessionCommandFacade(
                contStepper.Bridge,
                observedTickProvider: () => contStepper.ObservedTick);

            var catalog = CatalogReaderFactory.ResolveForScenario(scenarioId);
            var identity = SaveIdentityContext.Create(
                buildId: "test-build-1.0",
                scenarioPolicyId: scenarioId,
                catalogSnapshotId: "snap-test",
                catalogReader: catalog,
                globalSeed: seed);

            // Tick 1
            contStepper.StepOneObservedTick();
            contWorld.AdvanceKinematics(1.0);
            contFacade.SyncShadowStateAfterTick();

            // Take direct control of u1 (EntityKey 1) and enqueue Move
            Assert.That(contFacade.TryTakeDirectControl(new EntityKey(1), contStepper.ObservedTick), Is.True);
            Assert.That(contFacade.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Move, contStepper.ObservedTick), Is.True);

            // Tick 2: Move dispatches through BalticHeadlessSnapshot -> ReplayableSimWorld
            contStepper.StepOneObservedTick();
            contWorld.AdvanceKinematics(1.0);
            contFacade.SyncShadowStateAfterTick();

            // Enqueue Hold via hold-fire attack option
            Assert.That(contFacade.TryEnqueueAttackOption(new EntityKey(1), "hold-fire", contWorld, out var holdErr), Is.True, holdErr);

            // Tick 3: Hold dispatches
            contStepper.StepOneObservedTick();
            contWorld.AdvanceKinematics(1.0);
            contFacade.SyncShadowStateAfterTick();

            // Enqueue SetEmcon
            Assert.That(contFacade.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.SetEmcon, contStepper.ObservedTick), Is.True);

            // Ticks 4..6: SetEmcon dispatches; reach N = 6
            for (var t = 4; t <= 6; t++)
            {
                contStepper.StepOneObservedTick();
                contWorld.AdvanceKinematics(1.0);
                contFacade.SyncShadowStateAfterTick();
            }

            var capture = contFacade.TryCaptureEnvelope(identity, contStepper.CheckpointStore, contStepper.CombinedWorldHash);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
            var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value!);

            // Finish continuous run for M more ticks (ticks 7..10)
            for (var t = 0; t < mTicks; t++)
            {
                contStepper.StepOneObservedTick();
                contWorld.AdvanceKinematics(1.0);
                contFacade.SyncShadowStateAfterTick();
            }

            // Build fresh host to restore into
            var liveWorld = new ReplayableSimWorld();
            var liveStepper = BalticReplayStepper.Create(
                seed: seed,
                scenarioPolicyId: scenarioId,
                mvpEngagement: true,
                externalOrderSink: liveWorld,
                reportWatchTransitions: false);
            liveWorld.BindInnerSnapshot(liveStepper.Snapshot, liveStepper.Bridge.Registry, seed);
            var liveFacade = new SessionCommandFacade(
                liveStepper.Bridge,
                observedTickProvider: () => liveStepper.ObservedTick);
            var restoreHost = new HeadlessSaveResumeHost(
                new SaveResumeSessionUnit(liveWorld, liveWorld, liveWorld, liveStepper.Bridge, liveStepper, liveFacade, identity));

            var restore = SaveResumeDriver.TryRestore(restoreHost, saveBytes);
            Assert.That(restore.Succeeded, Is.True, restore.FailureReason);

            var restored = restoreHost.Active;
            Assert.That(restored.Bridge.Session!.IsSimPaused, Is.True);
            Assert.That(restored.Facade.TryResumeSim(explicitOverride: true, out var resumeErr), Is.True, resumeErr);

            for (var t = 0; t < mTicks; t++)
            {
                restored.Stepper.StepOneObservedTick();
                restored.World.AdvanceKinematics(1.0);
                restored.Facade.SyncShadowStateAfterTick();
            }

            Assert.That(restored.Stepper.CombinedWorldHash, Is.EqualTo(contStepper.CombinedWorldHash));
            Assert.That(
                OrderLogReplayFingerprint.ComputeSha256Hex(restored.Bridge.Orchestrator.DecisionLog),
                Is.EqualTo(OrderLogReplayFingerprint.ComputeSha256Hex(contStepper.Bridge.Orchestrator.DecisionLog)));
            Assert.That(
                restored.Bridge.Orchestrator.DecisionLog.ComputeFingerprint(),
                Is.EqualTo(contStepper.Bridge.Orchestrator.DecisionLog.ComputeFingerprint()));
            Assert.That(restored.World.AppliedOrders.Count, Is.EqualTo(contWorld.AppliedOrders.Count));
            Assert.That(restored.World.AppliedOrders.Count(o => o.Kind == OrderKind.Move), Is.EqualTo(1));
            Assert.That(restored.World.AppliedOrders.Count(o => o.Kind == OrderKind.Hold), Is.EqualTo(1));
            Assert.That(restored.World.AppliedOrders.Count(o => o.Kind == OrderKind.SetEmcon), Is.EqualTo(1));
            Assert.That(restored.World.EmconPostures["u1"], Is.EqualTo("active"));
            Assert.That(restored.World.ComputeSideEffectsDigestSha256(), Is.EqualTo(contWorld.ComputeSideEffectsDigestSha256()));
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void ReplayableSimWorld_forwards_to_standalone_inner_sink_and_guards_against_reentrancy()
    {
        var world = new ReplayableSimWorld();
        var standaloneSink = new RecordingAndOptionalReentrySink(reenterTarget: null);
        var stubSnapshot = new StubInnerSnapshot(1.0);
        world.BindInnerSnapshot(
            stubSnapshot,
            registry: null,
            layoutSeed: 42,
            additionalInnerSink: standaloneSink);

        world.ApplyOrder(new EntityKey(1), new Order(new OrderId(1), new TargetId("u1"), 1.0, OrderKind.Move, RiskLevel.Low));
        Assert.That(world.AppliedOrders, Has.Count.EqualTo(1));
        Assert.That(standaloneSink.ReceivedOrders, Has.Count.EqualTo(1));

        // Even if an inner sink calls back into world.ApplyOrder, re-entrancy guard prevents infinite recursion & duplication
        var circularWorld = new ReplayableSimWorld();
        var reentrantSink = new RecordingAndOptionalReentrySink(reenterTarget: circularWorld);
        circularWorld.BindInnerSnapshot(
            reentrantSink,
            registry: null,
            layoutSeed: 42,
            forwardOrdersToInnerSink: true);

        circularWorld.ApplyOrder(new EntityKey(1), new Order(new OrderId(2), new TargetId("u1"), 2.0, OrderKind.SetSensors, RiskLevel.Low));
        Assert.That(circularWorld.AppliedOrders, Has.Count.EqualTo(1));
        Assert.That(reentrantSink.ReceivedOrders, Has.Count.EqualTo(1));
        Assert.That(circularWorld.SensorPostures["u1"], Is.EqualTo("active"));
    }

    [Test]
    public void SessionCommandFacade_uses_combinedWorldHashProvider_when_combinedWorldHash_omitted_and_restores_cleanly()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            const string scenarioId = "baltic-patrol";
            const int seed = 42;

            var world = new ReplayableSimWorld();
            var stepper = BalticReplayStepper.Create(
                seed: seed,
                scenarioPolicyId: scenarioId,
                mvpEngagement: true,
                externalOrderSink: world,
                reportWatchTransitions: false);
            world.BindInnerSnapshot(stepper.Snapshot, stepper.Bridge.Registry, seed);
            var facade = new SessionCommandFacade(
                stepper.Bridge,
                observedTickProvider: () => stepper.ObservedTick,
                combinedWorldHashProvider: () => stepper.CombinedWorldHash);

            for (var t = 0; t < 3; t++)
            {
                stepper.StepOneObservedTick();
                world.AdvanceKinematics(1.0);
                facade.SyncShadowStateAfterTick();
            }

            // Omit combinedWorldHash argument (defaults to 0); provider must supply stepper.CombinedWorldHash
            var capture = facade.TryCaptureEnvelope(
                SaveIdentityContext.Create("test-build-1.0", scenarioId, "snap-test", CatalogReaderFactory.ResolveForScenario(scenarioId), seed),
                stepper.CheckpointStore);
            Assert.That(capture.Succeeded, Is.True, capture.FailureReason);
            Assert.That(capture.Value!.Checksums.CombinedWorldHash, Is.EqualTo(stepper.CombinedWorldHash));

            var identity = SaveIdentityContext.Create("test-build-1.0", scenarioId, "snap-test", CatalogReaderFactory.ResolveForScenario(scenarioId), seed);
            var host = new HeadlessSaveResumeHost(new SaveResumeSessionUnit(world, world, world, stepper.Bridge, stepper, facade, identity));
            var saveBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(capture.Value);
            var restore = SaveResumeDriver.TryRestore(host, saveBytes);
            Assert.That(restore.Succeeded, Is.True, restore.FailureReason);
            Assert.That(host.Active.Stepper.CombinedWorldHash, Is.EqualTo(stepper.CombinedWorldHash));
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    private sealed class StubInnerSnapshot(double simTime) : ISimWorldSnapshot
    {
        public double SimTime { get; } = simTime;
        public int ContactCount => 0;
        public int ActiveEngagementCount => 0;
        public TargetId? PrimaryHostileContactId => null;
        public bool HasFireControlTrackOnPrimaryContact => false;
        public bool ObserverRadarEmconActive => true;
        public bool IsMemberAlive(TargetId memberId) => true;
    }

    private sealed class RecordingAndOptionalReentrySink(IOrderSink? reenterTarget) : ISimWorldSnapshot, IOrderSink
    {
        public List<Order> ReceivedOrders { get; } = new();
        public double SimTime => 1.0;
        public int ContactCount => 0;
        public int ActiveEngagementCount => 0;
        public TargetId? PrimaryHostileContactId => null;
        public bool HasFireControlTrackOnPrimaryContact => false;
        public bool ObserverRadarEmconActive => true;
        public bool IsMemberAlive(TargetId memberId) => true;

        public void ApplyOrder(EntityKey entity, in Order order)
        {
            ReceivedOrders.Add(order);
            reenterTarget?.ApplyOrder(entity, in order);
        }
    }

    private static (HeadlessSaveResumeHost Host, byte[] SaveBytes) BuildActiveHostAndSaveAtTick(
        int ticks,
        string scenarioPolicyId,
        int seed,
        Action<SaveResumeSessionUnit>? beforeSaveAction = null,
        bool reportWatchTransitions = false)
    {
        var world = new ReplayableSimWorld();
        var stepper = BalticReplayStepper.Create(
            seed: seed,
            scenarioPolicyId: scenarioPolicyId,
            mvpEngagement: true,
            externalOrderSink: world,
            reportWatchTransitions: reportWatchTransitions);
        world.BindInnerSnapshot(stepper.Snapshot, stepper.Bridge.Registry, seed);
        var facade = new SessionCommandFacade(
            stepper.Bridge,
            observedTickProvider: () => stepper.ObservedTick,
            combinedWorldHashProvider: () => stepper.CombinedWorldHash);

        var catalog = CatalogReaderFactory.ResolveForScenario(scenarioPolicyId);
        var identity = SaveIdentityContext.Create(
            buildId: "test-build-1.0",
            scenarioPolicyId: scenarioPolicyId,
            catalogSnapshotId: "snap-test",
            catalogReader: catalog,
            globalSeed: seed);

        var unit = new SaveResumeSessionUnit(world, world, world, stepper.Bridge, stepper, facade, identity);
        var host = new HeadlessSaveResumeHost(unit);

        for (var t = 0; t < ticks; t++)
        {
            stepper.StepOneObservedTick();
            world.AdvanceKinematics(1.0);
            facade.SyncShadowStateAfterTick();
        }

        beforeSaveAction?.Invoke(unit);

        var envelopeResult = facade.TryCaptureEnvelope(identity, stepper.CheckpointStore, stepper.CombinedWorldHash);
        Assert.That(envelopeResult.Succeeded, Is.True, envelopeResult.FailureReason);
        return (host, SaveEnvelopeSerializer.SerializeToUtf8Bytes(envelopeResult.Value!));
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

        // Recompute envelope hash from parsed identity/position/journal/checksums
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
