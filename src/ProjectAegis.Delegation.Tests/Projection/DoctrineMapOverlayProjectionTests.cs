using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class DoctrineMapOverlayProjectionTests
{
    [Test]
    public void Project_empty_inheritance_returns_empty()
    {
        Assert.That(
            DoctrineMapOverlayProjection.Project(Array.Empty<DoctrineInheritanceEntry>()),
            Is.Empty);
    }

    [Test]
    public void Project_maps_roe_and_source_without_positions()
    {
        var policy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            missionRoe: new EffectivePolicy(RoeLevel.WeaponsTight, MaxSalvo: 2),
            missionUnitIds: ["u1"]);
        var inheritance = DoctrineInheritanceProjection.ProjectAllUnits(
            [new TargetId("u2"), new TargetId("u1")],
            policy,
            isFriendly: true);

        var overlay = DoctrineMapOverlayProjection.Project(inheritance);

        Assert.That(overlay, Has.Count.EqualTo(2));
        // Deterministic UnitId order
        Assert.That(overlay[0].UnitId, Is.EqualTo("u1"));
        Assert.That(overlay[0].RoeLabel, Is.EqualTo("ROE: WeaponsTight"));
        Assert.That(overlay[0].SourceLabel, Is.EqualTo("SOURCE: Mission"));
        Assert.That(overlay[0].NormalizedX, Is.Null);
        Assert.That(overlay[0].NormalizedY, Is.Null);

        Assert.That(overlay[1].UnitId, Is.EqualTo("u2"));
        Assert.That(overlay[1].RoeLabel, Is.EqualTo("ROE: WeaponsFree"));
        Assert.That(overlay[1].SourceLabel, Is.EqualTo("SOURCE: Scenario Default"));
    }

    [Test]
    public void Project_fills_positions_from_matching_map_symbols()
    {
        var inheritance = new[]
        {
            new DoctrineInheritanceEntry(
                "u1",
                "ROE: HoldFire",
                "SALVO: 1",
                "EMCON: ACTIVE",
                "SOURCE: Unit Override",
                false,
                true,
                "OVERRIDE: ACTIVE"),
            new DoctrineInheritanceEntry(
                "u2",
                "ROE: WeaponsFree",
                "SALVO: 4",
                "EMCON: ACTIVE",
                "SOURCE: Scenario Default",
                false,
                false,
                "OVERRIDE: NONE"),
        };
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "F", "Blue", 0.25f, 0.75f, false),
            new MapSymbolEntry("h1", "Hostile", "H", "Red", 0.9f, 0.1f, false),
        };

        var overlay = DoctrineMapOverlayProjection.Project(inheritance, symbols);

        Assert.That(overlay, Has.Count.EqualTo(2));
        Assert.That(overlay[0].UnitId, Is.EqualTo("u1"));
        Assert.That(overlay[0].NormalizedX, Is.EqualTo(0.25f));
        Assert.That(overlay[0].NormalizedY, Is.EqualTo(0.75f));
        Assert.That(overlay[0].RoeLabel, Is.EqualTo("ROE: HoldFire"));
        Assert.That(overlay[0].SourceLabel, Is.EqualTo("SOURCE: Unit Override"));

        // u2 has no matching symbol
        Assert.That(overlay[1].UnitId, Is.EqualTo("u2"));
        Assert.That(overlay[1].NormalizedX, Is.Null);
        Assert.That(overlay[1].NormalizedY, Is.Null);
    }

    [Test]
    public void Project_null_inheritance_throws()
    {
        try
        {
            DoctrineMapOverlayProjection.Project(null!);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException ex)
        {
            Assert.That(ex.ParamName, Is.EqualTo("inheritanceEntries"));
        }
    }

    [Test]
    public void Project_null_policy_placeholders_still_overlay()
    {
        var inheritance = DoctrineInheritanceProjection.ProjectAllUnits(
            [new TargetId("solo")],
            policy: null,
            isFriendly: true);
        var overlay = DoctrineMapOverlayProjection.Project(inheritance);
        Assert.That(overlay, Has.Count.EqualTo(1));
        Assert.That(overlay[0].RoeLabel, Is.EqualTo("ROE: —"));
        Assert.That(overlay[0].SourceLabel, Is.EqualTo("SOURCE: —"));
    }

    [Test]
    public void MapPanelApplyState_counts_doctrine_overlay()
    {
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "F", "Blue", 0.2f, 0.3f, false),
        };
        var doctrine = new[]
        {
            new DoctrineMapOverlayEntry("u1", "ROE: WeaponsFree", "SOURCE: Scenario Default", 0.2f, 0.3f),
            new DoctrineMapOverlayEntry("u2", "ROE: HoldFire", "SOURCE: Unit Override"),
        };

        var applied = MapPanelApplyState.BindAndApply(
            symbols,
            "BALTIC",
            selectedUnitId: "u1",
            selectedContactId: null,
            envelopeRings: null,
            datalinkEdges: null,
            doctrineOverlay: doctrine);

        Assert.That(applied.DoctrineOverlayCount, Is.EqualTo(2));
        Assert.That(applied.EnvelopeRingCount, Is.EqualTo(0));
        Assert.That(applied.DatalinkEdgeCount, Is.EqualTo(0));
    }
}
