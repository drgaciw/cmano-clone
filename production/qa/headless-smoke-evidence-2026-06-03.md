# Headless smoke evidence — 2026-06-03

| Gate | Result | Notes |
|------|--------|-------|
| `dotnet test ProjectAegis.sln` | PASS | 276+ tests (fuel ledger + FuelBurn rows) |
| PlayMode smoke | PASS | `PlayModeSmokeHarnessTests` |
| Fuel ledger AC-3 | PASS | `FuelLedgerTests`, `FuelTimelineTrackerTests`, `BalticReplayHarnessFuelTests` |
| Replay SHA-256 | PASS | `OrderLogReplayFingerprintSha256Tests`, harness `FingerprintSha256` |

## Stack

- Branch: `stack/sprint10-fuel-replay-qa-prep`
- PR: #55 + follow-up commits (fuel ledger, ADR-010)

## Still manual

- Unity C2 sign-off (`production/qa/c2-manual-signoff-2026-06-02.md`)
- Cesium spike (Editor)