# Release Checklist — Baltic v1.0 RC1

**Date:** 2026-06-20  
**Authority:** S46 B5 + release-enablement-scope-boundary + S41 ack "i provide the ack"  
**Target:** Ship C2 + Platform Editor + Baltic replay path (limited vertical slice)

## Pre-requisites (B1-B5)
- [x] B1 Content: 13 rows (S42/S43 waves) — verified in smoke + sprint-status
- [x] B2 Art bible partial (§1-9) — per S43
- [x] B3 Structural (S44) + GitNexus
- [x] B4 Perf (S45)
- [x] B5 Launch artifacts (this checklist + evidence)

## Build & Gates (S47/S48)
- [x] dotnet build Release: 0 errors
- [x] Full tests: 1226 PASS (Data 403, Sim 279, Delegation 245, UA 252, Cli 42, Excel 5)
- [x] ReplayGoldenSuite: 6/6 PASS (Baltic hash 17144800277401907079 preserved)
- [x] PlayModeSmokeHarness: 18/18 PASS
- [x] ZERO DelegationBridge.cs touch (verified)
- [x] CatalogWriteGate extend-only (projection changes only)
- [x] GitNexus index current; impacts on Catalog* logged for changes
- [x] RC1 analyzer hygiene (xUnit2012): TDD fix in Osint/OsintDigestRunnerTests.cs(24) Assert.True->Assert.Contains; cites S41 ack "i provide the ack", release-enablement-scope-boundary-2026-06-20.md, CatalogSensorBinding prior extension, GitNexus impact pre-edit. Data.Tests 403/403 PASS. (2026-06-20)

## Artifacts
- Release checklist: this file
- Evidence: production/qa/ (smoke-*-2026-06-20, evidence/README-*)
- Determinism: production/determinism/*-2026-06-20.md
- Gate: production/gate-checks/scope-expansion-decision-2026-06-20-S41-close.md (PASS with ack)
- Sprint status: updated with S41-S45 complete

## Go/No-Go
- All invariants held.
- No production hash change.
- RC1 candidate ready for S48 human gate + stage advance.

**Verdict:** RC BUILD GREEN. Ready for release dry-run / gate-check.

