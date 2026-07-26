using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>UAT validation gates for place-unit / ORBAT upsert (scenario editor).</summary>
public sealed class ScenarioOrbatPlaceValidationTests
{
    [Fact]
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
        Assert.Empty(editor.ToDto().Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>());
    }

    [Fact]
    public void UpsertOrbatUnit_rejects_out_of_range_or_non_finite_coords()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        Assert.Throws<InvalidOperationException>(() =>
            editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "u1",
                SideId = "blue",
                PlatformId = "ffg",
                Lat = 91,
                Lon = 20,
            }));
        Assert.Throws<InvalidOperationException>(() =>
            editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "u1",
                SideId = "blue",
                PlatformId = "ffg",
                Lat = 57,
                Lon = -181,
            }));
        Assert.Throws<InvalidOperationException>(() =>
            editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "u1",
                SideId = "blue",
                PlatformId = "ffg",
                Lat = double.NaN,
                Lon = 20,
            }));
        Assert.Empty(editor.ToDto().Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>());
    }

    [Fact]
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

        var u = Assert.Single(editor.ToDto().Orbat!.Units);
        Assert.Equal("blue", u.SideId);
        Assert.Equal("ffg", u.PlatformId);
    }
}
