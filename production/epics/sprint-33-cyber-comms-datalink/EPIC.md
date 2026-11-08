# Epic: Sprint 33 — Cyber/Comms Datalink Integration

> **Status:** Ready  
> **Sprint:** 33  
> **GDD:** Req 15 (datalinks), Req 19 (comms degrade), Req 18 (Phase 6 integration)

## Goal

Close **datalink share gate on comms degrade** (S32 deferred), prove on isolated fixtures, and integrate Phase 6 combat outputs when S32 completes.

## Stories

| # | Story | ID | Priority | Est. | Status |
|---|-------|-----|----------|------|--------|
| 04 | [Datalink comms share gate](story-033-04-datalink-comms-share-gate.md) | S33-04 | must-have | 1.5d | Not Started |
| 07 | [Datalink-comms isolated fixture](story-033-07-datalink-comms-fixture.md) | S33-07 | should-have | 1d | Not Started |
| 09 | [Phase 6 integration smoke](story-033-09-phase6-integration-smoke.md) | S33-09 | should-have | 2d | Not Started |

## Sprint gate

**S33-04** — `DatalinkSidePictureMerger` gates peer share on `CommsState`; default path unchanged.

## GitNexus

- **HIGH:** `DatalinkSidePictureMerger`, `BalticReplayHarness`
- **ZERO touch:** `DelegationBridge.cs`