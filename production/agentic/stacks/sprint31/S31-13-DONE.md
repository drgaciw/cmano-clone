# S31-13 story-done — Closeout Hygiene

**Story:** `production/epics/sprint-31-closeout-devops/story-031-13-closeout-hygiene.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Deliverables

- `npx gitnexus analyze . --force` @ stack tip — evidence `production/qa/sprint-31-gitnexus-2026-06-18.md`
- `ReplayGoldenSuiteTests` **6/6 PASS**
- Full solution `dotnet test ProjectAegis.sln` — **1006/1006 PASS** (closeout gate ≥996)
- Implementation tracker rows **06**, **18**, **21** updated — corpus approve complete, combat Phase 5, presentation sign-off
- `stack/sprint30/*` local branch refs documented for prune (**0 refs** @ closeout verify)
- **ZERO touch** `DelegationBridge.cs`
- Build fix: `IReadOnlySet<>` → `IEnumerable<string>` in `CatalogDamageHotTickApplier` (netstandard2.1)

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "ReplayGoldenSuiteTests" -v minimal
npx gitnexus analyze . --force
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# 1006/1006; ReplayGolden 6/6; GitNexus 14160/28928
```

## Acceptance criteria

| AC | Evidence | Verdict |
|----|----------|---------|
| Replay 6/6 | `ReplayGoldenSuiteTests` | **PASS** |
| Full sln ≥996 | `smoke-sprint-31-closeout-2026-06-18.md` — 1006/1006 | **PASS** |
| GitNexus @ stack tip | `sprint-31-gitnexus-2026-06-18.md` | **PASS** |
| Tracker rows 06/18/21 | `implementation-tracker-2026-06-04.md` | **PASS** |
| Prune `stack/sprint30/*` documented | GitNexus closeout section | **PASS** |
| Closeout smoke doc | `smoke-sprint-31-closeout-2026-06-18.md` | **PASS** |
| `sprint-status.yaml` closeout counters | Sprint 31 marked complete @ 1006/1006 | **PASS** |
| ZERO touch `DelegationBridge.cs` | empty diff vs HEAD | **PASS** |

## Verdict

**COMPLETE** — Sprint 31 hygiene gates green; 12/13 stories landed (S31-12 CI hygiene deferred).