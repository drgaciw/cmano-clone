# Project Aegis — Agent Reference

## What This Is

**Project Aegis** is a near-future hardcore military simulation inspired by Command: Modern Air/Naval Operations. The player commands theater-level forces and delegates tactical decisions to autonomous AI agents. The codebase is a **.NET 8 C# solution** (`ProjectAegis.sln`) — engine-agnostic at the core, with Unity 6.3 LTS as the optional rendering layer.

Production stage: **Release** (S48 gate PASS; S73–S80 Baltic v3 content COMPLETE as of 2026-06-26).

---

## Build & Test Commands

> **Prerequisite**: .NET SDK **8.0.400** exactly (enforced by `global.json`). If `dotnet` is missing:
> ```bash
> curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 8.0.400
> export PATH="$HOME/.dotnet:$PATH"
> ```

Run from repo root (`cmano-clone/`):

| Task | Command |
|------|---------|
| Restore | `dotnet restore ProjectAegis.sln` |
| Build | `dotnet build ProjectAegis.sln` |
| Full test suite (≥1638) | `dotnet test ProjectAegis.sln -v minimal` |
| Play Mode smoke (C2 proxy) | `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests` |
| Console demo | `dotnet run --project src/ProjectAegis.Delegation.Demo` |
| Format check | `dotnet format --verify-no-changes` |

**Required verification before marking any work complete (RUN+READ all):**

```bash
dotnet build ProjectAegis.sln                   # 0 errors, 0 warnings
dotnet test ProjectAegis.sln -v minimal          # ≥1638 / 0 failures (post S95 gauntlet land; prior floor ≥1599)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests   # ≥20/20
```

Run `.\tools\verify-ci-local.ps1` (PowerShell) for full CI parity including replay golden and secret scan steps.

---

## Unity / C# architecture skill (required)

When the task or diff touches **UnityAdapter**, **MonoBehaviour**, **C2 / UI chrome**, **EditorWindow**, **asmdef**, **snapshot / projection bind**, or **presentation bridges**:

1. Load skill: `production/agentic/skills/unity-csharp-architect/` (`SKILL.md`)
2. Before claiming Done, run [`checklists/pr-finish.md`](production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md)
3. Paste the **PASS / FAIL / BLOCKED** verdict block into the PR body
4. Presentation boundary cites **ADR-010 §2–3**, **ADR-007**, **ADR-001** — **never** Git **ADR-018** (sensor side-picture / datalink)
5. Prefer headless `dotnet test` proof before Play Mode; UI is a **client**, not sim authority

Optional advisory patterns: [`checklists/soft-ci-rg.md`](production/agentic/skills/unity-csharp-architect/checklists/soft-ci-rg.md) — **not** a product-suite floor.

Implementation under contract → `/c-sharp-engineer`. Reviewer prompts → [`checklists/review-gates.md`](production/agentic/skills/unity-csharp-architect/checklists/review-gates.md).

---

## Architecture: Project Map

```
ProjectAegis.sln
├── ProjectAegis.Delegation          # Agent delegation framework (CORE — engine-agnostic)
│   ├── Orchestration/               #   DelegationOrchestrator.Tick() — main entry point
│   ├── Decision/                    #   Trait-weighted stochastic DecisionPipeline
│   ├── Core/                        #   AutonomyLevel, Order, SimulationMode, Identifiers
│   ├── Mission/                     #   MissionRuntime, MissionContactTriggerRuntime
│   ├── Policy/                      #   IPolicy, PatrolCandidateEngagePolicy
│   ├── Roe/                         #   ROE filter + RoePolicyAdapter
│   ├── Traits/                      #   TraitVector (Aggression, Decisiveness, etc.)
│   ├── Attention/                   #   Bandwidth model + graceful overload degradation
│   ├── Controllers/                 #   HumanController, AgentController
│   ├── Groups/                      #   DetachRejoinService, group override semantics
│   └── Hindsight/                   #   Optional sidecar for session memory
│
├── ProjectAegis.Sim                 # Simulation internals
│   ├── Engage/                      #   MvpEngagementResolver, engagement outcomes
│   ├── Policy/                      #   IPolicyEvaluator, EffectivePolicy, PolicyContext
│   ├── Scenario/                    #   ScenarioPolicyProfile, speculative settings
│   ├── Catalog/                     #   CatalogEngageEnvelope
│   └── Sensors/                     #   Sensor classify FSM
│
├── ProjectAegis.Data                # Data / catalog layer
│   ├── Catalog/                     #   CatalogPlatformEntry, sensors, mounts, weapons
│   ├── WriteGate/                   #   CatalogWriteGate — EXTEND-ONLY, SQLite-backed
│   ├── Scenario/                    #   ScenarioPackage, ScenarioPolicyJsonCatalog
│   ├── Import/                      #   JSON/markdown importers
│   └── Snapshots/                   #   DB snapshot management
│
├── ProjectAegis.MissionEditor.Cli   # CLI tools (MCP-exposed): catalog browse, import,
│                                    #   kill-chain report, mission plan suggest, write-gate
│
├── ProjectAegis.Delegation.UnityAdapter  # Bridge to Unity / DOTS
│   ├── Bridge/                      #   DelegationBridge (facade) + ISimWorldSnapshot
│   ├── Baltic/                      #   BalticBatchRunner, BalticReplayHarness
│   └── Presentation/                #   C2PresentationController (UI Toolkit)
│
├── ProjectAegis.Delegation.Demo     # Console demo / quick smoke
└── [*.Tests]                        # Co-located per-project test assemblies
```

### Data & Control Flow

```
[Unity ECS / sim systems]
   implements ISimWorldSnapshot (contacts, engagements, member alive)
        │
        ▼
DelegationBridge.Tick(snapshot, orderSink)
   ObservedStateBuilder → DelegationOrchestrator.Tick()
        │
        ▼
   DecisionPipeline.Choose(candidates, traits, attention, rng)
   (trait-weighted softmax; deterministic given same seed)
        │
        ▼
   AutonomyGate  →  RoePolicyAdapter  →  IPolicyEvaluator (EffectivePolicy)
        │
        ▼
IOrderSink.ApplyOrder(entityKey, order)  →  movement / weapons / EW systems
```

`SimulationSession` optionally wraps the orchestrator with `MvpEngagementResolver` for headless engagement resolution (used by `BalticReplayHarness` and CI replay golden tests).

---

## Hard Invariants — Never Break These

| Invariant | Rule |
|-----------|------|
| **Replay golden hash** | `17144800277401907079` must be preserved in Baltic v2 replay golden files. Grep to verify: `grep -r "17144800277401907079" tests/ data/` |
| **Test baseline** | ≥1638 solution tests, 0 failures (post S95 gauntlet land; prior ≥1599; monotonic — never regress) |
| **ReplayGolden** | 6/6 (Baltic v2 replay suite) |
| **PlayModeSmokeHarness** | ≥20/20 (C2 proxy tests) |
| **DelegationBridge.cs** | Zero-touch through Release v1 — no hotpath changes |
| **CatalogWriteGate** | Extend-only (add new `Propose*` / `Approve*` overloads; never alter existing write paths) |
| **No proprietary CMO `.db3`** | Never commit or mount Command: Modern Operations product `*.db3` files; feed catalog via markdown/fixtures/workbook/OSINT only ([dual-track doc](docs/engineering/dual-track-cmo-analysis-and-catalog.md), [ADR-013](docs/architecture/adr-013-cmo-scenario-import-policy.md)) |
| **DelegationBridge hotpath** | ZERO — grep `DelegationBridge` in `src/` hotpath methods must stay 0 new |
| **Baltic v3 isolation** | `baltic-v3-*` policies/goldens are independent; never touch v2 goldens |

Before any `gt submit`, run the full verification block above AND grep the hash AND confirm ZERO DelegationBridge hotpath changes.

---

## Catalog migration SDLC (dual-track)

Catalog population follows the **dual-track firewall**: Track A (`cmo_db_inspector`, local licensed `*.db3` analysis only) → public citations → Track B (this repo, fixtures/workbook → write gate). Never commit proprietary CMO `.db3` files. Full policy: [dual-track-cmo-analysis-and-catalog.md](docs/engineering/dual-track-cmo-analysis-and-catalog.md).

**Parent orchestration:** For multi-entity fixture waves, use the global **`dispatching-parallel-agents`** skill to fan out independent **Importer** tasks (one fixture/entity per subagent). After propose, dispatch **Curator** + **Quality** in parallel (review `*-propose.json` vs run `verify-catalog-import.ps1`). **Importer** changes use **two-stage review**: (1) fixture diff, (2) re-propose + quality gate before approve. **Write Gate** approve stays human-gated for production DBs.

| Role | Task prompt template |
|------|----------------------|
| Curator | [`.cursor/agents/catalog-curator-agent.md`](.cursor/agents/catalog-curator-agent.md) |
| Importer | [`.cursor/agents/catalog-importer-agent.md`](.cursor/agents/catalog-importer-agent.md) |
| Write Gate | [`.cursor/agents/catalog-write-gate-agent.md`](.cursor/agents/catalog-write-gate-agent.md) |
| Quality | [`.cursor/agents/catalog-quality-agent.md`](.cursor/agents/catalog-quality-agent.md) |
| Workbook | [`.cursor/agents/catalog-workbook-agent.md`](.cursor/agents/catalog-workbook-agent.md) |
| Inspector (Track A) | [`.cursor/agents/cmo-inspector-analysis-agent.md`](.cursor/agents/cmo-inspector-analysis-agent.md) → runs in `cmo_db_inspector` |

Local skills: `.cursor/skills/aegis-catalog-curator`, `aegis-markdown-fixture-author`, `aegis-write-gate-approve`, `aegis-catalog-quality-gate`. Orchestration map: [aegis-catalog-sdlc-orchestration.md](docs/engineering/aegis-catalog-sdlc-orchestration.md).

---

## C# Conventions & Code Patterns

- **Namespaces**: `ProjectAegis.<Assembly>[.<Subfolder>]` — e.g. `ProjectAegis.Delegation.Orchestration`
- **Sealed classes by default** for domain objects (see `DelegationOrchestrator`, `CatalogWriteGate`, etc.)
- **`readonly record struct`** for small value types (e.g. `EffectivePolicy`, `TraitVector`)
- **`sealed record`** for immutable DTOs (e.g. `SimulationModeProfile`)
- **Dependency injection over singletons** — all public methods are testable; no `static` state in domain logic
- **Determinism is sacred**: everything in the delegation/sim pipeline uses `SeededRng` (never `Random.Shared` or `DateTime.UtcNow` in sim paths)
- **Doc comments on all public APIs** (`/// <summary>…</summary>`)
- **Conventional Commits**: `feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:` — reference story ID in body
- **Gameplay values are data-driven**: ROE levels, trait vectors, personality presets, and engagement parameters live in `data/scenarios/*.policy.json`, not in C# constants
- **`Directory.Build.props`** sets `ProduceReferenceAssembly=false` globally (prevents CS0006 on parallel builds — do not override per-project)

Naming specifics:
- Test classes: `[SystemUnderTest]Tests.cs`, test methods: `[Scenario]_[Expected]` (snake\_case within xUnit `[Fact]`)
- Scenario policies: kebab-case JSON, e.g. `baltic-patrol-combat-domains.policy.json`
- Catalog entries: `CatalogPlatformEntry`, `CatalogSensorBinding`, `CatalogMount` (always `Catalog` prefix)

---

## Test Layout

**Hybrid layout — do not migrate to flat.** This is an explicit architectural decision (S39-04 signed):

- `src/ProjectAegis.*.Tests/` — co-located xUnit test projects per source assembly (6 projects)
- `tests/regression/` — golden fixture files for replay/determinism tests

Test assembly breakdown at baseline:
- `ProjectAegis.Sim.Tests`: ~279 tests
- `ProjectAegis.MissionEditor.Cli.Tests`: ~43 tests
- `ProjectAegis.Delegation.Tests`: ~247 tests
- `ProjectAegis.Data.Excel.Tests`: ~5 tests
- `ProjectAegis.Delegation.UnityAdapter.Tests`: ~252 tests (includes 18 PlayModeSmoke + 6 ReplayGolden)
- `ProjectAegis.Data.Tests`: ~406 tests

---

## Scenario / Policy System

- **`data/scenarios/`**: JSON policy files (`*.policy.json`). Baltic Patrol has ~20+ policies covering patrol, BDA lifecycle, comms, EMCON, damage, ROE, near-future archetypes, etc.
- **`ScenarioPolicyRepository`**: loads JSON from `data/scenarios/` at startup; `DelegationBridge` passes `scenarioPolicyId` to look up a `ScenarioPolicyProfile`
- **`EffectivePolicy`**: `(RoeLevel, MaxSalvo)` — resolved per unit at evaluation time via `IPolicyEvaluator`
- **Baltic v3 policies** use `mission.triggers` + `MissionContactTriggerRuntime` for contact-triggered ROE escalation (ASuW/AAA → WeaponsFree on recon detection)
- **Autonomy levels**: `Manual(1)` → `Assisted(2)` → `SemiAutonomous(3)` → `FullAutonomous(4)`

---

## Workflow & Collaboration Rules

**Collaboration protocol (from `CLAUDE.md`):** User-driven — ask before writing files, show drafts first, get approval for multi-file changesets. No commits without user instruction.

**Parallel-agent / worktree pattern:**
- Sprint worktrees live under `/home/username01/cmano-clone/.worktrees/` (gitignored)
- One agent per track, isolated context; dispatch via `dispatching-parallel-agents` skill
- Serial prereqs (baseline + QA plan) always before parallel waves

**Files to NEVER commit:**
- `.cursor/hooks/` — local hook config
- `.pi/settings.json` — local agent config
- `.polly/` — local agent/tooling config

**Roadmap / sprint references:**
- Active sprint plans: `production/sprints/sprint-*.md`
- QA plans: `production/qa/qa-plan-sprint-*.md`
- Smoke closeouts: `production/qa/smoke-sprint-*-closeout-*.md`
- Forward roadmap: `docs/reports/future-sprint-roadpmap.md` (stable alias → latest dated file)
- Scope boundaries: `production/release-train-scope-boundary-2026-06-24.md`, `production/baltic-v3-scope-boundary-2026-06-25.md`

---

<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **cmano-clone**. Use the GitNexus MCP tools to understand code, assess impact, and navigate safely. See full GitNexus / Hindsight / Buildkite / Superpowers / Graphite / Cloud sections in repo history if truncated here.

**Always:** impact analysis before symbol edits; detect_changes before commit.

<!-- gitnexus:end -->

## Unity / C# architecture (UCA-A pointer)

Full skill: `production/agentic/skills/unity-csharp-architect/`. Adoption train: `production/agentic/uca-a-adoption-train-2026-08-12.md`. Presentation ADRs: **010 / 007 / 001** (not Git ADR-018).
