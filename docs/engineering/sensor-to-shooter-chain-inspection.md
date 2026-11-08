# Sensor-to-shooter chain inspection — developer guide

The **sensor-to-shooter chain** is the read-only kill-chain inspector: for a selected hostile
contact it answers *"can this contact technically be engaged right now, and if not, which link is
broken?"* by decomposing the kill chain into four inspectable links —
**sensor → track → targetability → eligible-shooter** — and naming the break cause when any link
fails.

It is split cleanly across the two read-side layers this repo already documents:

- a pure, engine-agnostic **projection** (`SensorToShooterProjection`, **DRG-207**) in the delegation
  core that folds the kill-chain contact state + catalog engage envelope into an immutable
  `SensorToShooterSnapshot`; and
- a Unity **presentation panel** (`SensorToShooterPresenter` / `SensorToShooterPanelHost`, **DRG-181**)
  that formats one contact's chain into inspectable text rows.

Everything here is **presentation-only**. The panel *inspects* the chain; it never issues fire
orders and its "COMPLETE" verdict is **technical eligibility, not release authority** (ADR-010 §2–3,
ADR-007, ADR-001). It is the read-side counterpart of the
[c2-sensor-to-shooter-pairing skill](../../production/docs/skills/c2-sensor-to-shooter-pairing/SKILL.md)
(`c2.pairing.recommend`), which ranks pairs for an *authorized* C2 model but still stops before fire.

Related: the projection contract these follow is [c2-projection-layer.md](c2-projection-layer.md); the
adapter-seam pattern is [c2-presentation-bridges.md](c2-presentation-bridges.md); the kill-chain
contact state it consumes comes from the [detection pipeline](detection-pipeline.md) /
[engagement pipeline](engagement-pipeline.md); the shooter-eligibility preview reuses the
[abort-reason catalog](abort-reason-catalog.md).

---

## Where it lives

| Layer | Type | Path |
|-------|------|------|
| Domain model | `SensorToShooterSnapshot` / `SensorToShooterChain` / `SensorToShooterChainLink` + `SensorToShooterLinkKind` / `SensorToShooterBreakCause` enums | [`SensorToShooterTypes.cs`](../../src/ProjectAegis.Delegation/SensorToShooter/SensorToShooterTypes.cs) |
| Projection | `SensorToShooterProjection` (`Project` + `ComputeFingerprint`) | [`SensorToShooterProjection.cs`](../../src/ProjectAegis.Delegation/SensorToShooter/SensorToShooterProjection.cs) |
| Tick read model | `SliceAContactFrame.Chains` (+ `EligibilityAvailable`) | [`SliceAContactFrame.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SliceAContactFrame.cs) |
| Presenter | `SensorToShooterPresenter.Build` → `SensorToShooterPresentation` | [`SensorToShooterPresentation.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/SensorToShooterPresentation.cs) |
| Unity panel host | `SensorToShooterPanelHost` (MonoBehaviour) | [`SensorToShooterPanelHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/SensorToShooterPanelHost.cs) |
| UI layout / style | `SensorToShooterPanel.uxml` / `.uss` | [`Assets/UI/SensorToShooter/`](../../unity/ProjectAegis/Assets/UI/SensorToShooter/) |

---

## The four links

`SensorToShooterProjection.Project(...)` builds one `SensorToShooterChain` per kill-chain contact,
each with exactly four links in this fixed order. A link is `IsLinked` or carries a
`SensorToShooterBreakCause`; the **first** broken link (top-down) becomes the chain's
`PrimaryBreakCause`, and a chain is `IsComplete` only when all four are linked.

| # | `SensorToShooterLinkKind` | Linked when | Typical break cause |
|---|---------------------------|-------------|---------------------|
| 1 | `Sensor` | contact not `Lost` **and** detection captured | `LostSensor` (`"lost sensor"`) |
| 2 | `Track` | not `Lost`/`Stale` **and** track continuous | `StaleTrack` (`"stale track"`) |
| 3 | `Targetability` | targetable, not degraded/stale/lost | `NoFireControl` (`"no FC"`), `StaleTrack`, `DegradedTrack` (`"degraded track"`) |
| 4 | `EligibleShooter` | a candidate shooter clears ammo + `EngagePreviewProjection.CanFire` | `NoEligibleShooter` (`"no eligible shooter"`); detail carries the abort code (e.g. `NO_AMMO`) |

Notes verified against source:

- **Downstream links inherit an upstream break.** If targetability is broken, the shooter link is
  reported broken with the *same* cause/detail — the projection does not fabricate shooter facts on
  top of a broken track.
- **BDA degradation keeps the track but breaks targetability.** A `DegradedL1`/`DegradedL2` contact
  stays track-linked and fails targetability with `DegradedTrack`, so the panel says "track quality"
  is the problem rather than claiming the track is gone.
- **Shooter eligibility is real, not optimistic.** `BuildEligibleShooterLink` orders candidates by
  `ShooterUnitId` (ordinal), applies the `CatalogEngageEnvelope`, rejects candidates without
  sufficient ammo for the salvo (`RoundsRemaining >= max(1, SalvoSize)`), and only marks the link
  linked when `EngagePreviewProjection.Project(...).CanFire` is true. The **first** eligible shooter
  wins; otherwise the last abort code (`AbortPreviewCode`, e.g. `NO_AMMO`) is surfaced as detail.

### The tick frame: `SliceAContactFrame` and fail-closed eligibility

The Unity host does not call `SensorToShooterProjection` directly. Once per tick,
`SliceAContactFrameBridge.Build(snapshot, bridge, catalog)` composes the Slice A read model
(`DelegationBridgeHost.LastSliceAContacts`) after `Bridge.Tick(...)` returns, and the chain snapshot
rides on `SliceAContactFrame.Chains`.

The load-bearing detail is `SliceAContactFrame.EligibilityAvailable`. `ISimWorldSnapshot` has **no
per-shooter side, geometry, or commitment facts**, so without an authoritative
`ISensorToShooterShooterSource` the frame **fails closed**: `EligibilityAvailable` is `false` and the
presenter withholds shooter verdicts instead of inventing them from scenario defaults or historical
engage contexts. When a live source is present it is wrapped in a `LiveCandidateGuard` that drops
candidates that are self-targeted, dead, unregistered, or not launch-ready, and rebinds every
surviving candidate's round count to the live magazine ledger — a missing ledger entry yields **zero
rounds**, not permission to seed from configuration, and the projection's ammo check
(`RoundsRemaining >= max(1, SalvoSize)`) then rejects the empty/under-salvo candidate.

---

## Determinism / replay safety — never break these

The whole path obeys the read-only projection contract ([c2-projection-layer.md](c2-projection-layer.md)).
It is pinned by tests and by the Baltic v2 replay hash `17144800277401907079`.

| Invariant | Rule |
|-----------|------|
| **Read-only, no sim writes** | The projection folds an already-recorded kill-chain snapshot + catalog reads into an immutable `record`. It never writes the sim, order log, catalog, or clock. The panel host is a `MonoBehaviour` client, not sim authority. |
| **Replay-stable fingerprint** | `ComputeFingerprint` emits a canonical string (`sts:c=<n>|contact,target,observer,complete,cause,linkCount;<per-link>…`) using invariant culture and ordinal ordering only. Contacts are projected in `StringComparer.Ordinal` order. Identical inputs ⇒ identical fingerprint. |
| **Fail closed, never fabricate** | No authoritative shooter source ⇒ `EligibilityAvailable == false` ⇒ shooter facts withheld (`UNKNOWN`), not guessed. |
| **Technical eligibility ≠ release authority** | A `COMPLETE` chain states only that the four links are technically satisfied. It never implies weapons release; the next-action text says the panel "does not issue fire orders". |
| **Off the `DelegationBridge.Tick` hotpath** | `SliceAContactFrameBridge.Build` runs *after* `Bridge.Tick(...)`; `DelegationBridge.cs` stays zero-touch (v2 hash unchanged). |
| **No `UnityEngine` in the core/adapter** | The projection and presenter target `netstandard2.1` and are exercised by plain `dotnet test`; only `SensorToShooterPanelHost` (under `#if UNITY_5_3_OR_NEWER`) references `UnityEngine`. |

---

## The presenter: `SensorToShooterPresenter.Build`

`SensorToShooterPresenter.Build(contactId, snapshot, eligibilityAvailable)` turns one contact's chain
into a `SensorToShooterPresentation` — a flat record of display lines (`ContactIdLine`, `StatusLine`,
one line per link, `NextActionLine`, `Fingerprint`, and a `Links` list). Its decision table:

| Input | Result |
|-------|--------|
| `contactId` null/empty | `SensorToShooterPresentation.Empty` — cleared "Select a contact…" chrome |
| `eligibilityAvailable == false`, or `snapshot` null/empty, or contact not found | **UNKNOWN** — contact id echoed, all links `UNKNOWN`, next action: *"Obtain current sensor-to-shooter facts before relying on technical eligibility."* |
| chain found, `IsComplete` | **COMPLETE (technical eligibility only — not release authority)**; four `LINKED` rows; next action routes intent through the command workflow (no fire orders here) |
| chain found, broken | **BROKEN — `<primary cause label>`**; next action is cause-specific (see below) |

The broken-chain next action is a fixed, plain-language remediation per `PrimaryBreakCause`:

| `SensorToShooterBreakCause` | Next action gist |
|-----------------------------|------------------|
| `LostSensor` | Reacquire the contact with a reporting sensor |
| `StaleTrack` | Refresh the sensor report / revalidate track custody |
| `NoFireControl` | Acquire a fire-control-quality track |
| `NoEligibleShooter` | Inspect shooter readiness, envelope, range, ammunition |
| `DegradedTrack` | Improve track quality, revalidate targetability |

---

## How the Unity host wires it up

`SensorToShooterPanelHost` is a `[RequireComponent(typeof(UIDocument))]` MonoBehaviour bound to
[`SensorToShooterPanel.uxml`](../../unity/ProjectAegis/Assets/UI/SensorToShooter/SensorToShooterPanel.uxml).
It reads two things off its `DelegationBridgeHost`: `SelectedContactId` and the cached
`LastSliceAContacts` frame. Each `LateUpdate` it:

1. hides the panel when nothing is selected (`showPanel && !string.IsNullOrEmpty(contactId)`);
2. computes the frame fingerprint and **skips the rebuild** when the frame reference, contact id, and
   fingerprint are all unchanged (`ReferenceEquals` on the cached frame) — an unchanged tactical
   picture never re-renders panel text;
3. otherwise calls `SensorToShooterPresenter.Build(...)`, maps the presentation through
   `SensorToShooterPanelBinder.Bind(...)`, and pushes the resulting lines onto the labels.

A headless/test path (`Apply(SensorToShooterPresentation)`) bypasses the frame lookup but uses the
same binder-backed label path.

The panel is registered in the delegation smoke scene by
[`DelegationSmokeSceneBuilder`](../../unity/ProjectAegis/Assets/Editor/DelegationSmokeSceneBuilder.cs)
— both in `Build(...)` (`CreatePanelHost<SensorToShooterPanelHost>("SensorToShooter", …)`) and in
`EnsureUiMaturityHostsOnOpenScene()` (`EnsurePanelHostIfMissing<…>`), so an already-open scene
back-fills the host.

---

## Extending without breaking replay

1. **New link or break cause?** Add the enum value to `SensorToShooterTypes.cs`, give it a stable
   label in `SensorToShooterBreakCauseLabels.Format` (empty string for `None`), and extend
   `SensorToShooterProjection.Build*Link` — keep the top-down "first broken link wins" ordering.
2. **New shooter-eligibility rule?** Put it in `BuildEligibleShooterLink` (or the `LiveCandidateGuard`
   for live-world filtering), reusing `CatalogEngageEnvelope` / `EngagePreviewProjection` — do not add
   a code path to `DelegationBridge.Tick`.
3. **New presenter line?** Add it to `SensorToShooterPresentation` and the `.uxml` (a
   `name="sts-…"` Label) together; the host contract test asserts the layout exposes each named row.
4. **If you change the fingerprint format**, you are changing a replay-stable surface — update the
   projection test and confirm the Baltic v2 hash `17144800277401907079` and `ReplayGolden 6/6` are
   unchanged.
5. **Before landing**, follow the `unity-csharp-architect`
   [`checklists/pr-finish.md`](../../production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md)
   (this path touches the presentation boundary).

---

## Tests that pin this doc

All green as of writing (DRG-207 projection landed in #580; DRG-181 chrome in #625):

| Test file | Cases | Covers |
|-----------|-------|--------|
| [`SensorToShooterProjectionTests.cs`](../../src/ProjectAegis.Delegation.Tests/SensorToShooter/SensorToShooterProjectionTests.cs) | 9 | Complete chain; stale-track / no-FC / lost-sensor / no-eligible-shooter / BDA-degraded break causes; zero-rounds and below-salvo shooter rejection; replay-stable fingerprint. |
| [`SensorToShooterPresentationTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/SensorToShooterPresentationTests.cs) | 12 | Empty selection clears; unknown contact; complete chain lists four links without release authority; each break cause → next action; fail-closed on missing eligibility; fingerprint matches projection + repeated-projection stability. |
| [`SensorToShooterHostContractTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/SensorToShooterHostContractTests.cs) | 4 | Host consumes the cached frame + fingerprint (not a live log) and skips rebuild on unchanged frames; `SensorToShooterPanelBinder.Bind` exposes every presenter line for the host; `.uxml` exposes the four link rows + next action; scene builder wires the host. |
| [`PlayModeSmokeHarnessTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs) (`…includes_sensor_to_shooter_host`) | 1 | The smoke scene builder registers the host in both `Build(...)` and `EnsureUiMaturityHosts`. |

Run just this subsystem:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~SensorToShooter"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~SensorToShooter"
```

---

*Verified against source at the paths above. If you change a link/break-cause enum, the fingerprint
format, or a presenter line, update this doc together with
[c2-projection-layer.md](c2-projection-layer.md) and
[c2-presentation-bridges.md](c2-presentation-bridges.md).*
