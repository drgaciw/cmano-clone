# 23 - Kill-Chain Explainability & Targetability

**Last Updated:** 2026-09-02  
**Status:** Draft — ready for design review (Audit remediation W3-KCX)  
**Requirement IDs:** KCX-01 … KCX-07  
**FR reverse-ref:** [FR-12](01-Project-Overview.md) — Engagement and fire control, [FR-13](01-Project-Overview.md) — Sensors, detection, EW  
**CMO basis:** Manual §3.3.1–2, §3.3.9–10, §9.1–2, §9.2.8 (Weapon won't fire diagnostics)  
**Related:** 04 Delegation, 13 Doctrine/ROE/WRA, 14 Engagement, 15 Sensors, 17 Order Log, 19 Comms/Cyber, 20 C2 UI  
**Audit Findings Closed:** B-02, B-04 (KCX-02/06), B-08 (KCX-07)  
**Shipped Slices Reconciled:** DRG-179, DRG-206, DRG-207, DRG-212, DRG-219, DRG-222, DRG-225, DRG-226

---

## Purpose

Specify the deterministic **kill-chain explainability**, **targetability composition**, **sensor-to-shooter traceability**, **track custody**, and **contact provenance/identity** pipelines.

This requirement defines the formal answers to operator and autonomous agent diagnostics: *"Why can't this platform fire on this track?"*, *"What broke the kill chain?"*, and *"What is the provenance and custody state of this contact?"*

All projections must be **headless**, **sim-clock driven**, **deterministic**, and **replay-stable**, deriving truth exclusively from simulation state and order logs without UI-derived state or wall-clock dependencies (ADR-010).

---

## Vision & Architecture

Kill-chain progression transforms raw sensor detections into actionable weapons engagements. In high-tempo naval/air combat, failures occur at multiple discrete stages: sensor loss, loss of track custody, fire-control unavailability, or shooter ineligibility (out-of-envelope, no ammo, ROE hold).

Rather than reporting opaque fire failures, Project Aegis computes an explicit, inspectable 4-link chain for every contact:

```
[ Sensor ] ───► [ Track Custody ] ───► [ Targetability (FC) ] ───► [ Eligible Shooter ]
    │                   │                          │                         │
    ▼                   ▼                          ▼                         ▼
LostSensor          StaleTrack                NoFireControl              NoEligibleShooter
                  DegradedTrack              (or StaleTrack)            (DLZ / Ammo / ROE)
```

Simultaneously, contact provenance captures origin observer identity, classification confidence, comms degradation, and catalog matching so that all tactical recommendations are fully explainable.

---

## Requirements Register (KCX-01 … KCX-07)

### KCX-01: Four-Link Sensor-to-Shooter Chain Decomposition

**Priority:** P0 (Shipped headless — `SensorToShooterProjection`)  
**Finding:** B-02  

The system shall project an inspectable 4-link chain for each active contact, composed of:
1. `Sensor`: Observer platform active detection state (`SensorToShooterLinkKind.Sensor`).
2. `Track`: Continuous track custody and freshness (`SensorToShooterLinkKind.Track`).
3. `Targetability`: Fire-control solution and tracking quality (`SensorToShooterLinkKind.Targetability`).
4. `EligibleShooter`: Availability of a capable platform with ammunition and firing envelope (`SensorToShooterLinkKind.EligibleShooter`).

- The chain shall identify the `PrimaryBreakCause` as the first broken link in sequence.
- If all links are verified, the chain is marked `IsComplete = true` with `PrimaryBreakCause = None`.
- **Targetability Composition Integration:** Targetability acceptability across authority, provenance, and sensor-to-shooter chains is aggregated via `TargetabilityAcceptProjection`, which evaluates whether a target is `Permitted` or `Withheld` (e.g. `TargetabilityAcceptCauseCodes.MissingProvenance`, `CatalogMiss`, `Stale`, `SilentComms`, `ApprovalRequired`).
- **Code Reference:** `ProjectAegis.Delegation.SensorToShooter.SensorToShooterProjection`, `SensorToShooterChain`, `SensorToShooterChainLink`, `SensorToShooterLinkKind`, `ProjectAegis.Delegation.TargetabilityAccept.TargetabilityAcceptProjection`.
- **Test Evidence:** `src/ProjectAegis.Delegation.Tests/SensorToShooter/SensorToShooterProjectionTests.cs` (`Complete_chain_links_sensor_track_targetability_and_eligible_shooter`), `src/ProjectAegis.Delegation.Tests/TargetabilityAccept/TargetabilityAcceptProjectionTests.cs`.

---

### KCX-02: Deterministic Contact Provenance & Source Attribution

**Priority:** P0 (Shipped headless — `ContactProvenanceProjection`)  
**Finding:** B-04  

Every active contact picture entry shall project a deterministic provenance record (`ContactProvenanceState`):
- `Source`: Originating `ObserverId`, target unit identity `TargetId`, and formatted correlation ref `SourceRef` (`observer:{id}|target:{id}`).
- `Confidence`: Discrete confidence derived from sensor lifecycle state:
  - `High`: Contact is `Identified`.
  - `Medium`: Contact is `Classified` or BDA-degraded (`DegradedL1`, `DegradedL2`).
  - `Low`: Contact is `Detected`.
  - `Unknown`: Unrecognized state.
- `LastKnown`: Snapshot of `LifecycleState`, `TargetId`, `LastSimTick`, and `LastSimTime`.
- `QualityState`: Combinable bitflags indicating data degradation (`ContactProvenanceQualityState`):
  - `CatalogMiss`: Target platform identifier cannot be resolved in catalog or ORBAT mapping.
  - `Stale`: Contact age exceeds effective staleness threshold.
  - `SilentComms`: Local or link communications are degraded or denied.
- `OutOfCommsUnknown`: Set to `true` when link state is `CommsState.Denied`.
- **Code Reference:** `ProjectAegis.Delegation.Projection.ContactProvenanceProjection`, `ContactProvenanceState`, `ContactProvenanceQualityState`, `ContactProvenanceConfidence`.
- **Test Evidence:** `src/ProjectAegis.Delegation.Tests/Projection/ContactProvenanceProjectionTests.cs` (`Fresh_track_publishes_source_confidence_and_last_known`, `Catalog_miss_is_named_when_target_not_in_reader`, `Denied_comms_marks_out_of_comms_unknown_and_silent_comms`).

---

### KCX-03: Track Custody, Staleness & Drop Lifecycles

**Priority:** P0 (Shipped headless — `KillChainContactStateProjection`)  
**Finding:** B-02  

Track continuity and custody shall be tracked deterministically via simulation ticks:
- `DefaultStaleThresholdTicks`: Configured threshold (default: 30 ticks) beyond which lack of sensor updates transitions track to `Stale` (`KillChainLossKind.Stale`), breaking downstream targetability.
- `DefaultDropThresholdTicks`: Configured threshold (default: 120 ticks) beyond which stale tracks are transitioned to `Lost` (`KillChainLossKind.Lost`) and dropped from active fire-control custody.
- `CommsTrackStaleness`: Under degraded or denied communications, staleness threshold divisors accelerate local track staleness per scenario comms display settings.
- Custody transitions shall emit explicit, ordered transition events (`KillChainContactTransition`) with sequence IDs and timestamps.
- **Track Custody Ledger:** Detailed custody state (`TrackCustodyState.Held` vs `Dropped`) and drop causes (`TrackCustodyCause.LostSensor`, `Stale`, `CommsDenied`, `ExplicitDrop`, `Unknown`) shall be projected deterministically via `TrackCustodyProjection` into a replay-stable custody snapshot and transition ledger (`TrackCustodyLedgerEntry`).
- **Code Reference:** `ProjectAegis.Delegation.Projection.KillChainContactStateProjection`, `KillChainLossKind`, `CommsTrackStaleness`, `ProjectAegis.Delegation.TrackCustody.TrackCustodyProjection`, `TrackCustodyState`, `TrackCustodyCause`.
- **Test Evidence:** `src/ProjectAegis.Delegation.Tests/Projection/KillChainContactStateProjectionTests.cs` (`Stale_threshold_marks_track_stale_and_drops_targetable`, `Drop_threshold_marks_track_lost`), `SensorToShooterProjectionTests.cs` (`Stale_track_names_stale_cause_and_breaks_downstream_links`), `src/ProjectAegis.Delegation.Tests/TrackCustody/TrackCustodyProjectionTests.cs`.

---

### KCX-04: Targetability Composition & Fire-Control Verification

**Priority:** P0 (Shipped headless — `KillChainContactStateProjection` & `SensorToShooterProjection`)  
**Finding:** B-02  

Targetability represents the tactical state where a contact is sufficiently resolved and tracked by fire-control sensors to support weapons release:
- **Prerequisites for Targetability:**
  1. `LocationSufficient = true` (contact classified/identified or fire-control track active).
  2. `TrackContinuous = true` (unbroken custody, not stale or lost).
  3. `HasFireControlTrack = true` (fire-control radar/director assigned via `IKillChainFireControlSource`).
  4. `Loss == KillChainLossKind.None` (no unrecovered sensor loss or battle-damage degradation).
- **BDA Interaction:** Battle damage assessment degradation (`DegradedL1`, `DegradedL2`) maintains track custody in `Phase = Track` but drops `Phase = Target`, breaking the targetability link with `SensorToShooterBreakCause.DegradedTrack`.
- **Code Reference:** `KillChainContactStateProjection.RefreshCapabilities`, `SensorToShooterProjection.BuildTargetabilityLink`.
- **Test Evidence:** `src/ProjectAegis.Delegation.Tests/Projection/KillChainContactStateProjectionTests.cs` (`Identified_with_fire_control_publishes_Target`, `Bda_degradation_drops_Targetable_while_keeping_TrackContinuous`), `SensorToShooterProjectionTests.cs` (`No_fire_control_names_no_fc_on_targetability_link`, `Bda_degradation_keeps_track_linked_and_breaks_targetability_with_degraded_track`).

---

### KCX-05: Inspectable Break Causes & Diagnostics Taxonomy

**Priority:** P0 (Shipped headless — `SensorToShooterBreakCause`, `SensorToShooterBreakCauseLabels`)  
**Finding:** B-02  

When a sensor-to-shooter chain cannot be formed, the system shall provide unambiguous, standardized break cause codes and user-legible labels:

| Break Cause (`SensorToShooterBreakCause`) | Standard Display Label | Root Cause Condition |
|---|---|---|
| `None` (0) | `""` | Complete chain; ready for weapons engagement. |
| `LostSensor` (1) | `"lost sensor"` | Observer sensor destroyed, offline, or contact state is `Lost`. |
| `StaleTrack` (2) | `"stale track"` | Contact update age exceeds `StaleThresholdTicks` or non-continuous track. |
| `NoFireControl` (3) | `"no FC"` | Track is held but lacks dedicated fire-control tracking/illumination. |
| `NoEligibleShooter` (4) | `"no eligible shooter"` | No shooter within dynamic launch zone (DLZ), zero ammo, or policy block. |
| `DegradedTrack` (5) | `"degraded track"` | Track degraded by BDA assessment or sensor jamming. |

- **Detail Propagation:** The eligible shooter link captures and exposes specific engagement abort codes (e.g., `NO_AMMO`, `DLZ_OUT`, `ROE_HOLD_FIRE`) when evaluating candidate platforms.
- **Code Reference:** `ProjectAegis.Delegation.SensorToShooter.SensorToShooterBreakCause`, `SensorToShooterBreakCauseLabels`.
- **Test Evidence:** `src/ProjectAegis.Delegation.Tests/SensorToShooter/SensorToShooterProjectionTests.cs` (`No_eligible_shooter_marks_no_shooter_link`, `Shooter_with_no_ammo_falls_back_to_next_candidate_or_breaks_link`).

---

### KCX-06: Identity & Standard Classification Ledger

**Priority:** P0 (Shipped headless — `KillChainContactState`, `App6Sidc`)  
**Finding:** B-04  

The system shall maintain deterministic contact identity mapping across sensor feeds, symbology, and kill-chain phases:
- **Kill-Chain Phases:** `None (0)` → `Find (1)` → `Fix (2)` → `Track (3)` → `Target (4)`.
- **Transition Publishing:** Every phase change generates an immutable transition event with `PreviousPhase`, `NewPhase`, `SimTick`, `SimTime`, and sequence correlation.
- **Standard Symbology Mapping:** Contact classification feeds deterministic MIL-STD-2525 / APP-6 standard identity resolution (Friendly `F`, Neutral `N`, Hostile `H`, Suspect `S`, Pending `P`, Unknown `U`).
- **Identity Classification Ledger:** Contact identification levels (`IdentityClassification.Unknown`, `Tentative`, `Classified`, `Identified`) and explicit causal reasons (`IdentityClassReasonCodes.LifecycleUnknown`, `LifecycleDetected`, `LifecycleClassified`, `LifecycleIdentified`, `CommsGap`, `CatalogMiss`, `StaleTrack`) shall be projected via `IdentityClassProjection` to ensure unambiguous classification auditability.
- **Code Reference:** `ProjectAegis.Delegation.Projection.KillChainPhase`, `KillChainContactTransition`, `App6Sidc`, `ProjectAegis.Delegation.IdentityClass.IdentityClassProjection`, `IdentityClassification`, `IdentityClassReasonCodes`.
- **Test Evidence:** `src/ProjectAegis.Delegation.Tests/Projection/KillChainContactStateProjectionTests.cs` (`Classified_publishes_Fix_and_Track_from_location_and_custody`), `src/ProjectAegis.Delegation.Tests/Projection/App6SidcTests.cs`, `src/ProjectAegis.Delegation.Tests/IdentityClass/IdentityClassProjectionTests.cs`.

---

### KCX-07: Threat Assessment & Next-Action Diagnostics

**Priority:** P1 (Shipped headless — `ThreatAssessmentProjection`, `WeaponRecommendation`)  
**Finding:** B-08  

When weapons release is withheld, the system shall project structured advisory explanations and actionable guidance:
- `Outcome`: `Feasible`, `WithheldByPolicy` (e.g., `ROE_WEAPONS_TIGHT`, `ROE_HOLD_FIRE`), or `WithheldByEngage` (e.g., `DLZ_OUT`, `NO_AMMO`, `WINCHESTER_ORDNANCE`).
- `StatusLine`: Standardized human-readable diagnostic line:
  - *Example Policy Withhold:* `"THREAT: WITHHELD BY POLICY — ROE_WEAPONS_TIGHT (recommend SM-2 Block IIIA if ROE permits)"`
  - *Example Engage Withhold:* `"THREAT: WITHHELD BY ENGAGE — DLZ_OUT (recommend SM-2 Block IIIA when feasible)"`
- `Assumptions`: Explicit list of tactical prerequisites and constraints governing the recommendation.
- `RecommendationKind`: Classified as `AdvisoryRecommendation` (never an automatic weapons release authorization; respects ADR-010/HOL-05).
- **Withheld-Order Next-Action Guidance:** For withheld orders, `EngageNextActionProjection` evaluates tactical inputs to project deterministic next-action recommendations (`EngageNextActionCodes.ReloadRearm` vs `Approval`) without initiating weapons release (`IsFireOrder = false`).
- **Code Reference:** `ProjectAegis.Delegation.ThreatAssessment.ThreatAssessmentProjection`, `WeaponRecommendation`, `WeaponRecommendationOutcome`, `ProjectAegis.Delegation.EngageNextAction.EngageNextActionProjection`, `EngageNextActionCodes`.
- **Test Evidence:** `src/ProjectAegis.Delegation.Tests/ThreatAssessment/ThreatAssessmentProjectionTests.cs` (`Feasible_recommendation_includes_confidence_range_and_policy_constraints`, `Weapons_tight_withholds_recommendation_by_policy`, `Engage_gate_blocks_when_dlz_out_even_under_weapons_free`), `src/ProjectAegis.Delegation.Tests/EngageNextAction/EngageNextActionProjectionTests.cs`.

---

## Data Structures & Canonical Schemas

### 1. `SensorToShooterSnapshot` & `SensorToShooterChain`

```csharp
public sealed record SensorToShooterSnapshot(IReadOnlyList<SensorToShooterChain> Chains);

public sealed record SensorToShooterChain(
    string ContactId,
    string TargetId,
    string ObserverId,
    bool IsComplete,
    SensorToShooterBreakCause PrimaryBreakCause,
    IReadOnlyList<SensorToShooterChainLink> Links);

public sealed record SensorToShooterChainLink(
    SensorToShooterLinkKind Kind,
    bool IsLinked,
    SensorToShooterBreakCause BreakCause,
    string? UnitId,
    string ContactId,
    string TargetId,
    string? Detail);
```

### 2. `ContactProvenanceState`

```csharp
public sealed record ContactProvenanceState(
    string ContactId,
    ContactProvenanceSource Source,
    ContactProvenanceConfidence Confidence,
    ContactProvenanceFreshness Freshness,
    ulong AgeTicks,
    ContactProvenanceLastKnown LastKnown,
    bool OutOfCommsUnknown,
    ContactProvenanceQualityState QualityState);
```

### 3. `WeaponRecommendation`

```csharp
public sealed record WeaponRecommendation(
    string ContactId,
    string TargetId,
    string ShooterUnitId,
    string WeaponId,
    string WeaponLabel,
    WeaponRecommendationOutcome Outcome,
    ThreatRecommendationKind RecommendationKind,
    double Confidence,
    IReadOnlyList<string> Assumptions,
    ThreatRangeAssessment Range,
    ThreatPolicyConstraints PolicyConstraints,
    string? WithheldReasonCode,
    string StatusLine,
    bool IsWeaponsReleaseAuthorization = false,
    bool IsFireOrder = false,
    bool IsAutomaticEngagement = false);
```

---

## Non-Functional Requirements & Invariants

| Attribute | Specification |
|---|---|
| **Determinism** | For any identical sequence of `ContactChangeRecord` and `DecisionLog` entries evaluated at tick $T$, projections must generate bit-for-bit identical fingerprints (`SensorToShooterProjection.ComputeFingerprint`, `ContactProvenanceFingerprint.Compute`, `KillChainContactStateProjection.ComputeFingerprint`). |
| **Presentation Boundary** | UI components (C2 panels, hover tooltips) are strictly read-only consumers of projection snapshots (ADR-010). Presentation layers must never re-derive custody or break causes locally. |
| **Replay Stability** | Fingerprint hashing uses ordinal sorting by `ContactId` and invariant culture formatting; zero wall-clock (`DateTime.UtcNow`) or non-deterministic hash codes. |
| **Performance Budget** | Full kill-chain and sensor-to-shooter projection across 100 active contacts shall evaluate in $<0.5\text{ ms}$ on headless sim threads. |

---

## Acceptance Criteria

1. **Chain Completion:** Given an identified target with active fire-control tracking and an in-DLZ shooter with ammunition, `SensorToShooterProjection` evaluates `IsComplete = true` and all 4 links `IsLinked = true`.
2. **Break Isolation:** If fire control is dropped, link 3 (`Targetability`) fails with `BreakCause = NoFireControl`, breaking downstream shooter candidacy while preserving links 1 (`Sensor`) and 2 (`Track`).
3. **Stale Decay:** A contact unrefreshed for $>30$ ticks transitions to `Stale`, emitting a `Degraded` transition and breaking `Track` custody with `BreakCause = StaleTrack`.
4. **Drop Purge:** A contact unrefreshed for $>120$ ticks transitions to `Lost`, emitting a `Lost` transition and breaking `Sensor` link with `BreakCause = LostSensor`.
5. **Catalog Resolution:** A contact whose `TargetId` does not exist in catalog platforms or ORBAT bindings sets `ContactProvenanceQualityState.CatalogMiss`.
6. **Withhold Diagnostics:** A threat assessment evaluation under `RoeLevel.WeaponsTight` outputs `Outcome = WithheldByPolicy`, `WithheldReasonCode = ROE_WEAPONS_TIGHT`, and non-null diagnostic `StatusLine`.

---

## Traceability & Implementation Mapping

| Requirement | Shipped Code Symbol | Assembly | Primary Unit Test |
|---|---|---|---|
| **KCX-01** | `SensorToShooterProjection`, `TargetabilityAcceptProjection` | `ProjectAegis.Delegation` | `SensorToShooterProjectionTests.Complete_chain_links_sensor_track_targetability_and_eligible_shooter`, `TargetabilityAcceptProjectionTests` |
| **KCX-02** | `ContactProvenanceProjection` | `ProjectAegis.Delegation` | `ContactProvenanceProjectionTests.Fresh_track_publishes_source_confidence_and_last_known` |
| **KCX-03** | `KillChainContactStateProjection`, `TrackCustodyProjection` | `ProjectAegis.Delegation` | `KillChainContactStateProjectionTests.Stale_threshold_marks_track_stale_and_drops_targetable`, `TrackCustodyProjectionTests` |
| **KCX-04** | `KillChainContactStateProjection` | `ProjectAegis.Delegation` | `KillChainContactStateProjectionTests.Identified_with_fire_control_publishes_Target` |
| **KCX-05** | `SensorToShooterBreakCause` | `ProjectAegis.Delegation` | `SensorToShooterProjectionTests.No_eligible_shooter_marks_no_shooter_link` |
| **KCX-06** | `KillChainContactTransition`, `IdentityClassProjection` | `ProjectAegis.Delegation` | `KillChainContactStateProjectionTests.Classified_publishes_Fix_and_Track_from_location_and_custody`, `IdentityClassProjectionTests` |
| **KCX-07** | `ThreatAssessmentProjection`, `EngageNextActionProjection` | `ProjectAegis.Delegation` | `ThreatAssessmentProjectionTests.Weapons_tight_withholds_recommendation_by_policy`, `EngageNextActionProjectionTests` |
