namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

using System.Diagnostics;
using C2Nodes;
using Controllers;
using Core;
using MissionIntent;
using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Sim.Comms;
using ProjectAegis.Sim.Scenario;
using TaskGroupCoord;

[TestFixture]
public sealed class CoordinationDetailProjectionTests
{
    [Test]
    public void Build_without_authored_facts_leaves_escort_sector_and_shared_targeting_unauthored()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var group = bridge.Registry.RegisterGroup(new EntityKey(100), "g1");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var u2 = bridge.Registry.RegisterUnit(new EntityKey(2), "u2");
        bridge.Registry.LinkGroupMember(group.TargetId, u1.TargetId);
        bridge.Registry.LinkGroupMember(group.TargetId, u2.TargetId);

        var snapshot = new StubSnapshot(simTime: 2.0, alive: new Dictionary<string, bool> { ["u1"] = true, ["u2"] = true });
        var frame = CoordinationBridge.Build(bridge, snapshot);
        var projected = frame.Groups.Single();

        Assert.That(projected.Details, Is.Not.Null);
        Assert.That(projected.Details!.Escorts, Is.Empty);
        Assert.That(projected.Details.Sectors, Is.Empty);
        Assert.That(projected.Details.SharedTargets, Is.Empty);

        var presentation = CoordinationPresentation.Build(frame, bridge, snapshot);
        var groupView = presentation.FindGroup("g1");
        Assert.That(groupView, Is.Not.Null);
        Assert.That(groupView!.Boundaries, Is.Empty);
    }

    [Test]
    public void Build_with_smoke_escort_facts_projects_escort_sector_and_responsibility_boundaries()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);
        var snapshot = new StubSnapshot(
            simTime: 2.0,
            alive: new Dictionary<string, bool>
            {
                [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = true,
                [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
            });
        var facts = PlayModeSmokeCoordinationFactsProvider.CreateDefault();
        var frame = CoordinationBridge.Build(bridge, snapshot, facts);
        var group = frame.Groups.Single();

        Assert.That(group.Details, Is.Not.Null);
        Assert.That(group.Details!.ControllerKind, Is.EqualTo("Human"));
        Assert.That(group.Details.Escorts, Has.Count.EqualTo(1));
        Assert.That(group.Details.Escorts[0].EscortUnitId, Is.EqualTo(PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId));
        Assert.That(group.Details.Escorts[0].ProtectedUnitId, Is.EqualTo(PlayModeSmokeOrbatSeeder.FriendlyUnitId));
        Assert.That(group.Details.Escorts[0].CapabilityScope, Is.EqualTo("escort-area-defense"));
        Assert.That(group.Details.Sectors, Has.Count.EqualTo(2));
        Assert.That(group.Details.Sectors.Select(s => s.CoverageId), Is.EqualTo(new[]
        {
            PlayModeSmokeCoordinationFactsProvider.C2CoverageId,
            PlayModeSmokeCoordinationFactsProvider.EscortCoverageId,
        }));

        var presentation = CoordinationPresentation.Build(frame, bridge, snapshot);
        var view = presentation.FindGroup(PlayModeSmokeOrbatSeeder.TaskGroupId);
        Assert.That(view, Is.Not.Null);
        Assert.That(view!.Boundaries.Any(b => b.Kind == "Escort" && b.OwnerUnitId == PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId), Is.True);
        Assert.That(view.Boundaries.Count(b => b.Kind == "Sector"), Is.EqualTo(2));
        Assert.That(view.Boundaries.Any(b => b.Kind == "PackageRole" && b.OwnerUnitId == PlayModeSmokeOrbatSeeder.FriendlyUnitId), Is.True);
    }

    [Test]
    public void Build_with_preferred_hostile_map_and_primary_contact_projects_shared_targeting_boundaries()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);
        var facts = PlayModeSmokeCoordinationFactsProvider.CreateDefault();

        var preferredSnapshot = new StubSnapshot(
            simTime: 3.0,
            alive: new Dictionary<string, bool>
            {
                [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = true,
                [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
            },
            primaryHostile: new TargetId("hostile-1"),
            hasFireControlTrack: true,
            preferredHostileByShooter: new Dictionary<string, string>
            {
                [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = "hostile-9",
                [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = "hostile-9",
            });

        var preferredFrame = CoordinationBridge.Build(bridge, preferredSnapshot, facts);
        var preferredTarget = preferredFrame.Groups.Single().Details!.SharedTargets.Single();
        Assert.That(preferredTarget.TargetId, Is.EqualTo("hostile-9"));
        Assert.That(preferredTarget.AssignedUnitIds, Is.EqualTo(new[]
        {
            PlayModeSmokeOrbatSeeder.FriendlyUnitId,
            PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId,
        }));
        Assert.That(preferredTarget.SourceKind, Is.EqualTo("PREFERRED_HOSTILE_MAP"));

        var primarySnapshot = new StubSnapshot(
            simTime: 3.0,
            alive: new Dictionary<string, bool>
            {
                [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = true,
                [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
            },
            primaryHostile: new TargetId("hostile-1"),
            hasFireControlTrack: true);

        var primaryFrame = CoordinationBridge.Build(bridge, primarySnapshot, facts);
        var primaryTarget = primaryFrame.Groups.Single().Details!.SharedTargets.Single();
        Assert.That(primaryTarget.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(primaryTarget.HasFireControlTrack, Is.True);
        Assert.That(primaryTarget.SourceKind, Is.EqualTo("SNAPSHOT_PRIMARY_CONTACT"));

        var presentation = CoordinationPresentation.Build(primaryFrame, bridge, primarySnapshot);
        var groupView = presentation.FindGroup(PlayModeSmokeOrbatSeeder.TaskGroupId)!;
        Assert.That(groupView.Boundaries.Any(b => b.Kind == "SharedTarget" && b.Detail.Contains("hostile-1", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void Build_with_explicit_escort_and_shared_target_facts_overrides_fallbacks()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);
        var defaults = PlayModeSmokeCoordinationFactsProvider.CreateDefault();
        var facts = new TestFacts(
            defaults.Packages,
            defaults.Coverage,
            defaults.Intents,
            defaults.Assignments,
            escorts: new[]
            {
                new CoordinationEscortFact(
                    PlayModeSmokeOrbatSeeder.TaskGroupId,
                    PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId,
                    PlayModeSmokeOrbatSeeder.FriendlyUnitId,
                    "custom-aaw-screen"),
            },
            sharedTargets: new[]
            {
                new CoordinationSharedTargetFact(
                    PlayModeSmokeOrbatSeeder.TaskGroupId,
                    "hostile-custom",
                    new[] { PlayModeSmokeOrbatSeeder.FriendlyUnitId, PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId },
                    HasFireControlTrack: true,
                    SourceRef: "scenario:shared-target/hostile-custom"),
            });

        var snapshot = new StubSnapshot(
            simTime: 2.0,
            alive: new Dictionary<string, bool>
            {
                [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = true,
                [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
            });

        var frame = CoordinationBridge.Build(bridge, snapshot, facts);
        var details = frame.Groups.Single().Details!;
        Assert.That(details.Escorts.Single().CapabilityScope, Is.EqualTo("custom-aaw-screen"));
        Assert.That(details.SharedTargets.Single().TargetId, Is.EqualTo("hostile-custom"));
        Assert.That(details.SharedTargets.Single().SourceKind, Is.EqualTo("AUTHORED_FACT"));
        Assert.That(details.SharedTargets.Single().SourceRef, Is.EqualTo("scenario:shared-target/hostile-custom"));
    }

    [Test]
    public void PreviewGroupDecision_hold_and_withdraw_project_per_unit_effects_and_exclude_detached_units()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);
        var facts = PlayModeSmokeCoordinationFactsProvider.CreateDefault();
        var alive = new Dictionary<string, bool>
        {
            [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = true,
            [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
        };
        var snapshot = new StubSnapshot(simTime: 2.0, alive: alive);

        var nominalFrame = CoordinationBridge.Build(bridge, snapshot, facts);
        var nominalHold = CoordinationPresentation.PreviewGroupDecision(
            nominalFrame.Groups.Single(),
            CoordinationDecision.Hold,
            bridge,
            snapshot);
        Assert.That(nominalHold.IsAllowed, Is.True);
        Assert.That(nominalHold.CommandId, Is.EqualTo("hold"));
        Assert.That(nominalHold.TargetOrderKind, Is.EqualTo(OrderKind.Hold));
        Assert.That(nominalHold.UnitEffects.Count(u => u.EffectStatus == "WILL_APPLY"), Is.EqualTo(2));

        // Detach escort unit -> excluded from group scope; remaining group member stays WILL_APPLY
        Assert.That(bridge.TryTakeDirectControl(PlayModeSmokeCoordinationFactsProvider.FriendlyEscortEntityKey, simTime: 2.0), Is.True);
        var splitFrame = CoordinationBridge.Build(bridge, snapshot, facts);
        var splitWithdraw = CoordinationPresentation.PreviewGroupDecision(
            splitFrame.Groups.Single(),
            CoordinationDecision.Withdraw,
            bridge,
            snapshot);
        Assert.That(splitWithdraw.IsAllowed, Is.True);
        Assert.That(splitWithdraw.CommandId, Is.EqualTo("rtb"));
        Assert.That(splitWithdraw.TargetOrderKind, Is.EqualTo(OrderKind.ReturnToBase));
        Assert.That(splitWithdraw.UnitEffects.Single(u => u.UnitId == PlayModeSmokeOrbatSeeder.FriendlyUnitId).EffectStatus, Is.EqualTo("WILL_APPLY"));
        var detachedRow = splitWithdraw.UnitEffects.Single(u => u.UnitId == PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId);
        Assert.That(detachedRow.IncludedInGroupScope, Is.False);
        Assert.That(detachedRow.EffectStatus, Is.EqualTo("EXCLUDED_DETACHED"));
    }

    [Test]
    public void PreviewGroupDecision_reattack_remains_advisory_only_and_never_allows_direct_order()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);
        var facts = new TestFacts(
            PlayModeSmokeCoordinationFactsProvider.CreateDefault().Packages,
            PlayModeSmokeCoordinationFactsProvider.CreateDefault().Coverage,
            Array.Empty<MissionIntentInput>(),
            PlayModeSmokeCoordinationFactsProvider.CreateDefault().Assignments);
        var snapshot = new StubSnapshot(
            simTime: 2.0,
            alive: new Dictionary<string, bool>
            {
                [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = true,
                [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
            });
        var frame = CoordinationBridge.Build(bridge, snapshot, facts);
        var reattack = CoordinationPresentation.PreviewGroupDecision(
            frame.Groups.Single(),
            CoordinationDecision.Reattack,
            bridge,
            snapshot);

        Assert.That(reattack.IsAllowed, Is.False);
        Assert.That(reattack.TargetOrderKind, Is.Null);
        Assert.That(reattack.BlockReasonCode, Is.EqualTo(CoordinationCommandBridge.ReasonRetattackAdvisoryOnly));
        Assert.That(reattack.BlockExplanation, Does.Contain("advisory"));
        Assert.That(reattack.UnitEffects.All(u => u.EffectStatus == "ADVISORY_ONLY"), Is.True);
    }

    [Test]
    public void PreviewGroupDecision_reports_not_human_missing_member_replay_intent_constraint_and_off_grid()
    {
        // 1. Not human control
        var bridge = new DelegationBridge(42, mvpEngagement: true);
        var grp = bridge.Registry.RegisterGroup(new EntityKey(100), "g1");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.Registry.LinkGroupMember(grp.TargetId, u1.TargetId);
        var snapshot = new StubSnapshot(simTime: 5.0, alive: new Dictionary<string, bool> { ["u1"] = true });
        var frame = CoordinationBridge.Build(bridge, snapshot);
        var notHuman = CoordinationPresentation.PreviewGroupDecision(frame.Groups.Single(), CoordinationDecision.Hold, bridge, snapshot);
        Assert.That(notHuman.IsAllowed, Is.False);
        Assert.That(notHuman.BlockReasonCode, Is.EqualTo(CoordinationCommandBridge.ReasonNotHumanControl));

        // 2. Human control + off-grid
        grp.Target.Slot.SetActive(new HumanController());
        bridge.Session!.CommsGrid = new CommsGridRegistry(new[] { new ScenarioCommsGridTransition(1, "g1", "OffGrid", "deep") });
        bridge.Session.CommsGrid.Advance(1);
        var offGridFrame = CoordinationBridge.Build(bridge, snapshot);
        Assert.That(offGridFrame.Groups.Single().Details!.IsOffGrid, Is.True);
        var offGridPreview = CoordinationPresentation.PreviewGroupDecision(offGridFrame.Groups.Single(), CoordinationDecision.Hold, bridge, snapshot);
        Assert.That(offGridPreview.IsAllowed, Is.False);
        Assert.That(offGridPreview.BlockReasonCode, Is.EqualTo(C2PlayerCommandBridge.ReasonOffGrid));

        // 3. Back on grid + Hold intent constraint blocks Withdraw
        bridge.Session.CommsGrid = null;
        var holdConstrainedFacts = new TestFacts(
            Array.Empty<PackageDefinition>(),
            Array.Empty<CoverageFact>(),
            new[] { new MissionIntentInput("g1", string.Empty, MissionIntentCode.Hold, new[] { MissionIntentConstraintCode.Hold }) },
            Array.Empty<CoordinationGroupPackageAssignment>());
        var constrainedFrame = CoordinationBridge.Build(bridge, snapshot, holdConstrainedFacts);
        var withdrawPreview = CoordinationPresentation.PreviewGroupDecision(constrainedFrame.Groups.Single(), CoordinationDecision.Withdraw, bridge, snapshot);
        Assert.That(withdrawPreview.IsAllowed, Is.False);
        Assert.That(withdrawPreview.BlockReasonCode, Is.EqualTo(CoordinationCommandBridge.ReasonIntentConstraint));

        // 4. Missing/lost member
        var deadSnapshot = new StubSnapshot(simTime: 5.0, alive: new Dictionary<string, bool> { ["u1"] = false });
        var deadFrame = CoordinationBridge.Build(bridge, deadSnapshot);
        var deadPreview = CoordinationPresentation.PreviewGroupDecision(deadFrame.Groups.Single(), CoordinationDecision.Hold, bridge, deadSnapshot);
        Assert.That(deadPreview.IsAllowed, Is.False);
        Assert.That(deadPreview.BlockReasonCode, Is.EqualTo(CoordinationCommandBridge.ReasonMissingMember));
        Assert.That(deadPreview.UnitEffects.Single().EffectStatus, Is.EqualTo("BLOCKED_LOST"));

        // 5. Replay attached
        bridge.AttachReplayViewer = true;
        var replayPreview = CoordinationPresentation.PreviewGroupDecision(frame.Groups.Single(), CoordinationDecision.Hold, bridge, snapshot);
        Assert.That(replayPreview.IsAllowed, Is.False);
        Assert.That(replayPreview.BlockReasonCode, Is.EqualTo(CoordinationCommandBridge.ReasonReplayAttached));
    }

    [Test]
    public void CoordinationBlockExplainer_explains_every_CoordinationCommandBridge_reason_constant_and_off_grid()
    {
        var codes = new[]
        {
            CoordinationCommandBridge.ReasonReplayAttached,
            CoordinationCommandBridge.ReasonUnknownGroup,
            CoordinationCommandBridge.ReasonEmptyGroup,
            CoordinationCommandBridge.ReasonMissingMember,
            CoordinationCommandBridge.ReasonNotHumanControl,
            CoordinationCommandBridge.ReasonAuthorityWithheld,
            CoordinationCommandBridge.ReasonIntentConstraint,
            CoordinationCommandBridge.ReasonRetattackAdvisoryOnly,
            CoordinationCommandBridge.ReasonEnqueueFailed,
            CoordinationCommandBridge.ReasonInvalidSimTime,
            CoordinationCommandBridge.ReasonGroupDecisionPending,
            CoordinationCommandBridge.ReasonIntentScopeMismatch,
            C2PlayerCommandBridge.ReasonOffGrid,
            CoordinationBlockExplainer.ReasonTimelineInspecting,
        };

        foreach (var code in codes)
        {
            var explanation = CoordinationBlockExplainer.Explain(code);
            Assert.That(explanation, Is.Not.Null.And.Not.Empty, code);
            Assert.That(explanation, Is.Not.EqualTo(code), $"Code {code} should map to a human explanation.");
        }

        Assert.That(
            CoordinationBlockExplainer.TryExtractReasonCode("Prevented: MISSION_INTENT_CONSTRAINT", out var extracted),
            Is.True);
        Assert.That(extracted, Is.EqualTo(CoordinationCommandBridge.ReasonIntentConstraint));
    }

    [Test]
    public void CommandReviewDashboard_groups_section_surfaces_escort_sector_shared_targeting_and_intent_under_budget()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);
        var snapshot = new StubSnapshot(
            simTime: 2.0,
            alive: new Dictionary<string, bool>
            {
                [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = true,
                [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
            },
            primaryHostile: new TargetId(PlayModeSmokeOrbatSeeder.HostileUnitId),
            hasFireControlTrack: true);
        var coordination = CoordinationBridge.Build(bridge, snapshot, PlayModeSmokeCoordinationFactsProvider.CreateDefault());

        var sw = Stopwatch.StartNew();
        var sections = CommandReviewDashboard.Build(status: null, advice: null, coordination);
        sw.Stop();

        var groups = sections.Single(s => s.Id == "groups");
        var joined = string.Join("\n", groups.Lines);
        Assert.That(joined, Does.Contain("Escort:"));
        Assert.That(joined, Does.Contain("Sector:"));
        Assert.That(joined, Does.Contain("Shared target:"));
        Assert.That(joined, Does.Contain("Controller:"));
        Assert.That(joined, Does.Contain("Command effects:"));
        Assert.That(sw.Elapsed.TotalMilliseconds, Is.LessThan(100));

        var pres = CoordinationPresentation.Build(coordination, bridge, snapshot);
        Assert.That(pres.Fingerprint, Is.Not.Empty);
        var grpPres = pres.Groups.Single();
        Assert.That(grpPres.ActiveBlockExplanation, Is.Not.Empty);
        Assert.That(grpPres.Boundaries[0].ScopeLabel, Is.Not.Empty);
        var holdPrev = grpPres.GetPreview(CoordinationDecision.Hold);
        Assert.That(holdPrev.SummaryLine, Is.Not.Empty);
        Assert.That(holdPrev.UnitEffects[0].RoleLabel, Is.Not.Empty);
        Assert.That(holdPrev.UnitEffects[0].EffectSummary, Is.Not.Empty);
        Assert.That(coordination.Groups.Single().Details!.Sectors[0].ElementId, Is.Not.Empty);
    }

    [Test]
    public void Build_and_PreviewGroupDecision_handle_null_gaps_null_intent_and_bridge_null_members_fallback()
    {
        var coord = new TaskGroupCoordSnapshot(
            "g-null-safe",
            new[] { "u1", "u2" },
            "pkg-1",
            "Baltic Escort",
            "NONE",
            TaskGroupCoordKind.AdvisoryCoordination,
            false,
            false,
            false,
            "ok");
        var details = new CoordinationGroupDetails(
            "Human",
            AutonomyLevel.Manual,
            PendingReplan: false,
            IsOffGrid: false,
            Escorts: Array.Empty<CoordinationEscortAssignment>(),
            Sectors: Array.Empty<CoordinationSectorResponsibility>(),
            SharedTargets: Array.Empty<CoordinationSharedTargetAssignment>());
        var groupWithNulls = new CoordinationGroupSnapshot(
            coord,
            Intent: null!,
            Effects: Array.Empty<CoordinationEffect>(),
            Gaps: null!,
            Details: details);

        var state = CoordinationPresentation.Build(new CoordinationSnapshot(new[] { groupWithNulls }));
        var presented = state.FindGroup("g-null-safe");
        Assert.That(presented, Is.Not.Null);
        Assert.That(presented!.GapLines, Is.Empty);
        Assert.That(presented.IntentHeaderLine, Does.Contain("constraints: none"));

        var holdPreview = CoordinationPresentation.PreviewGroupDecision(
            groupWithNulls,
            CoordinationDecision.Hold,
            bridge: null);
        Assert.That(holdPreview.IsAllowed, Is.True);
        Assert.That(holdPreview.UnitEffects.Select(u => u.UnitId), Is.EqualTo(new[] { "u1", "u2" }));
        Assert.That(holdPreview.UnitEffects.All(u => u.EffectStatus == "WILL_APPLY"), Is.True);

        var groupWithNullConstraints = groupWithNulls with
        {
            Intent = MissionIntentSnapshot.Empty with { GroupId = "g-null-safe", Constraints = null! },
        };
        var stateNullConstraints = CoordinationPresentation.Build(new CoordinationSnapshot(new[] { groupWithNullConstraints }));
        Assert.That(stateNullConstraints.FindGroup("g-null-safe")!.IntentHeaderLine, Does.Contain("constraints: none"));
        var withdrawPreview = CoordinationPresentation.PreviewGroupDecision(
            groupWithNullConstraints,
            CoordinationDecision.Withdraw,
            bridge: null);
        Assert.That(withdrawPreview.IsAllowed, Is.True);
    }

    [Test]
    public void Build_and_Presentation_never_mutate_comms_grid_or_decision_log_on_fractional_or_preview_sim_time()
    {
        var sink = new NoOpOrderSink();
        DelegationBridge BuildOffGridBridge()
        {
            var bridge = new DelegationBridge(42, mvpEngagement: false);
            var grp = bridge.Registry.RegisterGroup(new EntityKey(100), "g1");
            var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
            bridge.Registry.LinkGroupMember(grp.TargetId, u1.TargetId);
            bridge.Orchestrator.CommsGrid = new CommsGridRegistry(new[]
            {
                new ScenarioCommsGridTransition(10, "u1", "OffGrid", "below comms depth"),
                new ScenarioCommsGridTransition(20, "u1", "OnGrid", "periscope depth"),
            });
            bridge.BeginExecution();
            bridge.Tick(new StubSnapshot(simTime: 10.0, alive: new Dictionary<string, bool> { ["u1"] = true }), sink);
            bridge.Orchestrator.CommsGridRejoinReconciler.RecordUnitObservation(
                "u1",
                "hostile-1",
                ProjectAegis.Sim.Sensors.ContactLifecycleState.Identified);
            return bridge;
        }

        // Probe 1A.1: Tick(20.5) -> Build(20.5) vs Build(20.5) -> Tick(20.5) must yield identical DecisionLog fingerprints.
        var bridgeA = BuildOffGridBridge();
        var bridgeB = BuildOffGridBridge();
        var snap20Half = new StubSnapshot(simTime: 20.5, alive: new Dictionary<string, bool> { ["u1"] = true });

        bridgeA.Tick(snap20Half, sink);
        var frameA = CoordinationBridge.Build(bridgeA, snap20Half);
        CoordinationPresentation.Build(frameA, bridgeA, snap20Half);

        var frameB = CoordinationBridge.Build(bridgeB, snap20Half);
        CoordinationPresentation.Build(frameB, bridgeB, snap20Half);
        bridgeB.Tick(snap20Half, sink);

        Assert.That(
            bridgeB.Orchestrator.DecisionLog.ComputeFingerprint(),
            Is.EqualTo(bridgeA.Orchestrator.DecisionLog.ComputeFingerprint()),
            "Read-only coordination projections before Tick must not alter DecisionLog fingerprint.");

        // Probe 1A.2 + Follow-up Probe 2: preview Build(20.0) / Presentation.Build(20.0) while sim is at t=10.0.
        var previewBridge = BuildOffGridBridge();
        var fpBefore = previewBridge.Orchestrator.DecisionLog.ComputeFingerprint();
        var gridHashBefore = previewBridge.Orchestrator.CommsGrid!.ComputeHash();
        var offGridBefore = previewBridge.Orchestrator.CommsGrid.IsOffGrid("u1");
        var snap20 = new StubSnapshot(simTime: 20.0, alive: new Dictionary<string, bool> { ["u1"] = true });

        var previewFrame = CoordinationBridge.Build(previewBridge, snap20);
        Assert.That(previewBridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(fpBefore));
        Assert.That(previewBridge.Orchestrator.CommsGrid.ComputeHash(), Is.EqualTo(gridHashBefore));
        Assert.That(previewBridge.Orchestrator.CommsGrid.IsOffGrid("u1"), Is.EqualTo(offGridBefore));
        Assert.That(previewBridge.Orchestrator.DecisionLog.CommsGridRejoins, Is.Empty);

        CoordinationPresentation.Build(previewFrame, previewBridge, snap20);
        Assert.That(previewBridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(fpBefore));
        Assert.That(previewBridge.Orchestrator.CommsGrid.ComputeHash(), Is.EqualTo(gridHashBefore));
        Assert.That(previewBridge.Orchestrator.CommsGrid.IsOffGrid("u1"), Is.EqualTo(offGridBefore));
        Assert.That(previewBridge.Orchestrator.DecisionLog.CommsGridRejoins, Is.Empty);
    }

    [Test]
    public void Probe_3_1_build_and_preview_see_off_grid_transition_at_tick_10_before_tick_without_mutating_state()
    {
        var sink = new NoOpOrderSink();
        var (bridge, snap9, _, _) = CreateOffGridTransitionTestBridge(
            new ScenarioCommsGridTransition(10, "g1", "OffGrid", "submerged"),
            new ScenarioCommsGridTransition(20, "g1", "OnGrid", "surfaced"));
        bridge.Tick(snap9, sink);

        var fpBefore = bridge.Orchestrator.DecisionLog.ComputeFingerprint();
        var gridHashBefore = bridge.Orchestrator.CommsGrid!.ComputeHash();
        var offBefore = bridge.Orchestrator.CommsGrid.IsOffGrid("g1");
        Assert.That(offBefore, Is.False);

        var snap10 = new StubSnapshot(simTime: 10.0, alive: new Dictionary<string, bool> { ["u1"] = true });
        var frame = CoordinationBridge.Build(bridge, snap10);
        var group = frame.Groups.Single();
        var preview = CoordinationPresentation.PreviewGroupDecision(group, CoordinationDecision.Hold, bridge, snap10);
        var presentation = CoordinationPresentation.Build(frame, bridge, snap10);

        Assert.That(group.Details!.IsOffGrid, Is.True, "Probe 3.1: IsOffGrid at t=10 before Tick(10)");
        Assert.That(preview.IsAllowed, Is.False, "Probe 3.1: PreviewHold.Allowed at t=10 before Tick(10)");
        Assert.That(preview.BlockReasonCode, Is.EqualTo(C2PlayerCommandBridge.ReasonOffGrid));
        Assert.That(presentation.FindGroup("g1")!.GetPreview(CoordinationDecision.Hold).IsAllowed, Is.False);

        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(fpBefore), "Probe 3.1: DecisionLog must remain unmutated");
        Assert.That(bridge.Orchestrator.CommsGrid.ComputeHash(), Is.EqualTo(gridHashBefore), "Probe 3.1: CommsGrid must remain unmutated");
        Assert.That(bridge.Orchestrator.CommsGrid.IsOffGrid("g1"), Is.EqualTo(offBefore), "Probe 3.1: live IsOffGrid must remain unmutated");

        var submit = CoordinationCommandBridge.Submit(bridge, snap10, "g1", CoordinationDecision.Hold);
        Assert.That(submit.Accepted, Is.False);
        Assert.That(submit.FailureReason, Is.EqualTo(C2PlayerCommandBridge.ReasonOffGrid));
    }

    [Test]
    public void Probe_3_2_build_and_preview_see_rejoin_transition_at_tick_20_before_tick_without_mutating_state()
    {
        var sink = new NoOpOrderSink();
        var (bridge, _, _, snap19) = CreateOffGridTransitionTestBridge(
            new ScenarioCommsGridTransition(10, "g1", "OffGrid", "submerged"),
            new ScenarioCommsGridTransition(20, "g1", "OnGrid", "surfaced"));
        bridge.Tick(snap19, sink);

        var fpBefore = bridge.Orchestrator.DecisionLog.ComputeFingerprint();
        var gridHashBefore = bridge.Orchestrator.CommsGrid!.ComputeHash();
        var offBefore = bridge.Orchestrator.CommsGrid.IsOffGrid("g1");
        Assert.That(offBefore, Is.True);

        var snap20 = new StubSnapshot(simTime: 20.0, alive: new Dictionary<string, bool> { ["u1"] = true });
        var frame = CoordinationBridge.Build(bridge, snap20);
        var group = frame.Groups.Single();
        var preview = CoordinationPresentation.PreviewGroupDecision(group, CoordinationDecision.Hold, bridge, snap20);
        var presentation = CoordinationPresentation.Build(frame, bridge, snap20);

        Assert.That(group.Details!.IsOffGrid, Is.False, "Probe 3.2: IsOffGrid at t=20 before Tick(20)");
        Assert.That(preview.IsAllowed, Is.True, "Probe 3.2: PreviewHold.Allowed at t=20 before Tick(20)");
        Assert.That(preview.BlockReasonCode, Is.Null);
        Assert.That(presentation.FindGroup("g1")!.GetPreview(CoordinationDecision.Hold).IsAllowed, Is.True);

        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(fpBefore), "Probe 3.2: DecisionLog must remain unmutated");
        Assert.That(bridge.Orchestrator.CommsGrid.ComputeHash(), Is.EqualTo(gridHashBefore), "Probe 3.2: CommsGrid must remain unmutated");
        Assert.That(bridge.Orchestrator.CommsGrid.IsOffGrid("g1"), Is.EqualTo(offBefore), "Probe 3.2: live IsOffGrid must remain unmutated");

        var submit = CoordinationCommandBridge.Submit(bridge, snap20, "g1", CoordinationDecision.Hold);
        Assert.That(submit.Accepted, Is.True);
        Assert.That(submit.FailureReason, Is.Null);
    }

    [Test]
    public void Probe_3_3_build_and_preview_see_off_grid_transition_at_tick_0_before_tick_without_mutating_state()
    {
        var (bridge, _, _, _) = CreateOffGridTransitionTestBridge(
            new ScenarioCommsGridTransition(0, "g1", "OffGrid", "submerged-at-start"));

        var fpBefore = bridge.Orchestrator.DecisionLog.ComputeFingerprint();
        var gridHashBefore = bridge.Orchestrator.CommsGrid!.ComputeHash();
        var offBefore = bridge.Orchestrator.CommsGrid.IsOffGrid("g1");
        Assert.That(offBefore, Is.False);

        var snap0 = new StubSnapshot(simTime: 0.0, alive: new Dictionary<string, bool> { ["u1"] = true });
        var frame = CoordinationBridge.Build(bridge, snap0);
        var group = frame.Groups.Single();
        var preview = CoordinationPresentation.PreviewGroupDecision(group, CoordinationDecision.Hold, bridge, snap0);
        var presentation = CoordinationPresentation.Build(frame, bridge, snap0);

        Assert.That(group.Details!.IsOffGrid, Is.True, "Probe 3.3: IsOffGrid at t=0 before Tick(0)");
        Assert.That(preview.IsAllowed, Is.False, "Probe 3.3: PreviewHold.Allowed at t=0 before Tick(0)");
        Assert.That(preview.BlockReasonCode, Is.EqualTo(C2PlayerCommandBridge.ReasonOffGrid));
        Assert.That(presentation.FindGroup("g1")!.GetPreview(CoordinationDecision.Hold).IsAllowed, Is.False);

        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(fpBefore), "Probe 3.3: DecisionLog must remain unmutated");
        Assert.That(bridge.Orchestrator.CommsGrid.ComputeHash(), Is.EqualTo(gridHashBefore), "Probe 3.3: CommsGrid must remain unmutated");
        Assert.That(bridge.Orchestrator.CommsGrid.IsOffGrid("g1"), Is.EqualTo(offBefore), "Probe 3.3: live IsOffGrid must remain unmutated");

        var submit = CoordinationCommandBridge.Submit(bridge, snap0, "g1", CoordinationDecision.Hold);
        Assert.That(submit.Accepted, Is.False);
        Assert.That(submit.FailureReason, Is.EqualTo(C2PlayerCommandBridge.ReasonOffGrid));
    }

    [Test]
    public void Probe_3_4_preview_at_sim_time_5_while_sim_at_15_off_grid_sees_on_grid_without_mutating_state()
    {
        var sink = new NoOpOrderSink();
        var (bridge, _, snap15, _) = CreateOffGridTransitionTestBridge(
            new ScenarioCommsGridTransition(10, "g1", "OffGrid", "submerged"),
            new ScenarioCommsGridTransition(20, "g1", "OnGrid", "surfaced"));
        bridge.Tick(snap15, sink);

        var fpBefore = bridge.Orchestrator.DecisionLog.ComputeFingerprint();
        var gridHashBefore = bridge.Orchestrator.CommsGrid!.ComputeHash();
        var offBefore = bridge.Orchestrator.CommsGrid.IsOffGrid("g1");
        Assert.That(offBefore, Is.True);

        var snap5 = new StubSnapshot(simTime: 5.0, alive: new Dictionary<string, bool> { ["u1"] = true });
        var frame = CoordinationBridge.Build(bridge, snap5);
        var group = frame.Groups.Single();
        var preview = CoordinationPresentation.PreviewGroupDecision(group, CoordinationDecision.Hold, bridge, snap5);
        var presentation = CoordinationPresentation.Build(frame, bridge, snap5);

        Assert.That(group.Details!.IsOffGrid, Is.False, "Probe 3.4: preview at t=5 before AtTick=10 must be OnGrid");
        Assert.That(preview.IsAllowed, Is.True, "Probe 3.4: PreviewHold.Allowed at t=5 must be True");
        Assert.That(preview.BlockReasonCode, Is.Null);
        Assert.That(presentation.FindGroup("g1")!.GetPreview(CoordinationDecision.Hold).IsAllowed, Is.True);

        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(fpBefore), "Probe 3.4: DecisionLog must remain unmutated");
        Assert.That(bridge.Orchestrator.CommsGrid.ComputeHash(), Is.EqualTo(gridHashBefore), "Probe 3.4: CommsGrid must remain unmutated");
        Assert.That(bridge.Orchestrator.CommsGrid.IsOffGrid("g1"), Is.EqualTo(offBefore), "Probe 3.4: live IsOffGrid must remain unmutated");
    }

    private static (DelegationBridge Bridge, StubSnapshot SnapAt9, StubSnapshot SnapAt15, StubSnapshot SnapAt19) CreateOffGridTransitionTestBridge(
        params ScenarioCommsGridTransition[] transitions)
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var grp = bridge.Registry.RegisterGroup(new EntityKey(100), "g1");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.Registry.LinkGroupMember(grp.TargetId, u1.TargetId);
        grp.Target.Slot.SetActive(new HumanController());
        bridge.Orchestrator.CommsGrid = new CommsGridRegistry(transitions);
        bridge.BeginExecution();
        var alive = new Dictionary<string, bool> { ["u1"] = true };
        return (
            bridge,
            new StubSnapshot(simTime: 9.0, alive: alive),
            new StubSnapshot(simTime: 15.0, alive: alive),
            new StubSnapshot(simTime: 19.0, alive: alive));
    }

    private sealed class NoOpOrderSink : IOrderSink
    {
        public void ApplyOrder(EntityKey entity, in Order order) { }
    }

    private sealed class StubSnapshot : ISimWorldSnapshot
    {
        private readonly IReadOnlyDictionary<string, bool> _alive;

        public StubSnapshot(
            double simTime,
            IReadOnlyDictionary<string, bool> alive,
            TargetId? primaryHostile = null,
            bool hasFireControlTrack = false,
            IReadOnlyDictionary<string, string>? preferredHostileByShooter = null)
        {
            SimTime = simTime;
            _alive = alive;
            PrimaryHostileContactId = primaryHostile;
            HasFireControlTrackOnPrimaryContact = hasFireControlTrack;
            PreferredHostileByShooter = preferredHostileByShooter;
        }

        public double SimTime { get; }
        public int ContactCount => PrimaryHostileContactId is null ? 0 : 1;
        public int ActiveEngagementCount => 0;
        public TargetId? PrimaryHostileContactId { get; }
        public bool HasFireControlTrackOnPrimaryContact { get; }
        public bool ObserverRadarEmconActive => true;
        public IReadOnlyDictionary<string, string>? PreferredHostileByShooter { get; }
        public bool IsMemberAlive(TargetId memberId) => _alive.TryGetValue(memberId.Value, out var ok) && ok;
    }

    private sealed class TestFacts : ICoordinationFacts
    {
        public TestFacts(
            IReadOnlyList<PackageDefinition> packages,
            IReadOnlyList<CoverageFact> coverage,
            IReadOnlyList<MissionIntentInput> intents,
            IReadOnlyList<CoordinationGroupPackageAssignment> assignments,
            IReadOnlyList<CoordinationEscortFact>? escorts = null,
            IReadOnlyList<CoordinationSharedTargetFact>? sharedTargets = null)
        {
            Packages = packages;
            Coverage = coverage;
            Intents = intents;
            Assignments = assignments;
            Escorts = escorts ?? Array.Empty<CoordinationEscortFact>();
            SharedTargets = sharedTargets ?? Array.Empty<CoordinationSharedTargetFact>();
        }

        public IReadOnlyList<PackageDefinition> Packages { get; }
        public IReadOnlyList<CoverageFact> Coverage { get; }
        public IReadOnlyList<MissionIntentInput> Intents { get; }
        public IReadOnlyList<CoordinationGroupPackageAssignment> Assignments { get; }
        public IReadOnlyList<CoordinationEscortFact> Escorts { get; }
        public IReadOnlyList<CoordinationSharedTargetFact> SharedTargets { get; }
    }
}
