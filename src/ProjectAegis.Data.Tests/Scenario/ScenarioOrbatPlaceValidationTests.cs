using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>UAT validation gates for place-unit / ORBAT upsert (scenario editor).</summary>
public sealed class ScenarioOrbatPlaceValidationTests
{
    [Test]
    public void UpsertOrbatUnit_rejects_empty_platform_and_side()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        Assert.Throws<InvalidOperationException>(() =>
            editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "u1",
                SideId = "",
                PlatformId = "ffg",
                Lat = 57,
                Lon = 20,
            }));
        Assert.Throws<InvalidOperationException>(() =>
            editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "u1",
                SideId = "blue",
                PlatformId = "  ",
                Lat = 57,
                Lon = 20,
            }));
        Assert.That(editor.ToDto().Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>(), Is.Empty);
    }

    [Test]
    public void UpsertOrbatUnit_rejects_out_of_range_or_non_finite_coords()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var ex0 = Assert.Throws<ScenarioLatLonException>(() =>
            editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "u1",
                SideId = "blue",
                PlatformId = "ffg",
                Lat = 91,
                Lon = 20,
            }));
        Assert.That(ex0!.Code, Is.EqualTo(ScenarioLatLonDiagnostics.LatOutOfRange));
        var ex1 = Assert.Throws<ScenarioLatLonException>(() =>
            editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "u1",
                SideId = "blue",
                PlatformId = "ffg",
                Lat = 57,
                Lon = -181,
            }));
        Assert.That(ex1!.Code, Is.EqualTo(ScenarioLatLonDiagnostics.LonOutOfRange));
        var ex2 = Assert.Throws<ScenarioLatLonException>(() =>
            editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "u1",
                SideId = "blue",
                PlatformId = "ffg",
                Lat = double.NaN,
                Lon = 20,
            }));
        Assert.That(ex2!.Code, Is.EqualTo(ScenarioLatLonDiagnostics.NotFinite));
        Assert.That(editor.ToDto().Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>(), Is.Empty);
    }

    [Test]
    public void PlaceOrbatUnit_rejects_existing_id_without_replace()
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
        editor.CommitMutation();

        Assert.Throws<InvalidOperationException>(() =>
            editor.PlaceOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "u1",
                SideId = "red",
                PlatformId = "ssn",
                Lat = 10,
                Lon = 10,
            }));

        var u = ProjectAegis.Data.Tests.AssertEx.Single(editor.ToDto().Orbat!.Units);
        Assert.That(u.SideId, Is.EqualTo("blue"));
        Assert.That(u.PlatformId, Is.EqualTo("ffg"));
    }
}
