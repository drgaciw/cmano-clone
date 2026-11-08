using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// Side/faction CRUD on <see cref="ScenarioDocumentEditor"/> and
/// <see cref="ScenarioEditCommandBus"/> (ME-W3 / AME-4.5).
/// </summary>
public sealed class ScenarioSideEditorTests
{
    [Fact]
    public void UpsertSide_inserts_deep_copies_postures()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var postures = new List<string> { "defensive", "alert" };

        editor.UpsertSide(new ScenarioSideDto
        {
            Id = "blue",
            Name = "Blue Force",
            DefaultRoe = "WeaponsFree",
            DefaultEmcon = "Active",
            Postures = postures,
        });

        var side = Assert.Single(editor.ToDto().Sides);
        Assert.Equal("blue", side.Id);
        Assert.Equal("Blue Force", side.Name);
        Assert.Equal("WeaponsFree", side.DefaultRoe);
        Assert.Equal("Active", side.DefaultEmcon);
        Assert.Equal(new[] { "defensive", "alert" }, side.Postures);

        // Mutating the source list must not affect the stored side (deep copy).
        postures[0] = "offensive";
        postures.Clear();
        Assert.Equal(new[] { "defensive", "alert" }, editor.ToDto().Sides[0].Postures);
    }

    [Fact]
    public void UpsertSide_replaces_by_case_insensitive_id()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertSide(new ScenarioSideDto
        {
            Id = "Blue",
            Name = "Blue Force",
            DefaultRoe = "WeaponsHold",
        });

        editor.UpsertSide(new ScenarioSideDto
        {
            Id = "blue",
            Name = "NATO Blue",
            DefaultRoe = "WeaponsFree",
            DefaultEmcon = "Silent",
            Postures = ["patrol"],
        });

        var side = Assert.Single(editor.ToDto().Sides);
        Assert.Equal("blue", side.Id);
        Assert.Equal("NATO Blue", side.Name);
        Assert.Equal("WeaponsFree", side.DefaultRoe);
        Assert.Equal("Silent", side.DefaultEmcon);
        Assert.Equal(new[] { "patrol" }, side.Postures);
    }

    [Fact]
    public void UpsertSide_blank_id_throws()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        Assert.Throws<InvalidOperationException>(() =>
            editor.UpsertSide(new ScenarioSideDto { Id = "  ", Name = "X" }));
    }

    [Fact]
    public void TryRemoveSide_removes_without_cascading_orbat_units()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertSide(new ScenarioSideDto { Id = "blue", Name = "Blue" });
        editor.UpsertSide(new ScenarioSideDto { Id = "red", Name = "Red" });
        editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
        {
            Id = "u1",
            SideId = "blue",
            PlatformId = "ffg",
            Lat = 57,
            Lon = 20,
        });

        Assert.True(editor.TryRemoveSide("BLUE"));
        Assert.False(editor.TryRemoveSide("missing"));

        var sides = editor.ToDto().Sides;
        Assert.Single(sides);
        Assert.Equal("red", sides[0].Id);

        // ORBAT unit retained with dangling sideId (no cascade).
        var unit = Assert.Single(editor.ToDto().Orbat!.Units);
        Assert.Equal("u1", unit.Id);
        Assert.Equal("blue", unit.SideId);
    }

    [Fact]
    public void UpsertSide_round_trips_json_and_undo_restores_sides()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-side-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.UpsertSide(new ScenarioSideDto
            {
                Id = "blue",
                Name = "Blue Force",
                DefaultRoe = "WeaponsFree",
                Postures = ["defensive"],
            });
            editor.Save(path);

            var loaded = ScenarioDocumentEditor.Load(path);
            loaded.RequireEditVersion(1);
            var undo = loaded.CaptureUndoSnapshot();
            loaded.UpsertSide(new ScenarioSideDto
            {
                Id = "red",
                Name = "Red Force",
                DefaultRoe = "WeaponsHold",
            });
            loaded.PersistUndoSnapshot(path, undo);
            loaded.CommitMutation();
            loaded.Save(path);

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, dto.Metadata.EditVersion);
            Assert.Equal(2, dto.Sides.Count);
            Assert.Contains(dto.Sides, s => s.Id == "red");

            Assert.True(loaded.PopUndo(path));
            var restored = ScenarioDocumentJsonLoader.LoadFromFile(path);
            var side = Assert.Single(restored.Sides);
            Assert.Equal("blue", side.Id);
            Assert.Equal("Blue Force", side.Name);
            Assert.Equal(new[] { "defensive" }, side.Postures);
            Assert.DoesNotContain(restored.Sides, s => s.Id == "red");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var undoPath = path + ".undo-stack.json";
            if (File.Exists(undoPath))
            {
                File.Delete(undoPath);
            }
        }
    }

    [Fact]
    public void Bus_UpsertSide_and_DeleteSide_bump_edit_version()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-side-bus-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);

            var upsert = session.Bus.UpsertSide(
                expectedEditVersion: 1,
                new ScenarioSideDto
                {
                    Id = "blue",
                    Name = "Blue Force",
                    DefaultRoe = "WeaponsFree",
                },
                save: true);

            Assert.True(upsert.Ok);
            Assert.Equal(2, upsert.EditVersion);
            Assert.Single(session.Editor.ToDto().Sides);

            var del = session.Bus.DeleteSide(session.EditVersion, "blue", save: true);
            Assert.True(del.Ok);
            Assert.Equal(3, del.EditVersion);
            Assert.Empty(session.Editor.ToDto().Sides);
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
    public void Bus_UpsertSide_stale_edit_version_returns_conflict()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-side-bus-c-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);

            var result = session.Bus.UpsertSide(
                expectedEditVersion: 99,
                new ScenarioSideDto { Id = "blue", Name = "Blue" },
                save: true);

            Assert.False(result.Ok);
            Assert.Equal(ScenarioEditVersionGuard.ConflictCode, result.ErrorCode);
            Assert.Empty(session.Editor.ToDto().Sides);
            Assert.Equal(1, ScenarioDocumentJsonLoader.LoadFromFile(path).Metadata.EditVersion);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
