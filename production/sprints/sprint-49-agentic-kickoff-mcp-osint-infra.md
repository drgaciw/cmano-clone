# Sprint 49 — Agentic Kickoff: MCP/OSINT Production + Infra Foundations (E2 Lead)

**Dates:** ~TBD (~10–12 days)  
**Trunk:** `main` @ post-S48 Release  
**Predecessor:** Sprint 48 — COMPLETE (RC1 cut; stage Release)  
**Stage:** Post-Release internal engineering (S49+ program)  
**Authority:** [`production/post-release-scope-boundary-2026-06-21.md`](../post-release-scope-boundary-2026-06-21.md); [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §9

> **READY TO DISPATCH** — post-release scope boundary published 2026-06-21. E2 lead epic; Req **05** + **07** (foundations). Commercial launch (E7) and playtest sweep (E1) are **not** S49 scope.

## Sprint Goal

Production-harden the Dynamic Systems Agent MCP/OSINT path (Req 05) and lay Agentic Infrastructure foundations (Req 07 INF-1.x + batch experiment schema). Establish post-Release QA baseline and gate matrix. Maintain all v1.0 determinism invariants.

**S49 does not close Req 05 or Req 07 MVP-done** — remaining ACs land in S50+ per boundary §In scope.

## Capacity

- Total days: 12
- Buffer (20%): 2 days
- **Effective dev-days:** **10**
- **Commit target:** **8–9 stories**
- **Test baseline:** ≥ **1227** (S48 floor); monotonic growth expected
- **Parallelism:** **5 tracks** (MCP, OSINT data, agentic infra, baseline/QA, closeout)

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S49-01 | **Re-baseline + post-release gate matrix** — build/test; cite boundary | c-sharp-devops-engineer | 1 | Boundary published | 0 errors; `gate-matrix-post-release-2026-06-21.md` |
| S49-02 | **Sprint 49 QA plan** — Req 05/07 scope; blocks waves | team-qa | 1 | S49-01 | `production/qa/qa-plan-sprint-49-2026-06-21.md` |
| S49-03 | **Req 05 — MCP production** — complete CLI/MCP tool suite per req 05 | c-sharp-engineer | 2.5 | S49-02 | See §S49-03 below |
| S49-04 | **Req 05 — OSINT production** — connectors + staging production path | team-data | 2.5 | S49-02 | See §S49-04 below |
| S49-05 | **Req 07 — Infra foundations** — scenario validate/export + batch schema | c-sharp-engineer + team-simulation | 2.5 | S49-02 | See §S49-05 below |
| S49-06 | **Closeout** — smoke, evidence, sprint-status | c-sharp-devops-engineer | 0.5 | S49-03+ | `smoke-sprint-49-closeout-2026-06-21.md`; gates PASS |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S49-07 | **Daily digest batch stub** — deterministic UTC-day digest entry (Req 05 DSA-1.1) | team-data | 1 | S49-04 | CLI verb + run summary schema; fixture tests; no live network in CI |
| S49-08 | **Tracker row updates** — Req 05/07 progress notes in implementation tracker | writer / coordinator | 0.5 | S49-06 | Tracker cites S49 evidence paths |

---

## S49-03 — Req 05 MCP production

**Authority:** `Game-Requirements/requirements/05-Dynamic-Systems-Agent.md` (Unity-MCP tools); S21 precedent [`sprint-21-mcp-osint-cesium-data-polish.md`](sprint-21-mcp-osint-cesium-data-polish.md)

**Scope (production-harden, not greenfield):**

| Deliverable | Detail |
|-------------|--------|
| MCP manifest | `tools/mission-editor/mcp-tools.json` — all req 05 tools with JSON schemas; `McpToolsManifestTests` green |
| CLI verbs | `Program.cs` — `osint_search`, `osint_digest`, `osint_list_staging_proposals`, `osint_get_proposal_detail`, `osint_submit_review_decision` (extend existing); stable JSON via `McpToolResult` |
| E2E tests | `ProjectAegis.MissionEditor.Cli.Tests` — filter `Mcp\|Osint`; fixture-only paths |
| Determinism | No wall-clock in hashed outputs; connector sort stable (`SourceUrl` + `CanonicalId`) |

**GitNexus (pre-edit):** `impact()` on `Program`, `OsintDigestRunner`, `OsintStagingReviewCommand`, MCP command types. **CatalogWriteGate:** extend-only. **DelegationBridge:** ZERO touch.

**Out of S49-03:** Unity-MCP server wiring in Editor (document runbook gap if needed); live HTTP connectors in CI.

---

## S49-04 — Req 05 OSINT production

**Scope:**

| Deliverable | Detail |
|-------------|--------|
| Connector registry | Central registration for `IOsintConnector` impls (InMemory, File, Rss); deterministic fetch order |
| Staging production path | `OsintStagingReviewCommand` + gate path hardened; panel/CLI parity tests |
| TL routing | `OsintCatalogMapper` consumes `proposedTL`/`targetDoc`; staged bindings tagged for doc 09/10 gates |
| Mapper tests | Extend `Osint*Tests`; no `IWriteGate` bypass |

**Primary symbols:** `src/ProjectAegis.Data/Osint/*`, `OsintCatalogMapper`, `CatalogWriteGate` (extend-only hooks only).

**Unity (advisory):** `OsintStagingPanelHost` — headless parity via CLI proxy; live Editor PNG optional.

---

## S49-05 — Req 07 infra foundations

**Authority:** `Game-Requirements/requirements/07-Agentic-Infrastructure.md` INF-1.1–1.4, INF-3.1–3.2

**Scope (foundations — full workers in S50):**

| Deliverable | Detail |
|-------------|--------|
| Scenario metadata | Typed fields for theater / side count / TL flags on scenario package DTOs (INF-1.3) |
| Validate/export gates | `scenario_validate` deterministic `reportHash`; `canExport` before brief export (INF-1.2, INF-1.4) |
| Batch experiment schema | Document + stub types for seed grid + CSV columns (`scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint`) per INF-3.1 |
| Harness hook | `BalticBatchRunner` or Mission Editor CLI accepts schema params without breaking existing replays |

**Out of S49-05:** NL scenario generation (S50); Monte Carlo UI; operator copilot in C2.

**GitNexus:** impact on `ScenarioPackage`, Mission Editor CLI commands, `BalticBatchRunner` if touched. **Replay 6/6** if sim bind changes.

---

## Explicitly Out of Scope (S49)

- E7 commercial launch (store/i18n production)
- E1 playtest AAR remediation (S56)
- S51+ data corpora CI, S52+ perf/DOTS, S53–S54 speculative, S55 Cesium production
- Full Req 05/07 MVP-done closeout (partial progress only)
- DelegationBridge edits without ADR
- Production Baltic hash change
- Live social stream OSINT (`enableRealtimeSocialStream` stays `false`)

## GitNexus / Hard Gates

- ReplayGolden **6/6**; C2 proxy **18/18+**
- Baltic hash **`17144800277401907079`** immutable
- CatalogWriteGate **extend-only**
- `impact()` before every symbol edit; `detect_changes()` before commit
- Every artifact cites **`post-release-scope-boundary-2026-06-21.md`**

## Definition of Done

- [ ] Must Have S49-01..06 complete
- [ ] QA plan MET; smoke PASS
- [ ] Req 05 MCP + OSINT production deliverables landed with tests
- [ ] Req 07 foundation deliverables landed with tests
- [ ] Gate matrix + boundary cited in all artifacts
- [ ] sprint-status.yaml updated (S49 complete; S50 ready)

## Parallel Execution Model

| Track | Stack prefix | Agent env | Stories |
|-------|--------------|-----------|---------|
| MCP production | `stack/sprint49/mcp-production` | Cloud | S49-03 |
| OSINT production | `stack/sprint49/osint-production` | **Local lead** (Catalog/Osint cluster) | S49-04, S49-07 |
| Agentic infra | `stack/sprint49/agentic-infra` | Cloud | S49-05 |
| Baseline + QA | `stack/sprint49/baseline-qa` | Cloud | S49-01, S49-02 |
| Closeout | `stack/sprint49/closeout` | **Local** | S49-06, S49-08 |

See [`sprint-49-parallel-kickoff-2026-06-21.md`](../agentic/sprint-49-parallel-kickoff-2026-06-21.md).

## Quality Gates

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "FullyQualifiedName~Mcp|Osint"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "FullyQualifiedName~Osint|Catalog|WriteGate"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~ReplayGoldenSuiteTests|PlayModeSmokeHarnessTests"
```

## Related Artifacts

| Artifact | Path |
|----------|------|
| Scope boundary | `production/post-release-scope-boundary-2026-06-21.md` |
| Roadmap | `docs/reports/future-sprint-roadpmap.md` §9 |
| Kickoff | `production/agentic/sprint-49-parallel-kickoff-2026-06-21.md` |
| Req 05 | `Game-Requirements/requirements/05-Dynamic-Systems-Agent.md` |
| Req 07 | `Game-Requirements/requirements/07-Agentic-Infrastructure.md` |
| S21 precedent | `production/sprints/sprint-21-mcp-osint-cesium-data-polish.md` |
| S48 baseline | `production/gate-checks/s48-release-gate-2026-06-20.md` |

---

*Planning artifact 2026-06-21. Run `/qa-plan sprint` then `/dev-story dispatch S49-01` to begin execution.*
