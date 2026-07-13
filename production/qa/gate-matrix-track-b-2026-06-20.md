# Expanded Gate Matrix — Track B (Release Enablement, S42 start)

**Date:** 2026-06-20  
**Authority / Citation:** [`production/release-enablement-scope-boundary-2026-06-20.md`](../release-enablement-scope-boundary-2026-06-20.md) (published APPROVED 2026-06-20; supersedes polish-scope-boundary for Track B S42+). **Every Track B story MUST cite this boundary doc + committed row ID (B1) or epic (B2–B6).**  
**S42-01 baseline reference:** `production/qa/smoke-sprint-42-baseline-2026-06-20.md` (1226/1226, Replay 6/6, PlayMode 18/18, GitNexus CRITICAL on CatalogWriteGate)  
**S41 closeout packet (user ack unblock):** `production/gate-checks/scope-expansion-decision-2026-06-20-S41-close.md` (S41 PASS + human ack 2026-06-20; integrates S41-01 baseline `smoke-sprint-41-baseline-2026-06-20.md`, S41-04 determinism-audit-2026-06-20.md, S41-05 polish-exit evidence, ADR s41-structural-debt-decision-telemetry-osint.md, gap analysis)  
**S41 artifacts (standing floor):** `production/qa/smoke-sprint-41-closeout-2026-06-20.md` (1226 tests hold), `production/qa/qa-plan-sprint-41-2026-06-20.md`, `production/agentic/s39-s48-worktree-manifest.md` (S42 baseline-qa track), AGENTS.md, `docs/reports/s40-s48-local-cloud-agent-execution-plan-2026-06-20.md`, `production/agentic/sprint-42-parallel-kickoff-2026-06-20.md`, `production/sprints/sprint-42-release-kickoff-content-art-bible-w1.md`  
**Prior baseline (S39 closeout floor):** `production/qa/smoke-sprint-39-closeout-2026-06-20.md` (1215)  
**GitNexus / AGENTS.md discipline:** MUST `impact()` before any symbol edit (esp. B1 Catalog/Platform); `detect_changes()` pre-commit. HIGH/CRITICAL block merge w/o TD review.

> **Track B scope (B1–B6 summary from release-enablement-scope-boundary-2026-06-20.md):** B1 content completeness (13 tracker rows: S42 W1 Req 02/06/12/13/16/21 + S43 W2); B2 art bible partial (§1–4 S42, complete S43); B3 structural debt (S44, GitNexus+replay mandatory); B4 perf (S45, determinism paired); B5 launch artifacts (S46); B6 release gate (S47/48). Explicitly out: Req 01/05/07-11/20 full, globe prod, full corpora CI, multiplayer, hash change w/o golden ADR, DelegationBridge w/o ADR, CatalogWriteGate deviation w/o scope ADR.

## Standing Invariants & Gate Matrix (unchanged carry-forward + S42 baseline update)

From release-enablement-scope-boundary-2026-06-20.md §Standing invariants & gate matrix + cut-lines. **Maintained hard gates. Cite boundary.**

| Gate | Floor / Policy (Track B) | S42 Baseline (2026-06-20) | Notes / Expansion |
|------|---------------------------|---------------------------|-------------------|
| **Headless tests (full sln)** | **≥1215** at S42 start (smoke-sprint-39-closeout-2026-06-20.md; S40/S41 may raise); monotonic growth; **never regress below post-S41 closeout baseline** | **1226/1226** (hold from S40 closeout + S41-01/04/05) | Per-project: Data 403, Sim 279, Delegation 245, UnityAdapter 252, Cli 42, Excel 5. S41 closeout packet confirms no regression. Expand matrix on new UI (S43 Req 03/04). |
| **ReplayGolden** | **6/6** every sprint; S44+ after every B3 merge | **6/6** (~170 ms; A/B + golden match) | Baltic hash **immutable `17144800277401907079`** (golden ADR + Producer + TD sign-off ONLY). No prod hash change w/o ADR. Use `/replay-verify` skill. |
| **C2 proxy (PlayModeSmokeHarnessTests)** | **18/18+** baseline; expand when features land | **18/18** (266 ms) | Filters (from S41 QA + prior): PlayModeSmoke\|C2Selection\|OobTree\|LossesScoring\|BalticReplay\|FuelState\|AttackMenu\|PlatformImport\|Doctrine\|C2TopBar\|PlatformCatalogViewer\|PlatformComms\|PlatformLinkCatalog\|Graph* . **Append `DelegationBadge\|SimulationMode` on S43 Req 03/04.** Expand matrix in qa-plan-sprint-42 + future. |
| **Baltic hash** | **`17144800277401907079`** immutable for v1.0 | Unchanged (confirmed via replay) | Golden ADR + Producer + Technical Director **only**. |
| **DelegationBridge** | **ZERO touch** default — explicit ADR revokes S34 control manifest; required if Req 04 badges touch bridge | ZERO (git + impacts + all S41/S42 manifests) | **ADR mandatory** before any deviation. GitNexus context on bridge symbols before touch. |
| **CatalogWriteGate** (B1 critical) | **Extend-only** default — scope ADR may revoke for specific B1 data rows. Projection-side only. | Extend-only (confirmed); **GitNexus CRITICAL impact** | `impact("CatalogWriteGate", "upstream")`: CRITICAL, 176 impacted (93 d=1 direct callers), 7 processes (RunCatalogImportMarkdown, PlatformImportXlsxCommand, CmoMarkdownImportProposer, OnApproveSelected*, CatalogDiffProposalAgent), modules: Import(44), Platform(37), WriteGate(19), Catalog(14+), Telemetry/Osint/Snapshots/Tests. **IWriteGate also CRITICAL (113 impacted).** MUST `impact()` + boundary cite before S42-03 Catalog/Platform edits. No direct writes. |
| **GitNexus discipline (AGENTS.md)** | `impact()` before edit; `detect_changes()` before commit; HIGH/CRITICAL warnings block merge without TD review | ✅ up-to-date @ c4d6e52; detect_changes (unstaged): low risk (doc-only, 0 process impact) | Repo: cmano-clone (17797 symbols). Run before B1 symbols (Catalog*/Platform* projections, WriteGate). Use MCP `gitnexus__impact`, `gitnexus__detect_changes`, `gitnexus__context`. See AGENTS.md sections. |
| **Determinism** | Seeded only; no wall-clock/unordered iter in sim paths; pure SeededRng; fixed Clock + MixWorldHash; Sort/Comparers vs hash order | PASS (per S41-04 determinism-audit-2026-06-20.md 0 CRIT/HIGH/MED; S42 replay confirms) | Pair with B3 (S44) + B4 (S45). Use `/determinism-audit` skill pre-merge. |
| **Boundary / Scope** | Every Track B artifact cites `release-enablement-scope-boundary-2026-06-20.md` + row/epic ID. No creep into deferred (Req 01/05/07-11/20 etc.). | All S42-01 artifacts cite (this matrix + smoke-42-baseline) | Cut-lines: `impact()` on Catalog/Platform; no DelegationBridge w/o ADR; no hash change w/o golden ADR; B2 partial (§5/7 N/A); B4 isolated-fixture only until det sign-off. |
| **Test monotonic + evidence** | ≥ post-S41 baseline; evidence packs cite boundary | 1226 hold; cross-refs S41 polish-exit + gap | S42-02 qa-plan will expand proxy filters + B1 coverage. |

## Expanded Elements for Track B (B1 wave 1 + B2 + future)

**B1 content (S42 W1 + S43 W2):** 13 rows committed to MVP-done/Partial+ at S43 closeout. Catalog/Platform edits: mandatory GitNexus impact (CRITICAL on WriteGate) + projection-side only (no runtime mutation w/o ADR). Scenario: policy JSON maint only; replay-gated; no hash change. **Cites:** release-enablement-scope-boundary-2026-06-20.md + scope-expansion-decision-2026-06-20-S41-close.md (S41 PASS, user "i provide the ack", S42 UNBLOCKED per S42-04 delivery). See production/determinism/replay-s42-04-*.md + WT stack/sprint42/content-scenario . 6/6 maintained.

**B2 Art bible:** §1–4 (S42) refinement + AegisTokens.uss + gate matrix cross-ref; full §5-9 S43. Cite boundary in art-bible.md updates.

**B3+ gates (preview):** Replay 6/6 post every B3 merge (S44); perf benchmarks (S45); full gate-check at S48 (user + TD verdict).

**Proxy matrix expansion trigger:** On landing Req 03 (Simulation Modes UI), Req 04 (Delegation badges): append filters `DelegationBadge|SimulationMode`; update C2 proxy to 19/19+ or 20/20 in qa-plan + smokes. See release-boundary §C2 proxy filters.

**Headless floor evolution:** S42 start 1226; record raises in future baseline/closeout smokes (cite this matrix + boundary). Never below post-S41.

**GitNexus B1 clusters (from impacts + boundary):** Catalog cluster (WriteGate, readers, importers, triage, snapshots), Platform cluster (projections, workbook, loadout surfacing, catalog filters). Single local lead for Catalog per worktree-manifest + execution-plan. `impact()` + `detect_changes()` on every edit.

## Verification & Trace (S42-01 + verification-before-completion)

- **Commands (from smoke-42-baseline):** dotnet restore/build/test (1226), Replay 6/6, PlayMode 18/18, GitNexus status/impact/detect (CRITICAL CatalogWriteGate logged).
- **S41 baseline comparison:** Identical counts/gates + hash; S41 closeout packet + user ack unblocks S42.
- **Cites:** release-enablement-scope-boundary-2026-06-20.md (everywhere in this doc + smoke); S41-01/04/05/06/07/08 artifacts; worktree manifest S42 baseline-qa; AGENTS.md; sprint plans/kickoffs.
- **Hard gates:** All maintained (no violations).
- **Next:** S42-02 produces qa-plan-sprint-42-*.md (B1/B2 scope; expands this matrix; blocks S42-03+). S42-06 closeout updates with evidence + sprint-status.

**AC for S42-01 (gate matrix production):** [x] Produced/updated with cites; [x] standing invariants + expanded for B1 CRITICAL symbols + proxy triggers; [x] S41 artifacts + boundary cross-ref; [x] GitNexus B1 impacts; hard gates held.

---

**Track B gate matrix baseline established.** All future S42–S48 artifacts (stories, code, docs, qa, closeouts) **MUST cite `production/release-enablement-scope-boundary-2026-06-20.md`** and link to this matrix or successor. Maintain invariants. Use c-sharp-devops + csharpexpert + GitNexus discipline.

*Generated by S42-01 (c-sharp-devops-engineer). Parallel-safe. verification-before-completion applied (first reads of all manifests + boundary + S41 packet + cmds + re-checks + GitNexus).*
