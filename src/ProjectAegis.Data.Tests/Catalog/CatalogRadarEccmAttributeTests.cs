using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>EW-01 / DRG-386: catalog ECCM attribute fields + validation.</summary>
public sealed class CatalogRadarEccmAttributeTests
{
    [Test]
    public void Defaults_are_unspecified_so_existing_rows_are_unchanged()
    {
        var b = new CatalogSensorBinding("p1", "r1", 0.9);
        Assert.That(b.RadarScanType, Is.EqualTo(CatalogRadarScanTypes.Unspecified));
        Assert.That(b.FrequencyAgile, Is.False);
        Assert.That(b.RadarTechGeneration, Is.EqualTo(0));
        Assert.That(CatalogRadarScanTypes.Validate(b), Is.Empty);
    }
    [TestCase("Mechanical")]
    [TestCase("pesa")]
    [TestCase("AESA")]
    public void Known_scan_types_validate(string scan)
    {
        Assert.That(CatalogRadarScanTypes.Validate(new CatalogSensorBinding("p1", "r1", 0.9) { RadarScanType = scan, RadarTechGeneration = 4 }), Is.Empty);
    }

    [Test]
    public void Invalid_attributes_report_errors()
    {
        Assert.That(CatalogRadarScanTypes.Validate(new CatalogSensorBinding("p1", "r1", 0.9) { RadarScanType = "Phased" }), Has.Exactly(1).Items);
        Assert.That(CatalogRadarScanTypes.Validate(new CatalogSensorBinding("p1", "r1", 0.9) { RadarTechGeneration = 9 }), Has.Exactly(1).Items);
        Assert.That(CatalogRadarScanTypes.Validate(new CatalogSensorBinding("p1", "r1", 0.9, EccmFactor: 1.5)), Has.Exactly(1).Items);
        Assert.That(CatalogRadarScanTypes.Validate(new CatalogSensorBinding(
            "p1", "ir1", 0.9, Modality: CatalogSensorModalities.Infrared)
        { FrequencyAgile = true }), Has.Exactly(1).Items);
    }

    [Test]
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
            Assert.That(rows.Select(r => r.SensorId), Is.EqualTo(new[] { "r0", "r1" }));
            Assert.That(rows[0].RadarScanType, Is.EqualTo(CatalogRadarScanTypes.Unspecified));
            Assert.That(rows[1].RadarScanType, Is.EqualTo("Aesa"));
            Assert.That(rows[1].FrequencyAgile, Is.True);
            Assert.That(rows[1].RadarTechGeneration, Is.EqualTo(5));
        }
        finally
        {
            File.Delete(path);
        }
    }
    [TestCase("Phased", 0)]
    [TestCase("Aesa", 9)]
    public void Json_import_rejects_invalid_eccm_fields(string scanType, int generation)
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-eccm-bad-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, $$"""
            { "sensors": [
              { "platformId": "p1", "sensorId": "r1", "basePd": 0.8, "radarScanType": "{{scanType}}", "radarTechGeneration": {{generation}} } ] }
            """);
            var ex = Assert.Throws<InvalidDataException>(() => CatalogJsonImporter.ReadSensorBindings(path));
            Assert.That(ex!.Message, Does.Contain("p1/r1"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
