using System.Text.Json;
using ProjectAegis.Data.Scenario.Policy;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

/// <summary>DRG-379 / DRG-386 / DRG-390 scenario JSON authoring surface.</summary>
public sealed class ScenarioEnvironmentJsonLoaderTests
{
    private static ScenarioPolicyProfile Load(string json) =>
        ScenarioPolicyJsonLoader.ToProfile(JsonSerializer.Deserialize<ScenarioPolicyJsonDto>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!);

    [Fact]
    public void Parses_line_of_sight_eccm_jammer_generation_and_comms_grid()
    {
        var p = Load("""
        {
          "id": "env-test",
          "jammers": [ { "targetId": "h1", "jamStrength": 0.8, "techGeneration": 3 } ],
          "lineOfSight": [ { "observerId": "u1", "targetId": "h1", "observerHeightMslMeters": 30,
                             "targetHeightMslMeters": 15, "rangeMeters": 25000,
                             "terrain": [ { "distanceMeters": 9000, "elevationMslMeters": 5 },
                                          { "distanceMeters": 3000, "elevationMslMeters": 2 } ] } ],
          "radarEccm": [ { "sensorId": "radar-1", "scanType": "aesa", "techGeneration": 5 } ],
          "commsGrid": [ { "atTick": 4, "unitId": "sub-1", "membership": "OffGrid", "reason": "deep" } ]
        }
        """);

        Assert.Equal(3, p.Jammers[0].TechGeneration);
        var los = Assert.Single(p.LineOfSight);
        Assert.Equal(new[] { 3000.0, 9000.0 }, los.Terrain!.Select(t => t.DistanceMeters));
        Assert.Equal(new RadarEccmProfile(RadarScanType.Aesa, false, 5), p.RadarEccm["radar-1"]);
        Assert.Equal(new ScenarioCommsGridTransition(4, "sub-1", "OffGrid", "deep"), Assert.Single(p.CommsGridTransitions));
    }

    [Theory]
    [InlineData("Phased")]
    [InlineData("AESA ")]
    [InlineData("2")]
    [InlineData("Mechanical,Aesa")]
    public void Rejects_invalid_radar_eccm_scan_type(string scanType)
    {
        var ex = Assert.Throws<InvalidDataException>(() => Load($$"""
        { "id": "bad", "radarEccm": [ { "sensorId": "radar-1", "scanType": "{{scanType}}" } ] }
        """));
        Assert.Contains(scanType, ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(9)]
    public void Rejects_radar_eccm_tech_generation_outside_documented_range(int generation)
    {
        var ex = Assert.Throws<InvalidDataException>(() => Load($$"""
        { "id": "bad", "radarEccm": [ { "sensorId": "radar-1", "scanType": "Aesa", "techGeneration": {{generation}} } ] }
        """));
        Assert.Contains("techGeneration", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("OnGrid,OffGrid")]
    public void Rejects_numeric_and_combined_comms_membership_at_load(string membership)
    {
        var ex = Assert.Throws<InvalidDataException>(() => Load($$"""
        { "id": "bad", "commsGrid": [ { "atTick": 1, "unitId": "u1", "membership": "{{membership}}" } ] }
        """));
        Assert.Contains(membership, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_invalid_comms_grid_membership_at_load()
    {
        Assert.Throws<InvalidDataException>(() => Load("""
        { "id": "bad", "commsGrid": [ { "atTick": 1, "unitId": "u1", "membership": "Maybe" } ] }
        """));
    }

    [Fact]
    public void Rejects_negative_los_range()
    {
        Assert.Throws<InvalidDataException>(() => Load("""
        { "id": "bad", "lineOfSight": [ { "observerId": "u1", "targetId": "h1", "rangeMeters": -1 } ] }
        """));
    }

    [Fact]
    public void ToProfile_maps_dto_built_in_code()
    {
        var dto = new ScenarioPolicyJsonDto
        {
            Id = "code-built",
            Jammers = [new ScenarioJammerJsonDto { TargetId = "h1", ObserverId = "u1", TechGeneration = 2 }],
            LineOfSight =
            [
                new ScenarioLineOfSightJsonDto
                {
                    ObserverId = "u1",
                    TargetId = "h1",
                    ObserverHeightMslMeters = 40,
                    TargetHeightMslMeters = 10,
                    RangeMeters = 20_000,
                    Terrain = [new ScenarioTerrainSampleJsonDto { DistanceMeters = 5_000, ElevationMslMeters = 3 }],
                },
            ],
            RadarEccm = [new ScenarioRadarEccmJsonDto { SensorId = "r1", ScanType = "Pesa", FrequencyAgile = true, TechGeneration = 4 }],
            CommsGrid = [new ScenarioCommsGridJsonDto { AtTick = 2, UnitId = "u1", Membership = "OnGrid", Reason = "init" }],
        };

        var p = ScenarioPolicyJsonLoader.ToProfile(dto);

        Assert.Equal(2, p.Jammers[0].TechGeneration);
        Assert.Equal("u1", p.Jammers[0].ObserverId);
        Assert.Equal(new TerrainProfileSample(5_000, 3), p.LineOfSight[0].Terrain![0]);
        Assert.Equal(new RadarEccmProfile(RadarScanType.Pesa, true, 4), p.RadarEccm["r1"]);
        Assert.Equal("init", p.CommsGridTransitions[0].Reason);
    }

    [Fact]
    public void Absent_sections_default_to_empty()
    {
        var p = Load("""{ "id": "plain" }""");
        Assert.Empty(p.LineOfSight);
        Assert.Empty(p.RadarEccm);
        Assert.Empty(p.CommsGridTransitions);
    }
}
