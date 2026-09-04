# 25 - C2 Nodes, Mission Packages & Mission Command

**Last Updated:** 2026-09-02  
**Status:** Draft — remediation for Audit Finding B-05 (2026-09-02)  
**FR reverse-ref:** Related to **FR-03** ([04-Agent-Delegation.md](04-Agent-Delegation.md)) and **FR-18** ([19-Cyber-And-Comms.md](19-Cyber-And-Comms.md), [20-Command-And-Control-UI.md](20-Command-And-Control-UI.md))  
**Related:** [04-Agent-Delegation.md](04-Agent-Delegation.md), [08-Agentic-Architecture.md](08-Agentic-Architecture.md), [12-Terms-Glossary.md](12-Terms-Glossary.md), [13-Doctrine-ROE-EMCON-WRA.md](13-Doctrine-ROE-EMCON-WRA.md), [17-Replay-AAR-And-Order-Log.md](17-Replay-AAR-And-Order-Log.md), [19-Cyber-And-Comms.md](19-Cyber-And-Comms.md), [20-Command-And-Control-UI.md](20-Command-And-Control-UI.md), [23-Kill-Chain-Explainability.md](23-Kill-Chain-Explainability.md), [24-Human-On-The-Loop-Authority.md](24-Human-On-The-Loop-Authority.md)  
**Linear:** Milestone **H9 / C2 Architecture** · [DRG-213](https://linear.app/drgamtd-workspace/issue/DRG-213) (Headless C2 Nodes & Mission Packages), [DRG-214](https://linear.app/drgamtd-workspace/issue/DRG-214) (Headless C2 Network Health Projection), [DRG-223](https://linear.app/drgamtd-workspace/issue/DRG-223) (Task Group Coordination & Gaps), [DRG-229](https://linear.app/drgamtd-workspace/issue/DRG-229) (Mission Command Intent & Commander Guidance)

---

## Purpose

Define the requirements, architectural boundaries, and data representations for **C2 Nodes**, **Mission Packages**, **C2 Network Health**, **Task Group Coordination**, and **Mission Command Intent**.

This document captures the functional and non-functional requirements for how Project Aegis structures multi-platform tactical capability packages, evaluates network health and partitioned mesh degradation, assesses task group capability gaps, and executes intent-driven mission command across degraded comms environments.

---

## Vision

Modern naval and joint combat operations rely on composite force packages combining distributed C2 nodes, sensor platforms, relays, and weapon shooters. Rather than viewing task forces simply as flat unit collections or rigid group hierarchies, command and control in Project Aegis recognizes functional mission packages and dynamic C2 network topology.

When communications degrade, nodes are destroyed, or units detach, the simulation projects exact package availability and network health states in a deterministic, replay-stable manner. Command intent and task group coordination rules ensure autonomous agents understand commander priorities, detect critical package gaps (e.g., losing the sole datalink relay or targeting radar), and execute mission command within designated boundaries even when isolated from higher headquarters.

---

## Requirement Index

| ID | Requirement | Priority | Implementation Status | Shipped Code / Tests Reference |
|---|---|---|---|---|
| **C2N-01** | C2 Nodes & Mission Package Composition | P0 | **Shipped** (DRG-213) | `ProjectAegis.Delegation.C2Nodes` (`MissionPackageProjection`, `PackageDefinition`, `C2NodeElement`, `C2NodeRole`, `C2NodeAvailability`, `C2NodeMembershipKind`), `MissionPackageProjectionTests` |
| **C2N-02** | C2 Network Health & Mesh Partition Assessment | P0 | **Shipped** (DRG-214) | `ProjectAegis.Delegation.C2Network` (`C2NetworkHealthProjector`, `C2NetworkHealthSnapshot`, `C2NetworkHealthLevel`, `C2NetworkHealthFingerprint`), `C2NetworkHealthProjectorTests`, `C2NetworkHealthFingerprintTests` |
| **C2N-03** | Task Group Coordination & Capability Gap Detection | P1 | **Shipped** (DRG-223) | `ProjectAegis.Delegation.TaskGroupCoord` (`TaskGroupCoordProjection`, `TaskGroupCoordSnapshot`, `TaskGroupCoordInput`, `TaskGroupCoordGapCode`, `TaskGroupCoordKind`), `TaskGroupCoordProjectionTests` |
| **C2N-04** | Mission Command Intent & Degraded Operations | P1 | **Shipped** (DRG-229) | `ProjectAegis.Delegation.MissionIntent` (`MissionIntentProjection`, `MissionIntentSnapshot`, `MissionIntentInput`, `MissionIntentCode`, `MissionIntentConstraintCode`, `MissionIntentRetaskAdvice`, `MissionIntentKind`), `MissionIntentProjectionTests` |

---

## Detailed Requirements

### C2N-01 — C2 Nodes & Mission Package Composition **[P0]**

**Requirement.** The system shall support authored multi-platform **Mission Packages** (`PackageDefinition`) composed of distinct **C2 Node Elements** (`PackageElementDefinition`, `C2NodeElement`), and project their live availability into a replay-stable, read-only snapshot (`MissionPackageSnapshot`) via `MissionPackageProjection`.

1. **Node Roles (`C2NodeRole`):**
   - `C2` — Command and control node with decision/coordination authority.
   - `Sensor` — Organic or offboard detection/tracking sensor node.
   - `Shooter` — Kinetic or non-kinetic effector node.
   - `Relay` — Communications/datalink bridging node.
2. **Membership Kinds (`C2NodeMembershipKind`):**
   - `Organic` — Integral capability intrinsic to the platform host (scoped by capability name e.g., `organic-radar`, `organic-c2`).
   - `Package` — Capability contributed to or dependent on external package integration (e.g., `package-track-feed`, `package-engage`, `package-relay`).
3. **Node Availability States (`C2NodeAvailability`):**
   - `Available` — Platform is alive, functional, and connected within operational thresholds.
   - `Degraded` — Node capability impaired by platform damage or partial communications degradation.
   - `LastKnown` — Node state retained from prior updates when communications partition or line-of-sight is lost, but platform is not confirmed destroyed.
   - `Unavailable` — Platform is authoritatively destroyed, out of service, or out of network reach.
4. **Projection Invariants & Fold Rules:**
   - Projections are pure functions executed outside the tick hotpath: `MissionPackageProjection.Project(definitions, log, isPlatformAlive, currentSimTick, currentSimTime, activePackageId)`.
   - Folds platform alive state (`isPlatformAlive`), one-way platform destruction/damage changes (`PlatformDamageChangeRecord`), task-org detach/rejoin status, and comms state transitions (`CommsStateSnapshot`).
   - Platform death marks element `Unavailable` and records `CorrelationSequenceId` without dropping platform membership from the package definition.
   - Replay stability: `MissionPackageProjection.ComputeFingerprint` provides deterministic hashing (`pkg:empty` or SHA-256 digest of canonically ordered packages and elements).

**Acceptance Tests:**
- `ProjectAegis.Delegation.Tests.C2Nodes.MissionPackageProjectionTests`:
  - `Empty_definitions_yield_empty_snapshot`
  - `Composed_package_lists_all_roles_with_membership_and_availability`
  - `Unavailable_node_marks_shooter_without_dropping_package_membership`
  - `Authoritative_dead_platform_stays_unavailable_when_damage_row_shows_partial_hp`
  - `Comms_denied_marks_relay_and_c2_elements_unavailable`
  - `Comms_degraded_marks_relay_and_c2_elements_last_known`
  - `Organic_capabilities_stay_available_when_comms_are_denied`
  - `Deterministic_fingerprint_matches_regardless_of_definition_ordering`
  - `Deterministic_fingerprint_is_stable_across_identical_projections`

---

### C2N-02 — C2 Network Health & Mesh Partition Assessment **[P0]**

**Requirement.** The system shall project the real-time health, connectivity, and partition topology of the friendly C2 network via `C2NetworkHealthProjector`, generating a deterministic `C2NetworkHealthSnapshot` and fingerprint.

1. **Network Health Levels (`C2NetworkHealthLevel`):**
   - `Healthy` — All participating nodes have active, full-bandwidth datalink connectivity with unpartitioned graph topology.
   - `Degraded` — Partial throughput reduction, elevated latency, or localized link degradation without total graph partitioning.
   - `Partitioned` — Network graph is split into two or more disjoint subnets, or units are isolated from the primary C2 mesh.
2. **Topology & Mesh Evaluation:**
   - Evaluates friendly unit pairs using `DatalinkUnitPairFeed.BuildMesh` and `CatalogLinkEntry`.
   - Folds global comms state (`CommsStateSnapshot`), observer contact tracking feeds (`ProjectContactsByObserver`), and per-link status overrides (`LinkStatusOverride`).
   - Identifies `PartitionedUnits`, `LinkRows` (with endpoint statuses, latency, bandwidth, and lost-path metrics), `LastKnownContributors` (units sharing track custody across degraded links), and `LostPaths`.
3. **Replay Fingerprint:**
   - `C2NetworkHealthFingerprint.Compute(snapshot)` produces canonical hash strings (`C2NetworkHealth|...`) encoding network level, comms state, node ID, link statuses, and contributor sets for golden validation.

**Acceptance Tests:**
- `ProjectAegis.Delegation.Tests.C2Network.C2NetworkHealthProjectorTests`:
  - `Fully_connected_mesh_projects_healthy_state`
  - `Single_link_failure_partitions_isolated_node`
  - `Comms_degraded_state_yields_degraded_network_health`
  - `Comms_denied_state_yields_partitioned_network_health`
  - `Override_link_status_forces_partition_or_recovery`
- `ProjectAegis.Delegation.Tests.C2Network.C2NetworkHealthFingerprintTests`:
  - `Fingerprint_is_identical_for_equivalent_network_snapshots`
  - `Fingerprint_diverges_when_mesh_partitions`

---

### C2N-03 — Task Group Coordination & Capability Gap Detection **[P1]**

**Requirement.** The system shall evaluate task group functional completeness, detect missing or degraded critical capabilities, and project an advisory coordination snapshot (`TaskGroupCoordSnapshot`) via `TaskGroupCoordProjection` (DRG-223).

1. **Gap Codes (`TaskGroupCoordGapCode`) & Precedence:**
   - Evaluated by priority order (most specific wins):
     - `Split` — Formation fragmented or member detached from the task group (`input.IsSplit == true`).
     - `NoC2` — No command node / active C2 link present for the group (`input.HasC2 == false`).
     - `Unassigned` — No mission package assigned to the group (`string.IsNullOrWhiteSpace(input.PackageId)`).
     - `None` — Members present, package assigned, C2 active, and not split (`COORD OK`).
2. **Projection Invariants & Advisory Boundary:**
   - `TaskGroupCoordProjection.Project(TaskGroupCoordInput)` is strictly advisory (`TaskGroupCoordKind.AdvisoryCoordination`).
   - Projection facts are read-only views consumed by UI/agents: `IsWeaponsReleaseAuthorization = false`, `IsFireOrder = false`, `IsAutomaticEngagement = false`.
   - Replay stability: `TaskGroupCoordProjection.ComputeFingerprint` produces deterministic strings (`tgc:empty` or `tgc:<GroupId>|<Members>|<PackageId>|<PackageLabel>|<GapCode>|<Kind>|<Flags>|<StatusLine>`).

**Acceptance Tests:**
- `ProjectAegis.Delegation.Tests.TaskGroupCoord.TaskGroupCoordProjectionTests`:
  - `Complete_group_reports_gap_none_with_stable_fingerprint`
  - `Split_group_reports_split_gap_even_when_other_facts_missing`
  - `Missing_c2_reports_no_c2_gap_when_not_split`
  - `Unassigned_package_reports_unassigned_gap_when_c2_present`
  - `Empty_input_returns_empty_snapshot`

---

### C2N-04 — Mission Command Intent & Degraded Operations **[P1]**

**Requirement.** Autonomous agents and C2 systems shall support **Mission Command** principles, projecting commander guidance, intent codes, constraint sets, and advisory retask recommendations (`MissionIntentSnapshot`) via `MissionIntentProjection` (DRG-229).

1. **Intent Codes (`MissionIntentCode`):**
   - `Hold` — Maintain current station / defensive posture.
   - `Attack` — Execute offensive strike against designated target or operational area.
   - `Withdraw` — Disengage and retrograde along designated egress route.
2. **Active Constraints (`MissionIntentConstraintCode`):**
   - Constraints restrict tactical actions within the intent scope:
     - `Hold` — Geographic or positional constraint.
     - `NoStrike` — Offensive weapon release prohibited.
     - `RoeWithhold` — Restrictive ROE withholding weapons release.
3. **Advisory Retask Recommendations (`MissionIntentRetaskAdvice`):**
   - `None` — Maintain existing tasking.
   - `Retask` — Advisory recommendation to assign alternative package or task.
   - `Withdraw` — Advisory recommendation to disengage and retrograde.
4. **Projection Invariants & Advisory Boundary:**
   - `MissionIntentProjection.Project(MissionIntentInput)` produces read-only advisory intent (`MissionIntentKind.AdvisoryIntent`).
   - Does not directly issue orders or release weapons: `IsOrder = false`, `IsWeaponsReleaseAuthorization = false`, `IsFireOrder = false`, `IsAutomaticEngagement = false`.
   - Replay stability: `MissionIntentProjection.ComputeFingerprint` computes deterministic fingerprint strings (`mi:empty` or `mi:<Scope>|<Intent>|<Constraints>|<Retask>|<Flags>|<StatusLine>`).

**Acceptance Tests:**
- `ProjectAegis.Delegation.Tests.MissionIntent.MissionIntentProjectionTests`:
  - `Complete_hold_intent_reports_stable_fingerprint`
  - `Positive_attack_intent_on_unit_reports_stable_fingerprint`
  - `Constrained_withdraw_reports_constraints_and_advisory_retask_without_order`
  - `Empty_input_returns_empty_snapshot`

---

## Non-Functional Requirements

- **Replay Determinism:** All C2 node and network projections are strictly deterministic functions of scenario definition and decision logs, producing bit-exact fingerprints for replay verification.
- **Headless Performance:** Projection calculations must execute in under 1 ms for standard task groups (≤ 32 units) to support high-rate UI refreshing and fast-forward simulation runs.
- **Zero Bridge Hotpath Intrusion:** C2 node and network projections are read-only views computed on demand or during presentation passes, introducing zero allocations or mutation into `DelegationBridge.Tick()`.

---

## Architectural & Data Model Summary

```
PackageDefinition (Authored)
  ├── PackageId, Label
  └── PackageElementDefinition[]
        ├── ElementId, PlatformUnitId, Role (C2 | Sensor | Shooter | Relay)
        └── CapabilityScope (organic-* | package-*)
                 │
                 ▼  MissionPackageProjection.Project()
MissionPackageSnapshot (Projected View)
  ├── ActivePackageId
  ├── Elements: C2NodeElement[]
  │     ├── Role, Availability (Available | Degraded | LastKnown | Unavailable)
  │     ├── Membership (PackageId, PackageLabel, Kind: Organic | Package)
  │     ├── TaskOrgDetached, CorrelationSequenceId
  │     └── LastSimTick, LastSimTime
  └── Packages: MissionPackageMembership[]
        ├── PackageId, Label, ElementIds[], UnitIds[]

C2NetworkHealthProjector.Project()
  ├── Inputs: DecisionLog, FriendlyUnitIds, CatalogLinks, Overrides
  └── Outputs: C2NetworkHealthSnapshot
        ├── NetworkHealth (Healthy | Degraded | Partitioned)
        ├── CommsState, NodeId
        ├── LinkRows: C2NetworkLinkHealthEntry[]
        ├── LastKnownContributors: C2NetworkContributor[]
        └── LostPaths: C2NetworkLostPath[]
```
