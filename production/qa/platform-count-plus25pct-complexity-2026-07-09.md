# Platform inventory for +25% scenario variation (wave-3)

**Date:** 2026-07-09 / 2026-07-10  
**Catalog:** `assets/data/catalog/baltic_patrol.db`  
**Source:** cmo-db.com (new `/en/cmo/{ship|aircraft|submarine|weapon}/` URLs) via write-gate import  
**Fixtures:** `tools/cmano-db-crawler/fixtures/baltic-multidomain-wave3.md` (+ weapons/sensors)

## Perplexity review

URL: https://www.perplexity.ai/search/3625fcab-3e10-4cc6-bd7f-432deb2c3650  

**Fetch status:** page body not usable (auth/share shell only). Analysis therefore uses the live catalog product model already established in `platform-count-complexity-analysis-2026-07-09.md` and `platform-count-exponential-validation-2026-07-09.md`.

**Applied practice (aligned with prior model):** grow **composition \(C\)** via multi-domain combat platforms; do not rely on \(D_{\mathrm{card}}\) (already capped under \(k_{\max}=2\)–3). Prefer combat fittings (sensor + positive-range magazine weapons) over chrome headcount.

## Model

\[
N \approx 180 \times D_{\mathrm{card}} \times C,\quad
C = n_s^{2}\, n_a\, n_u
\]

(fixed axes \(M\times R\times E\times S=180\); multi-domain structure blue surface+air vs red surface+sub).  
\(D_{\mathrm{card}}=26\) for \(k_{\max}=2\) once each domain \(\ge 2\).

## Baseline (pre wave-3)

| Domain | Raw | Combat-ready |
|--------|----:|-------------:|
| Surface \(n_s\) | 19 | **17** |
| Air \(n_a\) | 14 | **13** |
| Sub \(n_u\) | 10 | **10** |
| Total platforms | 43 | 40 combat-ready |
| Weapons max_range>0 | 172 | — |

\[
C_0 = 17^{2}\times 13\times 10 = \mathbf{37\,570}
\]

Target for **+25%:** \(C \ge 1.25\times 37570 = \mathbf{46\,962.5}\).

### How many platforms for +25%?

Under equal growth, \(C\) scales ~fourth-order in domain counts. Minimal integer additions \((a,b,c)\) to \((n_s,n_a,n_u)\) with \(C\ge 46962.5\):

| Strategy | Adds (S,A,U) | Result \(C\) | vs baseline |
|----------|-------------:|-------------:|------------:|
| Minimal total adds | (0,0,**3**) sub only | 48 841 | +30% |
| Balanced +1 each | (1,1,1) | 49 896 | +33% |
| Stretch (this wave) | (**5**,**4**,**3**) combat | **106 964** | **+185%** |

**Recommendation:** ≥**~3 combat platforms** in the smallest domain (or ~1 per domain) clears +25% on \(C\); wave-3 overshoots deliberately for nationality diversity.

## After wave-3 import

| Domain | Raw | Combat-ready | Δ combat |
|--------|----:|-------------:|---------:|
| Surface | **24** | **22** | +5 |
| Air | **18** | **17** | +4 |
| Sub | **13** | **13** | +3 |
| Total | **55** | **52** | +12 |
| Weapons max_range>0 | **208** | — | +36 |

\[
C_1 = 22^{2}\times 17\times 13 = \mathbf{106\,964}
\]

\[
\frac{C_1}{C_0} = \frac{106964}{37570} \approx \mathbf{2.85}\quad(+185\% \ge +25\%)
\]

With fixed \(D_{\mathrm{card}}=26\): structure-level \(N\propto C\) also rises **~2.85×**.

## New platforms (showcase)

| Domain | Examples (platform_id) |
|--------|------------------------|
| Surface | `d-32-daring-type-45-batch-1`, `f-230-norfolk-type-23-duke`, `f-310-fridtjof-nansen`, `p-840-holland`, `p-960-skjold` |
| Air | `f-35a-lightning-ii`, `eurofighter-typhoon`, `f-a-18c-hornet-f-18c`, `nh90-nfh-caiman-marine` |
| Sub | `ssn-774-virginia-blk-i-ii`, `s-120-papanikolis-type-214hn`, `s-71-chakra-pla-971i-akula-ii` |

Combat weapons (numeric cmo-db-style IDs): Aster 30 (`cmo-weapon-80001`), AIM-120C-7 (`80004`), Mk48 ADCAP (`80007`), etc.

## Import path

1. Search + detail scrape of https://cmo-db.com/en/cmo/… (site migrated off `/ship/N/` index).  
2. Markdown fixtures → `ImportQaSlice` → `CmoMarkdownImportProposer` + `CatalogWriteGate.ApproveBatch`.  
3. `InferDomain` extended for Multirole/Fighter/Helicopter/SSK labels so air/sub domains classify correctly.

## Verdict

**PASS** — modeled multi-domain composition space **increased by ~185%** (≥25% required). Catalog multi-domain combat pools: surface **22**, air **17**, sub **13**.

## Legal

Harvest is internal design reference via community viewer cmo-db.com; TDM rights reserved by the site. Do not ship the proprietary CMO product database wholesale.
