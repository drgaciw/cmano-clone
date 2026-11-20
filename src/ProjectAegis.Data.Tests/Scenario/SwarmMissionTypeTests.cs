using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>DRG-109 / SWARM-C5: mission types for swarm tasking (SWARM-20).</summary>
public sealed class SwarmMissionTypeTests
{
    [TestCase(SwarmMissionType.Patrol, CatalogSwarmPlatformDefaults.ModeHold)]
    [TestCase(SwarmMissionType.Support, CatalogSwarmPlatformDefaults.ModeScreen)]
    [TestCase(SwarmMissionType.Strike, CatalogSwarmPlatformDefaults.ModeAssault)]
    public void DefaultMode_maps_mission_to_phase_b_mode(SwarmMissionType mission, string expectedMode)
    {
        Assert.That(SwarmMissionDefaults.DefaultMode(mission), Is.EqualTo(expectedMode));
        Assert.That(SwarmMissionDefaults.DefaultMode(SwarmMissionTypeNames.ToName(mission)), Is.EqualTo(expectedMode));
    }

    [Test]
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
        Assert.That(unit.MissionType, Is.EqualTo(SwarmMissionTypeNames.Patrol));
        Assert.That(unit.Mode, Is.EqualTo(CatalogSwarmPlatformDefaults.ModeHold));
    }

    [Test]
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
        Assert.That(unit.MissionType, Is.EqualTo(SwarmMissionTypeNames.Support));
        Assert.That(unit.Mode, Is.EqualTo(CatalogSwarmPlatformDefaults.ModeScreen));
    }

    [Test]
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
        Assert.That(unit.MissionType, Is.EqualTo(SwarmMissionTypeNames.Strike));
        Assert.That(unit.Mode, Is.EqualTo(CatalogSwarmPlatformDefaults.ModeAssault));
    }

    [Test]
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
        Assert.That(unit.MissionType, Is.EqualTo(SwarmMissionTypeNames.Strike));
        Assert.That(unit.Mode, Is.EqualTo(CatalogSwarmPlatformDefaults.ModeScatter));
    }

    [Test]
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
        Assert.That(ex!.Message, Does.Contain(SwarmScenarioValidation.MissionTypeUnknown));
    }

    [Test]
    public void Pure_validation_rejects_unknown_mission_type()
    {
        var r = SwarmScenarioValidation.ValidatePlacement(
            "p",
            5,
            10,
            null,
            true,
            missionType: "BombingRun");
        Assert.That(r.IsValid, Is.False);
        Assert.That(r.ErrorCode, Is.EqualTo(SwarmScenarioValidation.MissionTypeUnknown));
    }

    [Test]
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
            Assert.That(swarm.MissionType, Is.EqualTo(SwarmMissionTypeNames.Support));
            Assert.That(swarm.Mode, Is.EqualTo(CatalogSwarmPlatformDefaults.ModeScreen));
            Assert.That(swarm.DroneCount, Is.EqualTo(15));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void Configure_mission_type_applies_default_mode()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.PlaceSwarmUnit("swarm-1", "blue", "usn-cec-swarm", 57.0, 20.0, droneCount: 5, maxDrones: 20);
        editor.ConfigureSwarmUnit("swarm-1", missionType: SwarmMissionTypeNames.Patrol, maxDrones: 20);
        var unit = editor.ToDto().Orbat!.Units[0];
        Assert.That(unit.MissionType, Is.EqualTo(SwarmMissionTypeNames.Patrol));
        Assert.That(unit.Mode, Is.EqualTo(CatalogSwarmPlatformDefaults.ModeHold));
    }

    [Test]
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
        Assert.That(unit.MissionType, Is.Null);
        Assert.That(unit.Mode, Is.Null);
    }

    [Test]
    public void ResolveMode_prefers_explicit_over_default()
    {
        Assert.That(            SwarmMissionDefaults.ResolveMode(SwarmMissionTypeNames.Strike, CatalogSwarmPlatformDefaults.ModeRejoin), Is.EqualTo(            CatalogSwarmPlatformDefaults.ModeRejoin));
        Assert.That(            SwarmMissionDefaults.ResolveMode(SwarmMissionTypeNames.Strike, null), Is.EqualTo(            CatalogSwarmPlatformDefaults.ModeAssault));
    }
}
