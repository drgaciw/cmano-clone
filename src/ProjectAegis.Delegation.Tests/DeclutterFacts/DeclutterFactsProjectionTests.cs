using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.DeclutterFacts;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.DeclutterFacts;

[TestFixture]
public sealed class DeclutterFactsProjectionTests
{
    [Test]
    public void Single_engagement_row_emits_count_one_with_family_and_zoom_band_token()
    {
        var snapshot = DeclutterFactsProjection.Project(new DeclutterFactsEngagementFacts(
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            ZoomBandToken: DeclutterFactsZoomBand.Tactical));

        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));

        var row = snapshot.Rows[0];
        Assert.That(row.WeaponFamilyId, Is.EqualTo(CatalogWeaponIds.MvpDefault));
        Assert.That(row.Count, Is.EqualTo(1));
        Assert.That(row.ZoomBandToken, Is.EqualTo(DeclutterFactsZoomBand.Tactical));
        Assert.That(row.IsFireOrder, Is.False);
    }

    [Test]
    public void Aggregated_salvo_same_family_collapses_to_count_greater_than_one()
    {
        var snapshot = DeclutterFactsProjection.Project(new[]
        {
            new DeclutterFactsEngagementFacts(CatalogWeaponIds.MvpDefault, DeclutterFactsZoomBand.Tactical),
            new DeclutterFactsEngagementFacts(CatalogWeaponIds.MvpDefault, DeclutterFactsZoomBand.Tactical),
            new DeclutterFactsEngagementFacts(CatalogWeaponIds.MvpDefault, DeclutterFactsZoomBand.Tactical),
        });

        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));
        Assert.That(snapshot.Rows[0].Count, Is.EqualTo(3));
        Assert.That(snapshot.Rows[0].WeaponFamilyId, Is.EqualTo(CatalogWeaponIds.MvpDefault));
        Assert.That(snapshot.Rows[0].ZoomBandToken, Is.EqualTo(DeclutterFactsZoomBand.Tactical));
        Assert.That(snapshot.Rows[0].IsFireOrder, Is.False);
    }

    [Test]
    public void Round_count_on_input_row_contributes_to_aggregated_salvo_total()
    {
        var snapshot = DeclutterFactsProjection.Project(new[]
        {
            new DeclutterFactsEngagementFacts(CatalogWeaponIds.MvpDefault, DeclutterFactsZoomBand.Operational, RoundCount: 2),
            new DeclutterFactsEngagementFacts(CatalogWeaponIds.MvpDefault, DeclutterFactsZoomBand.Operational),
        });

        Assert.That(snapshot.Rows[0].Count, Is.EqualTo(3));
        Assert.That(snapshot.Rows[0].ZoomBandToken, Is.EqualTo(DeclutterFactsZoomBand.Operational));
    }

    [Test]
    public void IsFireOrder_is_false_on_snapshot_and_rows()
    {
        var snapshot = DeclutterFactsProjection.Project(new[]
        {
            new DeclutterFactsEngagementFacts("sam", DeclutterFactsZoomBand.Tactical),
            new DeclutterFactsEngagementFacts("asm", DeclutterFactsZoomBand.Operational, RoundCount: 4),
        });

        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.Rows.All(r => r.IsFireOrder == false), Is.True);
    }

    [Test]
    public void Same_weapon_family_in_different_zoom_bands_emits_separate_rows()
    {
        var snapshot = DeclutterFactsProjection.Project(new[]
        {
            new DeclutterFactsEngagementFacts("sam", DeclutterFactsZoomBand.Tactical),
            new DeclutterFactsEngagementFacts("sam", DeclutterFactsZoomBand.Operational, RoundCount: 2),
        });

        Assert.That(snapshot.Rows.Select(r => (r.WeaponFamilyId, r.ZoomBandToken, r.Count)), Is.EqualTo(new[]
        {
            ("sam", DeclutterFactsZoomBand.Operational, 2),
            ("sam", DeclutterFactsZoomBand.Tactical, 1),
        }));
    }

    [Test]
    public void Rows_sort_by_weapon_family_then_zoom_band()
    {
        var snapshot = DeclutterFactsProjection.Project(new[]
        {
            new DeclutterFactsEngagementFacts("sam", DeclutterFactsZoomBand.Tactical),
            new DeclutterFactsEngagementFacts("asm", DeclutterFactsZoomBand.Operational),
            new DeclutterFactsEngagementFacts("asm", DeclutterFactsZoomBand.Tactical),
        });

        Assert.That(snapshot.Rows.Select(r => (r.WeaponFamilyId, r.ZoomBandToken)), Is.EqualTo(new[]
        {
            ("asm", DeclutterFactsZoomBand.Operational),
            ("asm", DeclutterFactsZoomBand.Tactical),
            ("sam", DeclutterFactsZoomBand.Tactical),
        }));
    }

    [Test]
    public void Fingerprint_is_identical_for_identical_inputs()
    {
        var engagements = new[]
        {
            new DeclutterFactsEngagementFacts(CatalogWeaponIds.MvpDefault, DeclutterFactsZoomBand.Tactical),
            new DeclutterFactsEngagementFacts(CatalogWeaponIds.MvpDefault, DeclutterFactsZoomBand.Tactical),
            new DeclutterFactsEngagementFacts("sam", DeclutterFactsZoomBand.Operational, RoundCount: 2),
        };

        var a = DeclutterFactsProjection.Project(engagements);
        var b = DeclutterFactsProjection.Project(engagements);

        Assert.That(DeclutterFactsFingerprint.Compute(a), Is.EqualTo(DeclutterFactsFingerprint.Compute(b)));
    }

    [Test]
    public void Null_or_empty_input_returns_empty_snapshot()
    {
        Assert.That(DeclutterFactsProjection.Project((IReadOnlyList<DeclutterFactsEngagementFacts>?)null).Rows, Is.Empty);
        Assert.That(DeclutterFactsProjection.Project(Array.Empty<DeclutterFactsEngagementFacts>()).Rows, Is.Empty);
        Assert.That(DeclutterFactsFingerprint.Compute(DeclutterFactsSnapshot.Empty), Is.EqualTo("df:empty"));
    }

    [Test]
    public void Invalid_rows_are_skipped_without_affecting_valid_aggregation()
    {
        var snapshot = DeclutterFactsProjection.Project(new[]
        {
            new DeclutterFactsEngagementFacts(" ", DeclutterFactsZoomBand.Tactical),
            new DeclutterFactsEngagementFacts("sam", " "),
            new DeclutterFactsEngagementFacts("sam", DeclutterFactsZoomBand.Tactical),
        });

        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));
        Assert.That(snapshot.Rows[0].Count, Is.EqualTo(1));
    }

    [Test]
    public void Dtos_omit_ui_derived_truth_fields()
    {
        var types = new[]
        {
            typeof(DeclutterFactsEngagementFacts),
            typeof(DeclutterFactsRow),
            typeof(DeclutterFactsSnapshot),
        };
        string[] forbidden = ["selection", "hover", "camera", "visible", "visibility", "selected"];

        foreach (var type in types)
        {
            foreach (var property in type.GetProperties())
            {
                var name = property.Name.ToLowerInvariant();
                Assert.That(
                    forbidden.Any(token => name.Contains(token, StringComparison.Ordinal)),
                    Is.False,
                    $"{type.Name}.{property.Name} is UI-derived truth");
            }
        }
    }
}
