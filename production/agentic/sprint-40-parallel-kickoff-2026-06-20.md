# Sprint 40 Parallel Kickoff — Catalog/Import Surfacing + Perf P1 + Replay Maint

**Date:** 2026-06-20 (drafted during S39 closeout; execute post-S39 COMPLETE)  
**Trunk:** `main` @ (post-S39) — **≥1213**, ReplayGolden 6/6, C2 18/18+  
**Sprint plan:** `production/sprints/sprint-40-deeper-polish-catalog-import-perf.md`  
**QA plan:** `production/qa/qa-plan-sprint-40-*.md` (TBD via /qa-plan)  
**Authority:** `production/polish-scope-boundary-2026-06-19.md` + [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §Horizon 2. Lean. ZERO DelegationBridge. Extend-only CatalogWriteGate. Immutable Baltic hash.

## Sprint Goal (recap)

Horizon 2 deeper polish: Catalog/Import read-model surfacing (single-agent Catalog cluster), perf P1 burn-down, replay maintenance, evidence/playtest 12, hygiene closeout.

## Parallel Execution Model

**Prerequisites (serial, blocking):** S40-01 + S40-02 **MUST** complete before ANY feature dispatch.

**Parallelism cap: 4 tracks** — Catalog track is **one agent only** (HIGH blast-radius).

After prereqs: independent tracks, isolated context, one agent per sub-track.

## Wave plan

| Wave | Stories | Track | Est. | Agent env | Notes |
|------|---------|-------|------|-----------|-------|
| Day-1 | S40-01 | DevOps baseline | 1d | Local or Cloud | Blocks all |
| W0 | S40-02 | QA plan | 1d | Cloud | **BLOCKS** waves |
| W1 | S40-03 | **Catalog/Import surfacing** | 2.5d | **Local lead (1 agent)** | Mandatory `impact()`; projection-side |
| W2 | S40-04 | **Perf P1 burn-down** | 1.5d | Cloud | Appendix to perf baseline |
| W3 | S40-05 | **Replay maint** | 1d | Cloud | Isolated fixtures only |
| W4 | S40-07 | **Evidence / playtest 12** | 1.5d | **Local** | PNG + playtest doc |
| W5 | S40-08 | **Hygiene / coord** | 0.5d | Cloud | sprint-status, coord map |
| W6 | S40-06 | **Closeout** | 0.5d | **Local** | Smoke + gates |

## Track ownership

| Track | Sub-track Owner | Stories | Stack prefix | Agent env |
|-------|-----------------|---------|--------------|-----------|
| Catalog/Import | team-data (single) | S40-03 | `stack/sprint40/catalog-import-projection` | **Local lead** |
| Perf P1 | team-simulation | S40-04 | `stack/sprint40/perf-p1` | Cloud |
| Replay | team-simulation | S40-05 | `stack/sprint40/replay-maint` | Cloud |
| Evidence | team-qa + team-unity | S40-07 | `stack/sprint40/evidence` | **Local** |
| Hygiene | coordinator | S40-08 | `stack/sprint40/hygiene` | Cloud |
| Closeout | c-sharp-devops-engineer | S40-06 | `stack/sprint40/closeout` | **Local** |

## File ownership matrix (no parallel edits)

| File / path | Owner track | Notes |
|-------------|-------------|-------|
| `src/ProjectAegis.Delegation/Projection/PlatformCatalogFilterProjection.cs` | Catalog only | Or prior sprint owner — single agent |
| `src/ProjectAegis.Delegation/Projection/*Catalog*` | Catalog only | GitNexus `impact()` required |
| `src/ProjectAegis.Delegation.UnityAdapter/Presentation/C2PresentationController.cs` | **None this sprint** | Do not touch unless QA exception |
| `production/perf/perf-profile-polish-baseline-*.md` | Perf P1 | Append only |
| `production/sprint-status.yaml` | Hygiene or Closeout | Coordinator merge |
| `tests/regression/replay-golden-*.txt` | Replay | Isolated fixtures only |

## Hard gates

- `dotnet test ProjectAegis.sln` — **≥1213**
- ReplayGolden **6/6**; C2 proxy **18/18+**
- ZERO `DelegationBridge.cs`; extend-only `CatalogWriteGate`
- Baltic hash `17144800277401907079` unchanged
- GitNexus `impact()` before Catalog edits; `detect_changes()` before commit

## Worktree bootstrap

See `production/agentic/s39-s48-worktree-manifest.md` §S40.

## Cut line / minimum

Must ship: S40-01–06. Catalog surfacing (S40-03) is non-droppable.

---

*Draft kickoff per 10-sprint agent program. Cites polish-scope-boundary + roadmap §Horizon 2.*
