# Slice A contact-detail surface — developer guide

This is the **Combat UX "Slice A" contact-detail path**: the read-only chain that turns one
post-tick simulation picture into the Unity **Contact Detail** panel a commander reads when they
select a hostile contact. It answers *"what do we know about this track, how good is that
knowledge, and what can I safely do next?"* — without ever letting the panel become sim authority.

It sits *above* the [C2 projection layer](c2-projection-layer.md) (which produces the raw
provenance / kill-chain / sensor-to-shooter records) and is a **sibling** of the thin `*Bridge`
façades in [c2-presentation-bridges.md](c2-presentation-bridges.md). Where those façades each wrap
a single projection one-to-one, this slice **composes several projections into one tick-level frame**
and then formats it through **two** presenters into one panel. It landed as the DRG-206 provenance
projection → DRG-180 live surfaces / deep-links waves and is verified against source and pinned by
the tests at the end.

- **Tick frames (adapter, `netstandard2.1`):**
  [`SliceAContactFrame`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SliceAContactFrame.cs)
  + `SliceAContactFrameBridge.Build`,
  [`CombatPresentationFrame`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/CombatPresentationFrame.cs)
  + `CombatPresentationFrameBridge.Build`.
- **Presenters (adapter, `netstandard2.1`):**
  [`SliceAContactPresenter`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/SliceAContactPresentation.cs)
  → `SliceAContactPresentation` (the technical targeting chain), and
  [`SliceAContactLiveSurfaceBinder`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/SliceAContactLiveSurfaceBinder.cs)
  → `SliceAContactLiveSurfaceState` (the always-visible provenance rows + deep-link targets, DRG-180).
- **Provenance data model (core projection):**
  [`ContactProvenance`](../../src/ProjectAegis.Delegation/Projection/ContactProvenance.cs)
  (`ContactProvenanceState` + the freshness / confidence / quality-state enums), produced by
  `ContactProvenanceProjection` (DRG-206).
- **Unity host (MonoBehaviour):**
  [`ContactDetailPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/ContactDetailPanelHost.cs)
  binding the [`ContactDetailPanel.uxml`](../../unity/ProjectAegis/Assets/UI/ContactDetail/ContactDetailPanel.uxml)
  / [`.uss`](../../unity/ProjectAegis/Assets/UI/ContactDetail/ContactDetailPanel.uss) layout.
- **Related:** the raw records are [c2-projection-layer.md](c2-projection-layer.md); the one-to-one
  read-side façades are [c2-presentation-bridges.md](c2-presentation-bridges.md); the write/intent
  side is [c2-command-issuance-runtime.md](c2-command-issuance-runtime.md); the sim tick that
  produces the picture is [delegation-bridge-adapter-boundary.md](delegation-bridge-adapter-boundary.md).

---

## The pipeline at a glance

```
[post-tick] ISimWorldSnapshot + DelegationBridge (DecisionLog)
    │
    ▼  once per tick, on the host (DelegationBridgeHost)
SliceAContactFrameBridge.Build(snapshot, bridge, catalog)  →  SliceAContactFrame        (LastSliceAContacts)
CombatPresentationFrameBridge.Build(log, contacts, simTime) →  CombatPresentationFrame   (LastCombatFrame)
    │
    ▼  once per (frame change | selection change), in ContactDetailPanelHost.Refresh
SliceAContactPresenter.Build(...)          →  SliceAContactPresentation   (technical targeting chain, in a Foldout)
SliceAContactLiveSurfaceBinder.Bind(...)   →  SliceAContactLiveSurfaceState (SRC / CONF / AGE / LAST-KNOWN / COMMS rows + deep-links)
    │
    ▼  ApplyPresentationToLabels()
ContactDetailPanel.uxml labels + two deep-link buttons
```

Both frames are **read-only immutable records** with an `Empty` sentinel; both are built once per
tick and cached. The panel host never re-derives them and never touches the sim, the order log, or
the clock (ADR-010 §2–3, ADR-007, ADR-001).

---

## Design invariants — never break these

These are load-bearing and enforced by the tests below. Preserve them when adding a row, cue, or link.

| Invariant | Rule |
|-----------|------|
| **Read-only / no sim writes** | The frames consume a snapshot + `DecisionLog` (+ catalog) and return immutable records; the presenters format them into immutable state; the host only writes UI labels/classes. Nothing here mutates sim authority, the order log, or the clock. The UI is a *client* (ADR-010 §2–3). |
| **Fail-closed evidence** | An absent/unknown selection projects to `SliceAContactLiveSurfaceState.Empty` / `SliceAContactPresentation.Empty`. Missing provenance renders explicit `UNKNOWN` rows and an `[EVIDENCE:UNKNOWN]` declutter token — **never** a blank row or a silent default. Technical feasibility (a complete sensor-to-shooter chain) is **never** treated as release authority. |
| **Never color-only** | Every provenance cue pairs a text change (`SRC:`/`CONF:`/`AGE:`/`LAST-KNOWN:`/`COMMS:` prefixes, `[EVIDENCE:*]` tokens) with a non-color USS class (`contact-provenance-cue--*` = border-left weight + italic/bold), so state survives on a monochrome display or for a color-blind reader. Do not encode a state in color alone. |
| **Deep-links fail closed** | The two buttons are `SetEnabled(...)` from `ContactExplanationAvailable` / `EngagementExplanationAvailable`; a click handler re-checks availability before acting. No evidence ⇒ disabled button, not a dead-end action. |
| **Off the `DelegationBridge.Tick` hotpath** | Both frames are built by the host *after* `Bridge.Tick(...)` returns, from its result. `DelegationBridge.cs` stays zero-touch (Baltic v2 hash `17144800277401907079` unchanged). |
| **Replay-stable** | `Bind`/`Build` are pure functions of their frame inputs — repeated calls with equal frames return equal state (`Repeated_bind_is_replay_stable`). No wall-clock, no unseeded RNG, no iteration-order sensitivity. |
| **No `UnityEngine` in the binder** | The frames + presenters live in the `netstandard2.1` adapter (no `UnityEngine` reference), so they run under plain `dotnet test`. Only `ContactDetailPanelHost` is a MonoBehaviour. |

---

## The two frames

### `SliceAContactFrame` — the tick-level contact picture

`SliceAContactFrameBridge.Build(snapshot, bridge, catalog?, shooters?)` composes, once per tick:

- `KillChainContactSnapshot` — per-contact kill-chain phase + loss/degradation (folds BDA).
- `ContactProvenanceSnapshot` — the DRG-206 provenance rows (source, confidence, freshness/age,
  last-known, comms/quality), built from the contact picture + comms state + catalog.
- `SensorToShooterSnapshot` — the technical sensor → track → targetability → shooter chain.
- `Contacts` — the `ContactPictureEntry` list (kill-chain-aware, so a lost/degraded contact keeps
  its last-known identity instead of vanishing).
- `Authorities` — per-contact `C2AuthorityProjection`, added **only** when the snapshot supplies
  authoritative, actor-specific evidence for a *registered* eligible shooter (`ISliceAContactAuthoritySource`).

The composition is deliberately **fail-closed on eligibility**: without a supplied authoritative
shooter source, eligibility is unavailable — scenario defaults, historical engage contexts, an
untracked/empty magazine, denied comms, or a stale track never become present-tense clearance
(`Missing_shooter_evidence_never_becomes_clearance`, `Denied_comms_withholds_authority_despite_eligible_shooter`).

### `CombatPresentationFrame` — the one-sim-time combat picture

`CombatPresentationFrameBridge.Build(log, contacts, simTime)` produces the combat events + BDA
assessments + correlated engagement explanations *at or before* `simTime` (a replay seek must not
see future records, so the builder bounds the log first). This is what the engagement deep-link
correlates against. It is shared by the map, the history strip, and this detail panel.

---

## The two presenters

The detail panel has **two independent read surfaces** so a commander never confuses *technical
feasibility* with *authority to fire*:

### 1. `SliceAContactPresenter` → `SliceAContactPresentation` (technical targeting chain)

Formats the kill-chain + provenance + sensor-to-shooter chain + authority projection into six
lines — `PhaseLine`, `ProvenanceLine`, `FreshnessLine`, `ChainLine`, `AuthorityLine`,
`NextActionLine`. It lives behind the `targeting-explanation` **Foldout** ("Technical targeting
chain"). Key honesty rules baked in and pinned by tests:

- A **complete** chain reads `COMPLETE (not release authority)` — it never implies permission
  (`Complete_chain_does_not_infer_release_permission`).
- `APPROVAL REQUIRED` stays distinct from technical targetability
  (`Approval_required_remains_distinct_from_technical_targetability`).
- A **lost** contact stays explainable with a "reacquire — last-known is not a firing solution"
  next-action, even with no active provenance (`Lost_contact_remains_explainable_without_active_provenance`).
- Each broken-chain cause maps to a concrete next action (`Broken_chain_explains_cause_and_next_action`).

### 2. `SliceAContactLiveSurfaceBinder` → `SliceAContactLiveSurfaceState` (live provenance rows, DRG-180)

The always-visible rows at the top of the panel — the DRG-180 addition. `Bind(contactId, frame,
combatFrame)` returns a structured state with, per field, both a **label** and a **non-color cue
class**:

| Field | Label example | Cue when degraded/absent |
|-------|---------------|--------------------------|
| `SourceLine` | `SRC: sensor-1 \| ref source-ref` | `SRC: UNKNOWN — no active source record` |
| `ConfidenceLine` | `CONF: HIGH` | `CONF: UNKNOWN` |
| `AgeLine` | `AGE: 7 ticks \| FRESH` | `AGE: … \| STALE` / `AGE: UNKNOWN` |
| `LastKnownLine` | `LAST-KNOWN: Identified \| target target-1 \| …` | `LAST-KNOWN: LOST @ T …` / `UNKNOWN` |
| `CommsLine` | `COMMS: no degradation reported` | `COMMS: DENIED — current state unknown` / `DEGRADED` / `UNKNOWN` |

Plus:

- **`DeclutterToken`** — a single headless text token summarizing evidence quality, shown as its own
  bold row and hidden when empty: `[EVIDENCE:FRESH]` / `[EVIDENCE:STALE]` / `[EVIDENCE:COMMS-DENIED]`
  / `[EVIDENCE:CATALOG-MISS]` / `[EVIDENCE:UNKNOWN]`. Resolution is ordered: comms-denied →
  catalog-miss → stale → fresh.
- **Cue classes** — `ContactProvenanceCueClasses.{Unknown,Nominal,Degraded,Denied}`, each a
  `contact-provenance-cue--*` USS class (border weight + italic/bold, **not** color-only).
- **Deep-link availability** — `ContactExplanationAvailable` (true when any kill-chain or provenance
  row exists for the contact) and `EngagementExplanationAvailable` (true only when a correlated
  combat event exists), plus `EngagementInspectionKey`.

The binder is strictly fail-closed: unknown/empty `contactId`, or no matching kill-chain **and** no
matching provenance row, returns `Empty` (`Unknown_selection_fails_closed`,
`Missing_provenance_fails_closed_on_live_surface_rows`).

---

## Deep-links

Two buttons under the rows let the commander jump from *evidence* to the *explanation* behind it,
both fail-closed (disabled when unavailable):

- **"Open contact explanation"** — host-local. `ContactDetailPanelHost.OnContactExplanationClicked`
  opens the `targeting-explanation` Foldout and `ScrollView.ScrollTo`s the sensor-to-shooter line.
  No sim/bridge call.
- **"Open engagement explanation"** — cross-panel. The binder resolves the target for the selected
  contact, then scans `CombatPresentationFrame.Events` **from the newest backwards** for the latest
  event on that target and returns its collision-safe key via `CombatMapPresenter.KeyFor(shooterId,
  targetId, correlationId)` (`Engagement_deep_link_targets_latest_correlated_event_only`). On click,
  `ContactDetailPanelHost.OnEngagementExplanationClicked` calls `bridgeHost.InspectCombatEvent(key)`
  → `CombatInspectionState.Select(key)`, which drives the combat map/history selection. With no
  combat events the key is `null` and the button is disabled
  (`Engagement_deep_link_unavailable_without_combat_events`).

---

## How the host wires it up (per tick)

`DelegationBridgeHost` builds both frames once, from the **post-tick** snapshot + log, and exposes
them as read-only properties:

```csharp
// DelegationBridgeHost.RunTick(...), after Bridge.Tick(...) has advanced sim authority:
LastSliceAContacts = SliceAContactFrameBridge.Build(snapshot, Bridge, CatalogReader);
LastCombatFrame    = CombatPresentationFrameBridge.Build(
    Bridge.Orchestrator.DecisionLog, LastSliceAContacts, snapshot.SimTime);
```

`ContactDetailPanelHost.Refresh()` then rebinds **only when something changed** — either frame
reference differs, or the selected contact id differs — so binding is once-per-tick-or-selection,
not once-per-render-frame:

```csharp
var frame       = bridgeHost.LastSliceAContacts;   // cached, immutable
var combatFrame = bridgeHost.LastCombatFrame;       // cached, immutable
if (ReferenceEquals(frame, _lastFrame)
    && ReferenceEquals(combatFrame, _lastCombatFrame)
    && string.Equals(contactId, _lastContactId, StringComparison.Ordinal))
    return;                                          // nothing to do this frame

_sliceA      = SliceAContactPresenter.Build(contactId, frame.KillChain, frame.Provenance,
                   frame.EligibilityAvailable ? frame.Chains : null, authority);
_liveSurface = SliceAContactLiveSurfaceBinder.Bind(contactId, frame, combatFrame);
ApplyPresentationToLabels();                         // labels + cue classes + button enabled-state
```

`LastLiveSurface` / `LastSliceAPresentation` are exposed for headless assertions
(`SliceAContactPanelSmokeTests`, `SliceAContactHostContractTests`).

---

## Extending without breaking replay

1. **Adding a provenance field/row?** Add it to `ContactProvenanceState` + `ContactProvenanceProjection`
   in the [projection layer](c2-projection-layer.md) *first* (with its own headless projection test),
   then surface it in `SliceAContactLiveSurfaceBinder` with a label **and** a non-color cue class, and
   add the `Label` + name constant in `ContactDetailPanelHost` + the row in `ContactDetailPanel.uxml`.
   Never encode the new state in color alone.
2. **Adding a declutter token?** Add the constant to `ContactProvenanceDeclutterTokens`, slot it into
   `ResolveDeclutterToken`'s ordered precedence, and pin the ordering in a binder test.
3. **Adding a deep-link?** Model it on the two existing ones: an availability flag + (for cross-panel
   jumps) a key resolved from a *read-only* frame, wired to a host click handler that re-checks the
   flag. Never add a sim/order-log write.
4. **Absent input must fail closed** to `Empty` / `UNKNOWN`; never blank, never a silent default, and
   never let technical feasibility read as authority.
5. **Before landing:** run the suites below plus the full solution suite (`dotnet test`), confirm
   `ReplayGolden 6/6` and the Baltic v2 hash `17144800277401907079` are unchanged, and confirm ZERO
   `DelegationBridge` Tick hotpath edits. Because the task touches the adapter/presentation boundary,
   follow the `unity-csharp-architect`
   [`checklists/pr-finish.md`](../../production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md)
   and cite ADR-010 §2–3 / ADR-007 / ADR-001 (never Git ADR-018).

---

## Tests that pin this doc

All green as of writing (provenance projection DRG-206 → live surfaces / deep-links DRG-180). The
adapter fixtures are NUnit in the UnityAdapter test assembly (headless `dotnet test`); the smoke test
is a Unity PlayMode test.

| Test file | Covers |
|-----------|--------|
| [`SliceAContactLiveSurfaceBinderTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/SliceAContactLiveSurfaceBinderTests.cs) | Fail-closed unknown selection; the SRC/CONF/AGE/LAST-KNOWN/COMMS rows without color-only state; missing-provenance `UNKNOWN` rows + `[EVIDENCE:UNKNOWN]`; stale declutter token + degraded age cue; engagement deep-link targets the latest correlated event; deep-link unavailable without combat events; replay-stable repeated `Bind`. |
| [`SliceAContactPresentationTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/SliceAContactPresentationTests.cs) | The technical-chain presenter: unknown selection clears all detail; complete chain ≠ release permission; approval-required stays distinct; provenance source/confidence/age/comms without color; broken-chain cause → next action; lost contact stays explainable; stale beats complete chain; replay-stable projection. |
| [`SliceAContactHostContractTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/SliceAContactHostContractTests.cs) | The UXML layout has a `ScrollView`, a `Foldout`, and every named row (incl. `source/confidence/age/last-known/comms/declutter-token` + the two explanation links); the host binds `SliceAContactLiveSurfaceBinder`, exposes `LastLiveSurface`, and gates the deep-link buttons `SetEnabled(...)` on availability; the panel consumes the cached frame (not the live log); the composition root publishes a frame after tick. |
| [`SliceAContactFrameTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/SliceAContactFrameTests.cs) | Frame composition + the fail-closed eligibility contract: null-arg guards; missing shooter evidence never becomes clearance; multi-observer / per-observer BDA identity; deterministic rebuild without mutating the log; stale-but-visible-not-targetable; fire-control for another target does not transfer; tracked-empty magazine never refills; registered shooter without authority stays unknown; technical eligibility never overrides hold-fire; denied comms withholds authority. |
| [`SliceAContactPanelSmokeTests.cs`](../../unity/ProjectAegis/Assets/Tests/SliceAContactPanelSmokeTests.cs) | PlayMode: the panel binds once per frame and clears on unit selection; `LastLiveSurface` shows `UNKNOWN` source/comms when provenance is absent. |

Run just the headless Slice A fixtures:

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~SliceAContactLiveSurfaceBinder|FullyQualifiedName~SliceAContactPresentation|FullyQualifiedName~SliceAContactHostContract|FullyQualifiedName~SliceAContactFrame"
```

---

*Verified against source at the paths above. If you change a frame/presenter signature or a
provenance field, update this doc and [c2-projection-layer.md](c2-projection-layer.md) /
[c2-presentation-bridges.md](c2-presentation-bridges.md) together.*
