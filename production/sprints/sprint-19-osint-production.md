# Sprint 19 — OSINT Production (req 05)

**Dates:** 2026-06-05 → 2026-06-19 (proposed)  
**Trunk:** `main`  
**Predecessor:** Sprint 18 complete — C2 headless evidence + catalog P2 + OSINT spike (PROCEED) on `main` @ `eeed8e1`

## Sprint goal
Deliver production OSINT / Dynamic Speculative Systems Agent (req 05) slice: headless digest worker + basic connectors (e.g. file/RSS) producing `OsintDiscoveryRecord`s, routed via `OsintProposalGate` + existing `CatalogDiffProposalAgent` / `DatabaseIntelligenceOrchestrator` to `CatalogWriteGate` for approved catalog updates; plus minimal Unity staging review UI for listing/approving proposals. Update tracker/docs for req 05. (C2 Editor manual remains local Unity step; headless evidence from S18.)

## Must have
| ID | Task | Agent / skill | Acceptance |
|----|------|---------------|------------|
| S19-01 | `OsintDigestRunner` (headless deterministic job; uses `OsintProposalGate.Partition`, produces batches) | c-sharp-engineer / team-data | Test harness executes digest (seeded, no wall-clock), outputs valid `OsintDiscoveryRecord[]` proposals/log-only, integrates with gate. GitNexus impact on related symbols first. |
| S19-02 | Basic connector (file-based or simple parser → `OsintDiscoveryRecord`) | c-sharp-engineer | >=1 connector impl (e.g. from local markdown/JSON fixture or RSS stub), produces records matching spike contract, unit tests. |
| S19-03 | Wire proposals to write-gate (propose + approve path via existing agents) | c-sharp-engineer | End-to-end: runner/connector → proposals → `ProposeSensorBatch` (or via DiffProposalAgent) → `ApproveBatch` commits visible in `SqliteCatalogReader.GetSortedSensorBindings()` + snapshot if applicable. Replay golden unaffected. |
| S19-04 | Minimal Unity staging review UI (list proposals, approve/reject triggering gate) | unity-specialist / ui-programmer / team-unity | Panel (C2 or editor host) binds to proposals from Data layer, buttons invoke approve (via Cli or direct), shows state (proposed/approved/quarantined), PlayMode smoke covers basic. |

## Should have
| ID | Task | Agent / skill | Acceptance |
|----|------|---------------|------------|
| S19-05 | Additional connector or MCP search stub | team-data | e.g. arXiv-like or patent example; extends S19-02. |
| S19-06 | Update req 05 doc + implementation tracker | writer | `Game-Requirements/requirements/05-...md` and tracker row 05 cite new AC tests + evidence; status Partial+ . |

## Nice to have
| ID | Task | Notes |
|----|------|-------|
| S19-07 | Hindsight retain for OSINT decisions or full pipeline test | `dev-cmano-clone` bank when server up. |
| S19-08 | Cesium or asset manifest kickoff (from prior gaps) | If capacity. |

## GitNexus rules (mandatory)
- **Before any edit:** run `gitnexus__impact` (MCP) on `DatabaseIntelligenceOrchestrator` (HIGH risk - 5 impacted, extend don't break existing Run* / ValidationPipeline), `CatalogDiffProposalAgent`, `OsintProposalGate`, `OsintDiscoveryRecord`, new `OsintDigestRunner` etc. Report blast radius (direct callers, processes, risk).
- **CRITICAL:** `DelegationBridge` — no edits/touches.
- Data/catalog work stays in `ProjectAegis.Data` / `Import/` / `Agents/` ; **all writes via `IWriteGate` / `ApproveBatch`** (no bypass).
- Determinism: digest/runner pure (fixed clock or scenario time, stable sort, seeded; no `DateTime.UtcNow`).
- After changes: `npx gitnexus detect_changes --repo cmano-clone` ; `/replay-verify` (or filter) if any order-log/engage touch.
- CLI integration: prefer `MissionEditor.Cli` for commands.

## Parallel worktree layout (suggested)
```
main
 ├── stack/sprint19/osint-digest-runner   (S19-01 + S19-03 core)
 ├── stack/sprint19/osint-connectors      (S19-02)
 └── stack/sprint19/osint-unity-ui        (S19-04)
```

## Quality gates (unchanged + sprint specific)
```powershell
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
```
Replay when touching sim/delegation or order log: `--filter ReplayGolden|ReplayOrderLog`

For new Data/CLI: `dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter Osint|Digest|Proposal`

## References
- OSINT spike (deferred items): `production/agentic/sprint-18-osint-spike-2026-06-04.md`
- Sprint 18 closeout: `production/agentic/sprint-18-closeout-2026-06-05.md`
- Req 05: `Game-Requirements/requirements/05-Dynamic-Systems-Agent.md` + tracker
- Spikes/agents: `src/ProjectAegis.Data/Osint/`, `src/ProjectAegis.Data/Agents/`
- Catalog write: `CatalogWriteGate`, `IWriteGate`
- UI patterns: existing C2 panels in `unity/ProjectAegis/Assets/Scripts/Runtime/`, `MissionEditor.Cli`
- GitNexus: impacts on Orchestrator etc (baseline run pre-plan)

*Created following superpowers (sprint-plan phases + writing-plans principles) after Sprint 18 closeout. Lean review mode.*
