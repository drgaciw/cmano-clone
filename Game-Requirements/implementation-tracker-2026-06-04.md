# Game Requirements — Implementation Tracker

**Base:** `main` @ `02ed5b7`
**Last Updated:** 2026-06-04
**Index:** [Game-Requirements-Index.md](Game-Requirements-Index.md) | [00-Master-Index.md](../00-Master-Index.md)

## Verdict

| Scope | Status |
|-------|--------|
| Requirements **documentation** (01–20) | **Complete** — drafted, research-integrated |
| **MVP / Phase 1 gameplay** implementation | **In progress** — headless Baltic vertical slice; no doc is 100% MVP-done |
| **Index housekeeping** (this pass) | **Complete** — canonical index, CMAO DB doc, tracker |

Completing the full requirements corpus as *shipped game features* is multi-year work (req 07 phases 1–5). This tracker is the execution backlog for stacked `stack/*` branches.

## Verification baseline

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal   # 322 tests
pwsh tools/unity/Invoke-ManualQaHeadlessGate.ps1
```

## MVP status by requirement

| Req | Title | MVP status | Evidence (paths) | Next stack task |
|-----|-------|------------|------------------|-----------------|
| 01 | Project Overview | **Partial** | `design/gdd/game-concept.md`, `BalticReplayHarness.cs` | Multi-thousand-entity perf gate; agent personality on engage |
| 02 | Core Gameplay Loop | **Partial** | `SimulationSession.cs`, `data/scenarios/*.policy.json` | Phase 1–2 UX; explicit **Begin Execution** in Unity |
| 03 | Simulation Modes | **Partial** | `SimulationSessionPhaseTests.cs`, headless smoke | AvA batch benchmark; mode UI on C2 top bar |
| 04 | Agent Delegation | **Partial** | `DelegationOrchestrator.cs`, `TrustSignalEmitterTests.cs` | C2 delegation badges; trust emit-only in order log |
| 05 | Dynamic Speculative Systems Agent | **Not started** | — | OSINT proposal service + staging DB |
| 06 | Database Intelligence | **Partial** | `ProjectAegis.Data/Catalog`, `ScenarioValidationEngine` | DB agent stubs; full provenance on catalog fields |
| 07 | Agentic Infrastructure | **Partial** | `BalticReplayHarness`, `MissionEditor.Cli`, Hindsight | Scenario gen + experiment workers |
| 08 | Agentic Architecture | **Partial** | `ProjectAegis.Sim`, `Delegation`, `architecture.md` | DOTS sensor hot path; sim API export |
| 09 | Near-Future Technologies | **Partial** | `near_future_archetypes.json`, `CatalogArchetypeGate`, `SwarmTierLimits` | Hypersonic defense layer; CCA runtime spawn |
| 10 | Speculative Systems | **Partial** | `ScenarioSpeculativeSettings`, `SpeculativeEngageGate`, `speculative_platforms.json`, `baltic-patrol-black-project` fixture | Orbital DEW runtime; escalation ladder events |
| 11 | Agentic Mission Editor | **Partial** | `mission_update_patrol/strike`, `mission_delete`, MCP pipeline tests | Unity edit mode; NL planner; mcp-tools.json bindings |
| 12 | Terms Glossary | **Partial** | `abort_reason_manifest.json`, Sensor/Cyber families in `AbortReasonCatalog`, alignment tests | UI tooltips; mount-offline abort enum |
| 13 | Doctrine ROE EMCON WRA | **Partial** | `PolicyEvaluator` WRA salvo, `ResolvedUnitPolicy` mission ROE, `baltic-patrol-mission-roe` / `wra-cap` fixtures | Unity doctrine inheritance panel (ADR-010) |
| 14 | Engagement & Fire Control | **Partial** | `MvpEngagementResolver.cs`, engage goldens | DLZ personality timing; unit panel preview |
| 15 | Sensor Detection & EW | **Partial** | `ReplayGoldenSuite` + `baltic-patrol-stale` golden, contact FSM harness | ECCM Phase 2; datalink delay |
| 16 | Logistics & Magazines | **Partial** | `AIR_NOT_READY`, `FERRY_UNREACHABLE*`, `UnitReadiness` metadata | Runtime launch gate in sim; UNREP |
| 17 | Replay AAR & Order Log | **Partial** | `ReplayGoldenSuiteTests`, CI step in `dotnet-reusable.yml`, `tests/regression/README.md` | Scrub UI; AAR agent |
| 18 | Combat Domains | **Partial** | `CombatOutcomeResolver.cs`, kill checkpoints | Domain validators; mount offline abort |
| 19 | Cyber & Comms | **Partial** | `CommsOrderDelay`, `CommsTrackStaleness`, `PlayerOrderRecord.ExecuteSimTick`, comms policy fields | MCP comms tools; order-delay queue runtime |
| 20 | Command & Control UI | **Partial** | Unity C2 hosts, `c2-manual-signoff` | Globe map; close Editor PI-006 checklist |

## Research open gaps (P1)

| Gap | Owner doc | Tracker note |
|-----|-----------|--------------|
| `HYPERSONIC_ALERT` UI state | 20 | Deferred — add when hypersonic track in sim |
| `KESSLER_RISK_METER` | 18/19 | Deferred — speculative TL-4+ |
| JADC2 damageable node | 19 | Deferred — entity schema in Data epic |
| Monte Carlo experiment schema | 17 | Phase 5 / req 07 |

## Workflow (agents)

1. Pick row → read requirement MVP + linked GDD  
2. `gitnexus_impact` on target symbols  
3. `team-simulation` / `team-data` / `team-unity` by layer  
4. `replay-verify` for sim/delegation changes  
5. Update this row when MVP AC tests exist  

## Related

- [research-traceability.md](research-traceability.md)
- [reviews/requirements-13-20-design-review-2026-05-29.md](reviews/requirements-13-20-design-review-2026-05-29.md) — CONCERNS
- [production/agentic/pi-plan-completion-2026-06-04.md](../production/agentic/pi-plan-completion-2026-06-04.md)