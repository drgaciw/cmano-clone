# S34-07 — Datalink Catalog-Latency Isolated Fixture Evidence

**Date:** 2026-06-19  
**Story:** S34-07

## Deliverables

- `data/scenarios/baltic-patrol-datalink-catalog-latency.policy.json` — sharing enabled, `shareLagTicks` omitted
- `BalticReplayHarnessDatalinkCatalogLatencyTests.cs` — 6 tests
- `tests/regression/replay-golden-baltic-datalink-catalog-latency-2026-06-19.txt`

## Policy Summary

| Field | Value |
|-------|-------|
| `id` | `baltic-patrol-datalink-catalog-latency` |
| `datalink.organicOnly` | `false` |
| `datalink.shareLagTicks` | **omitted** (catalog applies) |
| `datalink.unitSides` | `u1`/`u2` → `blue` |
| Detection | `u1` Pd=1.0 on `hostile-1`; `u2` Pd=0.0 (peer share only) |

## Catalog → Lag Resolution

- Primary link: `NATO_TADIL_J` (`LatencyMsNominal=50ms`) from `CatalogValidationDefaults.BalticLinks()`
- Formula: `ceil(50 * 60 / 1000) = 3` ticks at 60 Hz
- Harness `DatalinkShareLagResolver` applies at bind — peer share at tick **4** (detection tick 1 + lag 3)

## Golden Hashes (seed=42, ticks=5)

| Hash | Value |
|------|-------|
| `WORLD_HASH` | `12661701758887629394` |
| `DETECTION_WORLD_HASH` | `10718004884873336994` |
| `FINGERPRINT_SHA256` | `942477faee0477d893864af2cef63a8289cdb739b807c4ad9b8ca37b1e9fcfe8` |

## Hard Gates

| Gate | Result |
|------|--------|
| NOT in `ReplayGoldenRegressionCatalog` | **PASS** — 6/6 catalog unchanged |
| Production Baltic `WORLD_HASH` | **PASS** — `17144800277401907079` unchanged |
| `DelegationBridge.cs` | **ZERO** diff |
| ReplayGolden 6/6 | **PASS** |

## Verification

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "DatalinkCatalogLatency|ReplayGoldenSuite" -v minimal

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

## Verdict

**COMPLETE**