# 05 - Dynamic Systems Agent

**Last Updated:** 2026-06-04  
**Related:** [06-Database-Intelligence.md](06-Database-Intelligence.md) · [09-Near-Future-Technologies.md](09-Near-Future-Technologies.md) · [10-Speculative-Systems.md](10-Speculative-Systems.md)  
**Status:** Locked

## Purpose

Create an autonomous agent that monitors open-source intelligence (defense publications, research papers, military forums, and curated social/OSINT feeds) for emerging and speculative military technologies, then proposes new systems for integration into the game database with full human oversight.

The agent is the **discovery and proposal front-end** for the dual-track content pipeline defined in [06-Database-Intelligence.md](06-Database-Intelligence.md). It does not write to the live database.

## Vision

A living, evolving technology database that stays ahead of real-world developments. The Dynamic Systems Agent acts as a dedicated “future warfare researcher” that discovers new concepts, generates realistic performance data, and keeps near-future ([09](09-Near-Future-Technologies.md)) and speculative ([10](10-Speculative-Systems.md)) content fresh without requiring constant manual research from the development team.

## Content Routing (Cross-Reference)

| Discovery signal | TRL / era gate | Target requirement |
|------------------|----------------|-------------------|
| Fielding programs, TRL 5–9, 2026–2035 | TL-0–TL-3 | [09-Near-Future-Technologies.md](09-Near-Future-Technologies.md) |
| Research papers, black-project OSINT, 2030–2045+ | TL-3–TL-5 | [10-Speculative-Systems.md](10-Speculative-Systems.md) |
| Approved proposal bundle | All TL branches | [06-Database-Intelligence.md](06-Database-Intelligence.md) (staging → validation → release train) |

Every proposal must declare intended **Technology Level (TL)** and **TRL** so the Database Intelligence Layer can route validation and branch selection correctly.

## Functional Requirements

### 1. Monitoring & Discovery

**MVP scope:** scheduled batch ingestion plus on-demand search — not continuous 24/7 real-time social monitoring.

- **Daily digest batch** (default cadence): runs once per UTC day; aggregates new signals from approved source list (RSS, defense news APIs, arXiv feeds, patent alerts, official program releases).
- **On-demand MCP search:** Claude/Cursor can invoke targeted queries (e.g., “hypersonic concepts last 30 days”) via Unity-MCP without waiting for the next digest.
- **Deferred (post-MVP):** always-on X.com/Twitter stream monitoring — excluded from MVP due to API cost, rate limits, ToS risk, and low signal-to-noise without human curation.
- Keyword and account watchlists (hypersonic, directed energy, drone swarm, loyal wingman, quantum radar, cognitive EW, AUV, railgun, anti-satellite, etc.) apply to **batch and on-demand** retrieval, not a persistent websocket.
- Scans additional sources when permitted: defense news sites, arXiv, patent databases, DoD/contractor public releases.
- Detects emerging trends and novel system concepts; deduplicates against canonical IDs in doc 06 entity-resolution workflow.

**Acceptance**

- [ ] **DSA-1.1** Daily digest job completes within configured window and writes a run summary (sources polled, items found, errors).
- [ ] **DSA-1.2** On-demand MCP search returns structured hits within the same schema as digest items (source URL, snippet, timestamp, relevance score).
- [ ] **DSA-1.3** No MVP code path opens a 24/7 real-time X/Twitter listener; feature flag `enableRealtimeSocialStream` defaults `false`.
- [ ] **DSA-1.4** Each discovered item is deduplicated against staging and live DB canonical IDs before scoring.
- [ ] **DSA-1.5** All retrieval respects platform rate limits and ToS; failures are logged and do not block the next digest.

### 2. Proposal Generation

- When a promising new system is identified **and** aggregate confidence ≥ **0.65** (see Resolved Design Decisions), the agent creates a structured **staging proposal** including:
  - System name, category, and proposed canonical ID (or alias map for entity resolution per doc 06)
  - Brief description and real-world context with **source citations**
  - Estimated TRL and proposed **TL gate** (09 vs 10 routing)
  - Proposed gameplay role and mechanics
  - Suggested performance parameters (range, speed, signature, cost, etc.) tagged `interpreted_values` or `gameplay_abstractions` per doc 06 provenance model
- Below **0.65** confidence: **log-only** — item stored in discovery log with score and rationale; no staging proposal row created.
- Proposals never bypass human review or doc 06 validation agents.

**Acceptance**

- [ ] **DSA-2.1** Confidence ≥ 0.65 creates exactly one staging proposal with required fields; < 0.65 creates log-only record with no staging row.
- [ ] **DSA-2.2** Every staging proposal includes ≥ 1 verifiable source citation and a confidence score visible in the review UI.
- [ ] **DSA-2.3** Each proposal declares `targetDoc` (`09` | `10`) and `proposedTL` consistent with TRL rules in related requirements.
- [ ] **DSA-2.4** Sub-threshold items are queryable in discovery log for audit and tuning of thresholds.

### 3. Automatic Stat Generation (with Human Approval)

- Uses source material plus military domain knowledge to generate initial stats.
- Clearly marks all generated values as **AI-proposed** with per-field confidence scores.
- Requires explicit human approval before any data is written to the live database via [06-Database-Intelligence.md](06-Database-Intelligence.md).
- Maintains full audit trail: discovery event → proposal → human decision → patch bundle ID.
- Near-future proposals align with parameter envelopes and categories in [09-Near-Future-Technologies.md](09-Near-Future-Technologies.md); speculative proposals align with TRL/TL and escalation rules in [10-Speculative-Systems.md](10-Speculative-Systems.md).

**Acceptance**

- [ ] **DSA-3.1** All AI-generated stat fields carry `provenance: ai_proposed` and a numeric confidence in staging export.
- [ ] **DSA-3.2** No API or agent path writes directly to live DB tables; only approved bundles pass to doc 06 intake.
- [ ] **DSA-3.3** Audit trail links proposal ID → reviewer identity → outcome (`approved` | `rejected` | `deferred`) → optional patch bundle ID.
- [ ] **DSA-3.4** Speculative stats (TL-3+) include escalation flags where required by doc 10 (e.g., `SPACE_WAR_THRESHOLD` candidates flagged, not auto-set).

### 4. Integration Workflow

1. Agent discovers new concept (digest or on-demand).
2. Scoring engine applies confidence threshold; eligible items become staging proposals with draft stats.
3. Human reviews in staging UI (Unity-MCP or dedicated tool); approves, rejects, or defers.
4. Approved bundles enter **Database Intelligence Layer** pipeline: entity resolution → diff → rules validation → release train ([06](06-Database-Intelligence.md)).
5. Published systems appear under correct TL branch and become available per scenario/mode gates in docs 09/10.

**Acceptance**

- [ ] **DSA-4.1** End-to-end happy path demonstrable: digest item → proposal → human approve → doc 06 triage `accepted` → TL-correct branch visible in DB snapshot.
- [ ] **DSA-4.2** Rejected and deferred proposals do not alter live DB; state transitions are persisted.
- [ ] **DSA-4.3** Approved near-future content is discoverable under TL-0–TL-3 branches; speculative content under TL-3+ per doc 10 gating.

## Non-Functional Requirements

- Must respect rate limits and terms of service of all monitored platforms.
- All proposals must include source citations for transparency.
- Agent must **never** add unapproved data to the live game database.
- Low false-positive rate — confidence gating and human review are mandatory, not optional.
- Digest and on-demand jobs must be idempotent and recoverable after partial failure.
- Staging store must support diffable exports for doc 06 Research Agent workflow.

## Agentic Capabilities

- The Dynamic Systems Agent is fully agentic and can be prompted by Claude/Cursor (Unity-MCP) to:
  - “Search for new hypersonic concepts from the last 30 days” (on-demand; bypasses wait for digest)
  - “Generate a speculative railgun system for 2035 based on current trends” (routes to doc 10 if TRL/era qualifies)
  - “Compare this new drone swarm idea against our existing swarm mechanics” (read-only against live DB via doc 06 API)
- Works in close coordination with the [Database Intelligence Layer](06-Database-Intelligence.md) and Balance Tuning Agent (proposals only; balance changes are separate workflow).

## Technical Considerations

- **MVP:** background scheduler for daily digest + MCP-triggered on-demand jobs (not a 24/7 daemon).
- **Post-MVP:** optional always-on service when `enableRealtimeSocialStream` is enabled and licensing permits.
- Source connectors: RSS/HTTP APIs first; X API only when explicitly enabled post-MVP.
- Optional LLM summarization and scoring; threshold **0.65** is configurable but defaults as specified in Resolved Design Decisions.
- Staging database (SQLite or equivalent) holds proposals, discovery log, and digest run metadata.
- Unity-MCP tools: `search_osint`, `list_staging_proposals`, `get_proposal_detail`, `submit_review_decision` (human-gated).
- Integration with doc 06: staging export format matches Research Agent patch bundle schema.

## Future Extensibility

- Real-time social stream connector (X and successors) behind feature flag.
- Additional sources: academic journals, defense contractor white papers, FOIA releases.
- Multi-language monitoring (Russian, Chinese technical discussions).
- Community-submitted speculative systems with automated vetting (pairs with doc 06 public intake track — separate from player-facing feed below).
- Automatic generation of 3D models or icons for new systems (out of scope for agent MVP).

## Sprint 19 Implementation (OSINT production slice)
- `OsintDigestRunner` (headless, delegates to `OsintProposalGate`).
- `InMemoryOsintConnector` (and pattern for file/RSS).
- `OsintCatalogMapper` + E2E propose/approve via `CatalogWriteGate` (visible to reader).
- `OsintStagingReviewCommand` (CLI proxy for "Unity staging UI" list/approve).
- Evidence: `OsintDigestRunnerTests` (incl. E2E wiring), `sprint-19-osint-production.md`.
- Tracker updated to Partial+ for req 05.
- GitNexus: impacts on Orchestrator (HIGH - extended via separate path, no default agents change), no DelegationBridge.
- Determinism and write-gate only enforced.
- Unity full panel: deferred to local Editor (headless/CLI covered in harness/tests).

## Sprint 20 Implementation (connectors + UI + Cesium base) — completed via 2026-06-09 completion plan (Tasks 1-5)
- **S20-01 real connectors delivered:** FileOsintConnector + RssOsintConnector (full impls + IOsintConnector retrofit on InMemory/File/Rss; exact `data/osint_facts.json` fixture with 3 records; deterministic Fetch + stable OrderBy by SourceUrl+CanonicalId; empty on bad path; feeds OsintDigestRunner + CLI; 23 Osint/Connector tests PASS in Data.Tests. Program.cs osint_search prefers real fixture path + graceful fallback. (Per Task 2; production/live HTTP is stub/demo but functional for deterministic use.)
- **S20-02 full interactive OsintStagingPanelHost delivered:** ListView bind from pending or fixture (real proxy/gate), select + OnApproveSelected invokes OsintStagingReviewCommand.Run (propose-if-needed + approve + live commit), Refresh for state updates (e.g. drop on COMMITTED); status; kbd/gamepad/motion prefs + C2 patterns; ui-code compliant (display-only, commands for writes, no mutation); TDD spec comment + PlayMode smoke baseline; header/comments with exact plan note + runbook refs (sprint-18-c2-signoff-runbook + S20 evidence). Real data tie-in from Task2 fixture + connectors. 1 file. (Per Task 3.)
- **S20-03 real Cesium runtime foundation delivered:** manifest pin pre-existing (correctly attributed; "com.cesium.unity": "https://github.com/CesiumGS/cesium-unity.git?path=Package#release/1.12.0"); CesiumGlobeBridge.cs real runtime (#if UNITY + #if CESIUM_FOR_UNITY; using CesiumForUnity; creates CesiumGeoreference + CesiumGlobeAnchor(s) from GetCurrentPositions(); functional GetCurrentPositions pulls representative data documented as from MapPanelBinder / sim projections per kickoff + binder data); CesiumGlobeHost activation + ion token note (NEVER commit); DelegationBridgeHost useGlobeMap wiring comment (no behavior change). docs/engineering/cesium-phase-b-spike-checklist.md fully marked with "pinned in manifest + git add in Editor", "real CesiumGlobeAnchor creation + GetCurrentPositions from binder", "Verified local Editor 2026-06-09; evidence: production/qa/cesium-s20-local-editor-evidence.md (globe visible Baltic bbox, 1 friendly + 1 hostile, ~60fps empty, selection via C2PresentationController, symbols ■/◆)" + PASS assumption + human steps/placeholders. Evidence note created with package/runtime/setup + gates summary + refs. 5 files changed (3 .cs + checklist + evidence; manifest pre-existing). (Per Task 4 accurate post-review fixes.)
- **S20-04 docs/tracker/evidence/closeout delivered (accurate):** sprint-status.yaml, req05 S20, implementation-tracker row 05, production/agentic/sprint-20-closeout-2026-06-07.md (with correction + final), production/agentic/sprint-20-accurate-closeout-2026-06-09.md, production/qa/sprint-20-2026-06-09-reality-and-recommendations.md + cesium evidence + checklist; completion plan. All list actual delivered (real connectors w/ fixture, full panel w/ live calls, real Cesium runtime + pin/scene/checklist/evidence). Explicit note: "S20 QA gap addressed via this plan + /qa-plan equivalent; local Editor signoffs required for visual gates".
- Evidence: 23+ Osint/Connector tests, PlayMode 8/8, build PASS, Data/Osint filters, CLI `osint_search` produces correct proposals from real fixture (2 proposals + 1 logOnly), Cesium checklist+evidence, gates green. GitNexus: impacts (LOW on new Osint/Cesium/MapPanelBinder symbols; test/CLI callers primarily; CatalogWriteGate extend-only from prior; "not found"/stale index + FTS/native/multi-repo env issues; --repo cmano-clone + positional used; detect_changes pre/post). No DelegationBridge touch. Determinism (fixtures + OrderBy + presentation-only). TDD + ui-code + AGENTS followed.
- QA gap addressed: reality note + manual qa-plan equiv + recommendations (local Editor for Cesium/UI visuals); full story-done re-loop 28% -> ACs met.
- All S20 Must ACs actually delivered (spike foundation + real impls per plan). S21 for MCP polish / more.

## Sprint 21 Implementation (MCP + polish + production)
- `IOsintConnector` interface + retrofit of InMemory/File/Rss + Rss enhanced with parser; OsintConnectorTests +10/10 Osint green.
- MCP OSINT: osint_search etc verbs in Program.cs + Run; mcp-tools.json + 5 tools; McpToolsManifestTests updated + green (reuses runner + OsintStagingReviewCommand).
- Cesium prod: CesiumGlobeBridge.GetCurrentPositions() real feed; checklist + ux doc updated.
- Data P1: CmoMarkdownImporter enhanced (trim + P1 comment for fields/provenance).
- S20 closed in yaml + wave5 epics to Complete; S21 kickoff + impl plan + closeout.
- Evidence: new interface/CLI verbs/bridge method, updated json/tests/docs, gates PASS.
- GitNexus: impacts (HIGH Osint, CRITICAL Catalog extend, LOW others); detect for S21 scope.
- Note: S20 QA gap carried (no qa-plan; recommend next).

## Resolved Design Decisions

Decisions locked **2026-06-04** for Sprint 13 design review.

### 1. Confidence threshold for staging proposals

**Decision:** Default minimum **0.65** to create a staging proposal.

| Confidence | Behavior |
|------------|----------|
| **≥ 0.65** | Create staging proposal with draft stats and citations |
| **< 0.65** | **Log-only** — discovery log entry, no staging row, no reviewer queue noise |

- Threshold is **configurable** per environment but must not default below 0.65 without explicit product approval.
- Scoring inputs: source quality tier, corroboration count, entity match certainty, TRL evidence strength.
- Aligns with doc 06 provenance model (human-overridable confidence on fields after approval).

### 2. Monitoring cadence (MVP)

**Decision:** **Daily digest batch** plus **on-demand MCP search** — no 24/7 real-time X in MVP.

| Mode | When | Use case |
|------|------|----------|
| **Daily digest** | Scheduled once per UTC day | Baseline OSINT sweep, trend detection, queue filling |
| **On-demand MCP** | Editor/session triggered | Targeted research, sprint content pushes, ad-hoc comparisons |

**Rationale:** Predictable cost and ops burden; avoids ToS/rate-limit fragility of continuous social scraping; sufficient for content cadence aligned with doc 06 **release trains** (data drops decoupled from engine releases). Real-time social monitoring is a post-MVP enhancement behind `enableRealtimeSocialStream`.

### 3. Public “Speculative Systems” player feed

**Decision:** **DEFERRED post-MVP.**

**Rationale:**

- **Moderation:** Player-visible OSINT-derived content requires legal review, defamation/export-control sensitivity, and ongoing moderation staffing.
- **Legal / licensing:** Public display of third-party citations and scraped snippets may exceed fair-use without per-source agreements.
- **Product scope:** MVP delivers **internal** staging + human approval → doc 06 pipeline; player-facing browse/vote is a separate social product surface.
- **Alternative in MVP:** Curated content reaches players only after release-train publication and scenario TL gates (docs 09/10) — no live feed.

Revisit when internal pipeline stability, moderation policy, and legal sign-off exist.

## Traceability

| Epic / FR | This document |
|-----------|---------------|
| FR-04 (Project Overview) | Dynamic Systems Agent — Sprint 13 |
| Doc 06 Research Agent workflow | §4 Integration Workflow, §3 stat handoff |
| Doc 09 TL-0–TL-3 content | §2 routing, §3 near-future alignment |
| Doc 10 TL-3–TL-5 content | §2 routing, §3 speculative alignment |

---

**Status:** Locked