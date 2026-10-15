using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// DRG-403: terminal <see cref="CombatEventSnapshot"/> rows become presentation-only
/// fire lines and impact markers. The snapshot carries no sim RNG (ADR-010).
/// </summary>
public sealed class CombatVfxProjectionTests
{
    [Test]
    public void Project_null_or_empty_returns_empty()
    {
        var positions = Positions(("u1", 0.2f, 0.3f), ("hostile-1", 0.7f, 0.6f));

        Assert.That(CombatVfxProjection.Project(null, positions, 1.0), Is.EqualTo(CombatVfxFrame.Empty));
        Assert.That(CombatVfxProjection.Project(CombatEventSnapshot.Empty, positions, 1.0), Is.EqualTo(CombatVfxFrame.Empty));
        Assert.That(
            CombatVfxProjection.Project(KillSnapshot(), new Dictionary<string, (float X, float Y)>(), 1.0),
            Is.EqualTo(CombatVfxFrame.Empty));
    }

    [Test]
    public void Project_terminal_outcome_emits_fire_line_and_impact_at_unit_positions()
    {
        var frame = CombatVfxProjection.Project(
            KillSnapshot(),
            Positions(("u1", 0.2f, 0.3f), ("hostile-1", 0.8f, 0.7f)),
            nowSimTime: 1.0);

        Assert.That(frame.FireLines, Has.Count.EqualTo(1));
        Assert.That(frame.ImpactMarkers, Has.Count.EqualTo(1));

        var line = frame.FireLines[0];
        Assert.That(line.ShooterUnitId, Is.EqualTo("u1"));
        Assert.That(line.TargetUnitId, Is.EqualTo("hostile-1"));
        Assert.That(line.EngagementId, Is.EqualTo(9ul));
        Assert.That(line.Key, Is.EqualTo("vfx-line:9"));
        Assert.That(line.FromX, Is.EqualTo(0.2f).Within(1e-6f));
        Assert.That(line.FromY, Is.EqualTo(0.3f).Within(1e-6f));
        Assert.That(line.ToX, Is.EqualTo(0.8f).Within(1e-6f));
        Assert.That(line.ToY, Is.EqualTo(0.7f).Within(1e-6f));
        Assert.That(line.StyleClass, Is.EqualTo(CombatVfxProjection.StyleFireLine));

        var impact = frame.ImpactMarkers[0];
        Assert.That(impact.TargetUnitId, Is.EqualTo("hostile-1"));
        Assert.That(impact.OutcomeCode, Is.EqualTo(EngagementOutcomeCodes.Kill));
        Assert.That(impact.Key, Is.EqualTo("vfx-impact:9"));
        Assert.That(impact.X, Is.EqualTo(0.8f).Within(1e-6f));
        Assert.That(impact.Y, Is.EqualTo(0.7f).Within(1e-6f));
        Assert.That(impact.StyleClass, Is.EqualTo(CombatVfxProjection.StyleImpactKill));
    }

    [Test]
    public void Project_skips_when_either_endpoint_is_unknown()
    {
        var events = KillSnapshot();
        var missingVictim = CombatVfxProjection.Project(events, Positions(("u1", 0.2f, 0.3f)), 1.0);
        var missingShooter = CombatVfxProjection.Project(events, Positions(("hostile-1", 0.8f, 0.7f)), 1.0);

        Assert.That(missingVictim, Is.EqualTo(CombatVfxFrame.Empty));
        Assert.That(missingShooter, Is.EqualTo(CombatVfxFrame.Empty));
    }

    [Test]
    public void Project_omits_expired_fire_line_and_impact_by_sim_time()
    {
        var events = KillSnapshot();
        var positions = Positions(("u1", 0.2f, 0.3f), ("hostile-1", 0.8f, 0.7f));

        var stillLive = CombatVfxProjection.Project(events, positions, 1.0 + CombatVfxProjection.FireLineHoldSeconds);
        Assert.That(stillLive.FireLines, Has.Count.EqualTo(1));
        Assert.That(stillLive.ImpactMarkers, Has.Count.EqualTo(1));

        var lineExpired = CombatVfxProjection.Project(
            events,
            positions,
            1.0 + CombatVfxProjection.FireLineHoldSeconds + 0.01);
        Assert.That(lineExpired.FireLines, Is.Empty);
        Assert.That(lineExpired.ImpactMarkers, Has.Count.EqualTo(1));

        var bothExpired = CombatVfxProjection.Project(
            events,
            positions,
            1.0 + CombatVfxProjection.ImpactHoldSeconds + 0.01);
        Assert.That(bothExpired, Is.EqualTo(CombatVfxFrame.Empty));
    }

    [Test]
    public void Project_non_terminal_events_emit_nothing()
    {
        var events = new CombatEventSnapshot(new[]
        {
            Event(CombatEventPhase.IntentAccepted, "u1", "hostile-1", "accepted", 1),
            Event(CombatEventPhase.Firing, "u1", "hostile-1", CombatEventProjection.OutcomeLaunch, 2),
            Event(CombatEventPhase.InFlight, "u1", "hostile-1", CombatEventProjection.OutcomeInFlight, 3),
            Event(CombatEventPhase.AuthorizationRefused, "u1", "hostile-1", "RoeHoldFire", 4),
        });

        var frame = CombatVfxProjection.Project(
            events,
            Positions(("u1", 0.2f, 0.3f), ("hostile-1", 0.8f, 0.7f)),
            1.0);

        Assert.That(frame, Is.EqualTo(CombatVfxFrame.Empty));
    }

    [Test]
    public void Project_maps_outcome_styles()
    {
        var events = new CombatEventSnapshot(new[]
        {
            Terminal("u1", "h1", EngagementOutcomeCodes.Hit, 1),
            Terminal("u1", "h2", EngagementOutcomeCodes.Miss, 2),
            Terminal("u1", "h3", EngagementOutcomeCodes.Intercept, 3),
            Terminal("u1", "h4", EngagementOutcomeCodes.Kill, 4),
        });
        var positions = Positions(
            ("u1", 0.1f, 0.1f),
            ("h1", 0.2f, 0.2f),
            ("h2", 0.3f, 0.3f),
            ("h3", 0.4f, 0.4f),
            ("h4", 0.5f, 0.5f));

        var a = CombatVfxProjection.Project(events, positions, 1.0);
        var b = CombatVfxProjection.Project(events, positions, 1.0);

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
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.15f, 0.25f, IsDestroyed: false),
            new MapSymbolEntry("hostile-1", "Hostile", "◆", "hostile-1", 0.65f, 0.55f, IsDestroyed: true),
        };

        var frame = CombatVfxProjection.Project(KillSnapshot(), symbols, 1.0);

        Assert.That(frame.FireLines, Has.Count.EqualTo(1));
        Assert.That(frame.FireLines[0].FromX, Is.EqualTo(0.15f).Within(1e-6f));
        Assert.That(frame.ImpactMarkers[0].X, Is.EqualTo(0.65f).Within(1e-6f));
    }

    [Test]
    public void Project_reads_combat_events_and_not_the_decision_log()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);
        var source = File.ReadAllText(Path.Combine(
            root!,
            "src",
            "ProjectAegis.Delegation",
            "Projection",
            "CombatVfxProjection.cs"));

        Assert.That(source, Does.Contain("CombatEventSnapshot"));
        Assert.That(source, Does.Contain("CombatEventPhase.TerminalOutcome"));
        Assert.That(source, Does.Not.Contain("DecisionLog"));
        Assert.That(source, Does.Not.Contain("PkDraw"));
        Assert.That(source, Does.Not.Contain("EngagementOutcomeRecord"));
        Assert.That(source, Does.Not.Contain("EngagementOutcomes"));
        Assert.That(source, Does.Not.Contain("ChronologicalEntries"));
    }

    private static CombatEventSnapshot KillSnapshot() =>
        new(new[] { Terminal("u1", "hostile-1", EngagementOutcomeCodes.Kill, 9) });

    private static CombatEvent Terminal(string shooter, string target, string outcome, ulong correlation) =>
        Event(CombatEventPhase.TerminalOutcome, shooter, target, outcome, correlation);

    private static CombatEvent Event(
        CombatEventPhase phase,
        string shooter,
        string target,
        string outcome,
        ulong correlation) =>
        new(phase, shooter, target, "Gun", outcome, correlation, 1.0, 1, $"outcome:{outcome}");

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

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
