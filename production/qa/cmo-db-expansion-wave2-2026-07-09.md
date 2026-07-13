# cmo-db expansion wave-2 evidence (ships + aircraft + subs)

**Date:** 2026-07-09  
**Method:** Playwright Chromium (`scrape-expand.mjs`) against cmano-db.com  
**Legal:** Internal design reference only; not redistributing CMO proprietary DB.

## Baseline → post

| Metric | Pre | Post |
|---|---|---|
| platform | 19 | **43** |
| surface | 9 | **19** |
| air | 6 | **14** |
| subsurface | 4 | **10** |
| weapon (max_range>0) | 109 | **172** (0 zero) |
| magazine | 133 | **281** |

## Harvest
- 24 new platforms (10 ship / 8 aircraft / 6 submarine), all HTTP 200
- 120 weapons extracted with Air/Surface Max format
- Artifacts: `/tmp/grok-goal-abd6a1415906/implementer/cmo-db-scrape/`

## Import
- Write-gate via `import-qa-slice`: 7 batches committed, 0 failed, 0 fitting quarantine
- Log: `catalog-import.log`

## Complexity analysis
- `platform-count-complexity-analysis.md` — product model \(N \approx 180 \times D_{card} \times C\)
- Recommended min met/exceeded (domain pools ≥8 multi-domain for exponential jump)

## Scenario
- `baltic-multidomain-stockholm-sample` — Stockholm (ship/76) detects Steregushchiy; seed 42 exit 0

## Tests
- BalticMultidomainImportResolutionTests: 5/5
- Data.Tests: see post-import-tests.log
