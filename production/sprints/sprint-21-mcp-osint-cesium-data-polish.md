# Sprint 21 — MCP OSINT Tools + Cesium Production + Data P1 + Connector Polish

**Dates:** 2026-06-08 → 2026-06-22 (proposed)  
**Trunk:** `main`  
**Predecessor:** Sprint 20 complete — OSINT connectors (File/Rss) + full OsintStagingPanelHost + Cesium foundation (manifest+bridge) + docs on `main`

## Sprint goal
Implement Unity-MCP / CLI agentic tools for OSINT per req05 (search_osint, list_staging_proposals, get_proposal_detail, submit_review_decision using runner + real connectors); introduce IOsintConnector interface + retrofit + one additional real source (e.g. RSS/JSON dir); advance Cesium from foundation/stub to production (real MapPanelBinder feed, scene/host integration, useGlobeMap); deliver one Data P1 (e.g. enhance CmoMarkdownImporter for more fields/provenance per req06 'full platform import'); update tracker/reqs/epics/closeout/evidence. Headless MCP+CLI coverage; Editor local for Cesium visuals (runbook). All deterministic, GitNexus first, gates.

## Must have
| ID | Task | Agent / skill | Acceptance |
|----|------|---------------|------------|
| S21-01 | IOsintConnector interface + retrofit InMemory/File/Rss + new real source (e.g. RssOsintConnector full parser or dir JSON scanner); update tests/runner for interface. | c-sharp-engineer / team-data | Interface defined, InMemory+File+ Rss implement it, new source produces stable OsintDiscoveryRecord[] from fixture/real-ish data, TDD tests pass (incl feeds runner), determinism (stable OrderBy). GitNexus impact on Osint* (HIGH from tests) first. LOW risk extension. |
| S21-02 | Add MCP OSINT tools to CLI (new verbs: osint_search, osint_digest, osint_list_staging_proposals, osint_get_proposal_detail, osint_submit_review_decision); impl using OsintDigestRunner + connectors (File for 'real'), OsintStaging* for staging. Update Program.cs (usage, dispatch), mcp-tools.json manifest (with schemas), McpToolsManifestTests, add OsintMcp*Command if pattern fits. | c-sharp-engineer | CLI verbs work (dotnet run -- osint_search ...), MCP manifest test passes, tools delegate to data layer (e.g. search returns proposals from connector+runner), JSON via McpToolResult. CLI tests/E2E. PlayMode note for Unity-MCP. |
| S21-03 | Cesium production: extend CesiumGlobeBridge to real position feed (from MapPanelBinder or sim), integrate useGlobeMap / host in DelegationBridgeHost or C2, update spike checklist (more rendering/data items marked with S21 evidence), update design/ux/c2-map-placeholder.md or c2-command-post for P0 complete. | unity-specialist / team-unity | Bridge provides live positions, compiles/runs, checklist updated, no breakage to MapPanelBinder. Editor PlayMode visual gate + runbook update (like S18 C2, S20 foundation). |
| S21-04 | Data P1: enhance CmoMarkdownImporter.cs (or related) for additional CMO fields + provenance per req06 tracker 'Full platform import'; or add simple TL gate stub in CatalogWriteGate (non-breaking); + tests + evidence. + full S21 docs/tracker/closeout/epic bump. | c-sharp-engineer / writer | One enhancement lands (e.g. more fields parsed, provenance recorded), tests green, tracker row 06 updated, no break to existing import (Catalog CRITICAL - extend only). |

## Should have
| ID | Task | Agent / skill | Acceptance |
|----|------|---------------|------------|
| S21-05 | Additional real connector (e.g. Http with cache/fixture for determinism) or full real-time. | team-data | Extends interface, tests. |
| S21-06 | More MCP tools or Cesium perf/camera. | ... | ... |

## Nice to have
| ID | Task | Notes |
|----|------|-------|
| S21-07 | Wave5 UI polish or other tracker items if capacity. | If ahead; defer otherwise. |

## GitNexus rules (mandatory)
- **Before ANY symbol edit:** run `gitnexus__impact` (MCP) upstream on target (e.g. OsintDigestRunner, CatalogWriteGate, MapPanelBinder, Program, new Osint*Command/Mcp* , CesiumGlobeBridge, CmoMarkdownImporter); report callers/processes/risk. Re-run `gitnexus__detect_changes` before commit-like state.
- **CRITICAL (extend-only):** CatalogWriteGate (18 impacted, 7 procs incl OnApproveSelected in OsintStagingPanelHost, Run* in Program; from S20+). ONLY post-commit optional hooks (non-fatal), no behavior/signature change to existing callers or tests. 
- **ZERO touch:** DelegationBridge (still CRITICAL from prior analysis).
- Osint* (runner, connectors, new MCP): HIGH (5+ tests from S20) or LOW; extend carefully.
- MCP CLI (Program, mcp-tools.json, ps1): LOW-MED (manifest/tests).
- Cesium/Map: LOW.
- After edits to Data/CLI/Cesium: `gitnexus__detect_changes repo=cmano-clone` (scope: unstaged or compare).
- Determinism for connectors: stable `OrderBy` on CanonicalId + source, seeded or fixture-driven only, no live network or `DateTime.UtcNow` in hash/sort paths (metadata timestamps ok if not affecting replay golden). MCP on-demand can use live but tests fixture-only.
- Cesium: manifest/package edits affect only Unity build/Editor — document pin version + Ion note (no secrets committed); update spike checklist + runbook.

## Parallel worktree layout (suggested)
```
main
 ├── stack/sprint21/osint-connectors-mcp   (S21-01 + S21-02 CLI/MCP)
 ├── stack/sprint21/cesium-prod          (S21-03)
 └── stack/sprint21/data-p1              (S21-04 + docs)
```

## Quality gates (unchanged + sprint specific)
```powershell
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "Osint|Catalog|Snapshot|WriteGate"
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter Mcp
# If any order-log / projection / engage touch: add --filter "ReplayGolden|ReplayOrderLog"
# Cesium/Unity: Editor PlayMode or adapter smoke; manual checklist evidence per cesium-phase-b-spike-checklist.md + new S21 runbook note
```

## References
- Prior S20: `production/sprints/sprint-20-osint-cesium-foundation.md`, `production/agentic/sprint-20-closeout-2026-06-07.md`, `docs/superpowers/plans/2026-06-07-sprint-20-osint-cesium-foundation-impl.md`
- Req 05 (MCP tools spec): `Game-Requirements/requirements/05-Dynamic-Systems-Agent.md` (Unity-MCP tools: `search_osint`, `list_staging_proposals`, `get_proposal_detail`, `submit_review_decision`; on-demand + digest; agentic capabilities)
- Tracker: `Game-Requirements/implementation-tracker-2026-06-04.md` (row 05/06/20 next)
- MCP CLI pattern: `src/ProjectAegis.MissionEditor.Cli/Program.cs` (osint_staging_review, Run*), `McpToolResult.cs`, `tools/mission-editor/mcp-tools.json`, `tools/mission-editor/Invoke-MissionEditorMcp.ps1`, `src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs`
- OSINT: `src/ProjectAegis.Data/Osint/*` (DigestRunner, OsintDiscoveryRecord, ProposalGate, Connectors/InMemory+File+Rss, OsintStagingReviewCommand), Osint*Tests
- Cesium / map: `unity/ProjectAegis/Assets/Scripts/Runtime/CesiumGlobeBridge.cs`, `manifest.json`, `src/ProjectAegis.Delegation/Projection/MapPanelBinder.cs`, `docs/engineering/cesium-phase-b-spike-checklist.md`, `design/ux/c2-map-placeholder.md`, `docs/architecture/adr-007-c2-map-presentation.md`
- Data: `src/ProjectAegis.Data/Import/CmoMarkdownImporter.cs`, `WriteGate/CatalogWriteGate.cs`
- UI: `unity/ProjectAegis/Assets/Scripts/Runtime/OsintStagingPanelHost.cs` (S20 full), `DelegationBridgeHost.cs` (useGlobeMap)
- GitNexus: impacts baseline done in session (CatalogWriteGate CRITICAL, Osint HIGH, others LOW); index fresh (8898 nodes).

*Created following superpowers (sprint-plan phases + approved plan) after S20. User approved [A] via ask. Lean review mode (production/review-mode.txt). S20 QA plan gap noted (per its own kickoff Phase 5 + sprint-plan skill); recommend run `/qa-plan sprint` or team-qa before S21 impl. All code changes will obey AGENTS.md (impact before edit symbol, detect before commit state, no DelegationBridge).*

## Pre-work (MANDATORY before any impl)
1. `npx gitnexus analyze .` (completed in session, fresh 8898 nodes)
2. Impacts on OsintDigestRunner (HIGH 5 tests), CatalogWriteGate (CRITICAL 18 impacted, 7 procs e.g. RunOsintStagingReview, OnApproveSelected - extend only), MapPanelBinder (LOW), OsintStagingReviewCommand (LOW), Program (MCP), CmoMarkdownImporter, CesiumGlobeBridge, DelegationBridgeHost (if wiring). Report before edits.
3. Baseline gates: build + full test + PlayMode + Osint + Mcp filter (to be re-run at start of impl loop)
4. Read this kickoff + S20 artifacts + req05 + mcp-tools.json + spike checklist + relevant code (Osint*, CLI MCP, Cesium*, Cmo*) before coding.

*Next after write/approval: writing-plans for detailed TDD impl plan in docs/superpowers/plans/ with per-task GitNexus + exact code/test steps; then subagent-driven-development loop over all tasks.*
