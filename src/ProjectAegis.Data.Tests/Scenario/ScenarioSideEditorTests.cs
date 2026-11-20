using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// Side/faction CRUD on <see cref="ScenarioDocumentEditor"/> and
/// <see cref="ScenarioEditCommandBus"/> (ME-W3 / AME-4.5).
/// </summary>
public sealed class ScenarioSideEditorTests
{
    [Test]
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

        var side = ProjectAegis.Data.Tests.AssertEx.Single(editor.ToDto().Sides);
        Assert.That(side.Id, Is.EqualTo("blue"));
        Assert.That(side.Name, Is.EqualTo("Blue Force"));
        Assert.That(side.DefaultRoe, Is.EqualTo("WeaponsFree"));
        Assert.That(side.DefaultEmcon, Is.EqualTo("Active"));
        Assert.That(side.Postures, Is.EqualTo(new[] { "defensive", "alert" }));

        // Mutating the source list must not affect the stored side (deep copy).
        postures[0] = "offensive";
        postures.Clear();
        Assert.That(editor.ToDto().Sides[0].Postures, Is.EqualTo(new[] { "defensive", "alert" }));
    }

    [Test]
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

        var side = ProjectAegis.Data.Tests.AssertEx.Single(editor.ToDto().Sides);
        Assert.That(side.Id, Is.EqualTo("blue"));
        Assert.That(side.Name, Is.EqualTo("NATO Blue"));
        Assert.That(side.DefaultRoe, Is.EqualTo("WeaponsFree"));
        Assert.That(side.DefaultEmcon, Is.EqualTo("Silent"));
        Assert.That(side.Postures, Is.EqualTo(new[] { "patrol" }));
    }

    [Test]
    public void UpsertSide_blank_id_throws()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        Assert.Throws<InvalidOperationException>(() =>
            editor.UpsertSide(new ScenarioSideDto { Id = "  ", Name = "X" }));
    }

    [Test]
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

        Assert.That(editor.TryRemoveSide("BLUE"), Is.True);
        Assert.That(editor.TryRemoveSide("missing"), Is.False);

        var sides = editor.ToDto().Sides;
        Assert.That(sides, Has.Exactly(1).Items);
        Assert.That(sides[0].Id, Is.EqualTo("red"));

        // ORBAT unit retained with dangling sideId (no cascade).
        var unit = ProjectAegis.Data.Tests.AssertEx.Single(editor.ToDto().Orbat!.Units);
        Assert.That(unit.Id, Is.EqualTo("u1"));
        Assert.That(unit.SideId, Is.EqualTo("blue"));
    }

    [Test]
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
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(2));
            Assert.That(dto.Sides.Count, Is.EqualTo(2));
            Assert.That(dto.Sides.Any(s => s.Id == "red"), Is.True);

            Assert.That(loaded.PopUndo(path), Is.True);
            var restored = ScenarioDocumentJsonLoader.LoadFromFile(path);
            var side = ProjectAegis.Data.Tests.AssertEx.Single(restored.Sides);
            Assert.That(side.Id, Is.EqualTo("blue"));
            Assert.That(side.Name, Is.EqualTo("Blue Force"));
            Assert.That(side.Postures, Is.EqualTo(new[] { "defensive" }));
            Assert.That(restored.Sides.Any(s => s.Id == "red"), Is.False);
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

    [Test]
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

            Assert.That(upsert.Ok, Is.True);
            Assert.That(upsert.EditVersion, Is.EqualTo(2));
            Assert.That(session.Editor.ToDto().Sides, Has.Exactly(1).Items);

            var del = session.Bus.DeleteSide(session.EditVersion, "blue", save: true);
            Assert.That(del.Ok, Is.True);
            Assert.That(del.EditVersion, Is.EqualTo(3));
            Assert.That(session.Editor.ToDto().Sides, Is.Empty);
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

            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(ScenarioEditVersionGuard.ConflictCode));
            Assert.That(session.Editor.ToDto().Sides, Is.Empty);
            Assert.That(ScenarioDocumentJsonLoader.LoadFromFile(path).Metadata.EditVersion, Is.EqualTo(1));
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
