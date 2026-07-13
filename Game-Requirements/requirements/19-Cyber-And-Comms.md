# 19 - Cyber Warfare and Communications

**Last Updated:** 2026-07-08  
**Status:** Draft — ready for design review  
**FR reverse-ref:** [FR-17](01-Project-Overview.md) — Cyber and comms degradation  
**CMO basis:** Manual §10.7 Comms Disruption & Cyber Attacks; links §9.2.6 EW  
**Related:** 15 Sensors (datalink), 13 Doctrine, 04 Delegation, 09 Near-Future, 11 Events  
**Tracker:** [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 19 — **Partial**  
**GDD:** [cyber-comms-degradation.md](../../design/gdd/cyber-comms-degradation.md)

## Purpose

Define **communications degradation**, **cyber effects** on C2 and datalinks, and their impact on **player UI**, **agent attention**, and **scenario events** — bridging classic EW (doc 15) and near-future cognitive EW (doc 09).

Implements hub **[FR-17](01-Project-Overview.md)** (cyber and comms degradation).

## Vision

In 2030s warfare, the network is a battlespace. Jamming is not only “worse radar” — it delays orders, desynchronizes swarms, and forces agents to act on stale tracks. Effects are **scenario-controlled**, **deterministic**, and **explainable**.

## CMO Parity Requirements

| Capability | CMO §10.7 | Aegis maturity (honest) |
|------------|-----------|-------------------------|
| Comms disruption | Described in appendix | **Partial / Shipped spine** — timeline + order delay + staleness fixtures (`CommsTimelineSimulator`, `CommsOrderDelay`) |
| Cyber attacks | Scenario/event driven | **Partial** — **`SpoofTracks` Shipped**; full action library **Phase N** |
| Integration with EW | §9.2.6 | **P0 coordination with doc 15** (jam/datalink paths); not a separate network stack |

## Comms Model

### Link types

- **P0 / Partial+** Side-wide strategic net effects via scenario `comms` transitions (orders delay, state Nominal/Degraded/Denied)
- **P0 / Partial+** Tactical datalink (track sharing — doc 15; share lag / comms share state on merger)
- **Phase N** Unit-local voice (flavor only unless degraded)
- **Phase N** Full satellite / SATCOM latency model beyond scenario lag ticks

### Link state

```yaml
linkId: NATO_TADIL_J
state: degraded  # up | degraded | down  (runtime: Nominal | Degraded | Denied)
degradation: { delayMs: 800, dropRate: 0.05, trackAgeMaxSec: 120 }
cause: { type: Jamming, sourceUnitId: RED_EW_01 }
untilSimTick: 190000
```

**Shipped (Partial+):** State changes driven by scenario policy timelines; logged as `CommsStateChange` records from `CommsTimelineSimulator.Drain`.

## Effects on Gameplay

| Effect | Player | Agent | Determinism | Maturity |
|--------|--------|-------|-------------|----------|
| Order delay | Commands queue; execute at `now+delay` | Same | Sorted queue by `(executeTick, orderId)` | **Shipped** (`CommsOrderDelay`) |
| Track staleness | Datalink contacts age faster | Agents see stale flag | **P0** | **Partial+** (stale fixtures / thresholds) |
| Partial picture | Missing subset of contacts | Configurable | **P0** | **Partial** (datalink/comms share gates) |
| Swarm desync | — | Sub-swarms pause coordination | **P1** | **Phase N** |
| Mission patch delay | Mission changes lag | **P1** | — | **Phase N** |

**P0** **Delegation blind** (doc 15) can combine with comms degradation for hard scenarios.

## Cyber Attacks

Scenario-defined **cyber actions** (events doc 11 or special actions / policy timelines):

| Action | Example effect | Maturity |
|--------|----------------|----------|
| `DegradeLink` | Tactical datalink / node degraded for duration | **Partial** via comms timeline transitions (not a free-form mid-run tool) |
| **`SpoofTracks`** | Insert / mark false contacts; engage aborts with **`CYBER_SPOOF_TRACK`** | **Shipped** — see evidence below |
| `DenyFireControl` | FC radar offline until reboot event | **Phase N** (design residual; not a full shipped action library entry) |
| `DelayOrders` | Side-wide order delay | **Partial / Shipped** via `CommsOrderDelay` + degraded display settings |

### SpoofTracks — **Shipped** (not Phase 2 residual)

**Do not** list spoof only under Phase 2. Wave 5 / Baltic fixtures already land spoof on the critical path:

| Evidence | Path / name |
|----------|-------------|
| Timeline simulator | `SpoofTrackTimelineSimulator` (`ProjectAegis.Delegation` · `Comms/`) |
| Scenario policy | `data/scenarios/baltic-patrol-spoof.policy.json` (`id: baltic-patrol-spoof`) |
| Abort log code | `CYBER_SPOOF_TRACK` (`AbortReasonCatalog.Cyber`, `data/glossary/abort_reason_manifest.json`) |
| Resolver tests | `MvpEngagementSpoofTrackTests` — resolve aborts when track spoofed |
| Timeline unit tests | `SpoofTrackTimelineSimulatorTests` |
| Replay harness | `BalticReplayHarnessSpoofTests` — fingerprint contains `CYBER_SPOOF_TRACK` |
| Golden | `tests/regression/replay-golden-baltic-spoof-2026-06-04.txt` |
| Attack menu | `DelegationBridgeAttackOptionTests` — fire disabled with `CYBER_SPOOF_TRACK` |

**P1 residual (not spoof itself):** broader cyber action library, cognitive EW profiles, player-visible vs hidden spoof UX polish.

**P0** Player briefing may list possible cyber effects in scenario (spoiler level configurable) — presentation **Partial**.

### Full cyber action library — **Phase N**

`Probe` / `Exploit` / `Disrupt` / free-form `cyber_trigger` action packs beyond scenario timelines and spoof are **Phase N**. Tracker residual: JADC2 node damage, ECCM depth (doc 15 coordination).

### Near-future (doc 09)

- **Phase N / P1** Cognitive EW: rotating jam profiles
- **Phase N / P2** AI-on-AI “cyber duels” between agent controllers (research mode)

## Agent Integration (doc 04)

- **P0 / Partial+** Agents receive `worldObservation` with `observationTimestamp` and `stale` flags where wired
- **P0 design** **Electronic Warfare Specialist** prioritizes restore comms / EMCON tradeoffs
- **Phase N / P1** **Attention budget** reduced under degraded comms (fewer units actively controlled)
- **P0 / Partial** Agent rationale may mention comms (“engaging on stale track — caution”)

## Functional Requirements — UI

- **Partial** Comms status indicator per side / task force (`CommsStateProjection` / C2 top-bar where present)
- **Partial+** Message log entries for link up/down/degraded (`CommsStateChange` category)
- **Partial** Contact list shows stale / datalink vs organic icon

### Major IDs (COM-* / CYB-*)

| ID | Summary | Priority / maturity |
|----|---------|---------------------|
| **COM-01** | Scenario-driven comms state machine (Nominal / Degraded / Denied) | **P0** — **Partial+ / Shipped spine** (`CommsTimelineSimulator`, `CommsState`) |
| **COM-02** | Deterministic order delay under degraded comms | **P0** — **Shipped** (`CommsOrderDelay`) |
| **COM-03** | Track staleness / datalink lag under degradation | **P0** — **Partial+** (stale + datalink-comms fixtures) |
| **COM-04** | Comms display / C2 projection | **P0** — **Partial** (`CommsStateProjection`, scenario `commsDisplay`) |
| **COM-05** | CLI status: `scenario_comms_status` | **P0** — **Shipped** (`ScenarioCommsStatusCommand`) |
| **CYB-01** | **SpoofTracks** timeline + engage abort **`CYBER_SPOOF_TRACK`** | **P0** — **Shipped** (`SpoofTrackTimelineSimulator`, `baltic-patrol-spoof`, harness/golden tests) |
| **CYB-02** | CLI status: `scenario_cyber_status` | **P0** — **Shipped** (`ScenarioCyberStatusCommand`) |
| **CYB-03** | Full cyber action library (`DenyFireControl`, free-form exploit packs, JADC2 node damage) | **Phase N** |
| **CYB-04** | Cognitive EW / AI-on-AI cyber duels | **Phase N** |
| **CYB-05** | Attention-budget reduction under degraded comms | **Phase N / P1** |

## MCP / CLI Tools (honest shipped surface)

Shipped Mission Editor CLI verbs (also exposed where MCP hosts CLI) — **status/read** tools, not fictional mid-sim apply tools:

| Tool | Description | Status |
|------|-------------|--------|
| `scenario_comms_status` | Link / comms display status for a scenario policy id | **Shipped** (`ScenarioCommsStatusCommand`) |
| `scenario_cyber_status` | Cyber/spoof-related status + abort code hints for a scenario policy id | **Shipped** (`ScenarioCyberStatusCommand`) |

**Not shipped as product apply tools:** inventing `comms_apply_effect` / `cyber_trigger` as live mutation surfaces. Scenario effects are **authored in policy JSON** (transitions) and applied by sim timelines — not free-form MCP apply. Any future apply verbs are **Phase N** and must stay deterministic / editor-scoped.

## Non-Functional Requirements

| Area | Target |
|------|--------|
| Determinism | Delay queues processed in fixed order; spoof activation by tick order |
| Security | No arbitrary network I/O; cyber is sim-internal |
| Scope | No real-world hacking; fictional effects only |

## Acceptance Criteria

Evidence policy: check only with named types/tests/fixtures.

1. Jamming / comms event degrades datalink; contacts age and expire per rules — **partial+** (comms + datalink fixtures; EW jam in doc 15).
2. Player order arrives after deterministic delay; order log / execute tick shows queue time — **met** (`CommsOrderDelay`, runtime tests).
3. Agent with degraded link logs stale-track warning before engage (Assisted) — **partial**.
4. Spoofed track blocks fire with `CYBER_SPOOF_TRACK` — **met** (`MvpEngagementSpoofTrackTests`, harness, golden).
5. Replay shows `CommsStateChange` at correct tick — **met** (`baltic-patrol-comms` / v2–v3 comms goldens).
6. Headless run with cyber/spoof scenario matches golden comms/spoof timeline — **met** (spoof + comms goldens).
7. Full `DenyFireControl` cyber library action — **Phase N** (not claimed shipped).

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP / Shipped spine** | Comms timeline + order delay + track staleness hooks; **SpoofTracks** + `CYBER_SPOOF_TRACK`; CLI `scenario_comms_status` / `scenario_cyber_status`; Baltic spoof/comms goldens |
| **Partial residual** | C2 comms indicator polish; agent attention under degrade; partial-picture completeness; briefing spoiler levels |
| **Phase N** | Full cyber action library; cognitive EW profiles; research cyber duels; free-form apply tools |

**Shipped (not Phase 2 residual):** `SpoofTrackTimelineSimulator`, `baltic-patrol-spoof`, `CYBER_SPOOF_TRACK`, `MvpEngagementSpoofTrackTests`, spoof replay golden/harness.  
**Also shipped spine:** `CommsTimelineSimulator`, `CommsOrderDelay` (Partial+ presentation).

## Implementation Mapping (headless)

| Area | Path / type | Status | Evidence |
|------|-------------|--------|----------|
| Comms timeline | `CommsTimelineSimulator`, `CommsState`, `CommsStateChangeRecord` (`ProjectAegis.Delegation` · `Comms/`) | **Partial+ / Shipped spine** | `CommsTimelineSimulatorTests`; policies `baltic-patrol-comms`, `baltic-v3-*-comms*`; goldens under `tests/regression/replay-golden-baltic*comms*` |
| Order delay | `CommsOrderDelay` | **Shipped** | `CommsOrderDelayTests`, `CommsOrderDelayRuntimeTests`; bridge uses delay for execute tick |
| Spoof timeline | `SpoofTrackTimelineSimulator` | **Shipped** | `SpoofTrackTimelineSimulatorTests`; `data/scenarios/baltic-patrol-spoof.policy.json` |
| Spoof engage abort | `MvpEngagementResolver` + `CYBER_SPOOF_TRACK` | **Shipped** | `MvpEngagementSpoofTrackTests`; `BalticReplayHarnessSpoofTests`; `replay-golden-baltic-spoof-2026-06-04.txt` |
| CLI status verbs | `scenario_comms_status`, `scenario_cyber_status` | **Shipped** | `ScenarioCommsStatusCommand`, `ScenarioCyberStatusCommand` (+ tests); `Program.cs` cases |
| Full cyber library / apply tools | — | **Phase N** | Not present as shipped product surface |
| Attention under degraded comms | — | **Phase N** | Design residual (doc 04) |

**Honesty note:** Design Status remains **Draft** (Template B). Tracker **Partial** is correct overall (comms + spoof spine shipped; full cyber library and C2 polish open). Spoof is **not** Phase-2-only.

## Open Questions

1. Player-visible spoof contacts or hidden until intel event? → **Open** (debug/marked spoof path exists; UX policy undecided).
2. Can player “reboot” FC network via special action? → **Phase N** (ties to unshipped `DenyFireControl` library).
3. Cross-side cyber in agent-vs-agent tournaments — symmetric rules? → **Open / research**.

## Traceability

| Doc | Relationship |
|-----|----------------|
| Hub **FR-17** ([01](01-Project-Overview.md)) | Cyber and comms degradation — this doc |
| 15 | EW and datalink; jam coordination |
| 11 | Events / special actions / policy authoring |
| 04 | Agent observation + attention residual |
| 09 | Cognitive EW Phase N |
| 14 | Engage abort on spoofed track |
| 17 | Comms / cyber log entries |
| 12 | Glossary: `CYBER_SPOOF_TRACK`, Spoof Track, `CommsStateChange` |
| `cmo-manual-traceability.md` | §10.7 |

---

**Implementation grade:** Partial — see [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 19.  
Design Status remains **Draft** (Template B). Charter re-honesty: Wave 2 2026-07-08.
