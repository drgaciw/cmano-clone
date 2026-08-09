using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class MagazineLoadoutProjectionTests
{
    [Test]
    public void Project_maps_remaining_capacity_fill_and_status()
    {
        var rows = new (string, string, string, string, string?, int, int)[]
        {
            ("u1", "plat-a", "vls-fwd", "w-aim", "AIM-120", 8, 10),
            ("u1", "plat-a", "wing", "w-agm", "AGM-88", 1, 8),
            ("u2", "plat-b", "bay", "w-jd", null, 0, 4),
        };

        var entries = MagazineLoadoutProjection.Project(rows);

        Assert.That(entries, Has.Count.EqualTo(3));
        Assert.That(entries[0].UnitId, Is.EqualTo("u1"));
        Assert.That(entries[0].MountId, Is.EqualTo("vls-fwd"));
        Assert.That(entries[0].WeaponLabel, Is.EqualTo("AIM-120"));
        Assert.That(entries[0].Remaining, Is.EqualTo(8));
        Assert.That(entries[0].Capacity, Is.EqualTo(10));
        Assert.That(entries[0].FillPct, Is.EqualTo(80.0).Within(0.01));
        Assert.That(entries[0].StatusLine, Is.EqualTo(MagazineLoadoutProjection.StatusOk));

        var low = entries.Single(e => e.MountId == "wing");
        Assert.That(low.FillPct, Is.EqualTo(12.5).Within(0.01));
        Assert.That(low.StatusLine, Is.EqualTo(MagazineLoadoutProjection.StatusLow));

        var empty = entries.Single(e => e.UnitId == "u2");
        Assert.That(empty.StatusLine, Is.EqualTo(MagazineLoadoutProjection.StatusEmpty));
        Assert.That(empty.WeaponLabel, Is.EqualTo("w-jd"));
        Assert.That(empty.FillPct, Is.EqualTo(0.0));
    }

    [Test]
    public void Project_orders_by_unit_mount_weapon_and_fills_missing_labels()
    {
        var rows = new (string, string, string, string, string?, int, int)[]
        {
            ("b", "  ", "m2", "w2", "  ", 2, 2),
            ("a", "p", "m1", "w1", "Label", 2, 2),
        };

        var entries = MagazineLoadoutProjection.Project(rows);

        Assert.That(entries.Select(e => e.UnitId), Is.EqualTo(new[] { "a", "b" }));
        Assert.That(entries[1].PlatformId, Is.EqualTo(MagazineLoadoutProjection.MissingLabel));
        Assert.That(entries[1].WeaponLabel, Is.EqualTo("w2"));
    }

    [Test]
    public void Project_empty_or_null_returns_empty()
    {
        Assert.That(MagazineLoadoutProjection.Project(null), Is.Empty);
        Assert.That(MagazineLoadoutProjection.Project(
            Array.Empty<(string, string, string, string, string?, int, int)>()), Is.Empty);
    }

    [Test]
    public void Project_skips_blank_unit_ids()
    {
        var rows = new (string, string, string, string, string?, int, int)[]
        {
            ("", "p", "m", "w", "L", 1, 1),
            ("  ", "p", "m", "w", "L", 1, 1),
            ("ok", "p", "m", "w", "L", 1, 1),
        };

        var entries = MagazineLoadoutProjection.Project(rows);
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].UnitId, Is.EqualTo("ok"));
    }

    [Test]
    public void HowManyAirframesCanArm_is_integer_division_of_stock_by_cost()
    {
        Assert.That(MagazineLoadoutProjection.HowManyAirframesCanArm(24, 6), Is.EqualTo(4));
        Assert.That(MagazineLoadoutProjection.HowManyAirframesCanArm(25, 6), Is.EqualTo(4));
        Assert.That(MagazineLoadoutProjection.HowManyAirframesCanArm(5, 6), Is.EqualTo(0));
        Assert.That(MagazineLoadoutProjection.HowManyAirframesCanArm(0, 6), Is.EqualTo(0));
        Assert.That(MagazineLoadoutProjection.HowManyAirframesCanArm(12, 0), Is.EqualTo(0));
        Assert.That(MagazineLoadoutProjection.HowManyAirframesCanArm(-3, 4), Is.EqualTo(0));
        Assert.That(MagazineLoadoutProjection.HowManyAirframesCanArm(10, -1), Is.EqualTo(0));
    }

    [Test]
    public void ComputeFillPct_and_ResolveStatus_edges()
    {
        Assert.That(MagazineLoadoutProjection.ComputeFillPct(0, 0), Is.EqualTo(0.0));
        Assert.That(MagazineLoadoutProjection.ComputeFillPct(5, 0), Is.EqualTo(100.0));
        Assert.That(MagazineLoadoutProjection.ComputeFillPct(3, 4), Is.EqualTo(75.0).Within(0.01));
        Assert.That(MagazineLoadoutProjection.ResolveStatus(0, 0), Is.EqualTo("EMPTY"));
        Assert.That(MagazineLoadoutProjection.ResolveStatus(1, 20), Is.EqualTo("LOW"));
        Assert.That(MagazineLoadoutProjection.ResolveStatus(1, 25), Is.EqualTo("LOW"));
        Assert.That(MagazineLoadoutProjection.ResolveStatus(2, 26), Is.EqualTo("OK"));
    }

    [Test]
    public void Aggregate_counts_status_bands()
    {
        var entries = MagazineLoadoutProjection.Project(new (string, string, string, string, string?, int, int)[]
        {
            ("a", "p", "m1", "w", "W", 8, 10),
            ("a", "p", "m2", "w", "W", 1, 10),
            ("b", "p", "m3", "w", "W", 0, 4),
        });

        var agg = MagazineLoadoutProjection.Aggregate(entries);

        Assert.That(agg.OkCount, Is.EqualTo(1));
        Assert.That(agg.LowCount, Is.EqualTo(1));
        Assert.That(agg.EmptyCount, Is.EqualTo(1));
        Assert.That(agg.TotalCount, Is.EqualTo(3));
        Assert.That(agg.TotalRemaining, Is.EqualTo(9));
        Assert.That(agg.TotalCapacity, Is.EqualTo(24));
        Assert.That(agg.SummaryLine, Is.EqualTo("OK 1  LOW 1  EMPTY 1  ·  3 mounts"));
    }

    [Test]
    public void Aggregate_null_or_empty_is_Empty()
    {
        Assert.That(MagazineLoadoutProjection.Aggregate(null), Is.EqualTo(MagazineLoadoutAggregate.Empty));
        Assert.That(
            MagazineLoadoutProjection.Aggregate(Array.Empty<MagazineLoadoutEntry>()),
            Is.EqualTo(MagazineLoadoutAggregate.Empty));
    }

    [Test]
    public void ApplyState_formats_lines_and_header()
    {
        var entries = MagazineLoadoutProjection.Project(new (string, string, string, string, string?, int, int)[]
        {
            ("cvn-1", "CVN", "vls", "aim120", "AIM-120", 6, 8),
        });

        var presentation = MagazineLoadoutApplyState.Apply(entries);

        Assert.That(presentation.HasMagazineData, Is.True);
        Assert.That(presentation.Rows, Has.Count.EqualTo(1));
        Assert.That(presentation.Lines[0], Does.Contain("cvn-1"));
        Assert.That(presentation.Lines[0], Does.Contain("mount=vls"));
        Assert.That(presentation.Lines[0], Does.Contain("weapon=AIM-120"));
        Assert.That(presentation.Lines[0], Does.Contain("6/8"));
        Assert.That(presentation.Lines[0], Does.Contain("(75%)"));
        Assert.That(presentation.Lines[0], Does.Contain("[OK]"));
        Assert.That(presentation.HeaderLine, Does.StartWith("MAGAZINE  ·  "));
        Assert.That(presentation.EmptyStateLine, Is.Empty);
    }

    [Test]
    public void ApplyState_without_magazine_data_is_honest_NO_MAGAZINE_DATA()
    {
        var presentation = MagazineLoadoutApplyState.Apply(entries: null, hasMagazineData: false);

        Assert.That(presentation, Is.SameAs(MagazineLoadoutPresentation.NoMagazineData));
        Assert.That(presentation.HasMagazineData, Is.False);
        Assert.That(presentation.EmptyStateLine, Is.EqualTo("NO MAGAZINE DATA"));
        Assert.That(presentation.HeaderLine, Does.Contain("NO MAGAZINE DATA"));
        Assert.That(presentation.Lines, Is.Empty);
    }

    [Test]
    public void ApplyState_null_or_empty_returns_Empty()
    {
        Assert.That(MagazineLoadoutApplyState.Apply(null).Rows, Is.Empty);
        Assert.That(MagazineLoadoutApplyState.Apply(Array.Empty<MagazineLoadoutEntry>()), Is.SameAs(MagazineLoadoutPresentation.Empty));
        Assert.That(MagazineLoadoutPresentation.Empty.HasMagazineData, Is.True);
    }

    [Test]
    public void FormatFeasibilityLine_uses_HowManyAirframesCanArm()
    {
        var line = MagazineLoadoutApplyState.FormatFeasibilityLine(18, 6);
        Assert.That(line, Does.Contain("ARMABLE AIRFRAMES  3"));
        Assert.That(line, Does.Contain("stock=18"));
        Assert.That(line, Does.Contain("cost=6/ac"));
    }
}
