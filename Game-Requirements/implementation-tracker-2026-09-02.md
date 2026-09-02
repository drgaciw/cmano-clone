# Game Requirements — Implementation Tracker

**Base:** `main` @ `103a5756` (post–requirements-audit remediation)  
**Last Updated:** 2026-09-02  
**Supersedes:** [implementation-tracker-2026-07-04.md](implementation-tracker-2026-07-04.md)  
**Index:** [Game-Requirements-Index.md](Game-Requirements-Index.md) | [00-Master-Index.md](../00-Master-Index.md)  
**Consistency:** [requirements-consistency-2026-09-02.md](../docs/reports/requirements-consistency-2026-09-02.md)

## Verdict

| Scope | Status |
|-------|--------|
| Requirements **documentation** (01–22) | **Current** — hub floors ≥1638 / PlayModeSmoke ≥20/20; ADR-005 **Superseded** in architecture hub; req 20 **CMD-31…39** appended; doc 06 **DBI-GOV-1…4** residuals |
| **MVP / Phase 1 gameplay** implementation | **COMPLETE 21/21** (S56 gate PASS 2026-06-21) — grades frozen; Baltic ACs (replay 6/6, proxy ≥20/20, hash `17144800277401907079`) |
| **Post-MVP content programs** (S57–S80) | **COMPLETE** — Baltic v2/v3; stage **Release** |
| **Forward engineering** (post-S80) | Editors + UI Maturity (CMD-31…39) shipped headless; Phase N speculative (DRG-47) deferred |

Completing the full requirements corpus as *shipped game features* is multi-year work (req 07 phases 1–5). **Post-MVP tracks add evidence additively; they do not re-litigate S56 MVP row grades.**

## Verification baseline

**Gate baseline (current, AGENTS.md):** ≥**1638/0f**; ReplayGolden **6/6**; PlayMode smoke **≥20/20**; hash **`17144800277401907079`** preserved; ZERO `DelegationBridge` hotpath edits.

**Historical floors (do not use as current law):** ≥1232 / PlayModeSmoke 18/18 (S80); ≥1599 (post-PE 2026-07-09).

```bash
dotnet build ProjectAegis.sln -m
dotnet test ProjectAegis.sln -v minimal                    # floor ≥1638/0f
dotnet test ProjectAegis.sln --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter CatalogGovernanceIntegrity
```

## GitNexus intelligence (2026-09-02)

| Symbol | Upstream impact | Risk | Notes |
|--------|-----------------|------|-------|
| `CatalogWriteGate` | ~190 symbols | **CRITICAL** | extend-only — remediation used additive `CatalogGovernanceIntegrity` only |
| `DbSnapshotStore` | ~58 symbols | **CRITICAL** | `RecordRelease` already fail-closes empty hash (DBI-GOV-1) |
| `DelegationBridge` | ZERO hotpath | **CRITICAL** | no hotpath edits this pass |
| `C2CommandIssuance` / `LiveEditApplyState` | present | LOW | August CMD-31 / CMD-35 evidence |

**Index:** `npx gitnexus analyze --force` @ HEAD `103a5756` → ~43,975 nodes / 87,091 edges.

## Requirements audit remediation (2026-09-02)

| Workstream | Deliverable |
|------------|-------------|
| Hub currency | req 01/02/03/08/20 floors; `architecture.md` ADR-005 **Superseded**; RTM gates ≥1638 / ≥20/20 |
| August C2 | req 20 **CMD-31…39** + honesty flips for shipped CMD-16…30 subset |
| Governance | doc 06 DBI-GOV-1…4; `CatalogGovernanceIntegrity` + tests; no CatalogWriteGate rewrite |
| Consistency | this tracker + [requirements-consistency-2026-09-02.md](../docs/reports/requirements-consistency-2026-09-02.md) |

## S56 MVP rows

Frozen — cite [implementation-tracker-2026-06-04.md](implementation-tracker-2026-06-04.md) for full evidence chains. Additive notes only in later trackers.

## Open residuals (non-blocking)

| ID | Residual | Owner |
|----|----------|-------|
| DBI-GOV-2…4 | Seed/legacy `baltic_patrol` change-log, watermark, reviewer backfill | Data / catalog seed |
| CMD-25 | Boat ops Phase N | doc 16 LOG-09…11 |
| OV-SC-N1 | 5k@60 / Cesium product globe | Phase N |
| C-03 | Commercial product name | Product |
