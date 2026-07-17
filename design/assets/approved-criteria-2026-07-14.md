# Asset Approved Criteria — Project Aegis (2026-07-14)

**Authority:** S94 Asset Wave 2 · [`roadmap-execution-plan-071426.md`](../../docs/reports/roadmap-execution-plan-071426.md) · [`asset-manifest.md`](asset-manifest.md)  
**Stage:** **Release** — this document does **not** authorize Launch or store submission.

## Purpose

Define the elevation path from **Done** → **Approved** so wave-2 placeholders and later art can graduate under a single bar. Until a row meets every criterion below, keep status **Done** (or Specced/In Production) — never mark **Approved** by default.

## Status ladder (recap)

| Status | Meaning |
|--------|---------|
| Needed | Required but no usable spec |
| Specced | Spec file exists under `design/assets/specs/` |
| In Production | Umbrella or multi-child work actively shipping children |
| Done | On-disk artifact under `production/assets/` matching the manifest path |
| Approved | Human art/producer review passed the criteria below |

## Done → Approved criteria (all must pass)

| # | Criterion | Check |
|---|-----------|--------|
| A1 | **File exists** | Manifest path resolves; `test -f` succeeds for primary binary/USS/MD |
| A2 | **Naming** | Follows asset-id / project naming conventions for category (UI USS, PNG dimensions if marketing, etc.) |
| A3 | **Art bible alignment** | Colors/type density match `design/art/art-bible.md` (lean B2) for UI; marketing matches store-capsule spec notes |
| A4 | **Spec fidelity** | Implements the child description in the governing specs file (not a random unrelated stub) |
| A5 | **Quality bar** | Not solely a “TODO empty” file: stub USS must include selectors; PNG must open and meet min resolution if marketing; MD overlays must describe layout |
| A6 | **No pipeline blockers** | No Addressables bulk import required for Approval; no store-live URL dependency |
| A7 | **Human review** | Named reviewer (art-director or producer) records ack in closeout or PR with phrase `asset approved: ASSET-NNN` |

## Placeholder policy (S94 wave 2)

- Placeholders **may** be marked **Done** when A1+A5 (minimal content) hold.
- Placeholders **must not** be marked **Approved** until A1–A7 all hold after art pass.
- Quality bar for S94 stubs: “structurally valid USS/PNG/MD with S94 header comment; not blank.”

## Out of scope for this criteria doc

- Launch stage advance  
- E7 commercial store submit  
- Unity Editor PNG capture pack (ASSET-027–034 still deferred without Editor host)  
- Auto-approval by CI alone  

## Revision

| Date | Change |
|------|--------|
| 2026-07-14 | Initial criteria for S94 Release Continuity wave 2 |

---
*Approved criteria — does not itself approve any asset.*
