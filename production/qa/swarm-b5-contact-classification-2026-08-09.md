# SWARM-B5 — Contact classification for swarms (DRG-96)

**Date:** 2026-08-09  
**Linear:** [DRG-96](https://linear.app/drgamtd-workspace/issue/DRG-96) · Epic [DRG-83](https://linear.app/drgamtd-workspace/issue/DRG-83) · Milestone H8 Phase B  
**Requirements:** SWARM-26  
**Surface:** `src/ProjectAegis.Sim/Sensors/**` · `src/ProjectAegis.Sim.Tests/Sensors/**` · this QA note  
**Verdict:** PASS (agent — pure classifier + unit tests)

## Scope

Hostile swarm contacts expose **classification** that can distinguish **UAS swarm cloud** from **single airframe** when sensors allow, with **confidence**. Misclassification is possible at low quality (truth `isSwarm` alone does not force cloud ID).

| Concern | Implementation |
|---------|----------------|
| Class enum | `SwarmContactClass` — Unknown / SingleAirframe / UasSwarmCloud / PossibleSwarm |
| Result | `SwarmContactClassificationResult` (Class, Confidence 0..1, ReasonCode) |
| Pure API | `SwarmContactClassifier.Classify(targetIsSwarmPlatform, sensorQuality, estimatedCountHint?, highResolutionMode)` |
| Label helper | `SwarmContactLabel.Format(result)` → e.g. `UAS swarm cloud (0.82)` |

**Not in this PR:** PdDetectionContactSimulator lifecycle wiring (standalone classifier first), Unity UI, C2 panel chrome, Swarm/** / Cec/** / Engage/** / Data/** / Delegation/** surfaces.

## Classification rules (deterministic)

| Sensor quality | Outcome |
|----------------|---------|
| `q < 0.25` | **Unknown**, low confidence (even if truth is swarm — misclassification path) |
| `0.25 ≤ q < 0.5` | If `isSwarm` or count hint ≥ 5 (4 in hi-res) → **PossibleSwarm**; else weak **SingleAirframe** |
| `q ≥ 0.5` | If `isSwarm` or count ≥ 8 (6 in hi-res) → **UasSwarmCloud** (conf scales with q); else count 3..7 → **PossibleSwarm**; else **SingleAirframe** |
| `highResolutionMode` | +0.08 confidence boost; lowers multi-return threshold for **UasSwarmCloud** |

Confidence is always clamped to `[0, 1]`. Same inputs → same outputs (pure function).

## Acceptance criteria

| AC | Evidence | Verdict |
|----|----------|---------|
| Low quality → Unknown | `Low_quality_yields_Unknown` | **PASS** |
| High quality + isSwarm → UasSwarmCloud | `High_quality_plus_isSwarm_yields_UasSwarmCloud` | **PASS** |
| High quality + single → SingleAirframe | `High_quality_single_yields_SingleAirframe` | **PASS** |
| Mid quality + count hint → PossibleSwarm | `Mid_quality_plus_count_hint_yields_PossibleSwarm` | **PASS** |
| Misclassification at low quality (swarm not fully ID'd as cloud) | `Low_quality_even_with_swarm_truth_stays_Unknown_not_UasSwarmCloud`; `Mid_quality_swarm_truth_is_PossibleSwarm_not_full_cloud` | **PASS** |
| Determinism | `Classify_is_deterministic_for_same_inputs` | **PASS** |
| Confidence clamped 0..1 | `Confidence_is_clamped_0_to_1` | **PASS** |

## Gates

| Gate | Result |
|------|--------|
| Surface discipline | Sensors + Sensors tests + production/qa only |
| Pure functions / no Unity | `SwarmContactClassifier` / `SwarmContactLabel` are static pure C# |
| Lifecycle left intact | `PdDetectionContactSimulator` not rewritten |
| Filtered unit tests | `FullyQualifiedName~SwarmContact\|FullyQualifiedName~PdContactClassify` |

## Key types / files

- `src/ProjectAegis.Sim/Sensors/SwarmContactClass.cs`
- `src/ProjectAegis.Sim/Sensors/SwarmContactClassificationResult.cs`
- `src/ProjectAegis.Sim/Sensors/SwarmContactClassifier.cs`
- `src/ProjectAegis.Sim/Sensors/SwarmContactLabel.cs`
- `src/ProjectAegis.Sim.Tests/Sensors/SwarmContactClassifierTests.cs`
- `production/qa/swarm-b5-contact-classification-2026-08-09.md`

## Verify commands

```bash
export PATH="$HOME/.dotnet:$PATH"
cd /workspace/artifacts/cmano-clone/.worktrees/drg-96-b5
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "FullyQualifiedName~SwarmContact|FullyQualifiedName~PdContactClassify" -v minimal
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "FullyQualifiedName~Sensors" -v minimal
```

## Follow-ons

- Lifecycle / contact simulator may consume `SwarmContactClassifier` at classify/identify ticks
- C2 / map projection via `SwarmContactLabel.Format`
- CEC mesh / composite track (B6) remains surface-disjoint
