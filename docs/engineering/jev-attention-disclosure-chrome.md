# Jev VS1 attention-score & skill-unavailable disclosure chrome (DRG-319)

> **ADR:** this is the **presentation / read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> — chrome is a **client**, not sim authority), with
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md) (C2 presentation) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).

The **Jev attention chrome** paints one advisory question — *which contacts should the player look at next?* — as
a ranked, highlighted list, plus a **disclosure banner** that is always honest about where the ranking came from
(a pinned model, a local heuristic, or nothing because the assist is offline). It is strictly advisory **chrome**:
it never recommends a weapon, builds an engage card, or enqueues an order. The whole feature lives in the adapter
Presentation layer plus one Unity host:

| File | Role |
|------|------|
| [`JevAttentionBinder.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevAttentionBinder.cs) | the pure projector (`Project`, `Score`, `Sort`) + the `JevAttentionCue` scoring record |
| [`JevAttentionLevel.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevAttentionLevel.cs) | the five score bands + `Rank` / `Highlights` / `CssClass` |
| [`JevAttentionRow.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevAttentionRow.cs) | one advisory row (`Line`, no order/weapon fields) |
| [`JevAttentionPanel.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevAttentionPanel.cs) | the projected panel + the `ScoreSourceLabel` provenance line |
| [`JevDisclosureBinder.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevDisclosureBinder.cs) | the fail-closed availability → disclosure map |
| [`JevDisclosurePresentation.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevDisclosurePresentation.cs) | the disclosure DTO + the pinned model ids and banner text |
| [`JevAttentionPanelHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/JevAttentionPanelHost.cs) | the UI-Toolkit host that paints the rows + banner |

It is **read-only**: it reads one synthetic projection document and produces display rows. It never issues an
order, calls `IOrderSink` / `TryIssue`, touches `DelegationBridge` / `CatalogWriteGate` / `SimulationSession`, or
calls a model at runtime, and it is **not** part of the Baltic v2 `SimWorldHash`.

---

## Input — one `aegis.jev.state.v0` projection document

`JevAttentionBinder.Project(json)` is the single entry point. The input is a **synthetic projection fixture**, not
sim internals:

- The root must be a JSON object whose `schema` is exactly `aegis.jev.state.v0` (`StateSchema`); any other schema
  throws.
- A privacy guard (`RejectPrivatePayload`) walks every object and array and **refuses** a document carrying a
  `decisionLog`, `scenarioXml`, or `secret` field (case-insensitive, at any depth). This keeps scenario XML,
  `DecisionLog`/sim internals, and secrets out of the chrome path (`Private_log_payloads_are_refused`,
  `Private_fields_are_refused_at_every_object_and_array_depth`).
- Oracle `expected*` fields (used by the fixtures to assert sort order / levels) are **not inputs** — stripping
  them does not change the score (`Oracle_fields_are_not_required_to_score`).

A document may carry a single contact at the root, or a `roster` array of them. Player context lives under
`playerInfoModel` (`selectedUnitId`, `cameraFocusId`), and assist availability under
`skillAvailability.jevDecide`.

---

## Score bands & the row model

```csharp
public static class JevAttentionLevel
{
    public const string Ignore = "ignore";       // Rank 1 — no highlight, bottom of sort
    public const string Low = "low";             // Rank 2 — soft optional glance
    public const string Watch = "watch";         // Rank 3 — secondary panel / soft highlight
    public const string Focus = "focus";         // Rank 4 — primary panel / strong highlight
    public const string Immediate = "immediate"; // Rank 5 — interrupt chrome, top of sort
    public const string Unavailable = "unavailable"; // fail-soft when the assist is offline — not a score
}
```

`Highlights` is true only for `immediate`/`focus`/`watch`/`low`; `ignore` and `unavailable` rows are listed
without a highlight. `CssClass` maps each band to a `jev-attention-row--<band>` USS class (and `unavailable` for
anything unscored). Each row carries the band on the row itself so the text cannot drift from the colour:

```csharp
public sealed record JevAttentionRow(
    string UnitId, string? ContactId, string? Level, int Rank, bool Highlight, string CssClass)
{
    public bool PlayerAlreadyLooking { get; init; }
    // "ATTN <level>  <unitId>  <contactId>" — never ENGAGE / WEAPON / SALVO.
    public string Line { get; }
}
```

---

## Local heuristic scoring — `Score(JevAttentionCue)`

When there is no pinned model result for a row, the band is derived from a pure, first-match rule ladder over the
projected contact cue (IFF, range, closing, urgency hint, hold-fire, ROE conflict). The ladder, in order:

1. hostile **and** closing **and** `rangeNm ≤ 12` → `immediate`
2. hostile **and** urgency `critical`/`imminent` → `immediate`
3. unknown **and** closing **and** `rangeNm < 18` → `focus`
4. urgency `urgent` **and** closing → `focus`
5. unknown **and** not closing **and** `15 ≤ rangeNm ≤ 28` → `watch`
6. urgency `elevated` → `watch`
7. friendly **and** not closing **and** `rangeNm ≥ 35` → `ignore`
8. neutral **or** hold-fire **or** ROE conflict → `low`
9. friendly → `ignore`
10. otherwise → `low`

**Range is sanitised, not trusted.** `OptionalDouble` returns null when `rangeNm` is missing, non-numeric,
negative, or non-finite — a null range never matches a proximity band, so an unknown range is *not* treated as
point-blank (`Unknown_or_invalid_range_does_not_create_proximity_alert`). Explicit `0` is a real range and still
scores `immediate` for a closing hostile (`Explicit_zero_range_closing_hostile_is_immediate`). When the range is
absent, an explicit urgency hint still applies (`Missing_range_preserves_explicit_urgency`).

**Already-looking demotion.** A unit the player is already selected on or camera-focused on steps **down one band**
(`immediate→focus→watch→low→ignore`), so an identical unattended contact out-ranks it
(`Already_looking_scores_below_an_identical_unattended_contact`). Both `selectedUnitId` and `cameraFocusId` count.

### Sort order

`Sort` orders rows by **rank descending**, then **unattended before attended** at an equal rank, then **unit id
ordinal** (`Sort_is_rank_then_unit_id_not_input_order`, `Equal_ignore_rows_place_the_unattended_unit_first`). Input
order is never preserved.

---

## Model result vs. local heuristic — `ModelScoreAvailable`

A row only counts as a **model** score when a real, pinned Jev result is attached:

```jsonc
"jevDecision": { "model": "typesafe/jev-1.13", "attention_priority": "watch" }
```

`ReadModelLevel` accepts the decision only when `model` is the pinned `typesafe/jev-1.13` **or** `jev-1.13.x`
(never a floating `latest`) and `attention_priority` canonicalises to a known band. The panel's
`ModelScoreAvailable` is true **only when the assist is up and every scored row carries such a result** — a
`roster` where even one row falls back to the heuristic keeps the whole panel on the heuristic
(`Partial_roster_result_does_not_mark_the_panel_as_a_model_score`). Important nuances the tests pin:

- `skillAvailability.jevDecide: true` **alone is not a result** — those rows use the local heuristic and show the
  `LOCAL HEURISTIC` banner.
- A `jevDecision` naming the wrong model stays a heuristic (`Decision_without_the_pinned_model_stays_a_heuristic`).
- When the assist is **down**, a stale `jevDecision` is ignored entirely; nothing is scored
  (`Skill_down_ignores_a_stale_jev_decision`).

`ScoreSourceLabel` reports provenance to the player, kept separate from the model pin used only as metadata:
`MODEL: jev-1.13.x · typesafe/jev-1.13`, else `SOURCE: LOCAL HEURISTIC`, else `SOURCE: UNAVAILABLE`.

---

## Disclosure — fail-closed by default

```csharp
public static JevDisclosurePresentation Project(bool? jevDecide) =>
    jevDecide == true ? JevDisclosurePresentation.Hidden : JevDisclosurePresentation.Shown;
```

The disclosure is **fail-closed**: a missing or `false` `jevDecide` shows the persistent offline banner
`ASSIST UNAVAILABLE — Jev decide is offline. Advisory chrome only; no engage suggestion.`
(`Missing_skill_flag_fails_closed_onto_disclosure`). `Project` resolves the three states:

| State | Condition | Banner | `ScoreSourceLabel` |
|-------|-----------|--------|--------------------|
| **Shown** (`jev-disclosure--visible`) | assist down (`jevDecide` missing/false) | `ASSIST UNAVAILABLE …` | `SOURCE: UNAVAILABLE` |
| **LocalHeuristic** (`jev-disclosure--visible`) | assist up, but no full model result | `LOCAL HEURISTIC — No Jev decision received. Priorities use local advisory rules.` | `SOURCE: LOCAL HEURISTIC` |
| **Hidden** (`jev-disclosure--hidden`) | assist up **and** every row is a pinned model result | *(none)* | `MODEL: jev-1.13.x · typesafe/jev-1.13` |

The banner text is always advisory and never mentions `WEAPON` / `ENGAGE`
(`Skill_down_shows_disclosure_and_does_not_invent_a_model_score`).

---

## The host — paint rows + banner, nothing else

[`JevAttentionPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/JevAttentionPanelHost.cs) is a
`UIDocument` MonoBehaviour that resolves three named elements under `jev-attention-root` —
`jev-model-pin` (the `ScoreSourceLabel`), `jev-disclosure-banner` (the banner text + visible/hidden class), and
`jev-attention-list` (one `jev-attention-row` `Label` per row, carrying `row.CssClass`). It exposes
`Apply(panel)` for an already-projected panel and `ApplyStateJson(json)` which projects then paints.

The host is pinned to chrome-only by [`JevAttentionPanelHostTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/JevAttentionPanelHostTests.cs):
the source contains **no** `IOrderSink`, `ApplyOrder`, `TryIssue`, `DelegationBridge`, `CatalogWriteGate`, or
`weapon`, and the [`JevAttentionPanel.uxml`](../../unity/ProjectAegis/Assets/UI/Jev/JevAttentionPanel.uxml) layout
has **no** `ui:Button` and no `engage` text. The
[`JevAttentionPanel.uss`](../../unity/ProjectAegis/Assets/UI/Jev/JevAttentionPanel.uss) declares the five
`.jev-attention-row--{immediate,focus,watch,low,ignore}` band classes plus `.jev-disclosure--visible` /
`.jev-disclosure--hidden`.

---

## Determinism & invariants

- **Advisory only.** No row, banner, or host path produces an order, weapon recommendation, or engage card; the
  row `Line` and banner text are asserted never to contain `ENGAGE` / `WEAPON` / `SALVO` / `order`.
- **Pure projector.** `JevAttentionBinder` reads only its `json` argument through `JsonDocument` — no RNG, no wall
  clock, no `UnityEngine`. Same document → same panel.
- **Privacy-guarded.** `decisionLog` / `scenarioXml` / `secret` payloads are refused recursively; only synthetic
  projection fixtures are accepted.
- **Fail-closed disclosure.** A missing/false `jevDecide` shows the offline banner; a missing full model result
  shows the local-heuristic banner. The panel never claims a model score it does not have.
- **Pinned model, never `latest`.** A model result is honoured only for `typesafe/jev-1.13` / `jev-1.13.x`.
- **Range is sanitised.** A missing/non-numeric/negative/non-finite range is unknown (not zero), so it never
  manufactures a proximity alert.
- **Off the fingerprint.** Built from a projection document; writes no log, is not part of the `SimWorldHash` or
  order-log fingerprint. The Baltic v2 hash `17144800277401907079` is untouched.
- **Off the Tick hot path.** The host paints in its UI refresh path, never on `DelegationBridge.Tick`.

---

## Tests (pins)

| Fixture | Cases | Covers |
|---------|-------|--------|
| [`JevAttentionBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/JevAttentionBinderTests.cs) | 32 | the immediate/ignore/mixed-roster scoring + sort, the rank/unattended/unit-id tie-break, oracle-field independence, the private-payload refusal at every depth, the range-sanitisation matrix (missing/non-numeric/negative/non-finite + explicit-zero), explicit-urgency fallback, already-looking demotion via selection/camera, and the pinned-model / wrong-model / partial-roster / skill-down `ModelScoreAvailable` rules. |
| [`JevDisclosureBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/JevDisclosureBinderTests.cs) | 4 | the skill-down offline banner, the skill-up local-heuristic banner, the fail-closed `Project(null/false/true)` map, and the pinned-model / forbid-enqueue question payload. |
| [`JevAttentionPanelHostTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/JevAttentionPanelHostTests.cs) | 2 | the host wires the binder + disclosure elements with no order/weapon/`DelegationBridge` surface, and the UXML/USS carry the band + disclosure classes with no order buttons. |

Fixtures live under
[`src/ProjectAegis.Delegation.UnityAdapter.Tests/Fixtures/Jev/vs1/`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Fixtures/Jev/vs1/).
Part of the ≥1638-test baseline.

---

## See also

- [c2-projection-layer.md](c2-projection-layer.md) — general read-model / presentation-boundary rules this chrome follows.
- [c2-presentation-bridges.md](c2-presentation-bridges.md) — the host-feed pattern the panel host uses.
- [agent-traits-and-attention.md](agent-traits-and-attention.md) — the sim-side **agent** attention/bandwidth model; distinct from this player-facing advisory attention ranking.
- [engage-explain-contract-projection.md](engage-explain-contract-projection.md) / [autonomy-roe-gating.md](autonomy-roe-gating.md) — the real engage-explain and ROE write paths this advisory chrome never touches.
- [jev-playwright-mcp-setup.md](jev-playwright-mcp-setup.md) — unrelated local browser-MCP tooling that shares the "Jev" name.
