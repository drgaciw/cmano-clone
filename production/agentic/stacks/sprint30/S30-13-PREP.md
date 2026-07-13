# S30-13 prep evidence — closeout hygiene (BLOCKED)

**Story:** `production/epics/sprint-30-closeout-devops/story-030-13-closeout-hygiene.md`  
**Status:** In Progress (prep only — **NOT Complete**)  
**Prepared:** 2026-06-18  
**Branch:** `main` @ `3406bc4`

## Verdict: **BLOCKED / PREP**

Full closeout cannot run until **S30-03+** must-have gate lands. Only S30-01 is complete; S30-02, S30-03, S30-04 remain open (`ready-for-dev` in `sprint-status.yaml`).

## Block reason

| Gate | Status | Notes |
|------|--------|-------|
| S30-03 TL Phase 4 `tlBranch` binding | **NOT LANDED** | Sprint fails without this; S30-13 dependency |
| S30-02 TL Phase 3 export | **NOT LANDED** | Blocks S30-03 |
| S30-04 Nightly ship approve scale | **NOT LANDED** | Must-have parallel path |
| Should-have cut line | **NOT APPLIED** | S30-05..08, S30-13 delivery pending wave 2/3 |

## Prep baseline gates (2026-06-18)

| Gate | Current | Closeout target | Delta |
|------|---------|-----------------|-------|
| `dotnet test ProjectAegis.sln` | **878/878 PASS** | **≥918** | **−40** |
| `ReplayGoldenSuiteTests` | **6/6 PASS** | 6/6 | met @ prep |
| `dotnet build ProjectAegis.sln` | not re-run @ prep | 0 errors | S30-01 green |
| GitNexus @ stack tip | not run @ prep | indexed evidence doc | deferred |
| `DelegationBridge.cs` | not verified @ prep | ZERO touch | deferred |

### Per-project counts @ prep

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 186 |
| ProjectAegis.Delegation.Tests | 212 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 169 |
| ProjectAegis.MissionEditor.Cli.Tests | 26 |
| ProjectAegis.Data.Tests | 280 |
| ProjectAegis.Data.Excel.Tests | 5 |
| **Total** | **878** |

## Verify commands (prep run)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test ProjectAegis.sln -v minimal
# 878/878 PASS

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "ReplayGoldenSuiteTests" -v minimal
# 6/6 PASS

git branch -a | grep sprint29 || true
# 0 refs @ prep
```

## Remaining AC checklist (final closeout — do NOT check until unblocked)

- [ ] `ReplayGoldenSuiteTests` — 6/6 PASS @ closeout stack tip
- [ ] `dotnet test ProjectAegis.sln` — **≥918** @ closeout
- [ ] `production/qa/sprint-30-gitnexus-*.md` with nodes/edges
- [ ] `production/qa/smoke-sprint-30-closeout-*.md` (end-of-sprint only)
- [ ] Tracker rows **06**, **18**, **21** updated
- [ ] `stack/sprint29/*` prune documented (merged branches only)
- [ ] `sprint-status.yaml` closeout counters + evidence list (**DEFER** — orchestrator marks sprint complete)
- [ ] ZERO touch `DelegationBridge.cs`

## Branch hygiene — `stack/sprint29/*`

**Verify @ S30-13 prep:** `git branch -a | grep sprint29` → **0 refs** (already pruned or merged direct-to-`main`).

Sprint 29 is **complete** (13/13 stories; `sprint29_status: complete`). All `stack/sprint29/*` branches from story `graphite_branch` fields are merged — **safe to prune** when local refs reappear.

### Prune candidates (merged — safe to delete)

```bash
git branch -d stack/sprint29/full-sln-gate
git branch -d stack/sprint29/tl-export-phase12
git branch -d stack/sprint29/corpus-approve
git branch -d stack/sprint29/unity-import-ui
git branch -d stack/sprint29/combat-baltic-enable
git branch -d stack/sprint29/catalog-sim-bridge
git branch -d stack/sprint29/doctrine-visual
git branch -d stack/sprint29/begin-execution
git branch -d stack/sprint29/hot-tick-damage
git branch -d stack/sprint29/balance-drift-pipeline
git branch -d stack/sprint29/datalink-side-picture
git branch -d stack/sprint29/closeout
```

### Exclude from prune (open Sprint 30 work)

Do **not** prune `stack/sprint30/*` — active sprint branches:

| Branch | Story | Status |
|--------|-------|--------|
| `stack/sprint30/tl-phase3-export` | S30-02 | ready-for-dev |
| `stack/sprint30/tl-phase4-binding` | S30-03 | ready-for-dev |
| `stack/sprint30/ship-approve-scale` | S30-04 | ready-for-dev |
| `stack/sprint30/combat-land-validator` | S30-05 | backlog |
| `stack/sprint30/presentation-evidence` | S30-06 | backlog |
| `stack/sprint30/planning-chrome` | S30-07 | backlog |
| `stack/sprint30/hot-tick-hits` | S30-08 | backlog |
| `stack/sprint30/baltic-flag-flip` | S30-09 | backlog |
| `stack/sprint30/datalink-lag` | S30-10 | backlog |
| `stack/sprint30/cmo-entity-slices` | S30-11 | backlog |
| `stack/sprint30/closeout` | S30-12, S30-13 | backlog / in-progress |

(Additional `stack/sprint29/*` refs may exist on other clones after S29 graphite work; prune only after confirming merged to `main`.)

## What remains for final S30-13 completion

1. **Unblock:** Land S30-02 → S30-03 (must-have gate) + S30-04; apply should-have cut line per sprint plan.
2. **Re-run full gates** at stack tip: build, sln ≥918, ReplayGolden 6/6, GitNexus `npx gitnexus analyze . --force`.
3. **Produce closeout artifacts:** `sprint-30-gitnexus-*.md`, `smoke-sprint-30-closeout-*.md`, `S30-13-DONE.md`.
4. **Update tracker** rows 06 (TL export/bind), 18 (combat domains), 21 (platform editor).
5. **Confirm prune** — re-verify `git branch -a | grep sprint29`; execute `git branch -d` if refs exist.
6. **DelegationBridge** empty-diff check.
7. **Defer** `sprint-status.yaml` sprint-complete counters to orchestrator.

## References

- S29-13 pattern: `production/agentic/stacks/sprint29/S29-13-DONE.md`
- S30-01 baseline: `production/agentic/stacks/sprint30/S30-01-DONE.md`
- Kickoff closeout target: `production/sprints/sprint-30-tl-bind-corpus-scale.md` (≥918)