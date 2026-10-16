# Contact combat card — the per-contact combat summary surface (DRG-169)

> **ADR:** this is the **presentation / read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> — a projection is a **client**, not sim authority), with
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md) (C2 presentation) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).
> Do **not** cite Git ADR-018 (that is sensor side-picture / datalink). Combat UX; the card is DRG-169,
> hardened by DRG-353/354/355 (#700).

The **contact combat card** folds the read-only combat facts already projected for *one selected hostile
contact* into a single
[`ContactCombatCard`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/ContactCombatCardProjection.cs):
the *"what do we know about this contact as a combat target right now?"* card a C2 contact-detail / engage
surface shows. It answers seven questions — **provenance**, **freshness**, **classification confidence**,
**posture**, **targetability**, **engagement status**, **BDA** — and nothing else. It is strictly a
**read-model**: it copies already-resolved facts, issues no orders, recomputes no damage, reads no world
snapshot, and is **not** part of the Baltic v2 `SimWorldHash`. Every missing fact renders as the explicit
`UNKNOWN` token — it never guesses a classification, posture, kill, or authorization.

It is structurally **distinct from own-unit status** (`UnitCommsDisplay`): the card has no `IsAlive` /
`CommsState` members, and its `SurfaceKind` is the fixed discriminator `contact-combat`. The card reads other
projections' outputs; it is one layer *above* them, the combat-target counterpart of the identity-side
[contact-provenance.md](contact-provenance.md).

Everything lives in one file under
[`ProjectAegis.Delegation.UnityAdapter/Presentation/`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/ContactCombatCardProjection.cs):

1. `ContactCombatCardProjection` — the pure static fold (`Project` single / `ProjectSelection` multi).
2. `ContactCombatCard` / `ContactCombatCardSnapshot` — the immutable DTOs (+ their `Empty` singletons).
3. `ContactCombatCardTokens` — the `UNKNOWN` token, the `contact-combat` `SurfaceKind`, the `ccc:empty` sentinel.
4. `ContactCombatCardPostureFact` — the one caller-supplied input (`ContactId` + verbatim `Label`).

---

## Inputs — four read-only snapshots + a posture label

`Project` reads only already-projected, read-only snapshots plus the selected contact id and an optional
posture. It never takes a live sim handle:

```csharp
ContactCombatCard Project(
    string? contactId,
    ContactProvenanceSnapshot? provenance,     // contact-provenance.md (DRG-206)
    TargetabilityAcceptSnapshot? targetability, // targetability-accept.md (DRG-219)
    BdaAssessSnapshot? assessments,             // bda-assess-projection.md (DRG-216)
    AfterActionLedgerSnapshot? afterAction,     // after-action-ledger-projection.md (DRG-218)
    string? postureLabel = null)
```

| Input | Role |
|-------|------|
| `string? contactId` | Null / blank → `ContactCombatCard.Empty`. Otherwise the exact contact to card. |
| `ContactProvenanceSnapshot?` | The identity leg: observer, resolved + last-known target, lifecycle, `Confidence`, `Freshness` + `AgeTicks`, `[Flags]` quality, out-of-comms, last tick/time. |
| `TargetabilityAcceptSnapshot?` | The `Disposition` (`Permitted`/`Withheld`) + `WithheldCauseCode` for the contact. |
| `BdaAssessSnapshot?` | The recorded battle-damage `State` / `Source` / target / observer / tick. |
| `AfterActionLedgerSnapshot?` | The combat-event ledger the **latest engagement row** is selected from. |
| `string? postureLabel` | Caller-supplied verbatim posture; blank / whitespace → `UNKNOWN`. Never inferred. |

The posture is the only *non-projected* fact, and the card copies it **verbatim** (leading/trailing
whitespace included — see `Whitespace_posture_stays_unknown_and_supplied_label_is_verbatim`); it is never
derived from the other facts.

---

## The fold — `Project`

### 1. Unique-row resolution (fail-closed on ambiguity)

For each snapshot, `TryFindUnique` walks every row with the matching `ContactId`. If two matching rows
**disagree** (`!row.Equals(candidate)`), the fact is **ambiguous** → treated as missing (`UNKNOWN`). Identical
duplicates are fine, and input order never matters. A `null` snapshot is "not supplied" (not ambiguous), so
its fact is `UNKNOWN`. This is the `Ambiguous_duplicate_provenance_is_unknown` /
`Provenance_list_order_does_not_change_the_card` discipline.

### 2. Target agreement (DRG-353/354/355 hardening)

A targetability or BDA row keyed by the selected contact id may still describe **another target**. `TargetAgrees`
cross-checks each assessment's `TargetId` against the provenance `Source.TargetId` / `LastKnown.TargetId` **and**
the other assessment's target. A row whose target disagrees with the known identity is **withheld** (rendered
`UNKNOWN`) rather than shown against the wrong contact:

- Targetability for `target-2` when provenance says `target-1` → targetability `UNKNOWN`.
- A disagreeing targetability row also suppresses a BDA row that disagrees with it.
- Without provenance, a targetability and BDA that disagree with *each other* are both `UNKNOWN`; agreeing ones
  (even for a different target) are read.

### 3. Engagement selection

`ResolveEngagementTargetId` takes the **single** target id agreed across provenance (source + last-known),
targetability, and BDA — if those facts name **more than one** distinct target, no engagement is selected
(`count == 1` or null). `SelectLatestEngagement` then walks the after-action ledger **from the end** and returns
the **last** entry whose `TargetId` matches. The ledger is read in its emitted order: same-tick attempts are
**not** re-sorted by correlation id, phase, or any field tie-break — the last matching row is the latest
engagement (`Same_tick_attempts_use_ledger_order_not_correlation_id`).

### 4. Format + fingerprint

Each resolved fact is formatted into a stable `;`-delimited string (see below) or the `UNKNOWN` token, then the
card is stamped with its detailed fingerprint.

---

## Fact formats

Each field is a deterministic, culture-invariant string so the fingerprint is replay-stable. `R`-round-trip is
used for `double` sim-times.

| Field | Format when known | When missing |
|-------|-------------------|--------------|
| `Provenance` | `obs=…;tgt=…;ref=…;life=…;knownTgt=…;quality=…;ooc=0\|1;tick=…;time=…` | `UNKNOWN` |
| `Freshness` | `<Fresh\|Stale>;age=<ticks>` — **label copied, never recomputed from age** | `UNKNOWN` |
| `Confidence` | the recorded `ContactProvenanceConfidence` name (incl. a recorded `Unknown`) | `UNKNOWN` |
| `Posture` | the verbatim caller label | `UNKNOWN` |
| `Targetability` | `<Disposition>;cause=<WithheldCauseCode>` | `UNKNOWN` |
| `EngagementStatus` | `shooter=…;phase=…;outcome=…;weapon=…;tick=…;time=…;corr=…;expl=…` | `UNKNOWN` |
| `Bda` | `state=…;source=…;tgt=…;obs=…;tick=…;time=…;corr=…` | `UNKNOWN` |

A **recorded** `Confidence = Unknown` (an enum value) is deliberately distinct from a **missing** provenance:
both render the same token, but the recorded case is driven by a present row, as pinned by
`Recorded_confidence_unknown_is_distinct_from_missing_provenance`. Likewise a BDA `state=None` / `state=Unknown`
is copied verbatim and never rewritten to `Damaged`/`Destroyed`.

---

## The surface — `ContactCombatCard` / `ContactCombatCardSnapshot`

`ContactCombatCard` is a `sealed` immutable card: `HasContact`, `ContactId`, the seven fact strings, the fixed
`SurfaceKind` (`contact-combat`), and the `Fingerprint`. `ContactCombatCard.Empty` is the no-selection card —
`HasContact == false`, every fact `UNKNOWN`, fingerprint `ccc:empty`.

`ProjectSelection(contactIds, …, postures?)` builds one card per **distinct** contact id (blank ids and
duplicates dropped), **ordinal-sorted**, so input order does not affect the snapshot. Per-contact posture comes
from `ContactCombatCardPostureFact` rows; **conflicting** labels for the same contact collapse to `UNKNOWN`
(`Conflicting_posture_facts_are_unknown`). The resulting `ContactCombatCardSnapshot` **copies** the card array
into a `ReadOnlyCollection` wrapper, so a later write to the caller's array cannot desync the snapshot or its
fingerprint, and `Cards` cannot be cast back to a mutable array.

---

## Fingerprint

Two replay-stable fingerprints, both `ccc:`-prefixed:

- **Per card** — `FormatDetailedFingerprint`: `ccc:surface=contact-combat;has=1;id=…;prov=…;fresh=…;conf=…;post=…;tgt=…;eng=…;bda=…`.
  A blank / no-contact card is `ccc:empty`.
- **Per snapshot** — `ccc:n=<count>|<card.Fingerprint>|…` over the ordinal card list; an empty list is `ccc:empty`.

`FingerprintMatches` re-computes and compares, so a test (or a consumer) can assert a card/snapshot has not
drifted. No wall clock, no RNG, fixed field order — the same inputs always yield the same string.

---

## Consumer

The card is **headless-first and tests-only today** — there is no wired Unity panel host yet (like several of
the [Combat-UX advisory ledgers](combat-ux-advisory-ledgers.md)). The natural surface is the Slice A
contact-detail / engage path behind [c2-presentation-bridges.md](c2-presentation-bridges.md); a host would call
`Project` (or `ProjectSelection` for a multi-select) in the post-Tick C2 refresh, never on the
`DelegationBridge.Tick` hot path, and bind the fact strings into the contact-detail panel.

---

## Determinism & invariants

- **Read-model, off the fingerprint.** Built from already-projected facts; it writes no log, is not part of the
  `SimWorldHash` or order-log fingerprint. The Baltic v2 hash `17144800277401907079` is untouched — changing a
  fact format cannot move a replay golden.
- **Never fabricates combat truth.** A missing fact is `UNKNOWN`, never a guessed classification, posture, or
  authorization. A terminal engagement does **not** invent a BDA, and a `Destroyed` BDA does **not** invent an
  engagement (`Terminal_engagement_does_not_invent_bda` / `Bda_destroyed_does_not_invent_engagement`). High
  confidence does **not** permit targetability.
- **Target-agreement fail-closed.** A row keyed by the contact id but describing a different target is withheld,
  not shown against the wrong contact (DRG-353/354/355).
- **Ambiguity fail-closed.** Disagreeing duplicate rows for the same contact render `UNKNOWN`; identical
  duplicates are read. Input order never changes a card.
- **Freshness is copied, not recomputed.** The `Fresh`/`Stale` label is whatever provenance recorded, even for
  a large age — the card does not re-derive it.
- **Distinct from own-unit status.** The DTO has no `IsAlive` / `CommsState`; `SurfaceKind == contact-combat`.
- **Immutable snapshots.** The card list is copied into a read-only wrapper; a later caller write cannot desync
  the fingerprint.
- **Pure.** `Project` / `ProjectSelection` / the fingerprint readers touch only their arguments — no RNG, no
  wall clock, no `UnityEngine` in the projection logic.

---

## Tests (pins)

All in
[`ContactCombatCardProjectionTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/ContactCombatCardProjectionTests.cs)
(NUnit, **33** tests). Highlights:

| Test | Covers |
|------|--------|
| `Missing_selection_is_explicit_unknown_contact_card` | null / blank id → the `Empty` singleton. |
| `Missing_inputs_for_selected_contact_stay_unknown` | known id, all facts missing → `UNKNOWN`, `ccc:surface=…;has=1;…`. |
| `Known_rows_are_copied_without_filling_gaps` | the exact `;`-delimited format of every fact; no own-unit tokens leak. |
| `Recorded_confidence_unknown_is_distinct_from_missing_provenance` | recorded `Unknown` ≠ missing provenance. |
| `Bda_state_unknown_and_none_are_not_rewritten` | `None`/`Unknown` BDA copied verbatim, never `Damaged`/`Destroyed`. |
| `Terminal_engagement_does_not_invent_bda` / `Bda_destroyed_does_not_invent_engagement` | the two no-cross-fabrication rules. |
| `High_confidence_does_not_permit_targetability` / `Posture_is_not_inferred_when_omitted` | no guessed disposition / posture. |
| `Freshness_label_is_not_recomputed_from_age` | the recorded label is copied even at age 400. |
| `Targetability_for_a_different_target_than_provenance_is_unknown` (+ BDA variants) | the DRG-353/354/355 target-agreement guards. |
| `Ambiguous_duplicate_provenance_is_unknown` / `Identical_duplicate_provenance_is_read` | the ambiguity fail-closed. |
| `Latest_engagement_is_the_last_matching_ledger_entry` / `Same_tick_attempts_use_ledger_order_not_correlation_id` | ledger-order engagement selection. |
| `Snapshot_orders_contact_ids_ordinally_and_is_replay_stable` / `Snapshot_cards_cannot_be_cast_back_to_a_mutable_array` / `Snapshot_copies_caller_array_so_later_writes_do_not_desync_fingerprint` | ordinal multi-card, immutability, defensive copy. |
| `Conflicting_posture_facts_are_unknown` / `Empty_contact_list_is_empty_snapshot` | posture conflict → `UNKNOWN`; blank selection → `Empty`. |
| `Card_shape_is_distinct_from_own_unit_status` | no `IsAlive`/`CommsState`; `contact-combat` surface. |
| `Repeated_projection_is_replay_stable` | identical inputs → identical card + fingerprint. |

Part of the ≥1638-test baseline.

---

## See also

- [contact-provenance.md](contact-provenance.md) — the identity-side `ContactProvenanceState` leg this card reads.
- [targetability-accept.md](targetability-accept.md) — the `TargetabilityAcceptContactRow` disposition/cause it shows.
- [bda-assess-projection.md](bda-assess-projection.md) — the `BdaAssessContactState` row it shows.
- [after-action-ledger-projection.md](after-action-ledger-projection.md) — the `AfterActionLedgerEntry` ledger it selects the latest engagement from.
- [engagement-explanation-projection.md](engagement-explanation-projection.md) — the deeper *why permitted / refused + remedy* surface for a selected engage-assess leg.
- [c2-projection-layer.md](c2-projection-layer.md) — general read-model / presentation-boundary rules; [c2-presentation-bridges.md](c2-presentation-bridges.md) — the host-feed pattern a future panel would use.
