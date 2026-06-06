# Sprint 20 — OSINT Connectors + Cesium Map Foundation

**Dates:** 2026-06-07 → 2026-06-21 (proposed)  
**Trunk:** `main`  
**Predecessor:** Sprint 19 complete — OSINT production slice (runner + InMemory + E2E + CLI proxy + Unity stub) on `main`

## Sprint goal
Deliver production-grade OSINT source connectors (file + RSS/HTTP stubs) feeding the digest/runner for real speculative data; complete the OsintStagingPanelHost to full interactive C2-style UI (list/approve with proper binding, input support); establish Cesium for Unity globe map foundation (package, basic scene + data bridge from existing MapPanelBinder / sim projections, position feed); update all trackers/req docs/evidence. Headless coverage + CLI proxies; full visual sign-off (Unity Editor local per established runbook pattern from S18 C2).

## Must have
| ID | Task | Agent / skill | Acceptance |
|----|------|---------------|------------|
| S20-01 | Real OSINT connectors (`FileOsintConnector`, `RssOsintConnector` stub or `HttpOsintConnector` minimal; implement `IOsintConnector`, produce `OsintRawFact` / feed `OsintDigestRunner`) | c-sharp-engineer / team-data | Connectors load from local fixtures or stub URLs, return stable lists, integrate with runner (no wall clock), unit + integration tests using temp files/seed data. GitNexus impact on Osint* first (LOW). |
| S20-02 | Full `OsintStagingPanelHost` (Unity UI Toolkit: ListView of proposals from CLI proxy or direct read via gate/reader, select + approve/reject buttons invoking OsintStagingReviewCommand or gate, status refresh, dim/hover per C2 patterns; keyboard/gamepad, motion prefs) | unity-specialist / ui-programmer / team-unity | Panel binds and displays pending/approved, approve triggers visible commit in reader + snapshot, state updates live. PlayMode smoke for logic; full visual + scene hookup per sprint-18-c2-signoff-runbook + new runbook note (Editor local only). |
| S20-03 | Cesium map foundation (pin CesiumForUnity in unity manifest if missing, basic globe scene or C2 globe host, minimal data bridge feeding unit/contact positions from MapPanelBinder or sim state to Cesium entities; bbox for Baltic) | unity-specialist / team-unity | Package loads, globe renders in Editor PlayMode without error, 1+ friendly + 1 hostile position projected (stub data ok), perf note (60fps empty). Headless: no breakage to projection tests. Editor visual gate + runbook (like C2). ADR-007 / spike checklist updates. |
| S20-04 | Docs, tracker, evidence, closeout | writer / all | Update Game-Requirements/implementation-tracker (req05 -> more complete, req20 map), 05-Dynamic-Systems-Agent.md (S20 section), add Cesium notes; production/sprint-status.yaml; production/agentic/sprint-20-closeout-*.md; qa/ evidence (smoke, osint extended, cesium checklist); epics if needed. All links to plans/evidence. |

## Should have
| ID | Task | Agent / skill | Acceptance |
|----|------|---------------|------------|
| S20-05 | MCP or additional connector (e.g. simple arxiv stub or search tool wiring) | team-data | Extends connector pattern; CLI or MCP exposure stub. |
| S20-06 | Minor Data P1 (e.g. enhance provenance or one import improvement from tracker row 06) | c-sharp-engineer | One small non-breaking addition, tests. |

## Nice to have
| ID | Task | Notes |
|----|------|-------|
| S20-07 | Full attack menu C2 polish or more wave5 UI if capacity | If ahead; defer otherwise. |

## GitNexus rules (mandatory)
- **Before ANY symbol edit:** run `gitnexus__impact` (MCP) on target symbol upstream (or `gitnexus_impact` via CLI skill); report direct callers, affected processes, risk level to user. Re-run `gitnexus__detect_changes` before commit-like state.
- **CRITICAL (extend-only):** CatalogWriteGate / ApproveBatch / related CLI (OsintStagingReviewCommand etc) — 17 impacted, 6 processes (RunOsintStagingReview, RunCatalogWriteApprove, RunCatalogWritePropose, RunCatalogImportMarkdown, RunCatalogIntelligence, ...). ONLY post-commit optional hooks (non-fatal), no behavior/signature change to existing callers or tests. 
- **ZERO touch:** DelegationBridge (still CRITICAL from prior analysis).
- Osint new code (connectors, runner extensions, OsintStagingPanelHost): LOW risk (primarily test callers in Osint*Tests).
- MapPanelBinder + new Cesium hosts: LOW (0 upstream hits).
- After edits to Data/CLI/Unity projection: `gitnexus__detect_changes repo=cmano-clone` (scope: unstaged or compare).
- Determinism for connectors: stable `OrderBy` on CanonicalId + source, seeded or fixture-driven only, no live network or `DateTime.UtcNow` in hash/sort paths (metadata timestamps ok if not affecting replay golden).
- Cesium: manifest/package edits affect only Unity build/Editor — document pin version + Ion note (no secrets committed); update spike checklist + runbook.

## Parallel worktree layout (suggested)
```
main
 ├── stack/sprint20/osint-connectors   (S20-01 + tests)
 ├── stack/sprint20/osint-staging-ui   (S20-02 + PlayMode)
 └── stack/sprint20/cesium-foundation  (S20-03 + manifest/docs + bridge)
```

## Quality gates (unchanged + sprint specific)
```powershell
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "Osint|Catalog|Snapshot|WriteGate"
# If any order-log / projection / engage touch: add --filter "ReplayGolden|ReplayOrderLog"
# Cesium/Unity: Editor PlayMode or adapter smoke; manual checklist evidence per cesium-phase-b-spike-checklist.md + new S20 runbook note
```

## References
- Prior S19: `production/sprints/sprint-19-osint-production.md`, `production/agentic/sprint-19-closeout-2026-06-06.md`, `docs/superpowers/plans/2026-06-06-sprint-19-osint-production-impl.md`
- Req 05 + S19 evidence: `Game-Requirements/requirements/05-Dynamic-Systems-Agent.md`, `Game-Requirements/implementation-tracker-2026-06-04.md` (row 05 next items)
- Cesium / map: `docs/engineering/cesium-phase-b-spike-checklist.md`, `docs/architecture/adr-007-c2-map-presentation.md`, `design/ux/c2-map-placeholder.md`, `src/ProjectAegis.Delegation/Projection/MapPanelBinder.cs`
- UI patterns: `unity/ProjectAegis/Assets/Scripts/Runtime/OsintStagingPanelHost.cs` (S19 stub to complete), `SensorC2PanelHost.cs`, `RightUnitPanelHost.cs`
- Write / CLI: `CatalogWriteGate`, `OsintStagingReviewCommand`, `Program.cs`
- GitNexus: impacts baseline (Osint LOW, CatalogWriteGate CRITICAL noted, MapPanelBinder LOW); index fresh (8821 nodes post-force).

*Created following superpowers (sprint-plan phases + writing-plans principles) after Sprint 19 closeout. User confirmed scope choice [A] via ask. Lean review mode (production/review-mode.txt). No QA plan for S20 yet — per sprint-plan skill Phase 5, will surface recommendation to run `/qa-plan sprint` before impl. All code changes will obey AGENTS.md (impact before edit symbol, detect before commit state).*

## Pre-work (MANDATORY before any impl)
1. `npx gitnexus analyze .` (completed, fresh)
2. Impacts on OsintDigestRunner / OsintProposalGate / CatalogWriteGate / MapPanelBinder / OsintStagingPanelHost (completed; CRITICAL on CatalogWriteGate reported above — extend only)
3. Baseline gates: build + full test + PlayMode + Osint filter (to be re-run at start of impl loop)
4. Read this kickoff + S19 artifacts + Cesium spike + relevant GDD/UX/ADR before coding.

*Next after write: writing-plans for detailed TDD plan in docs/superpowers/plans/ with per-task GitNexus + exact code/test steps; then subagent-driven-development loop over all tasks.*
