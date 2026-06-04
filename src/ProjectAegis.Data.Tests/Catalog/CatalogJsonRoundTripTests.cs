using System.Text.Json;
using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>Agent C — catalog import JSON contract stability.</summary>
public sealed class CatalogJsonRoundTripTests
{
    [Fact]
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
            Assert.Equal(2, bindings.Count);
            Assert.Equal("a-plat", bindings[0].PlatformId);
            Assert.Equal("z-plat", bindings[1].PlatformId);
            var zRow = bindings.Single(b => b.PlatformId == "z-plat");
            Assert.Equal("rt-batch", zRow.ImportBatchId);
            Assert.Equal(0.88, zRow.Confidence, 3);
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