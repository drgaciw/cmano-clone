// Render _raw/<cat>.json -> repo markdown. Usage: node render.mjs [category|all]
import { writeFileSync, readFileSync, existsSync, mkdirSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dir = dirname(fileURLToPath(import.meta.url));
const RAW = join(__dir, "_raw");
const OUT = join(__dir, "..", "..", "docs", "reference", "cmano-db");
mkdirSync(OUT, { recursive: true });

const ALL = ["aircraft", "ship", "submarine", "facility", "sensor", "weapon"];
const TITLES = { aircraft: "Aircraft", ship: "Ships", submarine: "Submarines", facility: "Facilities", sensor: "Sensors", weapon: "Weapons" };
const GROUPING = { aircraft: "country/operator", ship: "country/operator", submarine: "country/operator", facility: "country/operator", sensor: "type", weapon: "type" };
const arg = (process.argv[2] || "all").toLowerCase();
const cats = arg === "all" ? ALL.filter(c => existsSync(join(RAW, c + ".json"))) : [arg];

const SOURCE = "https://cmano-db.com/";
const CAPTURED = "2026-05-29";

const esc = s => String(s).replace(/\|/g, "\\|").replace(/\s+/g, " ").trim();
const slug = s => s.toLowerCase().replace(/[^\w\s-]/g, "").replace(/\s+/g, "-");
const generalOf = r => (r.sections || []).find(s => /^general/i.test(s.title || ""));
function typeOf(r) {
  const g = generalOf(r);
  if (g) { const c = g.cells.find(x => /^type\s*:/i.test(x)); if (c) return c.replace(/^type\s*:\s*/i, "").trim(); }
  return "(Other)";
}

function renderRecord(cat, r) {
  const out = [];
  out.push(`### ${r.label || ("#" + r.id)}`);
  out.push(`<sub>[/${cat}/${r.id}/](${SOURCE}${cat}/${r.id}/)</sub>`);
  out.push("");
  for (const sec of r.sections || []) {
    const t = sec.title || "";
    const isGeneral = /^general/i.test(t);
    if (isGeneral) {
      out.push(`| Field | Value |`, `|---|---|`);
      for (const c of sec.cells) {
        const i = c.indexOf(":");
        if (i > 0) out.push(`| ${esc(c.slice(0, i))} | ${esc(c.slice(i + 1))} |`);
        else out.push(`| | ${esc(c)} |`);
      }
      out.push("");
    } else {
      if (t) out.push(`**${t}**`, "");
      for (const c of sec.cells) out.push(`- ${c.replace(/\s*\|\s*/g, " — ").replace(/\s+-\s+—/g, " —").replace(/\s+-\s*$/g, "").trim()}`);
      out.push("");
    }
  }
  return out.join("\n");
}

function renderCategory(cat) {
  const data = JSON.parse(readFileSync(join(RAW, cat + ".json"), "utf8"));
  const recs = Object.values(data.records || {}).filter(r => r.ok);
  const failed = Object.values(data.records || {}).filter(r => !r.ok);
  const hasCountry = recs.some(r => r.group && r.group.trim());
  const groups = new Map();
  for (const r of recs) {
    const key = hasCountry ? (r.group && r.group.trim() ? r.group.trim() : "(Unspecified)") : typeOf(r);
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key).push(r);
  }
  const groupKeys = [...groups.keys()].sort((a, b) => a.localeCompare(b));

  const md = [];
  md.push(`# ${TITLES[cat]} — cmano-db.com`, "");
  md.push(`> Source: [${SOURCE}${cat}/](${SOURCE}${cat}/) · Database version **${data.dbVersion || "?"}** · Captured ${CAPTURED}`);
  md.push(`> Modern database. ${recs.length} records${failed.length ? ` (${failed.length} failed to fetch)` : ""}, grouped by ${GROUPING[cat] || (hasCountry ? "country/operator" : "type")}.`, "");
  md.push(`Data derived from the Command: Modern Air/Naval Operations game database via the community viewer cmano-db.com. The site reserves rights for AI-training/-input/search use (EU Dir. 2019/790 Art. 4); this capture is personal/internal design reference only.`, "");
  md.push(`**Contents:** ` + groupKeys.map(g => `[${g}](#${slug(g)})`).join(" · "), "");
  md.push("---", "");
  for (const g of groupKeys) {
    md.push(`## ${g}`, "");
    const list = groups.get(g).sort((a, b) => (a.label || "").localeCompare(b.label || ""));
    for (const r of list) md.push(renderRecord(cat, r), "");
  }
  if (failed.length) {
    md.push(`## Failed records (${failed.length})`, "");
    for (const r of failed) md.push(`- /${cat}/${r.id}/ — ${r.error}`);
    md.push("");
  }
  const path = join(OUT, cat + ".md");
  writeFileSync(path, md.join("\n"));
  return { cat, records: recs.length, failed: failed.length, dbVersion: data.dbVersion, groups: groupKeys.length, path };
}

const results = cats.map(renderCategory);

const idx = [];
idx.push(`# cmano-db.com — Command Modern Database (Reference Export)`, "");
idx.push(`Offline markdown reference of the Command: Modern Air/Naval Operations modern database, harvested from the community viewer **[cmano-db.com](${SOURCE})**.`, "");
idx.push(`- **Database version:** ${results[0]?.dbVersion || "?"}`);
idx.push(`- **Captured:** ${CAPTURED}`);
idx.push(`- **Scope:** Modern database, all platform/equipment categories, full specifications.`, "");
idx.push(`## Categories`, "");
idx.push(`| Category | Records | Groups | File |`, `|---|---:|---:|---|`);
let total = 0;
for (const c of ALL) {
  const r = results.find(x => x.cat === c);
  if (r) { total += r.records; idx.push(`| ${TITLES[c]} | ${r.records}${r.failed ? ` (+${r.failed} failed)` : ""} | ${r.groups} | [${c}.md](${c}.md) |`); }
  else idx.push(`| ${TITLES[c]} | _pending_ | | ${c}.md |`);
}
idx.push(`| **Total** | **${total}** | | |`, "");
idx.push(`## Source & terms`, "");
idx.push(`Data originates from the CMANO game database and is presented by cmano-db.com. The site's robots.txt asserts content-signal reservations (AI-train / AI-input / search) under EU Directive 2019/790 Article 4. This export is for **personal/internal game-design reference only** — not redistribution, AI training, or search indexing. Crawled politely (rate-limited, single-host, no re-fetching).`, "");
writeFileSync(join(OUT, "cmano-db-data.md"), idx.join("\n"));

console.log("Rendered:");
for (const r of results) console.log(`  ${r.cat.padEnd(10)} ${r.records} records, ${r.groups} groups -> ${r.path}`);
console.log("  index -> " + join(OUT, "cmano-db-data.md"));
