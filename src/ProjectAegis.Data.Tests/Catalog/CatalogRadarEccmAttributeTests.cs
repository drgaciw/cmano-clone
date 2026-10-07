using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>EW-01 / DRG-386: catalog ECCM attribute fields + validation.</summary>
public sealed class CatalogRadarEccmAttributeTests
{
    [Fact]
    public void Defaults_are_unspecified_so_existing_rows_are_unchanged()
    {
        var b = new CatalogSensorBinding("p1", "r1", 0.9);
        Assert.Equal(CatalogRadarScanTypes.Unspecified, b.RadarScanType);
        Assert.False(b.FrequencyAgile);
        Assert.Equal(0, b.RadarTechGeneration);
        Assert.Empty(CatalogRadarScanTypes.Validate(b));
    }

    [Theory]
    [InlineData("Mechanical")]
    [InlineData("pesa")]
    [InlineData("AESA")]
    public void Known_scan_types_validate(string scan)
    {
        Assert.Empty(CatalogRadarScanTypes.Validate(new CatalogSensorBinding("p1", "r1", 0.9, RadarScanType: scan, RadarTechGeneration: 4)));
    }

    [Fact]
    public void Invalid_attributes_report_errors()
    {
        Assert.Single(CatalogRadarScanTypes.Validate(new CatalogSensorBinding("p1", "r1", 0.9, RadarScanType: "Phased")));
        Assert.Single(CatalogRadarScanTypes.Validate(new CatalogSensorBinding("p1", "r1", 0.9, RadarTechGeneration: 9)));
        Assert.Single(CatalogRadarScanTypes.Validate(new CatalogSensorBinding(
            "p1", "ir1", 0.9, Modality: CatalogSensorModalities.Infrared, FrequencyAgile: true)));
    }

    [Fact]
    public void Json_import_reads_optional_eccm_fields()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-eccm-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, """
            { "importBatchId": "eccm", "sensors": [
              { "platformId": "p1", "sensorId": "r1", "basePd": 0.8, "radarScanType": "Aesa", "frequencyAgile": true, "radarTechGeneration": 5 },
              { "platformId": "p1", "sensorId": "r0", "basePd": 0.7 } ] }
            """);
            var rows = CatalogJsonImporter.ReadSensorBindings(path);
            Assert.Equal(new[] { "r0", "r1" }, rows.Select(r => r.SensorId));
            Assert.Equal(CatalogRadarScanTypes.Unspecified, rows[0].RadarScanType);
            Assert.Equal("Aesa", rows[1].RadarScanType);
            Assert.True(rows[1].FrequencyAgile);
            Assert.Equal(5, rows[1].RadarTechGeneration);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
