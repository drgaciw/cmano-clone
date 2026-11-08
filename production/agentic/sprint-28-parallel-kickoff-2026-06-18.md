# Sprint 28 Parallel Kickoff — 2026-06-18

**Sprint:** 28 — CMO Corpus v2 + Platform Write Path + Combat Phase 2  
**Plan:** `production/sprints/sprint-28-corpus-write-combat-v2.md`  
**Trunk:** `main` @ `a93b55e` (741/741, ReplayGolden 6/6)  
**Review mode:** `lean` (PR-SPRINT skipped)

## Sprint Goal (one line)

Extend nightly CMO to **platform corpus v2**, land **ADR-011 Phase D Excel write** through `CatalogWriteGate`, and advance **ADR-009** with bounded surface/subsurface validators — without Baltic golden drift or full corpus CI load.

## Parallel Tracks

| Track | Owner skill | Stories | Graphite prefix | Day-1 gate |
|-------|-------------|---------|-----------------|------------|
| **Data** | team-data | S28-02, S28-03, S28-04 (data half), S28-06 | `stack/sprint28/corpus-v2`, `stack/sprint28/excel-write` | CmoMarkdown\|WriteGate\|Platform\|Excel |
| **Unity** | team-unity | S28-04 (Unity half), S28-07 | `stack/sprint28/platform-write-ui` | PlatformCatalog\|Excel |
| **Simulation** | team-simulation | S28-05, S28-06 (sim half), S28-08, S28-09 | `stack/sprint28/combat-phase2` | Combat\|Domain\|Damage\|Readiness |
| **DevOps/QA** | c-sharp-devops-engineer | S28-01, S28-13, S28-12 | `stack/sprint28/closeout` | Full sln + ReplayGolden |

## Dispatch Order (after S28-01 baseline)

**Wave 1 (parallel, no cross-deps):**
- S28-02 Nightly platform corpus v2
- S28-05 Surface/subsurface validators (can start after S28-01)

**Wave 2:**
- S28-03 Corpus E2E (after S28-02)
- S28-04 Excel write path (after S28-01; parallel with S28-02)

**Wave 3:**
- S28-06 Live magazines (after S28-03)
- S28-07 Viewer export hook (after S28-04)
- S28-08 Damage consumer (after S28-01)

**Closeout:**
- S28-13 (after S28-03+)

## Hard Constraints

- **ZERO touch** `DelegationBridge.cs`
- **CatalogWriteGate** extend-only — `gitnexus impact` before every edit
- **ReplayGolden 6/6** on every sim/delegation merge
- **`combatDomainsEnabled=false`** on Baltic production fixtures
- **No 7208 sensor.md in CI** — nightly + curated fixtures only

## Baseline Gates (S28-01)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal                    # ≥741
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "ReplayGoldenSuiteTests" -v minimal             # 6/6
npx gitnexus analyze . --force
```

## Tracker Alignment

| Req | S28 stories |
|-----|-------------|
| **06** Database Intelligence | S28-02, S28-03, S28-11 |
| **18** Combat Domains | S28-05, S28-08, S28-09 |
| **21** Platform Editor | S28-04, S28-07 |
| **16** Logistics | S28-06 |

## Prerequisites Before `/dev-story`

1. **`/qa-plan sprint`** — no `qa-plan-sprint-28-*.md` exists yet
2. **`/create-epics`** — scaffold `production/epics/sprint-28-*` from epic themes in sprint plan
3. **`/design-review`** on `combat-domains-damage.md` if S28-05 scope expands to facility

## Next Command

`/qa-plan sprint` → then `/dev-story` on `S28-01` (re-baseline)