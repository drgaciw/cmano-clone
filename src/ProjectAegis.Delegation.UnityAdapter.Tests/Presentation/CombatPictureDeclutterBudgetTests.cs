using NUnit.Framework;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class CombatPictureDeclutterBudgetTests
{
    [Test]
    public void Default_budgets_enforce_strict_zoom_band_declutter_ordering()
    {
        var theater = CombatPictureDeclutterBudgets.ForZoomBand(CombatZoomBand.Theater);
        var operational = CombatPictureDeclutterBudgets.ForZoomBand(CombatZoomBand.Operational);
        var tactical = CombatPictureDeclutterBudgets.ForZoomBand(CombatZoomBand.Tactical);

        Assert.That(theater.ZoomBand, Is.EqualTo(CombatZoomBand.Theater));
        Assert.That(theater.MaxVisibleBattleLegs, Is.LessThan(operational.MaxVisibleBattleLegs));
        Assert.That(operational.MaxVisibleBattleLegs, Is.LessThanOrEqualTo(tactical.MaxVisibleBattleLegs));
        Assert.That(theater.MaxVisibleLabels, Is.LessThan(tactical.MaxVisibleLabels));
        Assert.That(tactical.MaxP95BindMilliseconds, Is.GreaterThan(0));
        Assert.That(tactical.MaxGcAllocBytesPerBind, Is.GreaterThan(0));
    }

    [Test]
    public void Evaluate_passes_when_counts_and_timing_are_within_band_budget()
    {
        var sample = new CombatPictureBudgetSample(
            ZoomBand: CombatZoomBand.Operational,
            CaptureSource: CombatFrameCaptureSource.HeadlessBindMeasurement,
            Resolution: "1600x979",
            EntityCount: 64,
            VisibleLabelCount: 18,
            VisibleBattleLegCount: 8,
            VisibleOverlayCount: 6,
            MeanBindMilliseconds: 1.4,
            P95BindMilliseconds: 3.2,
            GcAllocBytesPerBind: 16_384);

        var result = CombatPictureDeclutterEvaluator.Evaluate(sample);

        Assert.That(sample.Resolution, Is.EqualTo("1600x979"));
        Assert.That(sample.EntityCount, Is.EqualTo(64));
        Assert.That(result.Passed, Is.True, string.Join("; ", result.Violations));
        Assert.That(result.BudgetApplied.ZoomBand, Is.EqualTo(CombatZoomBand.Operational));
        Assert.That(result.ProvesUnityFrameTiming, Is.False);
        Assert.That(result.Violations, Is.Empty);
    }

    [Test]
    public void Evaluate_fails_closed_when_declutter_or_timing_exceeds_budget()
    {
        var sample = new CombatPictureBudgetSample(
            ZoomBand: CombatZoomBand.Theater,
            CaptureSource: CombatFrameCaptureSource.UnityFrameCapture,
            Resolution: "1280x779",
            EntityCount: 250,
            VisibleLabelCount: 40,
            VisibleBattleLegCount: 24,
            VisibleOverlayCount: 30,
            MeanBindMilliseconds: 12.5,
            P95BindMilliseconds: 25.0,
            GcAllocBytesPerBind: 512_000);

        var result = CombatPictureDeclutterEvaluator.Evaluate(sample);

        Assert.That(result.Passed, Is.False);
        Assert.That(result.ProvesUnityFrameTiming, Is.True);
        Assert.That(result.Violations, Has.Some.Contains("labels"));
        Assert.That(result.Violations, Has.Some.Contains("battle legs"));
        Assert.That(result.Violations, Has.Some.Contains("overlays"));
        Assert.That(result.Violations, Has.Some.Contains("p95"));
        Assert.That(result.Violations, Has.Some.Contains("GC"));
    }

    [Test]
    public void EvaluateBattleGraphic_projects_battle_graphic_visible_count_against_zoom_band()
    {
        var legs = Enumerable.Range(0, 6)
            .Select(i => new BattleGraphicLeg(
                $"k{i}",
                $"MSL {i}",
                $"MSL {i}",
                BattleGraphicCueClasses.Missile,
                "[MSL]",
                "u1",
                "h1",
                "missile",
                CombatEventPhase.Firing,
                true,
                2,
                1,
                Array.Empty<CombatMapTrailSample>()))
            .ToArray();

        var state = new BattleGraphicState(
            "BATTLE: ACTIVE",
            "MSL x6",
            "TRACK: FC",
            "TRAIL: 0",
            string.Empty,
            "BATTLE",
            "[MSL]",
            BattleGraphicCueClasses.Missile,
            BattleGraphicCueClasses.Track,
            BattleGraphicCueClasses.Trail,
            CombatZoomBand.Theater,
            legs.Length,
            legs);

        var result = CombatPictureDeclutterEvaluator.EvaluateBattleGraphic(state);
        Assert.That(result.Passed, Is.False);
        Assert.That(result.Violations, Has.Some.Contains("battle legs"));
    }
}
