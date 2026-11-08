# S25-10 story-done evidence — doctrine-emcon-readonly

**Branch:** `stack/sprint25/doctrine-emcon-readonly`  
**Base:** `main` @ `bd225ae` (S25-08/09)  
**Story:** `production/sprints/sprint-25-phase-b-damage-assurance.md` §S25-10 (S24-10 carryover)  
**Status:** Complete

## Deliverables

- Rebased EMCON-only changes from carryover `6e73584` onto current `main` (no S25-08 atlas revert)
- Read-only `EMCON` line on doctrine inheritance panel (projection → binder → UI Toolkit label)
- `DoctrineInheritanceEntry.EffectiveEmconLabel` resolved via `ScenarioEmconResolver` (no sim/catalog write path)
- Play Mode smoke harness + UXML validator assert `emcon-label` wiring
- **ZERO touch** `DelegationBridge.cs`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"

dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "Doctrine" -v minimal
# Passed: 9/9

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|Doctrine" -v minimal
# Passed: 16/16

dotnet test ProjectAegis.sln -v minimal
# Passed: 640/640

git diff main -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty — ZERO touch)
```

**Unity Play Mode (when Editor available):**

```bash
# From repo root with Unity 6.3 on PATH
./tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Batch doctrine
# -executeMethod ProjectAegis.Unity.Editor.C2PlayModeSignoffBatchRunner.RunDoctrineBatch
```

## Acceptance criteria

| AC | Verdict |
|----|---------|
| Rebase `doctrine-emcon-readonly` onto `main` without conflict (EMCON changes only) | **PASS** — cherry-picked EMCON files; S25-08 atlas preserved |
| Read-only EMCON line on doctrine panel binds from catalog/policy data | **PASS** — `emcon-label` bound via `FormatRadarEmconLabel` / `ScenarioEmconResolver` |
| `RunDoctrineBatch` / Doctrine filter PASS (Req 13 regression) | **PASS** — headless proxy + Doctrine filter green |
| PlayModeSmoke\|Doctrine filter green | **PASS** — 16/16 |
| Test floor ≥592 | **PASS** — 640/640 solution-wide |
| ZERO touch `DelegationBridge.cs` | **PASS** |