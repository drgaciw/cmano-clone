# Doctrine inheritance & override runtime

How a unit's **effective doctrine** (ROE + max-salvo) is resolved from scenario defaults, and
how the player overrides it per unit at runtime (req 13 P0, ADR-010 / ADR-002). This is the
*resolution and override* layer that sits **above** [`EffectivePolicy`](../../src/ProjectAegis.Sim/Policy/) and **feeds** the
[autonomy / ROE gate](autonomy-roe-gating.md) — it decides *which* `EffectivePolicy` a unit
carries; the gate decides what that policy *permits*.

There are three distinct surfaces, all deterministic and replay-safe:

1. **Sim-runtime resolution** — the precedence chain a live unit's policy is resolved through.
2. **Interactive override** — the headless command that changes one unit's ROE mid-run.
3. **Authoring-time validation** — the `DOCTRINE_RESOLVED` finding that reports resolved ROE per mission before publish.

Verified against source; pinned by the delegation policy tests and the scenario-validation
golden hashes (`ValidationGoldenHashes`).

---

## Where it lives

| Type | File | Role |
|------|------|------|
| `ScenarioPolicyProfile.ResolveUnitPolicy` | [`Sim/Scenario/ScenarioPolicyProfile.cs`](../../src/ProjectAegis.Sim/Scenario/ScenarioPolicyProfile.cs) | The **precedence chain** — unit override → mission ROE → side default. |
| `ResolvedUnitPolicy` | [`Sim/Policy/ResolvedUnitPolicy.cs`](../../src/ProjectAegis.Sim/Policy/ResolvedUnitPolicy.cs) | `(EffectivePolicy Effective, bool RoeInheritedFromMission)` — resolution + provenance. |
| `DoctrineOverrideCommand` | [`UnityAdapter/Bridge/DoctrineOverrideCommand.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DoctrineOverrideCommand.cs) | Interactive per-unit ROE override (Bridge write path). |
| `PolicySnapshotRegistry` | [`Delegation/Orchestration/PolicySnapshotRegistry.cs`](../../src/ProjectAegis.Delegation/Orchestration/PolicySnapshotRegistry.cs) | Per-unit pinned `EffectivePolicy` snapshots (`Capture` → monotonic id). |
| `PolicyUpdateRecord` | [`Delegation/Decision/PolicyUpdateRecord.cs`](../../src/ProjectAegis.Delegation/Decision/PolicyUpdateRecord.cs) | Order-log row for a doctrine/ROE change (C1). |
| `DoctrineInheritanceProjection` | [`Delegation/Projection/DoctrineInheritanceProjection.cs`](../../src/ProjectAegis.Delegation/Projection/DoctrineInheritanceProjection.cs) | Read-model: per-unit inheritance panel rows (source label + override state). |
| `DoctrineMapOverlayProjection` | [`Delegation/Projection/DoctrineMapOverlayProjection.cs`](../../src/ProjectAegis.Delegation/Projection/DoctrineMapOverlayProjection.cs) | Read-model: doctrine labels onto map symbol positions (CMD-33). |
| `ValidationRules.DoctrineInheritanceRule` | [`Data/Validation/Rules/ValidationRules.cs`](../../src/ProjectAegis.Data/Validation/Rules/ValidationRules.cs) | Authoring-time `DOCTRINE_RESOLVED` finding (AME-3.2 / AC-4). |

---

## 1. Sim-runtime resolution — the precedence chain

`ScenarioPolicyProfile.ResolveUnitPolicy(unitKey, isFriendly)` returns the unit's
`EffectivePolicy` **and** a provenance flag, checked in this fixed order (first match wins):

| Precedence | Source | Condition | `RoeInheritedFromMission` |
|-----------:|--------|-----------|:-------------------------:|
| 1 (highest) | **Unit override** | `UnitOverrides[unitKey]` present | `false` |
| 2 | **Mission ROE** | `MissionRoe != null` **and** `unitKey ∈ MissionUnitIds` (ordinal, case-insensitive) | `true` |
| 3 (fallback) | **Side default** | otherwise: `isFriendly ? FriendlyDefault : OpposingDefault` | `false` |

```csharp
ResolvedUnitPolicy resolved = profile.ResolveUnitPolicy("u1", isFriendly: true);
EffectivePolicy   effective = resolved.Effective;              // (RoeLevel, MaxSalvo)
bool              fromMission = resolved.HasInheritedDoctrineFromMission;
// EffectivePolicy-only shorthand:
EffectivePolicy   p = profile.ResolveForUnit("u1", isFriendly: true);
```

`OpposingDefault` falls back to `EffectivePolicy.DefaultFree` when the profile omits it. The
underlying `EffectivePolicy` `(RoeLevel, MaxSalvo)` and the JSON that populates
`FriendlyDefault` / `OpposingDefault` / `unitOverrides` / mission ROE are documented in
[`scenario-policy-authoring.md`](scenario-policy-authoring.md); gameplay numbers live in
`data/scenarios/*.policy.json`, never in C#.

The engage path reads this via `DelegationOrchestrator.ResolveEffectivePolicyForUnit(unitId)`,
which the `MvpEngagementResolver` is wired to at bind time — so the resolved doctrine is what
the [ROE gate](autonomy-roe-gating.md) and [engagement pipeline](engagement-pipeline.md) act on.

---

## 2. Interactive override — `DoctrineOverrideCommand`

`DoctrineOverrideCommand.TryApply(orchestrator, unitId, roeLevelLabel, simTime)` is the headless
command behind the C2 per-unit ROE override button (req 13 P0, ADR-010). It does **not** mutate
the scenario profile — it pins a new per-unit snapshot and logs the change:

```text
TryApply(orchestrator, unitId, "WeaponsFree", simTime)
  → parse roeLevelLabel → RoeLevel      (case-insensitive; unparseable → return false)
  → current = orchestrator.ResolveEffectivePolicyForUnit(unitKey)
  → if current.Roe == requested        → return false        (idempotent no-op)
  → newPolicy = (requested, current.MaxSalvo)                (MaxSalvo preserved)
  → snapshotId = orchestrator.PolicySnapshots.Capture(unitId, newPolicy, simTick)
  → DecisionLog.AppendPolicyUpdate(PolicyUpdateRecord(field="roe", prev, new))
  → return true
```

Key properties:

- **ROE only.** The command changes `RoeLevel` and preserves the unit's current `MaxSalvo`.
- **Idempotent.** Re-applying the current ROE is a no-op that returns `false` (no snapshot, no log row).
- **Order-logged.** Every applied change appends one `PolicyUpdateRecord` (`Field = "roe"`,
  `PreviousValue`/`NewValue` = ROE labels, carrying the new `PolicySnapshotId`), so it participates
  in replay and appears in the message log.
- **Snapshot-pinned.** The per-unit `PolicySnapshotRegistry` (also used at agent-assign time and by
  the [mission contact-trigger escalation](mission-timeline-runtime.md)) is the authoritative
  runtime doctrine store; its pinning contract and the ROE/WRA split are covered in
  [`autonomy-roe-gating.md`](autonomy-roe-gating.md).

> The mission contact-trigger runtime writes the *same* `PolicyUpdateRecord` shape when Baltic v3
> escalates to Weapons Free on first recon contact — see
> [`mission-timeline-runtime.md`](mission-timeline-runtime.md). `DoctrineOverrideCommand` is the
> **player-initiated** counterpart.

---

## 3. Read-models — inheritance panel & map overlay

`DoctrineInheritanceProjection.ProjectUnit(unitId, policy, isFriendly)` builds a pure
`DoctrineInheritanceEntry` for the C2 doctrine panel:

- `EffectiveRoeLabel` / `EffectiveMaxSalvoLabel` / `EffectiveEmconLabel` from the resolved policy
  (EMCON via `ScenarioEmconResolver.ResolveRadar`).
- `InheritanceSource` label — `"SOURCE: Mission"` (mission-inherited) → `"SOURCE: Unit Override"`
  (a `UnitOverrides` key exists) → `"SOURCE: Scenario Default"`.
- `HasLocalOverride` + `OverrideButtonLabel` (`OVERRIDE: ACTIVE` / `NONE` / `UNAVAILABLE` when no
  policy is loaded).
- `ProjectAllUnits(...)` emits rows in **ordinal `UnitId` order** (deterministic).

`DoctrineMapOverlayProjection.Project(inheritanceEntries, mapSymbols?)` (CMD-33) maps those rows
onto optional `MapSymbolEntry` positions (matched by `SymbolId == UnitId`, first match wins),
also ordered ordinally by `UnitId`, for host map counting/bind. Both are **presentation-only**
(no mutation), consistent with the [C2 projection layer](c2-projection-layer.md) read-only
contract that protects replay determinism.

---

## 4. Authoring-time validation — `DOCTRINE_RESOLVED`

Separately from the runtime chain, the scenario **validation engine** (ADR-008) reports how each
mission's ROE resolves *before* publish, via `ValidationRules.DoctrineInheritanceRule`
(AME-3.2 / AC-4). This is an **authoring-document** concern (`ScenarioDocumentDto`), so its
precedence differs from the sim-runtime chain above:

| Precedence | Source | Condition | `inheritanceSource` |
|-----------:|--------|-----------|--------------------|
| 1 | **Mission override** | `mission.RoeOverride` set | `"override"` |
| 2 | **Side default** | `scenario.Metadata.SideRoe` set | `"side"` |
| 3 | **Hard default** | neither set | `"side"` with value `WeaponsFree` |

Each mission emits one `DOCTRINE_RESOLVED` finding at **`Info`** severity (never blocks export)
with `Data["resolvedRoe"]` / `Data["inheritanceSource"]`. These findings are surfaced separately
in `ValidationReport.DoctrineResolution` (ordinal by mission id) and are part of the pinned
`ValidationGoldenHashes` (`StrikeUnreachable`, `CleanPatrol`). The validation engine itself
(ordered rules, report hashing, export gate) is described in [ADR-008](../architecture/adr-008-mission-editor-validation-engine.md).

> **Two chains, on purpose.** The runtime chain (§1) puts a live *unit override* above mission
> ROE; the authoring rule (§4) reports per *mission* (documents have no live unit-override slot).
> Don't conflate them — one resolves a running unit's policy, the other lints an authoring doc.

---

## Determinism & invariants

1. **Pure resolution.** `ResolveUnitPolicy` and both projections are pure functions of
   `(profile, unitId, isFriendly[, mapSymbols])` — no RNG, no wall-clock, ordinal ordering only.
2. **Override is order-logged, snapshot-pinned.** A doctrine change lands as a `PolicyUpdateRecord`
   against a captured `PolicySnapshotId`; it is replayable and idempotent (no-op when unchanged).
3. **Player override cannot loosen the ROE gate's own guarantees.** The override changes the
   *resolved policy*; the [autonomy / ROE gate](autonomy-roe-gating.md) still applies its
   ROE-first checks (player approval can't override ROE).
4. **Read-models never mutate** sim state — they only shape the resolved policy for display.
5. **Replay-safe.** None of these paths feed new floats into the fingerprinted world/order-log
   hashes beyond the already-logged `PolicyUpdateRecord`, so the Baltic v2 hash
   `17144800277401907079` is untouched.

---

## Extending it — pitfalls

- **New precedence tier?** Add it inside `ResolveUnitPolicy` (keep the first-match order explicit)
  and set `RoeInheritedFromMission` correctly — the panel `InheritanceSource` label reads it.
- **Overriding more than ROE?** `DoctrineOverrideCommand` deliberately touches ROE only and
  preserves `MaxSalvo`. Widening it means a new `PolicyUpdateRecord.Field` value and a matching
  message-log/replay expectation.
- **Don't mutate the `ScenarioPolicyProfile`** at runtime for an override — capture a new
  `PolicySnapshot` instead, so the change is logged and replayable.
- **Keep the two chains distinct.** The authoring `DOCTRINE_RESOLVED` rule operates on
  `ScenarioDocumentDto`; do not reuse it as the runtime resolver (or vice versa).

---

## Tests

| Area | Where |
|------|-------|
| Runtime resolution precedence | `ProjectAegis.Sim.Tests` (scenario policy profile) |
| Interactive override + policy update log | `ProjectAegis.Delegation(.UnityAdapter).Tests` (bridge doctrine override) |
| Inheritance / map-overlay projections | `ProjectAegis.Delegation.Tests/Projection` |
| `DOCTRINE_RESOLVED` finding + golden hashes | `ProjectAegis.Data.Tests/Validation` (`ValidationGoldenHashes`) |

## See also

| Topic | Doc |
|-------|-----|
| The ROE/autonomy gate this feeds; `PolicySnapshotRegistry` pinning; ROE/WRA split | [autonomy-roe-gating.md](autonomy-roe-gating.md) |
| Authoring `FriendlyDefault` / `OpposingDefault` / `unitOverrides` / mission ROE in JSON | [scenario-policy-authoring.md](scenario-policy-authoring.md) |
| Contact-triggered ROE escalation (same `PolicyUpdateRecord` shape) | [mission-timeline-runtime.md](mission-timeline-runtime.md) |
| The read-model layer the projections belong to | [c2-projection-layer.md](c2-projection-layer.md) |
| Headless-first, command-driven UI boundary | [ADR-010](../architecture/adr-010-headless-first-command-driven-ui.md) |
| Policy evaluator boundary (`EffectivePolicy`) | [ADR-002](../architecture/adr-002-policy-evaluator.md) |
| Scenario validation engine (`DOCTRINE_RESOLVED`) | [ADR-008](../architecture/adr-008-mission-editor-validation-engine.md) |
