# Sprint 17 — Smoke close-out gate

**Date:** 2026-06-04  
**Build:** `main` @ `cde26fe`  
**Verdict:** **PASS** (automated gates)

## Scope closed this sprint

| Task | Status |
|------|--------|
| DATA-4 — `ValidationPipeline`, `TryGetWeaponEnvelope`, engage envelope | Done @ `9d46c64` |
| DATA-5 — `CmoMarkdownImporter`, write-gate smoke | Done @ `cde26fe` |

## Automated gates

| Gate | Command | Result |
|------|---------|--------|
| Build | `dotnet build ProjectAegis.sln -v minimal` | **PASS** (0 errors) |
| Full suite | `dotnet test ProjectAegis.sln -v minimal` | **380/380 PASS** |
| PlayMode smoke | `--filter PlayModeSmokeHarnessTests` | **7/7 PASS** |
| Replay golden | `--filter ReplayGolden\|ReplayOrderLog` | **7/7 PASS** |
| Data import smoke | `CmoMarkdownImportSmokeTests` | Included in 58 Data tests |

## GitNexus

- DATA-5: no `CatalogWriteGate` signature changes (caller-only)
- `npx gitnexus detect_changes` at DATA-5 merge: **low risk**

## Open (not blocking sprint close)

| Item | Owner | Note |
|------|-------|------|
| Unity C2 manual sign-off | Human QA | 13 checks — `production/qa/c2-manual-signoff-2026-06-02.md` (+ attack menu row) |
| GitHub Actions billing | Producer | Local gate evidence used for prior merges |
| Full CMO catalog crawl | Sprint 18+ | P0 stack complete; Phase 2 import deferred |
| Hindsight retain | Agent | Server `localhost:8888` optional |

## Recommendation

Close Sprint 17; open **Sprint 18** on backlog themes (QA sign-off + OSINT spike + catalog Phase 2 planning).