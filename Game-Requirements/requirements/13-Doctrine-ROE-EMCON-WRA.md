# 13 - Doctrine, ROE, EMCON, and WRA

**Last Updated:** 2026-07-18  
**Status:** Draft — ready for design review (status locked per nemo-req-improv-plan §7 Q2)  
**FR reverse-ref:** [FR-11](01-Project-Overview.md) — Doctrine, ROE, EMCON, WRA  
**CMO basis:** Manual §3.3.12–16, §4.5.6–8, §6.3.8–9; parity with CMO side/unit/mission doctrine UI  
**Related:** 04 Agent Delegation, 11 Mission Editor, 14 Engagement, 12 Terms Glossary  
**Tracker:** [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 13 — **Partial**

## Purpose

Define how **doctrine**, **rules of engagement (ROE)**, **emissions control (EMCON)**, and **weapon release authority (WRA)** are modeled, inherited, evaluated at runtime, and bound to **human and agent controllers** — with full **explainability** when actions are blocked (CMO “My Weapon Won’t Fire” class of problems).

Implements hub **[FR-11](01-Project-Overview.md)** (doctrine / ROE / EMCON / WRA).

## Vision

Policy is not a hidden modifier. It is a **contract** between the theater commander, scenario designer, and every delegated agent. Players and agents share the same rules; agents differ only in **how aggressively they use the latitude** those rules allow. Every block or release decision is auditable in the order log (doc 17).

## CMO Parity Requirements

| Capability | CMO (manual) | Aegis |
|------------|--------------|-------|
| Side-level doctrine/ROE/WRA/EMCON | §6.3.8 | **P0** |
| Unit/mission override with inheritance | §3.3.12–15, §4.5.7–8 | **P0** — visual inheritance chain |
| EMCON per sensor class | §3.3.14, §6.3.9 | **P0** |
| Withdraw / redeploy rules | §3.3.16 | **P0** |
| Posture (hostile/neutral/friendly) | ScenEdit / events | **P0** — ties to doc 11 events |
| Special actions gated by scenario | §5.5.2, §6.3.10 | **P1** |

## Policy Inheritance Model

**Resolution order** (most specific wins unless “inherit” explicitly selected):

1. Unit instance override  
2. Embarked / hosted unit (e.g., helo on ship)  
3. Mission assignment  
4. Group / formation default  
5. Side default  
6. Scenario global default  

**P0 requirements:**

- UI and MCP expose the **effective policy** after inheritance, not only local overrides.
- Overrides store **provenance**: `inherited | user | agent | event | scenario_template`.
- Changing a parent policy optionally **cascades** or **locks** children (designer choice in editor).

## Policy Snapshot (Agent Contract)

When a unit is placed under agent control (doc 04), the sim captures a **Policy Snapshot** at assignment time:

```yaml
policySnapshotId: ps-00482
seed: 9034412
sideId: NATO
unitId: DDG-51
missionId: PATROL_BALTIC_A
roe: { weaponsFree: false, holdFire: false, ... }
wra: { weaponType: SAM, maxSalvo: 2, ... }
emcon: { radar: passive, sonar: active, datalink: on, ... }
doctrine: { ignorePlottedCourse: false, withdraw: whenDamaged, ... }
posture: { RED: hostile }
capturedAtSimTime: "T+00:15:00"
```

**Unique Aegis rules:**

- Snapshot is **immutable** for the tick unless player, event, or authorized agent issues `PolicyUpdate` (logged).
- Agent **personality** does not change ROE legality — only utility weights for legal options.
- Re-assignment or autonomy change creates a **new** snapshot id; old decisions remain attributable.

## Functional Requirements

### ROE

- **P0** Standard ROE states: hold fire, weapons tight, weapons free (extensible enum).
- **P0** ROE gates: detect only, illuminate, designate, fire ballistic, fire guided, jam, etc.
- **P0** Player and agent see the same ROE gate results before commit.

### WRA

- **P0** Per-weapon-type or per-mount-class salvo limits, range bands, target categories.
- **P0** WRA evaluated **before** engagement resolution (doc 14); failure → `FireAbortReason.WRA_*`.

### EMCON

- **P0** Per-emitter states: off, passive, active (domain-specific: radar, sonar, ESM, datalink, OECM).
- **P0** EMCON affects detection (doc 15) and enemy ESM contacts.
- **P1** Mission-level EMCON schedules (e.g., “go active at prosecution boundary”).

### Doctrine (behavioral)

- **P0** Mission-level behaviors: ignore plotted course, engage opportunity targets, refuel/unrep rules, withdraw when damaged, use nuclear weapons (scenario-gated).
- **P0** Withdraw/redeploy: conditions (damage %, magazine %, fuel bingo) and destinations.

### Explainability (“Weapon Won’t Fire”)

- **P0** Every denied fire or sensor activation returns a **FireAbortReason** code + human string + policy field reference.
- **P0** Message log entry and order log entry (doc 17) for denials at **Assisted** autonomy and above.
- **P0** Hover tooltip on greyed weapon buttons lists top 3 blocking rules.
- **P1** “What if?” preview: temporarily relax one rule in editor test only (not in ranked play).

### Agent integration (doc 04)

- Agents **must not** issue orders that violate the active Policy Snapshot.
- **Assisted** mode: agent proposes illegal action → shown as blocked with reason before player confirm.
- **Swarm Coordinator** may request WRA bump via `PolicyUpdate` proposal (player/event approval in Assisted+).
- **Electronic Warfare Specialist** prioritizes legal jamming/deception under EMCON and ROE.

### Editor integration (doc 11)

- Mission Board shows doctrine/ROE/EMCON tabs with inheritance diagram.
- Validation Agent flags contradictory policies (e.g., strike mission + hold fire ROE).

### Major IDs (ROE-*)

| ID | Summary | Priority / maturity |
|----|---------|---------------------|
| **ROE-01** | Policy inheritance chain (unit → mission → group → side → scenario) | **P0** — Partial (validate + projection; full multi-level cascade UI Phase N) |
| **ROE-02** | Immutable Policy Snapshot on agent assignment / rebind | **P0** — Shipped (`PolicySnapshotRegistry`) |
| **ROE-03** | ROE states and multi-gate checks (hold / tight / free + detect/illuminate/fire/jam) | **P0** — Partial (`RoeLevel` + `PolicyEvaluator`; full multi-gate matrix Phase N) |
| **ROE-04** | WRA per weapon/mount class (max salvo, range, target category) | **P0** — Partial (`MaxSalvo` / WRA denials; full category tables Phase N) |
| **ROE-05** | EMCON per emitter class (radar/sonar/ESM/datalink/OECM) | **P0** — Partial (scenario policy + engage EMCON abort; full schedules Phase 2) |
| **ROE-06** | Doctrine behavioral rules (ignore course, opportunity engage, withdraw) | **P0** — Partial (withdraw/damage gates; full doctrine set Phase 2) |
| **ROE-07** | Explainability: `FireAbortReason` + policy field reference on deny | **P0** — Shipped (headless + order log); UI tooltips Partial |
| **ROE-08** | Editor/MCP effective-policy resolve + contradictory-policy validation | **P0** — Partial (`DoctrineInheritanceValidateTests` + projection; full Mission Board Phase N) |

## Non-Functional Requirements

| Area | Target |
|------|--------|
| Determinism | Same snapshot + world state + seed → identical policy evaluation order |
| Performance | Policy evaluation O(1) per mount check; batch cache per unit per tick |
| Scale | 5,000+ units with inherited policies without UI stall |
| Localization | All FireAbortReason strings in string tables |

## Data Model (high level)

```
PolicyTemplate (side default)
├── roe, wra[], emcon, doctrine, withdraw
PolicyOverride (unit | mission | group)
├── parentRef, fields changed, provenance
PolicySnapshot (runtime, per controller assignment)
├── frozen copy + snapshotId
FireAbortReason (enum + metadata)
```

## MCP / Agentic Tools

| Tool | Description |
|------|-------------|
| `policy_get_effective` | Resolve inheritance for unit/mission |
| `policy_set_override` | Editor or authorized agent override |
| `policy_explain_fire` | Why weapon X cannot fire at target Y now |
| `policy_snapshot_list` | Audit snapshots for unit |

## Acceptance Criteria

1. Designer sets side ROE **weapons tight**, mission override **weapons free** for one strike package only; embarked units inherit correctly.
2. Player delegates unit to **Cautious** agent; agent does not fire through **hold fire**; log shows `FireAbortReason.ROE_HOLD_FIRE`.
3. EMCON **radar passive** prevents active radar illumination; ESM still legal; explanation cites EMCON.
4. WRA **max salvo 2** blocks third missile; salvo count visible in UI.
5. Withdraw rule triggers redeploy mission suggestion when damage threshold hit (Assisted: player confirm).
6. Headless run with identical snapshot produces identical policy denial sequence in order log.

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP** | ROE, WRA, EMCON, inheritance UI, FireAbortReason, snapshot on delegate |
| **Phase 2** | Withdraw automation, EMCON schedules, special actions linkage |
| **Phase 3** | Campaign-carry-forward policy reputation / trust (doc 04 open question) |

## Implementation Mapping (headless)

| Area | Path / type | Status | Evidence |
|------|-------------|--------|----------|
| Effective policy / ROE·WRA evaluate | `PolicyEvaluator`, `IPolicyEvaluator`, `EffectivePolicy`, `FireAbortReason` (`ProjectAegis.Sim` · `Policy/`) | **Shipped (Partial gates)** | `PolicySnapshotEvaluatorTests`; hold-fire / max-salvo denials on engage path; full multi-gate ROE matrix + full WRA category tables remain **Phase N** |
| ROE adapter into delegation | `RoePolicyAdapter`, `IRoeFilter`, `AutonomyGate` (`ProjectAegis.Delegation` · `Roe/`, `Orchestration/`) | **Shipped** | `RoePolicyAdapterTests`, `AutonomyGateTests`; ROE filter before engage |
| Policy snapshot registry | `PolicySnapshotRegistry`, `PolicySnapshot` (`Delegation` · `Orchestration/`; `Sim` · `Policy/`) | **Shipped** | `PolicySnapshotRegistryTests`; immutable snapshot at controller assignment |
| Scenario policy load | `ScenarioPolicyJsonLoader`, `ScenarioPolicyRepository`, `ScenarioPolicyProfile` (`Sim` · `Scenario/`) | **Shipped** | `ScenarioPolicyJsonLoaderTests`, `ScenarioPolicyJsonRoundTripTests`; `data/scenarios/*.policy.json` incl. v3 mission-roe (`data/scenarios/baltic-v3-mission-roe-band-c.policy.json` — friendlyRoe WeaponsTight, opposingRoe WeaponsFree, EMCON radar bands per UCAV side) |
| Doctrine inheritance validate / projection | `DoctrineInheritanceValidateTests`, `DoctrineInheritanceProjection`, `DoctrineInheritancePanelBinder` | **Partial** | `src/ProjectAegis.Data.Tests/Validation/DoctrineInheritanceValidateTests.cs`; projection tests; Unity panel host present — full inheritance cascade authoring UI **Phase N** (tracker: ADR-010 doctrine panel polish) |
| Full inheritance cascade / multi-gate ROE / full WRA categories / EMCON schedules | — | **Partial / Phase N** | Scenario JSON + MVP evaluator cover Baltic/v3 slices; CMO-parity multi-level cascade UI, full ROE gate matrix, and per-category WRA tables not complete |

**Honesty note:** Design Status remains **Draft** (Template B). Shipped = headless policy evaluate + snapshot + scenario load + validation fixture; not full CMO doctrine UI parity.

## Open Questions

1. Allow **runtime ROE change** by player during execution without invalidating replay hash? (Proposed: yes, logged as `PolicyUpdate`.)
2. Minimum WRA granularity: per mount vs per weapon type vs per magazine?
3. Nuclear release: scenario flag only, or separate legal review agent?

## Traceability

| Doc | Relationship |
|-----|----------------|
| 04 | Agents bound to Policy Snapshot |
| 11 | Authoring and validation |
| 14 | Engagement checks after policy |
| 15 | EMCON ↔ detection |
| 17 | Order log records denials |
| 15–20 | Sensors, logistics, combat, cyber, UI consumers |
| `cmo-manual-traceability.md` | §3.3.12–16, §4.5.8, §6.3.8 |

---

**Implementation grade:** Partial — see [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 13.
Design Status remains **Draft** (Template B). Charter re-honesty: Wave 2 2026-07-08.

**Review note (2026-07-18):** All Implementation Mapping paths verified to exist in `src/` (9 cited test files: `PolicySnapshotEvaluatorTests`, `RoePolicyAdapterTests`, `AutonomyGateTests`, `PolicySnapshotRegistryTests`, `ScenarioPolicyJsonLoaderTests`, `ScenarioPolicyJsonRoundTripTests`, `DoctrineInheritanceValidateTests`, `DoctrineInheritanceProjectionTests`, `DoctrineInheritancePanelBinderTests`); Unity host `DoctrineInheritancePanelHost.cs` present; fixture `data/scenarios/validation/doctrine-inheritance.json` present. ADR-010 (Headless-First / Command-Driven UI) **Accepted** — doctrine inheritance is listed as a projection view model; full Unity doctrine panel authoring cascade remains **Phase N** per tracker row 13. Status stays Draft per nemo-req-improv-plan §7 Q2 (user-locked).
