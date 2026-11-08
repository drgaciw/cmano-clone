# Sprint 94 — Asset Wave 2 + Approved Path

**Dates:** 2026-07-14 → 2026-07-21 (est. 4–6 days)  
**Lead:** art-director / producer (local closeout)  
**Program:** S94–S97 Release Continuity — **S94 only** (Asset wave 2)  
**Authority:** [`docs/reports/roadmap-execution-plan-071426.md`](../../docs/reports/roadmap-execution-plan-071426.md), [`docs/reports/future-sprint-roadmap-07142026.md`](../../docs/reports/future-sprint-roadmap-07142026.md), [`design/assets/asset-manifest.md`](../../design/assets/asset-manifest.md), [`critical-hub-merge-playbook-2026-07-14.md`](../agentic/critical-hub-merge-playbook-2026-07-14.md), AGENTS.md

**Roadmap approval:** User approved 0714 roadmap + execute plan via roadmap protocol **2026-07-14** (“I approve via roadmap protocol, then /sprint-plan new for S94 only”).  
**Plan approval:** S94 sprint package authored under that approval **2026-07-14**.  
**QA plan:** [`production/qa/qa-plan-sprint-94-asset-wave-2-2026-07-14.md`](../qa/qa-plan-sprint-94-asset-wave-2-2026-07-14.md)  
**Parallel kickoff:** [`production/agentic/sprint-94-parallel-kickoff-2026-07-14.md`](../agentic/sprint-94-parallel-kickoff-2026-07-14.md)

> **Predecessor:** S93 COMPLETE (first binary wave + residual 036/037/040/041 Done). Manifest baseline: **0 Needed / 27 Specced / 3 In Production / 12 Done / 0 Approved**. Umbrellas ASSET-001…003 remain **In Production**.

## Sprint Goal

Advance umbrella **children** Specced→**Done** (wave 2: ≥1 C2 child + ≥1 Baltic or store child) with on-disk placeholders under `production/assets/`, publish formal **Approved** criteria (Done→Approved elevation), and close S94 with stage **Release**. **Assets + docs only** — no Addressables bulk, no store upload, no C# hotpath, no Launch. **Does not plan S95–S97.**

## Capacity

| Dimension | Value |
|-----------|-------|
| Total days | 5 |
| Buffer (20%) | 1 day |
| Available | 4 days |
| Parallel tracks | 2 cloud + 1 local closeout |

## Tracks (parallel after baseline)

| Track | Stack prefix | Worktree | Env | Story | Owner |
|-------|--------------|----------|-----|-------|-------|
| C2 children | `stack/sprint94/asset-c2` | `.worktrees/stack/sprint94/asset-c2` | **Cloud** | S94-01 | technical-artist |
| Baltic + store children | `stack/sprint94/asset-baltic-store` | `.worktrees/stack/sprint94/asset-baltic-store` | **Cloud** | S94-02 | art-director |
| Approved criteria + closeout | `stack/sprint94/closeout` | main / local | **Local** | S94-03 | producer |

**Wave order:** Phase 0 baseline → (S94-01 ∥ S94-02) → S94-03 closeout

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S94-01 | C2 child wave — Message Log Panel (ASSET-006) | technical-artist | 1.5 | Phase 0 | File under `production/assets/c2/`; ASSET-006 → **Done**; honest manifest; no C# |
| S94-02 | Baltic/store child wave — Combat domains overlay (ASSET-021) and/or press-kit stub (ASSET-026) | art-director | 1.5 | Phase 0 | ≥1 file under `production/assets/baltic/` or `store/`; target child → **Done**; no store upload |
| S94-03 | Approved criteria + closeout | producer | 1 | S94-01..02 | `design/assets/approved-criteria-2026-07-14.md`; smoke closeout; stage **Release**; sprint-status S94 |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S94-04 | Execute-plan §9 S94 checkboxes flipped in closeout | producer | 0.25 | S94-03 | Closeout cites completed S94-01…03 |

## Carryover from S93 / post-S93

| Item | Prior state | S94 action |
|------|-------------|------------|
| ASSET-001…003 | In Production | Stay In Production; advance children |
| 12 Done | S93 + residual | Wave 2 adds ≥2 Done |
| Approved | 0 | Criteria path only (no auto-Approved without review) |
| Screenshots 027–034 | Specced (Editor deferred) | Out of S94 unless placeholder doc only |

## Hard Gates (S94 — docs/assets; suite if C# touched)

| Gate | Command / check | Pass criterion |
|------|-----------------|---------------|
| Scope | Assets under `production/assets/` + manifest/docs | No Addressables bulk; no store upload; no Launch |
| Stage | `production/stage.txt` | **Release** |
| Bridge | No `DelegationBridge` hotpath | **ZERO** |
| Suite (if C#) | `dotnet test ProjectAegis.sln -v minimal` | **≥1638 / 0 failed** |
| Suite (assets-only) | Cite last gate evidence | `gates-gauntlet-land-post-2026-07-14.log` / closeout note |

## Definition of Done

- [ ] S94-01 and S94-02 Must Have complete (honest Done + on-disk files)
- [ ] Approved criteria published
- [ ] Smoke closeout published
- [ ] sprint-status.yaml S94 section updated
- [ ] Stage remains **Release**
- [ ] S95–S97 **not** in this sprint scope

## Standing Rules

- Stage = **Release** throughout S94.
- GitNexus preflight on CRITICAL hubs only if C# symbols touched (assets/docs expect **low** risk).
- No `DelegationBridge` hotpath; CatalogWriteGate **extend-only**.
- Baltic hash frozen (`17144800277401907079`).
- Placeholder binaries / USS acceptable; quality bar documented in Approved criteria + closeout.

## Explicit non-goals (this sprint)

- S95 gauntlet productization, S96 architecture promote, S97 continuity gate
- Launch stage advance; E7 store submit; Addressables bulk; Unity Editor PNG pack
- Finishing **all** Specced children under 001–003

---
*Created 2026-07-14 from execute plan 071426 + roadmap 07142026. Graphite-first. Superpowers parallel tracks.*
<!-- harness workspace copy -->
