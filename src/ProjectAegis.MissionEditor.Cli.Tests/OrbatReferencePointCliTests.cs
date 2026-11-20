using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>
/// CLI/MCP parity tests for ORBAT place/move/clone and reference-point upsert verbs (P2.1 Task 7).
/// </summary>
[TestFixture]
public sealed class OrbatReferencePointCliTests
{
    [Test]
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

            Assert.That(code, Is.EqualTo(0));
            Assert.That(writer.ToString(), Does.Contain("\"ok\":true"));
            Assert.That(writer.ToString(), Does.Contain("\"unitId\":\"u-blue-1\""));

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(2));
            Assert.That(dto.Orbat, Is.Not.Null);
            Assert.That(dto.Orbat!.Units, Has.Exactly(1).Items);
            var unit = dto.Orbat!.Units.Single();
            Assert.That(unit.Id, Is.EqualTo("u-blue-1"));
            Assert.That(unit.SideId, Is.EqualTo("blue"));
            Assert.That(unit.PlatformId, Is.EqualTo("ffg-generic"));
            Assert.That(unit.Lat, Is.EqualTo(57.05));
            Assert.That(unit.Lon, Is.EqualTo(20.15));
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
    public void Orbat_move_stale_edit_version_returns_conflict()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-orbat-move-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using (var seedWriter = new StringWriter())
            {
                Assert.That(OrbatUpsertUnitCommand.Run(
                        path,
                        editVersion: 1,
                        unitId: "u1",
                        sideId: "blue",
                        platformId: "ffg",
                        lat: 57.0,
                        lon: 20.0,
                        seedWriter), Is.EqualTo(0));
            }

            var before = File.ReadAllText(path);
            var beforeDto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(beforeDto.Metadata.EditVersion, Is.EqualTo(2));

            using var writer = new StringWriter();
            var code = OrbatMoveUnitCommand.Run(
                path,
                editVersion: 99,
                unitId: "u1",
                lat: 58.0,
                lon: 21.0,
                writer);

            Assert.That(code, Is.EqualTo(3));
            Assert.That(writer.ToString(), Does.Contain("CONFLICT"));

            var after = File.ReadAllText(path);
            Assert.That(after, Is.EqualTo(before));
            var afterDto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(afterDto.Metadata.EditVersion, Is.EqualTo(2));
            Assert.That(afterDto.Orbat!.Units, Has.Exactly(1).Items);
            var unit = afterDto.Orbat!.Units.Single();
            Assert.That(unit.Lat, Is.EqualTo(57.0));
            Assert.That(unit.Lon, Is.EqualTo(20.0));
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

            Assert.That(code, Is.EqualTo(0));
            Assert.That(writer.ToString(), Does.Contain("\"ok\":true"));
            Assert.That(writer.ToString(), Does.Contain("\"referencePointId\":\"zone-patrol\""));
            Assert.That(writer.ToString(), Does.Contain("\"type\":\"polygon\""));

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(2));
            Assert.That(dto.ReferencePoints, Has.Exactly(1).Items);
            var rp = dto.ReferencePoints.Single();
            Assert.That(rp.Id, Is.EqualTo("zone-patrol"));
            Assert.That(rp.Type, Is.EqualTo("polygon"));
            Assert.That(rp.Geometry.Count, Is.EqualTo(3));
            Assert.That(rp.Geometry[0].Lat, Is.EqualTo(57.0));
            Assert.That(rp.Geometry[0].Lon, Is.EqualTo(20.0));
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
