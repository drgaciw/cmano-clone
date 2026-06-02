# Story 001: ContactChange order-log rows

> **Epic**: Sensor Headless Slice
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 3h

## Context

**GDD**: `design/gdd/sensor-detection-ew.md`, `design/gdd/order-log-replay.md`
**Requirement**: TR-sensor-001 (partial), order-log ContactChange variant

**Governing ADRs**: ADR-003

## Acceptance Criteria

- [x] `OrderLogEntryKind.ContactChange` with payload observer/contact/target + states.
- [x] `DecisionLog.AppendContactChange` + `ComputeFingerprint()` includes rows.
- [x] Regression: duplicate append → identical fingerprint.

## Test Evidence

- `src/ProjectAegis.Delegation.Tests/Decision/ContactChangeOrderLogTests.cs`