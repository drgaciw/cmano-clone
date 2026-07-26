# ADR-021: Mission Timeline & Contact-Trigger Runtime — Harness-Scoped Execution

## Status

**Accepted** (records as-built behaviour; headless/CI scope confirmed by owner 2026-07-24)

## Date

2026-07-24

## Last Verified

2026-07-24 (DRG-48 research + closeout; all cited behaviour verified against source)

## Decision Makers

Owner sign-off 2026-07-24 ("headless-only is correct"); DRG-48 research brief; systems index #9

## Summary

Records the architecture of the **mission timeline** and **contact-trigger** runtimes. Both ship and work; neither had a GDD or an ADR, so the traceability index listed system #9 as "Needs GDD + ADR". This ADR records four decisions that were previously implicit — most consequentially, that mission execution is **headless/CI-scoped and does not run in interactive play**.

Paired GDD: [`design/gdd/mission-runtime.md`](../../design/gdd/mission-runtime.md).

## Engine Compatibility

| Field | Value |
|-------|-------|
| Engine | Unity 6.3 LTS + .NET 8 headless |
| Unity APIs | None — `ProjectAegis.Delegation.Mission`, plain C# |
| Risk | **LOW** — `MissionRuntime` 27 impacted, `MissionContactTriggerRuntime` 37. The adjacent `DecisionLog` is CRITICAL/341 |

## ADR Dependencies

| Relationship | ADR / artifact |
|--------------|----------------|
| **Depends on** | ADR-003 (order log schema), ADR-004 (tick pipeline order) |
| **Amends** | ADR-004 — `architecture.md`'s pipeline table is inaccurate for this step (Decision 1) |
| **Enables** | Systems index #9 |
| **Related defect** | Linear **DRG-49** (vestigial `sequenceStart`) |

## GDD Requirements Addressed

| Source | Requirement |
|---|---|
| REQ-02 | Core gameplay loop — mission structure during a run |
| REQ-07 | INF-3.2, INF-2.5 |
| REQ-11 | AME-6.6 / AC-2, AME-5.5 / AC-7 |
| Systems index | **#9 Mission Runtime** |

## Decision

### 1. Mission execution is headless/CI-scoped — and `architecture.md` is wrong about this

`architecture.md`'s Fixed Timestep Tick Pipeline lists step 2 as "Apply mission timeline / events" in a pipeline it states runs "for interactive and headless modes (**same code path**)". Verified against source, that is **not true**:

- `SimTickPipeline` has **no mission step at all**
- `DelegationBridgeHost.RunTick` (the Unity interactive path) **never calls** `MissionRuntime.Tick` or `MissionContactTriggerRuntime.Evaluate` — it only calls `MissionListBridge.ProjectFrom` for a read-only display list
- Mission execution lives solely in `BalticReplayHarness.RunCore`

GitNexus corroborates: `MissionRuntime`'s only affected process is `RunCore`; `RunTick` does not appear in its impact graph.

**Decision: accept headless/CI scope as current and correct.** The system exists to make scenario timelines deterministically verifiable in the gauntlet and replay-verify paths. Wiring it into interactive play would be new subsystem work at an RC1 launch gate, and nothing in the roadmap asks for it.

**Consequence: `architecture.md`'s pipeline table must be corrected** — it currently misleads a reader into believing mission events fire during play. That correction is tracked as a follow-up rather than bundled here, since `architecture.md` is under its own review cadence.

### 2. The `sequenceId` sentinel convention is recorded as a contract

`DecisionLog.Append` treats `entry.SequenceId == 0` as "auto-assign the next global sequence" and any non-zero value as a literal, already-resolved id (`DecisionLog.cs:72`, `NextSequence()` at `:311` starting from 1).

**Decision: record this as an explicit contract.** It is currently an emergent property of `Append` plus defaulted factory parameters, documented nowhere. ADR-003 describes `sequenceId` as a global total order but says nothing about the sentinel.

**`DecisionLog` is the single authority for sequence assignment.** Emitters must pass `0`. This is what makes DRG-49 inert, and it is why the fix there is to *delete* the vestigial `sequenceStart` parameter rather than plumb it through — plumbing it activates a collision against two golden-backed fixtures.

### 3. Fire-once semantics and idempotent ROE application are intended

Both runtimes are one-shot: the timeline uses a monotonic cursor, triggers use a fire-once `HashSet<string>` keyed by `TriggerId`. `ApplyRoeToUnits` applies only where the value differs, so re-applying a unit's current ROE produces no `PolicyUpdate` row and no fingerprint delta.

**Decision: intended, not accidental.** Consistent, tested, and matches the engineering reference. The idempotency is a debugging trap worth knowing about — "my trigger didn't show up in the golden" usually means the ROE was already correct — so it is now documented at GDD level rather than only in an engineering note.

### 4. Id-prefix target classification is an accepted MVP shortcut

`MissionContactTargetClassifier.Classify` (`src/ProjectAegis.Sim/Scenario/MissionContactTargetClass.cs:12-15`) derives class from the target id: `StartsWith("ucav")` → `Air`, else `Surface`; `Any` matches everything.

**Decision: accepted as an MVP shortcut, explicitly not a permanent contract.** It is fragile — renaming an air unit without the prefix silently misclassifies it and the trigger stops matching, with no error. Recorded as GDD Open Question 1.

## Alternatives Considered

| Option | Why rejected |
|--------|--------------|
| Treat headless-only as a **gap to close** and wire mission timeline into `SimTickPipeline` | New subsystem work at an RC1 launch gate; not requested by the roadmap; would need its own scope boundary |
| Silently correct `architecture.md` and claim the pipeline is unified | Would make the document say something false about shipped behaviour |
| Leave the `sequenceId` sentinel undocumented | It is exactly the ambiguity that produced DRG-49's trap |
| Replace id-prefix classification with a domain field now | Real improvement, but a data-model change unrelated to closing a documentation gap |

## Consequences

### Positive

- System #9 has a GDD and an ADR; the "Needs GDD + ADR" row closes
- The `sequenceId` contract is written down, which is what makes DRG-49's correct fix obvious
- Undocumented traps (silent unit skip, idempotent ROE, prefix classification) are now discoverable by a designer rather than only by reading the runtime

### Negative

- `architecture.md` remains inaccurate until separately corrected — a known, tracked inaccuracy rather than a silent one
- Mission behaviour is exercised only by the headless harness, so a regression would be caught by scenario tests rather than by any interactive smoke path
- Target classification stays fragile to unit renames

## Validation Criteria

- [x] Timeline emission order is a **total** sort — `(FireAtTick, fireOrder index ?? int.MaxValue, EventId ordinal)`, ties impossible
- [x] Fire-once for both events (monotonic cursor) and triggers (`HashSet<TriggerId>`)
- [x] Trigger fires only on `Unknown → Detected` — `MissionContactTriggerRuntime.cs:24-25`
- [x] `ApplyRoeToUnits` iterates ordinally and applies only on change
- [x] No wall-clock read, no `SeededRng` draw, no unordered iteration for output — both runtimes are pure functions of `(simTick, simTime, transition)`
- [x] Authored `fire_order` returned verbatim when a timeline exists
- [ ] **OPEN:** correct the `architecture.md` tick-pipeline table (Decision 1)
- [ ] **OPEN (DRG-49):** delete the vestigial `sequenceStart`; add a guard test appending 2+ same-tick events through the production factory calls into a real `DecisionLog`

## Migration Plan

1. Publish the GDD and this ADR; update systems index #9 — done.
2. Correct `architecture.md`'s pipeline table to state that mission timeline is harness-scoped.
3. Resolve DRG-49 by deletion, with the guard test. **Do not bundle with documentation work** — `DecisionLog` is CRITICAL/341.
4. If interactive play is ever in scope, re-open Decision 1, rewrite the GDD's Player Fantasy, and open a scope boundary for the wiring work.
