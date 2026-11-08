using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// ORBAT place/move/clone and reference-point upsert/remove mutations on
/// <see cref="ScenarioDocumentEditor"/> (Phase 2.1 / map-first authoring).
/// </summary>
public sealed class ScenarioOrbatReferencePointEditorTests
{
    [Fact]
    public void UpsertUnit_places_unit_and_round_trips_json()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-orbat-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.Save(path);

            var loaded = ScenarioDocumentEditor.Load(path);
            loaded.RequireEditVersion(1);
            var undo = loaded.CaptureUndoSnapshot();
            loaded.UpsertOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "u-blue-1",
                SideId = "blue",
                PlatformId = "ffg-generic",
                Lat = 57.05,
                Lon = 20.15,
            });
            loaded.PersistUndoSnapshot(path, undo);
            loaded.CommitMutation();
            loaded.Save(path);

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, dto.Metadata.EditVersion);
            Assert.NotNull(dto.Orbat);
            Assert.Single(dto.Orbat!.Units);
            Assert.Equal("u-blue-1", dto.Orbat.Units[0].Id);
            Assert.Equal(57.05, dto.Orbat.Units[0].Lat);
            Assert.Equal(20.15, dto.Orbat.Units[0].Lon);
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
    public void MoveUnit_updates_lat_lon_preserves_other_fields()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
        {
            Id = "u1",
            SideId = "blue",
            PlatformId = "ffg",
            Lat = 57,
            Lon = 20,
            RoeOverride = "WeaponsHold",
        });
        editor.MoveOrbatUnit("u1", lat: 58.0, lon: 21.0);

        var u = editor.ToDto().Orbat!.Units.Single(x => x.Id == "u1");
        Assert.Equal(58.0, u.Lat);
        Assert.Equal(21.0, u.Lon);
        Assert.Equal("WeaponsHold", u.RoeOverride);
        Assert.Equal("blue", u.SideId);
    }

    [Fact]
    public void CloneUnit_creates_new_id_at_offset_position()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
        {
            Id = "u1",
            SideId = "blue",
            PlatformId = "ffg",
            Lat = 57,
            Lon = 20,
        });
        editor.CloneOrbatUnit("u1", newUnitId: "u1-clone", lat: 57.01, lon: 20.01);

        var units = editor.ToDto().Orbat!.Units;
        Assert.Equal(2, units.Count);
        Assert.Contains(units, u => u.Id == "u1-clone" && u.PlatformId == "ffg");
    }

    [Fact]
    public void CloneUnit_duplicate_id_throws()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
        {
            Id = "u1",
            SideId = "blue",
            PlatformId = "ffg",
            Lat = 57,
            Lon = 20,
        });
        Assert.Throws<InvalidOperationException>(() =>
            editor.CloneOrbatUnit("u1", newUnitId: "u1", lat: 57, lon: 20));
    }

    [Fact]
    public void UpsertReferencePoint_round_trips_polygon()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-rp-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.Save(path);
            var loaded = ScenarioDocumentEditor.Load(path);
            loaded.RequireEditVersion(1);
            loaded.UpsertReferencePoint(new ScenarioReferencePointDto
            {
                Id = "zone-patrol",
                Type = "polygon",
                Geometry =
                [
                    new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.2, Lon = 20.0 },
                ],
            });
            loaded.CommitMutation();
            loaded.Save(path);

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Single(dto.ReferencePoints);
            Assert.Equal("polygon", dto.ReferencePoints[0].Type);
            Assert.Equal(3, dto.ReferencePoints[0].Geometry.Count);
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
    public void RemoveReferencePoint_removes_by_id()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertReferencePoint(new ScenarioReferencePointDto
        {
            Id = "rp1",
            Type = "point",
            Geometry = [new ScenarioWaypointDto { Lat = 1, Lon = 2 }],
        });
        Assert.True(editor.TryRemoveReferencePoint("rp1"));
        Assert.Empty(editor.ToDto().ReferencePoints);
    }

    [Fact]
    public void MoveUnit_missing_throws()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        Assert.Throws<InvalidOperationException>(() => editor.MoveOrbatUnit("missing", 0, 0));
    }
}
