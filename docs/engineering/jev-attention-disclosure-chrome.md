# Jev attention-score + skill-unavailable disclosure chrome (DRG-319)

> **ADR:** this is **presentation chrome**, not a fire path or an order seam
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3,
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md),
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md)). It folds a
> synthetic projection document into one advisory **attention list** — *which units should the
> watch officer look at first, and is that ranking from the Jev model or a local fallback?* — plus a
> **fail-closed disclosure banner**. It **never** enqueues an order, recommends a weapon, or auto-engages.

"Jev" is an external decision service (`jev_decide`, pinned to model `jev-1.13.x` / OpenRouter
`typesafe/jev-1.13`; see [jev-playwright-mcp-setup.md](jev-playwright-mcp-setup.md)). This subsystem is
the **VS1** (Vertical Slice 1) C2 chrome that turns a Jev attention-priority decision — or, when no
real decision is present, a transparent **local heuristic** — into a ranked, highlight-coded attention
panel. Its load-bearing honesty rule is that it **never attributes a local score to the model**: a row
counts as a model score only when it carries a pinned `jevDecision`, and the panel's provenance label and
disclosure banner always tell the player which source produced the ranking.

The binder is a **pure** fold
([`JevAttentionBinder.Project`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevAttentionBinder.cs))
over a read-only `aegis.jev.state.v0` JSON document; it calls no model and performs no I/O. The Unity
[`JevAttentionPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/JevAttentionPanelHost.cs) only
paints an already-projected panel. Live wiring to the Jev decision server is future work (blocked on the
CMANO pre-launch GO); today the fixtures are synthetic and a "model result" arrives as a pre-pinned
`jevDecision` block on a projection row.

| Piece | File | Role |
|-------|------|------|
| [`JevAttentionBinder`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevAttentionBinder.cs) | `UnityAdapter/Presentation/` | The pure `Project(json)` fold + the local-heuristic `Score` / `Sort`. Accepts schema `aegis.jev.state.v0` only. |
| [`JevAttentionCue`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevAttentionBinder.cs) | `UnityAdapter/Presentation/` | Read-only cues the local heuristic scores (`Iff`, `RangeNm`, `Closing`, `UrgencyHint`, `HoldFire`, `RoeConflict`, `PlayerAlreadyLooking`). Oracle `expected*` fields are **not** inputs. |
| [`JevAttentionLevel`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevAttentionLevel.cs) | `UnityAdapter/Presentation/` | The band constants + `Rank` / `Highlights` / `CssClass` helpers. |
| [`JevAttentionRow`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevAttentionRow.cs) | `UnityAdapter/Presentation/` | One advisory row (`UnitId`, `ContactId`, `Level`, `Rank`, `Highlight`, `CssClass`, `PlayerAlreadyLooking`, `Line`). No order/weapon fields. |
| [`JevAttentionPanel`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevAttentionPanel.cs) | `UnityAdapter/Presentation/` | The output: `FixtureId`, `ModelScoreAvailable`, sorted `Rows`, `Disclosure`, and the player-visible `ScoreSourceLabel`. |
| [`JevDisclosurePresentation`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevDisclosurePresentation.cs) | `UnityAdapter/Presentation/` | The disclosure DTO + the model pins and the three canonical states (`Hidden` / `Shown` / `LocalHeuristic`). |
| [`JevDisclosureBinder`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevDisclosureBinder.cs) | `UnityAdapter/Presentation/` | The fail-closed `Project(bool?)` disclosure projector for the skill-availability flag alone. |
| [`JevAttentionPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/JevAttentionPanelHost.cs) | `unity/.../Scripts/Runtime/` | The Unity host (`MonoBehaviour`) that paints a projected panel into [`JevAttentionPanel.uxml`](../../unity/ProjectAegis/Assets/UI/Jev/JevAttentionPanel.uxml) / [`.uss`](../../unity/ProjectAegis/Assets/UI/Jev/JevAttentionPanel.uss). No order seam. |

---

## Attention bands — `JevAttentionLevel`

| Band | `Rank` | Highlights | USS class | Meaning (chrome) |
|------|-------:|:----------:|-----------|------------------|
| `immediate` | 5 | yes | `jev-attention-row--immediate` | Interrupt chrome; top of the sort. |
| `focus` | 4 | yes | `jev-attention-row--focus` | Primary panel / strong highlight. |
| `watch` | 3 | yes | `jev-attention-row--watch` | Secondary panel / soft highlight. |
| `low` | 2 | yes | `jev-attention-row--low` | Soft optional glance. |
| `ignore` | 1 | no | `jev-attention-row--ignore` | No highlight; bottom of the sort. |
| `unavailable` (or `null`) | 0 | no | `jev-attention-row--unavailable` | Fail-soft band when Jev decide is offline. **Not** a model score. |

`Rank` drives the sort; `Highlights` is `true` only for `immediate`/`focus`/`watch`/`low` (an `ignore` or
`unavailable` row is listed without a highlight).

---

## The projection — `JevAttentionBinder.Project`

`Project(json)` requires a non-empty JSON **object** whose `schema` is exactly `aegis.jev.state.v0`
(anything else throws). It then:

1. **Rejects private payloads** — recursively, in every nested object and array, any property named
   `decisionLog`, `scenarioXml`, or `secret` (case-insensitive) throws. VS1 accepts synthetic projection
   fixtures only; scenario or log dumps are refused.
2. Reads `skillAvailability.jevDecide` (`true` / `false` / absent → `null`) and the player's
   `playerInfoModel.selectedUnitId` + `cameraFocusId`.
3. Scores every entry in `roster[]` (or the root object itself when there is no `roster` array).
4. Sorts the rows and resolves the disclosure + provenance.

### Per-row source resolution — `ProjectEntry`

For each entry (keyed by a required `unitId`, with an optional `contactId`), the source is the **first**
of:

1. **Skill down** (`jevDecide` is not `true`) → a row with a `null` level and no highlight; never scored.
   A stale `jevDecision` on the row is ignored.
2. **Pinned model result** → the entry's `jevDecision` object whose `model` is `typesafe/jev-1.13` **or**
   `jev-1.13.x` **and** whose `attention_priority` is a known band. That band (canonicalized
   case-insensitively) becomes the row level and marks the row as a model score.
3. **Local heuristic** → otherwise, `Score(cue)` from the entry's `contact` / `flags` / `roe` facts. A
   `jevDecision` without the pinned model stays a heuristic.

### Local heuristic — `Score` → `ScoreThreat`

`ScoreThreat` takes the **first** matching rule (IFF is `contact.iff`, falling back to `contact.classHint`):

| # | Condition | Band |
|---|-----------|------|
| 1 | `hostile` & `closing` & `rangeNm ≤ 12` | `immediate` |
| 2 | `hostile` & urgency `critical` or `imminent` | `immediate` |
| 3 | `unknown` & `closing` & `rangeNm < 18` | `focus` |
| 4 | urgency `urgent` & `closing` | `focus` |
| 5 | `unknown` & not `closing` & `15 ≤ rangeNm ≤ 28` | `watch` |
| 6 | urgency `elevated` | `watch` |
| 7 | `friendly` & not `closing` & `rangeNm ≥ 35` | `ignore` |
| 8 | `neutral`, or `holdFire`, or `roeConflict` | `low` |
| 9 | `friendly` | `ignore` |
| 10 | otherwise | `low` |

A **missing, null, non-numeric, negative, or non-finite** `rangeNm` is *unknown* — it matches no proximity
band (rules 1/3/5/7 fall through), so a closing contact with no range is **not** treated as if it were on
top of the player. An explicit `0` is a real range. Explicit urgency still applies without a range.

`Score` then **demotes an attended unit one band** (`immediate→focus→watch→low→ignore`) when
`PlayerAlreadyLooking` — i.e. the unit is the `selectedUnitId` or the `cameraFocusId`. The player already
has eyes on it, so it should not also shout for attention.

### Sort — `Sort`

Rows sort by `Rank` **descending**, then **unattended before attended** at equal rank
(`PlayerAlreadyLooking: false` first), then `UnitId` ordinal.

---

## Disclosure and provenance

The disclosure banner ([`JevDisclosurePresentation`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/JevDisclosurePresentation.cs))
has three states, resolved after scoring:

- **`Shown`** (`jev-disclosure--visible`) — `jevDecide` is not `true`. Banner:
  `"ASSIST UNAVAILABLE — Jev decide is offline. Advisory chrome only; no engage suggestion."`
- **`LocalHeuristic`** (`jev-disclosure--visible`) — skill is up but at least one row is **not** a pinned
  model result. Banner: `"LOCAL HEURISTIC — No Jev decision received. Priorities use local advisory rules."`
- **`Hidden`** (`jev-disclosure--hidden`) — skill is up **and every row** carries a pinned `jevDecision`.

`JevDisclosureBinder.Project(bool?)` is the **fail-closed** projector for the flag alone: `true` → `Hidden`,
`false`/`null` → `Shown`.

`JevAttentionPanel.ModelScoreAvailable` is `true` only when skill is up, there is at least one row, and
**every** row came from the model. The player-visible `ScoreSourceLabel` is:

- `"MODEL: jev-1.13.x · typesafe/jev-1.13"` when `ModelScoreAvailable`;
- `"SOURCE: LOCAL HEURISTIC"` when rows exist with a level but it is not a full model score;
- `"SOURCE: UNAVAILABLE"` otherwise.

The model pins (`PinnedModel` / `PinnedOpenRouterModel`) are **pinned**, never a floating `latest`, and are
carried as metadata on every disclosure — but a pin alone is never evidence of a model score.

---

## The row line — `JevAttentionRow.Line`

Each row renders one advisory line `ATTN {level}  {unitId}  {contactId}` (level falls back to
`unavailable`; the `contactId` is dropped when empty). The contact id stays on the row so the text cannot
drift from it. The line carries no `ENGAGE` / `WEAPON` / `SALVO` / order vocabulary.

---

## Unity host — `JevAttentionPanelHost`

The host is `#if UNITY_5_3_OR_NEWER` chrome: it reads a projection JSON string, calls
`JevAttentionBinder.Project`, and paints the disclosure banner (`jev-disclosure-banner`), the model-pin /
`ScoreSourceLabel` field (`jev-model-pin`), and the attention list (`jev-attention-list`) into
`JevAttentionPanel.uxml` / `.uss`. It caches the `LastPanel` and repaints on enable. It has **no**
`IOrderSink`, `ApplyOrder`, `TryIssue*`, `DelegationBridge`, or `CatalogWriteGate` reference, and the UXML
has no `ui:Button` and no engage affordance — enforced by `JevAttentionPanelHostTests`.

---

## Invariants

- **Advisory only** — no order enqueue, no weapon recommendation, no auto-engage; the host carries no order
  seam and the row/banner text carries no engage vocabulary.
- **Never attribute a local score to the model** — `ModelScoreAvailable` requires a pinned `jevDecision`
  (`typesafe/jev-1.13` or `jev-1.13.x` + a known `attention_priority`) on **every** scored row; `jevDecide:
  true` alone keeps the local heuristic and labels `SOURCE: LOCAL HEURISTIC`.
- **Fail-closed disclosure** — a missing or `false` skill flag shows the offline banner and does not score;
  a stale `jevDecision` under a down skill is ignored.
- **Pure / deterministic** — a `static` fold over read-only JSON; no model call, no I/O, no RNG, no wall
  clock. Same document ⇒ same panel.
- **Unknown range is not proximity** — a missing/invalid `rangeNm` matches no distance band, so a
  range-less contact is never scored as if it were close.
- **Projection fixtures only** — `decisionLog` / `scenarioXml` / `secret` are refused recursively; VS1
  takes synthetic `aegis.jev.state.v0` documents, never scenario or log payloads.
- **Off the sim fingerprint** — presentation chrome; it reads nothing the sim fingerprints and the Baltic
  v2 replay hash is untouched. No `UnityEngine` in the binder/levels/panel; the Unity dependency lives only
  in the host.

---

## See also

| Topic | Doc |
|-------|-----|
| The Jev MCP tooling and model pins this chrome names | [jev-playwright-mcp-setup.md](jev-playwright-mcp-setup.md) |
| General C2 read-model / projection + binder/host rules | [c2-projection-layer.md](c2-projection-layer.md) |
| The adapter seam between Unity hosts and projections | [c2-presentation-bridges.md](c2-presentation-bridges.md) |
| The watch-officer attention/auto-pause spine (sim-side) | [watch-attention-autopause.md](watch-attention-autopause.md) |

---

## Tests

[`JevAttentionBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/JevAttentionBinderTests.cs),
[`JevDisclosureBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/JevDisclosureBinderTests.cs),
and [`JevAttentionPanelHostTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/JevAttentionPanelHostTests.cs)
(NUnit, 38 tests over the `Fixtures/Jev/vs1/` documents) pin the band ladder and the immediate/ignore
fixtures, the rank-then-unattended-then-id sort, the already-looking demotion (selection and camera both
count), the unknown/invalid-vs-explicit-`0` range rules, explicit-urgency-without-range, the pinned-model
vs non-pinned vs partial-roster model-score resolution, the skill-down / stale-decision fail-closed
disclosure, the recursive private-payload refusal, and the oracle-fields-are-not-inputs contract.
