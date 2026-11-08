using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>DRG-109 / SWARM-C5: mission types for swarm tasking (SWARM-20).</summary>
public sealed class SwarmMissionTypeTests
{
    [Theory]
    [InlineData(SwarmMissionType.Patrol, CatalogSwarmPlatformDefaults.ModeHold)]
    [InlineData(SwarmMissionType.Support, CatalogSwarmPlatformDefaults.ModeScreen)]
    [InlineData(SwarmMissionType.Strike, CatalogSwarmPlatformDefaults.ModeAssault)]
    public void DefaultMode_maps_mission_to_phase_b_mode(SwarmMissionType mission, string expectedMode)
    {
        Assert.Equal(expectedMode, SwarmMissionDefaults.DefaultMode(mission));
        Assert.Equal(expectedMode, SwarmMissionDefaults.DefaultMode(SwarmMissionTypeNames.ToName(mission)));
    }

    [Fact]
    public void Place_patrol_without_mode_applies_hold()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var unit = editor.PlaceSwarmUnit(
            "swarm-1",
            "blue",
            "usn-cec-swarm",
            57.0,
            20.0,
            droneCount: 8,
            maxDrones: 24,
            missionType: SwarmMissionTypeNames.Patrol);
        Assert.Equal(SwarmMissionTypeNames.Patrol, unit.MissionType);
        Assert.Equal(CatalogSwarmPlatformDefaults.ModeHold, unit.Mode);
    }

    [Fact]
    public void Place_support_without_mode_applies_screen()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var unit = editor.PlaceSwarmUnit(
            "swarm-1",
            "blue",
            "usn-cec-swarm",
            57.0,
            20.0,
            droneCount: 8,
            maxDrones: 24,
            missionType: SwarmMissionTypeNames.Support);
        Assert.Equal(SwarmMissionTypeNames.Support, unit.MissionType);
        Assert.Equal(CatalogSwarmPlatformDefaults.ModeScreen, unit.Mode);
    }

    [Fact]
    public void Place_strike_without_mode_applies_assault()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var unit = editor.PlaceSwarmUnit(
            "swarm-1",
            "blue",
            "usn-cec-swarm",
            57.0,
            20.0,
            droneCount: 8,
            maxDrones: 24,
            missionType: SwarmMissionTypeNames.Strike);
        Assert.Equal(SwarmMissionTypeNames.Strike, unit.MissionType);
        Assert.Equal(CatalogSwarmPlatformDefaults.ModeAssault, unit.Mode);
    }

    [Fact]
    public void Explicit_mode_overrides_mission_default()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var unit = editor.PlaceSwarmUnit(
            "swarm-1",
            "blue",
            "usn-cec-swarm",
            57.0,
            20.0,
            droneCount: 8,
            maxDrones: 24,
            missionType: SwarmMissionTypeNames.Strike,
            mode: CatalogSwarmPlatformDefaults.ModeScatter);
        Assert.Equal(SwarmMissionTypeNames.Strike, unit.MissionType);
        Assert.Equal(CatalogSwarmPlatformDefaults.ModeScatter, unit.Mode);
    }

    [Fact]
    public void Unknown_mission_type_rejected()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            editor.PlaceSwarmUnit(
                "swarm-1",
                "blue",
                "usn-cec-swarm",
                57.0,
                20.0,
                droneCount: 5,
                maxDrones: 20,
                missionType: "Escort"));
        Assert.Contains(SwarmScenarioValidation.MissionTypeUnknown, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Pure_validation_rejects_unknown_mission_type()
    {
        var r = SwarmScenarioValidation.ValidatePlacement(
            "p",
            5,
            10,
            null,
            true,
            missionType: "BombingRun");
        Assert.False(r.IsValid);
        Assert.Equal(SwarmScenarioValidation.MissionTypeUnknown, r.ErrorCode);
    }

    [Fact]
    public void Round_trip_json_preserves_MissionType_and_Mode()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-swarm-mission-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.PlaceSwarmUnit(
                "swarm-1",
                "blue",
                "usn-cec-swarm",
                57.1,
                20.1,
                droneCount: 15,
                maxDrones: 24,
                missionType: SwarmMissionTypeNames.Support);
            editor.CommitMutation();
            editor.Save(path);

            var loaded = ScenarioDocumentJsonLoader.LoadFromFile(path);
            var swarm = loaded.Orbat!.Units.Single(u => u.Id == "swarm-1");
            Assert.Equal(SwarmMissionTypeNames.Support, swarm.MissionType);
            Assert.Equal(CatalogSwarmPlatformDefaults.ModeScreen, swarm.Mode);
            Assert.Equal(15, swarm.DroneCount);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Configure_mission_type_applies_default_mode()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.PlaceSwarmUnit("swarm-1", "blue", "usn-cec-swarm", 57.0, 20.0, droneCount: 5, maxDrones: 20);
        editor.ConfigureSwarmUnit("swarm-1", missionType: SwarmMissionTypeNames.Patrol, maxDrones: 20);
        var unit = editor.ToDto().Orbat!.Units[0];
        Assert.Equal(SwarmMissionTypeNames.Patrol, unit.MissionType);
        Assert.Equal(CatalogSwarmPlatformDefaults.ModeHold, unit.Mode);
    }

    [Fact]
    public void MissionType_null_leaves_mode_unset()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var unit = editor.PlaceSwarmUnit(
            "swarm-1",
            "blue",
            "usn-cec-swarm",
            57.0,
            20.0,
            droneCount: 5,
            maxDrones: 20);
        Assert.Null(unit.MissionType);
        Assert.Null(unit.Mode);
    }

    [Fact]
    public void ResolveMode_prefers_explicit_over_default()
    {
        Assert.Equal(
            CatalogSwarmPlatformDefaults.ModeRejoin,
            SwarmMissionDefaults.ResolveMode(SwarmMissionTypeNames.Strike, CatalogSwarmPlatformDefaults.ModeRejoin));
        Assert.Equal(
            CatalogSwarmPlatformDefaults.ModeAssault,
            SwarmMissionDefaults.ResolveMode(SwarmMissionTypeNames.Strike, null));
    }
}
