# Release Enablement Scope Boundary — Track B (Baltic v1.0)

**Date:** 2026-06-20  
**Authority:** [`production/gate-checks/scope-expansion-decision-2026-06-20.md`](gate-checks/scope-expansion-decision-2026-06-20.md) — **APPROVED** (user sign-off 2026-06-20)  
**Supersedes for Track B:** [`production/polish-scope-boundary-2026-06-19.md`](polish-scope-boundary-2026-06-19.md) (remains Track A authority for S39–S41 only)  
**Roadmap:** [`docs/reports/future-sprint-roadpmap.md`](../docs/reports/future-sprint-roadpmap.md) §5, §9  
**Release target:** Baltic vertical slice v1.0 — shippable C2 + Platform Editor + Baltic replay path; not full Req 01–21 corpus MVP-done

## Purpose

Track B (S42–S48) advances committed post-MVP tracker rows, completes the art bible to a shippable standard, addresses structural debt, scales performance within determinism bounds, produces launch artifacts, and runs the Release gate. Any Track B story without a traceable link to this document and a committed row ID (where applicable) is out of scope until a new scope-expansion decision is recorded.

**Dispatch gate:** Scope expansion is **APPROVED**. S42 agent dispatch remains blocked until **S40 + S41 closeout PASS** (Track A exit + S41 ADR + polish-exit pack).

---

## In scope (B1–B6)

### B1 — Content completeness (13 tracker rows)

**S42 wave 1 — Catalog / Platform / Scenario / UX foundation**

| Req | Title | Committed scope |
|-----|-------|-----------------|
| **02** | Core Gameplay Loop | Complete Phase 1–2 UX; explicit **Begin Execution** flow sign-off |
| **06** | Database Intelligence | Dependency-graph platform→link edges in Editor UI; catalog provenance/quarantine surfacing in read-models (projection-side only) |
| **12** | Terms Glossary | UI tooltips for abort/sensor/cyber families in C2 + Platform panels |
| **13** | Doctrine ROE EMCON WRA | Doctrine inheritance panel sign-off complete (ADR-010); presentation-only |
| **16** | Logistics & Magazines | Live magazine counts from catalog in Platform Editor (loadout surfacing) |
| **21** | Platform Editor | Loadout/magazine Unity surfacing; advisory live Editor PNG refresh Phases C–H; bounded datalink latency workbook bridge |

**S42 scenario/data:** Baltic policy JSON maintenance only; **no production hash change** without golden ADR.

**S43 wave 2 — Engage / C2 features / combat remainder**

| Req | Title | Committed scope |
|-----|-------|-----------------|
| **03** | Simulation Modes | Mode UI on C2 top bar (RTwP / executing indicators; bounded) |
| **04** | Agent Delegation | C2 delegation badges; trust emit-only in order log (presentation via projection; **DelegationBridge ADR if bridge touched**) |
| **14** | Engagement & Fire Control | Swarm coordinator sectors (bounded Baltic fixture); DLZ Phase 2 (bounded) |
| **15** | Sensor Detection & EW | ECCM Phase 2 bounded; catalog onboard ECCM flags (read-model + staging) |
| **17** | Replay AAR & Order Log | Scrub UI stub + order-log AAR export hooks (headless-first) |
| **18** | Combat Domains | Mine-laying/clearing missions (bounded fixture); BDA lifecycle extension beyond S32-09 hook |
| **19** | Cyber & Comms | JADC2 node damage (bounded schema + fixture); ECCM Phase 2 tie-in with Req 15 |

**B1 exit criterion:** 13 committed rows updated to **MVP-done** (or **Partial+** with documented Baltic AC tests) at S43 closeout.

### B2 — Art bible (partial full structure)

- **S42 (3 agent-days):** §1–4 refinement; `AegisTokens.uss` recommendation implemented or documented; gate matrix cross-ref
- **S43 (5 agent-days):** §5/§7 formal N/A for v1 (policy docs, not character/VFX production); §8 full asset spec sheets; §9 sign-off; AD-ART-BIBLE full verdict
- **Budget:** 8 agent-days total (art-director + team-ui)

### B3 — Structural debt refactor (S44)

- **Decision module** refactor — target cohesion ≥70% (from 60%)
- **Telemetry module** refactor — target cohesion ≥72% (from 67%); **zero shared files** with Decision track
- **Osint audit** — read/fix minimal surface (68% cohesion)
- **Mandatory:** GitNexus `rename`/`impact`; ReplayGolden **6/6 after every merge**; determinism-engineer on replay gate track
- **Prerequisite:** S41 structural-debt ADR exists before S44 dispatch

### B4 — Performance scale-out (S45)

- **Req 01 subset:** Multi-thousand-entity **headless benchmark gate** (not full MVP row close)
- **Runtime/Sensors hot path** — profiled optimization on existing `ProjectAegis.Sim` path
- **DOTS/ECS sensor hot path** — **bounded pilot** on isolated fixture only (determinism-engineer sign-off required)
- **Engage scale** — hot-tick budgets per perf-profile appendix
- **Prerequisite:** B1 locked at S43 closeout

### B5 — Launch artifacts (S46)

| Artifact | Path (proposed) |
|----------|-----------------|
| Release checklist | `production/release/release-checklist-v1.md` |
| Store page drafts | `production/release/store/` |
| Localization pipeline spec | `production/release/i18n-pipeline-spec.md` |
| Launch evidence index | `production/qa/evidence/README-release-evidence-*.md` |

**Prerequisite:** B1 + B2 complete (S43 closeout).

### B6 — Release gate (S47–S48)

- **S47:** Full test sweep; gate-check draft; Buildkite preflight; Go/No-Go checklist
- **S48:** `/gate-check` Polish→Release; stage advance; program retro — **user + technical-director verdict mandatory**

---

## Standing invariants & gate matrix

Carry forward unchanged from Polish + scope-expansion decision §7:

| Gate | Floor / policy |
|------|----------------|
| **Headless tests** | **≥1215** at S42 start (`smoke-sprint-39-closeout-2026-06-20.md`; S40/S41 may raise); monotonic growth; never regress below post-S41 closeout baseline |
| **ReplayGolden** | **6/6** every sprint; S44+ after every B3 merge |
| **C2 proxy** | **18/18+** baseline; expand matrix when S43 lands Req 03/04 UI (checks 19–20 for mode UI + delegation badges) |
| **C2 proxy filters** | Existing PlayModeSmoke\|C2Selection\|OobTree\|LossesScoring\|BalticReplay\|FuelState\|AttackMenu\|PlatformImport\|Doctrine\|C2TopBar\|PlatformCatalogViewer\|PlatformComms\|PlatformLinkCatalog\|Graph* — append `DelegationBadge\|SimulationMode` when features land |
| **Baltic hash** | **`17144800277401907079`** immutable for v1.0 — Golden ADR + Producer + Technical Director sign-off only |
| **DelegationBridge** | **ZERO touch** default — explicit ADR revokes S34 control manifest; required if Req 04 badges touch bridge |
| **CatalogWriteGate** | **Extend-only** default — scope ADR may revoke for specific B1 data rows |
| **GitNexus** | `impact()` before edit; `detect_changes()` before commit; HIGH/CRITICAL warnings block merge without TD review |

**Expanded gate matrix doc:** S42-01 produces `production/qa/gate-matrix-track-b-2026-06-20.md` (or dated at S42 start).

---

## Explicitly out of scope (post-v1.0 / deferred tracker rows)

| Req | Reason to defer | Revisit |
|-----|-----------------|---------|
| **01** | Multi-thousand-entity perf gate → B4/S45 scoped benchmark, not content | S45 |
| **05** | S21+ MCP + Data P1 — OSINT panel exists; MCP integration post-Baltic | Post-release |
| **07** | Agentic Infrastructure — scenario gen + experiment workers | Post-release |
| **08** | Agentic Architecture — DOTS sim API export overlaps B4 selectively | S45 (hot path only) |
| **09** | Near-Future — full DOTS spawn, MASS tier | Post-release |
| **10** | Speculative Systems — orbital DEW, escalation ladder | Post-release |
| **11** | Agentic Mission Editor — full NL planner | Post-release |
| **20** | C2 UI — globe map / Cesium production, `HYPERSONIC_ALERT` UI | Post-v1.0 epic |
| **06** (subset) | Full corpora in CI (7208/4844/4403); runtime TL fork selection | Post-release data epic |

**Also out of scope:** globe/Cesium production; full corpora in CI; multiplayer; global campaigns; hypersonic DOTS spawn; production hash change without golden ADR; DelegationBridge changes without ADR.

---

## Cut-line rules

1. Every Track B story cites **this boundary** + committed row ID (B1) or epic ID (B2–B6).
2. **`impact()` mandatory** on Catalog/Platform symbols before edit.
3. **No scope creep** into deferred rows (Req 05/07/09/10/11/20) without new scope-expansion decision.
4. **B2 partial:** §5/§7 are policy N/A, not character/VFX production.
5. **B4 DOTS:** isolated-fixture pilot only until determinism-engineer sign-off.
6. **DelegationBridge / CatalogWriteGate:** ADR before any deviation from extend-only / zero-touch.

---

## Sprint mapping (S42–S48)

| Sprint | Epic(s) | Primary goal |
|--------|---------|--------------|
| **S42** | B1 wave 1 + B2 start | First committed tracker rows; art bible §1–4; expanded gate matrix |
| **S43** | B1 wave 2 + B2 complete | Engage/features batch; full 9-section art bible + asset specs |
| **S44** | B3 | Decision/Telemetry refactor + Osint audit |
| **S45** | B4 | Performance scale-out; determinism-engineer paired |
| **S46** | B5 | Release checklist, store pages, localization pipeline |
| **S47** | B6 prep | Full gate-check dry run; consolidated evidence |
| **S48** | B6 | `/gate-check` Polish→Release; stage advance |

See [`docs/reports/future-sprint-roadpmap.md`](../docs/reports/future-sprint-roadpmap.md) §9 for sprint plans, kickoffs, and dispatch status.

---

## Related artifacts

| Artifact | Path |
|----------|------|
| Scope-expansion decision (authority) | `production/gate-checks/scope-expansion-decision-2026-06-20.md` |
| Polish boundary (Track A only) | `production/polish-scope-boundary-2026-06-19.md` |
| Program execution guide | `production/agentic/s39-s48-program-execution-guide.md` |
| Track B readiness checklist | `production/agentic/sprint-42-48-readiness-checklist.md` |
| Implementation tracker | `Game-Requirements/implementation-tracker-2026-06-04.md` |
| Art bible | `design/art/art-bible.md` |
| Sprint plans S42–S48 | `production/sprints/sprint-42-*.md` … `sprint-48-*.md` |

---

*Published 2026-06-20 on APPROVE Track B default. Cite this document for all S42–S48 stories.*
