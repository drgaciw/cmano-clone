namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class BalticReplayHarnessV3UcavTests
{
    [Test]
    public void Baltic_v3_patrol_escalates_roe_and_missions_on_recon_contact()
    {
        var result = BalticReplayHarness.Run(42, "baltic-v3-patrol", ticks: 4);

        Assert.That(result.Fingerprint, Does.Contain("blue-asuw-surface|ASuW"));
        Assert.That(result.Fingerprint, Does.Contain("blue-aaa-air|AAA"));
        Assert.That(result.Fingerprint, Does.Contain("red-asuw-surface|ASuW"));
        Assert.That(result.Fingerprint, Does.Contain("red-aaa-air|AAA"));
        Assert.That(result.Fingerprint, Does.Contain("PolicyUpdate|"));
        Assert.That(result.Fingerprint, Does.Contain("|WeaponsFree"));
    }

    [Test]
    public void Baltic_v3_patrol_includes_friendly_ucav_recon_contact_changes()
    {
        var result = BalticReplayHarness.Run(42, "baltic-v3-patrol", ticks: 4);

        Assert.That(result.Fingerprint, Does.Contain("|ucav-blue|c-ucav-1|hostile-1|"));
        Assert.That(result.Fingerprint, Does.Contain("|ucav-blue|c-ucav-2|hostile-1|"));
    }

    [Test]
    public void Baltic_v3_patrol_includes_opfor_ucav_passive_and_active_recon_contacts()
    {
        var result = BalticReplayHarness.Run(42, "baltic-v3-patrol", ticks: 4);

        Assert.That(result.Fingerprint, Does.Contain("|ucav-red|c-ucav-red-1|u1|"));
        Assert.That(result.Fingerprint, Does.Contain("|ucav-red|c-ucav-red-2|u1|"));
    }

    [Test]
    public void Baltic_v3_patrol_logs_recon_mission_events_on_both_sides()
    {
        var result = BalticReplayHarness.Run(42, "baltic-v3-patrol", ticks: 4);

        Assert.That(result.Fingerprint, Does.Contain("recon_passive_start"));
        Assert.That(result.Fingerprint, Does.Contain("recon_active_start"));
    }

    [Test]
    public void Baltic_v3_patrol_does_not_reengage_friendly_after_hostile_kill()
    {
        var result = BalticReplayHarness.Run(42, "baltic-v3-patrol", ticks: 4);

        Assert.That(result.Fingerprint, Does.Not.Contain("|Kill|u1|"));
        Assert.That(result.EngagementCount, Is.GreaterThan(0));
    }

    [Test]
    public void Baltic_v3_catalog_includes_ucav_recon_loadouts_on_both_sides()
    {
        var catalog = InMemoryCatalogReader.BalticV3Fixture();
        Assert.That(catalog.GetSortedLoadouts(), Has.Some.Matches<CatalogLoadout>(l =>
            l.PlatformId == "ucav-blue" && l.LoadoutName == "Recon [Internal IR]"));
        Assert.That(catalog.GetSortedLoadouts(), Has.Some.Matches<CatalogLoadout>(l =>
            l.PlatformId == "ucav-red" && l.LoadoutName == "Recon [Internal IR]"));
        Assert.That(catalog.TryGetPlatformPosition("ucav-red", out _, out _), Is.True);
        Assert.That(catalog.TryGetBasePd("ucav-blue", "recon-radar", out _), Is.True);
        Assert.That(catalog.TryGetBasePd("ucav-red", "recon-radar", out _), Is.True);
    }

    [Test]
    public void Baltic_v3_patrol_is_deterministic_with_ucav_oob()
    {
        var a = BalticReplayHarness.Run(42, "baltic-v3-patrol", ticks: 4);
        var b = BalticReplayHarness.Run(42, "baltic-v3-patrol", ticks: 4);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
    }
}
