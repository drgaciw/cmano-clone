# DRG-47 — Phase N scoping decision (2026-08-09)

**Issue:** [DRG-47](https://linear.app/drgamtd-workspace/issue/DRG-47) · REQ-09 / REQ-10 owner decision  
**Role:** Owner-delegated product scoping (same pattern as DRG-84 owner triage)  
**Authority:** SWARM-00 triage + Release stage + SWARM Phase A–C complete  
**Scope of this turn:** Docs honesty only — **no runtime code**, **no Phase N GDDs/ADRs**

## Decision summary

| Item | Verdict | Notes |
|------|---------|-------|
| REQ-09 Near-Future product-in-scope | **Shipped spine only** | Archetype catalog, TL gates, spawn plan, hypersonic boolean gate, etc. already on `main` |
| REQ-09 design matrix | **Phase N post-release** | CCA/swarm advanced runtime beyond SWARM Phase C, full AUV, full DEW thermal, quantum sensing runtime, JADC2 node model, CEW, MASS tier runtime, full DOTS spawn, Baltic NF content pack — **not scheduled for current Release train** |
| REQ-10 Speculative product-in-scope | **Gate spine only** | `ScenarioSpeculativeSettings`, `SpeculativeEngageGate`, catalog metadata |
| REQ-10 full platform runtime | **Post-launch Phase N / vision** | Orbital DEW, Kessler, LAWS agency, escalation meters, etc. |
| SWARM-27…30 | **Phase N deferred** | Not building now; track under DRG-47 or a Phase N umbrella if created |
| Phase N GDD/ADR authoring | **No** (this turn) | Explicitly mark post-release so design text stops reading as pending commitments |

## SWARM-27…30 mapping

| ID | Requirement | Phase | DRG-47 disposition |
|----|-------------|-------|--------------------|
| SWARM-27 | Split / merge swarm platforms | N | **Post-release deferred** — not on current Release train |
| SWARM-28 | Per-member full physics SoT | N / Won't MVP | **Deferred / Won't MVP** unless product fantasy reopens and replay strategy redesigned |
| SWARM-29 | True multi-static ISR mesh | N | **Post-release deferred** — distinct from shipped CEC (SWARM-31) |
| SWARM-30 | Full MUM-T packages with manned flight leads | N | **Post-release deferred** — after host + air ops maturity |

Corpus pointer: [22-Drone-Swarm-Platforms.md](../../Game-Requirements/requirements/22-Drone-Swarm-Platforms.md) § Phase N.

## Shipped spine inventory (in-scope product)

### REQ-09 (doc 09)

- Near-future archetype catalog (4) + `NearFutureArchetypeCatalog`
- TL + swarm-tier gates (`CatalogArchetypeGate`, `SwarmTierLimits.MediumMaxEntities = 500`)
- Spawn **plan** only (`NearFutureArchetypeRuntime.PlanSpawns`) — not full DOTS spawn
- Scenario schema: `maxTechnologyLevel` / `nearFutureUnits`
- CLI `scenario_near_future_spawn`
- Baltic harness `NF_SPAWN` log/register
- `HypersonicEngageGate` boolean preview
- `CatalogPlatformBinding.GameTechnologyLevel`

### REQ-10 (doc 10)

- `ScenarioSpeculativeSettings` (TL + `BLACK_PROJECT_MODE`)
- `SpeculativeEngageGate` → `MvpEngagementResolver` pre-resolve
- Policy JSON `speculative` block + scenario fixtures
- Catalog metadata: `data/catalog/speculative_platforms.json`

## Explicit non-goals (this decision)

1. Do **not** implement Phase N runtime under `src/`.
2. Do **not** author full GDDs/ADRs for Phase N design matrix until product re-opens Phase N.
3. Do **not** treat research-integrated category sections in docs 09/10 as Release backlog commitments.
4. Do **not** re-land historical S54 worktree-only types as shipped evidence without a new trunk PR.

## Corpus edits (this PR)

| File | Change |
|------|--------|
| `Game-Requirements/requirements/09-Near-Future-Technologies.md` | Owner decision block + status honesty |
| `Game-Requirements/requirements/10-Speculative-Systems.md` | Owner decision block + status honesty |
| `Game-Requirements/requirements/22-Drone-Swarm-Platforms.md` | Phase N note: SWARM-27…30 post-release deferred via DRG-47 |
| `Game-Requirements/Game-Requirements-Index.md` | 09/10 honesty lines + next-workflow update |
| `production/qa/drg-47-phase-n-scoping-2026-08-09.md` | QA note |

## When to re-open Phase N

Product may re-open Phase N only via explicit owner decision (new Linear umbrella or DRG-47 children). Until then:

- Tracker rows remain **Partial** / **Partial+** / **Phase N / not on main** as already stated.
- Agents must not schedule SWARM-27…30 or full NF/speculative runtime as Release work.

## Related

- SWARM program closeout: `production/agentic/swarm-program-closeout-2026-08-09.md`
- Doc 22 owner triage (DRG-84): Phase A–C + SWARM-31 in scope; SWARM-27…30 near REQ-09/10
- Implementation tracker: `Game-Requirements/implementation-tracker-2026-07-04.md` rows 09, 10, 10b
