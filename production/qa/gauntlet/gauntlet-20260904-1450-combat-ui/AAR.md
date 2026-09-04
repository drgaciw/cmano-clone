# Gauntlet combat-ui AAR — gauntlet-20260904-1450-combat-ui

**Team:** `/qa-gauntlet-combat-ui` (via `/team-qa-gauntlet --mode combat-ui`)
**Verdict:** BLOCKED

| Gate | Result |
|------|--------|
| Headless Engage/Kill/CombatDomains + ReplayGolden engage/kill/magazine/salvo | **22/22** passed, 0 failed (floor ≥1) |
| `CombatDomainValidator` | **15/15** passed, 0 failed (floor ≥1) |
| CombatDomains GameObject in `DelegationSmoke` | **MISSING** — `grep CombatDomains Assets/Scenes/DelegationSmoke.unity` = 0 hits |
| Slice B chrome (DRG-165–170) | **Out** — not implemented this run |
| Hash / DelegationBridge | OK — hash hits 26; `DelegationBridge.cs` diff 0 bytes |

**Blocked reason:** Slice A presentation *gates* are green, but CombatDomains is not in the smoke scene. Per `/qa-gauntlet-combat-ui` missing combat chrome → **BLOCKED**, hand to Combat UX Slice B owners (DRG-165–170 / S123-02). Do not implement Slice B here. Do not treat headless green as a silent scene PASS.

**Manual UAT:** not in scope.
**C2 Play Mode ×5:** wrong skill — `/qa-gauntlet-ui`.
**Ladder:** not run.

## Run identity

| Field | Value |
|-------|-------|
| RUN_ID | `gauntlet-20260904-1450-combat-ui` |
| RUN_DIR | `production/qa/gauntlet/gauntlet-20260904-1450-combat-ui/` |
| git SHA | `7279aecad16fc9872f0461a60fbf4f7fe856f71a` |
| branch | `sprint-123-integration` |
| captured | 2026-09-04 |
| `dotnet` | `$HOME/.dotnet` |

## Filters

UA (`combat-ui/dotnet-combat-ui.log`):

`FullyQualifiedName~CombatDomains|FullyQualifiedName~PolicyEngage|FullyQualifiedName~ReplayGoldenBalticEngage|FullyQualifiedName~ReplayGoldenBalticKill|FullyQualifiedName~ReplayGoldenBalticMagazine|FullyQualifiedName~ReplayGoldenBalticSalvo`

```
Passed!  - Failed:     0, Passed:    22, Skipped:     0, Total:    22
```

Sim (`combat-ui/dotnet-combat-domain.log`):

`FullyQualifiedName~CombatDomainValidator`

```
Passed!  - Failed:     0, Passed:    15, Skipped:     0, Total:    15
```
