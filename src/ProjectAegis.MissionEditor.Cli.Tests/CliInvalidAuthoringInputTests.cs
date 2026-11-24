using System.Text.Json;
using NUnit.Framework;
using ProjectAegis.Data.Scenario.Authoring;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>UAT CLI-7: authoring verbs reject invalid enum-like / geometry values and leave the file untouched.</summary>
[TestFixture]
public sealed class CliInvalidAuthoringInputTests
{
    private static ScenarioWaypointDto[] Wp(int count) =>
        Enumerable.Range(0, count).Select(i => new ScenarioWaypointDto { Lat = 54 + i * 0.1, Lon = 18 + i * 0.1 }).ToArray();

    [TestCase("bogus", 1, null, "INVALID_TYPE")]
    [TestCase("circle", 1, -3.0, "INVALID_GEOMETRY")]
    [TestCase("circle", 1, null, "INVALID_GEOMETRY")]
    [TestCase("polygon", 2, null, "INVALID_GEOMETRY")]
    public void reference_point_upsert_rejects_invalid_type_or_geometry(string type, int vertices, double? radiusNm, string expectedCode)
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-rp-invalid-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var before = File.ReadAllBytes(path);

            using var writer = new StringWriter();
            var code = ReferencePointUpsertCommand.Run(path, 1, "rp-1", type, Wp(vertices), radiusNm, writer);

            Assert.That(code, Is.EqualTo(1));
            using var doc = JsonDocument.Parse(writer.ToString());
            Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.False);
            Assert.That(doc.RootElement.GetProperty("code").GetString(), Is.EqualTo(expectedCode));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(before), "file must be unchanged");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void side_upsert_rejects_unknown_roe_and_leaves_file_unchanged()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-side-invalid-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var before = File.ReadAllBytes(path);

            using var writer = new StringWriter();
            var code = SideUpsertCommand.Run(path, 1, "blue", "Blue", "Bogus", null, null, writer);

            Assert.That(code, Is.EqualTo(1));
            using var doc = JsonDocument.Parse(writer.ToString());
            Assert.That(doc.RootElement.GetProperty("code").GetString(), Is.EqualTo("INVALID_ROE"));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(before), "file must be unchanged");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestCase("HoldFire")]
    [TestCase("weaponstight")]
    [TestCase("WeaponsFree")]
    public void side_upsert_accepts_known_roe_values(string roe)
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-side-valid-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var writer = new StringWriter();
            Assert.That(SideUpsertCommand.Run(path, 1, "blue", "Blue", roe, null, null, writer), Is.EqualTo(0));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
