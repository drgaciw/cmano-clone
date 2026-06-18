# Sprint 24 Graphite PR Stack — Draft (submit fallback)

**Date:** 2026-06-17  
**Reason:** `gt submit --stack --no-interactive` failed — `stack/sprint24/closeout-replay-gitnexus` diverged from Graphite tracking after manual rebase.  
**Stack tip:** `fd80953` @ `stack/sprint24/closeout-replay-gitnexus`  
**Trunk:** `main`  
**Closeout smoke:** `production/qa/smoke-sprint-24-closeout-2026-06-17.md` — **577/577 PASS**, ReplayGolden **6/6**

Submit order: bottom → top (9 PRs).

---

## PR 1 — `stack/sprint24/full-sln-gate` (`505a7bb`)

**Title:** `chore(qa): S24-01 full-solution re-baseline gate`

**Body:**
```
## Summary
Sprint 24 day-1 full-solution gate: `dotnet build` + `dotnet test ProjectAegis.sln` green with pass count ≥538 recorded.

## Story
S24-01 — Full-solution re-baseline (Req 21 / CI gate)

## Evidence
- `production/qa/smoke-sprint-24-2026-06-17.md` — 540/540 @ e77696d
- ReplayGoldenSuiteTests 6/6 kickoff baseline

## Test plan
- [x] `dotnet build ProjectAegis.sln` — 0 errors
- [x] `dotnet test ProjectAegis.sln` — 0 failures, count ≥538
```

---

## PR 2 — `stack/sprint24/phase-b-reader` (`a101c99`)

**Title:** `feat(data): Phase B catalog reader API + CLI export [S24-02]`

**Body:**
```
## Summary
Additive `ICatalogReader` Phase B reads (Mobility/Signatures/Emcon); CLI snapshot resolver returns real export payload from DB.

## Story
S24-02 — Phase B reader API + export provider

## Key changes
- `SqliteCatalogReader` / `InMemoryCatalogReader` Phase B tables (migration 008)
- `PlatformCatalogExportResolver` wired into export/import/diff CLI verbs
- `CatalogPhaseBReaderTests.cs`

## Evidence
`production/agentic/stacks/sprint24/S24-02-DONE.md`

## Test plan
- [x] `dotnet test ...Data.Tests --filter "Platform|CatalogPhaseB"`
- [x] Phase A reader regression unchanged
```

---

## PR 3 — `stack/sprint24/phase-b-write-gate` (`fe4f235`)

**Title:** `feat(data): Phase B write-gate staging commit [S24-03]`

**Body:**
```
## Summary
**CRITICAL extend-only** `CatalogWriteGate`: `ProposeMobility/Signature/EmconBatch` + `ApproveBatch`/`RejectBatch` for Phase B staging tables.

## Story
S24-03 — Phase B write-gate commit path

## Constraints
- Sensor + Phase A commit paths regression mandatory
- No direct-SQL bypass
- GitNexus impact archived before edit

## Evidence
`production/agentic/stacks/sprint24/S24-03-DONE.md`

## Test plan
- [x] `dotnet test ...Data.Tests --filter "WriteGate|Platform|CatalogPhaseB"`
- [x] Sensor `ProposeSensorBatch` → `ApproveBatch` regression
```

---

## PR 4 — `stack/sprint24/phase-b-importer` (`0da54cb`)

**Title:** `feat(data): Phase B importer E2E + round-trip golden [S24-04]`

**Body:**
```
## Summary
`PlatformWorkbookImporter` wires Mobility/Signatures/Emcon sheets; empty-diff golden; FK orphan guard; export→edit→approve→read-back E2E.

## Story
S24-04 — Phase B importer wiring + round-trip golden

## Evidence
`production/agentic/stacks/sprint24/S24-04-DONE.md`

## Test plan
- [x] `dotnet test ...Data.Tests --filter "Platform|CatalogPhaseB"` — 73 PASS
- [x] Unedited round-trip empty diff on Phase B sheets
- [x] Orphan PlatformId quarantined
```

---

## PR 5 — `stack/sprint24/phase-b-validator` (`fa9db45`)

**Title:** `feat(data): Phase B validator rule pack [S24-05]`

**Body:**
```
## Summary
`PlatformWorkbookValidator` Phase B rule pack: header parity, FK/orphan guard, Emcon enum sanity, mobility bounds; validation hash golden pinned.

## Story
S24-05 — Phase B validator rule pack (should-have)

## Evidence
`production/agentic/stacks/sprint24/S24-05-SCAFFOLD.md` + validator branch tests

## Test plan
- [x] `dotnet test ...Data.Tests --filter "Platform|CatalogPhaseB|Validation"`
- [x] Blocking findings prevent staging
```

---

## PR 6 — `stack/sprint24/phase-b-sim-consumer` (`909ce7b`)

**Title:** `feat(sim): Phase B catalog reader consumer smoke [S24-06]`

**Body:**
```
## Summary
`PhaseBCatalogDetectionModifier` consumes `ICatalogReader.TryGetSignature`; committed signature affects detection trial; legacy fixtures unchanged.

## Story
S24-06 — Phase B sim consumer smoke

## Constraints
- **ZERO touch** `DelegationBridge.cs`
- ReplayGoldenSuiteTests 6/6 mandatory

## Evidence
`production/agentic/stacks/sprint24/S24-06-DONE.md`

## Test plan
- [x] `dotnet test ...Sim.Tests --filter "PhaseB|DetectionTrial"` — 8 PASS
- [x] ReplayGoldenSuiteTests 6/6 PASS
```

---

## PR 7 — `stack/sprint24/c2-app6-spike` (`87de70e`)

**Title:** `feat(unity): C2 APP-6 Phase C symbology spike [S24-07]`

**Body:**
```
## Summary
`App6Sidc` resolver: distinct friendly (▣) and hostile (⬥) Toolkit glyphs on `MapSymbolEntry`; map projection read-only.

## Story
S24-07 — C2 regression + APP-6 Phase C spike

## Constraints
- **ZERO touch** `DelegationBridge.cs`
- Spike verdict PROCEED

## Evidence
`production/agentic/stacks/sprint24/S24-07-DONE.md`
`production/qa/sprint-24-app6-spike-2026-06-17.md`

## Test plan
- [x] `PlayModeSmoke|Doctrine|MapPanelBinder` — 17 PASS
- [x] `App6|MapPanel|MapPicture` — 14 PASS
```

---

## PR 8 — `stack/sprint24/cesium-polish` (`37602f6`)

**Title:** `feat(unity): Cesium globe production polish [S24-08]`

**Body:**
```
## Summary
Cesium globe polish: `useGlobeMap=false` CI default on DelegationSmoke; depth/occlusion + selection OOB checklist closed; headless smoke regression.

## Story
S24-08 — Cesium globe production polish

## Constraints
- **ZERO touch** `DelegationBridge.cs`
- Editor screenshots advisory (lean mode)

## Evidence
`production/agentic/stacks/sprint24/S24-08-DONE.md`
`production/qa/sprint-24-cesium-polish-2026-06-17.md`

## Test plan
- [x] `PlayModeSmoke|Cesium|Globe` — 14/14 PASS
- [x] `DelegationSmoke.unity` → `useGlobeMap: 0`
```

---

## PR 9 — `stack/sprint24/closeout-replay-gitnexus` (`fd80953`)

**Title:** `chore(sprint24): closeout replay + GitNexus hygiene [S24-09] (+ S24-10 EMCON stretch)`

**Body:**
```
## Summary
Sprint 24 stack closeout: replay determinism gate, GitNexus re-index + detect_changes evidence, sprint-status reconciliation, QA sign-off docs. Tip includes stretch S24-10 doctrine EMCON read-only panel field.

## Stories
- S24-09 — Closeout hygiene (replay + GitNexus)
- S24-10 — Doctrine EMCON read-only panel field (stretch, landed on tip)

## Gates @ fd80953
- Full solution: **577/577 PASS**
- ReplayGoldenSuiteTests: **6/6 PASS**
- WriteGate|Platform: **77/77 PASS**
- PlayModeSmoke: **14/14 PASS**
- DelegationBridge.cs: **ZERO touch** vs main

## Evidence
- `production/qa/smoke-sprint-24-closeout-2026-06-17.md`
- `production/qa/sprint-24-gitnexus-2026-06-17.md`
- `production/agentic/stacks/sprint24/S24-09-DONE.md`
- `production/agentic/stacks/sprint24/S24-10-DONE.md`

## GitNexus
```
npx gitnexus detect_changes --repo cmano-clone --scope compare --base-ref main
# 50 files, 279 symbols, 70 processes, CRITICAL
```

## Test plan
- [x] `/replay-verify` proxy — ReplayGoldenSuiteTests 6/6
- [x] Full sln gate green
- [x] Scoped smoke filters per qa-plan §Smoke Test Scope
```

---

## Submit remediation

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

# Fix Graphite divergence (pick one):
git rebase stack/sprint24/cesium-polish stack/sprint24/closeout-replay-gitnexus
gt track stack/sprint24/closeout-replay-gitnexus --parent stack/sprint24/cesium-polish

gt submit --stack --no-interactive
```

If auth/network still fails, open PRs manually using titles/bodies above in stack order.