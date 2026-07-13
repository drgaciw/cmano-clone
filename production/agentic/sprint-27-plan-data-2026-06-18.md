# Sprint 27 — Data / Platform Track Plan

**Date:** 2026-06-18  
**Kickoff:** `production/sprints/sprint-27-cmo-corpus-combat-bounded.md`  
**Maps to:** S27-02..04, S27-09, S27-14

## Goal

Scale bounded CMO corpus intake to a nightly off-CI pipeline; complete mount→loadout→magazine markdown import through extend-only `CatalogWriteGate`; enrich browse projection for Phase C viewer.

## Critical path (6d must-have)

| Story | Days | Key deliverable |
|-------|------|-----------------|
| S27-02 | 2 | Nightly job for sensor 7208 + weapon corpus (propose-only, quarantine artifacts) |
| S27-03 | 2 | `ProposeLoadoutBatch` + `ProposeMagazineBatch` in CMO platform import |
| S27-04 | 1 | Golden hash pin + WriteGate regression + replay unchanged |

## Parallel slot

| Story | Days | Notes |
|-------|------|-------|
| S27-02 | 2 | Parallel after S27-01; merge before closeout |
| S27-09 | 0.5 | After S27-03 — `MountCount`/`SensorCount` on browse rows |

## Top risks

1. **CatalogWriteGate CRITICAL** — extend-only; full Phase A/B regression mandatory
2. **Corpus scope creep** — nightly only; CI keeps curated slices + `--max-records`
3. **Loadout derivation ambiguity** — default loadout per platform; quarantine bad FK pairs

## Verify (closeout)

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|CatalogPlatformBrowse" -v minimal
```

## Graphite branches

`stack/sprint27/nightly-cmo-corpus` (parallel) → `cmo-loadout-magazine` → `import-golden-hygiene` → `browse-projection-enrich`

*Condensed from parallel Data/Platform planning agent — 2026-06-18.*