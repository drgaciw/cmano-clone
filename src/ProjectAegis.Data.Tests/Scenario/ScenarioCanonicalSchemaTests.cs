using ProjectAegis.Data.Scenario.Authoring;
using System.Text.Json;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class ScenarioCanonicalSchemaTests
{
    [Fact]
    public void Expanded_metadata_and_sections_round_trip_json()
    {
        var document = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                Title = "Baltic Patrol",
                Description = "Headless fixture",
                Author = "aegis-test",
                SchemaVersion = "1.0.0",
                DbRef = "baltic_patrol",
                Seed = 42,
                EditVersion = 1,
            },
            Features = new ScenarioFeaturesDto { MaxTimeCompression = 4.0 },
            Sides =
            [
                new ScenarioSideDto { Id = "blue", Name = "Blue", Score = 0 },
            ],
            Orbat = new ScenarioOrbatDto
            {
                Units =
                [
                    new ScenarioOrbatUnitDto
                    {
                        Id = "u1",
                        SideId = "blue",
                        Lat = 57.0,
                        Lon = 20.0,
                    },
                ],
            },
            ReferencePoints =
            [
                new ScenarioReferencePointDto
                {
                    Id = "rp-patrol",
                    Type = "polygon",
                    Geometry =
                    [
                        new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                        new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                        new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                    ],
                },
            ],
            Variables = new Dictionary<string, JsonElement>
            {
                ["alertLevel"] = JsonSerializer.SerializeToElement(1),
            },
            EditorState = new ScenarioEditorStateDto { CameraLat = 57.0, CameraLon = 20.0 },
        };

        var path = Path.Combine(Path.GetTempPath(), $"aegis-canonical-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentJsonWriter.WriteToFile(document, path);
            var loaded = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal("Baltic Patrol", loaded.Metadata.Title);
            Assert.Equal("1.0.0", loaded.Metadata.SchemaVersion);
            Assert.Single(loaded.Sides);
            Assert.NotNull(loaded.Orbat);
            Assert.Single(loaded.Orbat!.Units);
            Assert.Single(loaded.ReferencePoints);
            Assert.NotNull(loaded.Variables);
            Assert.NotNull(loaded.EditorState);
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
