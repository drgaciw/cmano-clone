# Engineering docs index

> **Navigation hub for `docs/engineering/`.** Reference + runbook pages for the
> headless .NET subsystems, the CI/build pipeline, the Graphite PR workflow, the
> Cesium map spike, and the agentic-dev methodology. Architecture rationale lives
> in [`docs/architecture/`](../architecture/) (ADRs); scope/acceptance lives in
> [`Game-Requirements/`](../../Game-Requirements/); sprint plans live in
> [`docs/superpowers/`](../superpowers/). This index only points at those pages —
> it does not restate them.

Most subsystem pages follow the same shape — a `> **Engineering reference +
runbook.**` banner, an **Intent** table, an architecture sketch, a key-types /
source map, a copy-paste runbook, and **Common pitfalls** — so they read the same
way whether you arrive from code, an ADR, or a requirement.

## Subsystem reference + runbooks

The Mission Editor and catalog-data subsystems. The three catalog-write paths
(Excel, CMO markdown, OSINT) all stage through the same
[`CatalogWriteGate`](../../src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs) —
no path commits without an explicit approve step (ADR-006 / Req 06).

| Doc | Covers | Primary codepaths |
|-----|--------|-------------------|
| [mission-editor-cli-reference.md](mission-editor-cli-reference.md) | Every CLI verb and its Unity-MCP tool mapping; the deterministic one-process/one-verb/one-JSON contract, exit codes, and optimistic concurrency | `src/ProjectAegis.MissionEditor.Cli/` · [`tools/mission-editor/mcp-tools.json`](../../tools/mission-editor/mcp-tools.json) |
| [platform-editor-excel-roundtrip.md](platform-editor-excel-roundtrip.md) | Export → edit → diff → staged-import round-trip for platform/sensor data via Excel (ADR-011) | `src/ProjectAegis.Data.Excel/` · `src/ProjectAegis.Data/Platform/` |
| [cmo-markdown-catalog-import.md](cmo-markdown-catalog-import.md) | Chunked CMO markdown → proposed catalog records staged through the write gate | `src/ProjectAegis.Data/Import/` |
| [osint-catalog-staging.md](osint-catalog-staging.md) | OSINT discovery → relevance-scored proposals → staging review/approve | `src/ProjectAegis.Data/Osint/` |
| [balance-telemetry-drift-detection.md](balance-telemetry-drift-detection.md) | Advisory win-rate drift sink (off by default, never auto-balances); deterministic state hash for golden tests (DBI-5) | `src/ProjectAegis.Data/Telemetry/` |
| [doctrine-inheritance-panel.md](doctrine-inheritance-panel.md) | Side → mission → unit ROE inheritance resolution and the headless override command (Req 13) | `src/ProjectAegis.Sim/Policy/` · `src/ProjectAegis.Delegation/Projection/` |

## CI, build & branch protection

| Doc | Covers |
|-----|--------|
| [buildkite-ci.md](buildkite-ci.md) | Buildkite primary pipeline setup; replaces the legacy GitHub Actions .NET/Graphite/Post-Merge jobs and Gitleaks |
| [ci-and-branch-protection.md](ci-and-branch-protection.md) | Required checks, the Buildkite blocking gate, post-merge replay golden on `main`, and the CodeQL/GitNexus/Unity Actions |

## Graphite PR workflow

| Doc | Covers |
|-----|--------|
| [graphite-github-substitute-plan.md](graphite-github-substitute-plan.md) | **Canonical** — `gt`-first stacked-PR workflow (trunk `main`, `gt submit`, restack/sync) |
| [graphite-stack-backlog-2026-06.md](graphite-stack-backlog-2026-06.md) | Backlog appendix to the canonical plan |
| [graphite-stack-delegation-2026-05-30.md](graphite-stack-delegation-2026-05-30.md) | Historical (stack complete on `main`); kept for provenance — do not re-submit |

## Cesium map spike (ADR-007)

| Doc | Covers |
|-----|--------|
| [cesium-phase-b-spike-checklist.md](cesium-phase-b-spike-checklist.md) | De-risking the globe-map C2 tactical picture before production wiring |
| [cesium-unity-package-pin.md](cesium-unity-package-pin.md) | Pinned Cesium-for-Unity package version for `unity/ProjectAegis` |

## Agentic development methodology

| Doc | Covers |
|-----|--------|
| [superpowers-setup.md](superpowers-setup.md) | Installing/refreshing the global [obra/superpowers](https://github.com/obra/superpowers) skills (TDD, debugging, plans) |
| [hindsight-agentic-dev.md](hindsight-agentic-dev.md) | Pairing Hindsight session memory with GitNexus code intelligence |
| [pi-skills-recommendations-review.md](pi-skills-recommendations-review.md) | Milsim-lens review of the PI skills recommendations |

## Adding a new subsystem doc

1. Verify behavior against source — do not document intended-but-unbuilt behavior.
2. Match the existing shape: reference + runbook banner, Intent table, architecture
   sketch, key-types/source map, runbook, Common pitfalls.
3. Link the governing ADR (`docs/architecture/`) and requirement
   (`Game-Requirements/`) rather than restating them.
4. Add a row to the relevant table above so the page is discoverable.
