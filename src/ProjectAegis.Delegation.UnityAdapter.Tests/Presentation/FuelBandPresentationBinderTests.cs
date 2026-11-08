using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class FuelBandPresentationBinderTests
{
    [Test]
    public void Unit_detail_binder_resolves_joker_band_and_cue_from_projection_line()
    {
        var logistics = new ScenarioLogisticsSettings(300, 600, fuelCapacityKg: 10_000, burnRateKgPerSecond: 80);
        var fuelLine = FuelStateProjection.FormatUnitFuelLine("u1", 100, logistics);
        var presentation = new UnitDetailPresentation(
            "UNIT: u1",
            "STATUS: ALIVE",
            "MAGAZINE: —",
            "EMCON: —",
            "DOCTRINE: —",
            fuelLine,
            "ENGAGE: —",
            "ATTACK: —",
            "CONTACT: —",
            0);

        var bound = UnitDetailFuelBandBinder.Bind(presentation);

        Assert.That(bound.BandLabel, Is.EqualTo("JOKER"));
        Assert.That(bound.CueClass, Is.EqualTo(FuelBandCueClasses.Joker));
        Assert.That(bound.DeclutterToken, Is.EqualTo(FuelBandDeclutterTokens.Joker));
        Assert.That(bound.FuelLineText, Does.Contain("JOKER"));
    }

    [Test]
    public void Unit_detail_binder_resolves_bingo_band_from_projection_line()
    {
        var logistics = new ScenarioLogisticsSettings(300, 600, fuelCapacityKg: 10_000, burnRateKgPerSecond: 80);
        var fuelLine = FuelStateProjection.FormatUnitFuelLine("u1", 120, logistics);
        var presentation = UnitDetailApplyState.Apply(new UnitDetailPanelState(
            "UNIT: u1",
            "STATUS: ALIVE",
            "MAGAZINE: —",
            "EMCON: —",
            "DOCTRINE: —",
            fuelLine,
            "ENGAGE: —",
            "ATTACK: —",
            "CONTACT: —",
            Array.Empty<EngageAttackOptions.AttackOption>()));

        var bound = UnitDetailFuelBandBinder.Bind(presentation);

        Assert.That(bound.BandLabel, Is.EqualTo("BINGO"));
        Assert.That(bound.CueClass, Is.EqualTo(FuelBandCueClasses.Bingo));
        Assert.That(bound.DeclutterToken, Is.EqualTo(FuelBandDeclutterTokens.Bingo));
    }

    [Test]
    public void Message_log_binder_resolves_band_from_fuel_state_change_record_props()
    {
        var change = new FuelStateChangeRecord(
            SequenceId: 42,
            SimTime: 95.0,
            SimTick: 95,
            UnitId: new TargetId("u1"),
            PreviousState: "NOMINAL",
            NewState: "JOKER",
            RemainingFuelKg: 2400);

        var bound = MessageLogFuelBandBinder.Bind(change);

        Assert.That(bound.BandLabel, Is.EqualTo("JOKER"));
        Assert.That(bound.CueClass, Is.EqualTo(FuelBandCueClasses.Joker));
        Assert.That(
            MessageLogFuelBandBinder.FormatFuelStateChangeDisplayLine(change),
            Is.EqualTo("[FUEL] Fuel u1: NOMINAL → JOKER (2400 kg)"));
    }

    [Test]
    public void Message_log_binder_resolves_band_from_bound_display_row()
    {
        var row = new MessageLogDisplayRow(
            "FUEL",
            "[FUEL] Fuel u1: JOKER → BINGO (800 kg)",
            7,
            "u1");

        var bound = MessageLogFuelBandBinder.Bind(row);

        Assert.That(bound.BandLabel, Is.EqualTo("BINGO"));
        Assert.That(bound.CueClass, Is.EqualTo(FuelBandCueClasses.Bingo));
    }

    [Test]
    public void Message_log_binder_ignores_non_fuel_categories()
    {
        var row = new MessageLogDisplayRow(
            "MAGAZINE",
            "[MAGAZINE] Magazine u1 mount 1: -1 (ENGAGE)",
            3,
            "u1",
            "message-log-row--magazine");

        var bound = MessageLogFuelBandBinder.Bind(row);

        Assert.That(bound.BandLabel, Is.Null);
        Assert.That(bound.CueClass, Is.Null);
    }

    [Test]
    public void Nominal_band_uses_nominal_cue_without_declutter_token()
    {
        var presentation = new UnitDetailPresentation(
            "UNIT: u1",
            "STATUS: ALIVE",
            "MAGAZINE: —",
            "EMCON: —",
            "DOCTRINE: —",
            "FUEL: NOMINAL (u1)",
            "ENGAGE: —",
            "ATTACK: —",
            "CONTACT: —",
            0);

        var bound = UnitDetailFuelBandBinder.Bind(presentation);

        Assert.That(bound.BandLabel, Is.EqualTo("NOMINAL"));
        Assert.That(bound.CueClass, Is.EqualTo(FuelBandCueClasses.Nominal));
        Assert.That(bound.DeclutterToken, Is.Null);
    }
}
