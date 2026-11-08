using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class LossesScoringProjectionTests
{
    [Test]
    public void Project_counts_kills_and_denials_in_score()
    {
        var log = new DecisionLog();
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("u1"), new TargetId("hostile-1"), 1,
            EngagementOutcomeCodes.Kill, 0.1));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            2, 2.0, 2,
            new AgentId("a1"),
            new TargetId("hostile-1"),
            0,
            FireAbortReason.RoeHoldFire,
            OrderKind.Engage));

        var tally = LossesScoringProjection.Project(log);
        Assert.That(tally.HostileKills, Is.EqualTo(1));
        Assert.That(tally.PolicyDenials, Is.EqualTo(1));
        Assert.That(tally.Score, Is.EqualTo(100 - 5));
    }

    /// <summary>
    /// BUG-losses-scoring-side-unaware (QA Gauntlet): the projection counted every Kill-coded
    /// outcome in the shared DecisionLog regardless of which side fired it, so a RED kill
    /// against a BLUE unit inflated BLUE's own tally — the field is literally named
    /// HostileKills, so crediting a friendly loss to it is a contract violation. When a scored
    /// side is supplied, only kills whose SHOOTER is on that side may count.
    /// </summary>
    [Test]
    public void Project_for_a_side_excludes_kills_scored_by_the_opposing_side()
    {
        var log = new DecisionLog();

        // BLUE u1 kills RED hostile-1 — a genuine BLUE kill.
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("u1"), new TargetId("hostile-1"), 1,
            EngagementOutcomeCodes.Kill, 0.1));

        // RED hostile-1 kills BLUE u1 — a BLUE *loss*; must not count toward BLUE's kills.
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            2, 2.0, 2, new TargetId("hostile-1"), new TargetId("u1"), 2,
            EngagementOutcomeCodes.Kill, 0.2));

        var tally = LossesScoringProjection.Project(log, baseScore: 0, scoredSide: "blue");

        Assert.That(tally.HostileKills, Is.EqualTo(1), "only the BLUE-fired kill counts for BLUE");
        Assert.That(tally.Score, Is.EqualTo(100));
    }

    /// <summary>Scoring the same log for RED mirrors it — each side gets exactly its own kill.</summary>
    [Test]
    public void Project_for_the_opposing_side_counts_only_that_sides_kills()
    {
        var log = new DecisionLog();
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("u1"), new TargetId("hostile-1"), 1,
            EngagementOutcomeCodes.Kill, 0.1));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            2, 2.0, 2, new TargetId("hostile-1"), new TargetId("u1"), 2,
            EngagementOutcomeCodes.Kill, 0.2));

        var tally = LossesScoringProjection.Project(log, baseScore: 0, scoredSide: "red");

        Assert.That(tally.HostileKills, Is.EqualTo(1));
    }

    /// <summary>
    /// Units with no registered side (legacy synthetic scenarios that never call
    /// RegisterScenarioSide) must still be counted — the filter only excludes a kill when the
    /// shooter is positively known to be on a different side. Otherwise every pre-ORBAT
    /// scenario would silently score zero.
    /// </summary>
    [Test]
    public void Project_counts_kills_from_units_with_no_registered_side()
    {
        var log = new DecisionLog();
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("unregistered-shooter"), new TargetId("unregistered-victim"), 1,
            EngagementOutcomeCodes.Kill, 0.1));

        var tally = LossesScoringProjection.Project(log, baseScore: 0, scoredSide: "blue");

        Assert.That(tally.HostileKills, Is.EqualTo(1));
    }


    /// <summary>
    /// DRG-257 / Codex P2: after BalticReplayHarness.Run returns, the live registry is cleared.
    /// Catalog-backed shooters would then look "unknown" and inflate the wrong side's tally.
    /// Passing the captured scenario side map must keep filtering correct without the live registry.
    /// </summary>
    [Test]
    public void Project_with_captured_scenario_sides_filters_after_registry_clear()
    {
        BalticV3SideRegistry.ClearScenarioSides();
        var blueShooter = "f-361-iver-huitfeldt-2012";
        var redShooter = "f-450-elli-kortenaer-batch-ii-0";
        var log = new DecisionLog();
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId(blueShooter), new TargetId(redShooter), 1,
            EngagementOutcomeCodes.Kill, 0.1));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            2, 2, 2, new TargetId(redShooter), new TargetId(blueShooter), 2,
            EngagementOutcomeCodes.Kill, 0.2));

        var sides = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [blueShooter] = "blue",
            [redShooter] = "red",
        };

        Assert.That(BalticV3SideRegistry.GetSideForUnit(blueShooter), Is.Null, "registry must be clear");
        var withoutMap = LossesScoringProjection.Project(log, scoredSide: "red");
        Assert.That(withoutMap.HostileKills, Is.EqualTo(2), "cleared registry treats catalog ids as unknown");

        var withMap = LossesScoringProjection.Project(log, scoredSide: "red", scenarioSides: sides);
        Assert.That(withMap.HostileKills, Is.EqualTo(1), "captured map keeps RED from claiming BLUE kills");
    }

}