# Sprint 91 — Asset Spec Production

**Dates:** 2026-07-15 → 2026-07-21 (est. 4–6 days)  
**Lead:** art-director / producer (local closeout)  
**Program:** S89–S92 Post-Editor Engineering Hygiene + Asset Spec Production — sprint 3 of 4  
**Authority:** [`future-sprint-roadpmap-07092026.md`](../../docs/reports/future-sprint-roadpmap-07092026.md) §3/§6, [`roadmap-execute-plan-07092026.md`](../../docs/reports/roadmap-execute-plan-07092026.md) §3/§4 (S91), [`post-editor-hygiene-scope-boundary-2026-07-09.md`](../post-editor-hygiene-scope-boundary-2026-07-09.md), [`design/assets/asset-manifest.md`](../../design/assets/asset-manifest.md), [`design/art/art-bible.md`](../../design/art/art-bible.md), AGENTS.md, this file.

**Roadmap approval:** User approved S89–S92 roadmap **2026-07-09**.  
**Plan approval:** User approved write of this plan **2026-07-09**.  
**QA plan:** [`production/qa/qa-plan-sprint-91-asset-spec-production-2026-07-09.md`](../qa/qa-plan-sprint-91-asset-spec-production-2026-07-09.md) — published 2026-07-09.

> **Dispatch gate:** Prefer S90 complete before track dispatch (program serial order). Plan may exist while S90 is in flight; do not start S91-01/S91-02 stacks until S90 closeout or explicit user waive.

## Sprint Goal

Refine ASSET-001…003 from Phase B stubs into production-ready specs under `design/assets/specs/` (C2 UI, Baltic patrol theater, store capsule), update the asset manifest, and close S91 with standing gates held. **Specs only** — no Addressables bulk import, no store upload, no Unity binary asset pipeline. Stage stays **Release**.

## Capacity

| Dimension | Value |
|-----------|-------|
| Total days | 6 |
| Buffer (20%) | 1 day |
| Available | 5 days |
| Parallel tracks | 3 (2 cloud + 1 local closeout) |

## Tracks (parallel after baseline)

| Track | Stack prefix | Worktree | Env | Story | Owner |
|-------|--------------|----------|-----|-------|-------|
| C2 + Baltic specs | `stack/sprint91/asset-c2-baltic` | `.worktrees/stack/sprint91/asset-c2-baltic` | **Cloud** | S91-01 | art-director / technical-artist |
| Store capsule spec | `stack/sprint91/asset-store` | `.worktrees/stack/sprint91/asset-store` | **Cloud** | S91-02 | art-director |
| Closeout | `stack/sprint91/closeout` | `.worktrees/stack/sprint91/closeout` | **Local** | S91-03 | producer |

**Wave order:** Phase 0 baseline (day 1) → (S91-01 ∥ S91-02) → S91-03 closeout

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S91-01 | Refine C2 + Baltic specs — add ASSET-001 umbrella; expand `c2-ui-assets.md` + `baltic-patrol-assets.md` to production-ready fields | art-director / technical-artist | 2.5 | Phase 0; prefer S90 complete | ASSET-001 present in C2 spec; ASSET-002 + child stubs (004…022 / 018…022 / 039 as in files) have dims, format, naming, art-bible anchors, status per `/asset-spec`; no Unity/Addressables code; cites art bible + post-editor boundary |
| S91-02 | Refine store capsule pack — expand `store-capsule-assets.md` (ASSET-003 + 023…035) | art-director | 2 | Phase 0; prefer S90 complete | Capsules/logos/screenshot capture specs production-ready; generation prompts where applicable; **no store upload**; cites S70 `asset-checklist.md` + commercial-launch boundary (E7 prep only) |
| S91-03 | S91 closeout — manifest bump, gates, smoke, sprint-status, gt submit | producer | 1.5 | S91-01, S91-02 | `asset-manifest.md` ASSET-001…003 status → Specced (or In Progress); `smoke-sprint-91-closeout-2026-07-*.md`; gates ≥1599/0f, 6/6, 20/20, hash, ZERO bridge; sprint-status updated; tracks merged via `gt` |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S91-04 | Manifest progress summary + cross-links to S91 closeout | producer | 0.5 | S91-03 | Progress table counts reflect Specced priority stubs; optional dashboard/status-truth link |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S91-05 | Art bible §8 cross-ref to refined ASSET-001…003 paths | art-director | 0.25 | S91-01, S91-02 | One-line pointers in `design/art/art-bible.md` — no new art sections |

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|--------------|
| ASSET-001…003 Phase B stubs (2026-06-25) | Stubs only; ASSET-001 missing from `c2-ui-assets.md` | Owned by S91-01 / S91-02 |
| S90 agent/skill P0 sync | Program serial predecessor; plan may not be published at S91 plan time | **Block dispatch** until S90 complete or user waives |
| S89-04 / S89-05 backlog | Optional hygiene leftovers | Leave on S89; do not pull into S91 |

## Hard Gates (S91)

| Gate | Command / check | Pass criterion |
|------|-----------------|---------------|
| Build | `dotnet build ProjectAegis.sln` | 0 errors |
| Tests | `dotnet test ProjectAegis.sln -v minimal` | **≥1599 / 0 failed** |
| Replay | `--filter ReplayGoldenSuiteTests` | **6/6** |
| C2 | `--filter PlayModeSmokeHarnessTests` | **≥20/20** |
| Hash | `rg -l '17144800277401907079' tests/ data/` | 18 paths |
| Bridge | No `DelegationBridge.cs` hotpath edits | **ZERO** |
| Catalog | `CatalogWriteGate` | **extend-only** |
| Scope | Docs under `design/assets/` (+ optional art-bible cross-ref) | No Addressables bulk import; no store upload; no Baltic reopen |

**GitNexus watchlist (pre-edit if any code):** ScenarioDocumentEditor **233**, CatalogWriteGate **186**, DelegationBridge **145**, PatrolCandidateEngagePolicy **113**, BalticReplayHarness **54** — all CRITICAL. Docs-only stacks expect **low** `detect_changes` risk.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Specs drift into Addressables / Unity asset import | Medium | High | Boundary: specs only; no `unity/` binary imports; closeout greps for Addressables churn |
| Store capsule work mistaken for E7 launch execution | Medium | Medium | Explicit “no store upload”; cite commercial-launch boundary |
| S90 incomplete when S91 starts | High (at plan time) | Low for docs | Plan now; **do not dispatch** until S90 closeout or user waives serial order |
| Over-scoping ASSET-004…042 full production | Medium | Medium | Must-have = refine priority umbrellas + existing stub files; full pipeline deferred |

## Dependencies on External Factors

- S90 complete (or explicit user waive of serial order) before track dispatch
- Art bible lean B2 (`design/art/art-bible.md`) — already APPROVED
- Existing stubs: `design/assets/specs/{c2-ui,baltic-patrol,store-capsule}-assets.md`
- Graphite `gt` stack workflow for parallel tracks
- No Unity Editor required (docs + headless gates only)

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [x] QA plan exists — [`production/qa/qa-plan-sprint-91-asset-spec-production-2026-07-09.md`](../qa/qa-plan-sprint-91-asset-spec-production-2026-07-09.md)
- [ ] Standing gates RUN+READ at closeout (≥1599/0f, 6/6, 20/20, hash, ZERO bridge)
- [ ] Smoke closeout doc published
- [ ] sprint-status.yaml updated (S91 COMPLETE block)
- [ ] No S1 or S2 bugs introduced
- [ ] Stage remains **Release**
- [ ] Ready for S92 hygiene gate

## Standing Rules (from boundary)

- Stage = **Release** throughout S89–S92.
- GitNexus impact preflight on §5 CRITICALs before any symbol edit.
- No edits to `DelegationBridge` hotpath; extend-only on `CatalogWriteGate`.
- Baltic corpora + hash frozen (`17144800277401907079`).
- Specs-first in S91; no Addressables bulk import; no store submission.

## Next

Prefer `/sprint-plan` for **S90** (if not yet published) → S90 closeout → `/qa-plan sprint` for S91 → S91-01 ∥ S91-02 dispatch → S91-03 closeout → S92 hygiene gate.

**Scope check:** If stories expand beyond ASSET-001…003 / existing stub files, run `/scope-check` against post-editor hygiene epic before implementation.

---
*Created via `/sprint-plan` for S91 after S89–S92 roadmap approval (2026-07-09). Review mode: lean (PR-SPRINT skipped). Graphite-first. Superpowers.*
