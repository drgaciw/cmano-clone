# Sprint 10 — Fuel ledger, replay hash, QA prep

**Branch:** `stack/sprint10-fuel-replay-qa-prep`  
**Goal:** Close logistics P1 (fuel ledger + tick burn log), SHA-256 replay gate surface, catalog promote, headless QA evidence.

## Done (PR #55 + stack)

- [x] `FuelStateChange` band transitions (burn model)
- [x] `PlayerOrder` on human enqueue
- [x] `CatalogQuarantinePromoter`
- [x] `OrderLogReplayFingerprint.ComputeSha256Hex`
- [x] Tests 272/272

## In progress (this stack)

- [x] `FuelLedger` per-unit tick burn (Sim)
- [x] `FuelBurn` order-log rows (optional `logTickBurn` on scenario logistics)
- [x] Harness `FINGERPRINT_SHA256=` line
- [x] ADR-010 headless-first command-driven UI

## QA gate (unchanged)

- Unity manual C2 sign-off — `production/qa/c2-manual-signoff-2026-06-02.md`
- Cesium spike — Editor-only

## Verify

```bash
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
```