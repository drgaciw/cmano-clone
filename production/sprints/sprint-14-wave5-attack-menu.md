# Sprint 14 — Wave 5: interactive attack menu (req 14, 20)

**Dates:** 2026-07-16 → 2026-07-30  
**Branch:** `stack/wave5-attack-menu`  
**Goal:** CMO §4.1.1 attack-options menu — headless option list already exists; add player-selectable Unity flow wired to single engage resolver.

**Superpowers plan:** [2026-06-04-requirements-wave5-implementation.md](../../docs/superpowers/plans/2026-06-04-requirements-wave5-implementation.md) (Tasks 3–4)

## GitNexus pre-flight

| Symbol | Risk | Notes |
|--------|------|-------|
| `EngageAttackOptions` | LOW | Safe to extend menu entries |
| `EngageAttackOrderResolver` | LOW–MED | Verify impact before signature change |
| `UnitDetailProjection` | LOW | Attack-options line exists |
| `DelegationBridge` | CRITICAL | UI commit path only — avoid tick-loop edits |

## Must have

| ID | Story | Owner | Est. | Acceptance |
|----|-------|-------|------|------------|
| S14-01 | [wave5-005](../epics/wave5-engage-cyber-logistics-slice/story-005-attack-menu-projection.md) | c-sharp-engineer | 2 | Tests for salvo/mount/abort-disabled options |
| S14-02 | [wave5-006](../epics/wave5-engage-cyber-logistics-slice/story-006-attack-menu-ui.md) | team-ui | 2.5 | UI Toolkit dropdown/popup; issues `PlayerEngage` |
| S14-03 | [wave5-007](../epics/wave5-engage-cyber-logistics-slice/story-007-attack-menu-smoke.md) | c-sharp-test-engineer | 1 | PlayMode or binder test selects option → log row |
| S14-04 | Block engage when spoof/readiness abort (regression) | team-simulation | 1 | Combined scenario policy test |
| S14-05 | Update tracker rows 14 + 20 | Producer | 0.25 | Evidence links |

## Should have

| ID | Task | Est. | Acceptance |
|----|------|------|------------|
| S14-06 | ADR stub or amend ADR-010 for interactive attack UX | c-sharp-architect | 1 | Doc in `docs/architecture/` |
| S14-07 | UX spec delta for attack menu (doc 20) | ux-designer | 1 | `ux-spec` section or amend C2 spec |

## Verification

```bash
dotnet test ProjectAegis.sln -v minimal --filter "EngageAttack"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
```

## Gate

- `/code-review` on bridge + UI binder diff  
- Unity manual sign-off adds attack-menu check (extend `c2-manual-signoff` template)