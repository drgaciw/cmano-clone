# Sprint 16 — DATA-3 scenario bind (post–#69)

**Date:** 2026-06-04  
**Branch:** `stack/sprint16-data-3-scenario-bind` (from `main`)  
**Worktree:** `.worktrees/sprint16-data-p0-impl`

## Prerequisite

- Merge [PR #69](https://github.com/drgaciw/cmano-clone/pull/69) (Wave 5 + requirements program)
- CI blocked by **GitHub Actions billing** — local **365/365** is green (`production/qa/pr-69-ci-triage-2026-06-04.md`)

## Gap (from gap analysis)

| Item | Status on `main` |
|------|------------------|
| DATA-1, DATA-2 | **DONE** (44 Data tests) |
| `DbSnapshotStore` | **DONE** |
| `ScenarioPolicyRepository` in Sim | **MOVE** → `ProjectAegis.Data` |
| `ScenarioPackage` + `dbSnapshotId` bind | **MISSING** |
| `SimulationModeConfigurator` policy source | **UPDATE** after move |

## GitNexus (before move)

```bash
npx gitnexus impact ScenarioPolicyRepository -d upstream -r cmano-clone
```

Callers: `DelegationBridge`, `BalticReplayHarness`, `SimulationSession`, MissionEditor CLI, PlayMode smoke.

## Acceptance (DATA-3)

- Policy JSON loads from Data layer path seam
- Scenario package references `dbSnapshotId`
- `dotnet test ProjectAegis.sln` green
- Replay goldens unchanged