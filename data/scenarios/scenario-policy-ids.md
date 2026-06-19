# Scenario policy template IDs

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
