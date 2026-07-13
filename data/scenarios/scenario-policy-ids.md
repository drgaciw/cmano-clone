# Scenario policy template IDs

> **Full authoring reference:** for the complete `*.policy.json` field/loader reference (all
> top-level + `engage` fields, enum values, mission triggers, validation errors, replay
> constraints), see [`docs/engineering/scenario-policy-authoring.md`](../../docs/engineering/scenario-policy-authoring.md).
> This page is the template-ID matrix and per-sprint content log.

Used by `SimulationModeConfigurator.Apply(..., scenarioPolicyId: "...")` and `ScenarioPolicyCatalog`.

| ID | Friendly ROE | Opposing ROE | Loop policy overrides | Use |
|----|----------------|--------------|------------------------|-----|
| `baltic-patrol` | Weapons free | Weapons free | defaults | Default training |
| `baltic-patrol-opp-hold-fire` | Weapons free | Hold fire | defaults | Escalation drill |
| `restricted-engagement` | Weapons tight | Weapons tight | defaults | Restricted fires |

Optional JSON fields (default when omitted): `playerInfoModel` (`fullTransparency`), `personalityEditPolicy` (`anytime`), `engage` (range/envelope/magazine rounds for MVP engage priming), `allowDualSideControl` (`false`). See req 02/03 and `docs/superpowers/specs/2026-05-30-simulation-modes-decisions-design.md`.

JSON files: `data/scenarios/*.policy.json` loaded by `ScenarioPolicyRepository` (overrides built-in catalog when ids match).

| `baltic-patrol-catalog` | Weapons free | Weapons tight | `catalogDetection` (no inline `basePd`; uses platform catalog) | Catalog-backed Pd harness |
| `combat-domains-smoke` | Weapons free | Weapons tight | `engage.combatDomainsEnabled=true` | **Test-only** ADR-009 flag-on smoke (`CombatDomainsSmokePolicyTests`; not ReplayGolden 6/6) |
| `baltic-patrol-combat-domains` | Weapons free | Weapons tight | `engage.combatDomainsEnabled=true` (full Baltic detection trials) | **Test-only** ADR-009 flag-on Baltic golden (`BalticCombatDomainsPolicyTests`; not ReplayGolden 6/6) |

### Production Baltic `combatDomainsEnabled` policy (S29-05 → S30-09)

| Fixture | `combatDomainsEnabled` | Pinned hash gate | Status |
|---------|------------------------|------------------|--------|
| `baltic-patrol` (production default) | `true` (S30-09 flip) | ReplayGolden 6/6 — `WORLD_HASH=17144800277401907079` | **Active** — ADR-009 migration step 4; hash unchanged vs pre-flip engage golden (allow-path; no abort delta on seed=42 ticks=4) |
| `baltic-patrol-combat-domains` (isolated golden) | `true` | `BalticCombatDomainsPolicyTests` + `replay-golden-baltic-combat-domains-2026-06-18.txt` | **Unchanged** — isolated pin; hash matches production engage golden |
| `combat-domains-smoke` | `true` | `CombatDomainsSmokePolicyTests` — `WORLD_HASH=17144800277401907079` (unchanged) | **Unchanged** — separate minimal smoke; do not conflate with Baltic catalog pins |

Producer approval: `production/agentic/sprint-30-baltic-flag-flip-2026-06-18.md` (APPROVED 2026-06-18).

| `baltic-patrol-mine-transit-hazard` | Weapons free | Weapons tight | `engage.combatDomainsEnabled=true` + `mineHazard` zone/transit | **Test-only** S32-08 mine transit hazard hot-tick (`BalticReplayHarnessMineTransitHazardTests`; not ReplayGolden 6/6) |

Pre-merge guard: all other `baltic-patrol-*` variants keep default `combatDomainsEnabled=false` unless explicitly documented.

Override per unit via `unitOverrides` in JSON.

## S42-04 Content wave 1 — Scenario/data maintenance (B1)
**Citations (mandatory per task):** 
- `production/release-enablement-scope-boundary-2026-06-20.md` (B1: "S42 scenario/data: Baltic policy JSON maintenance only; no production hash change without golden ADR.")
- `production/gate-checks/scope-expansion-decision-2026-06-20-S41-close.md` (S41 CLOSEOUT PASS — HUMAN ACK RECEIVED; user: "i provide the ack"; "S42 dispatch UNBLOCKED")
- `production/sprints/sprint-42-release-kickoff-content-art-bible-w1.md` (S42-04: scenario packages, policy JSON (replay-gated); AC: Replay 6/6; golden updates ADR if needed)
- `production/agentic/sprint-42-parallel-kickoff-2026-06-20.md` + `production/agentic/s39-s48-worktree-manifest.md` (track: `stack/sprint42/content-scenario`, owner team-simulation)
- S41 closeout packet + prior determinism/replay: `production/determinism/replay-*.md`, `production/qa/smoke-sprint-41-closeout-2026-06-20.md`, `production/qa/qa-plan-sprint-42-2026-06-20.md`

**Scope:** Policy JSON maintenance only for committed B1 rows (e.g. mission-roe for Req13 doctrine, magazine for Req16/21 logistics/platform, replay/comms/classify etc for Req02/17). Scenario packages referenced via B1 (no new packages; maint on existing). Worktree: stack/sprint42/content-scenario . **impact() performed on BalticReplayHarness (CRITICAL 52), ScenarioPolicyProfile (CRITICAL 249).** No changes to production golden fixtures affecting `17144800277401907079`. Replay-gated verification required.

Initial maint performed in worktree (bootstrap complete). All policy JSONs under data/scenarios/ owned by S42-04. csharpexpert deterministic: policy load via ScenarioPolicyRepository/JsonLoader ensures SeededRng + no side effects; order-independent for replay. 

**Replay gate (post-maint):** 6/6 must PASS (baltic-patrol, baltic-patrol-comms, baltic-patrol-classify, baltic-patrol-stale, baltic-patrol-spoof, baltic-patrol-readiness per ReplayGoldenRegressionCatalog).

## S43-04 Content remainder — Platform/scenario data maint (B1 wave 2 close)
**Citations (mandatory):** 
- `production/release-enablement-scope-boundary-2026-06-20.md` (B1 wave 2: Req **03** Simulation Modes / **04** Agent Delegation / **14** Engagement & Fire Control / **15** Sensor Detection & EW / **17** Replay AAR & Order Log / **18** Combat Domains / **19** Cyber & Comms; platform/scenario data maint; S42 closeout handoff; B1 exit at S43 closeout)
- `production/gate-checks/scope-expansion-decision-2026-06-20-S41-close.md` (S41 CLOSEOUT PASS + HUMAN ACK "i provide the ack" 2026-06-20; S42 dispatch UNBLOCKED; S42 COMPLETE per smoke-sprint-42-closeout-2026-06-20.md)
- `production/sprints/sprint-43-content-wave2-art-bible-complete.md` (S43-04: Content Platform/scenario remainder — close B1; team-data 2.5d)
- `production/agentic/sprint-43-parallel-kickoff-2026-06-20.md` + worktree manifest (stack/sprint43/content-remainder; team-data + csharpexpert)
- S42 precedent: `production/qa/smoke-sprint-42-closeout-2026-06-20.md` (S42-04 replay-gated maint; 6/6; cites S41 ack + boundary; wave1 policy maint)
- `production/qa/qa-plan-sprint-42-2026-06-20.md` + gate-matrix-track-b

**GitNexus impact first (done pre-maint):** CatalogWriteGate CRITICAL (extend-only enforced); CatalogPlatformBrowseProjection / PlatformCatalogListProjection / CatalogPlatformBrowseRow LOW (projection data side safe); Scenario* LOW; SimulationSession (data paths) noted CRITICAL upstream — **zero DelegationBridge touch**; deterministic read-models only.

**Exact maint (deterministic projection/data side; no prod hash change; extend-only catalog data):**
- scenario-policy-ids.md S43-04 section (this)
- Policy JSON maint: doc-only `s43Wave2Maint` fields added to select baltic-patrol-* for wave2 req coverage (e.g. modes/delegation/engage/sensor/combat/cyber hints; non-impacting replay)
- Projection extensions: added GetSimulationModeFlags, GetDelegationTrustData, GetEngageDlzData, GetEccmCatalogFlags, GetBdaLifecycleSummary, GetCombatDomainProjection, GetCyberCommsData (IReadOnlyList/Dict, ordinal, pure; cite boundary + S41 ack + S42 closeout)
- Worktree: stack/sprint43/content-remainder (Beta-Content-Remainder.md manifest); data copied + updated here + main data/scenarios synced post-verify
- No changes to production golden baltic-patrol.policy.json or hash 17144800277401907079

**csharpexpert + team-data determinism:** All maint via deterministic accessors (GetSorted*, OrderBy StringComparer.Ordinal, IReadOnly* returns, no DateTime/wall, no unordered dict iter). Matches S41 audit (0 issues) + S42. Projection-side only for Req 03/04/14-19 data surfacing. Extend-only on catalog JSONs if any platform data appended.

**Replay verification (replay-gated; S43-04 AC):** 6/6 must hold pre/post (see verification report). 

**AC status (S43-04):** Platform/scenario data maint complete for remaining B1 committed rows; projections deterministic; tracker note for wave2 Partial+ data side; B1 close candidate at S43 closeout.

*Independent of Engage track (S43-03); parallel from Evidence. All artifacts cite S41 ack + S42 closeout + boundary.*

