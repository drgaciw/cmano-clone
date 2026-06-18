# Engineering Docs Index

> Developer-facing runbooks and operational guides for Project Aegis.
> Design rationale lives in [`docs/architecture/`](../architecture) (ADRs); product
> requirements live in [`Game-Requirements/`](../../Game-Requirements). This folder is the
> **how-to / how-it-works** layer that sits between them.

## Data layer (`ProjectAegis.Data`)

The catalog data flow, end to end: parse external sources → stage behind the write gate →
human-approve → commit, plus the advisory telemetry that observes (but never writes) the
catalog. All four are anchored on [ADR-006 — Data Layer Boundary](../architecture/adr-006-data-layer-boundary.md).

| Doc | Covers | Read when |
|-----|--------|-----------|
| [Catalog Ingestion Pipeline](catalog-ingestion-pipeline.md) | CMO-markdown + OSINT parsing into `Catalog*` records; inference rules; import-gate quarantine | Adding/parsing a new source format or debugging a dropped record |
| [Catalog Write-Gate & Determinism](catalog-write-gate-determinism.md) | propose → approve → commit; per-entity sort keys; snapshots; golden hashes | Adding a `Catalog*` type or chasing a golden-hash/determinism diff |
| [OSINT Discovery Pipeline](osint-discovery-pipeline.md) | connectors (`IOsintConnector`), digest runner, proposal gate, JSON shapes, staging review | Wiring an OSINT connector or staging digest proposals |
| [Balance Drift Telemetry (Advisory)](balance-drift-telemetry.md) | win-rate accumulation, ±8% drift flags, feature flag, determinism — never a write path | Observing agent-vs-agent balance without mutating the catalog |

Tooling: the headless CLI/MCP verbs that drive these paths are documented in
[`tools/mission-editor/README.md`](../../tools/mission-editor/README.md).

## CI & branch protection

| Doc | Covers |
|-----|--------|
| [Buildkite CI](buildkite-ci.md) | Canonical CI pipeline (replaced the GitHub Actions workflows) |
| [CI and branch protection](ci-and-branch-protection.md) | Required checks, branch-protection setup, free-plan caveats |

## Graphite PR workflow

| Doc | Covers |
|-----|--------|
| [Graphite-as-GitHub-Substitute](graphite-github-substitute-plan.md) | **Canonical** stack workflow (`gt create`/`gt submit`); supersedes older Graphite runbooks |
| [Graphite PR Backlog (2026-06)](graphite-stack-backlog-2026-06.md) | Backlog appendix of planned/landed stacks |
| [Stack Plan: Delegation → Sim wiring](graphite-stack-delegation-2026-05-30.md) | *Historical* — delegation stack, complete on `main` |

## Agentic development methodology

| Doc | Covers |
|-----|--------|
| [Superpowers setup](superpowers-setup.md) | Global obra/superpowers skills framework + repo skill priority |
| [Hindsight for agentic development](hindsight-agentic-dev.md) | Local session memory paired with GitNexus code intelligence |
| [Pi skills recommendations review](pi-skills-recommendations-review.md) | Milsim-lens review of the agentic skills recommendations |

## Cesium / C2 map (ADR-007, Phase B)

| Doc | Covers |
|-----|--------|
| [Cesium Phase B spike checklist](cesium-phase-b-spike-checklist.md) | De-risk gate before wiring the globe map into the tactical picture |
| [Cesium for Unity package pin](cesium-unity-package-pin.md) | Pinned Cesium package version for `unity/ProjectAegis` |

## Conventions

- Each runbook opens with a `> Scope / ADR / Requirements / Last updated` header so you can
  judge relevance and freshness at a glance.
- Runbooks link directly to the source symbols they describe — **verify behaviour against the
  cited source before relying on a value**; docs follow code, not the reverse.
- Build/test commands assume the repo root and the headless **.NET 8** path (see
  [`AGENTS.md`](../../AGENTS.md) "Cursor Cloud specific instructions").
