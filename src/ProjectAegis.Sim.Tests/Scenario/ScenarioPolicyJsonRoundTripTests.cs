using System.Text.Json;
using ProjectAegis.Data.Scenario.Policy;
using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

/// <summary>Agent C — JSON contract stability for scenario policy files.</summary>
public sealed class ScenarioPolicyJsonRoundTripTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void Logistics_logTickBurn_round_trips_through_json()
    {
        var dto = new ScenarioPolicyJsonDto
        {
            Id = "fuel-audit",
            FriendlyRoe = "WeaponsFree",
            OpposingRoe = "WeaponsTight",
            Logistics = new ScenarioLogisticsJsonDto
            {
                JokerSimSeconds = 90,
                BingoSimSeconds = 180,
                FuelCapacityKg = 10_000,
                BurnRateKgPerSecond = 80,
                JokerFuelFraction = 0.25,
                BingoFuelFraction = 0.10,
                LogTickBurn = true,
            },
        };

        var json = JsonSerializer.Serialize(dto, Options);
        var roundTrip = JsonSerializer.Deserialize<ScenarioPolicyJsonDto>(json, Options)
            ?? throw new InvalidOperationException("Deserialize returned null.");

        var profile = ScenarioPolicyJsonLoader.ToProfile(roundTrip);
        Assert.True(profile.Logistics.LogTickBurn);
        Assert.True(profile.Logistics.UsesFuelBurnModel);
        Assert.Equal(10_000, profile.Logistics.FuelCapacityKg);
    }

    [Fact]
    public void Extra_json_properties_are_ignored_on_policy_dto()
    {
        const string json = """
            {
              "id": "ext",
              "friendlyRoe": "WeaponsFree",
              "opposingRoe": "WeaponsFree",
              "futureMilsimField": { "nested": true }
            }
            """;

        var dto = JsonSerializer.Deserialize<ScenarioPolicyJsonDto>(json, Options);
        Assert.NotNull(dto);
        Assert.Equal("ext", dto!.Id);
    }
}