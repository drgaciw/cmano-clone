namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using NUnit.Framework;
using CombatEvents;
using Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

[TestFixture]
public sealed class BattleGraphicBinderTests
{
    [Test]
    public void Tactical_MissileGunEnergy_AreDistinctWithoutColor()
    {
        var frame = Frame(
            5,
            Event(CombatEventPhase.Firing, "s1", "t1", "missile", 1, 5),
            Event(CombatEventPhase.Firing, "s2", "t2", "gun", 2, 5),
            Event(CombatEventPhase.Firing, "s3", "t3", "energy", 3, 5));

        var bound = BattleGraphicBinder.Bind(frame, Symbols());

        Assert.That(bound.Zoom, Is.EqualTo(CombatZoomBand.Tactical));
        var missile = bound.Legs.Single(x => x.WeaponFamilyId == "missile");
        Assert.That(missile.ShooterId, Is.EqualTo("s1"));
        Assert.That(missile.TargetId, Is.EqualTo("t1"));
        Assert.That(missile.Phase, Is.EqualTo(CombatEventPhase.Firing));
        var marks = bound.Legs.Select(leg => (leg.CueClass, leg.DeclutterToken, leg.WeaponFamilyId)).ToArray();
        Assert.That(marks.Select(x => x.CueClass).Distinct().Count(), Is.EqualTo(3));
        Assert.That(marks.Select(x => x.DeclutterToken).Distinct().Count(), Is.EqualTo(3));
        Assert.That(bound.Legs.Single(x => x.WeaponFamilyId == "missile").CueClass, Is.EqualTo(BattleGraphicCueClasses.Missile));
        Assert.That(bound.Legs.Single(x => x.WeaponFamilyId == "missile").DeclutterToken, Is.EqualTo(BattleGraphicDeclutterTokens.Missile));
        Assert.That(bound.Legs.Single(x => x.WeaponFamilyId == "gun").CueClass, Is.EqualTo(BattleGraphicCueClasses.Gun));
        Assert.That(bound.Legs.Single(x => x.WeaponFamilyId == "gun").DeclutterToken, Is.EqualTo(BattleGraphicDeclutterTokens.Gun));
        Assert.That(bound.Legs.Single(x => x.WeaponFamilyId == "energy").CueClass, Is.EqualTo(BattleGraphicCueClasses.Energy));
        Assert.That(bound.Legs.Single(x => x.WeaponFamilyId == "energy").DeclutterToken, Is.EqualTo(BattleGraphicDeclutterTokens.Energy));
        Assert.That(bound.Legs.Single(x => x.WeaponFamilyId == "missile").Text, Does.Contain(BattleGraphicDeclutterTokens.Missile));
    }

    [Test]
    public void Laser_UsesEnergyCue()
    {
        var frame = Frame(5, Event(CombatEventPhase.Firing, "s1", "t1", "laser", 4, 5));

        var bound = BattleGraphicBinder.Bind(frame, Symbols());

        Assert.That(bound.CueClass, Is.EqualTo(BattleGraphicCueClasses.Energy));
        Assert.That(bound.DeclutterToken, Is.EqualTo(BattleGraphicDeclutterTokens.Energy));
        Assert.That(bound.PrimaryLine, Does.Contain("═"));
    }

    [Test]
    public void Tactical_PrimaryLine_ShowsShooterTargetFamilyAndLifecycle()
    {
        var frame = Frame(5, Event(CombatEventPhase.InFlight, "s1", "t1", "missile", 7, 5));

        var bound = BattleGraphicBinder.Bind(frame, Symbols());

        Assert.That(bound.PrimaryLine, Does.Contain("s1"));
        Assert.That(bound.PrimaryLine, Does.Contain("t1"));
        Assert.That(bound.PrimaryLine, Does.Contain("missile"));
        Assert.That(bound.PrimaryLine, Does.Contain("In flight"));
        Assert.That(bound.Tooltip, Does.Contain("s1"));
        Assert.That(bound.Tooltip, Does.Contain("t1"));
        Assert.That(bound.Tooltip, Does.Contain("missile"));
        Assert.That(bound.Tooltip, Does.Contain("In flight"));
        Assert.That(bound.DetailText, Does.Contain(bound.PrimaryLine));
    }

    [Test]
    public void TrackAllocation_AppearsOnlyWhenTheExplanationSaysSo()
    {
        var tracked = Frame(
            5,
            new[]
            {
                new CombatEngagementExplanation(9, "s1", "t1", true, 2),
            },
            Event(CombatEventPhase.Firing, "s1", "t1", "missile", 9, 5));
        var denied = Frame(
            5,
            new[]
            {
                new CombatEngagementExplanation(9, "s1", "t1", false, 2),
            },
            Event(CombatEventPhase.Firing, "s1", "t1", "missile", 9, 5));
        var silent = Frame(5, Event(CombatEventPhase.Firing, "s1", "t1", "missile", 9, 5));

        var withTrack = BattleGraphicBinder.Bind(tracked, Symbols());
        var withoutTrack = BattleGraphicBinder.Bind(denied, Symbols());
        var unknown = BattleGraphicBinder.Bind(silent, Symbols());

        Assert.That(withTrack.TrackLine, Is.EqualTo("TRACK s1 → t1 | salvo 2"));
        Assert.That(withTrack.TrackCueClass, Is.EqualTo(BattleGraphicCueClasses.Track));
        Assert.That(withTrack.Tooltip, Does.Contain("salvo 2"));
        Assert.That(withoutTrack.TrackLine, Is.EqualTo("NO-TRACK s1 → t1"));
        Assert.That(withoutTrack.TrackCueClass, Is.EqualTo(BattleGraphicCueClasses.NoTrack));
        Assert.That(withoutTrack.TrackLine, Does.Not.Contain("salvo"));
        Assert.That(unknown.TrackLine, Is.EqualTo("TRACK: —"));
        Assert.That(unknown.Legs[0].HasAllocationTrack, Is.Null);
        Assert.That(unknown.Legs[0].SalvoSize, Is.Null);
    }

    [Test]
    public void FindTrackAllocation_LastMatchingFactWins()
    {
        var frame = CombatPresentationFrame.Empty with
        {
            Explanations = new[]
            {
                new CombatEngagementExplanation(3, "s1", "t1", false, 1),
                new CombatEngagementExplanation(3, "s1", "t1", true, 4),
                new CombatEngagementExplanation(8, "s2", "t2", true, 1),
            },
        };

        var found = frame.FindTrackAllocation(3, "s1");

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.HasFireControlTrack, Is.True);
        Assert.That(found.SalvoSize, Is.EqualTo(4));
        Assert.That(frame.FindTrackAllocation(3, "missing"), Is.Null);
    }

    [Test]
    public void InFlight_Trail_IsDeterministicBetweenSymbolPositions()
    {
        var frame = Frame(5, Event(CombatEventPhase.InFlight, "s1", "t1", "missile", 7, 5));

        var first = BattleGraphicBinder.Bind(frame, Symbols());
        var second = BattleGraphicBinder.Bind(frame, Symbols());

        Assert.That(first.Legs[0].Trail, Has.Count.EqualTo(4));
        Assert.That(first.Legs[0].Trail[0].Progress, Is.EqualTo(0.25f).Within(0.0001f));
        Assert.That(first.Legs[0].Trail[0].X, Is.EqualTo(0.25f).Within(0.0001f));
        Assert.That(first.Legs[0].Trail[0].Y, Is.EqualTo(0.25f).Within(0.0001f));
        Assert.That(first.Legs[0].Trail[3].Progress, Is.EqualTo(1f).Within(0.0001f));
        Assert.That(first.Legs[0].Trail[3].X, Is.EqualTo(0.7f).Within(0.0001f));
        Assert.That(first.TrailLine, Does.StartWith("TRAIL "));
        Assert.That(first.TrailCueClass, Is.EqualTo(BattleGraphicCueClasses.Trail));
        Assert.That(first.TrailLine, Is.EqualTo(second.TrailLine));
        Assert.That(first.PrimaryLine, Is.EqualTo(second.PrimaryLine));
        Assert.That(first.Tooltip, Is.EqualTo(second.Tooltip));
    }

    [Test]
    public void Firing_UsesShortLivedEffectText_AndDropsAfterTheHold()
    {
        var live = Frame(5, Event(CombatEventPhase.Firing, "s1", "t1", "gun", 2, 4));
        var expired = Frame(5, Event(CombatEventPhase.Firing, "s1", "t1", "gun", 2, 1));

        var shown = BattleGraphicBinder.Bind(live, Symbols());
        var hidden = BattleGraphicBinder.Bind(expired, Symbols());

        Assert.That(shown.TrailLine, Is.EqualTo("EFFECT FIRE"));
        Assert.That(shown.Legs[0].Trail, Is.Empty);
        Assert.That(hidden.VisibleCount, Is.EqualTo(0));
        Assert.That(hidden.DetailText, Does.Contain("gun"));
        Assert.That(hidden.PrimaryLine, Is.EqualTo("—"));
    }

    [Test]
    public void Terminal_IsShortLived_AndInFlightSurvivesTheSameAge()
    {
        var terminal = Frame(5, Event(CombatEventPhase.TerminalOutcome, "s1", "t1", "missile", 1, 1, "Hit"));
        var inflight = Frame(5, Event(CombatEventPhase.InFlight, "s1", "t1", "missile", 1, 1));

        Assert.That(BattleGraphicBinder.Bind(terminal, Symbols()).VisibleCount, Is.EqualTo(0));
        var trail = BattleGraphicBinder.Bind(inflight, Symbols());
        Assert.That(trail.VisibleCount, Is.EqualTo(1));
        Assert.That(trail.TrailLine, Does.StartWith("TRAIL "));
    }

    [Test]
    public void SelectedExpiredFire_StaysInspectable()
    {
        var evt = Event(CombatEventPhase.Firing, "s1", "t1", "gun", 2, 1);
        var frame = Frame(5, evt);
        var key = CombatMapPresenter.KeyFor(evt);

        var bound = BattleGraphicBinder.Bind(frame, Symbols(), selectedKey: key);

        Assert.That(bound.VisibleCount, Is.EqualTo(1));
        Assert.That(bound.TrailLine, Is.EqualTo("EFFECT FIRE"));
        Assert.That(bound.Legs[0].Key, Is.EqualTo(key));
    }

    [Test]
    public void Operational_AggregatesMatchingSalvos_AndDropsPerWeaponTrails()
    {
        var frame = Frame(
            5,
            new[]
            {
                new CombatEngagementExplanation(1, "s1", "t1", true, 2),
                new CombatEngagementExplanation(2, "s1", "t1", true, 2),
            },
            Event(CombatEventPhase.InFlight, "s1", "t1", "missile", 1, 5),
            Event(CombatEventPhase.InFlight, "s1", "t1", "missile", 2, 5));

        var bound = BattleGraphicBinder.Bind(frame, Symbols(), CombatZoomBand.Operational);

        Assert.That(bound.Zoom, Is.EqualTo(CombatZoomBand.Operational));
        Assert.That(bound.VisibleCount, Is.EqualTo(1));
        Assert.That(bound.Legs[0].Count, Is.EqualTo(2));
        Assert.That(bound.Legs[0].Trail, Is.Empty);
        Assert.That(bound.PrimaryLine, Does.Contain("Operational summary"));
        Assert.That(bound.PrimaryLine, Does.Contain("×2"));
        Assert.That(bound.DeclutterToken, Is.EqualTo(BattleGraphicDeclutterTokens.Missile));
        Assert.That(bound.TrailLine, Is.EqualTo("EFFECT AGGREGATE ×2"));
        Assert.That(bound.TrackLine, Does.Contain("TRACK ×2"));
        Assert.That(bound.TrackLine, Does.Not.Contain("salvo"));
    }

    [Test]
    public void MissingPositions_KeepHistoryWithoutFabricatingATrack()
    {
        var frame = Frame(5, Event(CombatEventPhase.Firing, "missing", "t1", "gun", 2, 5));

        var bound = BattleGraphicBinder.Bind(frame, Symbols());

        Assert.That(bound.VisibleCount, Is.EqualTo(0));
        Assert.That(bound.TrackLine, Is.EqualTo("TRACK: —"));
        Assert.That(bound.DetailText, Does.Contain("missing"));
    }

    [Test]
    public void EmptyFrame_ClearsChrome()
    {
        var bound = BattleGraphicBinder.Bind(null, Symbols());

        Assert.That(bound.StateLine, Is.EqualTo(BattleGraphicState.Empty.StateLine));
        Assert.That(bound.VisibleCount, Is.EqualTo(0));
        Assert.That(bound.DeclutterToken, Is.Empty);
    }

    [Test]
    public void PanelRows_ExposeCueClassesForTheHost()
    {
        var frame = Frame(5, Event(CombatEventPhase.Firing, "s1", "t1", "missile", 1, 5));
        var state = BattleGraphicBinder.Bind(frame, Symbols());

        var rows = BattleGraphicPanelBinder.BindRows(state);

        Assert.That(rows.Select(row => row.ElementName), Is.EqualTo(new[]
        {
            "battle-graphic-summary",
            "battle-graphic-line",
            "battle-graphic-track",
            "battle-graphic-trail",
            "battle-graphic-detail",
        }));
        var line = rows.Single(row => row.ElementName == "battle-graphic-line");
        Assert.That(line.CueClass, Is.EqualTo(BattleGraphicCueClasses.Missile));
        Assert.That(line.Text, Does.Contain("[BATTLE:MISSILE]"));
        Assert.That(line.Tooltip, Does.Contain("[BATTLE:MISSILE]"));
        Action mutate = () => ((IList<BattleGraphicLeg>)state.Legs).Add(state.Legs[0]);
        Assert.Throws<NotSupportedException>(mutate);
    }

    private static CombatPresentationFrame Frame(double now, params CombatEvent[] events) =>
        Frame(now, Array.Empty<CombatEngagementExplanation>(), events);

    private static CombatPresentationFrame Frame(
        double now,
        IReadOnlyList<CombatEngagementExplanation> tracks,
        params CombatEvent[] events) =>
        CombatPresentationFrame.Empty with
        {
            Events = new CombatEventSnapshot(events),
            SimTime = now,
            Explanations = tracks,
        };

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
            Symbol("s1", "Friendly", 0.1f),
            Symbol("s2", "Friendly", 0.2f),
            Symbol("s3", "Friendly", 0.3f),
            Symbol("t1", "Hostile", 0.7f),
            Symbol("t2", "Hostile", 0.8f),
            Symbol("t3", "Hostile", 0.9f),
        };

    private static MapSymbolEntry Symbol(string id, string affiliation, float x) =>
        new(id, affiliation, "glyph", id, x, x, false);
}
