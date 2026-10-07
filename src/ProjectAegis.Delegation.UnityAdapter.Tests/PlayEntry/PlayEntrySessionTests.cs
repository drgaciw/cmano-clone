namespace ProjectAegis.Delegation.UnityAdapter.Tests.PlayEntry;

using Controllers;
using Core;
using Orchestration;
using Targets;
using Data.Scenario;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.PlayEntry;
using NUnit.Framework;

/// <summary>
/// S123-01 DRG-243 PLAY-ENTRY, S123-04 DRG-246 MODE-01, S123-05 W2-MODE-01, S123-06 W3-MODE-01,
/// S123-11 W3-CORE-03: headless play-entry façade over the existing <see cref="DelegationBridge"/>.
/// </summary>
[TestFixture]
public sealed class PlayEntrySessionTests
{
    private string _tempDir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "aegis-s123-play-entry-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Test]
    public void ListPackages_includes_available_baltic_patrol_package_in_deterministic_order()
    {
        var entries = PlayEntrySession.ListPackages(ScenariosDir());

        var baltic = entries.Single(e => IsBalticPatrolPath(e.SourcePath));
        Assert.That(baltic.Available, Is.True);
        Assert.That(baltic.PolicyId, Is.EqualTo("baltic-patrol-catalog"));
        Assert.That(entries.Select(e => e.ScenarioId), Is.Ordered.Using((IComparer<string>)StringComparer.Ordinal));
    }

    [Test]
    public void ListPackages_returns_empty_for_missing_directory()
    {
        Assert.That(PlayEntrySession.ListPackages(Path.Combine(_tempDir, "nope")), Is.Empty);
    }

    [Test]
    public void New_session_has_no_package_and_no_bridge()
    {
        var session = new PlayEntrySession();

        Assert.That(session.State, Is.EqualTo(PlayEntryState.Empty));
        Assert.That(session.State.HasPackage, Is.False);
        Assert.That(session.Bridge, Is.Null);
    }

    [Test]
    public void TryLoad_baltic_entry_enters_planning_with_package_bound_bridge()
    {
        var session = new PlayEntrySession();
        var entry = BalticEntry();

        var result = session.TryLoad(entry);

        Assert.That(result.Succeeded, Is.True, result.Message);
        Assert.That(result.ErrorCode, Is.Null);
        Assert.That(session.State.HasPackage, Is.True);
        Assert.That(session.State.SourcePath, Is.EqualTo(entry.SourcePath));
        Assert.That(session.State.PolicyId, Is.EqualTo("baltic-patrol-catalog"));
        Assert.That(session.State.Phase, Is.EqualTo(SimulationPhase.Planning));
        Assert.That(session.State.Mode, Is.Null);
        Assert.That(session.State.Side, Is.Null);
        Assert.That(session.State.SessionGeneration, Is.EqualTo(1));
        Assert.That(session.Bridge, Is.Not.Null);
        Assert.That(session.Bridge!.Phase, Is.EqualTo(SimulationPhase.Planning));
        Assert.That(session.Bridge.Orchestrator.ScenarioPolicy, Is.Not.Null);
        Assert.That(session.Package!.Seed, Is.EqualTo(42UL));
        Assert.That(session.Document, Is.Not.Null);
    }

    [Test]
    public void Failed_resolve_from_empty_session_leaves_session_empty()
    {
        var session = new PlayEntrySession();

        var result = session.TryLoadFromPath(Path.Combine(_tempDir, "missing.scenario.json"));

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(PlayEntryErrorCodes.FileUnreadable));
        Assert.That(result.Message, Does.Contain("missing.scenario.json"));
        Assert.That(session.State, Is.EqualTo(PlayEntryState.Empty));
        Assert.That(session.Bridge, Is.Null);
    }

    [Test]
    public void Failed_resolve_missing_file_preserves_prior_planning_session()
    {
        var session = LoadedBalticWithMixedFriendly();
        var priorState = session.State;
        var priorBridge = session.Bridge;
        var priorPackage = session.Package;

        var result = session.TryLoadFromPath(Path.Combine(_tempDir, "missing.scenario.json"));

        AssertNonMutatingFailure(session, result, PlayEntryErrorCodes.FileUnreadable, priorState, priorBridge, priorPackage);
        Assert.That(session.State.SourcePath, Is.EqualTo(BalticEntry().SourcePath));
    }

    [Test]
    public void Failed_resolve_malformed_json_preserves_prior_session()
    {
        var session = LoadedBalticWithMixedFriendly();
        var priorState = session.State;
        var priorBridge = session.Bridge;
        var priorPackage = session.Package;
        var path = Path.Combine(_tempDir, "broken.scenario.json");
        File.WriteAllText(path, "{ \"metadata\": { \"seed\": ");

        var result = session.TryLoadFromPath(path);

        AssertNonMutatingFailure(session, result, PlayEntryErrorCodes.SchemaError, priorState, priorBridge, priorPackage);
    }

    [Test]
    public void Failed_resolve_unknown_policy_preserves_prior_session()
    {
        var session = LoadedBalticWithMixedFriendly();
        var priorState = session.State;
        var priorBridge = session.Bridge;
        var priorPackage = session.Package;
        var path = Path.Combine(_tempDir, "ghost.scenario.json");
        File.WriteAllText(path, "{ \"metadata\": { \"policyId\": \"no-such-policy-s123\", \"seed\": 7 } }");

        var result = session.TryLoadFromPath(path);

        AssertNonMutatingFailure(session, result, PlayEntryErrorCodes.PolicyUnresolved, priorState, priorBridge, priorPackage);
        Assert.That(result.Message, Does.Contain("no-such-policy-s123"));
    }

    [Test]
    public void Failed_resolve_seed_outside_bridge_range_preserves_prior_session()
    {
        var session = LoadedBalticWithMixedFriendly();
        var priorState = session.State;
        var priorBridge = session.Bridge;
        var priorPackage = session.Package;
        var path = Path.Combine(_tempDir, "big-seed.scenario.json");
        File.WriteAllText(path, "{ \"metadata\": { \"policyId\": \"baltic-patrol\", \"seed\": 4294967296 } }");

        var result = session.TryLoadFromPath(path);

        AssertNonMutatingFailure(session, result, PlayEntryErrorCodes.SeedUnsupported, priorState, priorBridge, priorPackage);
    }

    [Test]
    public void Failed_resolve_unavailable_library_entry_preserves_prior_session()
    {
        var session = LoadedBalticWithMixedFriendly();
        var priorState = session.State;
        var priorBridge = session.Bridge;
        var priorPackage = session.Package;
        var unavailable = ScenarioLibraryProjection.ProjectUnavailable(
            "broken-ref",
            Path.Combine(_tempDir, "broken-ref.scenario.json"),
            ScenarioLibraryReasons.BrokenRef);

        var result = session.TryLoad(unavailable);

        AssertNonMutatingFailure(session, result, PlayEntryErrorCodes.PackageUnavailable, priorState, priorBridge, priorPackage);
        Assert.That(result.Message, Does.Contain(ScenarioLibraryReasons.BrokenRef));
    }

    [Test]
    public void Failed_resolve_during_execution_keeps_executing_session()
    {
        var session = LoadedBalticWithMixedFriendly();
        var (friendly, opposing) = RegisterBalticForces(session.Bridge!);
        Assert.That(session.TryBeginExecution(new[] { friendly }, new[] { opposing }).Succeeded, Is.True);
        var priorState = session.State;
        var priorBridge = session.Bridge;
        var priorPackage = session.Package;

        var result = session.TryLoadFromPath(Path.Combine(_tempDir, "missing.scenario.json"));

        AssertNonMutatingFailure(session, result, PlayEntryErrorCodes.FileUnreadable, priorState, priorBridge, priorPackage);
        Assert.That(session.Bridge!.Phase, Is.EqualTo(SimulationPhase.Executing));
    }

    [Test]
    public void Successful_reload_replaces_bridge_and_clears_mode_and_side()
    {
        var session = LoadedBalticWithMixedFriendly();
        var priorBridge = session.Bridge;

        var result = session.TryLoad(BalticEntry());

        Assert.That(result.Succeeded, Is.True);
        Assert.That(session.Bridge, Is.Not.SameAs(priorBridge));
        Assert.That(session.State.Phase, Is.EqualTo(SimulationPhase.Planning));
        Assert.That(session.State.Mode, Is.Null);
        Assert.That(session.State.Side, Is.Null);
        Assert.That(session.State.SessionGeneration, Is.EqualTo(2));
    }

    [Test]
    public void OfferedSides_maps_each_simulation_mode()
    {
        Assert.That(PlayEntrySession.OfferedSides(SimulationModeKind.Human), Is.EqualTo(new[] { PlaySide.Friendly }));
        Assert.That(
            PlayEntrySession.OfferedSides(SimulationModeKind.Mixed),
            Is.EqualTo(new[] { PlaySide.Friendly, PlaySide.Opposing }));
        Assert.That(
            PlayEntrySession.OfferedSides(SimulationModeKind.AgentVsAgent),
            Is.EqualTo(new[] { PlaySide.Friendly, PlaySide.Opposing }));
    }

    [Test]
    public void Mode_and_side_selection_require_a_loaded_package()
    {
        var session = new PlayEntrySession();

        var mode = session.TrySelectMode(SimulationModeKind.Mixed);
        var side = session.TrySelectSide(PlaySide.Friendly);

        Assert.That(mode.ErrorCode, Is.EqualTo(PlayEntryErrorCodes.NoPackage));
        Assert.That(side.ErrorCode, Is.EqualTo(PlayEntryErrorCodes.NoPackage));
        Assert.That(session.State, Is.EqualTo(PlayEntryState.Empty));
    }

    [TestCase(SimulationModeKind.Human)]
    [TestCase(SimulationModeKind.Mixed)]
    [TestCase(SimulationModeKind.AgentVsAgent)]
    public void Selecting_each_mode_stages_it_without_touching_the_bridge(SimulationModeKind mode)
    {
        var session = LoadedBaltic();

        var result = session.TrySelectMode(mode);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(session.State.Mode, Is.EqualTo(mode));
        Assert.That(session.Bridge!.Phase, Is.EqualTo(SimulationPhase.Planning),
            "AgentVsAgent configure begins execution, so selection must stay staged until Begin.");
        Assert.That(session.Bridge.Orchestrator.DecisionLog.ModeChanges, Is.Empty);
    }

    [Test]
    public void Side_pick_is_rejected_before_mode_selection()
    {
        var session = LoadedBaltic();

        var result = session.TrySelectSide(PlaySide.Friendly);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(PlayEntryErrorCodes.ModeRequired));
        Assert.That(session.State.Side, Is.Null);
    }

    [Test]
    public void Side_not_offered_by_mode_is_rejected()
    {
        var session = LoadedBaltic();
        session.TrySelectMode(SimulationModeKind.Human);

        var result = session.TrySelectSide(PlaySide.Opposing);

        Assert.That(result.ErrorCode, Is.EqualTo(PlayEntryErrorCodes.SideNotOffered));
        Assert.That(session.State.Side, Is.Null);
    }

    [Test]
    public void Changing_mode_clears_side_the_new_mode_does_not_offer()
    {
        var session = LoadedBaltic();
        session.TrySelectMode(SimulationModeKind.Mixed);
        session.TrySelectSide(PlaySide.Opposing);

        session.TrySelectMode(SimulationModeKind.Human);

        Assert.That(session.State.Mode, Is.EqualTo(SimulationModeKind.Human));
        Assert.That(session.State.Side, Is.Null);
    }

    [Test]
    public void Changing_mode_keeps_side_the_new_mode_still_offers()
    {
        var session = LoadedBaltic();
        session.TrySelectMode(SimulationModeKind.Mixed);
        session.TrySelectSide(PlaySide.Friendly);

        session.TrySelectMode(SimulationModeKind.Human);

        Assert.That(session.State.Side, Is.EqualTo(PlaySide.Friendly));
    }

    [Test]
    public void Begin_gate_blocks_without_package()
    {
        var gate = new PlayEntrySession().EvaluateBeginGate();

        Assert.That(gate.CanBegin, Is.False);
        Assert.That(gate.BlockedReasons, Is.EqualTo(new[] { PlayEntryErrorCodes.NoPackage }));
    }

    [Test]
    public void Begin_is_rejected_when_mode_and_side_are_both_missing()
    {
        var session = LoadedBaltic();
        var (friendly, opposing) = RegisterBalticForces(session.Bridge!);

        var gate = session.EvaluateBeginGate();
        var result = session.TryBeginExecution(new[] { friendly }, new[] { opposing });

        Assert.That(gate.CanBegin, Is.False);
        Assert.That(gate.BlockedReasons, Is.EqualTo(new[] { PlayEntryErrorCodes.ModeRequired, PlayEntryErrorCodes.SideRequired }));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(PlayEntryErrorCodes.ModeRequired));
        AssertStillPlanningAndUnconfigured(session, friendly, opposing);
    }

    [Test]
    public void Begin_is_rejected_when_side_is_missing_after_mode_selection()
    {
        var session = LoadedBaltic();
        session.TrySelectMode(SimulationModeKind.Mixed);
        var (friendly, opposing) = RegisterBalticForces(session.Bridge!);

        var gate = session.EvaluateBeginGate();
        var result = session.TryBeginExecution(new[] { friendly }, new[] { opposing });

        Assert.That(gate.BlockedReasons, Is.EqualTo(new[] { PlayEntryErrorCodes.SideRequired }));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(PlayEntryErrorCodes.SideRequired));
        AssertStillPlanningAndUnconfigured(session, friendly, opposing);
    }

    [Test]
    public void Begin_is_rejected_for_agent_vs_agent_without_side()
    {
        var session = LoadedBaltic();
        session.TrySelectMode(SimulationModeKind.AgentVsAgent);
        var (friendly, opposing) = RegisterBalticForces(session.Bridge!);

        var result = session.TryBeginExecution(new[] { friendly }, new[] { opposing });

        Assert.That(result.ErrorCode, Is.EqualTo(PlayEntryErrorCodes.SideRequired));
        AssertStillPlanningAndUnconfigured(session, friendly, opposing);
    }

    [Test]
    public void Begin_with_mode_and_side_transitions_to_executing_once()
    {
        var session = LoadedBalticWithMixedFriendly();
        var (friendly, opposing) = RegisterBalticForces(session.Bridge!);

        var result = session.TryBeginExecution(new[] { friendly }, new[] { opposing });

        Assert.That(result.Succeeded, Is.True, result.Message);
        Assert.That(session.State.Phase, Is.EqualTo(SimulationPhase.Executing));
        Assert.That(session.Bridge!.Orchestrator.DecisionLog.ModeChanges, Has.Count.EqualTo(1));
        Assert.That(session.EvaluateBeginGate().BlockedReasons, Is.EqualTo(new[] { PlayEntryErrorCodes.NotPlanning }));
    }

    [Test]
    public void Selection_is_locked_after_begin()
    {
        var session = LoadedBalticWithMixedFriendly();
        var (friendly, opposing) = RegisterBalticForces(session.Bridge!);
        session.TryBeginExecution(new[] { friendly }, new[] { opposing });

        var mode = session.TrySelectMode(SimulationModeKind.AgentVsAgent);
        var side = session.TrySelectSide(PlaySide.Opposing);
        var again = session.TryBeginExecution(new[] { friendly }, new[] { opposing });

        Assert.That(mode.ErrorCode, Is.EqualTo(PlayEntryErrorCodes.NotPlanning));
        Assert.That(side.ErrorCode, Is.EqualTo(PlayEntryErrorCodes.NotPlanning));
        Assert.That(again.ErrorCode, Is.EqualTo(PlayEntryErrorCodes.NotPlanning));
        Assert.That(session.State.Mode, Is.EqualTo(SimulationModeKind.Mixed));
        Assert.That(session.State.Side, Is.EqualTo(PlaySide.Friendly));
        Assert.That(session.Bridge!.Orchestrator.DecisionLog.ModeChanges, Has.Count.EqualTo(1));
    }

    [Test]
    public void Begin_human_mode_applies_human_friendly_agent_opposing_via_facade()
    {
        var (friendly, opposing) = BeginWith(SimulationModeKind.Human, PlaySide.Friendly);

        Assert.That(friendly.Slot.Active, Is.InstanceOf<HumanController>());
        Assert.That(opposing.Slot.Active, Is.InstanceOf<AgentController>());
    }

    [Test]
    public void Begin_mixed_friendly_applies_human_on_friendly_side_via_facade()
    {
        var (friendly, opposing) = BeginWith(SimulationModeKind.Mixed, PlaySide.Friendly);

        Assert.That(friendly.Slot.Active, Is.InstanceOf<HumanController>());
        Assert.That(opposing.Slot.Active, Is.InstanceOf<AgentController>());
    }

    [Test]
    public void Begin_mixed_opposing_applies_human_on_opposing_side_via_facade()
    {
        var (friendly, opposing) = BeginWith(SimulationModeKind.Mixed, PlaySide.Opposing);

        Assert.That(friendly.Slot.Active, Is.InstanceOf<AgentController>());
        Assert.That(opposing.Slot.Active, Is.InstanceOf<HumanController>());
    }

    [Test]
    public void Begin_agent_vs_agent_applies_agents_on_both_sides_via_facade()
    {
        var (friendly, opposing) = BeginWith(SimulationModeKind.AgentVsAgent, PlaySide.Opposing);

        Assert.That(friendly.Slot.Active, Is.InstanceOf<AgentController>());
        Assert.That(opposing.Slot.Active, Is.InstanceOf<AgentController>());
    }

    [Test]
    public void Reset_returns_executing_session_to_planning_with_fresh_bridge()
    {
        var session = LoadedBalticWithMixedFriendly();
        var (friendly, opposing) = RegisterBalticForces(session.Bridge!);
        session.TryBeginExecution(new[] { friendly }, new[] { opposing });
        var executingBridge = session.Bridge;
        var package = session.Package;

        var result = session.TryResetToPlanning();

        Assert.That(result.Succeeded, Is.True, result.Message);
        Assert.That(session.Bridge, Is.Not.SameAs(executingBridge));
        Assert.That(session.Bridge!.Phase, Is.EqualTo(SimulationPhase.Planning));
        Assert.That(session.Bridge.Orchestrator.DecisionLog.ModeChanges, Is.Empty);
        Assert.That(session.Bridge.Registry.CollectMemberIds(), Is.Empty);
        Assert.That(session.Package, Is.SameAs(package));
        Assert.That(session.State.Phase, Is.EqualTo(SimulationPhase.Planning));
        Assert.That(session.State.Mode, Is.Null);
        Assert.That(session.State.Side, Is.Null);
        Assert.That(session.State.SessionGeneration, Is.EqualTo(2));
        Assert.That(executingBridge!.Phase, Is.EqualTo(SimulationPhase.Executing));
    }

    [Test]
    public void Reset_without_package_is_rejected()
    {
        var session = new PlayEntrySession();

        var result = session.TryResetToPlanning();

        Assert.That(result.ErrorCode, Is.EqualTo(PlayEntryErrorCodes.NoPackage));
        Assert.That(session.State, Is.EqualTo(PlayEntryState.Empty));
    }

    internal static string ScenariosDir() =>
        ScenarioDataPaths.TryResolveScenariosDirectory()
        ?? throw new InvalidOperationException("data/scenarios not found from test base directory.");

    internal static bool IsBalticPatrolPath(string path) =>
        path.Replace('\\', '/').EndsWith("examples/baltic-patrol.scenario.json", StringComparison.Ordinal);

    internal static ScenarioLibraryEntry BalticEntry() =>
        PlayEntrySession.ListPackages(ScenariosDir()).Single(e => IsBalticPatrolPath(e.SourcePath));

    internal static (ICommandableTarget Friendly, ICommandableTarget Opposing) RegisterBalticForces(DelegationBridge bridge)
    {
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), PlayModeSmokeOrbatSeeder.FriendlyUnitId);
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), PlayModeSmokeOrbatSeeder.HostileUnitId);
        return (friendly.Target, opposing.Target);
    }

    private static (ICommandableTarget Friendly, ICommandableTarget Opposing) BeginWith(SimulationModeKind mode, PlaySide side)
    {
        var session = LoadedBaltic();
        Assert.That(session.TrySelectMode(mode).Succeeded, Is.True);
        Assert.That(session.TrySelectSide(side).Succeeded, Is.True);
        var (friendly, opposing) = RegisterBalticForces(session.Bridge!);

        var result = session.TryBeginExecution(new[] { friendly }, new[] { opposing });

        Assert.That(result.Succeeded, Is.True, result.Message);
        Assert.That(session.Bridge!.Phase, Is.EqualTo(SimulationPhase.Executing));
        Assert.That(session.Bridge.Orchestrator.DecisionLog.ModeChanges, Has.Count.EqualTo(1));
        return (friendly, opposing);
    }

    private static PlayEntrySession LoadedBaltic()
    {
        var session = new PlayEntrySession();
        var result = session.TryLoad(BalticEntry());
        Assert.That(result.Succeeded, Is.True, result.Message);
        return session;
    }

    private static PlayEntrySession LoadedBalticWithMixedFriendly()
    {
        var session = LoadedBaltic();
        Assert.That(session.TrySelectMode(SimulationModeKind.Mixed).Succeeded, Is.True);
        Assert.That(session.TrySelectSide(PlaySide.Friendly).Succeeded, Is.True);
        return session;
    }

    private static void AssertNonMutatingFailure(
        PlayEntrySession session,
        PlayEntryResult result,
        string expectedCode,
        PlayEntryState priorState,
        DelegationBridge? priorBridge,
        ScenarioPackage? priorPackage)
    {
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expectedCode));
        Assert.That(result.Message, Is.Not.Empty);
        Assert.That(session.State, Is.EqualTo(priorState));
        Assert.That(session.Bridge, Is.SameAs(priorBridge));
        Assert.That(session.Package, Is.SameAs(priorPackage));
    }

    private static void AssertStillPlanningAndUnconfigured(
        PlayEntrySession session,
        ICommandableTarget friendly,
        ICommandableTarget opposing)
    {
        Assert.That(session.State.Phase, Is.EqualTo(SimulationPhase.Planning));
        Assert.That(session.Bridge!.Phase, Is.EqualTo(SimulationPhase.Planning));
        Assert.That(session.Bridge.Orchestrator.DecisionLog.ModeChanges, Is.Empty);
        Assert.That(friendly.Slot.Active, Is.Null);
        Assert.That(opposing.Slot.Active, Is.Null);
    }
}
