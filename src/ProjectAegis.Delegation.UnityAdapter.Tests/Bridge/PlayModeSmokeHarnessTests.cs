namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

/// <summary>
/// Headless equivalent of <c>SimplePlayModeSimHost</c> (multi-tick snapshot + sink loop).
/// </summary>
[TestFixture]
public sealed class PlayModeSmokeHarnessTests
{
    [Test]
    public void Multi_tick_loop_applies_orders_like_play_mode_host()
    {
        var bridge = new DelegationBridge(42);
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "friendly-1");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "opposing-1");

        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            friendly: [friendly.Target],
            opposing: [opposing.Target],
            defaultTraits: ProjectAegis.Delegation.Traits.PersonalityCatalog.All[0].Traits);

        bridge.BeginExecution();

        var harness = new PlayModeHarness(contactCount: 2, hasFireControlTrack: true);
        for (var frame = 0; frame < 30; frame++)
        {
            harness.AdvanceTime(1.0 / 60.0);
            bridge.Tick(harness, harness);
        }

        Assert.That(harness.AppliedOrders, Is.Not.Empty);
        Assert.That(harness.SimTime, Is.EqualTo(30.0 / 60.0).Within(1e-6));
    }

    [Test]
    public void Engage_scenario_multi_tick_writes_stable_engagement_log()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        Assert.That(bridge.Session, Is.Not.Null);

        var unit = new UnitTarget(new TargetId("u1"));
        var agent = bridge.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        bridge.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        bridge.Orchestrator.Register(unit);
        bridge.BeginExecution();

        var harness = new PlayModeHarness(contactCount: 2, hasFireControlTrack: true);
        for (var frame = 0; frame < 5; frame++)
        {
            harness.AdvanceTime(1.0);
            bridge.Tick(harness, harness);
        }

        Assert.That(bridge.Orchestrator.DecisionLog.Engagements, Is.Not.Empty);
        Assert.That(
            bridge.Orchestrator.DecisionLog.Engagements.Any(e =>
                e.Launched && e.AbortReasonCode == EngagementAbortReasonCodes.Launched),
            Is.True);
        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Does.Contain("Engagement|"));
    }

    private sealed class EngageOnlyPolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits)
        {
            _ = perceived;
            _ = traits;
            return [new ScoredIntent(OrderKind.Engage, 1.0, RiskLevel.High)];
        }
    }

    [Test]
    public void Baltic_patrol_sensor_c2_snapshot_matches_harness_run()
    {
        var result = BalticReplayHarness.Run(7, "baltic-patrol", ticks: 2, mvpEngagement: false);
        Assert.That(result.SensorC2.Contacts, Is.Not.Empty);
        Assert.That(result.SensorC2.PrimaryTargetId, Is.Not.Null);
    }

    [Test]
    public void Baltic_patrol_scoring_csv_row_is_deterministic()
    {
        var a = BalticReplayHarness.Run(7, "baltic-patrol", ticks: 6, mvpEngagement: true);
        var b = BalticReplayHarness.Run(7, "baltic-patrol", ticks: 6, mvpEngagement: true);
        Assert.That(a.ScoringCsvRow, Is.EqualTo(b.ScoringCsvRow));
        Assert.That(a.ScoringCsvRow, Does.StartWith("baltic-patrol,7,BLUE,"));
    }

    [Test]
    public void Baltic_patrol_comms_harness_matches_manual_qa_preconditions()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol-comms", ticks: 6, mvpEngagement: true);
        Assert.That(result.Messages.Any(m => m.Text.Contains("Degraded", StringComparison.Ordinal)), Is.True);
        Assert.That(result.Messages.Any(m => m.Text.Contains("Denied", StringComparison.Ordinal)), Is.True);

        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet("baltic-patrol-comms");
        Assert.That(profile, Is.Not.Null);
        var fuelNominal = FuelStateProjection.FormatUnitFuelLine("u1", 10, profile!.Logistics);
        Assert.That(fuelNominal, Does.Contain("NOMINAL").And.Contains("kg"));
        var fuelAtJoker = FuelStateProjection.FormatUnitFuelLine("u1", 100, profile.Logistics);
        Assert.That(fuelAtJoker, Does.Contain("JOKER"));
    }

    [Test]
    public void Baltic_classify_map_symbols_include_hostile_for_selection_path()
    {
        var result = BalticReplayHarness.Run(7, "baltic-patrol-classify", ticks: 10, mvpEngagement: false);
        Assert.That(result.SensorC2.Contacts, Is.Not.Empty);

        var oob = new[] { new OobTreeEntry("u1", true) };
        var symbols = MapPictureProjection.Project(oob, result.SensorC2.Contacts, layoutSeed: 7);
        Assert.That(symbols.Any(s => s.Affiliation == "Hostile"), Is.True);
        Assert.That(C2SelectionResolver.ResolveDefaultFriendlyUnit(oob), Is.EqualTo("u1"));
    }

    [Test]
    public void Baltic_classify_selection_flow_syncs_map_oob_and_contact_summary()
    {
        var result = BalticReplayHarness.Run(7, "baltic-patrol-classify", ticks: 10, mvpEngagement: false);
        var oob = new[] { new OobTreeEntry("u1", true) };
        var defaultUnit = C2SelectionResolver.ResolveDefaultFriendlyUnit(oob);
        var symbols = MapPictureProjection.Project(oob, result.SensorC2.Contacts, layoutSeed: 7);

        var mapDefault = MapPanelBinder.Bind(symbols, "baltic-patrol-classify", defaultUnit, null);
        var oobDefault = OobTreePanelBinder.Bind(oob, defaultUnit);
        Assert.That(mapDefault.Symbols.Single(s => s.SymbolId == defaultUnit).IsSelected, Is.True);
        Assert.That(oobDefault.UnitRows.Single(r => r.UnitId == defaultUnit).IsSelected, Is.True);

        var hostile = symbols.First(s => s.Affiliation == "Hostile");
        Assert.That(
            C2SelectionResolver.TryResolveHostileContactFromSymbol(hostile.SymbolId, symbols, out var contactId),
            Is.True);
        var summary = ContactSummaryProjection.Project(contactId, result.SensorC2.Contacts);
        Assert.That(summary, Is.Not.Null);
        Assert.That(summary!.DisplayLine, Does.Contain("CONTACT"));

        var mapContact = MapPanelBinder.Bind(symbols, "baltic-patrol-classify", null, contactId);
        Assert.That(mapContact.Symbols.Single(s => s.SymbolId == contactId).IsSelected, Is.True);

        var drawerContacts = SensorC2PanelBinder.Bind(result.SensorC2);
        Assert.That(drawerContacts.ContactRows.Any(r => r.ContactId == contactId), Is.True);
    }

    [Test]
    public void Baltic_doctrine_mission_roe_harness_matches_doctrine_batch_preconditions()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol-mission-roe", ticks: 6, mvpEngagement: false);
        Assert.That(result.SensorC2.Contacts, Is.Not.Empty);

        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var policy = ScenarioPolicyRepository.TryGet("baltic-patrol-mission-roe");
        Assert.That(policy, Is.Not.Null);
        Assert.That(policy!.MissionRoe, Is.Not.Null);
        Assert.That(policy.MissionRoe!.Value.Roe, Is.EqualTo(RoeLevel.WeaponsTight));

        var unitId = new TargetId("u1");
        var entry = DoctrineInheritanceProjection.ProjectUnit(unitId, policy, isFriendly: true);
        Assert.That(entry, Is.Not.Null);
        var panel = DoctrineInheritancePanelBinder.Bind(entry!);
        Assert.That(panel.RoeLine, Does.Contain("WeaponsTight"));
        Assert.That(panel.SourceLine, Does.Contain("Mission"));

        var oob = new[] { new OobTreeEntry("u1", true) };
        var symbols = MapPictureProjection.Project(oob, result.SensorC2.Contacts, layoutSeed: 42);
        var map = MapPanelBinder.Bind(symbols, "baltic-patrol-mission-roe", "u1", null);
        Assert.That(map.Symbols.Single(s => s.SymbolId == "u1").IsSelected, Is.True);
    }

    [Test]
    public void Doctrine_override_round_trip_updates_policy_log_and_projection_bind()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "baltic-patrol-mission-roe");
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var agent = bridge.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous);
        bridge.Orchestrator.AssignAgentToTarget(agent, unit.Target, EffectivePolicy.DefaultFree);
        bridge.Orchestrator.Register(unit.Target);
        bridge.BeginExecution();

        var unitId = new TargetId("u1");
        var policy = bridge.Orchestrator.ScenarioPolicy;
        Assert.That(policy, Is.Not.Null);

        var entry = DoctrineInheritanceProjection.ProjectUnit(unitId, policy, isFriendly: true);
        var panel = DoctrineInheritancePanelBinder.Bind(entry);
        Assert.That(panel.RoeLine, Does.Contain("WeaponsTight"));
        Assert.That(panel.SalvoLine, Does.Contain("SALVO:"));
        Assert.That(panel.EmconLine, Does.StartWith("EMCON:"));
        Assert.That(panel.SourceLine, Does.Contain("Mission"));
        Assert.That(panel.OverrideLine, Does.Contain("OVERRIDE:"));
        Assert.That(panel.CanOverride, Is.False);
        Assert.That(panel.RoeOptions, Has.Count.EqualTo(3));

        Assert.That(
            DoctrineOverrideCommand.TryApply(bridge.Orchestrator, unitId, "HoldFire", simTime: 1.0),
            Is.True);

        var unitKey = ProjectAegis.Delegation.Roe.OrderActionMapper.TargetIdToUlong(unitId);
        Assert.That(
            bridge.Orchestrator.ResolveEffectivePolicyForUnit(unitKey).Roe,
            Is.EqualTo(RoeLevel.HoldFire));
        Assert.That(
            bridge.Orchestrator.DecisionLog.PolicyUpdates.Any(u =>
                u.Field == "roe" && u.NewValue == nameof(RoeLevel.HoldFire)),
            Is.True);

        Assert.That(
            DoctrineOverrideCommand.TryApply(bridge.Orchestrator, unitId, "HoldFire", simTime: 2.0),
            Is.False);
    }

    [Test]
    public void Doctrine_panel_uxml_assets_define_host_element_names()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var uxmlPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "DoctrineInheritance",
            "DoctrineInheritancePanel.uxml");
        var ussPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "DoctrineInheritance",
            "DoctrineInheritancePanel.uss");

        Assert.That(File.Exists(uxmlPath), Is.True);
        Assert.That(File.Exists(ussPath), Is.True);

        var uxml = File.ReadAllText(uxmlPath);
        var requiredNames = new[]
        {
            "doctrine-root",
            "unit-id-label",
            "roe-label",
            "salvo-label",
            "emcon-label",
            "source-label",
            "override-label",
            "roe-dropdown",
            "apply-override-button",
        };

        foreach (var name in requiredNames)
        {
            Assert.That(uxml, Does.Contain($"name=\"{name}\""), $"Missing UXML element: {name}");
        }

        Assert.That(
            uxml,
            Does.Contain("unit → embarked → mission → group → side → scenario"),
            "Inheritance order hint should be visible in panel layout");
    }

    [Test]
    public void Platform_catalog_viewer_baltic_fixture_sorted_rows_and_filter()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();
        var rows = CatalogPlatformBrowseProjection.FromReader(reader);

        Assert.That(rows, Is.Not.Empty);
        Assert.That(
            rows.Select(r => r.PlatformId),
            Is.EqualTo(rows.OrderBy(r => r.PlatformId, StringComparer.Ordinal).Select(r => r.PlatformId)));

        var browseRows = BalticBrowseRows();
        Assert.That(browseRows.Count, Is.GreaterThanOrEqualTo(3));
        var filtered = PlatformCatalogFilterProjection.Apply(browseRows, "hostile");
        Assert.That(filtered.Count, Is.LessThan(browseRows.Count));
        Assert.That(
            filtered.Select(r => r.PlatformId).ToArray(),
            Is.EqualTo(new[] { "hostile-1", "hostile-far" }));

        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var hostPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "PlatformCatalogViewerHost.cs");
        Assert.That(File.Exists(hostPath), Is.True);

        var source = File.ReadAllText(hostPath);
        Assert.That(source, Does.Not.Contain("CatalogWriteGate"), "Viewer host must not reference CatalogWriteGate");
    }

    [Test]
    public void Delegation_smoke_scene_builder_includes_platform_catalog_viewer()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var builderPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Editor",
            "DelegationSmokeSceneBuilder.cs");
        var builder = File.ReadAllText(builderPath);

        Assert.That(builder, Does.Contain("PlatformCatalogViewerHost"));
        Assert.That(builder, Does.Contain("\"PlatformCatalog\""));
        Assert.That(builder, Does.Contain("Assets/UI/PlatformCatalog/PlatformCatalogPanel.uxml"));
        Assert.That(builder, Does.Contain("Assets/UI/PlatformCatalog/PlatformCatalogPanel.uss"));
    }

    [Test]
    public void Doctrine_smoke_scene_builder_registers_doctrine_panel_host()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var builderPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Editor",
            "DelegationSmokeSceneBuilder.cs");
        var builder = File.ReadAllText(builderPath);

        Assert.That(builder, Does.Contain("CreatePanelHost<DoctrineInheritancePanelHost>"));
        Assert.That(builder, Does.Contain("\"DoctrineInheritance\""));
        Assert.That(builder, Does.Contain("Assets/UI/DoctrineInheritance/DoctrineInheritancePanel.uxml"));
        Assert.That(builder, Does.Contain("Assets/UI/DoctrineInheritance/DoctrineInheritancePanel.uss"));
    }

    [Test]
    public void Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var scenePath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scenes",
            "DelegationSmoke.unity");
        var builderPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Editor",
            "DelegationSmokeSceneBuilder.cs");

        Assert.That(File.Exists(scenePath), Is.True, "DelegationSmoke.unity must exist for CI PlayMode path");
        Assert.That(File.Exists(builderPath), Is.True);

        var sceneYaml = File.ReadAllText(scenePath);
        Assert.That(sceneYaml, Does.Contain("useGlobeMap: 0"), "DelegationSmoke must keep globe map disabled for headless CI");

        var builder = File.ReadAllText(builderPath);
        Assert.That(builder, Does.Contain("CreatePanelHost<MapPlaceholderPanelHost>"));
        Assert.That(builder, Does.Not.Contain("CreatePanelHost<CesiumGlobeHost>"));
        Assert.That(builder, Does.Not.Contain("useGlobeMap\", true"));
        Assert.That(builder, Does.Not.Contain("useGlobeMap\", True"));
    }

    [Test]
    public void Engage_without_fire_control_track_aborts_via_bridge_snapshot()
    {
        var bridge = new DelegationBridge(3, mvpEngagement: true);
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = bridge.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        bridge.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        bridge.Orchestrator.Register(unit);
        bridge.BeginExecution();

        var harness = new PlayModeHarness(contactCount: 1, hasFireControlTrack: false);
        bridge.Tick(harness, harness);

        Assert.That(bridge.Orchestrator.DecisionLog.Engagements, Has.Count.EqualTo(1));
        Assert.That(
            bridge.Orchestrator.DecisionLog.Engagements[0].AbortReasonCode,
            Is.EqualTo(AbortReasonCatalog.Engage.NO_FIRE_CONTROL_TRACK));
    }

    private sealed class PlayModeHarness : ISimWorldSnapshot, IOrderSink
    {
        private readonly int _contactCount;
        private readonly bool _hasFireControlTrack;
        private double _simTime;
        private readonly List<(EntityKey Entity, Order Order)> _applied = new();

        public PlayModeHarness(int contactCount, bool hasFireControlTrack)
        {
            _contactCount = contactCount;
            _hasFireControlTrack = hasFireControlTrack;
        }

        public double SimTime => _simTime;

        public int ContactCount => _contactCount;

        public int ActiveEngagementCount => 0;

        public TargetId? PrimaryHostileContactId =>
            _contactCount > 0 ? new TargetId("hostile-1") : null;

        public bool HasFireControlTrackOnPrimaryContact =>
            _contactCount > 0 && _hasFireControlTrack;

        public bool ObserverRadarEmconActive => true;

        public IReadOnlyList<(EntityKey Entity, Order Order)> AppliedOrders => _applied;

        public void AdvanceTime(double delta) => _simTime += delta;

        public bool IsMemberAlive(TargetId memberId) => true;

        public void ApplyOrder(EntityKey entity, in Order order) =>
            _applied.Add((entity, order));
    }

    private static IReadOnlyList<CatalogPlatformBrowseRow> BalticBrowseRows()
    {
        var platforms = CatalogValidationDefaults.BalticPlatforms();
        var bindings = platforms
            .Select(p => new CatalogSensorBinding(p.PlatformId, "radar-1", 1.0, $"baltic-fixture-{p.PlatformId}"))
            .ToArray();
        var reader = new InMemoryCatalogReader(bindings, "p0-baltic-fixture", platforms);
        return CatalogPlatformBrowseProjection.FromReader(reader);
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return null;
    }
}
