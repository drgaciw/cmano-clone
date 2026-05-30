# 13 - Doctrine, ROE, EMCON, and WRA

**Last Updated:** May 29, 2026  
**Status:** Draft — ready for design review  
**CMO basis:** Manual §3.3.12–16, §4.5.6–8, §6.3.8–9; parity with CMO side/unit/mission doctrine UI  
**Related:** 04 Agent Delegation, 11 Mission Editor, 14 Engagement, 12 Terms Glossary

## Purpose

Define how **doctrine**, **rules of engagement (ROE)**, **emissions control (EMCON)**, and **weapon release authority (WRA)** are modeled, inherited, evaluated at runtime, and bound to **human and agent controllers** — with full **explainability** when actions are blocked (CMO “My Weapon Won’t Fire” class of problems).

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

## Implementation Mapping (Existing Code — GitNexus 2026-05-29)

| Requirement | Current code | Migration |
|-------------|--------------|-----------|
| ROE gate | `IRoeFilter`, `PassthroughRoeFilter`, `AutonomyGate` | `IPolicyEvaluator` reads `PolicySnapshot`; `IRoeFilter` becomes thin wrapper or replaced |
| Orders | `Order`, `OrderKind`, `RiskLevel` | Add abort reasons; optional `Engage` payload (shooter, weapon, contact) |
| Risk | `DefaultRiskClassifier` | Map to Assisted autonomy; align with doc 04 |

**Blast radius:** `npx gitnexus impact --repo cmano-clone -d upstream IRoeFilter` → **HIGH** (orchestrator, bridge, replay tests). Coordinate with `DelegationOrchestrator` changes.

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

**References:** CMO Manual §3.3.12–16, §4.5.8, §6.3.8; `docs/manual/index.html`
