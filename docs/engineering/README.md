# Engineering docs — index

Developer-facing reference for building, testing, and operating Project Aegis. These docs
cover the *engineering machinery* (determinism, CI, tooling, local setup); the *what* and
*why* of the game live under [`Game-Requirements/`](../../Game-Requirements/) and the design
decisions under [`docs/architecture/`](../architecture/) (ADRs).

New to the repo? Start with the root [`README.md`](../../README.md) and
[`AGENTS.md`](../../AGENTS.md), then [local-dev-environment.md](local-dev-environment.md).

---

## Subsystem developer guides

Deep-dives into engine-agnostic subsystems, verified against source and pinned by tests.

| Doc | Covers |
|-----|--------|
| [determinism-and-replay.md](determinism-and-replay.md) | How the sim stays bit-for-bit reproducible per `(scenario, seed)`: determinism rules, world-state / order-log hashing, the golden-fixture workflow, and common pitfalls. |
| [agent-decision-pipeline.md](agent-decision-pipeline.md) | The delegation decision tick — how `DelegationOrchestrator.Tick(ObservedState)` turns the observed world into gated, logged orders: the per-agent `AgentController.TryDecide` path (reaction-delay throttle, `AttentionCalculator` load/degradation, SA fog-of-war `PerceivedState`, `IPolicy` candidate generation), the `DecisionPipeline.Choose` trait-weighted softmax + single seeded RNG draw (non-positive filter, `Decisiveness` temperature, narrowed focus), the ROE-first `AutonomyGate` (Manual/Assisted/Semi/Full + `PolicyDenialRecord`), what lands in the order log, the determinism invariants, and how to extend it without breaking replay goldens. |
| [baltic-replay-harness.md](baltic-replay-harness.md) | The single headless runner behind replay golden, the QA Gauntlet, the CLI, and 60-plus tests: the `BalticReplayHarness.Run(seed, scenarioPolicyId, ticks, …)` → `Result` API, the composition pipeline (profile/catalog/detection resolution, legacy-vs-`gauntlet.units` ORBAT build, per-domain engage-agent assignment, the deterministic tick loop, and result fold), `ResolveFireOrder` / `DiagnoseDivergence`, the full consumer map, and how to add a scenario or feature slice without breaking goldens. |
| [detection-pipeline.md](detection-pipeline.md) | The tick-4 detection/contact slice that produces the tactical picture (and the fire-control track the engagement layer needs): the pure sorted `DeterministicDetectionLoop.RollTick` (EMCON/jam gates, the `Pd` formula, the `RngDomain.Detection` draw), the `PdDetectionContactSimulator` contact FSM (`Unknown → Detected → Classified → Identified → Lost`, comms-degraded staleness, BDA/kill removal, primary-target + `HostileContactFilter` selection), the scheduled `ScenarioContactSimulator` fallback, the bounded `DatalinkSidePictureMerger` (per-side sharing, share lag, comms gating), the deterministic detection sub-hash, the tick-4 harness integration, and how to extend it without breaking replay goldens. |
| [engagement-pipeline.md](engagement-pipeline.md) | The tick-8 engage/kill-chain resolver (`MvpEngagementResolver`): the `IEngagementResolver.Resolve(EngageRequest)` seam, the `EngageContext` input surface, the **exact ordered gate chain** (speculative TL → policy/ROE → domain validators → readiness → EMCON → track → magazine → envelope → DLZ → consume → launch) and its abort reasons, the two-layer ROE/domain split, the DLZ personality table, the three-draw `Combat`-RNG outcome fold (Hit/Intercept/Kill) + kill registry / world-hash contribution, how `SimulationSession` drives it (per-shooter victim resolution + multi-domain concurrent engage via `PreferredHostileByShooter`, the scenario-policy magazine cap, swarm deconfliction, comms gate, kill marking), and how to add a gate/validator/outcome without breaking combat goldens. |
| [abort-reason-catalog.md](abort-reason-catalog.md) | The stable machine-readable abort codes (`ENGAGE_ABORT` / `POLICY_DENIAL`): the manifest → codegen workflow, the two-layer enum split, and how to add a code without breaking replay. |
| [mission-timeline-runtime.md](mission-timeline-runtime.md) | The runtime behind the scenario `mission` block: the deterministic `MissionRuntime` mission clock (locked-order timeline events → `MissionTransition` / `EventFired` order-log entries, `fire_order` resolution) and the `MissionContactTriggerRuntime` contact-triggered ROE escalation (Baltic v3 weapons-free-on-first-recon: the `Unknown → Detected` fire-once edge, id-prefix target classification, `ApplyRoeToUnits` change-only `PolicyUpdate`), the tick ordering that makes escalation take effect the same tick, the determinism guarantees, and how to extend it without breaking replay goldens. |
| [mission-editor-cli.md](mission-editor-cli.md) | Operational reference for the headless Mission Editor CLI and its MCP verbs — author, validate, simulate, publish scenarios and browse/extend the catalog without Unity. |
| [scenario-policy-authoring.md](scenario-policy-authoring.md) | How to author `data/scenarios/*.policy.json` sim policy files: the loader/override model, the full top-level + `engage` field reference, enum values (and which throw), contact-triggered ROE escalation, validation errors, and replay constraints. |
| [scenario-document-authoring.md](scenario-document-authoring.md) | How to author `*.scenario.json` authoring documents (`ScenarioDocumentDto`, ADR-008): the tolerant loader / canonical writer, the edit-version / undo / export-gate lifecycle, the full metadata + section + mission field reference, per-mission-type rules, and the validation-finding catalog. |
| [scenario-authoring-host.md](scenario-authoring-host.md) | The in-process authoring **host** object model behind interactive scenario editing: `ScenarioAuthoringSession` (file-backed lifecycle), the `ScenarioEditCommandBus` mutate pipeline (optimistic concurrency → undo capture → commit → save → live validate, `ScenarioMutationResult` + `CONFLICT` / `INVALID_OPERATION` codes, full verb table), and the engine-agnostic presenter view models (Mission Board, Map surface, selection inspector, Play↔Edit FSM, live findings). |
| [c2-projection-layer.md](c2-projection-layer.md) | The C2 read-model layer (`ProjectAegis.Delegation/Projection/`, ~75 types): the pure order-log → view-model projections behind the tactical picture, the `Projection → Binder → State` layering, the read-only/no-mutation contract that protects replay determinism, the projection catalog (message log, contact/facility picture, OOB tree, sensor C2, map, losses/scoring CSV, catalog surfacing), APP-6/2525C symbology (`App6Sidc` + atlas-optional glyph resolution), the C2 rev-2 alert/lifecycle contracts, host-side selection state, and how to add a panel. |
| [scenario-event-system.md](scenario-event-system.md) | The scenario event graph at authoring time: the event model, the `EventStaticAnalyzer` warning codes (dead triggers, unreachable actions, contradictions, cycles) wired into `scenario_validate`, the `scenario_event_trace` AC-7 debugger projection, deterministic fire order, the headless event-graph view model, and the `event_add` / `event_update` / `event_delete` CLI verbs. |
| [catalog-write-gate.md](catalog-write-gate.md) | The `CatalogWriteGate` propose → approve → commit workflow (ADR-006, req-06): the extend-only propose batch kinds, per-kind approve validation, the machine-readable error-code catalog, the CLI verbs, determinism constraints, and the runbook for adding a new catalog row kind. |
| [catalog-seeding.md](catalog-seeding.md) | How headless runs/tests get a catalog: `CatalogReaderFactory` reader resolution, `CatalogSeedBootstrap` (the deterministic Baltic patrol/v3 fixture + the `u1` engage chain), the two seed sources (`sensors_baltic.json` vs in-memory fixture), migrations on open, and how the committed `baltic_patrol.db` fits in. |
| [catalog-release-train.md](catalog-release-train.md) | The release-train layer above the write gate: the `catalog_snapshot` / `db_release` data model, immutable snapshots + content hashing, unified curator-drop manifests over the six nightly domains, the deterministic read-only `catalog_release_diff` (Added/Changed/Removed semantics), TL-tier branch resolution, and the extend-only constraints. |
| [cmo-markdown-import.md](cmo-markdown-import.md) | The CMO markdown import pipeline (`CmoMarkdownImporter` → `catalog_import_markdown` → nightly propose/approve): the seven entity categories, the markdown format and extraction rules, domain inference, the silent-loss/field-gap fixes (slug collisions, multi-clause ranges, nationality/domain fallbacks), and the off-CI nightly runbook. |
| [qa-gauntlet.md](qa-gauntlet.md) | The QA Gauntlet — the escalating-complexity headless QA loop and its **fail-closed oracle**: the batch → CSV → `gauntlet_oracle_eval` pipeline, the `gauntlet.expect` schema (numeric bounds + fingerprint / multi-domain launch gates), the 5-tier ladder and tick budgets, run-artifact layout, the defect registry, expect-regen discipline, and the CI fail-closed strip gate. |

## Local environment & agentic tooling

| Doc | Covers |
|-----|--------|
| [local-dev-environment.md](local-dev-environment.md) | Local editor setup and troubleshooting for this workspace's shape (large .NET 8 solution + Unity), including the VS Code Linux file-watcher `ENOSPC` fix. |
| [hindsight-agentic-dev.md](hindsight-agentic-dev.md) | Using Hindsight session memory alongside GitNexus code intelligence in the agentic dev loop. |
| [superpowers-setup.md](superpowers-setup.md) | Installing/refreshing the global [obra/superpowers](https://github.com/obra/superpowers) agent methodology (TDD, debugging, plan-driven execution). |
| [pi-skills-recommendations-review.md](pi-skills-recommendations-review.md) | Dated review (2026-06-03) of the `pi-skills-recommendations` doc through a milsim lens. |

## CI, branch protection & Graphite workflow

| Doc | Covers |
|-----|--------|
| [buildkite-ci.md](buildkite-ci.md) | The primary Buildkite pipeline — the blocking `.NET` gate that replaced the GitHub Actions build/test/secret-scan jobs. |
| [ci-and-branch-protection.md](ci-and-branch-protection.md) | The full CI surface and branch-protection model: Buildkite gate, Graphite optimizer, post-merge replay golden on `main`, and the GitHub Actions that remain (CodeQL / GitNexus / Unity). |
| [buildkite-agent-skills.md](buildkite-agent-skills.md) | The vendored Buildkite agent skills that let Claude Code / Cursor generate pipeline YAML and drive `bk` / the Buildkite API. |
| [graphite-github-substitute-plan.md](graphite-github-substitute-plan.md) | **Canonical** Graphite-first workflow — use `gt create` / `gt submit`, not `gh pr create`, for stack work. |
| [graphite-stack-backlog-2026-06.md](graphite-stack-backlog-2026-06.md) | Backlog appendix listing the June 2026 Graphite stacks (companion to the plan above). |
| [graphite-stack-delegation-2026-05-30.md](graphite-stack-delegation-2026-05-30.md) | Historical stack plan for the Delegation → Sim wiring — **COMPLETE on `main`, do not re-submit**. |

## Map presentation (Cesium, ADR-007)

| Doc | Covers |
|-----|--------|
| [cesium-phase-b-spike-checklist.md](cesium-phase-b-spike-checklist.md) | Gate checklist to de-risk the Cesium globe map before production wiring of the C2 tactical picture. |
| [cesium-unity-package-pin.md](cesium-unity-package-pin.md) | The pinned Cesium-for-Unity package version and install notes for the ADR-007 Phase B spike. |

## Operations

| Doc | Covers |
|-----|--------|
| [gitnexus-index-health.md](gitnexus-index-health.md) | Keeping the `cmano-clone` GitNexus code index current after merges — stale-index symptoms and re-index steps. |

---

## Related references

| Where | What |
|-------|------|
| [`docs/architecture/`](../architecture/) | ADRs (tick pipeline, policy evaluator, order-log schema, combat-domain validators, C2 map, …). |
| Per-project READMEs | [Delegation](../../src/ProjectAegis.Delegation/README.md) · [Sim](../../src/ProjectAegis.Sim/README.md) · [Data](../../src/ProjectAegis.Data/README.md) · [Data.Excel](../../src/ProjectAegis.Data.Excel/README.md) · [Unity adapter](../../src/ProjectAegis.Delegation.UnityAdapter/README.md) · [Mission Editor CLI](../../src/ProjectAegis.MissionEditor.Cli/README.md) · [Demo](../../src/ProjectAegis.Delegation.Demo/README.md) |
| [`Game-Requirements/`](../../Game-Requirements/) | Requirements, terms glossary (req 12), and the requirements master index. |
| [`AGENTS.md`](../../AGENTS.md) | Build/test commands, hard invariants, and Cursor Cloud agent setup. |
