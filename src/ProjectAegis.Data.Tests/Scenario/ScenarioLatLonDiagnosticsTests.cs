using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// S125-08 / AUTH-09: invalid or out-of-range lat/lon produce one actionable message (range, sign
/// convention, decimal example) instead of a bare rejection. Numeric validation only — no scenario parser.
/// </summary>
public sealed class ScenarioLatLonDiagnosticsTests
{
    [Test]
    public void Check_valid_position_returns_null()
    {
        Assert.That(ScenarioLatLonDiagnostics.Check(57.25, 20.5), Is.Null);
        Assert.That(ScenarioLatLonDiagnostics.Check(-90, 180), Is.Null);
    }

    [Test]
    public void Check_latitude_out_of_range_names_value_range_and_example()
    {
        var d = ScenarioLatLonDiagnostics.Check(95, 20);

        Assert.That(d, Is.Not.Null);
        Assert.That(d!.Code, Is.EqualTo(ScenarioLatLonDiagnostics.LatOutOfRange));
        Assert.That(d.Field, Is.EqualTo("lat"));
        Assert.That(d.Message, Does.Contain("95"));
        Assert.That(d.Message, Does.Contain("between -90 and 90"));
        Assert.That(d.Message, Does.Contain("south negative"));
        Assert.That(d.Message, Does.Contain("e.g."));
    }

    [Test]
    public void Check_longitude_out_of_range_names_value_range_and_example()
    {
        var d = ScenarioLatLonDiagnostics.Check(57, -181.5);

        Assert.That(d, Is.Not.Null);
        Assert.That(d!.Code, Is.EqualTo(ScenarioLatLonDiagnostics.LonOutOfRange));
        Assert.That(d.Field, Is.EqualTo("lon"));
        Assert.That(d.Message, Does.Contain("-181.5"));
        Assert.That(d.Message, Does.Contain("between -180 and 180"));
        Assert.That(d.Message, Does.Contain("west negative"));
    }

    [Test]
    public void Check_non_finite_is_reported()
    {
        var d = ScenarioLatLonDiagnostics.Check(double.NaN, 20);

        Assert.That(d, Is.Not.Null);
        Assert.That(d!.Code, Is.EqualTo(ScenarioLatLonDiagnostics.NotFinite));
        Assert.That(d.Message, Does.Contain("finite"));
    }
    [TestCase("57°15'N")]
    [TestCase("57,25")]
    [TestCase("")]
    public void CheckText_non_decimal_latitude_explains_decimal_degree_format(string latText)
    {
        var d = ScenarioLatLonDiagnostics.CheckText(latText, "20.5", out _, out _);

        Assert.That(d, Is.Not.Null);
        Assert.That(d!.Code, Is.EqualTo(ScenarioLatLonDiagnostics.NotNumeric));
        Assert.That(d.Field, Is.EqualTo("lat"));
        Assert.That(d.RawValue, Is.EqualTo(latText));
        Assert.That(d.Message, Does.Contain("decimal"));
        Assert.That(d.Message, Does.Contain("'.'"));
    }

    [Test]
    public void CheckText_valid_invariant_text_parses()
    {
        var d = ScenarioLatLonDiagnostics.CheckText("57.25", "-20.5", out var lat, out var lon);

        Assert.That(d, Is.Null);
        Assert.That(lat, Is.EqualTo(57.25));
        Assert.That(lon, Is.EqualTo(-20.5));
    }

    [Test]
    public void Editor_upsert_move_and_clone_reject_out_of_range_with_coded_exception()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto { Id = "u1", SideId = "blue", PlatformId = "u1", Lat = 57, Lon = 20 });

        var upsert = Assert.Throws<ScenarioLatLonException>(() => editor.UpsertOrbatUnit(
            new ScenarioOrbatUnitDto { Id = "u2", SideId = "blue", PlatformId = "u1", Lat = 91, Lon = 20 }));
        Assert.That(upsert!.Code, Is.EqualTo(ScenarioLatLonDiagnostics.LatOutOfRange));

        var move = Assert.Throws<ScenarioLatLonException>(() => editor.MoveOrbatUnit("u1", 57, 200));
        Assert.That(move!.Code, Is.EqualTo(ScenarioLatLonDiagnostics.LonOutOfRange));

        var clone = Assert.Throws<ScenarioLatLonException>(() => editor.CloneOrbatUnit("u1", "u1b", -95, 20));
        Assert.That(clone!.Code, Is.EqualTo(ScenarioLatLonDiagnostics.LatOutOfRange));

        var unit = ProjectAegis.Data.Tests.AssertEx.Single(editor.ToDto().Orbat!.Units);
        Assert.That(unit.Lat, Is.EqualTo(57));
        Assert.That(unit.Lon, Is.EqualTo(20));
    }

    [Test]
    public void Command_bus_surfaces_latlon_code_and_message()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-latlon-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto { Id = "u1", SideId = "blue", PlatformId = "u1", Lat = 57, Lon = 20 });
            editor.Save(path);

            using var session = ScenarioAuthoringSession.Open(path);
            var result = session.Bus.MoveUnit(session.EditVersion, "u1", 120, 20, save: false);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(ScenarioLatLonDiagnostics.LatOutOfRange));
            Assert.That(result.ErrorMessage, Does.Contain("between -90 and 90"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
