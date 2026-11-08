# Sprint 33 — Simulation Track Plan

**Owner:** team-simulation  
**Sprint gate:** S33-04 datalink share gate on comms degrade  
**Epic:** `production/epics/sprint-33-cyber-comms-datalink/`  
**Baseline:** S32 **COMPLETE** — 1073/1073 sln; ReplayGolden **6/6**; S32-09 `BdaContactLifecycleHotTickApplier` landed

## S32-complete context

| S32 sim story | Status | Isolated fixture |
|---------------|--------|------------------|
| S32-04 Facility aspect validator | Done | `baltic-patrol-combat-domains` (existing) |
| S32-05 ECCM scenario factor | Done | scenario factor tests |
| S32-08 Mine transit hazard | Done | `baltic-patrol-mine-transit-hazard` |
| S32-09 BDA contact lifecycle | Done | `baltic-patrol-bda-lifecycle` |

**S33-09 Path B (S32 carryforward) is retired.** All S32-04/05/08/09 ACs are satisfied; no AC clones under S33-09.

## Must-have

| ID | Story | Est. | req_trace |
|----|-------|------|-----------|
| S33-04 | Datalink comms share gate (`DatalinkSidePictureMerger` + `CommsState`) | 1.5d | Req 15, Req 19 |

Gate semantics (bounded MVP):
- Nominal → existing S29-11 / S30-10 merge
- Degraded → no new peer shares; partial picture
- Denied → suppress all share emit

Wiring: `BalticReplayHarness` passes `bridge.CurrentCommsState` — **no** `DelegationBridge.cs` edits.

## Should-have

| ID | Story | Est. | Notes |
|----|-------|------|-------|
| S33-07 | Isolated `baltic-patrol-datalink-comms` fixture | 1d | After S33-04 |
| S33-09 | Phase 6 integration smoke — **reduced** | 0.5d optional | See recommendation below |

## S33-09 recommendation (post-S32)

**Verdict: regression-only / optional — drop before S33-07 on cut line.**

S32 already proved facility, ECCM, mine, and BDA in isolated pins with `Combat|Domain|Facility|Eccm|Mine|Bda` filters PASS (126/126 BDA suite). S33-09 no longer needs 2d Path A (`baltic-patrol-combat-phase6-smoke` combined fixture) or Path B carryforward.

**If capacity remains (Wave 4, last sim wave):**
- Run existing S32 isolated pins + filter suite as regression gate (no new combined fixture required)
- Optional stretch: add `baltic-patrol-combat-phase6-smoke` combined pin — **not** blocking; estimate **0.5d** max

**If Wave 4 slips:** drop S33-09 entirely; S32 isolated evidence + ReplayGolden 6/6 satisfies Phase 6 regression.

## Sim wave order

Sim stories **do not** start until data kill-chain rules land. Ordering within sim track:

| Wave | Story | Depends on | Est. |
|------|-------|------------|------|
| Sim-1 | **S33-04** share gate | S33-01 + **S33-03** kill-chain rules | 1.5d |
| Sim-2 | **S33-07** datalink-comms fixture | S33-04 | 1d |
| Sim-3 | **S33-09** regression smoke (optional) | S33-04; S32-04/05/08/09 done | 0.5d |

> Cross-track: S33-04 was Wave 1 parallel with S33-02 in kickoff — **resequenced** to Sim-1 after S33-03 per data-rule dependency (comms degrade semantics align with kill-chain detect-only posture; no shared code path, but sprint gate ordering prefers data rules first).

## Hard gates (every sim merge)

| Gate | Rule |
|------|------|
| `DelegationBridge.cs` | **ZERO touch** — verify `git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` empty |
| ReplayGolden | **6/6** on default path (`ReplayGoldenRegressionCatalog.All`) |
| Isolated fixtures | S33-07, S33-09 (if built) **excluded** from `ReplayGoldenRegressionCatalog` |
| Production Baltic hash | `17144800277401907079` **pinned** — unchanged unless isolated fixture explicitly pins a new world hash |
| Test floor | ≥1046 day-1 (S32 closeout 1073); closeout ≥1086 |

## Graphite

```
stack/sprint33/full-sln-gate → datalink-share-gate → datalink-comms-fixture → combat-phase6-regression
```

Branch `combat-phase6-regression` is optional — skip if S33-09 dropped.

## Verify

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Datalink|Comms|Contact" -v minimal
/replay-verify
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs  # empty
```

S33-09 regression (optional):

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Facility|Eccm|Mine|Bda" -v minimal
/replay-verify
```

## GitNexus HIGH

`DatalinkSidePictureMerger`, `BalticReplayHarness`

## Cut line (sim track)

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S33-09 (regression-only — S32 evidence sufficient) | S33-04 share gate |
| 2 | — | S33-07 isolated fixture |

**Minimum shippable sim deliverable:** S33-04 only. Should-have floor: S33-04 + S33-07.

## Dispatch

```bash
# After S33-03 lands (Wave 2 data)
/dev-story dispatch S33-04

# After S33-04 merges
/dev-story dispatch S33-07

# Last sim wave — optional
/dev-story dispatch S33-09   # skip if capacity tight
```