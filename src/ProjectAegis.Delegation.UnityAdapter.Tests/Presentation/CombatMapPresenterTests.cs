namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using NUnit.Framework;
using CombatEvents;
using Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

[TestFixture]
public sealed class CombatMapPresenterTests
{
    [Test]
    public void Build_LastAsOfEventPerLeg_UsesDistinctFamilyPresentation()
    {
        var snapshot = Snapshot(
            Event(CombatEventPhase.Firing, "s1", "t1", "missile", 7, 4),
            Event(CombatEventPhase.InFlight, "s1", "t1", "missile", 7, 5),
            Event(CombatEventPhase.Firing, "s2", "t2", "gun", 7, 5),
            Event(CombatEventPhase.Firing, "s3", "t3", "laser", 8, 5));

        var result = CombatMapPresenter.Build(snapshot, Symbols(), 5, CombatZoomBand.Tactical);

        Assert.That(result.Effects.Select(x => x.Phase), Is.EquivalentTo(new[]
        {
            CombatEventPhase.InFlight,
            CombatEventPhase.Firing,
            CombatEventPhase.Firing,
        }));
        Assert.That(result.Effects.Select(x => (x.FamilyGlyph, x.LinePattern, x.MotionLabel)).Distinct().Count(), Is.EqualTo(3));
        Assert.That(result.Effects.Select(x => x.Key), Does.Contain(CombatMapPresenter.KeyFor(snapshot.Events[0])));
        Assert.That(result.Effects.Select(x => x.Key), Does.Contain(CombatMapPresenter.KeyFor(snapshot.Events[2])));
        Assert.That(result.Effects.Single(x => x.ShooterId == "s1").Label, Does.Contain("Friendly"));
        Assert.That(result.Effects.Single(x => x.ShooterId == "s1").Label, Does.Contain("Track"));
    }

    [Test]
    public void Build_AuthorizationRefused_ProducesHistoryButNoFireEffect()
    {
        var snapshot = Snapshot(Event(CombatEventPhase.AuthorizationRefused, "s1", "t1", "missile", 1, 5, "ROE denied"));

        var result = CombatMapPresenter.Build(snapshot, Symbols(), 5, CombatZoomBand.Tactical);

        Assert.That(result.Effects, Is.Empty);
        Assert.That(result.EventLines, Has.Count.EqualTo(1));
        Assert.That(result.EventLines[0].Text, Does.Contain("Authorization refused"));
        Assert.That(result.EventLines[0].Text, Does.Contain("ROE denied"));
    }

    [Test]
    public void Build_SelectedRefusal_UsesNonFiringInspectionMarker()
    {
        var snapshot = Snapshot(Event(CombatEventPhase.AuthorizationRefused, "s1", "t1", "missile", 1, 1, "ROE denied"));
        var selected = CombatMapPresenter.KeyFor(snapshot.Events[0]);

        var result = CombatMapPresenter.Build(snapshot, Symbols(), 20, CombatZoomBand.Operational, selected);

        Assert.That(result.Effects, Has.Count.EqualTo(1));
        Assert.That(result.Effects[0].Clearance, Is.EqualTo("Refused"));
        Assert.That(result.Effects[0].FamilyGlyph, Is.EqualTo("⊘"));
        Assert.That(result.Effects[0].LinePattern, Is.EqualTo("none"));
        Assert.That(result.Effects[0].MotionLabel, Is.EqualTo("Static"));
    }

    [Test]
    public void Build_MissingPosition_KeepsReadableEventWithoutFabricatingGeometry()
    {
        var snapshot = Snapshot(Event(CombatEventPhase.Firing, "missing", "t1", "gun", 2, 5));

        var result = CombatMapPresenter.Build(snapshot, Symbols(), 5, CombatZoomBand.Tactical);

        Assert.That(result.Effects, Is.Empty);
        Assert.That(result.EventLines.Single().Text, Does.Contain("missing"));
    }

    [Test]
    public void Build_SelectedExpiredLeg_RemainsSeparateAndFutureEventsStayHidden()
    {
        var snapshot = Snapshot(
            Event(CombatEventPhase.TerminalOutcome, "s1", "t1", "missile", 1, 1, "Hit"),
            Event(CombatEventPhase.Firing, "s2", "t2", "missile", 2, 20));
        var selected = CombatMapPresenter.KeyFor(snapshot.Events[0]);

        var result = CombatMapPresenter.Build(snapshot, Symbols(), 10, CombatZoomBand.Theater, selected, holdSeconds: 2);

        Assert.That(result.Effects, Has.Count.EqualTo(1));
        Assert.That(result.Effects[0].Key, Is.EqualTo(selected));
        Assert.That(result.Effects[0].Count, Is.EqualTo(1));
        Assert.That(result.EventLines, Has.Count.EqualTo(1));
    }

    [Test]
    public void Build_Operational_AggregatesOnlyMatchingDisplayFacts()
    {
        var snapshot = Snapshot(
            Event(CombatEventPhase.TerminalOutcome, "s1", "t1", "missile", 1, 5, "Hit"),
            Event(CombatEventPhase.TerminalOutcome, "s1", "t1", "missile", 2, 5, "Hit"),
            Event(CombatEventPhase.TerminalOutcome, "s3", "t3", "missile", 3, 5, "Miss"));

        var result = CombatMapPresenter.Build(snapshot, Symbols(), 5, CombatZoomBand.Operational);

        Assert.That(result.Effects, Has.Count.EqualTo(2));
        var aggregate = result.Effects.Single(x => x.Outcome == "Hit");
        Assert.That(aggregate.Count, Is.EqualTo(2));
        Assert.That(aggregate.CorrelationKeys, Is.EqualTo(new[]
        {
            CombatMapPresenter.KeyFor(snapshot.Events[0]),
            CombatMapPresenter.KeyFor(snapshot.Events[1]),
        }));
    }

    [Test]
    public void Build_Operational_DoesNotDrawOneLineForDifferentLocationsOrPhases()
    {
        var snapshot = Snapshot(
            Event(CombatEventPhase.Firing, "s1", "t1", "missile", 1, 5, "Pending"),
            Event(CombatEventPhase.Firing, "s2", "t2", "missile", 2, 5, "Pending"),
            Event(CombatEventPhase.InFlight, "s1", "t1", "missile", 3, 5, "Pending"));

        var result = CombatMapPresenter.Build(snapshot, Symbols(), 5, CombatZoomBand.Operational);

        Assert.That(result.Effects, Has.Count.EqualTo(3));
        Assert.That(result.Effects.All(x => x.Count == 1), Is.True);
        Assert.That(result.Effects.Select(x => x.Label), Has.Some.Contains("s1 → t1"));
    }

    [Test]
    public void Build_TheaterSummary_HasNoGeometryLineAndPreservesLegKeys()
    {
        var snapshot = Snapshot(
            Event(CombatEventPhase.Firing, "s1", "t1", "missile", 1, 5),
            Event(CombatEventPhase.Firing, "s2", "t2", "missile", 2, 5));

        var result = CombatMapPresenter.Build(snapshot, Symbols(), 5, CombatZoomBand.Theater);

        Assert.That(result.Effects, Has.Count.EqualTo(1));
        Assert.That(result.Effects[0].LinePattern, Is.EqualTo("none"));
        Assert.That(result.Effects[0].MotionLabel, Is.EqualTo("Static"));
        Assert.That(result.Effects[0].Label, Does.StartWith("Theater summary:"));
        Assert.That(result.Effects[0].CorrelationKeys, Is.EqualTo(snapshot.Events.Select(CombatMapPresenter.KeyFor)));
    }

    [Test]
    public void Build_NonFiniteInputsAreRejectedAndNonFinitePositionsKeepHistoryOnly()
    {
        var snapshot = Snapshot(Event(CombatEventPhase.Firing, "s1", "t1", "gun", 1, 5));
        var invalidSymbols = new[]
        {
            Symbol("s1", "Friendly", float.NaN),
            Symbol("t1", "Hostile", 0.5f),
        };

        Action invalidNow = () => CombatMapPresenter.Build(snapshot, Symbols(), double.PositiveInfinity, CombatZoomBand.Tactical);
        Action invalidHold = () => CombatMapPresenter.Build(snapshot, Symbols(), 5, CombatZoomBand.Tactical, holdSeconds: double.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(invalidNow);
        Assert.Throws<ArgumentOutOfRangeException>(invalidHold);
        var result = CombatMapPresenter.Build(snapshot, invalidSymbols, 5, CombatZoomBand.Tactical);
        Assert.That(result.Effects, Is.Empty);
        Assert.That(result.EventLines, Has.Count.EqualTo(1));
    }

    [Test]
    public void Build_MaxEffects_IsDeterministicAndPreservesSelectedLeg()
    {
        var snapshot = Snapshot(
            Event(CombatEventPhase.Firing, "s1", "t1", "unknown-family", 1, 5),
            Event(CombatEventPhase.Firing, "s2", "t2", "gun", 2, 5),
            Event(CombatEventPhase.Firing, "s3", "t3", "laser", 3, 5));

        var selected = CombatMapPresenter.KeyFor(snapshot.Events[2]);
        var result = CombatMapPresenter.Build(snapshot, Symbols(), 5, CombatZoomBand.Tactical, selected, maxEffects: 2);

        Assert.That(result.Effects, Has.Count.EqualTo(2));
        Assert.That(result.Effects.Any(x => x.Key == selected), Is.True);
        var unknown = result.EventLines.Single(x => x.CorrelationId == 1);
        Assert.That(unknown.Text, Does.Contain("unknown-family"));
    }

    [Test]
    public void KeyFor_LengthPrefixesPreventDelimiterCollisions()
    {
        var first = Event(CombatEventPhase.Firing, "a:b", "c", "gun", 12, 5);
        var second = Event(CombatEventPhase.Firing, "a", "b:c", "gun", 12, 5);

        Assert.That(CombatMapPresenter.KeyFor(first), Is.Not.EqualTo(CombatMapPresenter.KeyFor(second)));
        Assert.That(CombatMapPresenter.KeyFor(first), Is.EqualTo(CombatMapPresenter.KeyFor("a:b", "c", 12)));
    }

    [Test]
    public void Build_ResultCollectionsRejectMutation()
    {
        var result = CombatMapPresenter.Build(
            Snapshot(Event(CombatEventPhase.Firing, "s1", "t1", "gun", 1, 5)),
            Symbols(),
            5,
            CombatZoomBand.Tactical);

        Action mutateEffects = () => ((IList<CombatMapEffect>)result.Effects).Add(result.Effects[0]);
        Action mutateLines = () => ((IList<CombatMapEventLine>)result.EventLines).Add(result.EventLines[0]);
        Action mutateKeys = () => ((IList<string>)result.Effects[0].CorrelationKeys).Add("other");
        Assert.Throws<NotSupportedException>(mutateEffects);
        Assert.Throws<NotSupportedException>(mutateLines);
        Assert.Throws<NotSupportedException>(mutateKeys);
    }

    private static CombatEventSnapshot Snapshot(params CombatEvent[] events) => new(events);

    private static CombatEvent Event(
        CombatEventPhase phase,
        string shooter,
        string target,
        string family,
        ulong correlation,
        double time,
        string outcome = "Pending") =>
        new(phase, shooter, target, family, outcome, correlation, time, (ulong)time, "explain");

    private static IReadOnlyList<MapSymbolEntry> Symbols() =>
        new[]
        {
            Symbol("s1", "Friendly", 0.1f), Symbol("s2", "Friendly", 0.2f), Symbol("s3", "Friendly", 0.3f),
            Symbol("t1", "Hostile", 0.7f), Symbol("t2", "Hostile", 0.8f), Symbol("t3", "Hostile", 0.9f),
        };

    private static MapSymbolEntry Symbol(string id, string affiliation, float x) =>
        new(id, affiliation, "glyph", id, x, x, false);
}
