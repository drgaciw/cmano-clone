using NUnit.Framework;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.C2Network;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

[TestFixture]
public sealed class C2NetworkHealthBridgeTests
{
    [Test]
    public void Build_projects_healthy_mesh_for_alive_oob_units()
    {
        var log = new DecisionLog();
        var oob = new[]
        {
            new OobTreeEntry("u1", true),
            new OobTreeEntry("u2", true),
            new OobTreeEntry("hostile-1", false),
        };

        var snapshot = C2NetworkHealthBridge.Build(
            log,
            oob,
            InMemoryCatalogReader.BalticPatrolFixture(),
            currentSimTick: 1);

        Assert.That(snapshot.NetworkHealth, Is.EqualTo(C2NetworkHealthLevel.Healthy));
        Assert.That(snapshot.Links, Is.Not.Empty);
    }

    [Test]
    public void Build_null_log_throws()
    {
        try
        {
            C2NetworkHealthBridge.Build(null!, Array.Empty<OobTreeEntry>(), null, 0);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException ex)
        {
            Assert.That(ex.ParamName, Is.EqualTo("log"));
        }
    }
}
