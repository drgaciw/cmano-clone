using ProjectAegis.Data.Import;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;
using System.Text.Json;

// Usage: ImportQaSlice <db> <platform.md> <weapon.md> [sensor.md]
var db = args.ElementAtOrDefault(0) ?? "assets/data/catalog/baltic_patrol.db";
var platformMd = args.ElementAtOrDefault(1) ?? "tools/cmano-db-crawler/fixtures/baltic-multidomain-live.md";
var weaponMd = args.ElementAtOrDefault(2) ?? "tools/cmano-db-crawler/fixtures/baltic-multidomain-weapons-live.md";
var sensorMd = args.ElementAtOrDefault(3);

Console.WriteLine($"DB={db}");
Console.WriteLine($"platforms={platformMd}");
Console.WriteLine($"weapons={weaponMd}");
if (!string.IsNullOrWhiteSpace(sensorMd))
{
    Console.WriteLine($"sensors={sensorMd}");
}

// Apply migrations / ensure full schema via reader bootstrap
using (var _ = new ProjectAegis.Data.Catalog.SqliteCatalogReader(db, "qa-import-bootstrap")) { }
var baseTicks = DateTime.UtcNow.Ticks;
var report = new Dictionary<string, object?>();
var batchIds = new List<string>();

var weaponPropose = CmoMarkdownImportProposer.ProposeWeaponsFromMarkdown(
    db,
    weaponMd,
    clock: new FixedCatalogClock(baseTicks));
report["weaponPropose"] = new
{
    weaponPropose.ParsedCount,
    weaponPropose.ApprovedCount,
    Batches = weaponPropose.Batches.Select(b => new { b.BatchId, b.RecordCount }).ToArray(),
};
foreach (var b in weaponPropose.Batches)
{
    batchIds.Add(b.BatchId);
}

var plat = CmoMarkdownImportProposer.ProposePlatformWeaponMounts(
    db,
    platformMd,
    weaponMarkdownPath: weaponMd,
    mapBalticPlatformIds: false,
    clock: new FixedCatalogClock(baseTicks + 1));
report["platformPropose"] = new
{
    plat.PlatformCount,
    plat.WeaponCount,
    plat.MountCount,
    plat.LoadoutCount,
    plat.MagazineCount,
    plat.FittingQuarantinedCount,
    plat.PlatformBatchId,
    plat.WeaponBatchId,
    plat.MountBatchId,
    plat.LoadoutBatchId,
    plat.MagazineBatchId,
    Quarantine = plat.FittingQuarantineReport
        .Take(30)
        .Select(q => q.ToString())
        .ToArray(),
};

void AddBatch(string? id)
{
    if (!string.IsNullOrWhiteSpace(id))
    {
        batchIds.Add(id);
    }
}

AddBatch(plat.PlatformBatchId);
AddBatch(plat.WeaponBatchId);
AddBatch(plat.MountBatchId);
AddBatch(plat.LoadoutBatchId);
AddBatch(plat.MagazineBatchId);

if (!string.IsNullOrWhiteSpace(sensorMd) && File.Exists(sensorMd))
{
    var sensors = CmoMarkdownImportProposer.ProposeFromMarkdown(
        db,
        sensorMd,
        clock: new FixedCatalogClock(baseTicks + 2));
    report["sensorPropose"] = new
    {
        sensors.ParsedCount,
        sensors.ApprovedCount,
        sensors.QuarantinedCount,
        Batches = sensors.Batches.Select(b => new { b.BatchId, b.RecordCount }).ToArray(),
    };
    foreach (var b in sensors.Batches)
    {
        batchIds.Add(b.BatchId);
    }
}

var approved = new List<object>();
var failed = new List<object>();
using (var gate = new CatalogWriteGate(db, new FixedCatalogClock(baseTicks + 10)))
{
    foreach (var id in batchIds.Distinct(StringComparer.Ordinal))
    {
        var decision = gate.ApproveBatch(id, "human", "qa-cmo-import");
        if (decision.Committed)
        {
            approved.Add(new { id, decision.Committed });
        }
        else
        {
            failed.Add(new
            {
                id,
                decision.Committed,
                errors = decision.Errors,
            });
        }
    }
}

report["approvedCount"] = approved.Count;
report["failedCount"] = failed.Count;
report["approved"] = approved;
report["failed"] = failed;
report["batchIds"] = batchIds.Distinct(StringComparer.Ordinal).ToArray();

var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);

var logDir = Path.Combine("/tmp", "grok-goal-539a46171f2e", "implementer");
Directory.CreateDirectory(logDir);
File.WriteAllText(Path.Combine(logDir, "catalog-import.log"), json);

return failed.Count == 0 ? 0 : 2;
