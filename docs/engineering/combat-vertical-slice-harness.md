# Combat vertical-slice harness — developer guide

`CombatVerticalSliceHarness` is the **single headless runner** for the Combat UX "Slice B"
acceptance evidence (DRG-166 / DRG-170). It drives every fixture leg from a *player command*
through the real `MvpEngagementResolver`, then reads the outcome back **only through the real
projections** a Unity host would bind — Slice A targetability, the DRG-165 combat-event
contract, the presentation frame, the combat map at each zoom band, the DRG-168 engagement
explanation, and symbol LOD. Presentation never invents combat truth.

- **Source:** [`CombatVerticalSliceHarness.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/CombatVerticalSliceHarness.cs)
  (`ProjectAegis.Delegation.UnityAdapter/Baltic/`).
- **Scenario driver:** [`SliceBCombatScenario.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/SliceBCombatScenario.cs)
  (`RunCommanded`, `LoadDefaultDefinition`, `ContactIdFor`).
- **Fixture:** [`data/scenarios/slice-b-combat-acceptance.json`](../../data/scenarios/slice-b-combat-acceptance.json)
  — six synthetic legs (permitted and refused Missile, Gun, Laser). `synthetic: true`; **not** a
  Baltic v2 golden.
- **Acceptance record:** the DRG-166/170 evidence writeup in
  [`docs/superpowers/reviews/2026-10-06-drg-166-170-combat-vertical-slice-evidence.md`](../superpowers/reviews/2026-10-06-drg-166-170-combat-vertical-slice-evidence.md).

> **Scope / boundary.** The harness is read-only presentation evidence: it composes pure
> projections after the sim has resolved, never queries live simulation handles, and is isolated
> from the Baltic v2 replay goldens (`ADR-010 §2–3`, `ADR-007`, `ADR-001`). It is a sibling of the
> [Baltic replay harness](baltic-replay-harness.md) — that runner proves the sim pipeline; this one
> proves the *presentation reads the sim honestly*.

---

## The `Run` entry point

```csharp
// Default: the checked-in Slice B fixture, seed 7.
CombatVerticalSliceEvidence Run(int seed = 7);

// Run an explicit definition.
CombatVerticalSliceEvidence Run(int seed, SliceBCombatScenario.Definition definition, int maxEffects = 64);
```

`Run` calls `SliceBCombatScenario.RunCommanded(seed, definition)` and then `Project(...)`. Every
leg's shooter is a **Manual** agent: its engage order is queued, approved through the player
command path (`DelegationOrchestrator.TryApprovePendingOrder`), and executed on the next tick by
the real resolver. `Project` throws if the number of approved commands does not match the number
of fixture legs — an approved command that does not execute is a hard failure, not a silent drop.

| Parameter | Meaning |
|-----------|---------|
| `seed` | Global sim seed. Same seed + same definition ⇒ identical evidence and fingerprint. |
| `definition` | The fixture legs (shooter/target/weapon-family, expected-permitted, fire-control-track flag, rounds). `LoadDefaultDefinition()` loads `slice-b-combat-acceptance.json`. |
| `maxEffects` | Combat-map effect budget per zoom band (default 64). Governs declutter when fired legs exceed the pool (see DRG-170). |

## What one run produces — `CombatVerticalSliceEvidence`

```csharp
record CombatVerticalSliceEvidence(
    SliceBCombatScenario.Result Scenario,       // raw commanded run (order log, commands, symbols)
    TargetabilityAcceptSnapshot TargetabilityAccept, // Slice A acceptance, at command time, per target
    CombatEventSnapshot Events,                  // DRG-165 contract: events + Slice A facts + execution facts
    CombatPresentationFrame Frame,               // one sim-time picture shared by map/history/detail
    IReadOnlyList<MapSymbolEntry> Symbols,       // distinct map symbols
    IReadOnlyList<CombatVerticalSliceLeg> Legs,  // per-engagement correlated evidence
    IReadOnlyList<CombatVerticalSliceZoomMap> Maps, // unselected map at Tactical/Operational/Theater
    IReadOnlyList<MapLodApplyResult> SymbolLod,  // Close/Tactical/Theater/Overview symbol LOD
    string Fingerprint);                         // replay-stable composite
```

Each `CombatVerticalSliceLeg` correlates one commanded engagement across surfaces by a single
`CorrelationId` (the order-log sequence id, **not** the resolver engagement id): its combat
events, the per-zoom map `Views`, event-history lines, the DRG-168 `Explanation`, the command
order id, and the leg's `CombatTargetabilityFact?`.

## Pipeline — command in, projected evidence out

`Project` runs the following, strictly in order:

1. **Guard** — one approved command per leg, else `InvalidOperationException`.
2. **End time** — `endTime` is the max `SimTime` across emitted events (`0` if none).
3. **Slice A targetability at command time** — `ProjectTargetabilityAtCommand` (below).
4. **Combat events** — `CombatEventLogProjection.Build(scenario.Log, endTime, targetability)` (the
   DRG-165 contract, with the Slice A snapshot attached for each event's shooter/target leg).
5. **Presentation frame** — `CombatPresentationFrameBridge.Build(scenario.Log, contacts, endTime)`
   over a seeded `SliceAContactFrame` (below).
6. **Maps** — `CombatMapPresenter.Build` at each zoom band, unselected and per selected leg.
7. **Symbol LOD** — `MapLodApplyState.Apply` over Close / Tactical / Theater / Overview.
8. **Fingerprint** — `ComposeFingerprint` folds events, Slice A rows, commands, per-leg views,
   maps, event lines, and LOD clusters into one replay-stable string.

### Slice A acceptance is captured at command time

`ProjectTargetabilityAtCommand` is the load-bearing honesty rule. For each distinct target it
re-projects `TargetabilityAcceptProjection.Project` over the order log **bounded to that target's
first approved-command sim time**, under a `WeaponsFree` / organic-track read context. The rows
are ordered by `ContactId`. A later kill therefore never rewrites the reason a shot was permitted:
the acceptance picture reflects what the commander saw when the command was approved.

### The presentation frame carries the same Slice A facts

Before DRG-369, the harness built the frame from `SliceAContactFrame.Empty`, so `Frame.Contacts`
and `Frame.Events` carried an **empty** Slice A picture even though the harness had just computed
one — the frame and the harness disagreed. The harness now seeds the command-time acceptance into
the frame:

```csharp
var contacts = SliceAContactFrame.Empty with
{
    Targetability = targetability,
    SimTime = endTime,
    SimTick = endTime <= 0 ? 0UL : (ulong)endTime,
};
var frame = CombatPresentationFrameBridge.Build(scenario.Log, contacts, endTime);
```

Two constraints make this safe rather than a rebuild:

- **`SimTime` / `SimTick` must equal `endTime`, not exceed it.** `CombatPresentationFrameBridge.Build`
  discards the entire supplied contact frame (falls back to `Empty`) when the frame is stamped
  *ahead* of the requested sim time — `contacts.SimTime > simTime`, `contacts.SimTick > simTime`, or
  any contact/kill-chain row newer than `simTime`. Stamping both at `endTime` keeps the frame
  accepted while still rejecting a future-dated replay seek.
- **Only `Targetability` is seeded.** The harness holds command-time acceptance, not a live sim
  snapshot, so it does **not** fabricate kill-chain, provenance, or authority rows. `Build`
  recomputes `Frame.Events.Targetability` from the seeded picture, so `Frame.Events.Targetability`
  now equals the harness `Events.Targetability`, and `Frame.Contacts.Targetability` matches
  `TargetabilityAccept` by `(ContactId, TargetId, Disposition)`. A live contact-frame rebuild would
  recompute from a snapshot this harness does not hold — exactly what the comment in the source
  guards against.

## Determinism and isolation

- **Replay-stable:** same `seed` + same `definition` ⇒ identical `Fingerprint`, event order,
  per-zoom map labels, event-history lines, and explanation fingerprints.
- **Real projections only:** the harness never reads `DecisionLog.Engagements` /
  `.EngagementOutcomes` directly — combat facts arrive only through `CombatEventSnapshot`.
- **Off the hot path:** all composition runs *after* the resolver tick, with no
  `DelegationBridge.Tick` changes and no `UnityEngine` dependency.
- **Golden isolation:** the fixture is `synthetic` and references neither `tests/regression` nor
  the Baltic v2 hash `17144800277401907079`; there are no sim/tick-path changes.

## Extending

- **New leg / weapon family:** add it to `slice-b-combat-acceptance.json` (or pass a custom
  `Definition`). `WeaponFamilyId` is a sim-authored string set from
  `SimulationSession.CombatWeaponFamilyId`; every family resolves through the same
  `MvpEngagementResolver` probability-of-kill model (there is no directed-energy physics — see the
  acceptance writeup's laser limitation note).
- **New surface in the evidence:** add it inside `Project` and fold it into `ComposeFingerprint`
  so replay stability still covers it. Do not surface combat truth any way other than through the
  projections.
- **Zoom / LOD bands:** `ZoomBands` and `LodBands` are the fixed arrays at the top of the harness.

## Tests

| Fixture | Covers |
|---------|--------|
| [`CombatVerticalSliceHarnessTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/CombatVerticalSliceHarnessTests.cs) | DRG-166 command→outcome, permitted/refused states, one-correlation-id-per-engagement, zoom/LOD behaviour, non-color discriminators, real-projections-only fence, and that the presentation frame carries the same Slice A targetability facts as the harness (`Commanded_slice_carries_targetability_on_the_presentation_frame`). |
| [`CombatVerticalSliceDeclutterReplayTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/CombatVerticalSliceDeclutterReplayTests.cs) | DRG-170 Tactical/Operational/Theater clutter, non-color recognition of family/affiliation/clearance/outcome, and replay-stable event order + presentation state. |

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 \
  --filter "FullyQualifiedName~CombatVerticalSlice"
```

## See also

- [baltic-replay-harness.md](baltic-replay-harness.md) — the sibling headless runner that proves
  the sim pipeline (this one proves presentation reads it honestly).
- [engagement-explanation-projection.md](engagement-explanation-projection.md) — the DRG-168
  explanation this harness consumes per leg.
- [targetability-accept.md](targetability-accept.md) — the Slice A acceptance projection captured
  at command time.
- [c2-slice-a-contact-frame.md](c2-slice-a-contact-frame.md) — the tick-level `SliceAContactFrame`
  aggregate the frame is seeded from.
- [c2-projection-layer.md](c2-projection-layer.md) — the general read-model rules for all of these
  projections.
