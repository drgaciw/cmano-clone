# Architecture CONCERNS Gate — 2026-07-24

**Closes:** Launch exit criterion 4 (*"Architecture CONCERNS cleared"*)
**Linear:** DRG-41 (milestone H3 — Launch / Commercial Execution)
**Parent gate:** [`commercial-launch-execution-gate-2026-07-24.md`](commercial-launch-execution-gate-2026-07-24.md) §5
**Scope:** documentation only — no source files changed
**Stage:** **Release** — unchanged by this gate
**Amended:** 2026-08-06 (DRG-42) — §Where-they-are table and §8 residual risk updated to reflect the two restored artifacts. Original findings preserved; no verdict changed.

---

## 1. Verdict

**Arch-docs layer: CONCERNS → PASS (with one formally deferred item).**

Criterion 4 is **MET** — owner sign-off received 2026-07-24 (§9).

This does **not** clear Launch. Criteria 2 (store package) and 3 (assets, 34/42 outstanding) remain unmet.

---

## 2. Root cause: a cohort of governance artifacts is stranded on an unmerged PR

`architecture.md` and four other documents cite `architecture-review-post-s93-2026-07-14.md` as the **authority** for the Launch-blocking CONCERNS verdict. That file is **not present on `main` or in any working tree**.

It is **not lost**. It exists on the unmerged draft branch **`stack/post-editor/s93-asset-production`** (commit `805070e`, 2026-07-14) — the branch behind **draft PR #324**, open since 2026-07-19 and tracked in Linear as **DRG-35**.

### The full stranded cohort (as of 2026-07-24)

Every artifact dated **2026-07-14** that is cited but unresolvable traces to this same unmerged branch. Every **2026-07-15** artifact resolves fine.

| Artifact | Where it was on 2026-07-24 |
|---|---|
| `architecture-review-post-s93-2026-07-14.md` | PR #324 @ `805070e` |
| `post-s93-concerns-remediation-closeout-2026-07-14.md` | PR #324 @ `805070e` |
| `critical-hub-merge-playbook-2026-07-14.md` | PR #324 @ `d53da70` (also present on `main` under `production/agentic/`) |
| `gauntlet-stack-land-plan-2026-07-14.md` | PR #324 @ `d53da70` |
| `release-continuity-scope-boundary-2026-07-14.md` | Never committed anywhere — **restored 2026-08-06 under DRG-42** at `production/release-continuity-scope-boundary-2026-07-14.md` |
| `post-s93-track-a-closeout-2026-07-14.md` | Never committed anywhere — **restored 2026-08-06 under DRG-42** at `production/gate-checks/post-s93-track-a-closeout-2026-07-14.md` |

> **Post-gate resolution (DRG-42, 2026-08-06):** The two never-committed artifacts were restored on branch `docs/drg-42-governance-artifacts` (PR #405) as rewritten reconstructions from surviving evidence. See §10.

### Why this matters beyond criterion 4

These are not incidental files:

- **`post-s93-concerns-remediation-closeout`** is named by `future-sprint-roadmap-07142026.md` as a **primary authority for the S94+ program**.
- **`critical-hub-merge-playbook`** is the stated **enforcement mechanism** for the CRITICAL hub constraints (`ScenarioDocumentEditor` 233, `CatalogWriteGate` 186, `DelegationBridge` 142). It is cited by 8 documents including three sprint plans. The *constraints* are independently documented in `architecture.md` and the re-matrix, so the rules survive — but the playbook that operationalizes them does not resolve on `main`.
- **`release-continuity-scope-boundary-2026-07-14.md`** is a **scope boundary** — this repo's core governance instrument — and was never committed at all. *(DRG-42, 2026-08-06: rewritten from surviving evidence and committed; it now bounds S94–S97 as intended.)*

A verdict whose authority cannot be resolved from `main` is unauditable by anyone working from `main`. That is the root cause of criterion 4 sitting open, and it is a **process defect, not a documentation defect**: work was declared complete while its artifacts stayed on an unmerged draft.

### Resolution (2026-07-24 plan)

1. **Immediate:** `architecture-re-matrix-post-s93-s96-2026-07-15.md` is the operative authority on `main`. Its layer verdicts were checked against the stranded review (§3) and **match**.
2. **Proper fix:** merge **PR #324 / DRG-35**, which restores four of the six artifacts.
3. **Remaining:** the two never-committed artifacts must be rewritten or their citations retired.

Dated snapshots and historical plans were deliberately **not** modified at gate time — rewriting them would falsify the record of what was believed at the time.

---

## 3. CONCERNS enumerated — from the actual review, recovered

The stranded review was extracted from `805070e` and read directly. Its verdict table **matches** the re-matrix on every layer, which is what licenses using the re-matrix as the operative stand-in.

**Review verdict:** CONCERNS — *"Release hold **cleared**; Launch **not cleared**"*. Its arch-docs CONCERNS note reads: *"Master `architecture.md` still Draft; full ADR re-audit not re-run end-to-end."*

Its GDD/ADR coverage notes add:
- MVP systems ~**12/20 GDD-linked** (systems-index not fully refreshed)
- Editor ADRs 013–017 **accepted** for headless
- Gauntlet oracle schema needs **no new ADR** unless productized as a normative sim contract
- Traceability matrix not re-generated — residual CONCERNS **for Launch packaging only**

Its "still blocking Launch only" list is exactly the four Launch criteria — item 1 (human ack + commercial gate) was **satisfied 2026-07-24**, and item 4 is marked **"Optional"**.

Per the layer verdict table, **only one layer is CONCERNS**:

| Layer | Verdict |
|---|---|
| Sim (core / determinism) | PASS |
| Editors (SE / ME P2 / PE) | PASS |
| Catalog (write gate / import) | PASS w/ constraint — extend-only |
| Bridge (Delegation adapter) | PASS w/ constraint — ZERO hotpath |
| Gauntlet (oracle QA) | PASS |
| **Arch docs (master + ADR freshness)** | **CONCERNS** |
| Launch (commercial surface) | FAIL / deferred — separate criteria |

**No code-architecture layer is in CONCERNS.** Criterion 4 was entirely a documentation-currency issue.

The arch-docs CONCERNS decomposes into three items:

| # | Item | Disposition |
|---|---|---|
| 4a | Master doc freshness | **Resolved** — §4 |
| 4b | ADR freshness / completeness | **Resolved** — §5 |
| 4c | Full GDD→ADR re-matrix | **Formally deferred** — §6 |

---

## 4. Item 4a — master doc freshness (RESOLVED)

`architecture.md` header verified against current standing floors:

| Field | Value | Status |
|---|---|---|
| Suite floor | ≥1638/0f | Current |
| Baltic hash | `17144800277401907079` | Current |
| Stage | Release, Launch not advanced | Current |
| CRITICAL hub map | 5 symbols with upstream counts | Current |
| Architecture authority | *was dangling* | **Fixed this gate** |

Additionally fixed: `architecture-traceability-index.md` carried a stale gate floor of **≥1232** against a standing floor of **≥1638**, and was last updated 2026-07-08. Header refreshed; C2 proxy floor (≥20/20) added.

---

## 5. Item 4b — ADR freshness (RESOLVED)

Inventory verified by directory listing, 2026-07-24:

- **17 ADRs present:** ADR-001 … ADR-011, ADR-013 … ADR-017, plus `adr-simulation-session-frozen-hub-spirit1`
- **ADR-012: absent.** Confirmed a **numbering gap, not a missing decision** — consistent with the re-matrix's own finding ("Number gap only — not a Launch blocker by itself"). Now recorded explicitly in the traceability index so future readers stop re-investigating it.
- Editor ADRs 013–017 cover import policy, Lua scope, agent-authored transparency, event-graph caps, and editor topology.
- Gauntlet oracle schema is treated as a QA-gate extension of existing determinism policy; a normative ADR is optional and only required if it is productized as a sim contract.

---

## 6. Item 4c — full GDD→ADR re-matrix (FORMALLY DEFERRED)

**Not done. Deferred with rationale.**

`architecture-traceability-index.md` maps 47 requirements across TR-ID → GDD → System → ADR coverage. Its per-requirement statuses (15 Covered / 20 Partial / 12 Gaps) date from **2026-07-08** and predate sprints S94–S107. Re-assessing all 47 rows requires per-requirement verification against current code and is materially larger than the rest of this gate.

**Grounds for deferral:**

1. The operative authority **already classifies this item as optional** — re-matrix "Gaps remaining" item 4 reads *"**Optional** full ADR / GDD re-matrix and systems-index refresh"*, and the re-matrix states the full re-matrix is *"not required to hold Release."*
2. It is a **documentation-completeness** item. No code-architecture layer is in CONCERNS, so deferral carries no engineering risk to the Release invariants.
3. The staleness is now **explicitly labelled in the index itself** — the percentages are marked not-current with a do-not-cite warning, so the deferral cannot be mistaken for freshness.

**Deferral does not mean deletion.** The work remains tracked. Re-open it if any of these become true:
- A requirement-coverage claim is needed for store or press material
- An ADR is written that changes a layer verdict
- Launch packaging requires a formal traceability attestation

---

## 7. Changes made by this gate

| File | Change |
|---|---|
| `docs/architecture/architecture.md` | Authority re-pointed to re-matrix; missing-doc annotated; arch-docs remediation row added |
| `docs/architecture/architecture-re-matrix-post-s93-s96-2026-07-15.md` | Declared operative authority; Gaps items 1 and 4 updated to current disposition |
| `docs/architecture/architecture-traceability-index.md` | Gate floors ≥1232 → ≥1638 + C2 proxy; ADR inventory recorded; staleness of the 47 rows explicitly flagged |

No source files, tests, or scenario data touched. Standing invariants unaffected.

---

## 8. Residual risk (2026-07-24)

- **PR #324 remains unmerged.** Until it lands, four cited governance artifacts — including the S94+ primary authority and the CRITICAL-hub merge playbook — do not resolve from `main`. Tracked as **DRG-35**. This is the highest-value follow-up from this gate.
- ~~**Two artifacts were never committed** (`release-continuity-scope-boundary-2026-07-14.md`, `post-s93-track-a-closeout-2026-07-14.md`). A scope boundary that does not exist cannot bound anything; either write them or retire the citations.~~ **Closed 2026-08-06 (DRG-42)** — both rewritten from surviving evidence and committed. The Track A closeout carries one honest open gap: the dedicated evidence logs (`production/qa/evidence/gitnexus-gauntlet-land-pre-2026-07-14.log`, `…/gates-gauntlet-land-post-2026-07-14.log`) were never committed and are not reconstructable; the parent closeout remains the authoritative narrative. See also §10.
- **The 47 requirement-coverage rows are stale** and now marked as such. Anyone citing coverage percentages must re-assess first.
- **Systems-index is ~12/20 GDD-linked** per the recovered review — folded into the deferred item 4c.
- **GitNexus remains non-functional** (MCP connection closed; CLI Node version mismatch; index stale at `c2b1611`). Not blocking for this docs-only gate, but blocking for any Launch-program code work.

### Process finding

The stranding was invisible because **completion was recorded in documents that cite the artifacts, rather than verified against the artifacts existing on `main`**. The same class of gap produced the story-file drift at S36 and the roadmap staleness. A closeout step that resolves every link it cites would have caught all three.

---

## 9. Sign-off

| Field | Value |
|---|---|
| **Criterion 4 verdict** | **PASS recommended** (CONCERNS resolved; 4c formally deferred) |
| **Launch cleared?** | **No** — criteria 2 and 3 outstanding |
| **Stage change?** | **None** — remains Release |
| **Owner sign-off** | ☑ **RECEIVED 2026-07-24** — user: *"i sign off on all these decisions"* |

Per repo gate protocol, the verdict change takes effect on human acknowledgement.

---

## 10. Post-gate resolution — DRG-42 (2026-08-06)

Linear **DRG-42** restored the two never-committed 2026-07-14 artifacts as reconstructed files on `main` via PR **#405**:

| Artifact | Path on tip |
|---|---|
| Release Continuity scope boundary | `production/release-continuity-scope-boundary-2026-07-14.md` |
| Track A (gauntlet land) closeout | `production/gate-checks/post-s93-track-a-closeout-2026-07-14.md` |

Content was rewritten from surviving evidence (`post-s93-concerns-remediation-closeout`, roadmap 0714, land narratives). Dedicated binary evidence logs remain an **open honesty gap** (called out in the Track A closeout); the parent closeout is the authoritative narrative for measured suite results.

**Audit invariant:** this gate document must not claim those two paths are still "never committed" after #405 lands. Historical §2/§8 wording is preserved with *as-of* qualifiers; this section is the current disposition.

---

*DRG-41 remediation, 2026-07-24. Docs only; stage remains Release.*
*DRG-42 audit-loop closeout addendum, 2026-08-06.*
