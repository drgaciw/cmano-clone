using ProjectAegis.Data.Scenario.Policy;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class EccmScenarioFactorTests
{
    [Fact]
    public void Eccm_factor_defaults_to_neutral_when_omitted_from_policy_json()
    {
        var profile = ScenarioPolicyJsonLoader.ToProfile(new ScenarioPolicyJsonDto
        {
            Id = "eccm-default",
            FriendlyRoe = "WeaponsFree",
            OpposingRoe = "WeaponsFree",
            Detection =
            [
                new ScenarioDetectionJsonDto
                {
                    ObserverId = "u1",
                    SensorId = "radar-1",
                    TargetId = "hostile-1",
                    ContactId = "c1",
                    BasePd = 1.0,
                },
            ],
        });

        Assert.Single(profile.DetectionTrials);
        Assert.Equal(1.0, profile.DetectionTrials[0].EccmFactor);
    }

    [Fact]
    public void Eccm_factor_neutral_one_preserves_partial_jam_pd()
    {
        var rolls = DeterministicDetectionLoop.RollTick(
            SimSeed.FromScenario(11),
            1,
            [new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0, JamStrength: 0.5, EccmFactor: 1.0)],
            null);

        Assert.Single(rolls);
        Assert.Equal(0.5, rolls[0].Pd);
    }

    [Fact]
    public void Eccm_factor_scales_pd_under_partial_jam()
    {
        var neutral = DeterministicDetectionLoop.RollTick(
            SimSeed.FromScenario(11),
            1,
            [new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0, JamStrength: 0.5, EccmFactor: 1.0)],
            null);
        var suppressed = DeterministicDetectionLoop.RollTick(
            SimSeed.FromScenario(11),
            1,
            [new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0, JamStrength: 0.5, EccmFactor: 0.5)],
            null);

        Assert.Equal(0.5, neutral[0].Pd);
        Assert.Equal(0.25, suppressed[0].Pd);
        Assert.NotEqual(neutral[0].Detected, suppressed[0].Detected);
    }

    [Fact]
    public void Eccm_factor_boundary_values_clamp_pd_to_unit_interval()
    {
        Assert.Equal(0, DetectionProbability.ComputePd(1.0, eccmFactor: 0.0, jamStrength: 0.0));
        Assert.Equal(1, DetectionProbability.ComputePd(1.0, eccmFactor: 2.0, jamStrength: 0.0));
        Assert.Equal(0, DetectionProbability.ComputePd(1.0, eccmFactor: 2.0, jamStrength: 1.0));
    }

    [Fact]
    public void Eccm_same_seed_yields_identical_detection_sequence()
    {
        var trials = new[]
        {
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0, JamStrength: 0.4, EccmFactor: 0.8),
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-2", "c2", 0.6, JamStrength: 0.2, EccmFactor: 1.1),
        };
        var seed = SimSeed.FromScenario(4242);

        var a = DeterministicDetectionLoop.RollTick(seed, 3, trials, null);
        var b = DeterministicDetectionLoop.RollTick(seed, 3, trials, null);

        Assert.Equal(a.Count, b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].Pd, b[i].Pd);
            Assert.Equal(a[i].Draw, b[i].Draw);
            Assert.Equal(a[i].Detected, b[i].Detected);
        }
    }

    [Fact]
    public void Baltic_patrol_jammed_fixture_loads_authored_eccm_factor()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-jammed.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);

        Assert.Equal("baltic-patrol-jammed", profile.Id);
        Assert.Single(profile.DetectionTrials);
        Assert.Equal(0.75, profile.DetectionTrials[0].EccmFactor);
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "data", "scenarios", "baltic-patrol-jammed.policy.json")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}