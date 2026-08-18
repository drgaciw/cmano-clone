using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// Track C MVP: DecisionLog engagement outcomes → presentation-only fire lines / impact markers.
/// Must stay off sim RNG (ADR-010); PkDraw is ignored.
/// </summary>
public sealed class CombatVfxProjectionTests
{
    [Test]
    public void Project_null_or_empty_returns_empty()
    {
        var positions = Positions(("u1", 0.2f, 0.3f), ("hostile-1", 0.7f, 0.6f));

        Assert.That(CombatVfxProjection.Project(null, positions, 1.0), Is.EqualTo(CombatVfxFrame.Empty));
        Assert.That(CombatVfxProjection.Project(new DecisionLog(), positions, 1.0), Is.EqualTo(CombatVfxFrame.Empty));
        Assert.That(
            CombatVfxProjection.Project(SeededKillLog(), new Dictionary<string, (float X, float Y)>(), 1.0),
            Is.EqualTo(CombatVfxFrame.Empty));
    }

    [Test]
    public void Project_outcome_emits_fire_line_and_impact_at_unit_positions()
    {
        var log = SeededKillLog();
        var frame = CombatVfxProjection.Project(
            log,
            Positions(("u1", 0.2f, 0.3f), ("hostile-1", 0.8f, 0.7f)),
            nowSimTime: 1.0);

        Assert.That(frame.FireLines, Has.Count.EqualTo(1));
        Assert.That(frame.ImpactMarkers, Has.Count.EqualTo(1));

        var line = frame.FireLines[0];
        Assert.That(line.ShooterUnitId, Is.EqualTo("u1"));
        Assert.That(line.TargetUnitId, Is.EqualTo("hostile-1"));
        Assert.That(line.EngagementId, Is.EqualTo(9u));
        Assert.That(line.FromX, Is.EqualTo(0.2f).Within(1e-6f));
        Assert.That(line.FromY, Is.EqualTo(0.3f).Within(1e-6f));
        Assert.That(line.ToX, Is.EqualTo(0.8f).Within(1e-6f));
        Assert.That(line.ToY, Is.EqualTo(0.7f).Within(1e-6f));
        Assert.That(line.StyleClass, Is.EqualTo(CombatVfxProjection.StyleFireLine));

        var impact = frame.ImpactMarkers[0];
        Assert.That(impact.TargetUnitId, Is.EqualTo("hostile-1"));
        Assert.That(impact.OutcomeCode, Is.EqualTo(EngagementOutcomeCodes.Kill));
        Assert.That(impact.X, Is.EqualTo(0.8f).Within(1e-6f));
        Assert.That(impact.Y, Is.EqualTo(0.7f).Within(1e-6f));
        Assert.That(impact.StyleClass, Is.EqualTo(CombatVfxProjection.StyleImpactKill));
    }

    [Test]
    public void Project_skips_when_either_endpoint_is_unknown()
    {
        var log = SeededKillLog();
        var missingVictim = CombatVfxProjection.Project(
            log,
            Positions(("u1", 0.2f, 0.3f)),
            1.0);
        var missingShooter = CombatVfxProjection.Project(
            log,
            Positions(("hostile-1", 0.8f, 0.7f)),
            1.0);

        Assert.That(missingVictim, Is.EqualTo(CombatVfxFrame.Empty));
        Assert.That(missingShooter, Is.EqualTo(CombatVfxFrame.Empty));
    }

    [Test]
    public void Project_omits_expired_fire_line_and_impact_by_sim_time()
    {
        var log = SeededKillLog(simTime: 1.0);
        var positions = Positions(("u1", 0.2f, 0.3f), ("hostile-1", 0.8f, 0.7f));

        var stillLive = CombatVfxProjection.Project(log, positions, 1.0 + CombatVfxProjection.FireLineHoldSeconds);
        Assert.That(stillLive.FireLines, Has.Count.EqualTo(1));
        Assert.That(stillLive.ImpactMarkers, Has.Count.EqualTo(1));

        var lineExpired = CombatVfxProjection.Project(
            log,
            positions,
            1.0 + CombatVfxProjection.FireLineHoldSeconds + 0.01);
        Assert.That(lineExpired.FireLines, Is.Empty);
        Assert.That(lineExpired.ImpactMarkers, Has.Count.EqualTo(1));

        var bothExpired = CombatVfxProjection.Project(
            log,
            positions,
            1.0 + CombatVfxProjection.ImpactHoldSeconds + 0.01);
        Assert.That(bothExpired, Is.EqualTo(CombatVfxFrame.Empty));
    }

    [Test]
    public void Project_launch_without_outcome_and_aborted_engagement_emit_nothing()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(1, 1.0, 1, new TargetId("u1"), 4, Launched: true));
        log.AppendEngagement(new EngagementRecord(2, 1.0, 1, new TargetId("u1"), 5, Launched: false, "RoeHoldFire"));

        var frame = CombatVfxProjection.Project(
            log,
            Positions(("u1", 0.2f, 0.3f), ("hostile-1", 0.8f, 0.7f)),
            1.0);

        Assert.That(frame, Is.EqualTo(CombatVfxFrame.Empty));
    }

    [Test]
    public void Project_maps_outcome_styles_and_ignores_pk_draw()
    {
        var log = new DecisionLog();
        AppendOutcome(log, 1, 1.0, "u1", "h1", 1, EngagementOutcomeCodes.Hit, pkDraw: 0.01);
        AppendOutcome(log, 2, 1.0, "u1", "h2", 2, EngagementOutcomeCodes.Miss, pkDraw: 0.99);
        AppendOutcome(log, 3, 1.0, "u1", "h3", 3, EngagementOutcomeCodes.Intercept, pkDraw: 0.42);
        AppendOutcome(log, 4, 1.0, "u1", "h4", 4, EngagementOutcomeCodes.Kill, pkDraw: 0.00);

        var positions = Positions(
            ("u1", 0.1f, 0.1f),
            ("h1", 0.2f, 0.2f),
            ("h2", 0.3f, 0.3f),
            ("h3", 0.4f, 0.4f),
            ("h4", 0.5f, 0.5f));

        var a = CombatVfxProjection.Project(log, positions, 1.0);
        var b = CombatVfxProjection.Project(log, positions, 1.0);

        Assert.That(a.ImpactMarkers.Select(m => m.StyleClass), Is.EqualTo(new[]
        {
            CombatVfxProjection.StyleImpactHit,
            CombatVfxProjection.StyleImpactMiss,
            CombatVfxProjection.StyleImpactIntercept,
            CombatVfxProjection.StyleImpactKill,
        }));
        Assert.That(b, Is.EqualTo(a));
        Assert.That(a.FireLines, Has.Count.EqualTo(4));
    }

    [Test]
    public void Project_from_map_symbols_uses_normalized_xy()
    {
        var log = SeededKillLog();
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.15f, 0.25f, IsDestroyed: false),
            new MapSymbolEntry("hostile-1", "Hostile", "◆", "hostile-1", 0.65f, 0.55f, IsDestroyed: true),
        };

        var frame = CombatVfxProjection.Project(log, symbols, 1.0);

        Assert.That(frame.FireLines, Has.Count.EqualTo(1));
        Assert.That(frame.FireLines[0].FromX, Is.EqualTo(0.15f).Within(1e-6f));
        Assert.That(frame.ImpactMarkers[0].X, Is.EqualTo(0.65f).Within(1e-6f));
    }

    [Test]
    public void Project_is_pure_and_does_not_consume_pk_draw()
    {
        var logA = new DecisionLog();
        var logB = new DecisionLog();
        AppendOutcome(logA, 1, 2.0, "u1", "hostile-1", 7, EngagementOutcomeCodes.Hit, pkDraw: 0.11);
        AppendOutcome(logB, 1, 2.0, "u1", "hostile-1", 7, EngagementOutcomeCodes.Hit, pkDraw: 0.88);

        var positions = Positions(("u1", 0.2f, 0.3f), ("hostile-1", 0.8f, 0.7f));
        var a = CombatVfxProjection.Project(logA, positions, 2.0);
        var b = CombatVfxProjection.Project(logB, positions, 2.0);

        Assert.That(b, Is.EqualTo(a));
    }

    private static DecisionLog SeededKillLog(double simTime = 1.0)
    {
        var log = new DecisionLog();
        AppendOutcome(log, 1, simTime, "u1", "hostile-1", 9, EngagementOutcomeCodes.Kill, pkDraw: 0.5);
        return log;
    }

    private static void AppendOutcome(
        DecisionLog log,
        ulong sequenceId,
        double simTime,
        string shooter,
        string victim,
        ulong engagementId,
        string outcome,
        double pkDraw)
    {
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            sequenceId,
            simTime,
            SimTick: (ulong)Math.Max(0, (long)simTime),
            new TargetId(shooter),
            new TargetId(victim),
            engagementId,
            outcome,
            pkDraw));
    }

    private static IReadOnlyDictionary<string, (float X, float Y)> Positions(
        params (string Id, float X, float Y)[] rows)
    {
        var map = new Dictionary<string, (float X, float Y)>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            map[row.Id] = (row.X, row.Y);
        }

        return map;
    }
}
