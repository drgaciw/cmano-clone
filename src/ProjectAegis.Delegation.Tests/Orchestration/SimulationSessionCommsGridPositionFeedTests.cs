namespace ProjectAegis.Delegation.Tests.Orchestration;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Sim.Comms;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>DRG-417: SimulationSession feeds read-only unit positions to the comms-grid registry.</summary>
[TestFixture]
public sealed class SimulationSessionCommsGridPositionFeedTests
{
    private sealed class FakePositions : IUnitPositionSource
    {
        public readonly Dictionary<string, (double Lat, double Lon)> Map = new(StringComparer.Ordinal);
        public readonly List<string> Reads = new();

        public IReadOnlyCollection<string> UnitIds => Map.Keys.ToArray();

        public bool TryGetPosition(string unitId, out double latitudeDeg, out double longitudeDeg)
        {
            Reads.Add(unitId);
            if (Map.TryGetValue(unitId, out var p))
            {
                latitudeDeg = p.Lat;
                longitudeDeg = p.Lon;
                return true;
            }

            latitudeDeg = longitudeDeg = 0;
            return false;
        }
    }

    private static ObservedState State(double t) => new(t, 0, 0, new Dictionary<TargetId, bool>());

    private static SimulationSession Build(FakePositions? source)
    {
        var session = new SimulationSession(1)
        {
            CommsGrid = new CommsGridRegistry(new[]
            {
                new ScenarioCommsGridTransition(10, "u1", "OffGrid", "below comms depth"),
                new ScenarioCommsGridTransition(20, "u1", "OnGrid", "periscope depth"),
            }),
            PositionSource = source,
        };
        session.BeginExecution();
        return session;
    }

    [Test]
    public void Last_report_freezes_while_off_grid_and_resumes_after_OnGrid()
    {
        var src = new FakePositions();
        var session = Build(src);
        for (var t = 0; t <= 25; t++)
        {
            src.Map["u1"] = (55.0 + t * 0.01, 18.0 + t * 0.01);
            session.TickHeadless(State(t));
            Assert.That(session.CommsGrid!.TryGetLastReport("u1", out var r), Is.True);
            if (t >= 10 && t < 20)
            {
                Assert.That(r.ReportedAtTick, Is.EqualTo(9UL), $"tick {t}: frozen at last on-grid report");
            }
            else
            {
                Assert.That(r.ReportedAtTick, Is.EqualTo((ulong)t));
                Assert.That(r.LatitudeDeg, Is.EqualTo(55.0 + t * 0.01).Within(1e-9));
            }
        }
    }

    [Test]
    public void Positions_are_read_in_unit_id_ordinal_order()
    {
        var src = new FakePositions();
        src.Map["b"] = (1, 1);
        src.Map["U2"] = (2, 2);
        src.Map["a"] = (3, 3);
        var session = Build(src);
        session.TickHeadless(State(0));
        Assert.That(src.Reads, Is.EqualTo(new[] { "U2", "a", "b" }));
    }

    [Test]
    public void Null_source_is_a_no_op()
    {
        var session = Build(null);
        session.TickHeadless(State(0));
        Assert.That(session.CommsGrid!.TryGetLastReport("u1", out _), Is.False);
    }

    [Test]
    public void Position_feed_does_not_change_comms_grid_hash()
    {
        var withFeed = Build(new FakePositions { Map = { ["u1"] = (55, 18) } });
        var without = Build(null);
        for (var t = 0; t <= 25; t++)
        {
            withFeed.TickHeadless(State(t));
            without.TickHeadless(State(t));
        }

        Assert.That(withFeed.CommsGrid!.ComputeHash(), Is.EqualTo(without.CommsGrid!.ComputeHash()));
    }
}
