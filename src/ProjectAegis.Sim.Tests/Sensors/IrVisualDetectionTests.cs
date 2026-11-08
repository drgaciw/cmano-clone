using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class IrVisualDetectionTests
{
    [Fact]
    public void Visual_day_mask_is_greater_than_night_mask()
    {
        var day = IrVisualDetection.ComputeVisualEnvMask(dayFraction: 1.0);
        var night = IrVisualDetection.ComputeVisualEnvMask(dayFraction: 0.0);
        Assert.True(day > night);
        Assert.Equal(1.0, day, 6);
        Assert.Equal(IrVisualDetection.DefaultVisualNightFloor, night, 6);
    }

    [Fact]
    public void Visual_mask_respects_weather_and_clamps()
    {
        var masked = IrVisualDetection.ComputeVisualEnvMask(dayFraction: 1.0, weatherMask: 0.5);
        Assert.Equal(0.5, masked, 6);

        var over = IrVisualDetection.ComputeVisualEnvMask(dayFraction: 2.0, weatherMask: 2.0);
        Assert.Equal(1.0, over, 6);

        var under = IrVisualDetection.ComputeVisualEnvMask(dayFraction: -1.0, weatherMask: -1.0, nightFloor: 0);
        Assert.Equal(0.0, under, 6);
    }

    [Fact]
    public void Infrared_mask_uses_thermal_contrast()
    {
        var hot = IrVisualDetection.ComputeInfraredEnvMask(thermalContrast: 0.9);
        var cold = IrVisualDetection.ComputeInfraredEnvMask(thermalContrast: 0.1);
        Assert.True(hot > cold);
        Assert.Equal(0.9, hot, 6);
        Assert.Equal(0.1, cold, 6);

        var weathered = IrVisualDetection.ComputeInfraredEnvMask(thermalContrast: 0.8, weatherMask: 0.5);
        Assert.Equal(0.4, weathered, 6);
    }

    [Fact]
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

        Assert.Single(radarRolls);
        Assert.False(radarRolls[0].Detected);
        Assert.Equal(0, radarRolls[0].Pd);

        Assert.Single(irRolls);
        Assert.Equal(1.0, irRolls[0].Pd, 6);
        Assert.True(irRolls[0].Detected);
    }

    [Fact]
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
        Assert.Single(rolls);
        Assert.Equal(1.0, rolls[0].Pd, 6);
        Assert.True(rolls[0].Detected);
    }

    [Fact]
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

        Assert.Equal(a.Count, b.Count);
        Assert.True(a.Count >= 3);
        for (var i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].Trial.TargetId, b[i].Trial.TargetId);
            Assert.Equal(a[i].Trial.Modality, b[i].Trial.Modality);
            Assert.Equal(a[i].Pd, b[i].Pd);
            Assert.Equal(a[i].Draw, b[i].Draw);
            Assert.Equal(a[i].Detected, b[i].Detected);
        }
    }
}
