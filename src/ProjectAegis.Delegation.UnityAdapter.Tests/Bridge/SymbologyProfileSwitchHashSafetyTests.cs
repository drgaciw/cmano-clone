namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using Core;
using Orchestration;
using Projection;
using Traits;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

/// <summary>
/// S126-04 SYM-03 (DRG-231): headless symbology profile switch is presentation-only.
/// Toggling profiles between ticks leaves orders, decision-log fingerprint and the
/// Baltic v2 replay world hash unchanged.
/// </summary>
[TestFixture]
public sealed class SymbologyProfileSwitchHashSafetyTests
{
    private const ulong ProductionBalticWorldHash = 17144800277401907079UL;
    private const int Ticks = 4;

    [Test]
    public void Profile_toggles_between_ticks_leave_orders_and_decision_log_unchanged()
    {
        var (baselineBridge, baselineSink) = CreateMixedBridge();
        var (toggledBridge, toggledSink) = CreateMixedBridge();
        var profileSwitch = new SymbologyProfileSwitch();
        var observedKeys = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["friendly-1"] = SymbolKeyRegistry.NavalSurfaceCombatant,
        };

        for (var tick = 1; tick <= Ticks; tick++)
        {
            var snapshot = new SimWorldSnapshotStub(simTime: tick, contactCount: 2);
            baselineBridge.Tick(snapshot, baselineSink);

            var pictureBefore = MapPictureBridge.Build(
                snapshot, toggledBridge.Registry, toggledBridge.Orchestrator.DecisionLog, layoutSeed: 7);
            var logBefore = toggledBridge.Orchestrator.DecisionLog.ComputeFingerprint();
            for (var i = 0; i < 3; i++)
            {
                var styled = SymbologyProjection.Apply(pictureBefore, profileSwitch.Toggle(), observedKeys);
                _ = SymbologyLegendProjection.Build(profileSwitch.Current);
                Assert.That(styled, Has.Count.EqualTo(pictureBefore.Count));
            }

            var pictureAfter = MapPictureBridge.Build(
                snapshot, toggledBridge.Registry, toggledBridge.Orchestrator.DecisionLog, layoutSeed: 7);
            Assert.That(pictureAfter, Is.EqualTo(pictureBefore));
            Assert.That(toggledBridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(logBefore));

            toggledBridge.Tick(snapshot, toggledSink);
        }

        Assert.That(baselineSink.Applied, Is.Not.Empty);
        Assert.That(toggledSink.Applied, Is.EqualTo(baselineSink.Applied));
        Assert.That(
            toggledBridge.Orchestrator.DecisionLog.ComputeFingerprint(),
            Is.EqualTo(baselineBridge.Orchestrator.DecisionLog.ComputeFingerprint()));
    }

    [Test]
    public void Profile_toggles_around_baltic_replay_preserve_production_world_hash()
    {
        var baseline = BalticReplayHarness.Run(42, "baltic-patrol", Ticks);

        var profileSwitch = new SymbologyProfileSwitch();
        for (var i = 0; i < 5; i++)
        {
            _ = SymbologyLegendProjection.Build(profileSwitch.Toggle());
        }

        var afterToggle = BalticReplayHarness.Run(42, "baltic-patrol", Ticks);

        Assert.That(baseline.WorldHash, Is.EqualTo(ProductionBalticWorldHash));
        Assert.That(afterToggle.WorldHash, Is.EqualTo(ProductionBalticWorldHash));
        Assert.That(afterToggle.DetectionWorldHash, Is.EqualTo(baseline.DetectionWorldHash));
        Assert.That(afterToggle.FingerprintSha256, Is.EqualTo(baseline.FingerprintSha256));
        Assert.That(BalticReplayHarness.DiagnoseDivergence(baseline, afterToggle), Is.EqualTo("MATCH"));
    }

    private static (DelegationBridge Bridge, RecordingSink Sink) CreateMixedBridge()
    {
        var bridge = new DelegationBridge(globalSeed: 42, mvpEngagement: false);
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "friendly-1");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "opposing-1");
        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            friendly: [friendly.Target],
            opposing: [opposing.Target],
            defaultTraits: PersonalityCatalog.All[0].Traits);
        bridge.BeginExecution();
        return (bridge, new RecordingSink());
    }

    private sealed class RecordingSink : IOrderSink
    {
        public List<(EntityKey Entity, Order Order)> Applied { get; } = new();

        public void ApplyOrder(EntityKey entity, in Order order) =>
            Applied.Add((entity, order));
    }
}
