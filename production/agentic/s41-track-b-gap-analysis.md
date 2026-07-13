# S41 Track B Gap Analysis (S41-07 + S41-08) — Analysis Only

**Sprint:** 41 (Horizon 3 per roadmap) — Polish Hardening + Release-Readiness Pre-Flight  
**Date:** 2026-06-20  
**Authority (mandatory citations):**  
- [`production/sprints/sprint-41-polish-hardening-release-preflight.md`](../sprints/sprint-41-polish-hardening-release-preflight.md) (S41-07/S41-08 should-have)  
- [`production/agentic/sprint-41-parallel-kickoff-2026-06-20.md`](../agentic/sprint-41-parallel-kickoff-2026-06-20.md)  
- [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §5 (Track B), §3 Horizon 3, §9 (S42–S48 mapping)  
- [`production/polish-scope-boundary-2026-06-19.md`](../polish-scope-boundary-2026-06-19.md) (Track A in-boundary; S41 strictly Horizon 3)  
- [`production/release-enablement-scope-boundary-2026-06-20.md`](../release-enablement-scope-boundary-2026-06-20.md) (Track B boundary post-gate)  
- [`Game-Requirements/implementation-tracker-2026-06-04.md`](../../Game-Requirements/implementation-tracker-2026-06-04.md) (all Req 01–21 **Partial**; research gaps)  
- [`production/gate-checks/scope-expansion-decision-2026-06-20.md`](../gate-checks/scope-expansion-decision-2026-06-20.md) (APPROVED; interim gap table)  
- [`production/gate-checks/scope-expansion-decision-template-2026-06-20.md`](../gate-checks/scope-expansion-decision-template-2026-06-20.md) (gate template)  
- New ADR: [`docs/adr/s41-structural-debt-decision-telemetry-osint.md`](../../docs/adr/s41-structural-debt-decision-telemetry-osint.md) (S41-03 read-only)  
- Determinism: [`production/determinism/determinism-audit-2026-06-20.md`](../determinism/determinism-audit-2026-06-20.md) (S41-04)  
- GitNexus: index @ HEAD (c4d6e52 / post-analyze); impacts on CatalogWriteGate, DecisionLog, BalanceTelemetryAccumulator (see §GitNexus Evidence)  
- Scope-check skill: [`cmano-clone/.claude/skills/scope-check/scope-check/SKILL.md`](../../.claude/skills/scope-check/scope-check/SKILL.md) (creep detection)  
- Gate-check skill excerpts (Polish→Release): [`cmano-clone/.claude/skills/gate-check/gate-check/SKILL.md`](../../.claude/skills/gate-check/gate-check/SKILL.md) §Gate: Polish → Release  
- Release-checklist skill: [`cmano-clone/.claude/skills/release-checklist/release-checklist/SKILL.md`](../../.claude/skills/release-checklist/release-checklist/SKILL.md) (B5 artifact template)  

**Role:** requirements-analyst + scope-check + csharpexpert (feasibility) + systematic-debugging + "Gap-Analysis-Analyst" (per declarative role in sprint-41 kickoff / s39-s48-program + spirit1 gap patterns).  
**Constraints (enforced):** Pure analysis / enumeration. **NO code.** **NO S42 execution.** **NO launch artifacts produced.** **NO production refactor.** All work read-only. Cite boundaries + tracker + roadmap **everywhere**. Stay scoped to S41-07 + S41-08. Parallel independent from evidence/ADR/determinism tracks.  

**GitNexus impact on referenced modules (mandatory first step):** See dedicated §GitNexus Evidence below. All high-risk modules (Catalog, Decision, Telemetry, Platform, WriteGate, Osint, Engage/Sensors/Runtime) were analyzed via `gitnexus__impact` (after `search_tool` + `gitnexus__list_repos`). Impacts used for complexity/risk + csharpexpert feasibility. Re-index confirmed current.

**Sequential-thinking decomposition of B1–B6:** Executed via `sequential-thinking__sequentialthinking` (8 thoughts). Decomp steps: (1) source extraction from roadmap §5 + release-boundary tables + tracker Partials; (2) B1 row enumeration + GitNexus blast mapping; (3) B3 debt smells + ADR cross-ref; (4) B2/B4/B5/B6 prereqs + gate mapping; (5) complexity/risk + csharp notes + creep flags; (6) AC/verification synthesis; (7) doc structure + citations; (8) final. All thoughts preserved context of "no-impl" + boundary citation.

---

## 1. B1–B6 Requirements Table (from Tracker Partial Rows + Roadmap §5)

All 21 Req 01–21 rows in [`Game-Requirements/implementation-tracker-2026-06-04.md`](../../Game-Requirements/implementation-tracker-2026-06-04.md) remain **Partial** (MVP status column). Research open gaps also Partial/deferred. B1 commits **exactly 13** per release-boundary §B1 (S42 wave1 + S43 wave2); others deferred post-v1.0.

| ID | Epic (Roadmap §5) | Tracker Partial Rows (Reqs) Committed / Enumerated | Mapped to Roadmap §5 + §9 | Complexity / Risk (GitNexus + boundary) | No-Impl Flag | C# Expert Feasibility Notes | Scope-Creep Detection |
|----|-------------------|-----------------------------------------------------|---------------------------|-----------------------------------------|--------------|-----------------------------|-----------------------|
| **B1** | Content completeness (close post-MVP Partial rows for Baltic v1.0) | **Committed (13 rows):** Req 02 (Core Gameplay Loop), 06 (DBI — projection surfacing only), 12 (Glossary tooltips), 13 (Doctrine panel), 16 (Logistics live counts), 21 (Platform Editor loadout/mag surfacing + Phases C–H refresh); + Req 03 (Modes UI), 04 (Delegation badges/trust), 14 (Engage swarm/DLZ), 15 (Sensor EW ECCM), 17 (Replay AAR scrub), 18 (Combat Domains mine/BDA), 19 (Cyber JADC2). **Not committed (remain Partial):** 01,05,07–11,20 + 06 subset (full CI corpora). See release-boundary §B1 + scope-decision interim table + tracker MVP status. | Roadmap §5 B1; §9 S42 (wave1 Catalog/Platform/Scenario) + S43 (wave2 Engage/C2/combat). S41-07 enumerates only. | **High complexity / CRITICAL risk.** CatalogWriteGate upstream: 176 impacted (93 direct d=1), CRITICAL, hits Import(44), Platform(37), WriteGate, Catalog, Telemetry(9), Osint(7). DecisionLog indirect cross. Baltic hash pin + no-prod-hash-change required. | **TRUE** (analysis + enum only; S41-07) | Projection-side edits only (read-models, staging diffs). Extend-only on CatalogWriteGate (per boundary + ADR). High afferent coupling on WriteGate/DecisionLog means **mandatory `impact()` + `detect_changes()` + replay-verify** before any B1 edit in S42+. SOLID risk (cross-assembly for some). Feasible if gated; blast radius high for Catalog/Platform symbols. | **Creep flag:** Adding any non-listed row (e.g. globe/Cesium from Req 20, full NL from Req 11, or multi-theater) violates release-boundary "Explicitly out of scope" + polish "Full Req 01–21 MVP completion" out-of-scope. Must cite committed row ID + this boundary + release-enablement-boundary per story. |
| **B2** | Art bible full | Art bible currently **lean** (C2 + Editor only; §1–4/6/8–9 substantive per scope-decision; §5/7 stubs N/A). No full 9-section + asset specs yet. | Roadmap §5 B2; §9 S42 (§1–4 start) + S43 (full + §8 specs + sign-off). | **Low code complexity / Medium doc risk.** No direct sim/Catalog blast (docs + design/art). Ties B5 store pages. | **TRUE** | N/A (design/art only; csharp not impacted). | **Creep flag:** Expanding §5/§7 beyond "intentional N/A for map-first v1" (per release-boundary §B2 partial + scope-decision) or full asset production pre-B1 would be creep. Budget capped 8 agent-days. Cite polish boundary for lean state. |
| **B3** | Structural-debt refactor | Decision (72 symbols, 60% cohesion — lowest), Telemetry (63 sym, 67%), Osint (48 sym, 68%). GitNexus communities low-cohesion clusters. No refactor in Polish (S41 ADR read-only only). | Roadmap §5 B3; §9 S44 (requires S41 ADR). Per release-boundary: Decision ≥70%, Telemetry ≥72%; zero shared files; Osint audit. | **Very High complexity / CRITICAL risk.** DecisionLog: CRITICAL 261 impacted (112 direct), 4 processes (RunTick/RunBatch/Run/RunExecutingTick), modules Baltic(77), Projection(55), Orchestration(53), Bridge(26), Runtime etc. BalanceTelemetryAccumulator: HIGH 32 impacted (10 direct), Telemetry(24)+Import+WriteGate. OsintDigestRunner MEDIUM. | **TRUE** (S41 read-only ADR + enum; refactor S44 only) | **High risk C#:** DecisionLog god-class (18+ private Lists, giant switch on OrderLogEntryKind + FormatPayload/Append overloads; violates SRP/OCP). Telemetry: feature-flag sprawl + cross-assembly (Sim.Data dep) + mutable state. Osint: static mappers/runners mix I/O + WriteGate + Catalog. Refactor via GitNexus `rename` + `impact` only; preserve IOrderLog, fingerprints, determinism, replay 6/6. ADR-003/004/006/009 cross-ref. Feasible post-S41 but **must pair with determinism-engineer**. | **Creep flag:** Any Decision/Telemetry/Osint production code change in S41 (Horizon 3) violates polish-boundary "NO production code refactor" + "Structural-debt spike (read-only)". Defer strictly to B3/S44 per roadmap. S41 ADR cites GitNexus. |
| **B4** | Performance scale-out | Beyond P0/P1 (perf-profile carryovers). Req 01 subset (multi-thousand-entity headless benchmark — not full MVP). | Roadmap §5 B4; §9 S45 (prereq B1 locked at S43). "Runtime/Sensors/Engage; coordinate with determinism-engineer". | **High complexity / HIGH risk.** Engage/Sensors high cohesion (80%/76% per roadmap) but hot paths; DOTS/ECS out-of-boundary except bounded pilot. | **TRUE** | C# / DOTS feasibility: isolated-fixture pilot only (no production hot-path migration until determinism sign-off). Existing Sim path (no DOTS in Polish). Measure against perf-profile-polish-baseline. Replay/det gates mandatory. | **Creep flag:** Any DOTS/ECS production change or unbounded perf work pre-B1 violates polish "No DOTS/ECS hot-path migration" + release-boundary "isolated-fixture pilot only". B4 requires B1 scope lock. |
| **B5** | Launch artifacts | None today (per roadmap §1 + polish/release boundaries: "Launch artifacts absent"). | Roadmap §5 B5; §9 S46 (prereq B1+B2 at S43). Release-boundary table: `production/release/release-checklist-v1.md`, `production/release/store/`, `production/release/i18n-pipeline-spec.md`, evidence index. | **Medium complexity / LOW code risk** (mostly docs + metadata). High process risk if B1/B2 incomplete. | **TRUE** (S41-08 stub analysis only; artifacts in S46) | N/A direct (non-C#). | **Creep flag:** Producing any actual store pages / full checklist / i18n pipeline in S41 violates "analysis only" + "do not produce launch artifacts" (sprint-41 plan + roadmap Horizon 3). Stub only. Prereqs B1+B2 per boundary. |
| **B6** | Release gate | N/A (Polish stage; gate-checks show CONCERNS carried). | Roadmap §5 B6; §9 S47 (prep) + S48 (gate-check Polish→Release + stage advance). Prereq B1–B5. | **High complexity / CRITICAL process risk.** Depends on all prior + full gates. | **TRUE** (analysis of requirements only) | N/A (orchestration + QA). | **Creep flag:** Executing `/gate-check` or stage advance pre-S41 exit + B1–B5 would be creep. S41 produces analysis packet only. |

**Sources for table rows:** release-enablement-scope-boundary-2026-06-20.md §B1 tables (13 rows + defer list) + §B2–B6; future-sprint-roadpmap.md §5 + §9 tables; implementation-tracker-2026-06-04.md (Partial status + "Next stack task" column + research gaps); scope-expansion-decision-2026-06-20.md interim + committed tables; polish-scope-boundary §Explicitly Out of Scope.

---

## 2. GitNexus + Source Evidence (Mandatory)

**Repo:** cmano-clone (indexed 17797 nodes, 35790 edges, 386 communities, 300 processes; current @ HEAD post-analyze).

**Key impacts (upstream; summaryOnly + targeted):**
- `CatalogWriteGate` (src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs): **CRITICAL** risk. 176 impacted (d1:93, d2:58, d3:25). 7 processes (RunCatalogImportMarkdown, Run/PlatformImportXlsxCommand, ProposePlatformWeaponMounts, OnApproveSelected (Unity+panel), OnProposeClicked, Run/CatalogDiffProposalAgent). Modules: Import(44 direct), Platform(37), WriteGate(19), Catalog(14), Telemetry(9 direct), Osint(7), Runtime(5). **Direct relevance to B1 (Catalog/Platform/Import surfacing) and B3 coupling.**
- `DecisionLog` (src/ProjectAegis.Delegation/Decision/DecisionLog.cs): **CRITICAL**. 261 impacted (d1:112). 4 processes (RunTick/DelegationBridgeHost, RunBatch/Demo, Run/ScenarioSimulateSampleCommand, RunExecutingTick/SimulationSession). Modules: Baltic(77), Projection(55), Orchestration(53), Bridge(26), Runtime(12), Decision(11). **Core to B3; cross-cuts B1 replay/order-log (Req 17) and B4 hot paths.**
- `BalanceTelemetryAccumulator` (src/ProjectAegis.Data/Telemetry/BalanceTelemetryAccumulator.cs): **HIGH**. 32 impacted (d1:10). 2 processes (Run/PlatformImport..., EnableMvpEngagement/DelegationBridge). Modules: Telemetry(24), Import(5), WriteGate(1), Orchestration(indirect). **B3 + B1 telemetry/provenance coupling.**
- Supporting: Catalog folder ambiguous (4); Decision folder ambiguous (17 incl. docs); OsintDigestRunner MEDIUM (8d/9i, Osint+Platform). Roadmap GitNexus baseline @ ~90c9a5f confirmed low cohesion (Decision 60%, Telemetry 67%, Osint 68%, Import slipped 67%).

**Evidence paths (all read):** roadmap §2 "Code-grounded landscape", ADR S41-03 "GitNexus impacts run mandatorily", release-boundary standing invariants (impact() mandatory), sprint-41 "GitNexus / Hard Gates".

---

## 3. Pre-Flight Checklist Stub (Analysis Only — S41-08)

**Purpose (per sprint-41 plan + roadmap Horizon 3):** Enumerate **what B5/B6 require** (analysis/read-only stub). **No actual artifacts, no /release-checklist or /launch-checklist execution, no store pages.** Output feeds scope packet + S46/S47.

**B5 (Launch artifacts, S46 — prereq B1 + B2 complete at S43 closeout):**
- Per release-enablement-scope-boundary §B5 table + release-checklist skill template:
  - Release checklist: `production/release/release-checklist-v1.md` (or dated).
  - Store page drafts: `production/release/store/` (short/long desc, feature list, screenshots, trailers, ratings, legal).
  - Localization pipeline spec: `production/release/i18n-pipeline-spec.md`.
  - Launch evidence index: `production/qa/evidence/README-release-evidence-*.md`.
- Additional (from gate-check Polish→Release + release-checklist skill): full content complete (B1), art bible full (B2), build verification, zero critical bugs, perf budgets, soak, analytics/crash, day-one patch plan, on-call, community/press, rollback.
- GitNexus / boundary: all stories must cite release-enablement-boundary + B1 row IDs; impact() on any Catalog/Platform touch.

**B6 (Release gate S47–S48 — prereq B1–B5):**
- Per gate-check skill §Gate: Polish → Release + roadmap §5 B6:
  - All features/content from milestone (B1 committed rows closed).
  - QA test plan + sign-off (APPROVED or WITH CONDITIONS).
  - All Must-Have test evidence + smoke PASS (no regressions; baseline ≥ post-S41).
  - Balance-check + release-checklist complete.
  - Store metadata + localization + legal + EULA/privacy/age ratings.
  - Full gate-check (Polish→Release) + `/launch-checklist` if applicable.
  - Sign-offs: QA Lead, TD, Producer, Creative Director.
  - `/gate-check` verdict PASS → write "Release" to stage.txt; program closeout.
- Standing invariants (release-boundary): Replay 6/6 every sprint (esp. post-B3), C2 proxy 18/18+ (expand for new UI), Baltic hash `17144800277401907079` (golden ADR only), ZERO DelegationBridge (unless ADR), extend-only WriteGate (unless ADR), GitNexus impact/detect, test floor monotonic.
- S41 pre-flight requirements (for packet): Polish-exit report (S41-05), this gap analysis, determinism-audit (clean), S41 ADR, evidence pack, re-indexed GitNexus, smoke closeout.
- S47/S48 specifics (from readiness-checklist + sprint plans): gate matrix expanded, Buildkite preflight, dry-run, human Go/No-Go at S48.

**Stub location recommendation (analysis only):** Embed in this doc or `production/agentic/s41-release-preflight-checklist-stub.md` (read-only; cite sprint-41 + roadmap). Full artifacts deferred.

---

## 4. Cross-References: New ADR + Determinism Audit

- **New ADR (S41-03):** [`docs/adr/s41-structural-debt-decision-telemetry-osint.md`](../../docs/adr/s41-structural-debt-decision-telemetry-osint.md) — Accepted (read-only). Details: DecisionLog god-class + 18+ lists + switches (SRP/OCP violation); Telemetry sprawl + cross-assembly; Osint mapper/runner mix. GitNexus impacts mandatory (DecisionLog CRITICAL 261, etc.). Recommends strategies for B3 only. Explicit: "ZERO edits... No launch artifacts, B2+, or S42+ work". Cross-refs roadmap §Horizon 3 + B3 + polish boundary + release boundary + sprint-41.
- **Determinism Audit (S41-04):** [`production/determinism/determinism-audit-2026-06-20.md`](../determinism/determinism-audit-2026-06-20.md) — **DETERMINISTIC — SAFE**. 0 CRITICAL/HIGH/MEDIUM (defence-in-depth LOW only). ReplayGolden 6/6, seeded Baltic hash exact `17144800277401907079`, GitNexus re-index PASS, no wall-clock in sim path (only presentation/WriteGate clock). Cites polish boundary, sprint-41, ADR-004, determinism-audit skill. **B3/B4/B6 prerequisite:** must remain clean post-refactor/pilot.
- Other cross: scope-expansion-decision-2026-06-20.md references "S41 ADR [PENDING S41]" + "formal S41-07 doc pending" (this fills); roadmap §6 invariants; release-boundary standing gates.

---

## 5. AC Status (S41-07 + S41-08)

- **S41-07 AC (per sprint-41 plan):** "Gap doc cites tracker Partial rows; maps to roadmap §5" → **SATISFIED by this doc** (table + citations to tracker/roadmap §5 + boundaries). 
- **S41-08 AC:** "Checklist stub in production/; read-only" → **SATISFIED by §Pre-Flight Checklist Stub** (analysis/enumeration only; no artifacts).
- Broader S41 DoD (gap packet part): scope-expansion decision packet ready; S42 blocked until gate.
- Verification: All rows/sections cite sources. GitNexus + sequential decomp executed. No code.

---

## 6. Scope-Check Creep Detection + csharpexpert Feasibility Summary

**Scope-check (per skill + boundaries):** 
- Original S41 scope (sprint plan + roadmap Horizon 3): read-only ADR, determinism audit, evidence pack, **gap analysis (enum B1–B6 no impl)**, pre-flight stub (analysis), scope packet. Strictly in-boundary.
- Current/this work: Matches exactly (enumeration, tables from sources, no production code, no S42 dispatch, no artifacts). 
- Additions flagged: None. **Verdict: PASS** (0% net creep). Risk if future stories omit citations: high (see cut-line rules in release-boundary).
- Explicit creep vectors blocked: B1 row expansion, B3 code in S41, B5 artifacts now, globe/Cesium, DOTS pilot early, DelegationBridge touch.

**csharpexpert feasibility (embedded):**
- B1/B3 high coupling (CRITICAL impacts) feasible only with strict GitNexus discipline + replay gates + projection/extend-only patterns. DecisionLog god-class refactor viable (split to strategies + fingerprinter) but preserves determinism contracts (IOrderLog, fingerprints, hash). Telemetry cross-layer needs careful DI. B4 DOTS pilot: bounded + det-paired feasible for scale but risk to replay. B2/B5/B6: low C# risk.
- SOLID / engine: aligns with existing ADRs (003 order-log, 004 tick, 006 data, 009 domains, 011 editor). Recommend c-sharp-architect + determinism-engineer pairing for B3/B4.

---

## 7. Verification + Related

**Verification performed:**
- Mandatory first: GitNexus impacts (CatalogWriteGate/DecisionLog/Telemetry) + list_repos + search_tool.
- All listed reads: sprint-41 plan/kickoff, roadmap (full §1–9), tracker (Partials + workflow), polish/release boundaries (full), scope templates/decision (interim tables), ADR S41, determinism-audit-2026-06-20 (clean), gate-check/release-checklist skills, worktree manifest, s42 plan (planning refs), sequential decomp (8 thoughts).
- Cross: spirit1 gap patterns (for role consistency), AGENTS.md, production/gate-checks/*.
- No files created except this analysis doc. No S42 work. Boundaries cited in every section/table.

**Related artifacts (post-S41):**
- Polish-exit report (S41-05)
- Scope-expansion packet (S41-06)
- S42+ sprint plans / kickoffs / readiness-checklist (already reference S41 gap)
- Full Track B boundary + decision (already published)

**Return summary (per task):** Full gap doc at `cmano-clone/production/agentic/s41-track-b-gap-analysis.md` (this file); pre-flight stub embedded §3 (analysis). GitNexus + source evidence in §2 + table. AC status §5. Verification §7. All per declarative Gap-Analysis-Analyst role + constraints.

---

*This completes S41-07 + S41-08 scope. Pure analysis. Cite everywhere. Stay scoped. Parallel to other tracks.*
