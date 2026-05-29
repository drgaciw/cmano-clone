// cmano-db.com harvester — resumable, polite, checkpointed.
// Usage: node harvest.mjs <category|all> [concurrency] [delayMs]
import { writeFileSync, readFileSync, existsSync, mkdirSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dir = dirname(fileURLToPath(import.meta.url));
const RAW = join(__dir, "_raw");
mkdirSync(RAW, { recursive: true });

const UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
const BASE = "https://cmano-db.com";
const ALL = ["aircraft", "ship", "submarine", "facility", "sensor", "weapon"];

const arg = (process.argv[2] || "all").toLowerCase();
const CATS = arg === "all" ? ALL : [arg];
const CONC = parseInt(process.argv[3] || "4", 10);
const DELAY = parseInt(process.argv[4] || "200", 10); // ms after each request, per worker

const sleep = ms => new Promise(r => setTimeout(r, ms));

async function fetchText(url, tries = 4) {
  for (let i = 0; i < tries; i++) {
    try {
      const ac = new AbortController();
      const to = setTimeout(() => ac.abort(), 30000);
      const r = await fetch(url, { headers: { "User-Agent": UA, "Accept": "text/html" }, signal: ac.signal });
      clearTimeout(to);
      if (r.status === 200) return await r.text();
      if (r.status === 429 || r.status >= 500) { await sleep(2000 * (i + 1)); continue; }
      throw new Error("HTTP " + r.status);
    } catch (e) {
      if (i === tries - 1) throw e;
      await sleep(1000 * (i + 1));
    }
  }
}

function clean(s) {
  return s.replace(/<[^>]+>/g, " ")
    .replace(/&nbsp;/g, " ").replace(/&amp;/g, "&").replace(/&lt;/g, "<").replace(/&gt;/g, ">")
    .replace(/&#0?39;/g, "'").replace(/&quot;/g, '"').replace(/&deg;/g, "°")
    .replace(/[ \t]+/g, " ").replace(/\s*\n\s*/g, " | ").replace(/(\s*\|\s*)+/g, " | ").trim()
    .replace(/^\|\s*|\s*\|$/g, "").trim();
}

function parseRecord(html) {
  const sections = [];
  for (const tm of html.matchAll(/<table[^>]*>([\s\S]*?)<\/table>/gi)) {
    const tbl = tm[1];
    let title = "";
    const cells = [];
    for (const rm of tbl.matchAll(/<tr[^>]*>([\s\S]*?)<\/tr>/gi)) {
      const row = rm[1];
      const ths = [...row.matchAll(/<th[^>]*>([\s\S]*?)<\/th>/gi)].map(m => clean(m[1])).filter(Boolean);
      const tds = [...row.matchAll(/<td[^>]*>([\s\S]*?)<\/td>/gi)].map(m => clean(m[1])).filter(Boolean);
      if (ths.length && !title) title = ths.join(" ").replace(/:$/, "").trim();
      cells.push(...tds);
    }
    if (title || cells.length) sections.push({ title, cells });
  }
  const ver = html.match(/db v\.\s*(\d+)/i);
  return { sections, dbVersion: ver ? ver[1] : null };
}

function enumerate(cat, html) {
  const groups = [...html.matchAll(/<div class="country" id="([^"]+)">/gi)]
    .map(m => ({ pos: m.index, name: decodeURIComponent(m[1].replace(/\+/g, " ")) }));
  const records = [];
  const re = new RegExp(`<a href="${cat}/(\\d+)/">([\\s\\S]*?)</a>`, "gi");
  let m;
  while ((m = re.exec(html))) {
    const pos = m.index;
    let group = "";
    for (let i = groups.length - 1; i >= 0; i--) { if (groups[i].pos < pos) { group = groups[i].name; break; } }
    const label = m[2].replace(/<[^>]+>/g, " ").replace(/&amp;/g, "&").replace(/\s+/g, " ").trim();
    records.push({ id: m[1], label, group });
  }
  const seen = new Set(); const out = [];
  for (const r of records) { if (!seen.has(r.id)) { seen.add(r.id); out.push(r); } }
  return out;
}

async function runCategory(cat) {
  const file = join(RAW, cat + ".json");
  let store = { category: cat, dbVersion: null, capturedAt: new Date().toISOString(), records: {} };
  if (existsSync(file)) {
    try { store = JSON.parse(readFileSync(file, "utf8")); store.records ||= {}; } catch {}
  }
  const idxHtml = await fetchText(`${BASE}/${cat}/`);
  const list = enumerate(cat, idxHtml);
  const todo = list.filter(r => !store.records[r.id] || store.records[r.id].ok === false);
  console.log(`[${cat}] index=${list.length} alreadyDone=${list.length - todo.length} todo=${todo.length}`);

  let done = 0, errs = 0, since = 0;
  const flush = () => { writeFileSync(file, JSON.stringify(store)); };

  let qi = 0;
  async function worker() {
    while (qi < todo.length) {
      const rec = todo[qi++];
      try {
        const html = await fetchText(`${BASE}/${cat}/${rec.id}/`);
        const { sections, dbVersion } = parseRecord(html);
        if (dbVersion && !store.dbVersion) store.dbVersion = dbVersion;
        store.records[rec.id] = { id: rec.id, label: rec.label, group: rec.group, sections, ok: true };
      } catch (e) {
        store.records[rec.id] = { id: rec.id, label: rec.label, group: rec.group, ok: false, error: String(e.message || e) };
        errs++;
      }
      done++; since++;
      if (since >= 200) { since = 0; flush(); console.log(`[${cat}] ${done}/${todo.length} (errors ${errs})`); }
      await sleep(DELAY);
    }
  }
  store.indexCount = list.length;
  await Promise.all(Array.from({ length: CONC }, () => worker()));
  flush();
  const okCount = Object.values(store.records).filter(r => r.ok).length;
  console.log(`[${cat}] DONE captured=${okCount}/${list.length} errors=${errs} dbVersion=${store.dbVersion}`);
  return { cat, index: list.length, captured: okCount, errors: errs };
}

const summary = [];
for (const c of CATS) summary.push(await runCategory(c));
console.log("\n===== SUMMARY =====");
for (const s of summary) console.log(`${s.cat.padEnd(10)} captured ${s.captured}/${s.index}  errors=${s.errors}`);
writeFileSync(join(RAW, "_summary.json"), JSON.stringify(summary, null, 2));
