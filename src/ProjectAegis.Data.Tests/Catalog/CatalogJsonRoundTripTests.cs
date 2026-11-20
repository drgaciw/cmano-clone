using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>Agent C — catalog import JSON contract stability.</summary>
public sealed class CatalogJsonRoundTripTests
{
    [Test]
    public void ReadSensorBindings_ignores_extra_fields_and_preserves_ordering()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-rt-{Guid.NewGuid():N}.json");
        const string json = """
            {
              "importBatchId": "rt-batch",
              "sensors": [
                {
                  "platformId": "z-plat",
                  "sensorId": "radar-z",
                  "basePd": 0.42,
                  "confidence": 0.88,
                  "reviewState": "approved",
                  "trlLevel": 7,
                  "futureField": "ignored"
                },
                {
                  "platformId": "a-plat",
                  "sensorId": "radar-a",
                  "basePd": 0.5
                }
              ]
            }
            """;
        try
        {
            File.WriteAllText(path, json);
            var bindings = CatalogJsonImporter.ReadSensorBindings(path);
            Assert.That(bindings.Count, Is.EqualTo(2));
            Assert.That(bindings[0].PlatformId, Is.EqualTo("a-plat"));
            Assert.That(bindings[1].PlatformId, Is.EqualTo("z-plat"));
            var zRow = bindings.Single(b => b.PlatformId == "z-plat");
            Assert.That(zRow.ImportBatchId, Is.EqualTo("rt-batch"));
            Assert.That(zRow.Confidence, Is.EqualTo(0.88).Within(0.0005));
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