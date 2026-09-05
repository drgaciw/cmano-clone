namespace ProjectAegis.Data.Tests.Scenario;

using System.Text.Json;
using ProjectAegis.Data.Scenario.Policy;
using Xunit;

public sealed class ScenarioEngageWeaponFamilyJsonDtoTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public void Deserialize_reads_explicit_weapon_family()
    {
        const string json = """
            { "id": "family", "engage": { "weaponFamilyId": "Laser" } }
            """;

        var dto = JsonSerializer.Deserialize<ScenarioPolicyJsonDto>(json, Options);

        Assert.Equal("Laser", dto!.Engage!.WeaponFamilyId);
    }

    [Fact]
    public void Deserialize_keeps_omitted_weapon_family_null_for_sim_defaulting()
    {
        const string json = """
            { "id": "family", "engage": { "rangeMeters": 1000 } }
            """;

        var dto = JsonSerializer.Deserialize<ScenarioPolicyJsonDto>(json, Options);

        Assert.Null(dto!.Engage!.WeaponFamilyId);
    }
}
