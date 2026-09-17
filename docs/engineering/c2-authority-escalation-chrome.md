# C2 authority / ROE / escalation chrome — developer guide

This is the **advisory read-model** that answers "what am I allowed to do to *this* contact,
and why?" for the Slice A command chrome. It is a pure, headless projection stack that folds a
unit's rules-of-engagement (ROE) doctrine, skill-lane authority, and approval requirements into
explicit **authority dispositions**, an **escalation-gate ledger**, and the display lines a
Unity C2 panel binds to.

It sits *beside* the decision-time authorization gate ([autonomy-roe-gating.md](autonomy-roe-gating.md))
— not in front of it. The [`AutonomyGate`](autonomy-roe-gating.md) is the **write path**: it turns a
chosen `Order` into `ExecuteNow` / `QueueForApproval` / `Rejected`. This stack is the **read/explain
path**: it never enqueues an order, never resolves combat, and never clears a gate. Every record it
produces carries `IsOrder=false`. That separation is what lets the chrome tell the operator the truth
about authority every frame without ever perturbing a deterministic run (the Baltic v2 hash
`17144800277401907079` is untouched by anything this stack draws).

Landed across **DRG-209** (the projector + ROE slice), **DRG-228** (the escalation-gate ledger),
and **DRG-182** (the presenter + Unity chrome and the per-contact authority frame). Verified against
source and pinned by the tests at the end.

> **Presentation boundary (ADR-010 §2–3, ADR-007, ADR-001).** Everything here is read-only. Build
> the projection at tick/selection boundaries, not every render frame — the presenter is a
> formatter, not sim authority. The UI is a *client*. See the
> [UnityAdapter README](../../src/ProjectAegis.Delegation.UnityAdapter/README.md).

Related: read-model layer [c2-projection-layer.md](c2-projection-layer.md) ·
adapter seam [c2-presentation-bridges.md](c2-presentation-bridges.md) ·
decision-time gate [autonomy-roe-gating.md](autonomy-roe-gating.md) ·
approval queue [pending-approval-queue.md](pending-approval-queue.md).

---

## The stack, end to end

Four pure stages plus a Unity host. The first three are engine-agnostic (`ProjectAegis.Delegation`,
`netstandard2.1`, exercised by plain `dotnet test`); the presenter is in the adapter; only the host
is `UnityEngine`-dependent.

```text
C2AuthorityProjectionContext            (ROE + lane + approval + track source + fire-control facts)
   │  C2AuthorityProjector.Project(in ctx)          — Delegation/Skills/ (DRG-209)
   ▼
C2AuthorityProjection                   (RoeProjection · C2TargetingAuthority · Actions[6])
   │  EscalationGateProjection.ProjectFromAuthority  — Delegation/EscalationGate/ (DRG-228)
   ▼
EscalationGateSnapshot                  (0..1 EscalationGateRow, IsOrder=false)
   │  C2AuthorityPresenter.Build(contactId, authority) — UnityAdapter/Presentation/ (DRG-182)
   ▼
C2AuthorityPresentation                 (header · badge · ROE line · targeting line · verb rows · gate lines · next action)
   │  bound by
   ▼
AuthorityRoePanelHost (MonoBehaviour)   — unity/.../Scripts/Runtime/ (DRG-182)
```

Per-contact authority projections are pre-built once per tick in
[`SliceAContactFrameBridge.Build`](#per-contact-authorities-sliceacontactframe) and handed to the
host as a keyed dictionary, so the host formats an already-resolved projection rather than
re-running the projector on the render thread.

| Stage | Type | Where |
|-------|------|-------|
| **Inputs** | `C2AuthorityProjectionContext` (+ `AuthorityBasis` via `FromEnvelope`) | [`Skills/C2AuthorityTypes.cs`](../../src/ProjectAegis.Delegation/Skills/C2AuthorityTypes.cs) |
| **Projector** | `C2AuthorityProjector` (static, `Project(in ctx)`) | [`Skills/C2AuthorityProjector.cs`](../../src/ProjectAegis.Delegation/Skills/C2AuthorityProjector.cs) |
| **Projection** | `C2AuthorityProjection` = `RoeProjection` + `C2TargetingAuthority` + `IReadOnlyList<C2AuthorityActionState>` | [`Skills/C2AuthorityTypes.cs`](../../src/ProjectAegis.Delegation/Skills/C2AuthorityTypes.cs) · [`Skills/RoeProjection.cs`](../../src/ProjectAegis.Delegation/Skills/RoeProjection.cs) |
| **Gate ledger** | `EscalationGateProjection` → `EscalationGateSnapshot`/`EscalationGateRow` | [`EscalationGate/`](../../src/ProjectAegis.Delegation/EscalationGate/) |
| **Presenter** | `C2AuthorityPresenter` (static) → `C2AuthorityPresentation` | [`Presentation/C2AuthorityPresentation.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/C2AuthorityPresentation.cs) |
| **Host** | `AuthorityRoePanelHost` (+ summary line on `EngageExplainPanelHost` / `PendingApprovalPanelHost`) | [`Scripts/Runtime/`](../../unity/ProjectAegis/Assets/Scripts/Runtime/AuthorityRoePanelHost.cs) |

---

## Inputs — `C2AuthorityProjectionContext`

The projector is a pure function of this context; it never reads `DelegationBridge`,
`SimulationSession`, the order log, or the clock.

| Field | Type | Meaning |
|-------|------|---------|
| `Roe` | `RoeLevel` | `HoldFire` / `WeaponsTight` / `WeaponsFree` (from `ProjectAegis.Sim.Policy`). |
| `Lane` | `SkillLane` | `Read` / `Propose` / `Submit` — the skill boundary the actor is acting through. |
| `RequiredApproval` | `RequiredApproval` | `None` / `Operator` / `WeaponsRelease` — an approval hint supplied by the envelope. |
| `TrackSource` | `TrackSource` | `Organic` / `DatalinkShared` / `FusedWithoutOrganicFc` / `Unknown`. Shared SA is **not** fire-control (ADR-018). |
| `FireControlSatisfied` | `bool` | Whether an organic fire-control track is held. |
| `CommandId` | `string?` | The command being inspected; `"engage"` (case-insensitive) is the fire verb. |
| `HumanControlled` | `bool` | Whether a human owns the actor (default `true`). Gates `Abort` / `Retask`. |

`C2AuthorityProjectionContext.FromEnvelope(basis, lane, requiredApproval, commandId, roeOverride?)`
builds a context from an [`AuthorityBasis`](../../src/ProjectAegis.Delegation/Skills/C2AuthorityTypes.cs)
(the DRG-196 skill-contract evidence: policy snapshot id, ROE label, EMCON, track source,
fire-control) — parsing the ROE label with `C2AuthorityProjector.ParseRoeLabel` unless overridden.
`ParseRoeLabel` is tolerant: blank/unknown → `WeaponsFree`, `"HOLD…"` → `HoldFire`, `"TIGHT…"` →
`WeaponsTight` (underscores normalized to spaces first).

---

## Projector rules (`C2AuthorityProjector.Project`)

`Project` produces three things: an **ROE slice**, a **targeting leg**, and an ordered array of six
**per-verb dispositions**. Every disposition is explicit — one of `Permitted`, `Withheld`, or
`ApprovalRequired` — with a stable machine-readable reason code. There is never a silent deny.

### ROE slice → `RoeProjection`

| `ctx.Roe` | `TargetingDisposition` | `EngageAllowedByRoe` | Reason code |
|-----------|------------------------|----------------------|-------------|
| `WeaponsFree` | `Permitted` | `true` | *(none)* |
| `WeaponsTight` | `Withheld` | `false` | `WeaponsTight` |
| `HoldFire` | `Withheld` | `false` | `RoeHoldFire` |

`RoeProjection.FormatRoeLabel` gives the stable UI label: `HOLD_FIRE` / `WEAPONS_TIGHT` /
`WEAPONS_FREE`.

### Targeting leg → `C2TargetingAuthority` (first matching rule wins)

1. **Shared SA is not fire-control.** `TrackSource ∈ { DatalinkShared, FusedWithoutOrganicFc }` →
   `Withheld` · `SharedTrackNoRelease` (no pending approval).
2. **No organic fire-control** in an engage context (`CommandId == "engage"` or lane is
   `Propose`/`Submit`) and `!FireControlSatisfied` → `Withheld` · `NO_FIRE_CONTROL`.
3. **ROE forbids** (`!EngageAllowedByRoe`) → `Withheld` · the ROE reason code above.
4. **Approval gate** (`ResolvePendingApproval` returns non-null) → `ApprovalRequired` ·
   `WeaponsReleaseRequired` (weapons-release) or `ApprovalRequired` (operator) · with the pending
   `RequiredApproval` attached.
5. Otherwise → `Permitted` (no reason, no pending approval).

`ResolvePendingApproval` returns `WeaponsRelease` when the command is `engage`; else the explicit
`ctx.RequiredApproval` when it is `Operator`/`WeaponsRelease`; else `Operator` when the lane is
`Propose`/`Submit`; else `null`. A blank `CommandId` yields no pending approval.

### Per-verb dispositions → `C2AuthorityActionState[]`

Ordered `Observe(0) → Recommend(1) → Approve(2) → Engage(3) → Abort(4) → Retask(5)`.

| Verb | Rule |
|------|------|
| **Observe** | Always `Permitted`. |
| **Recommend** | `Withheld` · `LANE_SUBMIT_NO_RECOMMEND` when lane is `Submit`; else `Permitted`. |
| **Approve** | `Withheld` · `ApprovalRequired` when `CommandId` is blank; else mirrors the targeting leg (`ApprovalRequired` / `Withheld` carry its reason; `Permitted` otherwise). |
| **Engage** | Mirrors the targeting leg for `Withheld`/`ApprovalRequired`; `Permitted` only when ROE allows **and** `TrackSource == Organic` **and** `FireControlSatisfied`; else `Withheld` · `NO_FIRE_CONTROL`. |
| **Abort** | `Permitted` when `HumanControlled`; else `Withheld` · `NOT_HUMAN_CONTROL`. |
| **Retask** | `Withheld` · `NOT_HUMAN_CONTROL` when not human-controlled. For a non-engage/blank command: `ApprovalRequired` if operator approval is pending, else `Permitted`. For the engage command: mirrors the targeting withhold, else `Permitted`. |

Reason-code constants live on `C2AuthorityProjector` (`ReasonWeaponsTight`, `ReasonRoeHoldFire`,
`ReasonNoFireControl`, `ReasonSharedTrackNoRelease`, `ReasonWeaponsReleaseRequired`,
`ReasonApprovalRequired`, `ReasonNotHumanControlled`, `ReasonLaneSubmit`) — several re-exported from
`SkillEnvelopeValidator` so the chrome and the skill validator speak the same vocabulary.

---

## Escalation-gate ledger (`EscalationGateProjection`, DRG-228)

The gate ledger reduces an authority projection to **at most one** advisory gate row per contact —
the single reason escalation/approval is required — using a stable named code. Every row and
snapshot is `IsOrder=false`.

| Precedence | Condition | `GateCode` | `RequiredAuthority` |
|-----------|-----------|-----------|---------------------|
| 1 | ROE `HoldFire` | `HOLD_FIRE` | `None` |
| 2 | ROE `WeaponsTight` | `WEAPONS_TIGHT` | `None` |
| 3 | Targeting `ApprovalRequired` | `HIGHER_HQ` | `WeaponsRelease` or `Operator` (from the pending approval) |
| — | Otherwise (weapons-free + permitted targeting) | *(no row → `EscalationGateSnapshot.Empty`)* | |

Codes are the constants in [`EscalationGateCode`](../../src/ProjectAegis.Delegation/EscalationGate/EscalationGateCode.cs).
Three entry points:

- `Project(EscalationGateInput?)` — projects one row, **running the projector itself** from
  `input.AuthorityContext`.
- `Project(IReadOnlyList<EscalationGateInput>?)` — many rows, sorted by `ContactOrOrderId` ordinal
  (replay-stable); blank ids skipped, empty input → `Empty`.
- `ProjectFromAuthority(contactOrOrderId, authority)` — **DRG-182 presentation bind.** Projects a
  row from an **already-resolved** `C2AuthorityProjection` *without re-running*
  `C2AuthorityProjector`, so it preserves the exact ledger truth the frame already computed. This is
  the path the presenter uses; it keeps the panel and the frame from disagreeing and avoids a
  redundant projection on the hot selection path.

---

## Presenter (`C2AuthorityPresenter`, DRG-182)

`C2AuthorityPresenter.Build(contactId, authority)` formats a projection into the immutable
`C2AuthorityPresentation` the host renders. A blank `contactId` or `null` authority returns
`C2AuthorityPresentation.Empty` (a cleared panel with the "Select a contact…" prompt).

| Presentation field | Content |
|--------------------|---------|
| `HeaderLine` | Constant `"COMMAND AUTHORITY"`. |
| `AdvisoryBadge` | Constant `"ADVISORY ONLY · IsOrder=false"` — the non-negotiable label. |
| `RoeLine` | `ROE: {label} · {PERMITTED\|WITHHELD\|APPROVAL REQUIRED}` (+ reason code). |
| `TargetingLine` | `Targeting: {disposition}` (+ reason code, + `requires {approval}`). |
| `VerbRows` | One `C2AuthorityVerbRow(VerbLabel, DispositionLabel, ReasonCode?)` per action, ordered by verb. |
| `GateLines` | One line per gate row (`Gate {code} · {reason} · requires {authority} · IsOrder=false`), or `"none active…"` when the ledger is empty. |
| `NextActionLine` | Operator guidance derived from the targeting disposition (below). |

Disposition labels are fixed: `Permitted → PERMITTED`, `ApprovalRequired → APPROVAL REQUIRED`,
`Withheld → WITHHELD`. `NextActionLine` reflects intent, never authority to fire:

- `ApprovalRequired` → "Request {approval} approval through the command workflow; this panel does
  not clear the gate."
- `Withheld` → "Review the authority restriction ({reason}) before engaging."
- Permitted but `Engage` not permitted → states the engage disposition + reason.
- Fully permitted → "Authority projection permits engage intent; execution still revalidates
  eligibility and ROE."

`FormatSummaryLine(authority)` gives a compact one-liner —
`ROE {label} · Targeting {disposition} · Approval {pending}` (or `"Authority: UNKNOWN — no
projection bound"` when null) — used by the EngageExplain and PendingApproval hosts to show the same
authority truth inline.

---

## Per-contact authorities (`SliceAContactFrame`)

The host does not build projections itself. The tick-level frame that carries this map — and folds
the kill-chain picture, provenance, and sensor-to-shooter chains alongside it — is documented in full
in [c2-slice-a-contact-frame.md](c2-slice-a-contact-frame.md). Once per tick,
[`SliceAContactFrameBridge.Build`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SliceAContactFrame.cs)
composes the Slice A read model and, for each sensor-to-shooter chain, resolves an authority
projection into `SliceAContactFrame.Authorities` — an `IReadOnlyDictionary<string,
C2AuthorityProjection>` keyed by contact id (ordinal). It **fails closed**: a contact is only added
when an eligible, registered shooter exists **and** the snapshot implements
`ISliceAContactAuthoritySource` and supplies a context with a known `TrackSource`. The supplied
context is finalized to the fire path (`Lane = Propose`, `RequiredApproval = WeaponsRelease`,
`CommandId = "engage"`, and `FireControlSatisfied` **and**-ed with a complete chain and a fresh
in-comms track) before projection. Missing evidence means no entry — not a permissive default.

`DelegationBridgeHost.LastSliceAContacts` exposes the frame; the host reads
`frame.Authorities[SelectedContactId]` and `frame.SimTick` to refresh only when the selection or
tick changes.

### Host wiring (`AuthorityRoePanelHost`)

`AuthorityRoePanelHost` is a `UIDocument` MonoBehaviour that queries the UXML panel
(`Assets/UI/AuthorityRoe/AuthorityRoePanel.uxml`) by element name — `authority-roe-header` /
`-badge` / `-roe` / `-targeting` / `-next` labels plus `-verbs` / `-gates` `ListView`s under
`authority-roe-root`. Each `LateUpdate` it calls `Refresh()`, which short-circuits unless the
selected contact id or sim tick changed, then `C2AuthorityPresenter.Build(...)` and applies the
lines. `Apply(presentation)` is a headless/test entry point, and `LastPresentation` exposes the
last projection for assertions. The panel is registered in
[`DelegationSmokeSceneBuilder`](../../unity/ProjectAegis/Assets/Editor/DelegationSmokeSceneBuilder.cs)
(`Build` and `EnsureUiMaturityHostsOnOpenScene`). `EngageExplainPanelHost` and
`PendingApprovalPanelHost` render `FormatSummaryLine` into an `…-authority` label so the authority
truth also appears alongside the engage explain and the approval queue.

---

## Invariants — never break these

| Invariant | Rule |
|-----------|------|
| **Advisory only / `IsOrder=false`** | Nothing here enqueues an order, resolves combat, clears a gate, or refills a magazine. The badge and every gate row assert it. |
| **Explicit disposition, never a silent deny** | Every verb and gate carries a `Permitted`/`Withheld`/`ApprovalRequired` + a stable reason code. Unknown/blank inputs fail *closed* with a code, never a throw. |
| **Pure & deterministic** | The projector/gate/presenter read only their arguments — no wall-clock, RNG, `DelegationBridge`, `SimulationSession`, or order log. Same inputs ⇒ identical presentation (pinned by the replay-stability test). |
| **Off the `DelegationBridge.Tick` hotpath** | Projections are built at the tick/selection boundary in the frame bridge and formatted in the host; `DelegationBridge.cs` stays zero-touch (Baltic v2 hash `17144800277401907079` unchanged). |
| **Fail closed on missing evidence** | `SliceAContactFrameBridge` only surfaces an authority when a registered eligible shooter and authoritative source exist; absence is not permission. |
| **Presentation ≠ authority** | The panel explains authority; it does not confer it. Execution still revalidates eligibility and ROE through the [engage path](engagement-pipeline.md) and [autonomy gate](autonomy-roe-gating.md). |
| **No `UnityEngine` in the pure layers** | Projector, ROE slice, gate ledger, and presenter target `netstandard2.1` and run under plain `dotnet test`. |

---

## Common pitfalls

- **Re-running the projector in the host.** Use `ProjectFromAuthority` / the frame's `Authorities`
  map, not `EscalationGateProjection.Project(new EscalationGateInput(...))`, on the render path —
  re-projecting can drift from the frame's finalized context and wastes work every selection.
- **Treating shared-SA / fused tracks as fire-control.** Rule 1 of the targeting leg withholds them
  (`SharedTrackNoRelease`). Do not "helpfully" permit engage off a datalink-only track.
- **Inventing a permissive default.** The frame fails closed; a missing authority entry means the
  panel clears, it does not imply `Permitted`.
- **Formatting in the projector.** USS/label strings live in the presenter/host; the projector and
  gate produce semantic dispositions + reason codes only.
- **Colour-only gate signalling.** Gate/disposition state is always carried in text
  (`WITHHELD` / `APPROVAL REQUIRED` / gate code), never colour alone (accessibility, ADR-010).

---

## Extending without breaking replay

1. **New authority verb?** Add it to `C2AuthorityActionKind` (append an enum value — the presenter
   orders by `(int)action.Action`), a `Project{Verb}` arm in the projector, and a fixture case.
2. **New escalation reason?** Add a `EscalationGateCode` constant and a precedence branch in
   `ResolveGateRow`; keep the "no row when permitted" default. Update the reason-code table above.
3. **New input fact?** Extend `C2AuthorityProjectionContext` with a defaulted parameter (additive,
   non-breaking), thread it through `FromEnvelope`, and add a projector rule + test.
4. **New display line?** Add it to `C2AuthorityPresentation` + the presenter formatter and bind a
   UXML element in the host; do not move formatting into the projector.
5. **Before landing**, run the suites below plus the full solution suite (`dotnet test`), confirm
   `ReplayGolden 6/6`, `PlayModeSmoke ≥20/20`, the Baltic v2 hash is unchanged, and ZERO
   `DelegationBridge` Tick hotpath edits. Adapter/presentation work follows the
   `unity-csharp-architect` [`checklists/pr-finish.md`](../../production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md).

---

## Tests that pin this doc

All green as of writing (DRG-209 projector · DRG-228 gate · DRG-182 presenter/chrome):

| Test file | Covers |
|-----------|--------|
| [`Skills/C2AuthorityProjectorTests.cs`](../../src/ProjectAegis.Delegation.Tests/Skills/C2AuthorityProjectorTests.cs) | ROE slice, targeting-leg precedence, per-verb dispositions, `ParseRoeLabel`, `FromEnvelope`. |
| [`EscalationGate/EscalationGateProjectionTests.cs`](../../src/ProjectAegis.Delegation.Tests/EscalationGate/EscalationGateProjectionTests.cs) | Gate-code precedence, ordinal sort, empty/blank handling, and `ProjectFromAuthority` reuse (identical rows to the context path). |
| [`Presentation/C2AuthorityPresentationTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/C2AuthorityPresentationTests.cs) | Empty presentation, weapons-tight/approval-required/weapons-free surfacing, `FormatSummaryLine`, and replay stability. |
| [`Bridge/PlayModeSmokeHarnessTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs) | `AuthorityRoePanelHost` is wired in `Build()` / `EnsureUiMaturityHosts` with the `Assets/UI/AuthorityRoe/AuthorityRoePanel.uxml` asset. |

Run just this subsystem:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~Skills.C2AuthorityProjector|FullyQualifiedName~EscalationGate.EscalationGateProjection"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~Presentation.C2AuthorityPresentation|FullyQualifiedName~PlayModeSmokeHarness"
```

---

## See also

| Topic | Doc |
|-------|-----|
| Decision-time ROE / autonomy gate (the write path) | [autonomy-roe-gating.md](autonomy-roe-gating.md) |
| Human-in-the-loop approval queue | [pending-approval-queue.md](pending-approval-queue.md) |
| Tick-level Slice A read-model aggregate that builds the `Authorities` map | [c2-slice-a-contact-frame.md](c2-slice-a-contact-frame.md) |
| C2 read-model layer & `Projection → Binder → State` | [c2-projection-layer.md](c2-projection-layer.md) |
| Adapter seam (bridges, `IC2PresentationFeed`, host refresh) | [c2-presentation-bridges.md](c2-presentation-bridges.md) |
| Engage gate chain that actually authorizes fire | [engagement-pipeline.md](engagement-pipeline.md) |
| Presentation boundary decisions | [ADR-010](../architecture/adr-010-headless-first-command-driven-ui.md) · [ADR-007](../architecture/adr-007-c2-map-presentation.md) |

---

*Verified against source at the paths above (DRG-209 / DRG-228 / DRG-182). If you change a projector
rule, gate code, or presenter line, update this doc and its `See also` siblings together.*
