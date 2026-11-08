# After-action ledger read-model (DRG-218)

> **Scope.** The pure, headless **after-action ledger** read-model added in Combat-UX Slice C
> (DRG-218, DRG-171 navigation) under
> [`ProjectAegis.Delegation/AfterAction/`](../../src/ProjectAegis.Delegation/AfterAction/). It folds
> a **combat-event picture** into an ordered, filterable, replay-linked ledger a Command-Review /
> after-action panel can page and filter. Like the other Combat-UX read-models it follows the
> read-only projection contract of ADR-010 §2–3 / ADR-007: **no `UnityEngine` dependency**, **no
> mutation**, **no order enqueue, combat resolution, retask, or catalog write**, and it never touches
> the sim hot path or the replay fingerprint. Each row is copied **verbatim** from a combat-event
> row — no reconstructed facts — and carries no authorize/release/fire field.
>
> Its input is a *consume-only mirror* of the DRG-211 `CombatEvent` contract (from the
> `CombatEvents/` subsystem, #583), and the engage-time source of those events is
> [engagement-pipeline.md](engagement-pipeline.md). The read-only presentation contract it obeys is
> [c2-projection-layer.md](c2-projection-layer.md); the authorization gate it never speaks for is
> [autonomy-roe-gating.md](autonomy-roe-gating.md).

---

## What it is

`AfterActionLedgerProjection` (a `static` class) turns the combat-event stream of a scenario run
into an `AfterActionLedgerSnapshot` — an ordered list of `AfterActionLedgerEntry` rows — plus a
`Filter` helper and a replay-stable `ComputeFingerprint`. It is the "what happened, and why" review
surface: every fire attempt / in-flight / terminal-outcome event becomes one navigable ledger row,
each still carrying the `ExplanationRef` back-pointer to its explanation record.

Two structural notes distinguish it from the eight
Combat-UX advisory ledgers:

- **No separate `*Fingerprint.cs` file.** The canonical fingerprint is a static
  `AfterActionLedgerProjection.ComputeFingerprint(snapshot)` method *on the projection* (like the
  posture projectors), not a dedicated `*Fingerprint` class. The folder is just three files:
  `AfterActionLedgerTypes.cs`, `AfterActionConsumeTypes.cs`, `AfterActionLedgerProjection.cs`.
- **It consumes a mirror contract, not the live `CombatEvent` type.** `AfterActionConsumeTypes.cs`
  declares `CombatEventRowConsume` / `CombatEventSnapshotConsume` / `CombatEventPhaseConsume` as
  **consume-only mirrors** of the DRG-211 `CombatEvent` / `CombatEventSnapshot` / `CombatEventPhase`
  types in [`ProjectAegis.Delegation/CombatEvents/`](../../src/ProjectAegis.Delegation/CombatEvents/).
  They are field-for-field compatible so a caller can map a `CombatEvent` row across with **no
  reconstruction**, while the ledger keeps a decoupled, presentation-only input surface.

## The types

| Type | Role |
|------|------|
| `CombatEventPhaseConsume` | Consume mirror of DRG-211 `CombatEventPhase`: `IntentAccepted(1)` / `Authorized(2)` / `AuthorizationRefused(3)` / `Firing(4)` / `InFlight(5)` / `TerminalOutcome(6)`. |
| `CombatEventRowConsume` | Consume mirror of one `CombatEvent`: `Phase`, `ShooterId`, `TargetId`, `WeaponFamilyId`, `Outcome`, `CorrelationId`, `SimTime`, `SimTick`, `ExplanationRef`. |
| `CombatEventSnapshotConsume` | The event picture (`Events` + `Empty`). The projection's input. |
| `AfterActionLedgerEntry` | One ledger row — the same nine fields, copied verbatim. |
| `AfterActionLedgerSnapshot` | Ordered `Entries` (+ `Empty`). The projection's output. |
| `AfterActionLedgerFilter` | Optional AND-combined filter: `ShooterId?` / `TargetId?` / `WeaponFamilyId?` / `Outcome?` (each `null` = wildcard). |

## Where it sits

```
CombatEvents/ (DRG-211) ──map──▶ CombatEventSnapshotConsume   (consume-only mirror; no reconstruction)
        │
        ▼
   AfterActionLedgerProjection.Project(snapshot)      (pure, off the DelegationBridge.Tick hot path)
        │
        ├─▶ AfterActionLedgerSnapshot  (ordered Entries, 1:1 with input events)
        │        │
        │        ▼
        │   AfterActionLedgerProjection.Filter(ledger, filter)   (AND-combined ordinal filter)
        │
        ▼
   Command-Review / after-action panel (DRG-171 navigation) — read-only display
```

**Consumers today.** The ledger is not bound to a presentation surface yet — it is validated by its
test fixture pending a Command-Review binding (the pattern the other headless-first Combat-UX
read-models follow). When binding one, read the snapshot in a UnityAdapter Command-Review bridge in
the post-Tick C2 refresh — never on the Tick hot path (see
[c2-presentation-bridges.md](c2-presentation-bridges.md)).

---

## `Project` — verbatim 1:1 map

`Project(CombatEventSnapshotConsume? snapshot)` maps each combat-event row to an
`AfterActionLedgerEntry` **in input order** and returns the snapshot. A null or empty input returns
`AfterActionLedgerSnapshot.Empty`. A second overload, `Project(IReadOnlyList<CombatEventRowConsume>?
events)`, wraps a raw row list (same #583 contract) and delegates to the snapshot form.

The map is deliberately a straight field copy (`ToEntry`) with **no re-sort and no derivation** —
the ledger inherits its ordering from the (already deterministic) combat-event picture and copies
the facts unchanged. This is the load-bearing "no reconstructed facts" property: the after-action
view can never disagree with the combat-event record it mirrors.

## `Filter` — AND-combined ordinal equality

`Filter(AfterActionLedgerSnapshot ledger, AfterActionLedgerFilter filter)` returns the rows matching
**all** supplied filter fields, by ordinal string equality; a `null` filter field is a wildcard.
An empty ledger, or a filter that matches nothing, returns `AfterActionLedgerSnapshot.Empty`.
Matching preserves order. This is the DRG-171 navigation primitive — e.g. "all `Kill` outcomes",
"everything shooter `u1` did with `sm-2`", or "every event against `hostile-1`".

## `ComputeFingerprint` — replay-stable

`ComputeFingerprint(AfterActionLedgerSnapshot? ledger)` returns `aal:empty` for a null/empty ledger,
else `aal:e=<count>` followed by one `|`-delimited record per entry: `ShooterId`, `TargetId`,
`WeaponFamilyId`, `Outcome`, `CorrelationId`, `SimTime` (`"R"` round-trip, invariant culture),
`SimTick`, `(int)Phase`, `ExplanationRef`. Invariant culture, ordinal ordering, no wall clock — same
inputs yield the same string. (Fields are appended raw rather than length-prefixed; the ids here are
sim entity / weapon-family / correlation values, not free text.)

> **Not fingerprinted into the sim.** The snapshot and its fingerprint are a *presentation* hash for
> change-detection / test pinning. They are **not** part of the `DecisionLog` `ComputeFingerprint()`
> or the `SimWorldHash`, so the Baltic v2 replay hash `17144800277401907079` is untouched.

## Invariants

| Invariant | Why |
|-----------|-----|
| **No `UnityEngine` in the folder** | Pure `ProjectAegis.Delegation.AfterAction`; only a future UnityAdapter consumer references Unity. Keeps it headless-testable. |
| **Verbatim mirror, no reconstruction** | `ToEntry` copies combat-event fields 1:1 with no re-sort or derivation, so the ledger can never contradict the combat-event record it reflects. |
| **Advisory-only DTO** | `AfterActionLedgerEntry` carries no `IsFireOrder` / `IsWeaponsReleaseAuthorization` / `IsAutomaticEngagement` / `Enqueue` / `Authorize` (or `Selection` / `Hover` / `Camera` / `Panel`) member — a reflection test fails the build if one is added. It is a review record, never a fire verdict. |
| **Deterministic ordering** | Order is inherited from the deterministic combat-event input; `Filter` preserves it. No wall-clock, no hash-set enumeration leakage. |
| **Empty discipline** | Null/empty input, or a no-match filter, returns `Snapshot.Empty` and the `aal:empty` fingerprint — never null, never a partial row. |
| **Consume-only input** | The ledger depends on the `*Consume` mirror, not the live `CombatEvent` type, keeping its input a stable presentation contract decoupled from the `CombatEvents/` subsystem. |
| **Not fingerprinted into the sim** | Presentation hash only; not in `DecisionLog.ComputeFingerprint()` / `SimWorldHash`, so the Baltic v2 hash is untouched. |

## Extending it

- **Add a ledger field** — add it to `CombatEventRowConsume` **and** `AfterActionLedgerEntry`, copy
  it in `ToEntry`, and **append** it to `ComputeFingerprint` (append-only keeps the `aal:empty`
  sentinel and existing goldens stable). Keep the mirror field-for-field with the DRG-211
  `CombatEvent` so the "no reconstruction" copy stays valid. Add a `[Test]` asserting the field's
  effect on the fingerprint. Never add an authorize/fire/UI field — the DTO-surface test bans it.
- **Add a filter dimension** — add a `string?` to `AfterActionLedgerFilter` and an ordinal-equality
  branch to `MatchesFilter` (a `null` field must stay a wildcard). Add a fixture pinning the new
  filter.
- **Bind a consumer** — read the snapshot in a UnityAdapter Command-Review bridge in the post-Tick
  C2 refresh, like the other C2 feeds ([c2-presentation-bridges.md](c2-presentation-bridges.md)).
  Never call it on the `DelegationBridge.Tick` hot path.
- Nothing here needs a replay-golden re-bless — it is read-only presentation.

## Tests

| Suite (`ProjectAegis.Delegation.Tests`, NUnit) | Covers |
|-----|--------|
| `AfterActionLedgerProjectionTests` (7) | Verbatim field mapping (no reconstruction), filter-by-target / by-outcome / by-shooter+weapon-family (AND-combined, order-preserving), identical-input fingerprint stability (`aal:e=…` prefix), the empty-snapshot → empty-ledger + `aal:empty` sentinel, and the reflection-pinned `IsFireOrder`/`IsWeaponsReleaseAuthorization`/`Enqueue`/`Authorize`/UI-truth DTO-surface ban. |

Verified green with
`dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "FullyQualifiedName~AfterAction"`
(**7 passed / 0 failed**).

## See also

- [engagement-pipeline.md](engagement-pipeline.md) — the *engage-time* kill-chain that produces the outcomes these combat events (and thus ledger rows) record.
- [c2-projection-layer.md](c2-projection-layer.md) — the read-only projection contract this follows.
- [c2-presentation-bridges.md](c2-presentation-bridges.md) — the UnityAdapter host-feed pattern a future after-action / Command-Review consumer would use.
- [autonomy-roe-gating.md](autonomy-roe-gating.md) — the authorization gate; the ledger is a read-only after-action record, never a fire verdict.
- [abort-reason-catalog.md](abort-reason-catalog.md) — the stable outcome / abort codes that appear in an entry's `Outcome` / `ExplanationRef`.
