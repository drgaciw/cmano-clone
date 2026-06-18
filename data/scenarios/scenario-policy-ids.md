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

Override per unit via `unitOverrides` in JSON.
