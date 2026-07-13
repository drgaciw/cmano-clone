# Sprint 16 DATA-3 — GitNexus notes (2026-06-04)

**Branch:** `stack/sprint16-data-3-scenario-bind`  
**Worktree:** `.worktrees/sprint16-data-p0-impl`

## Impact (pre-edit)

`npx gitnexus impact ScenarioPolicyRepository -d upstream -r cmano-clone` returned **0 callers** (stale index). Manual grep confirmed consumers: `DelegationBridge`, `BalticReplayHarness`, `SimulationSession`, MissionEditor CLI, PlayMode smoke.

## Changes (blast radius)

| Symbol | Layer | Risk |
|--------|-------|------|
| `ScenarioPackage` | Data | LOW — new |
| `ScenarioPackageLoader` | Data | LOW — new |
| `ScenarioPolicyJsonCatalog` | Data | MEDIUM — JSON policy cache moved from Sim |
| `ScenarioPolicyJsonDto` (+ nested) | Data | MEDIUM — namespace move Sim→Data |
| `ScenarioPolicyRepository` | Sim | MEDIUM — delegates to Data catalog |
| `SimulationModeConfigurator.ApplyFromPackage` | Delegation | LOW |

## Verification

- `dotnet test ProjectAegis.sln -c Release` → **354/354 PASS** (main baseline; no Wave 5 tests on this branch)
- Replay goldens unchanged (no sim tick logic edits)

## detect_changes

Run after merge: `npx gitnexus analyze` then `npx gitnexus detect_changes --repo cmano-clone`