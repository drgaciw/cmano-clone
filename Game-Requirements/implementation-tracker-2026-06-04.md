# Game Requirements — Implementation Tracker

**Base:** `main` @ `43c24d2`  
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
dotnet test ProjectAegis.sln -v minimal   # 283 tests
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
| 09 | Near-Future Technologies | **Doc only** | req 09, `CatalogImportGate` (TRL) | TL-gated archetypes + swarm tier cap |
| 10 | Speculative Systems | **Doc only** | req 10 | `BLACK_PROJECT_MODE` in scenario policy |
| 11 | Agentic Mission Editor | **Partial** | `MissionEditor.Cli`, `ScenarioValidationEngine` | Unity edit mode; MCP create/patrol/strike tools |
| 12 | Terms Glossary | **Partial** | req 12 + TL rows (2026-06-04) | UI tooltips; codegen for abort reason enums |
| 13 | Doctrine ROE EMCON WRA | **Partial** | `PolicyEvaluator` WRA salvo, `ResolvedUnitPolicy` mission ROE, `baltic-patrol-mission-roe` / `wra-cap` fixtures | Unity doctrine inheritance panel (ADR-010) |
| 14 | Engagement & Fire Control | **Partial** | `MvpEngagementResolver.cs`, engage goldens | DLZ personality timing; unit panel preview |
| 15 | Sensor Detection & EW | **Partial** | `PdDetectionContactSimulator.cs`, sensor GDD | Contact FSM golden; ECCM Phase 2 |
| 16 | Logistics & Magazines | **Partial** | `FuelLedger`, `MagazineLedger`, `STRIKE_UNREACHABLE_FUEL` | Air ready/launch; ferry runtime |
| 17 | Replay AAR & Order Log | **Partial** | `DecisionLog`, `ReplayGolden*`, SHA-256 goldens | Scrub UI; CI golden gate on `main` |
| 18 | Combat Domains | **Partial** | `CombatOutcomeResolver.cs`, kill checkpoints | Domain validators; mount offline abort |
| 19 | Cyber & Comms | **Partial** | `CommsTimelineSimulator`, comms goldens | Order-delay timestamps; track staleness |
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