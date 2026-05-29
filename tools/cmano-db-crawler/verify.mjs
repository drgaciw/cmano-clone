// Live verification: re-fetch sampled records, diff vs harvested JSON + confirm presence in rendered md.
import { readFileSync, existsSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
const __dir = dirname(fileURLToPath(import.meta.url));
const RAW = join(__dir, "_raw");
const OUT = join(__dir, "..", "..", "docs", "reference", "cmano-db");
const UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
const sleep = ms => new Promise(r => setTimeout(r, ms));
const get = async url => (await fetch(url, { headers: { "User-Agent": UA } })).text();
function clean(s){return s.replace(/<[^>]+>/g," ").replace(/&nbsp;/g," ").replace(/&amp;/g,"&").replace(/&lt;/g,"<").replace(/&gt;/g,">").replace(/&#0?39;/g,"'").replace(/&quot;/g,'"').replace(/&deg;/g,"°").replace(/[ \t]+/g," ").replace(/\s*\n\s*/g," | ").replace(/(\s*\|\s*)+/g," | ").trim().replace(/^\|\s*|\s*\|$/g,"").trim();}
function parse(html){const out=[];for(const tm of html.matchAll(/<table[^>]*>([\s\S]*?)<\/table>/gi)){const tbl=tm[1];let title="";const cells=[];for(const rm of tbl.matchAll(/<tr[^>]*>([\s\S]*?)<\/tr>/gi)){const row=rm[1];const ths=[...row.matchAll(/<th[^>]*>([\s\S]*?)<\/th>/gi)].map(m=>clean(m[1])).filter(Boolean);const tds=[...row.matchAll(/<td[^>]*>([\s\S]*?)<\/td>/gi)].map(m=>clean(m[1])).filter(Boolean);if(ths.length&&!title)title=ths.join(" ").replace(/:$/,"").trim();cells.push(...tds);}if(title||cells.length)out.push({title,cells});}return out;}

const cats = ["aircraft","ship","submarine","facility","sensor","weapon"];
let pass=0, fail=0;
for(const cat of cats){
  const data = JSON.parse(readFileSync(join(RAW, cat+".json"),"utf8"));
  const ids = Object.keys(data.records);
  const sample = [ids[0], ids[Math.floor(ids.length/2)], ids[ids.length-1]];
  const md = existsSync(join(OUT,cat+".md")) ? readFileSync(join(OUT,cat+".md"),"utf8") : "";
  for(const id of sample){
    const live = parse(await get(`https://cmano-db.com/${cat}/${id}/`));
    const stored = data.records[id].sections;
    const sameData = JSON.stringify(live) === JSON.stringify(stored);
    const inMd = md.includes(`/${cat}/${id}/`);
    const ok = sameData && inMd;
    console.log(`${ok?"PASS":"FAIL"} ${cat}/${id}  liveVsStored=${sameData?"identical":"DIFF"} inMarkdown=${inMd}  "${data.records[id].label.slice(0,40)}"`);
    ok?pass++:fail++;
    await sleep(250);
  }
}
console.log(`\nVERIFY: ${pass} passed, ${fail} failed (of ${pass+fail} sampled records across 6 categories)`);
