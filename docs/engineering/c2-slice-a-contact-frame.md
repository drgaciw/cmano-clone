# Slice A contact frame — developer guide

`SliceAContactFrame` is the **tick-level read-model aggregate** for the Slice A command chrome. It
is the single object the Unity C2 hosts read to answer "what contacts do I see, how good is each
track, is a firing solution complete, and what am I allowed to do about it?" — folded once per
simulation tick from the order log and the current world snapshot, with **no live simulation
handles** kept past the build.

One call — [`SliceAContactFrameBridge.Build`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SliceAContactFrame.cs)
— composes four pure projections (kill-chain picture, contact provenance, sensor-to-shooter chains,
and the per-contact authority map) into an immutable `record`. The composition **fails closed**:
a contact only carries a firing/authority verdict when a registered, eligible, in-comms shooter and
authoritative evidence actually exist. Missing evidence is never read as permission.

It is the aggregate that the authority chrome ([c2-authority-escalation-chrome.md](c2-authority-escalation-chrome.md))
depends on for its `Authorities` map, and the frame that the combat, status, and advice bridges each
consume downstream — so it is the natural "one read model per tick" seam between the projection layer
and every Slice A panel host.

> **Presentation boundary (ADR-010 §2–3, ADR-007, ADR-001).** Everything here is read-only. The
> frame is built at the tick boundary and cached on the host; hosts bind an already-resolved frame,
> they never re-run a projection on the render thread. The UI is a *client*, not sim authority. See
> the [UnityAdapter README](../../src/ProjectAegis.Delegation.UnityAdapter/README.md).

Related: read-model layer [c2-projection-layer.md](c2-projection-layer.md) ·
adapter seam [c2-presentation-bridges.md](c2-presentation-bridges.md) ·
authority chrome [c2-authority-escalation-chrome.md](c2-authority-escalation-chrome.md) ·
decision-time gate [autonomy-roe-gating.md](autonomy-roe-gating.md).

---

## Where it sits

```text
ISimWorldSnapshot (+ optional ISensorToShooterShooterSource / ISliceAContactAuthoritySource)
   │  DelegationBridge.Orchestrator.DecisionLog (order log — the source of truth)
   ▼
SliceAContactFrameBridge.Build(snapshot, bridge, catalog?, shooters?)   — UnityAdapter/Bridge/
   │  composes four pure projections (below)
   ▼
SliceAContactFrame  { KillChain · Provenance · Chains · Contacts · Authorities · EligibilityAvailable · SimTick · SimTime }
   │  cached once per tick on
   ▼
DelegationBridgeHost.LastSliceAContacts
   │  fanned out to
   ├─▶ ContactDetailPanelHost / AuthorityRoePanelHost / EngageExplainPanelHost / PendingApprovalPanelHost
   └─▶ CombatPresentationFrameBridge · StatusFrameBridge · AdviceBridge (downstream frames)
```

Only `SliceAContactFrameBridge` lives in the adapter; the four projections it folds are
engine-agnostic (`ProjectAegis.Delegation`, `netstandard2.1`, plain `dotnet test`). The frame keeps
**no** reference to `ISimWorldSnapshot`, `DelegationBridge`, or `SimulationSession` — it is a plain
value snapshot.

---

## The frame — `SliceAContactFrame`

An immutable `sealed record` in `ProjectAegis.Delegation.UnityAdapter.Bridge`. `SliceAContactFrame.Empty`
is the "no received simulation frame" default the host starts from.

| Field | Type | Meaning |
|-------|------|---------|
| `KillChain` | [`KillChainContactSnapshot`](../../src/ProjectAegis.Delegation/Projection/KillChainContactState.cs) | F2T2 picture: per-contact `Targetable`/phase/`Loss` state plus published transitions. |
| `Provenance` | [`ContactProvenanceSnapshot`](../../src/ProjectAegis.Delegation/Projection/ContactProvenance.cs) | Per-contact information quality: `Freshness` (`Fresh`/`Stale`), `OutOfCommsUnknown`, confidence, catalog-miss flags. |
| `Chains` | [`SensorToShooterSnapshot`](../../src/ProjectAegis.Delegation/SensorToShooter/SensorToShooterTypes.cs) | Sensor → track → targetability → eligible-shooter chain per contact; `IsComplete` gates a firing solution. |
| `Contacts` | `IReadOnlyList<`[`ContactPictureEntry`](../../src/ProjectAegis.Delegation/Projection/ContactPictureEntry.cs)`>` | The C2 contact-list rows (one per contact id), with BDA loss/degradation folded into `LifecycleState`. |
| `Authorities` | `IReadOnlyDictionary<string, `[`C2AuthorityProjection`](../../src/ProjectAegis.Delegation/Skills/C2AuthorityTypes.cs)`>` | Per-contact advisory authority (ordinal, keyed by contact id). **Fail-closed: a missing key ≠ permitted.** |
| `EligibilityAvailable` | `bool` | Whether a shooter-candidate source was supplied this tick (`false` ⇒ no per-shooter side; treat all firing solutions as unproven). |
| `SimTick` | `ulong` | Whole-tick stamp (`0` when `SimTime ≤ 0`); hosts refresh only when this or the selection changes. |
| `SimTime` | `double?` | Raw sim time of the frame. |

`Authorities` is keyed by **contact id**, not target id — two observers of the same target get two
rows and never collide.

---

## Optional evidence hooks on the snapshot

`Build` reads two optional interfaces off the world snapshot (or the explicit `shooters` argument).
Both are **sim-authored**; UI selection is never evidence.

| Interface | Method | Supplies |
|-----------|--------|----------|
| [`ISensorToShooterShooterSource`](../../src/ProjectAegis.Delegation/SensorToShooter/SensorToShooterTypes.cs) | `GetCandidatesForTarget(targetId)` | Candidate shooters with scenario engage defaults and magazine rounds. |
| [`ISliceAContactAuthoritySource`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SliceAContactFrame.cs) | `TryGetAuthorityContext(contactId, shooterUnitId, out ctx)` | Present per-shooter authority evidence (ROE, track source, fire-control) for exactly this contact + shooter. |

When the snapshot implements neither (e.g. a bare `ISimWorldSnapshot`), the frame still builds the
picture, provenance, and chains — but `EligibilityAvailable` is `false`, no chain completes, and
`Authorities` is empty. That is the intended fail-closed default, not an error.

---

## `Build` pipeline (`SliceAContactFrameBridge.Build`)

`Build(ISimWorldSnapshot snapshot, DelegationBridge bridge, ICatalogReader? catalog = null,
ISensorToShooterShooterSource? shooters = null)`. Null `snapshot` or `bridge` throw
`ArgumentNullException`. It is pure over its inputs and the order log — it reads
`bridge.Orchestrator.DecisionLog` but never appends to it.

1. **Kill-chain picture** — `KillChainContactStateBridge.Build(snapshot, log)`.
2. **Contact list** — `ContactPictureProjection.Project(log)` indexed by contact id. For a contact
   whose kill-chain `Loss` is `Lost` / `DegradedL1` / `DegradedL2`, the row is rebuilt with the BDA
   `LifecycleState` ([`BdaContactDamageStates`](../../src/ProjectAegis.Delegation/Projection/BdaContactDamageStates.cs):
   `"Lost"` / `"Degraded-L1"` / `"Degraded-L2"`) and kill-chain timing, so a lost contact keeps its
   identity; otherwise the live picture row is used verbatim.
3. **Provenance** — `ContactProvenanceProjection.Project(contacts, tick, commsState, catalog, commsDisplay)`,
   folding the current comms state and catalog identity into freshness/quality per contact.
4. **Sensor-to-shooter chains** — `SensorToShooterProjection.Project(killChain, guard, catalog)`,
   where `guard` is a [`LiveCandidateGuard`](#live-candidate-guard) wrapping the shooter source
   (or `null` when none is supplied, which forces every chain incomplete).
5. **Per-contact authorities** — for each chain, resolve at most one authority projection (below).

The result is `new SliceAContactFrame(killChain, provenance, chains, contacts, authorities,
EligibilityAvailable: source != null, SimTick: tick, SimTime: snapshot.SimTime)`.

### Per-contact authority resolution (fail-closed)

For each `chain` in `Chains`, an entry is added to `Authorities` **only when every** condition holds
— any miss `continue`s with no entry:

1. The chain has a linked `EligibleShooter` link with a `UnitId`.
2. That shooter is **registered** in `bridge.Registry` (`TryGetBinding`).
3. The snapshot implements `ISliceAContactAuthoritySource` **and** `TryGetAuthorityContext` returns
   `true` for this contact + shooter.
4. The supplied context's `TrackSource` is **not** `Unknown`.

When all four hold, the supplied context is **finalized to the fire path** before projection:

```csharp
context = supplied with {
    Lane             = SkillLane.Propose,
    RequiredApproval = RequiredApproval.WeaponsRelease,
    CommandId        = "engage",
    // fire-control is only satisfied if the sim says so AND the chain is complete AND the track is current
    FireControlSatisfied = supplied.FireControlSatisfied && chain.IsComplete && currentTrack,
};
authorities.Add(chain.ContactId, C2AuthorityProjector.Project(context));
```

`currentTrack` is `true` only when the contact's provenance is `Fresh` **and** not
`OutOfCommsUnknown`. So a stale or out-of-comms track — even behind an otherwise-eligible shooter —
drops `FireControlSatisfied`, and the projector withholds engage with `NO_FIRE_CONTROL`. The
projector itself (ROE slice, targeting leg, per-verb dispositions) is documented in
[c2-authority-escalation-chrome.md](c2-authority-escalation-chrome.md).

### Live candidate guard

`LiveCandidateGuard` wraps the raw `ISensorToShooterShooterSource` and drops any candidate that is not
a **live, launch-ready, armed** shooter, so the chain never completes off a paper candidate. Per
candidate for a target it excludes:

- the target itself (a unit cannot shoot itself),
- a member that is not alive (`snapshot.IsMemberAlive`),
- a shooter not registered in `bridge.Registry`,
- a shooter whose `UnitReadiness.IsReadyForLaunch` is `false`.

Rounds are then taken from the **magazine ledger** when a session is present:
`Magazines.TryGetRounds(...)` — a **missing ledger entry yields `0`, never a config-seeded refill**.
`0` rounds means the chain cannot complete.

---

## Consumers

`SliceAContactFrame` is built once per tick and cached; everything downstream reads the cache.

| Consumer | Uses |
|----------|------|
| [`DelegationBridgeHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs) | Builds `LastSliceAContacts = SliceAContactFrameBridge.Build(snapshot, Bridge, CatalogReader)` each tick and exposes it. |
| `ContactDetailPanelHost` | The contact list + selected-contact evidence (picture, provenance, chain). |
| `AuthorityRoePanelHost` | `frame.Authorities[SelectedContactId]` for the authority/ROE/escalation panel. |
| `EngageExplainPanelHost` / `PendingApprovalPanelHost` | The same `Authorities` map for the inline authority summary line. |
| `CombatPresentationFrameBridge` | Folds the frame into the combat picture. |
| `StatusFrameBridge` / `AdviceBridge` | Command-review status and advice frames built from the same contacts. |

Because the frame carries `SimTick`, hosts short-circuit their refresh unless the selected contact
id **or** the tick changed — the projections are not re-run on every render frame.

---

## Invariants — never break these

| Invariant | Rule |
|-----------|------|
| **Fail closed on missing evidence** | No registered eligible shooter, no `ISliceAContactAuthoritySource`, `TrackSource == Unknown`, stale/out-of-comms track, or empty magazine ⇒ **no authority entry / incomplete chain**. Absence is never permission. |
| **Contact-id keyed, no observer collision** | `Contacts` and `Authorities` key on contact id, so two observers of one target stay distinct. |
| **Tick-level, no live handles** | The frame keeps no `ISimWorldSnapshot` / `DelegationBridge` / `SimulationSession` reference; it is a value snapshot built at the tick boundary. |
| **Read-only / order-log is truth** | `Build` reads `DecisionLog` and never mutates it; rebuilding the same tick is deterministic and side-effect free. |
| **Magazine ledger wins** | A tracked magazine of `0` overrides a permissive candidate source and is never refilled from configuration. |
| **Off the `DelegationBridge.Tick` hotpath** | Composition happens in the host's post-tick refresh; `DelegationBridge.cs` stays zero-touch (Baltic v2 hash `17144800277401907079` unchanged). |
| **No `UnityEngine` in the folded projections** | Only `SliceAContactFrameBridge` is in the adapter; the four projections target `netstandard2.1` and run under plain `dotnet test`. |

---

## Common pitfalls

- **Reading a missing `Authorities` key as "permitted."** The map only contains fail-closed,
  evidence-backed entries. `TryGetValue` miss ⇒ show *unknown*, never a green engage.
- **Re-running a projection in the host.** Bind `LastSliceAContacts`; do not call
  `SliceAContactFrameBridge.Build` (or a sub-projection) from a host `Update`/render path.
- **Trusting a candidate source without the guard.** The raw `ISensorToShooterShooterSource` may list
  dead, unregistered, unready, or empty-magazine shooters; the `LiveCandidateGuard` is what makes a
  complete chain meaningful.
- **Treating a stale / out-of-comms track as fire-control.** `currentTrack` gates
  `FireControlSatisfied`; a `Stale` or `OutOfCommsUnknown` contact withholds engage even behind an
  eligible shooter.
- **Keying on target id.** Use contact id; target id collides across observers.

---

## Extending without breaking replay

1. **New evidence fact?** Add it to the relevant projection (`ContactProvenance`,
   `SensorToShooter`, kill-chain) and thread it through `Build`; keep the fail-closed default so a
   missing fact withholds rather than permits.
2. **New shooter-eligibility rule?** Add it to `LiveCandidateGuard` (exclude the candidate), not to
   the host — the guard is the single choke point for "is this a real shooter."
3. **New downstream frame?** Consume `LastSliceAContacts` read-only like the combat/status/advice
   bridges; do not add a second tick-time build.
4. **Before landing**, run the suites below plus the full solution suite (`dotnet test`), confirm
   `ReplayGolden 6/6`, `PlayModeSmoke ≥20/20`, the Baltic v2 hash is unchanged, and ZERO
   `DelegationBridge` Tick hotpath edits. Adapter work follows the `unity-csharp-architect`
   [`checklists/pr-finish.md`](../../production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md).

---

## Tests that pin this doc

| Test file | Covers |
|-----------|--------|
| [`Bridge/SliceAContactFrameTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/SliceAContactFrameTests.cs) | Null-input throws, missing-shooter fail-closed, multi-observer non-collision, BDA loss/degradation identity, deterministic rebuild (no log mutation), stale/other-target fire-control non-transfer, explicit-shooter ⇒ `ApprovalRequired`, empty/tracked-empty magazine ⇒ incomplete chain, no-authority-evidence ⇒ unknown, hold-fire and denied-comms ⇒ withheld. |
| [`Presentation/SliceAContactHostContractTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/SliceAContactHostContractTests.cs) | Host consumes the **cached frame not the live log**, the composition root publishes the frame after a tick, and the contact panel wires its assets with an independent authority region. |

Run just this subsystem:

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~Bridge.SliceAContactFrame|FullyQualifiedName~Presentation.SliceAContactHostContract"
```

---

## See also

| Topic | Doc |
|-------|-----|
| Advisory authority / ROE / escalation chrome (the `Authorities` consumer) | [c2-authority-escalation-chrome.md](c2-authority-escalation-chrome.md) |
| C2 read-model layer & `Projection → Binder → State` (the folded projections) | [c2-projection-layer.md](c2-projection-layer.md) |
| Adapter seam (bridges, host refresh order, `IC2PresentationFeed`) | [c2-presentation-bridges.md](c2-presentation-bridges.md) |
| Decision-time ROE / autonomy gate (the write path) | [autonomy-roe-gating.md](autonomy-roe-gating.md) |
| Engage gate chain that actually authorizes fire | [engagement-pipeline.md](engagement-pipeline.md) |
| Presentation boundary decisions | [ADR-010](../architecture/adr-010-headless-first-command-driven-ui.md) · [ADR-007](../architecture/adr-007-c2-map-presentation.md) |

---

*Verified against source at the paths above (`SliceAContactFrame` / `SliceAContactFrameBridge` and
its four folded projections). If you change the build pipeline, the guard rules, or the fail-closed
authority resolution, update this doc and its `See also` siblings together.*
