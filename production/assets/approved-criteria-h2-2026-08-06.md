# H2 — Asset Approved Path (formal criteria)

**Status:** Proposed 2026-08-06 (unblocks Linear **DRG-40** Launch criterion 3)
**Stage:** Release (does not advance Launch)
**Authority:** `production/release-continuity-scope-boundary-2026-07-14.md` (S94 asset wave), `design/assets/asset-manifest.md`

---

## Why this exists

Launch criterion 3 (DRG-40) cannot be assessed while **Approved = 0** and "Approved" is undefined. This document defines **Approved** so production can move Specced / In Production / Done → Approved without inventing store-submission claims.

---

## Status ladder (authoritative)

| Status | Meaning |
|--------|---------|
| **Needed** | Identified gap; no spec |
| **Specced** | Spec exists under `design/assets/specs/` |
| **In Production** | Work in flight (umbrella or child) |
| **Done** | Deliverable on tree under `production/assets/` or Unity path cited in manifest |
| **Approved** | Meets **all** criteria below |

---

## Approved criteria (all required)

1. **Artifact present** — File(s) exist on `main` at the path recorded in the manifest (or Addressables key documented).
2. **Spec satisfied** — Spec acceptance criteria checked off or explicitly waived with reason in the asset row.
3. **Import path** — Unity import / Addressables load path verified **or** marked N/A for pure docs/audio stubs with host unavailable.
4. **No blocking defect** — No open High/Urgent Linear bug linked to this asset ID.
5. **License / provenance** — Source noted (original, CC0, purchased, generated) in manifest or sibling `PROVENANCE.md`.
6. **Human sign-off** — Product owner records `Approved by <name> on <date>` on the manifest row or in a closeout note.

**Not required for Approved:** Steam upload, marketing spend, or Launch stage change.

---

## Process

1. Producer moves asset to **Done** with evidence path.
2. Reviewer applies checklist above.
3. On pass, status → **Approved**; bump dashboard counts.
4. Umbrella (ASSET-001…003) is Approved only when **all children** are Approved.

---

## Relationship to DRG-40

| Blocker (DRG-40) | Disposition |
|------------------|-----------|
| 4 Needed | Spec first → Specced |
| 27 Specced | Produce → Done |
| Umbrella incomplete | Child completion |
| Addressables unresolved | Design spike under H5; Approved may use N/A import path until host exists |
| Editor PNG deferred | Explicit N/A until Unity Editor host |

Criterion 3 progress is **count of Approved / 42**, not Done-only.

---

*DRG-40 enablement — 2026-08-06.*
