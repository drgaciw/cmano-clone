# Domain-abort explain chrome — the Engage Explain panel's "which domain blocked this?" line (DRG-265 / REQ-18 DOM-02)

> **ADR:** this is the **presentation / read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> — a binder is a **client**, not sim authority), with
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md) (C2 presentation) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).
> Do **not** cite Git ADR-018 (that is sensor side-picture / datalink). Combat UX; the binder is DRG-265 /
> REQ-18 DOM-02.

The **domain-abort explain** binder answers one narrow question for the Engage Explain panel: *was this
engagement blocked by a **cross-domain** rule (an air/surface/subsurface/land/mine/facility aspect block, or a
"no domain solution"), and if so which one?* It folds the already-projected refusal facts for the current
selection into a single labelled, non-color-coded chrome row — `DOMAIN: BLOCKED — <code>`, `DOMAIN: CLEAR`, or
the fail-closed `DOMAIN: —` — that the Engage Explain panel drops next to its status/reason lines. It lives in a
single file under
[`ProjectAegis.Delegation.UnityAdapter/Presentation/`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/DomainAbortExplainBinder.cs):

1. `DomainAbortExplainBinder` — the pure static fold (`Bind`, `BindRows`).
2. `DomainAbortExplainState` — the immutable result (`StateLine`, `Code`, `IsBlocked`, `DeclutterToken`, `CueClass`, `Fingerprint`) + its `Empty` singleton.
3. `DomainAbortExplainRow` — one per-row binding (`ElementName`, `Text`, `CueClass`).
4. `DomainAbortCueClasses` / `DomainAbortDeclutterTokens` — the paired USS cue classes and the text declutter tokens (never color-only state).

It is strictly a **read-model**: it reads only the [engage-explain](engage-explain-contract-projection.md) DTO,
an engage preview, and a [combat-detail](slice-a-contact-detail-surface.md) card that other projections already
produced. It **does not** evaluate the combat-domain validators itself, recompute an engagement, query the sim,
or issue orders, and it is **not** part of the Baltic v2 `SimWorldHash`. It never invents a code — the only
reason strings it will surface are the seven domain codes below.

---

## Inputs — explain + preview + optional combat-detail

```csharp
DomainAbortExplainState Bind(
    EngageExplain? explain,
    EngagePreview? preview = null,
    CombatDetailPresentation? detail = null)
```

| Input | Role |
|-------|------|
| `EngageExplain?` | The plain-language engage refusal ([`EngageExplainProjection.Project`](../../src/ProjectAegis.Delegation/Projection/EngageExplainProjection.cs)): `StatusLine`, `ReasonCode`, `ReasonPlain`, `IsBlocked`. |
| `EngagePreview?` | The live engage preview ([`EngagePreviewProjection`](../../src/ProjectAegis.Delegation/Projection/EngagePreviewProjection.cs)): `DlzLabel`, `CanFire`, `AbortPreviewCode`. |
| `CombatDetailPresentation?` | The selected unit's most recent engagement card — its ten text lines (`StatusLine`, `WeaponLine`, `HardConstraintsLine`, `PolicyLine`, `ConfidenceLine`, `FiringSolutionLine`, `NextActionLine`, `BdaLine`, `PostureLine`, `CorrelationLine`). Optional historical context. |

### Evidence gating

`HasEvidence` requires at least one live source: a non-null `preview`, a non-`Empty` `explain`, or a non-`Empty`
`detail`. If none is live the result is `DomainAbortExplainState.Empty` — the **fail-closed `DOM:UNKNOWN`** state
(`Missing_projection_fails_closed_to_unknown`, `Empty_explain_and_empty_detail_stay_unknown`). An unknown panel
is never silently rendered as "clear".

---

## The seven domain codes

```csharp
AbortReasonCatalog.Engage.AIR_ASPECT_BLOCK
AbortReasonCatalog.Engage.SURFACE_ASPECT_BLOCK
AbortReasonCatalog.Engage.SUBSURFACE_ASPECT_BLOCK
AbortReasonCatalog.Engage.LAND_ASPECT_BLOCK
AbortReasonCatalog.Engage.MINE_ASPECT_BLOCK
AbortReasonCatalog.Engage.FACILITY_ASPECT_BLOCK
AbortReasonCatalog.Engage.DOMAIN_NO_SOLUTION
```

These are the only strings the binder recognises. Any other abort reason (`DLZ_OUT`, `ROE_HOLD_FIRE`,
`NO_AMMO`, …) is **not** a domain abort, so the binder reports `DOMAIN: CLEAR` even though the engagement is
blocked for some *other* reason (`Non_domain_abort_is_domain_clear`). This line is deliberately scoped: it is the
domain cross-check, not a general refusal readout — that is the [engage-explain contract](engage-explain-contract-projection.md).

---

## Resolution — explicit codes beat prose, earliest match wins

`ResolveCode` walks the sources in a fixed precedence so an authoritative, machine-written code always beats a
code that merely appears in free-text prose:

1. `explain.ReasonCode` — the explicit refusal code.
2. `preview.AbortPreviewCode` — the explicit preview abort code (`Engage_preview_abort_code_binds_when_explain_has_no_reason`).
3. `explain.StatusLine`, then `explain.ReasonPlain` — prose scan.
4. each of the combat-detail card's ten lines, in order.

The first source that yields a domain code wins, so an explicit `ReasonCode` of `MINE_ASPECT_BLOCK` is honoured
even when the prose also names `AIR_ASPECT_BLOCK` (`Explicit_reason_code_wins_over_a_different_code_in_prose`),
and a combat-detail outcome can supply the domain code when the preview's abort is a non-domain reason such as
`DLZ_OUT` (`Combat_detail_outcome_binds_domain_code_when_preview_is_not_a_domain_abort`).

### Word-boundary + earliest-in-line matching

`FindCode` scans a line for every domain code and keeps the one at the **earliest** index
(`Earliest_code_in_one_line_wins`). Matching is **word-boundary** (`IndexOfToken` / `IsCodeChar`): a hit must not
be flanked by `[0-9A-Za-z_]`, so `AIR_ASPECT_BLOCKED_EXTRA` does **not** match the `AIR_ASPECT_BLOCK` code
(`Code_token_does_not_match_a_longer_identifier`).

---

## The three states

| Outcome | `StateLine` | `Code` | `IsBlocked` | `DeclutterToken` | `CueClass` | `Fingerprint` |
|---------|-------------|--------|-------------|------------------|-----------|---------------|
| no live evidence | `DOMAIN: — [DOM:UNKNOWN]` | `null` | `false` | `[DOM:UNKNOWN]` | `domain-abort-cue--unknown` | `dom:unknown` |
| live but no domain code | `DOMAIN: CLEAR [DOM:CLEAR]` | `null` | `false` | `[DOM:CLEAR]` | `domain-abort-cue--clear` | `dom:clear` |
| domain code found | `DOMAIN: BLOCKED — <code> [DOM:BLOCKED]` | `<code>` | `true` | `[DOM:BLOCKED]` | `domain-abort-cue--blocked` | `dom:blocked:<code>` |

A permitted launch (`EngageExplainProjection.Project` of a `CanFire` preview) resolves to `DOMAIN: CLEAR`
(`Permitted_launch_is_domain_clear`). The fingerprint is per-code, so switching the blocking domain changes the
fingerprint (`Fingerprint_changes_when_domain_code_changes`) and the host can skip a rebind when it is unchanged.

### Non-color-only state (declutter tokens)

Every cue class is paired with a text `DeclutterToken` (`[DOM:UNKNOWN]` / `[DOM:CLEAR]` / `[DOM:BLOCKED]`) baked
into the `StateLine` and set as the element tooltip, so the state is legible without relying on color — the
accessibility contract shared across the Combat-UX chrome. `DomainAbortCueClasses.All` is the full set a host
must clear before applying the active cue (`Cue_class_catalog_covers_blocked_and_clear`).

---

## Mapping onto the row — `BindRows`

```csharp
IReadOnlyList<DomainAbortExplainRow> BindRows(DomainAbortExplainState state)
```

`BindRows` returns a single binding for the `engage-explain-domain-abort` element carrying the `StateLine` text
and the active `CueClass` (`Bind_rows_exposes_domain_abort_element`; throws `ArgumentNullException` on a null
state).

### Stable element contract

| Constant | Value |
|----------|-------|
| `DomainAbortExplainBinder.ElementName` | `engage-explain-domain-abort` |
| `DomainAbortCueClasses.Unknown` / `Clear` / `Blocked` | `domain-abort-cue--unknown` / `--clear` / `--blocked` |
| `DomainAbortDeclutterTokens.Unknown` / `Clear` / `Blocked` | `[DOM:UNKNOWN]` / `[DOM:CLEAR]` / `[DOM:BLOCKED]` |

---

## Consumer

The live wiring is the Unity
[`EngageExplainPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/EngageExplainPanelHost.cs). Per the
host contract (`DomainAbortExplainHostContractTests`), in the panel's `Refresh` — **never** on the
`DelegationBridge.Tick` hot path — the host:

1. projects the live preview with `EngageExplainProjection.Project(preview)` and the per-selection combat detail,
2. folds all three with `DomainAbortExplainBinder.Bind(explain, preview, detail)` (the headless `Apply` path uses the explain-only `Bind(_last)` overload),
3. caches `_lastDomainAbortFingerprint` so an unchanged state skips the rebind,
4. maps the result through `BindRows`, clearing every `DomainAbortCueClasses.All` class off the
   `engage-explain-domain-abort` label before adding the active `CueClass`, copying the `StateLine` text, and
   setting the `DeclutterToken` as the tooltip.

The host touches **no** `IOrderSink` / `ApplyOrder` — it only renders. The `engage-explain-domain-abort` label
and the `domain-abort-cue--{unknown,clear,blocked}` styles live in
[`EngageExplainPanel.uxml`](../../unity/ProjectAegis/Assets/UI/EngageExplain/EngageExplainPanel.uxml) /
[`.uss`](../../unity/ProjectAegis/Assets/UI/EngageExplain/EngageExplainPanel.uss).

---

## Determinism & invariants

- **Read-model, off the fingerprint.** Built from already-projected DTOs; writes no log, is not part of the
  `SimWorldHash` or order-log fingerprint. The Baltic v2 hash `17144800277401907079` is untouched.
- **Never evaluates the domain gate.** It reads the already-emitted abort code; the engage-time cross-domain
  check is `CombatDomainValidator` in [engagement-pipeline.md](engagement-pipeline.md).
- **Scoped to seven codes.** Only the six `*_ASPECT_BLOCK` codes plus `DOMAIN_NO_SOLUTION` are domain aborts;
  every other refusal reads `DOMAIN: CLEAR`.
- **Fail-closed to UNKNOWN.** No live explain/preview/detail → `Empty` (`DOM:UNKNOWN`), never a false "clear".
- **Explicit over prose, earliest-in-line.** An explicit `ReasonCode` / `AbortPreviewCode` beats a code found in
  prose; within one line the earliest match wins.
- **Word-boundary matching.** A code that is part of a larger identifier does not match.
- **Never color-only.** Each cue class is paired with a `[DOM:*]` declutter token in the text and tooltip.
- **Pure.** `Bind` / `BindRows` touch only their arguments and the one-time code table — no RNG, no wall clock,
  no `UnityEngine` in the binder.

---

## Tests (pins)

Across
[`DomainAbortExplainBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/DomainAbortExplainBinderTests.cs)
and
[`DomainAbortExplainHostContractTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/DomainAbortExplainHostContractTests.cs)
(NUnit, **22** cases — the binder's blocked-code case runs once per domain code via `[TestCaseSource]`).
Highlights:

| Test | Covers |
|------|--------|
| `Explain_reason_code_binds_blocked_domain_abort` | each of the seven codes → `DOMAIN: BLOCKED` + `dom:blocked:<code>`. |
| `Engage_preview_abort_code_binds_when_explain_has_no_reason` | the preview abort code supplies the domain code. |
| `Combat_detail_outcome_binds_domain_code_when_preview_is_not_a_domain_abort` | a combat-detail outcome supplies the code when the preview abort is `DLZ_OUT`. |
| `Explicit_reason_code_wins_over_a_different_code_in_prose` | explicit `ReasonCode` beats a code named in prose. |
| `Non_domain_abort_is_domain_clear` | a non-domain block (`DLZ_OUT`) reads `DOMAIN: CLEAR`. |
| `Permitted_launch_is_domain_clear` | a `CanFire` preview reads `DOMAIN: CLEAR`. |
| `Missing_projection_fails_closed_to_unknown` / `Empty_explain_and_empty_detail_stay_unknown` | no live evidence → the `Empty` `DOM:UNKNOWN` singleton. |
| `Code_token_does_not_match_a_longer_identifier` | word-boundary: `AIR_ASPECT_BLOCKED_EXTRA` is not the code. |
| `Earliest_code_in_one_line_wins` | the earliest domain code in a line is chosen. |
| `Bind_rows_exposes_domain_abort_element` | the single `engage-explain-domain-abort` row binding. |
| `Fingerprint_changes_when_domain_code_changes` | per-code fingerprint. |
| `Cue_class_catalog_covers_blocked_and_clear` | `DomainAbortCueClasses.All` carries all three cues. |
| `Engage_explain_host_binds_domain_abort_with_fingerprint` / `Engage_explain_layout_declares_domain_abort_line` / `Engage_explain_styles_declare_blocked_and_clear_cues` | the host wiring + the UXML/USS element/style contract (and no `DelegationBridge.Tick`). |

Part of the ≥1638-test baseline.

---

## See also

- [engage-explain-contract-projection.md](engage-explain-contract-projection.md) — the general *why permitted / why withheld* engage-explain row this chrome line sits beside.
- [abort-reason-catalog.md](abort-reason-catalog.md) — the stable `*_ASPECT_BLOCK` / `DOMAIN_NO_SOLUTION` codes this binder matches on.
- [engagement-pipeline.md](engagement-pipeline.md) — the engage-time `CombatDomainValidator` / `DOMAIN_NO_SOLUTION` gate that *produces* the domain abort codes this line reads.
- [autonomy-roe-gating.md](autonomy-roe-gating.md) — the decision-time authorization gate; domain aborts are orthogonal to the ROE/autonomy refusals.
- [slice-a-contact-detail-surface.md](slice-a-contact-detail-surface.md) — the combat-detail card the binder reads for historical context.
- [c2-projection-layer.md](c2-projection-layer.md) — general read-model / presentation-boundary rules; [c2-presentation-bridges.md](c2-presentation-bridges.md) — the host-feed pattern the Engage Explain panel uses.
