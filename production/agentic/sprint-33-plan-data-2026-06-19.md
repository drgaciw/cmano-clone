# Sprint 33 — Data Track Plan

**Owner:** team-data  
**Sprint gate:** S33-02 dependency graph index operational  
**Epic:** `production/epics/sprint-33-kill-chain-intelligence/`  
**Updated:** 2026-06-19 (post–S32 closeout refresh)

## Predecessor context (S32 complete)

| Metric | S32 closeout | S33 day-1 target | S33 closeout target |
|--------|--------------|------------------|---------------------|
| Full sln tests | **1073/1073** | ≥1073 (S33-01 records baseline) | ≥1086 (+13 data track minimum) |
| ReplayGolden | **6/6 PASS** | 6/6 unchanged | 6/6 unchanged |
| Baltic `WORLD_HASH` | `17144800277401907079` | unchanged | unchanged |
| Data.Tests | 335/335 | 335 baseline | ~370–385 after S33-02..08 |
| Cli.Tests | 36/36 | 36 baseline | ~46–48 after S33-08 |

**S32 carryover:** **NONE** for data track. Sprint 32 closed **13/13** (QA approved @ 2026-06-19). Deferred P1 items land here:

- DBI-1.5 weapon→mount→sensor graph → **S33-02**
- DBI-3.5 kill-chain impossibility rules → **S33-03**

S32 prerequisites now green (no kickoff preemption):

- **S32-02** unified release-train manifest — `UnifiedReleaseTrainManifest` + `RecordUnifiedRelease` done
- **S32-03** mount/loadout quarantine triage — `MountLoadoutQuarantineTriage` + repair envelope done
- **S32-07** `catalog_release_diff` CLI — pattern reference for **S33-08**

## Stories owned

| ID | Story | Est. | Priority | req_trace |
|----|-------|------|----------|-----------|
| S33-02 | Weapon→mount→sensor dependency graph (DBI-1.5) | 2.5d | must-have | DBI-1.5, DBI-1.1, DBI-1.4, DBI-7.3 |
| S33-03 | Kill-chain impossibility rule pack (DBI-3.5) | 2.5d | must-have | DBI-3.5, DBI-3.1, DBI-3.3, DBI-2.1, DBI-2.2 |
| S33-05 | Orchestrator + write-gate kill-chain gate | 1.5d | should-have | DBI-8.1, DBI-7.2, DBI-3.4 |
| S33-08 | `catalog_dependency_graph` + `catalog_kill_chain_report` CLI | 1d | should-have | DBI-1.5, DBI-4.5 |

**Data track capacity:** 7.5d planned / 8 effective dev-days (20% buffer in sprint).

## Wave ordering (strict)

Data stories are **sequential within track**; parallel only with sim/unity after day-1 gate.

```
Day-1 gate (DevOps)
  S33-01 full-sln re-baseline ──BLOCKS──► S33-02

Wave 1 (data, parallel with S33-04 / S33-06 on other tracks)
  S33-02 dependency-graph-index

Wave 2 (data, parallel with S33-07 on sim track)
  S33-03 kill-chain-rule-pack  ◄── hard dep on S33-02

Wave 3 (data only — both deps satisfied)
  S33-05 orchestrator-kill-chain-gate  ◄── hard dep on S33-03
  S33-08 kill-chain-cli                ◄── hard deps on S33-02 + S33-03
```

| Wave | Story | Blocker | Can start when |
|------|-------|---------|----------------|
| Day-1 | — | S33-01 | S32-13 closeout ✅ (cleared) |
| Wave 1 | S33-02 | S33-01 PASS | Day-1 gate green |
| Wave 2 | S33-03 | S33-02 merged | Graph index + `GetSortedDependencyEdges()` live |
| Wave 3a | S33-05 | S33-03 merged | `KILL_CHAIN_*` codes in `CatalogRulesValidationAgent` |
| Wave 3b | S33-08 | S33-02 + S33-03 merged | Graph API + rule pack both stable |

**Day-1 gate (mandatory):** Do **not** branch `stack/sprint33/dependency-graph-index` until **S33-01** records ≥1073/1073, ReplayGolden 6/6, and GitNexus @ trunk. S33-02 AC assumes S33-01 baseline, not S32 tip alone.

## Graphite stack

```
stack/sprint33/full-sln-gate          # S33-01 (DevOps — day-1 gate)
  └─ stack/sprint33/dependency-graph-index   # S33-02 Wave 1
       └─ stack/sprint33/kill-chain-rule-pack   # S33-03 Wave 2
            ├─ stack/sprint33/orchestrator-kill-chain   # S33-05 Wave 3a
            └─ stack/sprint33/kill-chain-cli            # S33-08 Wave 3b
```

## Hard gates (every data merge)

| Gate | Command / rule | Applies |
|------|----------------|---------|
| **CatalogWriteGate extend-only** | `npx gitnexus impact CatalogWriteGate` before edit; no signature/behavior removal | S33-02, S33-05 |
| **ZERO BranchDatabase** | `rg -l "TlBranchDatabase\|BranchDatabase" src/ --glob "*.cs"` → empty | all |
| **No full corpora in CI** | Baltic patrol fixture + `ship-slice-100.md` only; never 7208/4844/4403 in `dotnet test` | S33-02, S33-03 |
| **ZERO touch** | `git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` → empty | all |
| **Detect-only rules** | No auto-repair of `KILL_CHAIN_*` findings (DBI-2.3) | S33-03, S33-05 |
| **Read-only CLI** | S33-08 verbs must not call `ApproveBatch` or mutate live tables | S33-08 |
| **Baltic hash pin** | Production replay hash `17144800277401907079` unchanged | all |

## Test targets per story

Baseline @ S32 closeout verified 2026-06-19: Data.Tests **335**, WriteGate filter **26**, DatabaseIntelligence filter **2**, Cli.Tests **36**.

### S33-02 — Dependency graph index (sprint gate)

| Metric | Target |
|--------|--------|
| New tests | **+10–14** |
| Filter PASS | `DependencyGraph\|WriteGate\|Platform` → **≥38** (26 WriteGate + ~12 graph) |
| Full Data.Tests | **≥347** after merge |
| Evidence | `production/agentic/sprint-33-dependency-graph-*.md` |

**Must cover:** `GetSortedDependencyEdges()` stable `(platformId, mountId, weaponId, sensorId)` keys; platform→mount→weapon + platform→sensor edges; quarantined bindings excluded; cache invalidated on `CatalogWriteGate.ApproveBatch` commit (mirror `SqliteCatalogReader` sensor cache pattern); Baltic + `ship-slice-100` fixtures only.

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "DependencyGraph|WriteGate|Platform" -v minimal
npx gitnexus impact CatalogWriteGate
npx gitnexus impact ICatalogReader
```

### S33-03 — Kill-chain rule pack (sprint gate)

| Metric | Target |
|--------|--------|
| New tests | **+12–18** |
| Filter PASS | `KillChain|CatalogRules|Validation|CrossSystem` → **≥14** |
| Full Data.Tests | **≥361** after merge |
| Evidence | `production/agentic/sprint-33-kill-chain-rules-*.md` |

**Must cover:** R1 `KILL_CHAIN_ORPHAN_EDGE`, R2 `KILL_CHAIN_RANGE_EXCEEDS_SENSOR`, R3 `KILL_CHAIN_SPEED_MISMATCH`, R4 `KILL_CHAIN_WEAPON_EXCEEDS_PLATFORM_REACH`; deterministic sorted findings; Baltic golden hash stable; `DatabaseIntelligenceOrchestrator.RunBalticDefault()` surfaces new codes; detect-only (no live-table mutation on report path).

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "KillChain|CatalogRules|Validation|CrossSystem" -v minimal
npx gitnexus impact CatalogRulesValidationAgent
```

### S33-05 — Orchestrator kill-chain gate

| Metric | Target |
|--------|--------|
| New tests | **+6–10** |
| Filter PASS | `DatabaseIntelligence|WriteGate|KillChain` → **≥36** |
| Full Data.Tests | **≥371** after merge |

**Must cover:** Orchestrator step ordering documented + tested; blocking `KILL_CHAIN_*` errors prevent `ApproveBatch` on platform/weapon/mount batches; quarantined bindings excluded from sim export path; WriteGate regression **26/26** unchanged.

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "DatabaseIntelligence|WriteGate|KillChain" -v minimal
npx gitnexus impact DatabaseIntelligenceOrchestrator
```

### S33-08 — Kill-chain CLI verbs

| Metric | Target |
|--------|--------|
| New tests | **+8–12** |
| Filter PASS | `KillChain|DependencyGraph` in Cli.Tests → **≥10** |
| Full Cli.Tests | **≥46** after merge |

**Must cover:** `catalog_dependency_graph` + `catalog_kill_chain_report` registered with `--help`; deterministic sorted stdout; empty report golden on clean Baltic re-import; read-only (no `ApproveBatch`). Follow S32-07 `catalog_release_diff` command + `Program.cs` wiring pattern.

```bash
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "KillChain|DependencyGraph" -v minimal
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_dependency_graph --help
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_kill_chain_report --help
```

## GitNexus-safe file touch hints

Run `npx gitnexus impact <symbol>` before editing any **HIGH/CRITICAL** symbol.

### S33-02 — expected touches

| Risk | Path | Notes |
|------|------|-------|
| **CRITICAL** | `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs` | Extend-only: wire graph cache invalidation on commit |
| **HIGH** | `src/ProjectAegis.Data/Catalog/ICatalogReader.cs` | Add `GetSortedDependencyEdges()` (+ default empty impl) |
| **HIGH** | `src/ProjectAegis.Data/Catalog/SqliteCatalogReader.cs` | Materialize + cache edges; invalidate on commit hook |
| NEW | `src/ProjectAegis.Data/Catalog/CatalogDependencyGraphIndex.cs` | Sorted edge builder |
| NEW | `src/ProjectAegis.Data/Catalog/CatalogDependencyEdge.cs` | Edge DTO / record |
| LOW | `src/ProjectAegis.Data/Catalog/InMemoryCatalogReader.cs` | Fixture edges for unit tests |
| LOW | `src/ProjectAegis.Data/Catalog/CatalogReaderFactory.cs` | Baltic reader factory if needed |
| NEW | `src/ProjectAegis.Data.Tests/Catalog/DependencyGraphIndexTests.cs` | Primary test home |
| LOW | `src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGate*Tests.cs` | Commit-invalidation regression only |

### S33-03 — expected touches

| Risk | Path | Notes |
|------|------|-------|
| **HIGH** | `src/ProjectAegis.Data/Agents/CatalogRulesValidationAgent.cs` | Add R1–R4 `KILL_CHAIN_*` findings |
| NEW | `src/ProjectAegis.Data/Validation/KillChainRules.cs` | Bounded rule helpers (optional split) |
| NEW | `src/ProjectAegis.Data.Tests/Validation/KillChainRulePackTests.cs` | Golden + per-rule fixtures |
| LOW | `src/ProjectAegis.Data.Tests/Agents/DatabaseIntelligenceOrchestratorTests.cs` | Baltic surfaces new codes |

### S33-05 — expected touches

| Risk | Path | Notes |
|------|------|-------|
| **HIGH** | `src/ProjectAegis.Data/Agents/DatabaseIntelligenceOrchestrator.cs` | Kill-chain gate in pipeline ordering |
| **CRITICAL** | `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs` | Block commit on blocking `KILL_CHAIN_*` |
| LOW | `src/ProjectAegis.Data/Catalog/MountLoadoutQuarantineTriage.cs` | Read-only partition alignment if needed |
| NEW | `src/ProjectAegis.Data.Tests/Agents/OrchestratorKillChainGateTests.cs` | ApproveBatch block + quarantine partition |

### S33-08 — expected touches

| Risk | Path | Notes |
|------|------|-------|
| NEW | `src/ProjectAegis.MissionEditor.Cli/CatalogDependencyGraphCommand.cs` | Read-only graph export |
| NEW | `src/ProjectAegis.MissionEditor.Cli/CatalogKillChainReportCommand.cs` | Read-only report export |
| LOW | `src/ProjectAegis.MissionEditor.Cli/Program.cs` | Verb registration (mirror `catalog_release_diff`) |
| NEW | `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogDependencyGraphCommandTests.cs` | |
| NEW | `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogKillChainReportCommandTests.cs` | |

### ZERO touch (all stories)

- `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`
- `src/ProjectAegis.Sim/**` (sim owns S33-04)
- Full corpora paths (`sensor.md`, `ship.md`, `weapon.md` bulk) in test fixtures

## Track verify (closeout)

```bash
export PATH="/home/username01/.dotnet:$PATH"

# Day-1 gate
dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal   # ≥1073 day-1; ≥1086 closeout

# Data track aggregate
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|DependencyGraph|KillChain|CrossSystem|DatabaseIntelligence" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "KillChain|DependencyGraph" -v minimal

# Hard gates
npx gitnexus impact CatalogWriteGate
npx gitnexus impact CatalogRulesValidationAgent
npx gitnexus impact DatabaseIntelligenceOrchestrator
rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Cut line (data track)

If Wave 3 slips, drop **S33-08** (CLI) before **S33-05** (orchestrator gate). Minimum shippable data deliverable beyond must-have: **S33-05** only.

## Related artifacts

- Sprint plan: `production/sprints/sprint-33-kill-chain-intelligence-comms-integration.md`
- Parallel kickoff: `production/agentic/sprint-33-parallel-kickoff-2026-06-19.md`
- QA plan: `production/qa/qa-plan-sprint-33-2026-11-27.md`
- Stories: `production/epics/sprint-33-kill-chain-intelligence/story-033-0{2,3,5,8}-*.md`