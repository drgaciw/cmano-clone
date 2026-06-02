// Export CMO sensor harvest -> ProjectAegis catalog import JSON.
// Usage: node export-catalog-sensors.mjs [rawSensorJson] [outJson]
import { readFileSync, writeFileSync, existsSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dir = dirname(fileURLToPath(import.meta.url));
const rawPath = process.argv[2] || join(__dir, "_raw", "sensor.json");
const outPath =
  process.argv[3] ||
  join(__dir, "..", "..", "assets", "data", "catalog", "import", "cmo_sensors_export.json");

if (!existsSync(rawPath)) {
  console.error(`Missing harvest file: ${rawPath}`);
  console.error("Run: node harvest.mjs sensor");
  process.exit(1);
}

const data = JSON.parse(readFileSync(rawPath, "utf8"));
const records = Object.values(data.records || {}).filter(r => r.ok);
const sensors = [];

for (const r of records) {
  const platformId = slugId(r.label || `platform-${r.id}`);
  const sensorId = `cmo-sensor-${r.id}`;
  const basePd = inferBasePd(r);
  sensors.push({
    platformId,
    sensorId,
    basePd,
    sourceFactId: `cmano-db:sensor/${r.id}`,
    confidence: 0.7,
    reviewState: "provisional",
    trlLevel: 6,
  });
}

sensors.sort((a, b) =>
  a.platformId.localeCompare(b.platformId) || a.sensorId.localeCompare(b.sensorId),
);

const out = {
  importBatchId: `cmo-sensors-${data.dbVersion || "unknown"}`,
  sensors,
};

writeFileSync(outPath, JSON.stringify(out, null, 2) + "\n", "utf8");
console.log(`Wrote ${sensors.length} sensors -> ${outPath}`);

function slugId(label) {
  return String(label)
    .toLowerCase()
    .replace(/[^\w]+/g, "-")
    .replace(/^-|-$/g, "")
    .slice(0, 64) || "unknown-platform";
}

/** MVP: map CMO range cell to a nominal basePd for detection loop tuning. */
function inferBasePd(record) {
  const sections = record.sections || [];
  for (const sec of sections) {
    for (const cell of sec.cells || []) {
      const m = /range\s*:\s*([\d.]+)\s*nm/i.exec(cell);
      if (m) {
        const nm = parseFloat(m[1]);
        if (Number.isFinite(nm) && nm > 0) return Math.min(1, Math.max(0.05, nm / 200));
      }
    }
  }
  return 0.5;
}