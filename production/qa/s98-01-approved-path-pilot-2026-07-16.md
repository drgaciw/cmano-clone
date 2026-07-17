# S98-01 Approved-Path Pilot Package — 2026-07-16

**Sprint:** S98 · Story **S98-01**  
**Stage:** **Release** (not Launch)  
**Authority:** [`design/assets/approved-criteria-2026-07-14.md`](../../design/assets/approved-criteria-2026-07-14.md) · [`asset-manifest.md`](../../design/assets/asset-manifest.md) · [`sprint-98-release-residual-backlog.md`](../sprints/sprint-98-release-residual-backlog.md)

## Purpose

Human-reviewable **Done → Approved** pilot for **two** Done assets with real on-disk USS content. This package **scores** A1–A7; it does **not** promote status to **Approved**.

**No** `asset approved: ASSET-NNN` phrase is claimed. Manifest **Approved** count remains **0**.

## Umbrella honesty (S98-05)

| ID | Name | Status |
|----|------|--------|
| ASSET-001 | C2 Command Post panel suite | **In Production** |
| ASSET-002 | Baltic patrol theater presentation | **In Production** |
| ASSET-003 | Store capsule + header art pack | **In Production** |

Do not mark umbrellas Done/Approved from this pilot.

---

## Pilot selection

| ID | Name | Path | Manifest status |
|----|------|------|-----------------|
| **ASSET-006** | Message Log Panel | `production/assets/c2/MessageLogPanel.uss` | **Done** |
| **ASSET-021** | Combat domains hot-tick overlay | `production/assets/baltic/CombatDomainsHotTick.uss` | **Done** |

Rationale: non-README wave-2 USS with selectors and S94 headers; structural content suitable for A1–A6 review. Full art pass still required for A7.

---

## A1–A7 scorecard — ASSET-006

| # | Criterion | Result | Evidence |
|---|-----------|--------|----------|
| A1 | File exists | **PASS** | `test -f production/assets/c2/MessageLogPanel.uss` — **1350** bytes |
| A2 | Naming | **PASS** | `MessageLogPanel.uss` under `production/assets/c2/`; ASSET-006 header comment |
| A3 | Art bible alignment | **PARTIAL** | Uses `AegisTokens.uss` / semantic log colors; lean B2 density OK for stub — **full art-director review still required** for Approved |
| A4 | Spec fidelity | **PASS** | Header cites `design/assets/specs/c2-ui-assets.md` ASSET-006; message-log panel + scroll + row structure present |
| A5 | Quality bar | **PASS** (stub bar) | Not empty: selectors (`.message-log-panel`, `__scroll`, `__row`); S94 quality-bar comment; ≥ structural USS |
| A6 | No pipeline blockers | **PASS** | Local USS only; no Addressables bulk / store URL |
| A7 | Human review | **FAIL / pending** | No recorded `asset approved: ASSET-006` — **do not Approved** |

**Pilot verdict ASSET-006:** Eligible for art/producer review queue; **remain Done**. Not Approved.

---

## A1–A7 scorecard — ASSET-021

| # | Criterion | Result | Evidence |
|---|-----------|--------|----------|
| A1 | File exists | **PASS** | `test -f production/assets/baltic/CombatDomainsHotTick.uss` — **1331** bytes |
| A2 | Naming | **PASS** | `CombatDomainsHotTick.uss` under `production/assets/baltic/`; ASSET-021 in header |
| A3 | Art bible alignment | **PARTIAL** | HUD overlay layout; kill-tint note vs AegisTokens; **full art review still required** |
| A4 | Spec fidelity | **PASS** | Header cites `design/assets/specs/baltic-patrol-assets.md` ASSET-021; hot-tick panel structure present |
| A5 | Quality bar | **PASS** (stub bar) | Not empty: selectors (`.combat-domains-hot-tick`, `__title`); S94 placeholder comment |
| A6 | No pipeline blockers | **PASS** | Local USS only |
| A7 | Human review | **FAIL / pending** | No recorded `asset approved: ASSET-021` — **do not Approved** |

**Pilot verdict ASSET-021:** Eligible for art/producer review queue; **remain Done**. Not Approved.

---

## Manifest / Approved policy

| Check | Status |
|-------|--------|
| Auto-flip to Approved | **Forbidden** — not performed |
| Human phrase invented | **No** |
| Approved count after pilot | **0** (unchanged) |
| Review phrase when ready | `asset approved: ASSET-006` / `asset approved: ASSET-021` (human only) |

## Recommendation for reviewers

1. Open USS in Unity UI Toolkit preview (or text review vs art bible).  
2. Confirm A3 colors/type vs `design/art/art-bible.md`.  
3. If pass: record phrase + date in PR/closeout; then update `asset-manifest.md` Done→Approved.  
4. If fail: leave Done; open art revision story — do not close residual by status rewrite alone.

---
*S98-01 pilot package — 2026-07-16. Stage Release. Not Launch. Not an Approved promotion.*
