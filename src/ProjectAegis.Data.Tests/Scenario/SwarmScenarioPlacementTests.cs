using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>DRG-101 / SWARM-B9: scenario editor place/configure swarm (SWARM-22).</summary>
public sealed class SwarmScenarioPlacementTests
{
    [Fact]
    public void Place_swarm_with_count_within_max_succeeds()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertSide(new ScenarioSideDto { Id = "blue", Name = "Blue" });
        var unit = editor.PlaceSwarmUnit(
            "swarm-1",
            "blue",
            "usn-cec-swarm",
            57.0,
            20.0,
            droneCount: 12,
            maxDrones: 24);
        Assert.Equal(12, unit.DroneCount);
        Assert.Equal("usn-cec-swarm", unit.PlatformId);
        Assert.Single(editor.ToDto().Orbat!.Units);
    }

    [Fact]
    public void Place_with_count_over_max_fails()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            editor.PlaceSwarmUnit(
                "swarm-1",
                "blue",
                "usn-cec-swarm",
                57.0,
                20.0,
                droneCount: 30,
                maxDrones: 24));
        Assert.Contains(SwarmScenarioValidation.CountExceedsMax, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_assign_persists_on_dto()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.PlaceOrbatUnit(new ScenarioOrbatUnitDto
        {
            Id = "host-ship",
            SideId = "blue",
            PlatformId = "ddg",
            Lat = 57.0,
            Lon = 19.9,
        });
        var swarm = editor.PlaceSwarmUnit(
            "swarm-1",
            "blue",
            "usn-cec-swarm",
            57.0,
            20.0,
            droneCount: 8,
            hostUnitId: "host-ship",
            maxDrones: 24);
        Assert.Equal("host-ship", swarm.HostUnitId);
        Assert.Equal("host-ship", editor.ToDto().Orbat!.Units.Single(u => u.Id == "swarm-1").HostUnitId);
    }

    [Fact]
    public void Round_trip_json_preserves_DroneCount_and_HostUnitId()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-swarm-scenario-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.PlaceOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "host-ship",
                SideId = "blue",
                PlatformId = "ddg",
                Lat = 57.0,
                Lon = 19.9,
            });
            editor.PlaceSwarmUnit(
                "swarm-1",
                "blue",
                "usn-cec-swarm",
                57.1,
                20.1,
                droneCount: 15,
                hostUnitId: "host-ship",
                maxDrones: 24);
            editor.CommitMutation();
            editor.Save(path);

            var loaded = ScenarioDocumentJsonLoader.LoadFromFile(path);
            var swarm = loaded.Orbat!.Units.Single(u => u.Id == "swarm-1");
            Assert.Equal(15, swarm.DroneCount);
            Assert.Equal("host-ship", swarm.HostUnitId);
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
    public void Missing_platform_id_fails()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            editor.PlaceSwarmUnit("swarm-1", "blue", "  ", 57.0, 20.0, droneCount: 5, maxDrones: 10));
        Assert.Contains(SwarmScenarioValidation.PlatformMissing, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Configure_updates_count()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.PlaceSwarmUnit("swarm-1", "blue", "usn-cec-swarm", 57.0, 20.0, droneCount: 5, maxDrones: 20);
        editor.ConfigureSwarmUnit("swarm-1", droneCount: 10, maxDrones: 20);
        Assert.Equal(10, editor.ToDto().Orbat!.Units[0].DroneCount);
    }

    [Fact]
    public void Missing_host_fails_validation()
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
                hostUnitId: "no-such-host",
                maxDrones: 20));
        Assert.Contains(SwarmScenarioValidation.HostMissing, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Pure_validation_count_exceeds_max()
    {
        var r = SwarmScenarioValidation.ValidatePlacement("p", 25, 10, null, true);
        Assert.False(r.IsValid);
        Assert.Equal(SwarmScenarioValidation.CountExceedsMax, r.ErrorCode);
    }
}
