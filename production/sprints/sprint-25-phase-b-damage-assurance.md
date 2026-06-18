# Sprint 25 — Phase B Damage Model + Presentation Assurance

**Dates:** 2026-08-07 → 2026-08-20  
**Trunk:** `main` @ `9ecbf2c` (Sprint 24 stack merged; 592/592 baseline)  
**Predecessor:** Sprint 24 — Platform Editor Phase B Import Loop + Presentation Polish (complete, 9/9 + stretch on branches)

## Sprint Goal

**Close ADR-011 Phase B** by landing damage-model columns on the Platforms sheet through the full Excel write-gate round-trip (tracker row 21 complete); **merge Sprint 24 stretch carry-over** (S24-10 EMCON panel, S24-11 ClosedXML UX); and **close presentation assurance gaps** (Cesium Editor evidence, C2 tri-batch sign-off, APP-6 glyph atlas foundation) while maintaining **≥592** full-solution tests and **6/6** replay goldens.

## Capacity

| Metric | Value |
|--------|-------|
| Total days | 10 |
| Buffer (20%) | 2 days reserved |
| **Effective dev-days** | **8** |

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S25-01 | **Full-solution re-baseline** — day-1 `dotnet build` + `dotnet test ProjectAegis.sln`; record ≥592 in `sprint-status.yaml` + smoke doc; GitNexus analyze @ trunk | c-sharp-devops-engineer | 1 | S24 merged | 0 errors; 0 failures; ReplayGolden 6/6; indexed commit recorded |
| S25-02 | **Damage schema migration `009`** — additive platform damage columns + staging; `CatalogPlatformDamage` types | team-data | 1 | S25-01 | Migration idempotent; Baltic fixture seeded; no new workbook sheets |
| S25-03 | **Damage reader + export** — `ICatalogReader.TryGetPlatformDamage`; Platforms sheet columns in export | team-data | 1.5 | S25-02 | Export payload includes damage fields; reader tests PASS |
| S25-04 | **Damage write-gate commit** — extend-only `ProposePlatformDamageBatch` + `ApproveBatch`/`RejectBatch` | team-data | 2 | S25-03 | Staging → live upsert; sensor + Phase A/B regression unchanged |
| S25-05 | **Damage importer E2E** — diff/stage/approve damage column edits; empty-diff golden | team-data | 1.5 | S25-04 | Edit `MaxHp` → approve → read-back; unedited round-trip empty diff |

**Sprint fails** if S25-05 damage round-trip does not land.

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S25-06 | **Damage validator rule pack** — HP bounds, withdraw threshold, flag sanity; validation hash golden | team-data | 1 | S25-05 | Blocking findings before staging; hash pinned |
| S25-07 | **S24-11 ClosedXML carryover** — merge `stack/sprint24/closedxml-phase-b-ux`; Emcon enum dropdowns + binary golden | team-data | 1 | S25-01 | `Data.Excel.Tests` PASS; update golden if Platforms columns shift |
| S25-08 | **APP-6 glyph atlas MVP** — expand `App6Sidc`; USS/atlas-backed map glyphs (≥2 distinct icons) | team-unity | 1.5 | S25-01 | Headless App6 + MapPanel tests PASS; spike doc PROCEED |
| S25-09 | **Cesium Editor evidence** — execute S24-08 manual protocol; PNG attachments | team-unity | 1 | S25-01 | `cesium-s25-*.png` in attachments; QA condition cleared |
| S25-10 | **S24-10 EMCON panel merge** — rebase `doctrine-emcon-readonly`; read-only EMCON line on doctrine panel | team-unity | 0.5 | S25-01 | Doctrine batch PASS; ZERO touch `DelegationBridge` |
| S25-11 | **C2 Editor tri-batch sign-off** — `Invoke-C2PlayModeSignoffBatch.ps1` comms/classify/doctrine | team-unity | 0.5 | S25-10 | Log archived; no `SIGNOFF_ERROR` |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S25-12 | **Closeout hygiene** — replay 6/6; GitNexus @ stack tip; tracker row 21 Phase B complete; CI branch cleanup | c-sharp-devops-engineer | 0.5 | S25-05+ | Evidence in `sprint-25-gitnexus-*.md`; stale `stack/sprint24/*` pruned |
| S25-13 | **Damage sim consumer stub** — `TryGetPlatformDamage` readiness/withdraw smoke | c-sharp-engineer | 1.5 | S25-03 | Sim tests PASS; `/replay-verify` if merged |
| S25-14 | **Cesium APP-6 billboards** — globe markers use `App6Sidc` affiliation | team-unity | 1 | S25-08, S25-09 | Editor evidence updated; `useGlobeMap: 0` on DelegationSmoke |

## Carryover from Sprint 24

| Item | S24 status | S25 placement |
|------|------------|---------------|
| Damage-model columns | Explicit defer | **S25-02..05 must-have** |
| S24-10 EMCON panel | Branch `doctrine-emcon-readonly` | **S25-10 should-have** |
| S24-11 ClosedXML UX | Branch `closedxml-phase-b-ux` | **S25-07 should-have** |
| Cesium Editor screenshots | Advisory gap | **S25-09 should-have** |
| S24-07 tri-batch visuals | Advisory gap | **S25-11 should-have** |
| APP-6 glyph atlas | Spike PROCEED; unicode only | **S25-08 should-have** |
| GitNexus @ main post-merge | Retro action | **S25-01 + S25-12** |
| GitHub Actions billing | CodeQL advisory fail | **S25-12 / CI hygiene doc** |

## Explicitly Out of Scope

- **CMO catalog Phase 2 import** (spike-only at most — defer full import to Sprint 26)
- **C5 human override UX** (long-deferred program item)
- **Runtime damage application / BDA** (ADR-009 combat domain)
- **Component-level damage tables** (platform HP + flags only)
- **In-engine platform viewer** (ADR-011 Phase C)
- **ZERO touch violation** on `DelegationBridge.cs`

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S25-14 (Cesium APP-6 billboards) | Damage path + S25-08/09/10 |
| 2 | S25-13 (sim consumer) | Must-have damage loop |
| 3 | S25-11 (Editor tri-batch) | Only if no Unity Editor host |
| 4 | Narrow S25-08 to USS frames only | S25-09 + S25-10 minimum |

**Minimum shippable (beyond must-have):** S25-06 + S25-10 + S25-12.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| `CatalogWriteGate` extend-only violation | Medium | CRITICAL | `gitnexus impact` before edit; full WriteGate regression |
| Damage scope creep into combat runtime | High | High | Catalog columns + Excel only; PR review scope lock |
| Unity Editor unavailable | Medium | Medium | Headless remains merge authority; APPROVED WITH CONDITIONS |
| S24-11 / S24-10 merge conflicts | Medium | Medium | Land early (day 2–3) parallel to damage schema |

## GitNexus Rules (Mandatory)

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `ICatalogReader`, `PlatformWorkbookImporter`
- `/replay-verify` mandatory on S25-13 sim consumer merge

## Parallel Kickoff

See `production/agentic/sprint-25-parallel-kickoff-2026-06-17.md` and track plans:
- `production/agentic/sprint-25-plan-data-2026-06-17.md`
- `production/agentic/sprint-25-plan-unity-2026-06-17.md`
- `production/agentic/sprint-25-plan-qa-2026-06-17.md`

Graphite runbook: `docs/superpowers/plans/sprint-25-graphite-stack.md`

## Producer Decisions Needed

1. **Damage column schema sign-off** — final column set + migration strategy before S25-02 (Day 1).
2. **CMO Phase 2** — spike-only in S25 vs full defer to S26 (reject import creep).
3. **GitHub Actions billing** — remediate, trim workflows, or document permanent advisory status by Day 2.

## Quality Gates

```bash
export PATH="/home/username01/.dotnet:$PATH"

# Day-1 (S25-01)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal

# Data track
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|CatalogPhaseB|CatalogPhaseBDamage" -v minimal

# Unity track
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|Doctrine|MapPanelBinder|App6" -v minimal
```