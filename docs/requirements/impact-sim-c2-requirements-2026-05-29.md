# Requirements Impact Analysis: Simulation & C2 Bundle (Docs 13–20)

**Date:** 2026-05-29  
**Analyst:** requirements-analyst (with GitNexus CLI)  
**Scope:** P0 requirements from CMO manual gap fill — doctrine, engagement, sensors, logistics, replay, combat, cyber, C2 UI

### Data Sources Analyzed

- `Game-Requirements/requirements/13` through `20`
- `Game-Requirements/cmo-manual-traceability.md`
- `src/ProjectAegis.Delegation/**` (indexed codebase)
- GitNexus: `query`, `impact`, `context`, `detect-changes` (`--repo cmano-clone`)
- `docs/superpowers/specs/2026-05-28-agent-delegation-framework-design.md`

### GitNexus Index Status

- Repository: **cmano-clone** — up-to-date (commit `10dae1a`)
- **Note:** GitNexus MCP server not registered in Cursor; analysis used **CLI** (`npx gitnexus … --repo cmano-clone`).
- `detect-changes`: no symbol-level diff (requirements markdown not in code graph; re-run after C# implementation).

---

## Impact Summary

| Dimension | Impact Level | Affected Systems | Status |
|-----------|--------------|------------------|--------|
| Technical | **HIGH** | New sim domains (sensor, engage, logistics) | CONCERN — greenfield |
| Architecture | **HIGH** | ECS sim core, delegation bridge, order sinks | CONCERN — seam design |
| Integration | **HIGH** | `ProjectAegis.Delegation`, Unity adapter | OK — planned extension |
| Performance | **HIGH** | 5k+ entities, detection tick, logging | CONCERN — budgets TBD |

### Overall Impact: **HIGH**

---

## GitNexus Blast Radius (Existing Code)

### `IRoeFilter` — **HIGH** risk (doc 13 expansion)

| Depth | Affected |
|-------|----------|
| 1 | `PassthroughRoeFilter` |
| 2 | `AutonomyGateTests`, `DelegationOrchestrator` |
| 3 | `DelegationBridge`, `Program.cs`, mode tests, `ReplayGoldenTests` |

**Implication:** Doc 13 Policy Snapshot / EMCON / WRA must extend or replace `IRoeFilter` + `Order` model without breaking autonomy gate and golden replay tests.

### `DelegationOrchestrator` — **MEDIUM** risk

- Direct: `DelegationBridge`, demo, orchestration tests, `ReplayGoldenTests.RunAndFingerprint`
- **Implication:** Mission runtime, mode changes, and order routing (docs 14, 17, 20) touch the orchestrator tick path.

### `DecisionLog` — **LOW** risk (doc 17 expansion)

- Direct: tests, demo, orchestrator ctor
- **Implication:** Order log (doc 17) should **evolve** `DecisionLog` / `DecisionRecord`, not parallel logs.

### Execution flows found (no combat sim yet)

| Process | Symbols | Relevance |
|---------|---------|-----------|
| `ConfigureSimulationMode → …` | `SimulationModeConfigurator`, `AgentController`, `SeededRng` | Docs 03, 04 |
| `RunTick → ApplyOrder` | `OrderDispatcher`, `IOrderSink` | Docs 14, 17, 20 |
| `Tick → PerceivedState` | `PerceivedStateFactory` | Docs 15, 19 |

**Gap:** No indexed symbols for engagement resolver, contact FSM, magazine, or sensor detection — **greenfield** in `ProjectAegis.Sim` (or equivalent) per doc 08.

---

## Affected Systems

### Primary Impact (new or major extend)

| System | Impact | Description | Priority |
|--------|--------|-------------|----------|
| Policy / ROE (doc 13) | **modify** | Extend `IRoeFilter`, `OrderKind`, add `FireAbortReason`, Policy Snapshot | P0 |
| Order log (doc 17) | **modify** | Unify `DecisionLog` + sim events + engage/policy denials | P0 |
| Engagement (doc 14) | **new** | Pipeline beyond `OrderKind.Engage` + risk classifier | P0 |
| Sensors & contacts (doc 15) | **new** | Feed `ObservedState` / `ISimWorldSnapshot` | P0 |
| Logistics (doc 16) | **new** | Magazines, fuel, air ops; validation hooks | P0 |
| Combat domains (doc 18) | **new** | Domain validators on engage pipeline | P0 |
| C2 UI (doc 20) | **new** | Unity presentation of sim + delegation | P0 |
| Mission runtime (doc 11) | **modify** | Execute missions, timeline, not just editor | P0 |

### Secondary Impact

| System | Impact | Description | Priority |
|--------|--------|-------------|----------|
| Cyber/comms (doc 19) | **new** | Degrade `ObservedState`, order delay queues | P1 |
| Database (doc 06) | **modify** | Sensor/weapon/magazine schema for 15–18 | P0 |
| Agent delegation (doc 04) | **modify** | Policy snapshot on assign; stale track flags | P0 |
| Unity bridge | **modify** | `OrderDispatcher`, `ObservedStateBuilder` | P0 |

### Tertiary

| System | Impact | Description |
|--------|--------|-------------|
| Near-future (09–10) | **modify** | Hooks in 14, 15, 18 |
| AAR agents (07) | **modify** | Consume order log schema |
| MCP tools (11) | **modify** | `engage_*`, `contact_*`, `policy_*` |

---

## Conflicts

| Conflict | Type | Impact | Resolution |
|----------|------|--------|------------|
| `DecisionLog` vs full Order Log (doc 17) | Architecture | High | Extend schema; single append-only log; migrate golden tests |
| `IRoeFilter` vs Policy Snapshot (doc 13) | Architecture | High | `IRoeFilter` becomes facade over snapshot + rules engine |
| `OrderKind.Engage` vs engage pipeline (doc 14) | Integration | Medium | Engage intent → pipeline → `Order` emission |
| `PassthroughRoeFilter` vs real ROE/WRA | Correctness | High | Replace for production; keep passthrough for unit tests |
| Requirements docs vs no sim assembly | Schedule | High | Add `ProjectAegis.Sim` (or module) in architecture pass |
| Doc 17 Tacview P2 vs no ACMI code | Scope | Low | Defer |

---

## Performance Impact

| Aspect | Current | Proposed | Impact |
|--------|---------|----------|--------|
| Entities | Delegation stub, no mass sim | 5,000+ units | **Critical** — ECS/DOTS per doc 08 |
| Per-tick work | Orchestrator + policy choose | + detection + engage checks | **High** — broadphase required (doc 15) |
| Logging | `DecisionRecord` list | Full order log | **Medium** — ring buffer / sampling |
| Headless | Demo + tests | Batch CSV metrics | **Medium** |

---

## Recommendations

| Priority | Recommendation | Rationale |
|----------|----------------|-----------|
| **P0** | ADR: **Sim core assembly** + bridge contracts (`ISimWorldSnapshot`, `IOrderSink`) | GitNexus shows bridge exists; sim is not indexed |
| **P0** | Extend `DecisionLog` → `OrderLog` with typed entries (doc 17) | LOW blast radius; avoids dual timelines |
| **P0** | Implement `IPolicyEvaluator` + snapshot before expanding `IRoeFilter` | HIGH blast radius on `IRoeFilter` |
| **P0** | Run `gitnexus impact` on each new public type before merge | Project rule; CLI needs `--repo cmano-clone` |
| **P1** | Re-index after sim code: `npx gitnexus analyze` | Stale graph after large adds |
| **P1** | Golden scenarios: delegation + engage + contact in one replay test | Validates cross-doc contracts |

---

## Affected Design Documents

| Document | Impact | Action |
|----------|--------|--------|
| `design/gdd/systems-index.md` | High | Created — see systems-index |
| Per-system GDDs (15 systems) | High | Run `/design-system` in dependency order |
| `08-Agentic-Architecture.md` | High | Align ECS, event queue, serialization with 17 |
| Delegation framework spec | Medium | Update order log + policy sections |

---

## Affected ADRs

| ADR | File | Status |
|-----|------|--------|
| Sim assembly boundary | [adr-001](architecture/adr-001-sim-assembly-boundary.md) | Proposed |
| IPolicyEvaluator | [adr-002](architecture/adr-002-policy-evaluator.md) | Proposed |
| Order log schema | [adr-003](architecture/adr-003-order-log-schema.md) | Proposed |
| Tick pipeline order | [adr-004](architecture/adr-004-tick-pipeline-order.md) | Proposed |
| DOTS sim core | [adr-005](architecture/adr-005-dots-sim-core.md) | Proposed |

Master doc: [architecture.md](architecture/architecture.md)

---

## Next Steps (updated 2026-05-29)

1. ~~Design review~~ — CONCERNS; blockers C1 addressed in `order-log-replay.md` GDD
2. ~~`/create-architecture`~~ — done
3. ~~Foundation GDDs~~ — policy + order log done; sim-core skeleton at `design/gdd/simulation-core-time.md`
4. **Accept ADR-002** before implementing `IPolicyEvaluator` (GitNexus: `IRoeFilter` **HIGH**)
5. **`/setup-engine`** — pin Unity reference
6. **`/design-system`** — flesh simulation-core; then sensors → engage
7. **`npx gitnexus analyze`** after `ProjectAegis.Sim` code lands
