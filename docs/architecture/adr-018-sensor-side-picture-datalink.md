# ADR-018: Sensor Side Picture & Datalink Sharing

## Status

**Accepted** (documents shipped `DatalinkSidePictureMerger`; harness-scoped, excluded from the pinned golden suite; shared tracks are situational-awareness only)

## Date

2026-07-24

## Last Verified

2026-07-24 (DRG-43 research + direct source verification; GitNexus impact re-run on a freshly rebuilt index)

## Decision Makers

Owner sign-off 2026-07-24; DRG-43 research brief; `sensor-detection-ew.md` GDD

## Summary

Records the architecture of **side-picture / datalink sharing** — the mechanism by which one unit's organic sensor track is shared to peers on the same side, subject to doctrine, comms state, and share lag. The mechanism is **already implemented and tested**; this ADR closes `TR-sensor-004` by recording four decisions that were previously implicit: where the merge runs, whether shared tracks may authorise weapons release, whether shared output participates in the pinned replay hash, and what "done" means for this requirement.

**This ADR records existing behaviour. It authorises no new runtime code.**

## Engine Compatibility

| Field | Value |
|-------|-------|
| Engine | Unity 6.3 LTS + .NET 8 headless |
| Unity APIs | None — `DatalinkSidePictureMerger` is plain C# in `ProjectAegis.Sim` (ADR-001 boundary holds) |
| Burst/Jobs | Not applicable — single-threaded, ordinal-sorted merge; parallelising it would require re-establishing determinism |
| Risk | **CRITICAL blast radius, LOW change risk** — see Consequences |

## ADR Dependencies

| Relationship | ADR / artifact |
|--------------|----------------|
| **Depends on** | ADR-001 (sim assembly boundary), ADR-003 (order log schema), ADR-004 (tick pipeline order) |
| **Enables** | `TR-sensor-004`; future cooperative-engagement work if ever scoped |
| **Blocks** | None |
| **Conflicts with** | `architecture.md` tick-pipeline table — see Decision A |

## GDD Requirements Addressed

| TR-ID | GDD | Requirement |
|-------|-----|-------------|
| TR-sensor-004 | sensor-detection-ew.md | Side picture / datalink |

## Decision

### A. The merge runs in the replay harness, not in `SimTickPipeline`

`DatalinkSidePictureMerger` is constructed and invoked inside `BalticReplayHarness.RunCore` (`BalticReplayHarness.cs:145,155,158` construction; `:384-386` invocation), guarded by `ScenarioDatalinkDoctrine.IsSharingEnabled`. It is **not** a step on `SimTickPipeline`.

**Decision: keep it harness-local, and record the debt explicitly rather than silently.**

`architecture.md`'s Fixed Timestep Tick Pipeline table lists mission/sensor steps as running on one code path "for interactive and headless modes." For side-picture merge that is **not accurate today**. Promoting the merge into `SimTickPipeline` would touch a HIGH-risk hub (75 impacted, epistemic *lower-bound* — `ISimTickRunner` has 2 implementations), duplicating logic across the Unity runtime and headless harness call sites. That is disproportionate to closing a documentation gap.

### B. A datalink-shared track is situational awareness only — it does **not** authorise weapons release

Shared contacts reach the order log and the C2 contact-picture projection. They do **not** feed `ObservedState` / `HasFireControlTrackOnPrimaryContact`, and therefore cannot satisfy fire control.

**Decision: shared tracks are SA-only. Weapons release continues to require an organic track.**

This preserves the fire-control contract in GDD AC-5, matches real-world doctrine for most engagement types, and requires no change to `ObservedStateBuilder`. It is also the cheaply reversible option: opening cooperative engagement later is additive, whereas retracting it would be a gameplay-contract break.

### C. Shared output is excluded from the pinned golden hash

Four scenarios enable sharing (`organicOnly: false`):

- `data/scenarios/baltic-patrol-datalink.policy.json`
- `data/scenarios/baltic-patrol-datalink-comms.policy.json`
- `data/scenarios/baltic-patrol-datalink-lag.policy.json`
- `data/scenarios/baltic-patrol-datalink-catalog-latency.policy.json`

**None of them has a replay golden in `tests/regression/`, and none is part of the ReplayGolden 6/6 suite.** They are exercised by dedicated harness tests (`BalticReplayHarnessDatalinkCommsTests`, `…DatalinkLagTests`, `…DatalinkCatalogLatencyTests`).

**Decision: sharing stays out of the pinned golden set.** The Baltic production hash `17144800277401907079` is unaffected because the pinned scenarios leave `OrganicOnly` at its `true` default (`ScenarioDatalinkDoctrine.cs:5`), so `datalinkMerger` is never constructed for them.

> ⚠️ This currently holds **by construction, not by assertion.** No test fails if someone later folds shared transitions into the world hash. Closing that is the one open validation item below.

### D. `TR-sensor-004` closes on the mechanism, not on pinned-golden adoption

**Decision: mark `TR-sensor-004` Covered.** The mechanism is implemented, tested (18 tests), doctrine-gated, and exercised by dedicated scenarios. Adding sharing to a *pinned* golden scenario is a separate content decision requiring its own hash review, and is explicitly **not** required by this ADR.

## Alternatives Considered

| Option | Why rejected |
|--------|--------------|
| **A2** — promote merge to a `SimTickPipeline` step | Touches a HIGH/75 hub with lower-bound epistemics; duplicates the call across Unity + harness paths. Disproportionate for a documentation gap |
| **A3** — move ownership into `DelegationOrchestrator`/`SimulationSession` | Best long-term hygiene, largest diff. Recorded as the preferred direction *if* the tick paths are ever unified; not now |
| **B2** — shared track unconditionally authorises fire control | True Cooperative Engagement / NIFC-CA behaviour, but a large authenticity-vs-playability swing that breaks the GDD AC-5 contract as a side effect of closing a Gap row |
| **B3** — conditional authorisation per weapon/sensor class | Genuinely interesting tactical lever ("your fire-control radar is dead — can your active-seeker missile shoot off a buddy's track?"), but needs a weapon-catalog design pass, not a by-product of this ADR |
| **C2** — include shared output in the pinned hash | Would require a hash-bump ADR and re-pinning; no benefit while no pinned scenario enables sharing |
| **D2** — enable sharing on a pinned Baltic scenario now | Separate content decision; should follow validation of B in play, not precede it |

## Consequences

### Positive

- `TR-sensor-004` closes with no new runtime code and no hash risk
- The fire-control contract stays explicit and testable rather than emergent
- The harness-local scope is now documented, so the `architecture.md` discrepancy is a known, tracked inaccuracy rather than a silent trap
- Determinism posture is recorded: ordinal sorts at every stage, no `SeededRng` draw, dictionaries used for membership only — never for output ordering

### Negative

- `architecture.md`'s pipeline table remains inaccurate for this step until either it is corrected or A3 is adopted
- Sharing is not exercised by the pinned golden suite, so a regression in the merge would be caught only by the dedicated datalink tests, not by the 6/6 gate
- The hash-exclusion guarantee is structural-by-accident, not asserted (see Validation Criteria)

## Validation Criteria

- [x] Merge is doctrine-gated and never constructed when sharing is disabled — `ScenarioDatalinkDoctrine.cs:12-13`, `BalticReplayHarness.cs:155-158`
- [x] Deterministic output ordering under adversarial input — `DatalinkSidePictureMergerTests.cs` (18 tests: peer share, organic-only suppression, side isolation, classify promotion, dedup, sort order, share-lag apply/cancel, comms-state gating)
- [x] Comms state gates sharing (Degraded suppresses new shares but allows Lost propagation; Denied suppresses all) — `DatalinkCommsShareState`, `BalticReplayHarnessDatalinkCommsTests`
- [x] Share lag derived from catalog link latency — `DatalinkShareLagResolver`, `BalticReplayHarnessDatalinkLagTests`
- [x] Shared contacts reach the order log / contact-picture projection — `ContactPictureProjectionTests.Datalink_shared_contact_projects_for_peer_observer`
- [x] Baltic production hash `17144800277401907079` unaffected — pinned scenarios leave `OrganicOnly = true`
- [ ] **OPEN:** a regression test asserting the world hash is identical whether or not `datalinkMerger` fires. Today this holds by construction only; nothing fails if a future change folds shared transitions into the hash

## Migration Plan

1. Mark `TR-sensor-004` **Covered** in `architecture-traceability-index.md` — done with this ADR.
2. **Add the open validation test** above. This is the only code change this ADR calls for, and it is additive test-only.
3. If the tick paths are ever unified (A3), revisit Decision A and correct `architecture.md`'s pipeline table in the same change.
4. If cooperative engagement (B2/B3) is ever scoped, it requires a **new ADR** — it changes the fire-control contract and must not be folded into this one.
