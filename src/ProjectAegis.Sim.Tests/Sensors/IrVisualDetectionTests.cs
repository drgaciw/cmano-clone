using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class IrVisualDetectionTests
{
    [Test]
    public void Visual_day_mask_is_greater_than_night_mask()
    {
        var day = IrVisualDetection.ComputeVisualEnvMask(dayFraction: 1.0);
        var night = IrVisualDetection.ComputeVisualEnvMask(dayFraction: 0.0);
        Assert.That(day > night, Is.True);
        Assert.That(
            Math.Round(day, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(1.0, 6, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round(night, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(IrVisualDetection.DefaultVisualNightFloor, 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Visual_mask_respects_weather_and_clamps()
    {
        var masked = IrVisualDetection.ComputeVisualEnvMask(dayFraction: 1.0, weatherMask: 0.5);
        Assert.That(
            Math.Round(masked, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.5, 6, MidpointRounding.AwayFromZero)));

        var over = IrVisualDetection.ComputeVisualEnvMask(dayFraction: 2.0, weatherMask: 2.0);
        Assert.That(
            Math.Round(over, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(1.0, 6, MidpointRounding.AwayFromZero)));

        var under = IrVisualDetection.ComputeVisualEnvMask(dayFraction: -1.0, weatherMask: -1.0, nightFloor: 0);
        Assert.That(
            Math.Round(under, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.0, 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Infrared_mask_uses_thermal_contrast()
    {
        var hot = IrVisualDetection.ComputeInfraredEnvMask(thermalContrast: 0.9);
        var cold = IrVisualDetection.ComputeInfraredEnvMask(thermalContrast: 0.1);
        Assert.That(hot > cold, Is.True);
        Assert.That(
            Math.Round(hot, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.9, 6, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round(cold, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.1, 6, MidpointRounding.AwayFromZero)));

        var weathered = IrVisualDetection.ComputeInfraredEnvMask(thermalContrast: 0.8, weatherMask: 0.5);
        Assert.That(
            Math.Round(weathered, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.4, 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Radar_rf_jam_suppresses_identical_ir_trial_does_not()
    {
        var jammers = new[] { new ScenarioJammer("hostile-1", 1.0, ActiveFromTick: 1) };
        var seed = SimSeed.FromScenario(42);
        const ulong tick = 1;

        var radarTrial = new ScenarioDetectionTrial(
            "u1", "radar-1", "hostile-1", "c1", 1.0,
            RequiresActiveRadar: false,
            Modality: SensorModality.Radar);
        var irTrial = new ScenarioDetectionTrial(
            "u1", "ir-1", "hostile-1", "c-ir", 1.0,
            RequiresActiveRadar: false,
            Modality: SensorModality.Infrared);

        var radarRolls = DeterministicDetectionLoop.RollTick(
            seed, tick, [radarTrial], null, jammers: jammers);
        var irRolls = DeterministicDetectionLoop.RollTick(
            seed, tick, [irTrial], null, jammers: jammers);

        AssertEx.Single(radarRolls);
        Assert.That(radarRolls[0].Detected, Is.False);
        Assert.That(radarRolls[0].Pd, Is.EqualTo(0));

        AssertEx.Single(irRolls);
        Assert.That(
            Math.Round(irRolls[0].Pd, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(1.0, 6, MidpointRounding.AwayFromZero)));
        Assert.That(irRolls[0].Detected, Is.True);
    }

    [Test]
    public void Visual_trial_also_ignores_rf_jammers()
    {
        var jammers = new[] { new ScenarioJammer("hostile-1", 1.0) };
        var visual = new ScenarioDetectionTrial(
            "u1", "eo-1", "hostile-1", "c-vis", 1.0,
            EnvMask: IrVisualDetection.ComputeVisualEnvMask(1.0),
            RequiresActiveRadar: false,
            Modality: SensorModality.Visual);

        var rolls = DeterministicDetectionLoop.RollTick(
            SimSeed.FromScenario(7), 1, [visual], null, jammers: jammers);
        AssertEx.Single(rolls);
        Assert.That(
            Math.Round(rolls[0].Pd, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(1.0, 6, MidpointRounding.AwayFromZero)));
        Assert.That(rolls[0].Detected, Is.True);
    }

    [Test]
    public void Mixed_modality_rolls_are_deterministic_for_same_seed()
    {
        var trials = new[]
        {
            new ScenarioDetectionTrial(
                "u1", "radar-1", "hostile-2", "c2", 0.5,
                Modality: SensorModality.Radar),
            new ScenarioDetectionTrial(
                "u1", "ir-1", "hostile-1", "c1", 0.7,
                EnvMask: IrVisualDetection.ComputeInfraredEnvMask(0.7),
                RequiresActiveRadar: false,
                Modality: SensorModality.Infrared),
            new ScenarioDetectionTrial(
                "u1", "eo-1", "hostile-3", "c3", 0.6,
                EnvMask: IrVisualDetection.ComputeVisualEnvMask(0.8),
                RequiresActiveRadar: false,
                Modality: SensorModality.Visual),
        };
        var jammers = new[] { new ScenarioJammer("hostile-2", 0.3) };
        var seed = SimSeed.FromScenario(4242);

        var a = DeterministicDetectionLoop.RollTick(seed, 3, trials, null, jammers: jammers);
        var b = DeterministicDetectionLoop.RollTick(seed, 3, trials, null, jammers: jammers);

        Assert.That(b.Count, Is.EqualTo(a.Count));
        Assert.That(a.Count >= 3, Is.True);
        for (var i = 0; i < a.Count; i++)
        {
            Assert.That(b[i].Trial.TargetId, Is.EqualTo(a[i].Trial.TargetId));
            Assert.That(b[i].Trial.Modality, Is.EqualTo(a[i].Trial.Modality));
            Assert.That(b[i].Pd, Is.EqualTo(a[i].Pd));
            Assert.That(b[i].Draw, Is.EqualTo(a[i].Draw));
            Assert.That(b[i].Detected, Is.EqualTo(a[i].Detected));
        }
    }
}
