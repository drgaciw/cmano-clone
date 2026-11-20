using ProjectAegis.Data.Scenario.Authoring;
using System.Text.Json;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class ScenarioCanonicalSchemaTests
{
    [Test]
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
            Assert.That(loaded.Metadata.Title, Is.EqualTo("Baltic Patrol"));
            Assert.That(loaded.Metadata.SchemaVersion, Is.EqualTo("1.0.0"));
            Assert.That(loaded.Sides, Has.Exactly(1).Items);
            Assert.That(loaded.Orbat, Is.Not.Null);
            Assert.That(loaded.Orbat!.Units, Has.Exactly(1).Items);
            Assert.That(loaded.ReferencePoints, Has.Exactly(1).Items);
            Assert.That(loaded.Variables, Is.Not.Null);
            Assert.That(loaded.EditorState, Is.Not.Null);
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
