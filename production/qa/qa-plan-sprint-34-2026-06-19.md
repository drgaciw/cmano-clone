# QA Plan — Sprint 34

**Date:** 2026-06-19  
**Sprint:** LinkCatalog Workbook, Datalink Catalog Latency, Platform Editor Phase H  
**Review mode:** Lean  
**Stage:** Production  
**Stories in scope:** 12 planned; **11 implemented** (S34-09, S34-12 skipped nice-to-have)

## Scope

Sprint 34 closes Req 21 LinkCatalog workbook round-trip beyond comms surfacing, Req 15 catalog-derived datalink share lag (TR-sensor-004 slice), and Req 20 C2 Check 18 LinkCatalog sign-off. Hard gates: ZERO touch `DelegationBridge.cs`, `CatalogWriteGate` extend-only, ReplayGolden 6/6 on default path, production Baltic hash `17144800277401907079` unchanged.

## Story Classification

| Story | Type | Automated Required | Manual Required | Blocker? |
|-------|------|--------------------|-----------------|----------|
| S34-01 Full-sln re-baseline | Config | Yes — full sln + ReplayGolden | Smoke doc audit | No |
| S34-02 Link catalog staging | Integration + Logic | Yes — `WriteGate\|LinkCatalog` | Evidence review | No |
| S34-03 LinkCatalog workbook | Integration | Yes — `PlatformWorkbook\|LinkCatalog` | Golden hash review | No |
| S34-04 Catalog share lag | Integration + Logic | Yes — `Datalink\|ShareLag`; ReplayGolden 6/6 | — | No |
| S34-05 Link validation rules | Logic | Yes — `LinkCatalogRulePack` 17 tests | Finding message review | No |
| S34-06 Phase H LinkCatalog Unity | Integration | Yes — `PlatformLinkCatalog` 13/13 | Lean headless sufficient | No |
| S34-07 Catalog-latency fixture | Integration | Yes — isolated golden; ∉ ReplayGolden 6/6 | — | No |
| S34-08 catalog_link_report CLI | Integration | Yes — `LinkReport` 2 tests; read-only | Curator stdout review | No |
| S34-09 Datalink regression smoke | Integration | — | — | **Out of scope** (skipped) |
| S34-10 Presentation evidence | Visual / UI | Yes — headless ≥48/48 | Lean protocol PNG placeholders | No |
| S34-11 C2 sign-off upgrade | Config + UI | Yes — headless ≥55/55 checks 14–18 | Lean C2 checklist review | No |
| S34-12 CI hygiene | Config | — | — | **Out of scope** (skipped) |
| S34-13 Closeout hygiene | Config | Yes — ≥1156 sln; GitNexus; ReplayGolden | Tracker/smoke audit | No |

**Smoke check:** **PASS** — `production/qa/smoke-sprint-34-closeout-2026-06-19.md` (1193/1193; ReplayGolden 6/6; GitNexus 16,138/33,074)

## Automated Test Requirements

| Story | Filter / path | Expected |
|-------|---------------|----------|
| S34-01, S34-13 | `dotnet test ProjectAegis.sln` | ≥1156 PASS (actual 1193) |
| S34-01, S34-04, S34-13 | `ReplayGoldenSuiteTests` | 6/6 |
| S34-02 | `WriteGate\|LinkCatalog` | PASS |
| S34-03 | `PlatformWorkbook\|LinkCatalog` | PASS |
| S34-04 | `Datalink\|ShareLag` | PASS |
| S34-05 | `Link\|KillChain\|Validation` (Data.Tests) | PASS (+17 new) |
| S34-06 | `PlatformLinkCatalog` | 13/13 |
| S34-07 | `BalticReplayHarnessDatalinkCatalogLatency` | PASS; isolated golden |
| S34-08 | `LinkReport\|KillChain` (Cli.Tests) | 4/4 |
| S34-10 | `PlatformImport\|PlatformCatalogViewer\|PlatformComms\|PlatformLinkCatalog` | ≥48/48 (actual 51/51) |
| S34-11 | Above + `C2TopBar` | ≥55/55 (actual 58/58 with Doctrine) |

## Manual QA Scope (Lean)

| Story | Manual action | Lean substitute |
|-------|---------------|-----------------|
| S34-10 | Unity Editor capture LinkCatalog viewer + staging diff | Protocol PNG placeholders + headless 51/51 |
| S34-11 | Live C2 walkthrough Check 18 | Headless proxy 58/58 + checklist update |

## Out of Scope

- S34-09 datalink regression smoke (nice-to-have; cut at closeout)
- S34-12 CI/local gate refresh (5th deferral OK)
- Full corpora in CI; TL Phase 5 forks; ECCM Phase 2; Globe map
- Live Unity Editor screenshots (advisory polish only)

## Entry Criteria

1. Smoke check PASS at `production/qa/smoke-sprint-34-closeout-2026-06-19.md` — **met**
2. Build stable — `dotnet build ProjectAegis.sln` 0 errors — **met**
3. All must-have stories `done` in `production/sprint-status.yaml` — **met** (S34-01..04, S34-06)

## Exit Criteria

- All in-scope stories PASS or PASS WITH NOTES
- No open S1/S2 bugs
- QA sign-off report written to `production/qa/qa-signoff-sprint-34-2026-06-19.md`