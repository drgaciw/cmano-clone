# S99-01 Approved Human-Review Queue Package — 2026-07-16

**Sprint:** S99 · Story **S99-01**  
**Stage:** **Release** (not Launch)  
**Authority:**  
[`s98-01-approved-path-pilot-2026-07-16.md`](s98-01-approved-path-pilot-2026-07-16.md) ·  
[`design/assets/approved-criteria-2026-07-14.md`](../../design/assets/approved-criteria-2026-07-14.md) ·  
[`design/art/art-bible.md`](../../design/art/art-bible.md) ·  
[`sprint-99-approved-review-queue-hygiene.md`](../sprints/sprint-99-approved-review-queue-hygiene.md)

## Purpose

Queue **human art/producer review** for pilot assets that already have S98 A1–A7 scorecards. This package is a **briefing only**.

| Forbidden | Status |
|-----------|--------|
| Invent / claim `asset approved: ASSET-NNN` as already provided | **Not done** |
| Auto-flip `asset-manifest.md` to **Approved** | **Not done** — count remains **0** |
| Launch / store submit language | **Out of scope** |

---

## Umbrella honesty (S99-05)

| ID | Status |
|----|--------|
| ASSET-001 | **In Production** |
| ASSET-002 | **In Production** |
| ASSET-003 | **In Production** |

Do not mark umbrellas Done/Approved from this queue.

---

## Queue entries

### 1. ASSET-006 — Message Log Panel

| Field | Value |
|-------|--------|
| Path | `production/assets/c2/MessageLogPanel.uss` (1350 bytes, on-disk) |
| Manifest status | **Done** (not Approved) |
| Spec | `design/assets/specs/c2-ui-assets.md` |
| S98 pilot scorecard | [`s98-01-approved-path-pilot-2026-07-16.md`](s98-01-approved-path-pilot-2026-07-16.md) § ASSET-006 |
| S98 A1–A6 | PASS (A3 PARTIAL — art-bible full review still required) |
| S98 A7 | **FAIL / pending** — no human phrase recorded |

**Reviewer steps**

1. Open USS + `AegisTokens.uss` import; preview in Unity UI Toolkit if available.  
2. Check A3 colors/type density vs `design/art/art-bible.md` (lean B2).  
3. Confirm A4/A5 selectors match message-log panel intent in c2-ui-assets spec.  
4. If **pass all A1–A7**, record **only as a human** (not agent-invented):

```
asset approved: ASSET-006
```

5. Then update `design/assets/asset-manifest.md` Done → **Approved** for ASSET-006.  
6. If **fail**: leave **Done**; file art revision note — do not invent phrase.

### 2. ASSET-021 — Combat domains hot-tick overlay

| Field | Value |
|-------|--------|
| Path | `production/assets/baltic/CombatDomainsHotTick.uss` (1331 bytes, on-disk) |
| Manifest status | **Done** (not Approved) |
| Spec | `design/assets/specs/baltic-patrol-assets.md` |
| S98 pilot scorecard | [`s98-01-approved-path-pilot-2026-07-16.md`](s98-01-approved-path-pilot-2026-07-16.md) § ASSET-021 |
| S98 A1–A6 | PASS (A3 PARTIAL) |
| S98 A7 | **FAIL / pending** |

**Reviewer steps** — same A1–A7 checklist; human phrase if pass:

```
asset approved: ASSET-021
```

---

## Human phrase template (not claimed)

Use **exactly** when a named art-director or producer completes review:

```
asset approved: ASSET-NNN
```

Examples (templates only — **not recorded as provided**):

```
asset approved: ASSET-006
asset approved: ASSET-021
```

**Queue status:** **AWAITING HUMAN REVIEW** — A7 still pending for both IDs.  
**Approved count after this package:** **0** (unchanged).

---

## A1–A7 quick reference (from approved-criteria)

| # | Criterion |
|---|-----------|
| A1 | File exists at manifest path |
| A2 | Naming conventions |
| A3 | Art bible alignment |
| A4 | Spec fidelity |
| A5 | Quality bar (not empty stub) |
| A6 | No Addressables/store pipeline blockers |
| A7 | Human review phrase `asset approved: ASSET-NNN` |

---
*S99-01 queue package — 2026-07-16. Stage Release. Not Launch. Template phrase only — not a claim of human approval.*
