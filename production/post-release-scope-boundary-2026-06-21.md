# Post-Release Scope Boundary — Internal Engineering Program (S49+)

**Date:** 2026-06-21  
**Status:** **PUBLISHED** (user prioritization locked 2026-06-21)  
**Authority:** [`docs/reports/future-sprint-roadpmap.md`](../docs/reports/future-sprint-roadpmap.md) §3–§5, §9; S48 Release gate PASS + v1.0 milestone **CLOSED**  
**Supersedes for S49+:** v1.0 defer list in [`release-enablement-scope-boundary-2026-06-20.md`](release-enablement-scope-boundary-2026-06-20.md) §Explicitly out of scope  
**Archived (v1.0 only):** [`release-enablement-scope-boundary-2026-06-20.md`](release-enablement-scope-boundary-2026-06-20.md), [`polish-scope-boundary-2026-06-19.md`](polish-scope-boundary-2026-06-19.md)  
**Program target:** **Internal engineering milestone** at S56 — all **21** implementation-tracker rows MVP-done (or Partial+ with documented Baltic AC tests); **not** commercial launch

## Purpose

S49–S56 advances every tracker row deferred at v1.0 closeout, with **E2 Agentic platform** as the lead epic (S49 dispatch). RC1 remains an internal engineering cut — store/i18n production (E7) is out of this program unless a new scope decision is recorded.

Any S49+ story without a traceable link to this document and a committed row ID (or epic ID for cross-cutting work) is out of scope.

**Dispatch gate:** This boundary is **published**. S49 agent dispatch may proceed per [`sprint-49-agentic-kickoff-mcp-osint-infra.md`](sprints/sprint-49-agentic-kickoff-mcp-osint-infra.md) + [`sprint-49-parallel-kickoff-2026-06-21.md`](agentic/sprint-49-parallel-kickoff-2026-06-21.md).

---

## Program map (S49–S56)

| Sprint | Epic | Req rows | Primary goal |
|--------|------|----------|--------------|
| **S49** | **E2** ★ | **05**, **07** (start) | MCP/OSINT production + agentic infra foundations |
| **S50** | **E2** | **07**, **11** | Scenario gen workers + Mission Editor NL planner |
| **S51** | E5 | **06** (subset) | Full corpora CI + TL runtime fork selection |
| **S52** | E6 | **01**, **08** | Multi-k entity gate + sim API / DOTS expand |
| **S53** | E3 | **09** | Near-future full DOTS spawn + MASS tier |
| **S54** | E3 | **10** | Speculative systems (orbital DEW, escalation) |
| **S55** | E4 | **20** | Cesium/globe production + hypersonic C2 UI |
| **S56** | E1 + gate | (cross-cutting) | Playtest AAR sweep + internal milestone gate |

**Program exit (S56):** 21/21 tracker rows meet MVP-done criteria + internal gate PASS; stage remains **Release** (no commercial launch requirement).

---

## In scope — committed tracker rows (all v1.0 deferrals)

### S49 — E2 lead (Req 05 + Req 07 start)

| Req | Title | S49 committed scope |
|-----|-------|---------------------|
| **05** | Dynamic Speculative Systems Agent | **MCP production path:** complete Unity-MCP/CLI tool suite per req 05 (`search_osint`, `list_staging_proposals`, `get_proposal_detail`, `submit_review_decision`, digest batch entry point); deterministic fixture tests; `mcp-tools.json` schemas + manifest tests green. **OSINT production:** connector registry pattern; staging review production path (CLI + panel parity); TL routing on staged bindings; extend-only through `CatalogWriteGate`. **Out of S49:** live HTTP feeds in CI; `enableRealtimeSocialStream` remains `false`. |
| **07** | Agentic Infrastructure | **Foundations only (INF-1.x + batch schema):** typed scenario metadata for parameter overrides; `scenario_validate` / export gate hardening for deterministic `reportHash`; headless batch worker schema (seed grid + CSV fingerprint columns per INF-3.1); hooks into existing `BalticBatchRunner` / Mission Editor CLI — **no** full NL scenario gen (S50) or Monte Carlo UI. |

### S50 — E2 (Req 07 + Req 11)

| Req | Committed scope (direction) |
|-----|----------------------------|
| **07** | Scenario generation workers (INF-1.5 path); experiment orchestration beyond schema; balance batch worker integration |
| **11** | Agentic Mission Editor NL planner UX beyond CLI suggest; Unity edit-mode path |

### S51 — E5 (Req 06 subset)

Full corpora in CI strategy; runtime TL fork selection beyond export metadata; Import cohesion monitoring.

### S52 — E6 (Req 01 + Req 08)

Multi-thousand-entity headless benchmark as **MVP-done** for Req 01; stable sim API export; expand S45 DOTS pilot (determinism-engineer sign-off).

### S53 — E3 (Req 09)

Full DOTS spawn; MASS tier beyond harness `NF_SPAWN`; isolated fixtures before production hash.

### S54 — E3 (Req 10)

Orbital DEW runtime; escalation ladder; `KESSLER_RISK_METER` where scoped.

### S55 — E4 (Req 20)

Cesium/globe production; `HYPERSONIC_ALERT` UI; live Editor PNG evidence refresh.

### S56 — E1 sweep + program gate

Playtest AAR remediation ([`game-players-report-0620206.md`](../game-players-report-0620206.md)); proxy filter expansion; internal milestone gate when 21/21 rows satisfied.

---

## Standing invariants & gate matrix

Carry forward from v1.0 Release gate unless ADR explicitly revises:

| Gate | Floor / policy |
|------|----------------|
| **Headless tests** | **≥1227** at S49 start (`s48-release-gate-2026-06-20.md`); monotonic growth; never regress |
| **ReplayGolden** | **6/6** every sprint; after every sim-touching merge in S52–S54 |
| **C2 proxy** | **18/18+** baseline; expand matrix when new UI lands (S55 hypersonic alert, etc.) |
| **C2 proxy filters** | Retain S43 matrix (`DelegationBadge\|SimulationMode` + prior filters); append per sprint QA plan |
| **Baltic hash** | **`17144800277401907079`** immutable unless golden ADR + Producer + TD sign-off |
| **DelegationBridge** | **ZERO touch** default — ADR required |
| **CatalogWriteGate** | **Extend-only** default — ADR may revoke for specific data rows |
| **GitNexus** | `impact()` before edit; `detect_changes()` before commit; HIGH/CRITICAL → TD review |
| **Hindsight** | No `recall`/`reflect` inside sim `Tick()` or policy code (determinism) |
| **OSINT determinism** | Connectors: stable `OrderBy`; fixture-driven tests in CI; no wall-clock in hash paths |

**S49-01 produces:** `production/qa/gate-matrix-post-release-2026-06-21.md`

---

## Explicitly out of scope (S49+ program)

| Item | Reason | Revisit |
|------|--------|---------|
| **E7 Commercial launch** | RC1 is internal engineering only | Future commercial train + new scope decision |
| **E8 Art §5/§7 production** | Not tracker-row gates; optional parallel | Art-director request |
| **Multiplayer / global campaigns** | Never committed | Post-program epic |
| **Live social stream OSINT** | Req 05 MVP excludes 24/7 X/Twitter | Feature flag stays `false` |
| **Full corpora in CI** | Deferred to **S51** | Not S49 |
| **Cesium/globe production** | Deferred to **S55** | Not S49 |
| **Speculative / near-future runtime** | Deferred to **S53–S54** | Not S49 |
| **Playtest AAR gameplay fixes** | Deferred to **S56** sweep | Not S49 lead (E2 priority) |
| **Production hash change** | Requires golden ADR | Any sprint |

---

## Cut-line rules

1. Every S49+ story cites **this boundary** + committed row ID or epic ID.
2. **`impact()` mandatory** on Osint/Catalog/Platform/CLI symbols before edit.
3. **E2 lead:** S49 must not dispatch S51–S55 scope early without user ack + plan amendment.
4. **Req 05/07 S49:** production-hardening and foundations — not full MVP-done until S50+ closes remaining ACs.
5. **DelegationBridge / CatalogWriteGate:** ADR before deviation from zero-touch / extend-only.
6. **Determinism-engineer** required on any S52+ sim or hash-touching work.

---

## Sprint mapping & artifacts

| Sprint | Plan | Kickoff |
|--------|------|---------|
| **S49** | [`sprints/sprint-49-agentic-kickoff-mcp-osint-infra.md`](sprints/sprint-49-agentic-kickoff-mcp-osint-infra.md) | [`agentic/sprint-49-parallel-kickoff-2026-06-21.md`](agentic/sprint-49-parallel-kickoff-2026-06-21.md) |
| S50–S56 | TBD via `/sprint-plan` | TBD |

| Artifact | Path |
|----------|------|
| Roadmap | `docs/reports/future-sprint-roadpmap.md` §9 |
| v1.0 Release gate | `production/gate-checks/s48-release-gate-2026-06-20.md` |
| Implementation tracker | `Game-Requirements/implementation-tracker-2026-06-04.md` |
| Req 05 spec | `Game-Requirements/requirements/05-Dynamic-Systems-Agent.md` |
| Req 07 spec | `Game-Requirements/requirements/07-Agentic-Infrastructure.md` |
| Vertical slice (closed) | `production/milestones/vertical-slice-mvp.md` |
| Local/cloud routing | `production/agentic/local-cloud-agent-routing.md` |

---

## Prioritization record (2026-06-21)

| Decision | Value |
|----------|-------|
| Train | S49+ numbered sprints |
| Lead epic | E2 Agentic |
| RC1 intent | Internal engineering milestone |
| Tracker commitment | All v1.0-deferred rows through S56 |
| v1.0 / Spirit 1 | **CLOSED** |

---

*Published 2026-06-21. Cite this document for all S49+ stories.*
