using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Tests.Helpers;
using ProjectAegis.Sim.Logistics;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Orchestration;

[TestFixture]
public sealed class SimulationSessionLogisticsFsmTests
{
    [Test]
    public void Tick_advances_seeded_Prepping_toward_Airborne()
    {
        var session = new SimulationSession(11);
        var unit = new UnitTarget(new TargetId("air-1"));
        unit.Slot.SetActive(new HumanController());
        session.Orchestrator.Register(unit);
        session.BeginExecution();

        var launched = AirOpsFsm.RequestLaunch(AirOpsUnitState.OnGround("air-1", readyForLaunch: true));
        Assert.That(launched.Accepted, Is.True);
        session.AirOps = new AirOpsStateMap();
        session.AirOps.Upsert(launched.State);

        // Default prep3 + taxi2 + takeoff1 = 6 ticks to Airborne from Prepping start.
        for (var t = 0; t < 6; t++)
        {
            session.Tick(MvpObservedStates.EngageTick(t));
        }

        Assert.That(session.AirOps.TryGet("air-1", out var state), Is.True);
        Assert.That(state.Phase, Is.EqualTo(AirOpsPhase.Airborne));
        Assert.That(state.TimeToReadyTicks, Is.EqualTo(0));
        Assert.That(state.CanAbortLaunch, Is.False);
    }

    [Test]
    public void Tick_executed_LaunchAircraft_starts_prep_when_unit_ready()
    {
        var session = new SimulationSession(22);
        var unit = new UnitTarget(new TargetId("air-1"));
        var human = new HumanController();
        unit.Slot.SetActive(human);
        session.Orchestrator.Register(unit);
        session.UnitReadiness = new UnitReadinessMap(
            new Dictionary<string, bool> { ["air-1"] = true });
        session.BeginExecution();

        human.Enqueue(
            new Order(new OrderId(1), new TargetId("air-1"), 0, OrderKind.LaunchAircraft, RiskLevel.Low),
            executeSimTick: 0);

        session.Tick(MvpObservedStates.EngageTick(0));

        Assert.That(session.AirOps, Is.Not.Null);
        Assert.That(session.AirOps!.TryGet("air-1", out var state), Is.True);
        Assert.That(state.Phase, Is.EqualTo(AirOpsPhase.Prepping));
        // Order starts prep (timer=3) then same-tick TickAll decrements once → 2.
        Assert.That(state.TimeToReadyTicks, Is.EqualTo(AirOpsFsm.DefaultPrepTicks - 1));
        Assert.That(state.CanAbortLaunch, Is.True);
    }

    [Test]
    public void Tick_LaunchAircraft_refuses_when_unit_not_ready()
    {
        var session = new SimulationSession(23);
        var unit = new UnitTarget(new TargetId("air-1"));
        var human = new HumanController();
        unit.Slot.SetActive(human);
        session.Orchestrator.Register(unit);
        session.UnitReadiness = new UnitReadinessMap(
            new Dictionary<string, bool> { ["air-1"] = false });
        session.BeginExecution();

        human.Enqueue(
            new Order(new OrderId(1), new TargetId("air-1"), 0, OrderKind.LaunchAircraft, RiskLevel.Low),
            executeSimTick: 0);

        session.Tick(MvpObservedStates.EngageTick(0));

        Assert.That(session.AirOps, Is.Not.Null);
        Assert.That(session.AirOps!.TryGet("air-1", out var state), Is.True);
        Assert.That(state.Phase, Is.EqualTo(AirOpsPhase.OnGround));
        Assert.That(state.ReadyForLaunch, Is.False);
    }

    [Test]
    public void Tick_executed_LaunchBoat_starts_prep()
    {
        var session = new SimulationSession(24);
        var unit = new UnitTarget(new TargetId("boat-1"));
        var human = new HumanController();
        unit.Slot.SetActive(human);
        session.Orchestrator.Register(unit);
        session.BeginExecution();

        human.Enqueue(
            new Order(new OrderId(1), new TargetId("boat-1"), 0, OrderKind.LaunchBoat, RiskLevel.Low),
            executeSimTick: 0);

        session.Tick(MvpObservedStates.EngageTick(0));

        Assert.That(session.BoatOps, Is.Not.Null);
        Assert.That(session.BoatOps!.TryGet("boat-1", out var state), Is.True);
        Assert.That(state.Phase, Is.EqualTo(BoatOpsPhase.Prepping));
        Assert.That(state.TimeToReadyTicks, Is.EqualTo(BoatOpsFsm.DefaultPrepTicks - 1));
    }

    [Test]
    public void Tick_two_runs_same_seed_same_air_ops_snapshot()
    {
        static string Fingerprint(SimulationSession session)
        {
            if (session.AirOps is null || session.AirOps.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(
                "|",
                session.AirOps.Snapshot()
                    .Select(s => $"{s.UnitId}:{s.Phase}:{s.TimeToReadyTicks}:{s.ReadyForLaunch}:{s.CanAbortLaunch}"));
        }

        static SimulationSession RunOnce(int seed)
        {
            var session = new SimulationSession(seed);
            var unit = new UnitTarget(new TargetId("air-1"));
            var human = new HumanController();
            unit.Slot.SetActive(human);
            session.Orchestrator.Register(unit);
            session.UnitReadiness = new UnitReadinessMap(
                new Dictionary<string, bool> { ["air-1"] = true });
            session.BeginExecution();

            human.Enqueue(
                new Order(new OrderId(1), new TargetId("air-1"), 0, OrderKind.LaunchAircraft, RiskLevel.Low),
                executeSimTick: 0);

            for (var t = 0; t < 4; t++)
            {
                session.Tick(MvpObservedStates.EngageTick(t));
            }

            return session;
        }

        var a = Fingerprint(RunOnce(99));
        var b = Fingerprint(RunOnce(99));
        Assert.That(a, Is.Not.Empty);
        Assert.That(a, Is.EqualTo(b));
    }
}
