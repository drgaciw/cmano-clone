namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>
/// S33-07 isolated datalink + comms fixture — excluded from ReplayGolden 6/6 catalog.
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessDatalinkCommsTests
{
    private const string PolicyId = "baltic-patrol-datalink-comms";
    private const int Seed = 42;
    private const int Ticks = 6;

    private static string GoldenPath =>
        ReplayGoldenAssertions.ResolveRegressionGoldenPath("replay-golden-baltic-datalink-comms-2026-06-19.txt");

    [Test]
    public void Policy_loads_datalink_sharing_and_comms_transitions()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet(PolicyId);

        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.DatalinkDoctrine.IsSharingEnabled, Is.True);
        Assert.That(profile.DatalinkDoctrine.OrganicOnly, Is.False);
        Assert.That(profile.DatalinkDoctrine.ShareLagTicks, Is.EqualTo(0));
        Assert.That(profile.DatalinkDoctrine.ResolveSide("u1"), Is.EqualTo("blue"));
        Assert.That(profile.DatalinkDoctrine.ResolveSide("u2"), Is.EqualTo("blue"));
        Assert.That(profile.CommsTransitions, Has.Count.EqualTo(2));
        Assert.That(profile.CommsTransitions[0].AtTick, Is.EqualTo(2UL));
        Assert.That(profile.CommsTransitions[0].NewState, Is.EqualTo("Degraded"));
        Assert.That(profile.CommsTransitions[1].AtTick, Is.EqualTo(4UL));
        Assert.That(profile.CommsTransitions[1].NewState, Is.EqualTo("Denied"));
    }

    [Test]
    public void Policy_is_not_in_replay_golden_regression_catalog()
    {
        var catalogPolicyIds = ReplayGoldenRegressionCatalog.All.Select(c => c.PolicyId).ToArray();

        Assert.That(catalogPolicyIds, Does.Not.Contain(PolicyId));
        Assert.That(catalogPolicyIds, Has.Length.EqualTo(6));
    }

    [Test]
    public void Datalink_comms_fixture_replay_is_deterministic()
    {
        var a = BalticReplayHarness.Run(Seed, PolicyId, Ticks);
        var b = BalticReplayHarness.Run(Seed, PolicyId, Ticks);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
        Assert.That(b.FingerprintSha256, Is.EqualTo(a.FingerprintSha256));
    }

    [Test]
    public void Comms_state_changes_appear_at_expected_ticks()
    {
        var result = BalticReplayHarness.Run(Seed, PolicyId, Ticks);
        var changes = result.DecisionLog.CommsStateChanges;

        Assert.That(changes, Has.Count.EqualTo(2));
        Assert.That(changes[0].SimTick, Is.EqualTo(2));
        Assert.That(changes[0].PreviousState, Is.EqualTo(CommsState.Nominal));
        Assert.That(changes[0].NewState, Is.EqualTo(CommsState.Degraded));
        Assert.That(changes[0].Reason, Is.EqualTo("jamming"));
        Assert.That(changes[1].SimTick, Is.EqualTo(4));
        Assert.That(changes[1].PreviousState, Is.EqualTo(CommsState.Degraded));
        Assert.That(changes[1].NewState, Is.EqualTo(CommsState.Denied));
        Assert.That(changes[1].Reason, Is.EqualTo("link-down"));

        Assert.That(result.Fingerprint, Does.Contain("CommsStateChange"));
        Assert.That(result.Fingerprint, Does.Contain("|2|2|brigade-net|Nominal|Degraded|jamming"));
        Assert.That(result.Fingerprint, Does.Contain("|4|4|brigade-net|Degraded|Denied|link-down"));
    }

    [Test]
    public void Peer_datalink_share_occurs_under_Nominal_before_comms_degrade()
    {
        var result = BalticReplayHarness.Run(Seed, PolicyId, Ticks);

        Assert.That(result.Fingerprint, Does.Contain("ContactChange|3|1|1|u1|c1|hostile-1|Unknown|Detected"));
        Assert.That(result.Fingerprint, Does.Contain("|u2|dl-hostile-1|hostile-1|Unknown|Detected"));
        Assert.That(result.Fingerprint, Does.Not.Contain("|u2|dl-hostile-1|hostile-1|Unknown|Detected|2|"));
    }

    [Test]
    public void Existing_peer_share_updates_continue_under_Degraded_after_transition()
    {
        var result = BalticReplayHarness.Run(Seed, PolicyId, Ticks, mvpEngagement: false);

        Assert.That(result.Fingerprint, Does.Contain("|u2|dl-hostile-1|hostile-1|Detected|Classified"));
        Assert.That(result.Fingerprint, Does.Contain("|u2|dl-hostile-1|hostile-1|Classified|Identified"));
    }

    [Test]
    public void Comms_denied_blocks_engage_after_tick_four()
    {
        var result = BalticReplayHarness.Run(Seed, PolicyId, Ticks);

        Assert.That(result.Fingerprint, Does.Contain("PolicyDenial"));
        Assert.That(result.Fingerprint, Does.Contain("CommsDenied"));
        Assert.That(result.Messages.Count(m => m.Category == "COMMS"), Is.EqualTo(2));
    }

    [Test]
    public void Pinned_isolated_golden_hashes_match_harness()
    {
        var result = BalticReplayHarness.Run(Seed, PolicyId, Ticks);
        ReplayGoldenAssertions.AssertPinnedHashes(result, File.ReadAllLines(GoldenPath));
    }
}