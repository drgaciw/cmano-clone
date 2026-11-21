using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>
/// CLI/MCP parity tests for side_list / side_upsert / side_delete (ME-W3 / AME-4.5).
/// </summary>
[TestFixture]
public sealed class SideCliTests
{
    [Test]
    public void Side_list_returns_sides()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-side-list-{Guid.NewGuid():N}.json");
        try
        {
            var seeded = new ScenarioDocumentDto
            {
                Metadata = ScenarioDocumentEditor.CreateNew().ToDto().Metadata,
                Sides =
                [
                    new ScenarioSideDto
                    {
                        Id = "blue",
                        Name = "Blue Force",
                        DefaultRoe = "WeaponsFree",
                    },
                    new ScenarioSideDto
                    {
                        Id = "red",
                        Name = "Red Force",
                        DefaultRoe = "WeaponsHold",
                    },
                ],
            };
            ScenarioDocumentJsonWriter.WriteToFile(seeded, path);

            using var writer = new StringWriter();
            var code = SideListCommand.Run(path, writer);

            Assert.That(code, Is.EqualTo(0));
            var json = writer.ToString();
            Assert.That(json, Does.Contain("\"ok\":true"));
            Assert.That(json, Does.Contain("\"id\":\"blue\""));
            Assert.That(json, Does.Contain("\"id\":\"red\""));
            Assert.That(json, Does.Contain("Blue Force"));
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
    public void Side_upsert_persists_canonical_roe_name()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-side-roe-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var writer = new StringWriter();
            var code = SideUpsertCommand.Run(
                path, editVersion: 1, sideId: "blue", name: "Blue", defaultRoe: " weaponsfree ",
                defaultEmcon: null, postures: null, writer);

            Assert.That(code, Is.EqualTo(0), writer.ToString());
            var side = ScenarioDocumentJsonLoader.LoadFromFile(path).Sides.Single();
            Assert.That(side.DefaultRoe, Is.EqualTo("WeaponsFree"));
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
    public void Side_upsert_ok_bumps_edit_version_and_persists()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-side-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            using var writer = new StringWriter();
            var code = SideUpsertCommand.Run(
                path,
                editVersion: 1,
                sideId: "blue",
                name: "Blue Force",
                defaultRoe: "WeaponsFree",
                defaultEmcon: "Active",
                postures: ["defensive"],
                writer);

            Assert.That(code, Is.EqualTo(0));
            Assert.That(writer.ToString(), Does.Contain("\"ok\":true"));
            Assert.That(writer.ToString(), Does.Contain("\"sideId\":\"blue\""));

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(2));
            Assert.That(dto.Sides, Has.Exactly(1).Items);
            var side = dto.Sides.Single();
            Assert.That(side.Id, Is.EqualTo("blue"));
            Assert.That(side.Name, Is.EqualTo("Blue Force"));
            Assert.That(side.DefaultRoe, Is.EqualTo("WeaponsFree"));
            Assert.That(side.DefaultEmcon, Is.EqualTo("Active"));
            Assert.That(side.Postures, Is.EqualTo(new[] { "defensive" }));
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
    public void Side_upsert_stale_edit_version_returns_conflict()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-side-conflict-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var before = File.ReadAllText(path);

            using var writer = new StringWriter();
            var code = SideUpsertCommand.Run(
                path,
                editVersion: 99,
                sideId: "blue",
                name: "Blue",
                defaultRoe: null,
                defaultEmcon: null,
                postures: null,
                writer);

            Assert.That(code, Is.EqualTo(3));
            Assert.That(writer.ToString(), Does.Contain("CONFLICT"));
            Assert.That(File.ReadAllText(path), Is.EqualTo(before));
            Assert.That(ScenarioDocumentJsonLoader.LoadFromFile(path).Metadata.EditVersion, Is.EqualTo(1));
            Assert.That(ScenarioDocumentJsonLoader.LoadFromFile(path).Sides, Is.Empty);
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
    public void Side_delete_ok_removes_side()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-side-del-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using (var seed = new StringWriter())
            {
                Assert.That(SideUpsertCommand.Run(
                        path,
                        editVersion: 1,
                        sideId: "to-delete",
                        name: "Temp",
                        defaultRoe: null,
                        defaultEmcon: null,
                        postures: null,
                        seed), Is.EqualTo(0));
            }

            using var writer = new StringWriter();
            var code = SideDeleteCommand.Run(path, editVersion: 2, sideId: "to-delete", writer);

            Assert.That(code, Is.EqualTo(0));
            Assert.That(writer.ToString(), Does.Contain("\"ok\":true"));
            Assert.That(writer.ToString(), Does.Contain("\"deletedSideId\":\"to-delete\""));

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(3));
            Assert.That(dto.Sides, Is.Empty);
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
}
