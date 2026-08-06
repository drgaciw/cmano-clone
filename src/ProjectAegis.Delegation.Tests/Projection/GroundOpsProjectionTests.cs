using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class GroundOpsProjectionTests
{
    [Test]
    public void Empty_input()
    {
        Assert.That(GroundOpsProjection.Project(null), Is.Empty);
        Assert.That(GroundOpsProjection.Aggregate(null).FormationCount, Is.EqualTo(0));
    }

    [Test]
    public void Brigade_leaf_no_battalion_expander()
    {
        var entries = GroundOpsProjection.Project(new[]
        {
            new GroundFormationInput(
                "bde-1",
                "1st BDE",
                "BLUE",
                "Battalion",
                "Grid 12",
                0.9,
                AdaAssetCount: 4,
                AdaOnlineCount: 3,
                FacilityKind: "Airbase",
                FacilityDamageFraction: 0.0),
        });

        Assert.That(entries.Count, Is.EqualTo(1));
        Assert.That(entries[0].IsLeaf, Is.True);
        Assert.That(entries[0].EchelonLabel, Does.Contain("Brigade+"));
        Assert.That(entries[0].StrengthBand, Does.Contain("GREEN"));
        Assert.That(entries[0].AdaContributionLine, Does.Contain("3/4"));
        Assert.That(entries[0].FacilityStatusLine, Does.Contain("OPEN"));
    }

    [Test]
    public void Aggregate_summary()
    {
        var entries = GroundOpsProjection.Project(new[]
        {
            new GroundFormationInput("a", "A", "BLUE", "Brigade", "X", 1, 0, 0, null, 0),
            new GroundFormationInput("b", "B", "BLUE", "Division", "Y", 0.4, 2, 1, "Port", 0.6),
        });
        var agg = GroundOpsProjection.Aggregate(entries);
        Assert.That(agg.FormationCount, Is.EqualTo(2));
        Assert.That(agg.SummaryLine, Does.Contain("2"));
    }
}
