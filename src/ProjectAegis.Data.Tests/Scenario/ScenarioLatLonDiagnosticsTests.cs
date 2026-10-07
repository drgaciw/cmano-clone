using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// S125-08 / AUTH-09: invalid or out-of-range lat/lon produce one actionable message (range, sign
/// convention, decimal example) instead of a bare rejection. Numeric validation only — no scenario parser.
/// </summary>
public sealed class ScenarioLatLonDiagnosticsTests
{
    [Fact]
    public void Check_valid_position_returns_null()
    {
        Assert.Null(ScenarioLatLonDiagnostics.Check(57.25, 20.5));
        Assert.Null(ScenarioLatLonDiagnostics.Check(-90, 180));
    }

    [Fact]
    public void Check_latitude_out_of_range_names_value_range_and_example()
    {
        var d = ScenarioLatLonDiagnostics.Check(95, 20);

        Assert.NotNull(d);
        Assert.Equal(ScenarioLatLonDiagnostics.LatOutOfRange, d.Code);
        Assert.Equal("lat", d.Field);
        Assert.Contains("95", d.Message, StringComparison.Ordinal);
        Assert.Contains("between -90 and 90", d.Message, StringComparison.Ordinal);
        Assert.Contains("south negative", d.Message, StringComparison.Ordinal);
        Assert.Contains("e.g.", d.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_longitude_out_of_range_names_value_range_and_example()
    {
        var d = ScenarioLatLonDiagnostics.Check(57, -181.5);

        Assert.NotNull(d);
        Assert.Equal(ScenarioLatLonDiagnostics.LonOutOfRange, d.Code);
        Assert.Equal("lon", d.Field);
        Assert.Contains("-181.5", d.Message, StringComparison.Ordinal);
        Assert.Contains("between -180 and 180", d.Message, StringComparison.Ordinal);
        Assert.Contains("west negative", d.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_non_finite_is_reported()
    {
        var d = ScenarioLatLonDiagnostics.Check(double.NaN, 20);

        Assert.NotNull(d);
        Assert.Equal(ScenarioLatLonDiagnostics.NotFinite, d.Code);
        Assert.Contains("finite", d.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("57°15'N")]
    [InlineData("57,25")]
    [InlineData("")]
    public void CheckText_non_decimal_latitude_explains_decimal_degree_format(string latText)
    {
        var d = ScenarioLatLonDiagnostics.CheckText(latText, "20.5", out _, out _);

        Assert.NotNull(d);
        Assert.Equal(ScenarioLatLonDiagnostics.NotNumeric, d.Code);
        Assert.Equal("lat", d.Field);
        Assert.Equal(latText, d.RawValue);
        Assert.Contains("decimal", d.Message, StringComparison.Ordinal);
        Assert.Contains("'.'", d.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckText_valid_invariant_text_parses()
    {
        var d = ScenarioLatLonDiagnostics.CheckText("57.25", "-20.5", out var lat, out var lon);

        Assert.Null(d);
        Assert.Equal(57.25, lat);
        Assert.Equal(-20.5, lon);
    }

    [Fact]
    public void Editor_upsert_move_and_clone_reject_out_of_range_with_coded_exception()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto { Id = "u1", SideId = "blue", PlatformId = "u1", Lat = 57, Lon = 20 });

        var upsert = Assert.Throws<ScenarioLatLonException>(() => editor.UpsertOrbatUnit(
            new ScenarioOrbatUnitDto { Id = "u2", SideId = "blue", PlatformId = "u1", Lat = 91, Lon = 20 }));
        Assert.Equal(ScenarioLatLonDiagnostics.LatOutOfRange, upsert.Code);

        var move = Assert.Throws<ScenarioLatLonException>(() => editor.MoveOrbatUnit("u1", 57, 200));
        Assert.Equal(ScenarioLatLonDiagnostics.LonOutOfRange, move.Code);

        var clone = Assert.Throws<ScenarioLatLonException>(() => editor.CloneOrbatUnit("u1", "u1b", -95, 20));
        Assert.Equal(ScenarioLatLonDiagnostics.LatOutOfRange, clone.Code);

        var unit = Assert.Single(editor.ToDto().Orbat!.Units);
        Assert.Equal(57, unit.Lat);
        Assert.Equal(20, unit.Lon);
    }

    [Fact]
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

            Assert.False(result.Ok);
            Assert.Equal(ScenarioLatLonDiagnostics.LatOutOfRange, result.ErrorCode);
            Assert.Contains("between -90 and 90", result.ErrorMessage, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
