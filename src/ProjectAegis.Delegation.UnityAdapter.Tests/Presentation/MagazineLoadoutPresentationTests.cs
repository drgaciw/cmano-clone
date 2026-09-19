using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class MagazineLoadoutPresentationTests
{
    [Test]
    public void Build_without_magazine_data_is_honest_NO_MAGAZINE_DATA()
    {
        var presentation = MagazineLoadoutPresenter.Build(entries: null, hasMagazineData: false);

        Assert.That(presentation, Is.SameAs(MagazineLoadoutPresentation.NoMagazineData));
        Assert.That(presentation.HasMagazineData, Is.False);
        Assert.That(presentation.EmptyStateLine, Is.EqualTo("NO MAGAZINE DATA"));
    }

    [Test]
    public void Binder_exposes_positional_remaining_capacity_and_fill_pct()
    {
        var entries = MagazineLoadoutProjection.Project([
            ("cvn-1", "CVN", "vls", "aim120", "AIM-120", 6, 8),
        ]);
        var presentation = MagazineLoadoutPresenter.Build(entries, hasMagazineData: true);
        var labels = MagazineLoadoutPanelBinder.Bind(presentation, roundsPerAirframe: 6);

        Assert.That(labels.Rows, Has.Count.EqualTo(1));
        Assert.That(labels.Rows[0].Remaining, Is.EqualTo(6));
        Assert.That(labels.Rows[0].Capacity, Is.EqualTo(8));
        Assert.That(labels.Rows[0].FillPct, Is.EqualTo(75.0).Within(0.01));
        Assert.That(labels.Rows[0].StatusLine, Is.EqualTo(MagazineLoadoutProjection.StatusOk));
        Assert.That(labels.Rows[0].DisplayLine, Does.Contain("6/8"));
        Assert.That(labels.Rows[0].DisplayLine, Does.Contain("(75%)"));
        Assert.That(labels.HeaderLine, Does.StartWith("MAGAZINE  ·  "));
        Assert.That(labels.FeasibilityLine, Does.Contain("ARMABLE AIRFRAMES  1"));
    }

    [Test]
    public void Binder_formats_feasibility_from_aggregate_stock()
    {
        var entries = MagazineLoadoutProjection.Project([
            ("u1", "p", "m1", "w1", "W1", 10, 12),
            ("u1", "p", "m2", "w2", "W2", 8, 12),
        ]);
        var labels = MagazineLoadoutPanelBinder.Bind(
            MagazineLoadoutPresenter.Build(entries, hasMagazineData: true),
            roundsPerAirframe: 6);

        Assert.That(labels.FeasibilityLine, Does.Contain("ARMABLE AIRFRAMES  3"));
        Assert.That(labels.FeasibilityLine, Does.Contain("stock=18"));
    }

    [Test]
    public void Fingerprint_is_stable_for_identical_entries()
    {
        var entries = MagazineLoadoutProjection.Project([
            ("u1", "p", "vls", "w", "AIM-120", 4, 8),
        ]);

        var first = MagazineLoadoutPresenter.ComputeFingerprint(entries, hasMagazineData: true);
        var second = MagazineLoadoutPresenter.ComputeFingerprint(entries, hasMagazineData: true);

        Assert.That(second, Is.EqualTo(first));
        Assert.That(first, Does.StartWith("ml:r=1|"));
        Assert.That(first, Does.Contain("4,8,50"));
    }

    [Test]
    public void Fingerprint_changes_when_remaining_changes()
    {
        var before = MagazineLoadoutProjection.Project([
            ("u1", "p", "vls", "w", "AIM-120", 4, 8),
        ]);
        var after = MagazineLoadoutProjection.Project([
            ("u1", "p", "vls", "w", "AIM-120", 3, 8),
        ]);

        Assert.That(
            MagazineLoadoutPresenter.ComputeFingerprint(after, hasMagazineData: true),
            Is.Not.EqualTo(MagazineLoadoutPresenter.ComputeFingerprint(before, hasMagazineData: true)));
    }

    [TestCase(false, "ml:no-data")]
    [TestCase(true, "ml:empty")]
    public void Fingerprint_reports_missing_and_empty_feeds(bool hasData, string expected)
    {
        Assert.That(
            MagazineLoadoutPresenter.ComputeFingerprint(Array.Empty<MagazineLoadoutEntry>(), hasData),
            Is.EqualTo(expected));
    }

    [Test]
    public void Presentation_fingerprint_matches_entry_fingerprint()
    {
        var entries = MagazineLoadoutProjection.Project([
            ("u1", "p", "vls", "w", "AIM-120", 2, 4),
        ]);
        var presentation = MagazineLoadoutPresenter.Build(entries, hasMagazineData: true);

        Assert.That(
            MagazineLoadoutPresenter.ComputeFingerprint(presentation),
            Is.EqualTo(MagazineLoadoutPresenter.ComputeFingerprint(entries, hasMagazineData: true)));
    }
}
