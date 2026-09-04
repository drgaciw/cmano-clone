# Gauntlet combat-ui AAR — gauntlet-20260904-1450-combat-ui

**Team:** `/qa-gauntlet-combat-ui` (via `/team-qa-gauntlet --mode combat-ui`)
**Verdict:** PASS

| Gate | Result |
|------|--------|
| Headless Engage/Kill/CombatDomains + ReplayGolden engage/kill/magazine/salvo | **22/22** passed, 0 failed (floor ≥1) |
| `CombatDomainValidator` | **15/15** passed, 0 failed (floor ≥1) |
| CombatDomains GameObject in `DelegationSmoke` | **PRESENT** — `m_Name: CombatDomains` + `CombatDomainsHotTickHost` at scene lines 669 / 686 (committed HEAD; `git diff` on the scene is empty) |
| Slice B chrome (DRG-165–170) | **Out** — not implemented this run (correct; not a Slice A gate fail) |
| Hash / DelegationBridge | OK — hash hits 26; `DelegationBridge.cs` diff 0 bytes |

An earlier pass of this AAR said the GO was missing after a workspace `grep` miss. Re-audit of `unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity` shows the GO is in HEAD. Headless floors were already green; this correction does **not** flip a red test to PASS.

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
