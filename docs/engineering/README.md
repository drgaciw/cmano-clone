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
| [abort-reason-catalog.md](abort-reason-catalog.md) | The stable machine-readable abort codes (`ENGAGE_ABORT` / `POLICY_DENIAL`): the manifest → codegen workflow, the two-layer enum split, and how to add a code without breaking replay. |
| [mission-editor-cli.md](mission-editor-cli.md) | Operational reference for the headless Mission Editor CLI and its MCP verbs — author, validate, simulate, publish scenarios and browse/extend the catalog without Unity. |
| [scenario-policy-authoring.md](scenario-policy-authoring.md) | How to author `data/scenarios/*.policy.json` sim policy files: the loader/override model, the full top-level + `engage` field reference, enum values (and which throw), contact-triggered ROE escalation, validation errors, and replay constraints. |

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
