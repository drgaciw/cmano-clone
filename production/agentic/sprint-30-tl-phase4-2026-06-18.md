# Sprint 30 — TL Export Phase 4 (S30-03)

**Date:** 2026-06-18  
**Story:** `production/epics/sprint-30-tl-export-phase34/story-030-03-tl-phase4-binding.md`  
**Epic:** sprint-30-tl-export-phase34  
**ADR:** ADR-006 (snapshot binding), ADR-011 (write-gate governance)  
**Prerequisite:** `production/agentic/sprint-30-tl-phase3-2026-06-18.md` (S30-02 COMPLETE)  
**Spike:** `production/agentic/sprint-28-tl-branching-spike-2026-06-18.md` — Phase 4 scenario `tlBranch`

## Verdict: **COMPLETE (Phase 4)**

Scenario package `tlBranch` field (`TL-0`…`TL-5`) validated at **load** by `ScenarioValidationEngine`. Binding resolved at authoring/load via `ScenarioPackage.TlBranch`. **No** `TlBranchDatabaseResolver`, **no** physical branch DBs, **ZERO** touch `DelegationBridge.cs`.

## Phase map (S28-11)

| Phase | Scope | Status |
|-------|-------|--------|
| 1 | Export manifest `tlTier` on workbook/JSON drops | Done (S29-02) |
| 2 | Migration `010` `catalog_snapshot.branch` | Done (S29-02) |
| 3 | Per-tier filtered `ICatalogReader` export filters | Done (S30-02) |
| **4** | Scenario package `tlBranch` + validation | **Done (S30-03)** |
| 5 | Optional physical SQLite fork per TL | Post-MVP |

## Implementation summary

### Scenario package shape

```json
{
  "metadata": {
    "dbRef": "baltic_patrol",
    "tlBranch": "TL-0",
    "seed": 42,
    "policyId": "baltic-patrol-catalog"
  }
}
```

- `ScenarioMetadataDto.TlBranch` — JSON `tlBranch` (camelCase loader)
- `ScenarioPackage.TlBranch` — normalized label bound at `FromDocument` / load
- `ScenarioDocumentEditor.CreateNew` — defaults `tlBranch` to `TL-0`

### Load-time validation (`ScenarioValidationEngine`)

| Code | Condition |
|------|-----------|
| `TL_BRANCH_MISSING` | `metadata.tlBranch` absent or whitespace |
| `TL_BRANCH_INVALID` | Value not in `TL-0`…`TL-5` |
| `TL_BRANCH_SNAPSHOT_MISMATCH` | Resolved `tlBranch` ≠ `catalog_snapshot.branch` for bound snapshot |

Rules run at validation/export gate — **not** mid-tick. `ICatalogReader.TryGetSnapshotBranch` reads branch metadata only (SQLite + in-memory fixture).

### CLI / MCP

- `scenario_validate` surfaces `tlBranch` findings via existing `ScenarioValidationExportGate` path (no CLI code change required)
- Curated fixtures migrated: `golden_clean.json`, `golden_strike_unreachable.json` → `"tlBranch": "TL-0"`

## Acceptance criteria

| AC | Status | Evidence |
|----|--------|----------|
| Scenario package carries `tlBranch`; validated at load | PASS | `TlBranchRule`; `TlBranchValidationTests` |
| Invalid/missing `tlBranch` → structured reject at load | PASS | `TL_BRANCH_*` codes; CLI test |
| `rg TlBranchDatabase\|BranchDatabase` → zero | PASS | grep gate below |
| CLI `scenario_validate` surfaces findings | PASS | `ScenarioValidateCliTests` |
| Binding at authoring/load only | PASS | No sim/resolver types; `ScenarioPackage.TlBranch` |
| WriteGate regression PASS | PASS | filtered Data.Tests 58/58 |
| Evidence doc | PASS | this file |
| ZERO touch `DelegationBridge.cs` | PASS | empty `git diff` |

## Verify commands (recorded)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Snapshot|TlTier|Scenario" -v minimal
# Passed: 58

dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform|Scenario" -v minimal
# Passed: 19

dotnet test ProjectAegis.sln -v minimal
# Passed: 896/896 (baseline 887 + 9 new)

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Passed: 6/6

rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true
# (empty)

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Rollback

- **Validation:** omit `TlBranchRule` call → pre-S30-03 behavior (no `tlBranch` gate)
- **Packages:** remove `tlBranch` from JSON — validation fails until field restored or rule reverted
- **Release train:** scenario `dbRef` rollback unchanged (P0 model)

## S30-04 merge-conflict risks

| Area | Risk | Mitigation |
|------|------|------------|
| `ScenarioValidationEngine` | Low | Additive rule at top of pipeline |
| `ICatalogReader` | Low | Default `TryGetSnapshotBranch` returns false |
| `CatalogWriteGate` | **None** | ZERO touch |
| Golden fixtures | Low | `tlBranch: TL-0` on curated validation JSON |

## References

- S30-02: `production/agentic/sprint-30-tl-phase3-2026-06-18.md`
- S29-02: `production/agentic/sprint-29-tl-export-phase12-2026-06-18.md`
- Spike: `production/agentic/sprint-28-tl-branching-spike-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md` § S30-03