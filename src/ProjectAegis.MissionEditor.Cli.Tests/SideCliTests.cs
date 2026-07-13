using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>
/// CLI/MCP parity tests for side_list / side_upsert / side_delete (ME-W3 / AME-4.5).
/// </summary>
public sealed class SideCliTests
{
    [Fact]
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

            Assert.Equal(0, code);
            var json = writer.ToString();
            Assert.Contains("\"ok\":true", json);
            Assert.Contains("\"id\":\"blue\"", json);
            Assert.Contains("\"id\":\"red\"", json);
            Assert.Contains("Blue Force", json);
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

            Assert.Equal(0, code);
            Assert.Contains("\"ok\":true", writer.ToString());
            Assert.Contains("\"sideId\":\"blue\"", writer.ToString());

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, dto.Metadata.EditVersion);
            var side = Assert.Single(dto.Sides);
            Assert.Equal("blue", side.Id);
            Assert.Equal("Blue Force", side.Name);
            Assert.Equal("WeaponsFree", side.DefaultRoe);
            Assert.Equal("Active", side.DefaultEmcon);
            Assert.Equal(new[] { "defensive" }, side.Postures);
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

            Assert.Equal(3, code);
            Assert.Contains("CONFLICT", writer.ToString());
            Assert.Equal(before, File.ReadAllText(path));
            Assert.Equal(1, ScenarioDocumentJsonLoader.LoadFromFile(path).Metadata.EditVersion);
            Assert.Empty(ScenarioDocumentJsonLoader.LoadFromFile(path).Sides);
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
    public void Side_delete_ok_removes_side()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-side-del-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using (var seed = new StringWriter())
            {
                Assert.Equal(
                    0,
                    SideUpsertCommand.Run(
                        path,
                        editVersion: 1,
                        sideId: "to-delete",
                        name: "Temp",
                        defaultRoe: null,
                        defaultEmcon: null,
                        postures: null,
                        seed));
            }

            using var writer = new StringWriter();
            var code = SideDeleteCommand.Run(path, editVersion: 2, sideId: "to-delete", writer);

            Assert.Equal(0, code);
            Assert.Contains("\"ok\":true", writer.ToString());
            Assert.Contains("\"deletedSideId\":\"to-delete\"", writer.ToString());

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(3, dto.Metadata.EditVersion);
            Assert.Empty(dto.Sides);
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
