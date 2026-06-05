using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

public sealed class BalticReplayHarnessCommsTests
{
    [Test]
    public void Comms_scenario_logs_denied_and_blocks_repeat_fingerprint()
    {
        var a = BalticReplayHarness.Run(7, "baltic-patrol-comms", ticks: 6, mvpEngagement: true);
        var b = BalticReplayHarness.Run(7, "baltic-patrol-comms", ticks: 6, mvpEngagement: true);
        Assert.That(a.Fingerprint, Is.EqualTo(b.Fingerprint));
        Assert.That(a.Fingerprint, Does.Contain("CommsStateChange"));
        Assert.That(a.Messages.Count(m => m.Category == "COMMS"), Is.EqualTo(2));
    }

    [Test]
    public void Comms_denied_appends_policy_denial_for_engage()
    {
        var result = BalticReplayHarness.Run(7, "baltic-patrol-comms", ticks: 6, mvpEngagement: true);
        Assert.That(result.Fingerprint, Does.Contain("PolicyDenial"));
        Assert.That(result.Fingerprint, Does.Contain("CommsDenied").Or.Contains("RoeHoldFire"));
    }

    [Test]
    public void Comms_scenario_policy_exposes_logistics_and_ghost_map_rows()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet("baltic-patrol-comms");
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.Logistics.JokerSimSeconds, Is.EqualTo(90));

        var symbols = new[]
        {
            new MapSymbolEntry("c1", "Hostile", "◆", "c1", 0.5f, 0.5f, false),
        };
        var map = MapPanelBinder.Bind(symbols, "baltic-patrol-comms", null, null, CommsState.Degraded, profile.CommsDisplay);
        Assert.That(map.Symbols.Any(s => s.IsGhost), Is.True);
    }
}