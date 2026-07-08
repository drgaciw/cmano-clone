using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>
/// CLI/MCP parity tests for ORBAT place/move/clone and reference-point upsert verbs (P2.1 Task 7).
/// </summary>
public sealed class OrbatReferencePointCliTests
{
    [Fact]
    public void Orbat_upsert_unit_ok_bumps_edit_version()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-orbat-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            using var writer = new StringWriter();
            var code = OrbatUpsertUnitCommand.Run(
                path,
                editVersion: 1,
                unitId: "u-blue-1",
                sideId: "blue",
                platformId: "ffg-generic",
                lat: 57.05,
                lon: 20.15,
                writer);

            Assert.Equal(0, code);
            Assert.Contains("\"ok\":true", writer.ToString());
            Assert.Contains("\"unitId\":\"u-blue-1\"", writer.ToString());

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, dto.Metadata.EditVersion);
            Assert.NotNull(dto.Orbat);
            var unit = Assert.Single(dto.Orbat!.Units);
            Assert.Equal("u-blue-1", unit.Id);
            Assert.Equal("blue", unit.SideId);
            Assert.Equal("ffg-generic", unit.PlatformId);
            Assert.Equal(57.05, unit.Lat);
            Assert.Equal(20.15, unit.Lon);
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
    public void Orbat_move_stale_edit_version_returns_conflict()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-orbat-move-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using (var seedWriter = new StringWriter())
            {
                Assert.Equal(
                    0,
                    OrbatUpsertUnitCommand.Run(
                        path,
                        editVersion: 1,
                        unitId: "u1",
                        sideId: "blue",
                        platformId: "ffg",
                        lat: 57.0,
                        lon: 20.0,
                        seedWriter));
            }

            var before = File.ReadAllText(path);
            var beforeDto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, beforeDto.Metadata.EditVersion);

            using var writer = new StringWriter();
            var code = OrbatMoveUnitCommand.Run(
                path,
                editVersion: 99,
                unitId: "u1",
                lat: 58.0,
                lon: 21.0,
                writer);

            Assert.Equal(3, code);
            Assert.Contains("CONFLICT", writer.ToString());

            var after = File.ReadAllText(path);
            Assert.Equal(before, after);
            var afterDto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, afterDto.Metadata.EditVersion);
            var unit = Assert.Single(afterDto.Orbat!.Units);
            Assert.Equal(57.0, unit.Lat);
            Assert.Equal(20.0, unit.Lon);
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
    public void Reference_point_upsert_polygon_ok()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-rp-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            var geometry = new[]
            {
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                new ScenarioWaypointDto { Lat = 57.2, Lon = 20.0 },
            };

            using var writer = new StringWriter();
            var code = ReferencePointUpsertCommand.Run(
                path,
                editVersion: 1,
                referencePointId: "zone-patrol",
                type: "polygon",
                geometry,
                radiusNm: null,
                writer);

            Assert.Equal(0, code);
            Assert.Contains("\"ok\":true", writer.ToString());
            Assert.Contains("\"referencePointId\":\"zone-patrol\"", writer.ToString());
            Assert.Contains("\"type\":\"polygon\"", writer.ToString());

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, dto.Metadata.EditVersion);
            var rp = Assert.Single(dto.ReferencePoints);
            Assert.Equal("zone-patrol", rp.Id);
            Assert.Equal("polygon", rp.Type);
            Assert.Equal(3, rp.Geometry.Count);
            Assert.Equal(57.0, rp.Geometry[0].Lat);
            Assert.Equal(20.0, rp.Geometry[0].Lon);
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
