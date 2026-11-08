using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class DeckHangarCapacityTests
{
    [Test]
    public void Project_maps_spots_ready_and_band()
    {
        var rows = new (string, int, int, int, int, int, int?)[]
        {
            ("CVN-1", 4, 1, 20, 5, 2, 12),
            ("LHA-1", 2, 2, 8, 7, 0, null),
        };

        var entries = DeckHangarCapacityProjection.Project(rows);

        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.That(entries[0].HostId, Is.EqualTo("CVN-1"));
        Assert.That(entries[0].DeckSpotsTotal, Is.EqualTo(4));
        Assert.That(entries[0].DeckSpotsOccupied, Is.EqualTo(1));
        Assert.That(entries[0].HangarSpotsTotal, Is.EqualTo(20));
        Assert.That(entries[0].HangarSpotsOccupied, Is.EqualTo(5));
        Assert.That(entries[0].ReadyOnDeck, Is.EqualTo(2));
        Assert.That(entries[0].SortieRatePerHour, Is.EqualTo(12));
        Assert.That(entries[0].CapacityBand, Is.EqualTo(DeckHangarCapacityProjection.BandOpen));

        var fullDeck = entries.Single(e => e.HostId == "LHA-1");
        Assert.That(fullDeck.CapacityBand, Is.EqualTo(DeckHangarCapacityProjection.BandFull));
        Assert.That(fullDeck.SortieRatePerHour, Is.Null);
    }

    [Test]
    public void Project_from_DeckHangarCapacityState()
    {
        var states = new[]
        {
            new DeckHangarCapacityState("HOST-B", 4, 3, 10, 8, 1, 6),
            new DeckHangarCapacityState("HOST-A", 2, 0, 4, 0, 2),
        };

        var entries = DeckHangarCapacityProjection.Project(states);

        Assert.That(entries.Select(e => e.HostId), Is.EqualTo(new[] { "HOST-A", "HOST-B" }));
        Assert.That(entries[0].CapacityBand, Is.EqualTo(DeckHangarCapacityProjection.BandOpen));
        // HOST-B: deck 3/4 = 75% congested, hangar 8/10 = 80% congested
        Assert.That(entries[1].CapacityBand, Is.EqualTo(DeckHangarCapacityProjection.BandCongested));
    }

    [Test]
    public void ResolveCapacityBand_open_congested_full()
    {
        Assert.That(DeckHangarCapacityProjection.ResolveCapacityBand(0, 10), Is.EqualTo("Open"));
        Assert.That(DeckHangarCapacityProjection.ResolveCapacityBand(7, 10), Is.EqualTo("Open"));
        Assert.That(DeckHangarCapacityProjection.ResolveCapacityBand(8, 10), Is.EqualTo("Congested"));
        Assert.That(DeckHangarCapacityProjection.ResolveCapacityBand(9, 10), Is.EqualTo("Congested"));
        Assert.That(DeckHangarCapacityProjection.ResolveCapacityBand(10, 10), Is.EqualTo("Full"));
        Assert.That(DeckHangarCapacityProjection.ResolveCapacityBand(11, 10), Is.EqualTo("Full"));
        Assert.That(DeckHangarCapacityProjection.ResolveCapacityBand(0, 0), Is.EqualTo("Open"));
    }

    [Test]
    public void ResolveCombinedBand_takes_worse_of_deck_and_hangar()
    {
        Assert.That(
            DeckHangarCapacityProjection.ResolveCombinedBand(0, 4, 0, 10),
            Is.EqualTo("Open"));
        Assert.That(
            DeckHangarCapacityProjection.ResolveCombinedBand(0, 4, 8, 10),
            Is.EqualTo("Congested"));
        Assert.That(
            DeckHangarCapacityProjection.ResolveCombinedBand(4, 4, 0, 10),
            Is.EqualTo("Full"));
        Assert.That(
            DeckHangarCapacityProjection.ResolveCombinedBand(1, 0, 9, 10),
            Is.EqualTo("Congested"));
    }

    [Test]
    public void Project_empty_or_null_returns_empty()
    {
        Assert.That(DeckHangarCapacityProjection.Project(
            (IReadOnlyList<(string, int, int, int, int, int, int?)>?)null), Is.Empty);
        Assert.That(DeckHangarCapacityProjection.Project(
            Array.Empty<(string, int, int, int, int, int, int?)>()), Is.Empty);
        Assert.That(DeckHangarCapacityProjection.Project(
            (IReadOnlyList<DeckHangarCapacityState>?)null), Is.Empty);
    }

    [Test]
    public void Aggregate_counts_bands_and_ready()
    {
        var entries = DeckHangarCapacityProjection.Project(new (string, int, int, int, int, int, int?)[]
        {
            ("A", 4, 0, 10, 0, 3, null),
            ("B", 4, 3, 10, 8, 1, 4),
            ("C", 2, 2, 4, 4, 0, null),
        });

        var agg = DeckHangarCapacityProjection.Aggregate(entries);

        Assert.That(agg.OpenCount, Is.EqualTo(1));
        Assert.That(agg.CongestedCount, Is.EqualTo(1));
        Assert.That(agg.FullCount, Is.EqualTo(1));
        Assert.That(agg.TotalHosts, Is.EqualTo(3));
        Assert.That(agg.TotalReadyOnDeck, Is.EqualTo(4));
        Assert.That(agg.SummaryLine, Is.EqualTo("OPEN 1  CONGESTED 1  FULL 1  ·  ready 4"));
    }

    [Test]
    public void ApplyState_formats_lines_and_header()
    {
        var entries = DeckHangarCapacityProjection.Project(new (string, int, int, int, int, int, int?)[]
        {
            ("CVN-1", 4, 1, 20, 5, 2, 12),
        });

        var presentation = DeckHangarCapacityApplyState.Apply(entries);

        Assert.That(presentation.HasCapacityData, Is.True);
        Assert.That(presentation.Lines[0], Does.Contain("CVN-1"));
        Assert.That(presentation.Lines[0], Does.Contain("deck=1/4"));
        Assert.That(presentation.Lines[0], Does.Contain("hangar=5/20"));
        Assert.That(presentation.Lines[0], Does.Contain("ready=2"));
        Assert.That(presentation.Lines[0], Does.Contain("sortie/h=12"));
        Assert.That(presentation.Lines[0], Does.Contain("[Open]"));
        Assert.That(presentation.HeaderLine, Does.StartWith("DECK/HANGAR  ·  "));
        Assert.That(presentation.EmptyStateLine, Is.Empty);
    }

    [Test]
    public void ApplyState_without_capacity_data_is_honest_NO_CAPACITY_DATA()
    {
        var presentation = DeckHangarCapacityApplyState.Apply(entries: null, hasCapacityData: false);

        Assert.That(presentation, Is.SameAs(DeckHangarCapacityPresentation.NoCapacityData));
        Assert.That(presentation.HasCapacityData, Is.False);
        Assert.That(presentation.EmptyStateLine, Is.EqualTo("NO CAPACITY DATA"));
        Assert.That(presentation.HeaderLine, Does.Contain("NO CAPACITY DATA"));
        Assert.That(presentation.Lines, Is.Empty);
    }

    [Test]
    public void ApplyState_null_or_empty_returns_Empty()
    {
        Assert.That(DeckHangarCapacityApplyState.Apply(null).Rows, Is.Empty);
        Assert.That(
            DeckHangarCapacityApplyState.Apply(Array.Empty<DeckHangarCapacityEntry>()),
            Is.SameAs(DeckHangarCapacityPresentation.Empty));
        Assert.That(DeckHangarCapacityPresentation.Empty.HasCapacityData, Is.True);
    }
}
