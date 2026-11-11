namespace ProjectAegis.Sim.Tests.Scenario;

using ProjectAegis.Data.Scenario.Policy;
using ProjectAegis.Sim.Scenario;
using Xunit;

public sealed class ScenarioWeaponFamilyPolicyTests
{
    [Theory]
    [InlineData("slice-b-missile.policy.json", "Missile")]
    [InlineData("slice-b-gun.policy.json", "Gun")]
    [InlineData("slice-b-laser.policy.json", "Laser")]
    public void Synthetic_policy_round_trips_explicit_weapon_family(string fileName, string expected)
    {
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(ScenarioPath(fileName));

        Assert.NotNull(profile.EngageDefaults);
        Assert.Equal(expected, profile.EngageDefaults!.WeaponFamilyId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_or_blank_weapon_family_defaults_to_unknown(string? family)
    {
        var profile = ScenarioPolicyJsonLoader.ToProfile(new ScenarioPolicyJsonDto
        {
            Id = "family-default",
            Engage = new ScenarioEngageJsonDto { WeaponFamilyId = family },
        });

        Assert.Equal("Unknown", profile.EngageDefaults!.WeaponFamilyId);
        Assert.Equal("Unknown", ScenarioEngageDefaults.MvpFallback.WeaponFamilyId);
    }

    private static string ScenarioPath(string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "ProjectAegis.sln")))
        {
            current = current.Parent;
        }

        Assert.NotNull(current);
        return Path.Combine(current!.FullName, "data", "scenarios", fileName);
    }
}
