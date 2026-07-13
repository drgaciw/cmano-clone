using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

/// <summary>
/// Slice 1: ladder inject/cascade/ROE-change policies must emit real mid-run
/// state deltas (CommsStateChange and/or PolicyUpdate roe).
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessLadderInjectTests
{
    private static readonly string[] LadderInjectIds =
    [
        "gauntlet-t4-random-inject",
        "gauntlet-t5-cascade",
        "gauntlet-t5-roe-change",
    ];

    [TestCase("gauntlet-t4-random-inject")]
    [TestCase("gauntlet-t5-cascade")]
    [TestCase("gauntlet-t5-roe-change")]
    public void Ladder_inject_policy_emits_mid_run_comms_or_roe_state_delta(string scenarioId)
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        Assert.That(ScenarioPolicyRepository.TryGet(scenarioId), Is.Not.Null, scenarioId);

        var result = BalticReplayHarness.Run(42, scenarioId, ticks: 12, mvpEngagement: true);
        Assert.That(result.Fingerprint, Is.Not.Null.And.Not.Empty);

        var hasCommsDelta = result.DecisionLog.CommsStateChanges.Any(c =>
            c.NewState is CommsState.Degraded or CommsState.Denied);
        var hasRoeUpdate = result.DecisionLog.ChronologicalEntries().Any(e =>
            e.Kind == OrderLogEntryKind.PolicyUpdate
            && e.Payload is PolicyUpdateRecord p
            && string.Equals(p.Field, "roe", StringComparison.Ordinal));

        Assert.That(
            hasCommsDelta || hasRoeUpdate,
            Is.True,
            $"{scenarioId}: must emit CommsStateChange(Degraded/Denied) and/or PolicyUpdate field=roe — not EventFired-only");

        if (hasCommsDelta)
        {
            Assert.That(result.Fingerprint, Does.Contain("CommsStateChange"));
            Assert.That(
                result.Fingerprint,
                Does.Contain("Degraded").Or.Contain("Denied"));
            Assert.That(
                result.DecisionLog.CommsStateChanges.Min(c => c.SimTick),
                Is.GreaterThanOrEqualTo(1ul),
                "comms inject should be mid-run (atTick >= 1)");
        }

        if (hasRoeUpdate)
        {
            Assert.That(result.Fingerprint, Does.Contain("PolicyUpdate"));
        }
    }

    [Test]
    public void Ladder_inject_policies_declare_comms_timeline_or_mission_triggers()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        foreach (var sid in LadderInjectIds)
        {
            var dto = ProjectAegis.Data.Scenario.ScenarioPolicyJsonCatalog.TryGetJson(sid);
            Assert.That(dto, Is.Not.Null, sid);
            var hasComms = dto!.Comms is { Count: > 0 };
            var hasTriggers = dto.Mission?.Triggers is { Count: > 0 };
            Assert.That(
                hasComms || hasTriggers,
                Is.True,
                $"{sid}: policy JSON must include comms[] and/or mission.triggers for real injects");
        }
    }
}
