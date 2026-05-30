# Scenario policy template IDs

Used by `SimulationModeConfigurator.Apply(..., scenarioPolicyId: "...")` and `ScenarioPolicyCatalog`.

| ID | Friendly ROE | Opposing ROE | Use |
|----|----------------|--------------|-----|
| `baltic-patrol` | Weapons free | Weapons free | Default training |
| `baltic-patrol-opp-hold-fire` | Weapons free | Hold fire | Escalation drill |
| `restricted-engagement` | Weapons tight | Weapons tight | Restricted fires |

JSON files: `data/scenarios/*.policy.json` loaded by `ScenarioPolicyRepository` (overrides built-in catalog when ids match).

Override per unit via `unitOverrides` in JSON.
