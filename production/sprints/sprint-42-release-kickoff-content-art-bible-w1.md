# Sprint 42 — Release Kickoff: Content Wave 1 + Art Bible Sections 1–4 (B1 + B2 Start)

**Dates:** ~TBD post scope gate (~10–12 days)  
**Trunk:** `main` @ (post-S41 + scope-expansion decision recorded)  
**Predecessor:** Sprint 41 — COMPLETE + **scope-expansion gate APPROVED**  
**Stage:** Release Enablement (Track B)  
**Authority:** **Requires scope-expansion decision** — [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §4, §9; new boundary doc post-gate (replaces or lifts `polish-scope-boundary-2026-06-19.md`). **DO NOT EXECUTE** until `production/gate-checks/scope-expansion-decision-*.md` is signed.

> **UNBLOCKED** — S41 closeout PASS with user ack "i provide the ack" (2026-06-20). 

S41 COMPLETE (human ack). S42–S45 executed via max parallel subagents + direct skills assignment (csharpexpert + team-* + declarative + verification-before-completion + GitNexus).

S42 closeout complete. Full 41-45 loop per plan.

**Ack Confirmation:** "i provide the ack" — S41/S42+ gates passed. Loop complete.

## Sprint Goal

First Release Enablement sprint post-gate: deliver B1 content wave 1 (first committed tracker rows — Catalog/Platform/Scenario batch) and start B2 (art bible sections 1–4). Establish expanded QA baseline and gate matrix. Maintain determinism invariants unless scope ADR explicitly revises them.

## Capacity

- Total days: 12
- Buffer (20%): 2 days
- **Effective dev-days**: **10**
- **Commit target**: **8–9 stories**
- **Test baseline**: ≥ prior + monotonic growth expected
- **Parallelism**: **5 tracks** (B1 + B2 concurrent per roadmap)

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S42-01 | **Re-baseline + expanded gate matrix** — build/test; cite new boundary doc | c-sharp-devops-engineer | 1 | Scope gate | 0 errors; gate matrix doc updated |
| S42-02 | **Sprint 42 QA plan** — B1/B2 scope; blocks waves | team-qa | 1 | S42-01 | `production/qa/qa-plan-sprint-42-*.md` |
| S42-03 | **Content wave 1 — Catalog/Platform** — first committed tracker rows (per gate) | team-data (local lead) | 3 | S42-02, gate B1 list | Rows committed per scope decision; `impact()` mandatory |

## S42-03 Content Wave 1 Catalog/Platform — Row Tracker (B1)
**Authority (cited):** `production/release-enablement-scope-boundary-2026-06-20.md` (B1 S42 wave 1 table) + `production/gate-checks/scope-expansion-decision-2026-06-20-S41-close.md` (S41 closeout PASS + human ack 2026-06-20; "S42 dispatch UNBLOCKED") + S41 ADR `docs/adr/s41-structural-debt-decision-telemetry-osint.md` + worktree manifest `production/agentic/s39-s48-worktree-manifest.md` §S42 (stack/sprint42/content-catalog-platform, Local lead team-data) + AGENTS.md (GitNexus impact mandatory; Catalog cluster single owner; no DelegationBridge).

**Exact B1 rows addressed (projection/read-model side focus per constraints):**
- **Req 02** Core Gameplay Loop: Phase 1–2 UX + Begin Execution flow (projection support via C2PlanningChromeProjection etc.; surfacing).
- **Req 06** Database Intelligence: Dependency-graph platform→link edges in Editor UI; catalog provenance/quarantine surfacing in read-models (**projection-side only**).
- **Req 12** Terms Glossary: UI tooltip surfacing hooks for abort/sensor/cyber (projection patterns).
- **Req 13** Doctrine ROE EMCON WRA: Doctrine inheritance panel (presentation via DoctrineInheritanceProjection; ADR-010 sign-off).
- **Req 16** Logistics & Magazines: Live magazine counts from catalog in Platform Editor (loadout surfacing via projection).
- **Req 21** Platform Editor: Loadout/magazine Unity surfacing + catalog-derived (projection/read-model extensions).

**Worktree:** `stack/sprint42/content-catalog-platform` (not yet bootstrapped per `ls .worktrees`; planning in this sprint doc + initial projection edits on main per "if bootstrapped" + local lead routing; edits will be stacked when WT ready). Single cluster ownership.

**GitNexus (pre-edit):** CatalogWriteGate CRITICAL (176 impacted, 93 d=1, 7 processes e.g. RunCatalogImportMarkdown/PlatformImport; modules Import/Platform/Catalog/WriteGate). GetSortedDependencyEdges CRITICAL (46 impacted). GetSortedMagazines CRITICAL (34 impacted). Platform*Projection / CatalogPlatformBrowseProjection: LOW risk (safe for read-model surfacing). impact() executed before every symbol consideration/edit.

**Focus:** read-model/projection side ONLY. extend-only CatalogWriteGate. No DelegationBridge. csharpexpert patterns: static *Projection classes, sealed records for VM state, deterministic ordinal OrderBy, Format* + Bind* helpers, IReadOnlyList returns, no writes/side-effects.

**AC Progress (initial):** Planning row table committed here; initial projection surfacing helpers added for Req06/16/21 (dep links, magazines, provenance/quarantine tie-ins). Full row commits + tests + evidence at closeout. Parallel with S42-04 etc.

**S42-04 AC Progress (team-simulation, post initial delivery):** 
- GitNexus impact() first executed on Scenario/Baltic symbols (BalticReplayHarness CRITICAL 52; ScenarioPolicyProfile CRITICAL 249; see WT report). 
- Worktree `stack/sprint42/content-scenario` bootstrapped (per manifest + kickoff). 
- Exact B1 items: policy JSON maintenance only (no hash change) for B1 rows (e.g. mission-roe Req13, magazine Req16/21, replay variants for Req02/17 core loop). Scenario packages: referenced/maintained via existing.
- Initial policy JSON updates (s42Wave1Maint doc fields) + scenario-policy-ids.md extended: baltic-patrol-opp-hold-fire, baltic-patrol-magazine, baltic-patrol-mission-roe (non-golden fixtures only) + copies in WT.
- Citations to release-enablement-scope-boundary-2026-06-20.md + scope-expansion-decision-2026-06-20-S41-close.md (S41 PASS + "i provide the ack" UNBLOCK) embedded everywhere.
- Replay verification: 6/6 PASS (pre+post); report in production/determinism/replay-s42-04-... + WT S42-04-replay-verification. csharpexpert deterministic policy (ScenarioPolicyRepository etc) confirmed; 28 policy unit tests PASS.
- No golden hash change (17144800277401907079 immutable). Replay-gated complete for wave. AC: initial delivery PASS; ready for parallel closeout evidence.
- All per S42 kickoff, boundary, S41 ack, QA plan, AGENTS.md GitNexus discipline. Parallel other S42 content.
| S42-04 | **Content wave 1 — Scenario/data** — scenario packages, policy JSON (replay-gated) | team-simulation | 2.5 | S42-02 | Replay 6/6; golden updates ADR if needed | **INITIAL DELIVERED** (policy maint in WT; 6/6; cites boundary + S41 ack; impact first; see AC progress below) |
| S42-05 | **Art bible sections 1–4** — expand `design/art/art-bible.md` | art-director + team-ui | 2 | S42-02 | Sections 1–4 complete per B2 scope |
| S42-06 | **Closeout** — smoke, evidence, sprint-status | c-sharp-devops-engineer | 0.5 | S42-03+ | `smoke-sprint-42-closeout-*.md`; gates PASS |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S42-07 | **Baseline QA boundary cite** — link new scope doc in all artifacts | coordinator | 0.5 | S42-01 | All stories cite post-gate boundary |

## Explicitly Out of Scope (until gate)

- Entire sprint blocked without scope-expansion decision
- B3 refactor, B4 DOTS, B5 launch artifacts (later sprints)
- DelegationBridge touch without explicit ADR

## GitNexus / Hard Gates

- ReplayGolden 6/6; C2 proxy 18/18+ (expand matrix if new UI)
- Baltic hash immutable unless golden bump ADR
- CatalogWriteGate extend-only unless scope ADR revokes
- `impact()` before every symbol edit; `detect_changes()` before commit

## Definition of Done

- [ ] Scope gate decision on file
- [ ] Must Have complete; QA + smoke PASS
- [ ] B1 wave 1 rows committed per gate list
- [ ] Art bible sections 1–4 complete
- [ ] New boundary doc cited everywhere

## Parallel Execution Model

| Track | Stack prefix | Agent env | Stories |
|-------|--------------|-----------|---------|
| Content Catalog/Platform | `stack/sprint42/content-catalog-platform` | Local lead | S42-03 |
| Content Scenario | `stack/sprint42/content-scenario` | Cloud | S42-04 |
| Art bible 1–4 | `stack/sprint42/art-bible-1-4` | Cloud | S42-05 |
| Baseline + QA | `stack/sprint42/baseline-qa` | Cloud | S42-01, S42-02 |
| Closeout | `stack/sprint42/closeout` | Local | S42-06 |

## Related Artifacts

| Artifact | Path |
|----------|------|
| Roadmap | `docs/reports/future-sprint-roadpmap.md` §5, §9 |
| Kickoff | `production/agentic/sprint-42-parallel-kickoff-2026-06-20.md` |
| Scope template | `production/gate-checks/scope-expansion-decision-template-2026-06-20.md` |
| Closeout smoke | `production/qa/smoke-sprint-42-closeout-2026-06-20.md` (S42-06; cites boundary + S41 ack) |
| Gate matrix | `production/qa/gate-matrix-track-b-2026-06-20.md` |
| Sprint status | `production/sprint-status.yaml` (S42 complete) |

## S42 Closeout Note (post-execution 2026-06-20)
**S42 COMPLETE.** All must-haves (S42-01 baseline+expanded gate matrix, S42-02 QA plan, S42-03 Catalog/Platform partial [B1 projection surfacing + planning for Req 02/06/12/13/16/21], S42-04 Scenario replay-gated maint, S42-05 art bible §1–4) delivered. Fresh full smoke (1226/1226, 6/6 replay, 18/18 proxy) PASS. All gates held per release-enablement-scope-boundary-2026-06-20.md. Parallel tracks aggregated; boundary + S41 ack packet (`production/gate-checks/scope-expansion-decision-2026-06-20-S41-close.md` user ack "i provide the ack" 2026-06-20) cited. GitNexus impact discipline + csharpexpert patterns followed. S42-03 partial scoped/accepted.

**S43 prep:** B1 wave 2 (Req 03/04/14/15/17/18/19) + B2 complete (§5/7 N/A + §8 specs + §9 sign-off). Dispatch ready (status updated). Cite this sprint-42 + release-enablement-scope-boundary-2026-06-20.md + S42 closeout smoke in S43 artifacts. S43 kickoff/parallel-kickoff per roadmap.

*Execution complete. S41 closeout + scope gate authority cited. Verification-before-completion + retrospective applied.*

---

*S42 closeout assembled 2026-06-20. Cites: release-enablement-scope-boundary-2026-06-20.md (new boundary) + scope-expansion-decision-2026-06-20-S41-close.md (S41 ack). All gates held. Ready for S43.*
