# Slice A contact-evidence frame — technical eligibility vs. release authority (DRG-206/207/209)

The **Slice A contact-evidence frame** is the read model behind the C2 *contact detail* panel: for a
selected hostile contact it explains, in plain text, **whether the sensor→shooter kill chain is
technically complete** and — as a strictly separate fact — **whether an actor is cleared to fire**.
Its load-bearing rule is that *technical feasibility is never permission*: when current runtime
evidence is missing it reports **UNKNOWN**, never a green light, and it **fails closed**.

It lives in the Unity adapter as two pure, engine-agnostic parts:

- [`SliceAContactFrameBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SliceAContactFrame.cs) —
  the **composition root**: one `Build(...)` call after a sim tick folds the kill-chain, contact
  provenance, sensor-to-shooter chain, and (optional) authority projections into an immutable
  [`SliceAContactFrame`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SliceAContactFrame.cs).
- [`SliceAContactPresenter`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/SliceAContactPresentation.cs) —
  the **formatter**: turns the selected contact's slice of that frame into a six-line
  [`SliceAContactPresentation`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/SliceAContactPresentation.cs)
  (phase, provenance, freshness, technical chain, release authority, next action).

> **Scope / boundary (Combat UX Slice A, ADR-010 §2–3 / ADR-007 / ADR-001):** this is a **read-only**
> presentation frame. It binds immutable projection output, issues **no** commands, and never primes
> engagement state or refills magazines. It composes existing projections
> ([`KillChainContactStateBridge`](../../src/ProjectAegis.Delegation/Projection/KillChainContactStateProjection.cs),
> [`ContactPictureProjection`](c2-projection-layer.md), [`ContactProvenanceProjection`](c2-projection-layer.md),
> [`SensorToShooterProjection`](../../src/ProjectAegis.Delegation/SensorToShooter/SensorToShooterProjection.cs),
> [`C2AuthorityProjector`](../../src/ProjectAegis.Delegation/Skills/C2AuthorityProjector.cs)); it derives
> **no** combat truth of its own. `DelegationBridge.cs` is **zero-touch**. Rebuilding is deterministic
> and does not mutate the `DecisionLog`, so it is **off** the Baltic v2 `SimWorldHash` / replay
> goldens. **Slice A is not fully accepted** — see [Known gaps](#known-gaps).

---

## Types

| Type | Kind | Role |
|------|------|------|
| [`SliceAContactFrameBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SliceAContactFrame.cs) | `static` | The fold: `Build(snapshot, bridge, catalog?, shooters?)` → `SliceAContactFrame`. |
| [`SliceAContactFrame`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SliceAContactFrame.cs) | `sealed record` | `(KillChain, Provenance, Chains, Contacts, Authorities, EligibilityAvailable, SimTick, SimTime)`; `Empty` for "no frame received". |
| [`ISliceAContactAuthoritySource`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SliceAContactFrame.cs) | `interface` | Optional world-snapshot seam: `TryGetAuthorityContext(contactId, shooterUnitId, out C2AuthorityProjectionContext)` — authoritative, actor-specific authority facts. |
| [`ISensorToShooterShooterSource`](../../src/ProjectAegis.Delegation/SensorToShooter/SensorToShooterTypes.cs) | `interface` | Sim-authored shooter candidacy: `GetCandidatesForTarget(targetId)`. Supplied explicitly or via the snapshot. |
| [`SliceAContactPresenter`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/SliceAContactPresentation.cs) | `static` | `Build(contactId, killChain, provenance, chains, authority)` → `SliceAContactPresentation`. |
| [`SliceAContactPresentation`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/SliceAContactPresentation.cs) | `sealed record` | Six text lines: `(PhaseLine, ProvenanceLine, FreshnessLine, ChainLine, AuthorityLine, NextActionLine)`; `Empty` clears the panel. |

Underlying projection enums it surfaces (unchanged by this layer): `KillChainPhase` (`Find/Fix/Track/Target`),
`KillChainLossKind` (`Stale/DegradedL1/DegradedL2/Lost`), `SensorToShooterLinkKind`
(`Sensor/Track/Targetability/EligibleShooter`), `SensorToShooterBreakCause`
(`LostSensor/StaleTrack/NoFireControl/NoEligibleShooter/DegradedTrack`), and
`C2AuthorityDisposition` (`Permitted/Withheld/ApprovalRequired`).

---

## The fold — `SliceAContactFrameBridge.Build`

`Build(snapshot, bridge, catalog?, shooters?)` runs once after a sim tick, reading the orchestrator's
`DecisionLog` (never writing it). Null `snapshot` or `bridge` throw `ArgumentNullException`.

1. **Kill chain.** `KillChainContactStateBridge.Build(snapshot, log)` folds the F2T2 picture, including
   per-contact BDA (loss/degradation).
2. **Contacts (multi-observer safe).** The active `ContactPictureProjection.Project(log)` is indexed by
   `ContactId` — **not** by target — so two observers of the same target do not collide. For contacts
   the kill chain marks `Lost` / `DegradedL1` / `DegradedL2`, the frame substitutes a
   `ContactPictureEntry` carrying the matching `BdaContactDamageStates` label so a **lost identity stays
   visible** rather than disappearing.
3. **Provenance.** `ContactProvenanceProjection.Project(...)` attaches source ref, confidence, freshness,
   and comms state (from `CommsStateProjection`), using the read-only `catalog` when supplied.
4. **Sensor→shooter chain.** `SensorToShooterProjection.Project(killChain, guard?, catalog)`. The shooter
   source is the explicit `shooters` argument, else the snapshot cast to `ISensorToShooterShooterSource`,
   else `null` (see [fail-closed eligibility](#fail-closed-eligibility)). When present it is wrapped in a
   `LiveCandidateGuard`.
5. **Authority (opt-in, per contact).** For each chain the frame finds the `EligibleShooter` link that
   `IsLinked` and gates it — see [authority gating](#authority-gating). Contacts without authoritative
   evidence get **no** entry in `Authorities` (the panel renders UNKNOWN).

`EligibilityAvailable` is `true` iff a shooter source was resolved; it distinguishes "runtime supplies
eligibility facts" from "no eligibility producer, everything UNKNOWN".

### Fail-closed eligibility

The composition **never** manufactures shooter eligibility from configuration. Without an authoritative
`ISensorToShooterShooterSource`, `ISimWorldSnapshot` has no per-shooter side, current geometry, or
commitment facts — so the chain is left incomplete. Scenario engage defaults and historical
`EngageWorld` contexts are explicitly **not** evidence of present eligibility.

The `LiveCandidateGuard` filters every candidate the source proposes and drops it when the unit:

- is the target itself, or is not `IsMemberAlive`, or is not registered in `bridge.Registry`, or
  `Session.UnitReadiness.IsReadyForLaunch(...)` is `false`; and
- **magazine authority is the ledger, not the candidate.** When `Session.Magazines` exists, rounds come
  from `TryGetRounds(...)` — a *tracked-empty* magazine stays `0` and a *missing* ledger entry reads `0`.
  A missing entry is **not** permission to seed or refill from configuration. A tracked-empty shooter
  therefore cannot complete the chain, and the ledger is never mutated.

### Authority gating

An authority projection is produced for a contact **only** when all hold:

1. the chain has an `EligibleShooter` link that `IsLinked` to a shooter unit id; and
2. that shooter is registered in `bridge.Registry`; and
3. the snapshot implements `ISliceAContactAuthoritySource` and `TryGetAuthorityContext(...)` returns
   `true` with `TrackSource != Unknown`.

The supplied context is then forced into a Slice A shape before projection:
`Lane = Propose`, `RequiredApproval = WeaponsRelease`, `CommandId = "engage"`, and
`FireControlSatisfied = supplied.FireControlSatisfied && chain.IsComplete && currentTrack`, where
`currentTrack` requires the provenance row to be `Fresh` **and** not `OutOfCommsUnknown`. The result is
`C2AuthorityProjector.Project(context)`. Consequences observed by the tests:

- **HoldFire ROE ⇒ `Withheld`** even when the technical chain is complete.
- **Denied comms ⇒ `OutOfCommsUnknown` ⇒ `currentTrack` false ⇒ `Withheld`**, despite an eligible shooter.
- **Eligible + fresh + `WeaponsFree` ⇒ `ApprovalRequired`** (Slice A always requires weapons-release
  approval; it never emits `Permitted` as a fire clearance).
- A **registered shooter with no authority evidence** yields a complete chain but an empty `Authorities`
  entry — i.e. authority remains **UNKNOWN**, not implied.

---

## The presenter — `SliceAContactPresenter.Build`

Formats the selected contact's slice into six lines; a null/blank/unknown `contactId` (or a contact with
no kill-chain, provenance, or chain row) returns `SliceAContactPresentation.Empty`, clearing the panel.

| Line | Content |
|------|---------|
| `PhaseLine` | `Phase / Loss / Technical targetability (YES/NO)`, or `Phase: UNKNOWN`. |
| `ProvenanceLine` | Observer, confidence, source ref, classification, quality — or `UNKNOWN — no active source record`. |
| `FreshnessLine` | `Freshness / Age (ticks) / Comms`. Comms reads `DENIED — current state unknown`, `DEGRADED` (silent-comms), or `no degradation reported`. |
| `ChainLine` | `Technical chain: COMPLETE (not release authority)` or `BROKEN — <cause>`, then a per-link `LINKED/BROKEN` breakdown with unit id, cause label, and detail. |
| `AuthorityLine` | `ROE` + `Release authority: PERMITTED … (technical checks remain separate)` / `APPROVAL REQUIRED` / `WITHHELD`, plus reason and pending approval — or `UNKNOWN — … not cleared to engage`. |
| `NextActionLine` | The single most important action (see below). |

`NextActionLine` is a fixed priority ladder that always names an operator action rather than a verdict:
lost contact ⇒ *reacquire* (last-known is not a firing solution) → missing provenance ⇒ *obtain a
current report* → out-of-comms ⇒ *restore comms* → stale ⇒ *refresh the track* → catalog miss ⇒
*resolve identity* → broken chain ⇒ cause-specific fix (`NoFireControl` / `NoEligibleShooter` /
`DegradedTrack` / other) → missing authority ⇒ *obtain the actor's authority projection* →
`ApprovalRequired` ⇒ *request approval through the command workflow* → other non-permitted ⇒ *review the
restriction with command* → otherwise *submit intent through the command workflow; execution
revalidates eligibility and authority*.

The presenter never colors its output or infers permission: a **COMPLETE** chain with a null authority
still prints `Release authority: UNKNOWN` and a next action about obtaining authority.

---

## Host wiring (Unity)

- [`DelegationBridgeHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs) calls
  `SliceAContactFrameBridge.Build(...)` **once per sim tick** and caches the result as
  `LastSliceAContacts` (default `SliceAContactFrame.Empty`). `DelegationBridge.cs` itself is untouched.
- [`ContactDetailPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/ContactDetailPanelHost.cs)
  (CMD-29) binds the cached frame and calls `SliceAContactPresenter.Build(...)` **only when the frame or
  selected contact changes** (`ReferenceEquals` + ordinal id compare) — never per render frame — and
  exposes `LastSliceAPresentation`. Build at tick/selection boundaries, not every frame.

To wire a new host: read `DelegationBridgeHost.LastSliceAContacts`, pass `SelectedContactId` plus the
frame's `KillChain` / `Provenance` / `Chains` and `Authorities[contactId]` (may be absent → pass `null`)
into `SliceAContactPresenter.Build`, and render the six lines read-only.

---

## Determinism & tests

Pure and replay-safe: no RNG, no wall-clock, no `DecisionLog` mutation, and rebuilding the frame from the
same log/snapshot yields equal `Contacts` / `Provenance` (verified by
`Rebuilding_is_deterministic_and_does_not_mutate_log`). The presenter is likewise idempotent
(`Repeated_projection_is_replay_stable`). Nothing here contributes to the Baltic v2
`17144800277401907079` hash.

Co-located NUnit fixtures in `ProjectAegis.Delegation.UnityAdapter.Tests`:

- [`Bridge/SliceAContactFrameTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/SliceAContactFrameTests.cs)
  — null-guards, missing-evidence-never-clearance, multi-observer non-collision, per-observer BDA +
  lost identity, determinism, stale visibility, cross-target FC isolation, approval-required, empty/
  tracked-empty ammo, registered-shooter-without-authority, HoldFire, denied-comms.
- [`Presentation/SliceAContactPresentationTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/SliceAContactPresentationTests.cs)
  — clear-on-unknown-selection, complete-chain-is-not-permission, approval-vs-targetability, provenance/
  age/comms surfacing, broken-cause → next-action mapping, lost-without-provenance, stale-priority,
  replay stability.
- [`Presentation/SliceAContactHostContractTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/SliceAContactHostContractTests.cs)
  and the Unity-side `Assets/Tests/SliceAContactPanelSmokeTests.cs` — host/asset wiring.

Run headless:

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "SliceAContact"
```

---

## Known gaps

Per the [Slice A implementation evidence](../../production/qa/slice-a-contact-ui-2026-09-05.md) (verdict
**BLOCKED**), the headless frame/presenter and their contracts are complete and green, but full Slice A
acceptance is **not** closed:

- The runtime still needs an **authoritative live producer** for `ISensorToShooterShooterSource` +
  `ISliceAContactAuthoritySource` (side, current geometry, readiness, commitment). Without it the panel
  correctly shows sensing data with **UNKNOWN** shooter/authority.
- The eligible shooter is the projection's first nomination; **authority-aware best-shooter ranking**
  across alternatives is not implemented.
- **Unity Editor import / layout and DRG-208 Play Mode** signoff remain open; headless proxy tests are
  not that signoff.

---

## Related

- Read-model layer & projection contract: [c2-projection-layer.md](c2-projection-layer.md).
- Adapter seam between Unity hosts and projections: [c2-presentation-bridges.md](c2-presentation-bridges.md).
- The engine↔core write-side boundary: [delegation-bridge-adapter-boundary.md](delegation-bridge-adapter-boundary.md).
- Authorization at decision time (sim side): [autonomy-roe-gating.md](autonomy-roe-gating.md) ·
  [doctrine-inheritance-and-override.md](doctrine-inheritance-and-override.md).
- The C2 network-health read model (sibling Combat UX Slice A projection):
  [c2-network-health-projection.md](c2-network-health-projection.md).
