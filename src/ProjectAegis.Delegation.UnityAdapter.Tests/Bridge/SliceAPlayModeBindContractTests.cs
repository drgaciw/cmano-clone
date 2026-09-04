namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using NUnit.Framework;

/// <summary>
/// S122 P0: Session NRE guard, smoke contact select, live comms quality, stacked right rail.
/// Headless file contracts — no Editor required.
/// </summary>
[TestFixture]
public sealed class SliceAPlayModeBindContractTests
{
    [Test]
    public void Delegation_bridge_host_session_is_null_safe_when_bridge_uninitialized()
    {
        var host = ReadRuntime("DelegationBridgeHost.cs");
        Assert.That(host, Does.Contain("Session => Bridge?.Session"));
        Assert.That(host, Does.Not.Contain("Session => Bridge.Session;"));
    }

    [Test]
    public void Contact_detail_host_passes_out_of_comms_from_last_comms_state()
    {
        var host = ReadRuntime("ContactDetailPanelHost.cs");
        Assert.That(host, Does.Contain("LastCommsState"));
        Assert.That(host, Does.Contain("outOfComms"));
        Assert.That(host, Does.Contain("OutOfCommsFromNetwork"));
        Assert.That(host, Does.Not.Contain("DelegationBridge.Tick"));
    }

    [Test]
    public void Smoke_host_selects_seeded_contact_after_orbat()
    {
        var host = ReadRuntime("SimplePlayModeSimHost.cs");
        Assert.That(host, Does.Contain("SelectContact(SmokeContactId)"));
        Assert.That(host, Does.Contain("SelectUnit(SmokeFriendlyUnitId)"));
    }

    [Test]
    public void Contact_and_chain_uss_share_right_rail_not_map_overlap()
    {
        var repo = FindRepoRoot();
        Assert.That(repo, Is.Not.Null);
        var contactUss = File.ReadAllText(Path.Combine(
            repo!, "unity", "ProjectAegis", "Assets", "UI", "ContactDetail", "ContactDetailPanel.uss"));
        var chainUss = File.ReadAllText(Path.Combine(
            repo!, "unity", "ProjectAegis", "Assets", "UI", "SensorToShooter", "SensorToShooterPanel.uss"));
        Assert.That(contactUss, Does.Contain("right: 0"));
        Assert.That(chainUss, Does.Contain("right: 0"));
        Assert.That(chainUss, Does.Not.Contain("right: 304px"));
        Assert.That(contactUss, Does.Contain("bottom: 48%"));
        Assert.That(chainUss, Does.Contain("top: 52%"));
    }

    private static string ReadRuntime(string fileName)
    {
        var repo = FindRepoRoot();
        Assert.That(repo, Is.Not.Null);
        var path = Path.Combine(
            repo!, "unity", "ProjectAegis", "Assets", "Scripts", "Runtime", fileName);
        Assert.That(File.Exists(path), Is.True, path);
        return File.ReadAllText(path);
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return null;
    }
}
