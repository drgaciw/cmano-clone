# Swarm contact classification (SWARM-26 / DRG-96)

`SwarmContactClassifier` is the pure, deterministic sensor-side helper that decides whether a
hostile air contact reads as a **single airframe** or a **UAS swarm cloud** — and how confident
the observer is in that call. It models the real ambiguity of low-quality sensing: ground truth
alone is *not* enough, so a genuine swarm can be **mis-identified** when sensor quality is poor.

It lives in [`src/ProjectAegis.Sim/Sensors/`](../../src/ProjectAegis.Sim/Sensors/) alongside the
[detection pipeline](detection-pipeline.md) but is a **distinct concern**:

- [detection-pipeline.md](detection-pipeline.md) answers *"is there a contact, and what is its
  lifecycle state?"* (`Unknown → Detected → Classified → Identified → Lost`).
- **This** helper answers *"is that contact one aircraft or a drone cloud, and how sure are we?"*

It is a **pure derivation seam**: a static function with no RNG, no lifecycle side effects, and
**no production tick-path call site** today (repo-wide `SwarmContactClassifier` hits are only
`SwarmContactClassifier.cs` itself and `SwarmContactClassifierTests`). The source comment states
*"call sites may consume later"*. Documenting it now pins the classification contract so a future
C2 / picture consumer can wire it without re-deriving the thresholds.

> **Scope.** Contact *classification* (SWARM-26 / DRG-96, "SWARM-B5"). The sibling
> `SwarmSensorScale` (SWARM-04 / DRG-89) is a different concern — it scales detection **Pd** by
> living swarm integrity in the detection trial (`ScenarioDetectionTrial`). See
> [Sibling: integrity → Pd scaling](#sibling-integrity--pd-scaling).

---

## Where it lives

| File | Role |
|------|------|
| [`SwarmContactClassifier.cs`](../../src/ProjectAegis.Sim/Sensors/SwarmContactClassifier.cs) | The `Classify(...)` decision function + the tunable threshold constants. |
| [`SwarmContactClass.cs`](../../src/ProjectAegis.Sim/Sensors/SwarmContactClass.cs) | The four-value result enum (`Unknown`/`SingleAirframe`/`UasSwarmCloud`/`PossibleSwarm`). |
| [`SwarmContactClassificationResult.cs`](../../src/ProjectAegis.Sim/Sensors/SwarmContactClassificationResult.cs) | The immutable `(Class, Confidence, ReasonCode)` record. |
| [`SwarmContactLabel.cs`](../../src/ProjectAegis.Sim/Sensors/SwarmContactLabel.cs) | A thin pure projection helper — formats a result as e.g. `UAS swarm cloud (0.82)`. |

---

## The classification result

```csharp
public sealed record SwarmContactClassificationResult(
    SwarmContactClass Class,
    double Confidence,      // clamped to [0, 1]
    string ReasonCode);     // stable machine-readable tag
```

`SwarmContactClass`:

| Value | Meaning |
|-------|---------|
| `Unknown` (0) | Sensor quality too low to form a useful class. |
| `SingleAirframe` (1) | Resolved as a single air platform. |
| `UasSwarmCloud` (2) | Resolved as a multi-vehicle UAS swarm / cloud. |
| `PossibleSwarm` (3) | Ambiguous multi-return / swarm-like signature (a hedge, not a firm call). Used in **both** mid-band swarm hints and the high-band 3–7 count band. |

`Confidence` is always clamped to `[0, 1]` via `SwarmContactClassifier.ClampConfidence`. `ReasonCode`
is a stable string (see [reason-code catalog](#reason-code-catalog)) suitable for logging or UI
tooltips — it explains *why* the class was chosen.

---

## The `Classify` function

```csharp
public static SwarmContactClassificationResult Classify(
    bool targetIsSwarmPlatform,      // ground truth (catalog isSwarm)
    double sensorQuality,            // observer quality, clamped to [0,1]
    int? estimatedCountHint = null,  // optional multi-return count
    bool highResolutionMode = false) // hi-res sensor tightens/boosts the call
```

Classification is driven by three **sensor-quality bands** (`sensorQuality` is clamped to
`[0, 1]` first). `targetIsSwarmPlatform` is ground truth, but the observer only *acts on it* once
quality is high enough — the whole point is that a low-quality look can miss a real swarm.

### Quality bands

| Band | `sensorQuality` | Behaviour |
|------|-----------------|-----------|
| **Low** | `< 0.25` (`LowQualityCeiling`) | Always `Unknown`, even when truth is swarm and the count hint is large — this is the deliberate **misclassification path**. |
| **Mid** | `≥ 0.25` and `< 0.5` (`MidQualityCeiling`) | A swarm signal (truth *or* count `≥` the mid bar) yields `PossibleSwarm` — never a firm `UasSwarmCloud`. Otherwise a weak `SingleAirframe`. |
| **High** | `≥ 0.5` | Firm calls: `UasSwarmCloud`, `PossibleSwarm` (ambiguous count band), or `SingleAirframe`. |

`q == 0.25` is **mid**, not low (`q < LowQualityCeiling`). `q == 0.5` is **high**.

### High-band decision order

1. **Cloud** — `targetIsSwarmPlatform` **or** `count ≥ cloudThreshold` ⇒ `UasSwarmCloud`.
   `cloudThreshold` = `HighCountHintForSwarmCloud` (8), lowered to `HighResCountHintForSwarmCloud`
   (6) when `highResolutionMode`. **Truth swarm wins even when `estimatedCountHint` is null or 0.**
2. **Ambiguous count band** — no truth flag but `count` in `[HighQualityAmbiguousCountMin (3),
   HighQualityAmbiguousCountMax (7)]` ⇒ `PossibleSwarm`. (In hi-res mode counts 6–7 already hit the
   cloud rule above, so the effective band narrows to 3–5.)
3. **Default** ⇒ `SingleAirframe`.

### Mid-band swarm bar

The mid-band count bar is `MidCountHintForPossibleSwarm` (5), lowered to `max(3, 5 − 1) = 4` in
hi-res mode. Truth-swarm at mid quality yields `PossibleSwarm` with reason
`mid_quality_truth_swarm_ambiguous`; a count-only hint yields
`mid_quality_count_hint_possible_swarm`.

### Confidence & high-resolution mode

Confidence grows with `sensorQuality` inside each branch (each branch has its own base + slope).
`highResolutionMode` adds a flat `HighResolutionConfidenceBoost` (`0.08`) via `Boost` in the
mid/high branches; the **low** branch adds `HighResolutionConfidenceBoost * 0.5` (0.04), then the
total is clamped to `[0, 1]`. Hi-res therefore does two things: it **lowers the swarm-cloud count
bar** (8 → 6) and **nudges confidence up** — pinned by
`High_resolution_lowers_swarm_cloud_count_threshold` and `High_resolution_boosts_confidence_slightly`
(which asserts the mid/high delta equals `HighResolutionConfidenceBoost` to 6 decimals).

---

## Tunable constants

All thresholds are `public const` on `SwarmContactClassifier` so call sites and tests reference
them by name rather than magic numbers:

| Constant | Value | Meaning |
|----------|-------|---------|
| `LowQualityCeiling` | `0.25` | Below ⇒ always `Unknown`. |
| `MidQualityCeiling` | `0.5` | Below (and ≥ low) ⇒ only weak/ambiguous classes. |
| `MidCountHintForPossibleSwarm` | `5` | Normal-mode mid-band count that forces `PossibleSwarm`. |
| `HighCountHintForSwarmCloud` | `8` | Normal-mode high-band count that forces `UasSwarmCloud`. |
| `HighResCountHintForSwarmCloud` | `6` | Hi-res high-band cloud count bar. |
| `HighQualityAmbiguousCountMin` / `Max` | `3` / `7` | Inclusive high-band ambiguous `PossibleSwarm` count band. |
| `HighResolutionConfidenceBoost` | `0.08` | Additive confidence boost in hi-res mid/high (`* 0.5` in low). |

---

## Reason-code catalog

`ReasonCode` values are stable machine-readable tags (verified against source + tests):

| Reason code | Class | Condition |
|-------------|-------|-----------|
| `low_quality_unknown` | `Unknown` | `sensorQuality < 0.25`. |
| `mid_quality_truth_swarm_ambiguous` | `PossibleSwarm` | Mid band, truth is swarm. |
| `mid_quality_count_hint_possible_swarm` | `PossibleSwarm` | Mid band, count ≥ mid bar (no truth). |
| `mid_quality_single_airframe_weak` | `SingleAirframe` | Mid band, no swarm signal. |
| `high_quality_truth_swarm_cloud` | `UasSwarmCloud` | High band, truth is swarm. |
| `high_quality_count_hint_swarm_cloud` | `UasSwarmCloud` | High band, count ≥ cloud threshold (no truth). |
| `high_quality_count_band_possible_swarm` | `PossibleSwarm` | High band, count in `[3,7]` (no truth). |
| `high_quality_single_airframe` | `SingleAirframe` | High band, otherwise. |

---

## Projection helper

[`SwarmContactLabel.Format(result)`](../../src/ProjectAegis.Sim/Sensors/SwarmContactLabel.cs) is a
pure string formatter for a future UI / C2 picture consumer:

| Class | Formatted label (confidence `0.82`) |
|-------|-------------------------------------|
| `UasSwarmCloud` | `UAS swarm cloud (0.82)` |
| `PossibleSwarm` | `Possible swarm (0.82)` |
| `SingleAirframe` | `Single airframe (0.82)` |
| `Unknown` | `Unknown (0.82)` |

Confidence is rendered with the `0.00` format. It throws `ArgumentNullException` on a null result
(hand-written for `netstandard2.1`, which lacks `ArgumentNullException.ThrowIfNull`). An
unrecognized enum value falls through to `result.Class.ToString()`.

---

## Determinism

- **Pure & deterministic.** `Classify` is a static function of its four arguments only — no RNG,
  no clock, no state. `Classify_is_deterministic_for_same_inputs` pins that equal inputs give an
  equal `record` (class, confidence, reason).
- **Off the fingerprint.** With no tick-path call site today, it contributes nothing to the
  order-log hash or the Baltic v2 replay hash `17144800277401907079`. A future consumer that feeds
  classification into the fingerprinted picture must follow the [detection pipeline](detection-pipeline.md)
  determinism rules (sorted iteration, no wall-clock).
- **Confidence always bounded.** Every return path routes through `ClampConfidence` ⇒ `[0, 1]`
  (pinned by the `Confidence_is_clamped_0_to_1` theory across out-of-range quality inputs).

---

## Sibling: integrity → Pd scaling

Do not confuse classification with **detection scaling**.
[`SwarmSensorScale`](../../src/ProjectAegis.Sim/Sensors/SwarmSensorScale.cs) (SWARM-04 / DRG-89) is a
separate pure helper that multiplies base detection **Pd** by a living-swarm **integrity fraction**
(`droneCount / maxDrones`, clamped `[0,1]`, raised to `IntegrityPower` = `1.0` by default):

- `IntegrityFraction(droneCount, maxDrones)` → `[0,1]` (0 when either count ≤ 0).
- `ScaleFactor(...)` → monotonic non-decreasing multiplier (`MinLivingScale` default `0.0`).
- `ScalePd(basePd, ...)` → `basePd × ScaleFactor`, clamped `[0,1]`; `0` when no drones remain.

It plugs into the detection **trial** (`Sim/Scenario/ScenarioDetectionTrial`), so a depleted swarm
gets progressively harder to detect — the *probability* side of the same swarm-sensor story, versus
the *class* side documented above.

---

## Tests & related

| Where | What |
|-------|------|
| [`SwarmContactClassifierTests`](../../src/ProjectAegis.Sim.Tests/Sensors/SwarmContactClassifierTests.cs) | Pins every band, reason code, hi-res behaviour, determinism, confidence clamp, and label formatting. |
| [`SwarmSensorScaleTests`](../../src/ProjectAegis.Sim.Tests/Sensors/SwarmSensorScaleTests.cs) · [`SwarmDetectionLoopIntegrationTests`](../../src/ProjectAegis.Sim.Tests/Sensors/SwarmDetectionLoopIntegrationTests.cs) | The integrity-Pd sibling. |
| [detection-pipeline.md](detection-pipeline.md) | Contact detection + lifecycle FSM (the consumer this classifier will feed). |
| [swarm-runtime.md](swarm-runtime.md) | The aggregate drone-swarm runtime whose `droneCount` feeds `SwarmSensorScale`. |
