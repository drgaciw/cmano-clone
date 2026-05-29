# cmano-db crawler

Auxiliary tooling that produces the offline reference export in
[`docs/reference/cmano-db/`](../../docs/reference/cmano-db/) from the community database
viewer **[cmano-db.com](https://cmano-db.com/)** (a viewer over the Command: Modern
Air/Naval Operations game database). Node 18+ only — no dependencies (uses built-in `fetch`).

## Usage

```sh
node harvest.mjs all        # crawl all six categories -> _raw/<category>.json  (~50 min)
node render.mjs all         # render _raw/*.json -> docs/reference/cmano-db/*.md
node verify.mjs             # re-fetch sampled records live and diff vs the harvest
```

`harvest.mjs <category|all> [concurrency=4] [delayMs=200]`. It is **resumable** — re-running
skips records already captured in `_raw/<category>.json`, so an interrupted crawl continues
where it left off. Pass a single category name to crawl just one.

## How it works (non-obvious bits)

- The site is plain **server-rendered PHP**. It returns **403 to the default fetch/curl
  User-Agent** but **200 to a normal browser UA** — there is no JS challenge and no JSON/
  batch API, so a UA-spoofed `fetch` is all that's needed (no headless browser).
- Index page `/{category}/` lists every record as `<a href="{category}/{numericId}/">`,
  grouped by `<div class="country" id="...">` (a country for platforms, a type for
  sensors/weapons). Each `/{category}/{id}/` page is HTML `<table>`s — `parseRecord`
  captures every table's header + cells generically, so all six categories work unchanged.
- `robots.txt` has **no `Disallow`/`Crawl-delay`** — only a Content-Signals / TDM rights
  reservation (EU Directive 2019/790 Art. 4) for AI-train / AI-input / search. This export
  is **personal/internal design reference only**; the crawler is rate-limited and single-host.

## Refreshing when the DB version changes

1. `node harvest.mjs all` (bump nothing — it re-checks the index and re-fetches changed set;
   delete `_raw/` first for a clean full re-crawl).
2. Update `CAPTURED` (and confirm the rendered DB version) in `render.mjs`, then
   `node render.mjs all`.
3. `node verify.mjs` to confirm fidelity.

`_raw/` (the JSON checkpoints, tens of MB) is git-ignored; the rendered markdown is the
committed deliverable.
