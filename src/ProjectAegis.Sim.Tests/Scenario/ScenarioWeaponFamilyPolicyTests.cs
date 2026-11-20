namespace ProjectAegis.Sim.Tests.Scenario;

using ProjectAegis.Data.Scenario.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

public sealed class ScenarioWeaponFamilyPolicyTests
{

    [TestCase("slice-b-missile.policy.json", "Missile")]
    [TestCase("slice-b-gun.policy.json", "Gun")]
    [TestCase("slice-b-laser.policy.json", "Laser")]
    public void Synthetic_policy_round_trips_explicit_weapon_family(string fileName, string expected)
    {
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(ScenarioPath(fileName));

        Assert.That(profile.EngageDefaults, Is.Not.Null);
        Assert.That(profile.EngageDefaults!.WeaponFamilyId, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Missing_or_blank_weapon_family_defaults_to_unknown(string? family)
    {
        var profile = ScenarioPolicyJsonLoader.ToProfile(new ScenarioPolicyJsonDto
        {
            Id = "family-default",
            Engage = new ScenarioEngageJsonDto { WeaponFamilyId = family },
        });

        Assert.That(profile.EngageDefaults!.WeaponFamilyId, Is.EqualTo("Unknown"));
        Assert.That(ScenarioEngageDefaults.MvpFallback.WeaponFamilyId, Is.EqualTo("Unknown"));
    }

    private static string ScenarioPath(string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "ProjectAegis.sln")))
        {
            current = current.Parent;
        }

        Assert.That(current, Is.Not.Null);
        return Path.Combine(current!.FullName, "data", "scenarios", fileName);
    }
}
