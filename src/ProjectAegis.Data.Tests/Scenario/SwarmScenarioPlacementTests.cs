using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>DRG-101 / SWARM-B9: scenario editor place/configure swarm (SWARM-22).</summary>
public sealed class SwarmScenarioPlacementTests
{
    [Test]
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
        Assert.That(unit.DroneCount, Is.EqualTo(12));
        Assert.That(unit.PlatformId, Is.EqualTo("usn-cec-swarm"));
        Assert.That(editor.ToDto().Orbat!.Units, Has.Exactly(1).Items);
    }

    [Test]
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
        Assert.That(ex!.Message, Does.Contain(SwarmScenarioValidation.CountExceedsMax));
    }

    [Test]
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
        Assert.That(swarm.HostUnitId, Is.EqualTo("host-ship"));
        Assert.That(editor.ToDto().Orbat!.Units.Single(u => u.Id == "swarm-1").HostUnitId, Is.EqualTo("host-ship"));
    }

    [Test]
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
            Assert.That(swarm.DroneCount, Is.EqualTo(15));
            Assert.That(swarm.HostUnitId, Is.EqualTo("host-ship"));
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
    public void Missing_platform_id_fails()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            editor.PlaceSwarmUnit("swarm-1", "blue", "  ", 57.0, 20.0, droneCount: 5, maxDrones: 10));
        Assert.That(ex!.Message, Does.Contain(SwarmScenarioValidation.PlatformMissing));
    }

    [Test]
    public void Configure_updates_count()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.PlaceSwarmUnit("swarm-1", "blue", "usn-cec-swarm", 57.0, 20.0, droneCount: 5, maxDrones: 20);
        editor.ConfigureSwarmUnit("swarm-1", droneCount: 10, maxDrones: 20);
        Assert.That(editor.ToDto().Orbat!.Units[0].DroneCount, Is.EqualTo(10));
    }

    [Test]
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
        Assert.That(ex!.Message, Does.Contain(SwarmScenarioValidation.HostMissing));
    }

    [Test]
    public void Pure_validation_count_exceeds_max()
    {
        var r = SwarmScenarioValidation.ValidatePlacement("p", 25, 10, null, true);
        Assert.That(r.IsValid, Is.False);
        Assert.That(r.ErrorCode, Is.EqualTo(SwarmScenarioValidation.CountExceedsMax));
    }
}
