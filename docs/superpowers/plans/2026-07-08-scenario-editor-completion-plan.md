# Scenario Editor Requirements Completion Plan (Headless + Unity AC-8)

> **For agentic workers:** REQUIRED SUB-SKILLS: `dispatching-parallel-agents`, `subagent-driven-development`, `using-git-worktrees`, `test-driven-development` for code tracks.  
> **Governing docs:** `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`, `design/gdd/agentic-mission-editor.md`, `production/scenario-editor-scope-boundary-2026-07-04.md`, `docs/reports/roadmap-execute-plan-07042026.md`, `docs/reports/future-sprint-roadpmap-07042026.md`, `production/qa/qa-plan-scenario-editor-2026-07-01.md`, S81–S88 sprint plans + closeouts.

**Goal:** Complete the **headless scenario editor (req 11) program** with honest AC/doc alignment, formal program close hygiene, and **productionized Unity AC-8** load/inspect (beyond thin proxy) — **without** Phase 2 map-first GUI or reopening Baltic/v2 invariants.

**Architecture:** Multi-wave program on trunk **Release**. Core truth stays in `ProjectAegis.Data` + `MissionEditor.Cli`; Unity is a **thin loader/presenter** only (ADR-008, ADR-017). Parallel tracks per wave; GitNexus impact before symbol edits.

**Tech Stack:** .NET 8 / ProjectAegis.Data + MissionEditor.Cli + Delegation.UnityAdapter tests; Unity 6.3 optional for Editor evidence; docs under `Game-Requirements/`, `production/`, `docs/reports/`.

---

## Context (truth from parallel audit 2026-07-08)

### What is already shipped (headless Partial+)

| Surface | Evidence |
|---------|----------|
| Canonical document + schema | `ScenarioDocumentEditor`, `data/scenarios/scenario-document.schema.json`, examples |
| Validation Engine + export gate | `ScenarioValidationEngine`, `ScenarioValidationExportGate`, AC-1/3/4/9/12 tests under `src/**.Tests` |
| CLI verbs (ferry, undo, export, sample, publish, …) | `ProjectAegis.MissionEditor.Cli/Program.cs` (~39 cases) |
| Determinism / AC-6 | `ScenarioSimulateSampleCliTests`, `tools/ci/smoke-ac6.sh` |
| Event debugger / teleport export | Partial+ (`EventDebuggerTrace`, `TeleportUnitExportTests`) |
| AC-8 proxy | `PlayModeSmokeHarnessTests` headless load path (not full Editor map host) |

### What blocks “complete”

| Gap | Severity |
|-----|----------|
| Doc 11 AC-1…12 still `[ ]` with **phantom** `tests/unit/editor/**` paths | Honesty **BLOCKER** for program close |
| Mapping text still claims ferry/undo missing in places | Stale |
| S88 human ack / tracker / stable roadmap alias **inconsistent** | Governance |
| MCP manifest lag vs `Program.cs` verbs | Tooling residual |
| `mission_update_support` missing (add exists) | Small code residual |
| AC-8 true Unity load + `editorState` defaults evidence | User-selected: **in scope** |
| Phase 2 GUI (map, Mission Board, visual graph, NL agents, CMO import, Lua) | **Out of this plan** |

### Standing invariants (never break)

| Invariant | Rule |
|-----------|------|
| Test floor | ≥1232 (monotonic) |
| ReplayGolden | 6/6 v2 |
| Hash | `17144800277401907079` |
| PlayModeSmoke | 18/18 (+ AC-8 additive tests OK) |
| `DelegationBridge.cs` | **ZERO hotpath edits** |
| `CatalogWriteGate` | **Extend-only** (scenario edits are **file-based**, not write-gate) |
| Baltic policies/goldens | Frozen v2/v3 isolation |
| Stage | **Release** (no Launch side-effect) |

---

## Locked decisions (user 2026-07-08)

| Decision | Choice |
|----------|--------|
| Completion meaning | **Headless + Unity AC-8 productionize** |
| Status ambiguity | **Wave 0 hygiene first** (ack, alias, tracker, then code/docs) |
| Phase 2 map/GUI | **Deferred** to a separate scope boundary |
| S56 / Baltic | Do not re-litigate closed content trains |

---

## Program packaging

**Epic (new):** `scenario-editor-completion`  
**Path:** `production/epics/scenario-editor-completion/EPIC.md`  
**Stories:** SE-001…SE-004 (one per wave below)  
**Does not reopen:** `requirements-corpus-maturity` (complete); Baltic v3 content gate.

| Wave | Story | Goal | Duration (est.) |
|------|-------|------|-----------------|
| **SE-W0** | SE-001 | Hygiene + formal close prep | 1–2 days |
| **SE-W1** | SE-002 | Doc honesty + headless residual code | 3–5 days |
| **SE-W2** | SE-003 | Unity AC-8 productionize + evidence | 4–6 days |
| **SE-W3** | SE-004 | Program gate + human sign-off | 1–2 days |

---

## SE-W0 — Integration & truth hygiene (first)

**Goal:** One coherent status story on trunk before new product code.

### Parallel tracks (docs/governance)

| Track | Owner skill/agent | Deliverable |
|-------|-------------------|-------------|
| **W0-a** | producer | Confirm/record human ack phrase for “scenario editor headless program complete” (or explicit PENDING with owner) |
| **W0-b** | producer / tech writer | Flip stable alias `docs/reports/future-sprint-roadpmap.md` → `future-sprint-roadpmap-07042026.md` **or** publish new dated post-S88 snapshot + alias |
| **W0-c** | producer | Tracker: headless S81–S88 **program-complete (Partial+ headless)**; next stack = AC-8 productionize / Phase 2 deferred |
| **W0-d** | devops | Reconcile `sprint-status` single narrative; verify `fix-scenario-publish-cli-wiring` / PR #237 state vs main (merge only if still needed and gates green) |
| **W0-e** | gitnexus | Post-merge `node .gitnexus/run.cjs analyze` if code lands; impact snapshot for `ScenarioDocumentEditor`, `ScenarioValidationEngine` |

### Acceptance (W0)

1. Single written status: **headless engineering complete | AC-8 remaining | Phase 2 out**  
2. Roadmap alias or new dated snapshot points at editor program (not Baltic-v3-as-current only)  
3. Tracker row 11 Post-S56 note updated (no false “ferry missing”)  
4. No production sim edits in W0 unless merge-only  

### Out

- New features, Unity work, AC checkbox mass-edit without evidence map (that is W1)

---

## SE-W1 — Headless honesty + small code residuals

**Goal:** Make req 11 / GDD / tests tell the same story; close small tool gaps.

### Parallel tracks

#### W1-a — Doc honesty (req 11 + light GDD) — **docs-only**

**Files:**

- `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`
- Optionally `design/gdd/agentic-mission-editor.md` (v1 map fantasy → phased)

**Steps:**

1. Build AC → real test matrix (from audit):

| AC | Real evidence (check only if green) |
|----|-------------------------------------|
| AC-1 | `ScenarioValidationEngineTests` / `ReachabilityCalculatorTests` |
| AC-2 | `ScenarioSimulateSampleCliTests` |
| AC-3 | `ScenarioValidationEngineTests` / `ValidationGoldenTests` |
| AC-4 | `DoctrineInheritanceValidateTests` + `data/scenarios/validation/doctrine-inheritance.json` |
| AC-5 | `SampleCompletePipelineTests` + ferry/support verbs |
| AC-6 | `tools/ci/smoke-ac6.sh` (wording: **≤2 hunks** / editVersion honesty) |
| AC-7 | `EventDebuggerTests` — mark **Partial** if stub-pinned fields incomplete |
| AC-8 | Leave unchecked until W2 (or Partial proxy with explicit note) |
| AC-9 | `DerivedOnlyInvariantTests` / `EditorStateSchemaLint` |
| AC-10 | `ScenarioEditVersionGuardTests` + CLI CONFLICT tests |
| AC-11 | `TeleportUnitExportTests` |
| AC-12 | `SaveVsExportGateTests` |

2. Replace phantom paths with real `src/**.Tests` paths.  
3. Check boxes only with cited green tests; Partial ACs stay open with note.  
4. Refresh Implementation Mapping: ferry/undo/support **Shipped**; drop “ferry missing”.  
5. Footer: Charter honesty SE-W1 date.

#### W1-b — MCP manifest + CLI test parity — **code + tests (TDD)**

**Files:**

- `tools/mission-editor/mcp-tools.json`
- `src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs`
- Add `ScenarioExportCliTests.cs` (and migrate/umpire if cheap)

**Steps (TDD):**

1. RED: extend `McpToolsManifestTests` to require verbs that exist in `Program.cs` (at least `scenario_export`, `scenario_undo`, ferry verbs, `scenario_event_trace`).  
2. GREEN: update `mcp-tools.json`.  
3. RED/GREEN: CLI test for `scenario_export` exit 0 + export gate reject path.  

**Constraints:** No DelegationBridge; no CatalogWriteGate path changes.

#### W1-c — `mission_update_support` symmetry — **code + tests (TDD)**

**Files:**

- `src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentEditor.cs` (if method missing)
- `src/ProjectAegis.MissionEditor.Cli/MissionUpdateSupportCommand.cs` (new, mirror ferry)
- `Program.cs` case + MCP entry
- Tests: mirror `MissionAddFerryCommandTests` / update ferry pattern

**GitNexus:** `impact(ScenarioDocumentEditor, upstream)` before edit; warn on CRITICAL.

#### W1-d — AC-7 honesty pin (optional small)

If debugger is still stub-pinned: either (1) document Partial in req 11 only, or (2) extend `EventDebuggerTrace` fields to match AC JSON contract under TDD — **prefer docs Partial** unless easy.

### Acceptance (W1)

1. Doc 11 AC paths real; checked ACs have green cited tests  
2. Mapping no longer claims ferry/undo missing  
3. MCP manifest matches Program.cs core editor verbs  
4. `mission_update_support` CLI + test green  
5. Full solution tests ≥ floor; ReplayGolden 6/6; hash grep; ZERO bridge  

---

## SE-W2 — Unity AC-8 productionize

**Goal:** Scenario authored headless loads in Unity host path with intact ORBAT/missions/events and **`editorState` defaults** (camera theater centroid, layers on). Evidence pack for QA.

### Architecture constraints

- Load via **Data** APIs (`ScenarioDocumentDto` / package loader) — **no** validation rewrite in Unity  
- `editorState` is **derived-only** (AC-9); never read by Validation Engine / sim  
- **Do not** implement map placement (AME-4.x)  
- Prefer extending `PlayModeSmokeHarnessTests` + thin host binder over new engine systems  
- **ZERO** `DelegationBridge` production hotpath edits; presentation-only projection OK if already pattern-compliant  

### Parallel tracks

| Track | Work | Files (indicative) |
|-------|------|---------------------|
| **W2-a** | Load path + assertions | `ProjectAegis.Delegation.UnityAdapter` thin host **or** harness-only if host exists; `PlayModeSmokeHarnessTests` new Facts |
| **W2-b** | Fixture selection | `data/scenarios/examples/strike-package.scenario.json` or `baltic-patrol.scenario.json` (examples only — not frozen v3 golden mutation) |
| **W2-c** | Manual QA evidence pack | `production/qa/ac8-unity-roundtrip-evidence-YYYY-MM-DD.md` (screenshots checklist if Editor available; else headless PlayMode log evidence) |
| **W2-d** | Doc 11 AC-8 | Check box only when W2-a green; cite test name |

### TDD outline (W2-a)

1. RED: `AC8_headless_scenario_json_loads_with_intact_missions_and_editorState_defaults`  
2. Implement minimal load + default `editorState` fill  
3. GREEN; keep PlayModeSmoke suite green (18/18 baseline + new tests)  

### Acceptance (W2)

1. Automated AC-8 test green in UnityAdapter.Tests  
2. Evidence pack path recorded  
3. Doc 11 AC-8 checked with real path  
4. No Baltic golden / hash / bridge changes  

---

## SE-W3 — Program gate + human sign-off

**Goal:** Certificate that **headless + AC-8** scenario editor requirements for this program are complete.

### Serial closeout

1. Full gates RUN+READ:

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal   # ≥1232, 0 unexpected fails
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/... --filter PlayModeSmokeHarnessTests
# + AC-8 filter if separate
bash tools/ci/smoke-ac6.sh   # or documented CI path
rg -n '17144800277401907079' tests/ data/ | head
# ZERO new DelegationBridge hotpath
```

2. Gate doc: `production/gate-checks/se-completion-gate-YYYY-MM-DD.md`  
   - AC-1…12 table with status Met / Partial / Phase 2 deferred  
   - QA plan 19 units addressed/waived  
   - Invariants checklist  

3. Human ack phrase: **“Scenario editor headless + AC-8 program complete”**  
4. Tracker: Partial+ headless+AC-8; next = Phase 2 GUI (new boundary)  
5. Optional: epic story SE-004 Complete; close `scenario-editor-completion` epic  

### Explicit still-deferred (not failure)

| Item | Defer to |
|------|----------|
| Map-first ORBAT / Mission Board / visual event graph | Phase 2 boundary |
| Full event static analysis beyond stub | Phase 2 |
| Reversible migration disk | Optional residual train |
| NL agents / CMO import / Lua | ADR-013/014/015 later |
| NFR 5k ORBAT | Waived / opportunistic |

---

## Superpowers execution model

```
Orchestrator
├── SE-W0 serial/parallel hygiene (docs first)
├── SE-W1 parallel: W1-a docs ∥ W1-b MCP ∥ W1-c support update
│     └── review + full gates
├── SE-W2 parallel: W2-a Unity test ∥ W2-c evidence (local Unity if needed)
│     └── doc AC-8
└── SE-W3 gate + human ack
```

| Phase | Skills |
|-------|--------|
| Isolation | `using-git-worktrees` → `.worktrees/se-w{N}-*` |
| Code | `test-driven-development` + `team-csharp` / `c-sharp-test-engineer` |
| Unity | `team-unity` / `unity-specialist` (W2 only) |
| Docs honesty | general-purpose + design-review |
| Safety | GitNexus impact before `ScenarioDocumentEditor` / Validation edits |
| Memory | Hindsight `dev-cmano-clone` optional retain after each wave |

---

## Critical files map

| Path | Waves |
|------|-------|
| `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` | W0 note, W1 honesty, W2 AC-8 |
| `Game-Requirements/implementation-tracker-2026-07-04.md` | W0, W3 |
| `docs/reports/future-sprint-roadpmap.md` (+ dated) | W0 |
| `tools/mission-editor/mcp-tools.json` | W1-b |
| `src/ProjectAegis.MissionEditor.Cli/Program.cs` | W1-b/c |
| `src/ProjectAegis.Data/Scenario/Authoring/*` | W1-c, careful W2 load |
| `src/ProjectAegis.Data.Tests/**` | W1, pins |
| `src/ProjectAegis.MissionEditor.Cli.Tests/**` | W1 |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs` | W2 |
| `production/gate-checks/se-completion-gate-*.md` | W3 |
| `production/epics/scenario-editor-completion/*` | all |

**Never touch:** `DelegationBridge.cs` hotpath, Baltic v2/v3 goldens, production hash, CatalogWriteGate existing write paths.

---

## Verification (every code wave)

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj -v minimal
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "FullyQualifiedName~Scenario|Validation|EventDebugger|SaveVsExport" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests -v minimal
# hash + bridge
rg -n '17144800277401907079' tests/ data/ | head
# GitNexus detect_changes before commit if code changed
```

---

## Success definition

| Criterion | Met when |
|-----------|----------|
| Headless ACs honest | Doc 11 checkboxes match real tests; no phantom paths |
| Tool surface | MCP + CLI support update; ferry/undo documented shipped |
| AC-8 | Automated productionized load test + evidence |
| Program close | Gate doc + human ack; tracker Partial+ headless+AC-8 |
| Invariants | Floor, ReplayGolden, hash, bridge, stage Release |

---

## Out of scope (Phase 2+ program — separate plan)

- Map drawing, sides UI, Mission Board, operations timeline UI  
- Full event graph static analysis productization  
- CMO `.scen` import (ADR-013), Lua (ADR-014), agent labeling (ADR-015)  
- Scenario Lab fork (ADR-017)  
- Baltic content / commercial launch (E7)  

---

## Next step after plan approval

1. Create epic `scenario-editor-completion` + story SE-001  
2. Worktree `docs/se-w0-hygiene` → execute SE-W0  
3. Fan-out SE-W1 parallel tracks  
4. SE-W2 Unity (local)  
5. SE-W3 gate + user-instructed commit/merge  

**Recommended first execution mode:** Subagent-driven (fresh agent per track) with orchestrator review between waves.
