using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

/// <summary>Phase-3 theater: mid-run injects and dynamic multi-kill victory.</summary>
[TestFixture]
public sealed class BalticReplayHarnessTheaterPolicyTests
{
    [Test]
    public void Theater_inject_mid_run_comms_state_change_and_contact_roe_policy_update()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        Assert.That(ScenarioPolicyRepository.TryGet("gauntlet-theater-inject"), Is.Not.Null);

        var result = BalticReplayHarness.Run(42, "gauntlet-theater-inject", ticks: 10, mvpEngagement: true);

        // Mission event markers (not sufficient alone — must also change real state).
        Assert.That(result.Fingerprint, Does.Contain("inject_comms_degrade"));
        Assert.That(result.Fingerprint, Does.Contain("inject_mid_run_event"));

        // Real mid-run inject: policy comms[] → CommsStateChange in order log
        Assert.That(result.Fingerprint, Does.Contain("CommsStateChange"));
        Assert.That(result.Fingerprint, Does.Contain("Degraded"));
        Assert.That(result.Fingerprint, Does.Contain("inject_jamming"));
        Assert.That(result.Fingerprint, Does.Contain("Denied").Or.Contain("inject_link_down"));

        var changes = result.DecisionLog.CommsStateChanges.OrderBy(c => c.SimTick).ToList();
        Assert.That(changes.Count, Is.GreaterThanOrEqualTo(1), "must emit at least one CommsStateChange from comms[]");
        Assert.That(
            changes.Any(c => c.NewState == CommsState.Degraded && c.Reason.Contains("inject", StringComparison.OrdinalIgnoreCase)),
            Is.True,
            "mid-run inject must set CommsState.Degraded with inject reason");
        Assert.That(changes[0].SimTick, Is.GreaterThanOrEqualTo(3ul), "degrade inject is scheduled at tick 3+");

        // Contact-triggered ROE rebind leaves PolicyUpdate in the log (ApplyRoeToUnits).
        Assert.That(
            result.Fingerprint,
            Does.Contain("PolicyUpdate"),
            "contact mission trigger must ApplyRoe and emit PolicyUpdate");
        Assert.That(result.DecisionLog.ChronologicalEntries()
            .Any(e => e.Kind == OrderLogEntryKind.PolicyUpdate
                      && e.Payload is PolicyUpdateRecord p
                      && p.Field == "roe"),
            Is.True,
            "PolicyUpdate field=roe required from mission.triggers ApplyRoeToUnits");

        // Combat still on catalog path
        Assert.That(result.Fingerprint, Does.Contain("k-31-visby-2009").And.Contain("Engagement"));
        Assert.That(result.Fingerprint, Does.Contain("em-sovremenny-i-pr-956-sarych"));
    }

    [Test]
    public void Theater_dynamic_victory_dual_kill_path()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        Assert.That(ScenarioPolicyRepository.TryGet("gauntlet-theater-dynamic-victory"), Is.Not.Null);

        var result = BalticReplayHarness.Run(42, "gauntlet-theater-dynamic-victory", ticks: 12, mvpEngagement: true);
        Assert.That(result.Fingerprint, Does.Contain("dynamic_victory_require_dual_kill"));

        var csv = LossesScoringCsvExporter.FormatRow(
            "gauntlet-theater-dynamic-victory",
            42,
            "BLUE",
            result.DecisionLog);
        var kills = int.Parse(csv.Split(',')[4], System.Globalization.CultureInfo.InvariantCulture);
        Assert.That(kills, Is.GreaterThanOrEqualTo(2), $"dynamic victory needs dual kills; csv={csv}");
    }
}
