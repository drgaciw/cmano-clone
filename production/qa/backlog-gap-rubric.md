# Project Aegis — Backlog Requirements Gap Analysis Rubric (`v1`)

- **Version:** `v1`
- **Ratified:** `2026-11-22`
- **Primary Jev Model:** `typesafe/jev-1.14` (`typesafe/jev-1.14-20261014` via `https://openrouter.ai/api/alpha/decisions`)
- **Fallback Jev Model:** `typesafe/jev-1.13` (via `https://openrouter.ai/api/v1/systemone`)
- **M1 Decision Provenance ID:** `gen-dec-1795388377-OviBPfiHh1PQl2eUKKlv` (`TypeSafe`, `3,224` input tokens, `512` output tokens, cost `$0.000135408`, `fallback_used: false`)
- **Raw Provenance File:** `production/qa/evidence/backlog-gap-2026-11-22/scope-decisions-jev.json`
- **Pinned Question Pack:** `data/jev/backlog-gap-v1.questions.json`
- **Evaluator Tool:** `tools/jev/backlog_gap_eval.py`
- **Governing Authorities:**
  - Notion: `📐 Project Aegis Rubrics (DRAFT v1)`, `🧹 Backlog rubric pass 2026-10-29`, `Project Aegis (Notion Hub)`, `🗄️ Frozen Git mirror`, `🧭 Routing table v1.0 (APPROVED)`
  - Git: `production/agentic/linear-usage-contract.md`, `production/agentic/drg-47-phase-n-scoping-decision-2026-08-09.md`, `production/qa/uat-dod-rubric.md`, `docs/superpowers/specs/2026-09-30-requirements-planning-reconciliation.md`

---

## 1. Jev-Ratified Open-Scope Decisions (Live M1 Provenance)

All 10 open-scope and completeness calibration questions were decided in a single batched `typesafe/jev-1.14` call (`gen-dec-1795388377-OviBPfiHh1PQl2eUKKlv`). Per `Routing table v1.0 §13`, any ruling with `confidence < 0.80` (or `noul < 0.80`) would be marked provisional and `< 0.60` / `other` escalated to the owner (`drg`); **all 10 live rulings exceeded `0.93`** (`0` provisional, `0` owner escalations).

| # | Question Key | Type | Live Jev Ruling | Probability / Score | Confidence | Status | Operational Rule |
|---|---|---|---|---|---|---|---|
| 1 | `notion_target_surface` | `choice` | **`tasks_db_plus_wiki_drafts`** | `0.99` (vs `tasks_db_only` `0.01`) | `0.99` | Ratified | Evaluate & remediate actionable requirement gaps in Notion `Tasks` DB (`Project = Project Aegis`, page icon, `Status`, `Note` with Linear URL + source path + `req_id`s) and keep `cmano-clone/Requirements drafts` wiki current for narrative drafts. |
| 2 | `notion_frozen_mirror_respect` | `noul` | **`true`** | `noul = 0.94` | `—` | Ratified | `🗄️ Frozen Git mirror` and its inline `📌 Requirements` DB remain **strictly read-only** (never mutated in M3) per its explicit *"Not a tracker. Do not edit rows here"* banner. |
| 3 | `extraction_granularity` | `choice` | **`req_id_clause_cluster`** | `1.00` | `1.00` | Ratified | Extract per addressable requirement ID or cohesive capability-clause cluster (`KCX-01`, `ENV-01`, `AME-6.11`, `RPL-29..31`, `SYM-MIL-01`, `P0-1`, or section capability cluster where IDs are absent) yielding one testable single-outcome backlog unit per entry. |
| 4 | `shipped_req_linear_rule` | `choice` | **`complete_if_code_verified_no_backfill`** | `0.99` (vs `other` `0.01`) | `0.99` | Ratified | Requirements verified as shipped/implemented on `main` (with cited code/test/gate evidence or a `Done` `DRG-*` issue) classify as `complete` (`sub_status: shipped_verified`) in Linear without creating ~350 redundant historical `Backlog` tickets (`linear-usage-contract.md §8`). Flag only unshipped or partial residuals as `incomplete`/`missing`. |
| 5 | `phase_n_deferred_rule` | `choice` | **`complete_when_tracked_deferred_backlog`** | `0.99` (vs `always_incomplete` `0.01`) | `0.99` | Ratified | Deferred Phase N items (`10b`, `SWARM-27..30`, `REQ-09/10` matrix, `ADR-023..026` parked to `2027-01-15`) classify as `complete` (`sub_status: deferred_phase_n`) when explicitly tracked in Linear `Backlog` and active Notion target with `[Phase N]`/`Deferred` owner-gate markers and NO `Surface:` line (`DRG-112..117`). |
| 6 | `draft_docs_inclusion` | `choice` | **`in_scope_backlog_preserve_draft`** | `1.00` | `1.00` | Ratified | Draft/Proposed docs (`23–27`, `SYM-MIL-01`/`SYM-CIV-01`, `CMD-38/39`, `2026-09-30` clauses, `Milsim Comparative Research` `ENV..OPS`) are in scope; require Linear `Backlog` (no `Surface:`) and active Notion tracking explicitly labeled `Draft` / `owner-acceptance-pending` (never promoted to `Approved` or `Done`). |
| 7 | `backlog_surface_line_rule` | `noul` | **`true`** | `noul = 0.93` | `—` | Ratified | Backlog intake omits dispatch-only gate `G2` (`Surface:` stamp); agents must **never** stamp `Surface:` on `Backlog` issues (`Project Aegis Rubrics §2(b)-(c)`). |
| 8 | `completeness_gate_model` | `choice` | **`hybrid_prechecks_plus_jev_thresholds`** | `1.00` | `1.00` | Ratified | Script-first two-stage gate: deterministic mechanical pre-checks run first (`P_*`), followed by Jev System One (`noul >= 0.80`, `choice confidence >= 0.50`, `score >= 1.50` aligned to `S1–S6 >= 9/12`). Jev **never** overrides a failed mechanical pre-check. |
| 9 | `low_confidence_escalation` | `noul` | **`true`** | `noul = 0.93` | `—` | Ratified | When Jev returns `choice.confidence < 0.50` or `choice == "other"`, set `needs_human_review = true` and conservatively classify the side as `incomplete` (never auto-`complete`). |
| 10 | `backlog_rigor_level` | `score` | **`1` (`balanced_intake_s1_s6_no_surface`)** | `score = 1.01` (`p0=0`, `p1=0.99`, `p2=0.01`) | `0.98` | Ratified | Require valid source path, `req_id`, testable AC or deferral note, lifecycle honesty, bidirectional Linear↔Notion links, and `S1–S6 >= 9/12` equivalent without dispatch-only `Surface:`/`Play Mode` gates. |

---

## 2. Project Isolation & Hard Invariants (Mandatory Exclusions)

### 2.1 In-Scope Corpus & Trackers
- **Linear Team:** `DRG` (`463fb01d-3daf-46c8-abbb-f5270282d76c`)
  - Projects: `cmano-clone` (`P-DRG-1`), `Combat Interaction UX` (`P-DRG-5`), `Aegis Horizons H4–H6` (`P-DRG-7`), completed Aegis projects (`P-DRG-2`, `P-DRG-3`, `P-DRG-4`), and unassigned Aegis epic `DRG-377` (`[MILSIM-R2]`) + children (`DRG-378..401`).
- **Notion Surfaces:**
  - Active tracker: `Tasks` DB (`a722ffc6704848d992db8207b0ef5cae`, `collection://d07263cd-13fa-4fc7-9e57-1a99151e2521`, queried via **`mode: "view"`** `view://c5b106a3-eb07-4138-8c72-09200f2cecba`) filtered strictly to `Project = Project Aegis` (`3d5f7cb4-e4df-8196-bbd0-fcc18493713a`).
  - Active narrative/draft wiki: `📘 cmano-clone` (`3a7f7cb4-e4df-811d-814f-c31384674e46`, including `📝 Requirements drafts` `3bef7cb4-e4df-81b7-b0e3-d0161d6f3e4b`).
  - Read-only reference audit only: `📌 Requirements` DB inside `🗄️ Frozen Git mirror` (`view://2b9b349c-b3a3-4bda-884d-2bb968e1f064`). **NEVER mutate.**
- **Requirements Markdown Corpus:**
  - `Game-Requirements/requirements/01..22.md`
  - `Game-Requirements/drafts/23..27.md` and `Game-Requirements/drafts/2026-09-05-*-symbology.md` (`SYM-MIL-01`, `SYM-CIV-01`)
  - `docs/product/prd-mvp-ui-2026-07-15.md` (+ brainstorm specs where normative)
  - `design/accessibility-requirements.md`, `design/difficulty-curve.md`
  - `docs/superpowers/specs/2026-09-30-requirements-planning-reconciliation.md` (`AME-6.11`, `RPL-29..31`, `LIB-05.1..3`, `VER-07.3..6`), `docs/superpowers/specs/2026-08-17-cmd-38/39-*-draft.md`
  - `docs/requirements/impact-sim-c2-requirements-2026-05-29.md`, `docs/engineering/sim-capability-gap-backlog.md`
  - Notion-only requirements source: `Milsim Comparative Research & New Requirements — 2026-10-07` (`ENV-01..03`, `ASW-01..05`, `EW-01..04`, `C3-01..04`, `DMG-01..02`, `HUM-01..02`, `OPS-01..04`).

### 2.2 Mandatory Exclusions (NEVER count, evaluate, or mutate)
- **`OPEN-CRM-CPQ` (`P-DRG-6`):** 344 Linear issues, 27 Notion tasks, `tests/uat/*cpq*.md`, `tests/uat/lead-to-fulfillment-*.md`.
- **`docs/reference/cmano-db/`:** Catalog reference data, not project requirements.
- **Notion SQL mode:** Never call `notion-query-data-sources` in `sql` or `rows` mode (workspace quota limit); always use `mode: "view"`, `notion-search`, and `notion-fetch` at 1–2 req/s.

### 2.3 Safety & Hygiene Invariants
1. **Zero secrets or `.db3` data:** Never include `.env` credentials, API keys, or proprietary `.db3` filenames/extracts in Jev `state`, JSON outputs, or Linear/Notion records (`backlog_gap_eval.py` enforces pre-flight hygiene rejection).
2. **Never invent `Done`:** Linear `Done` means merged to `main`; Notion `Done` only after Linear is `Done` (`H2`).
3. **Never remove owner holds:** Preserve `hold:owner-go`, `NEEDS_OWNER`, and `single-owner` labels.
4. **Never stamp `Surface:` on Backlog:** Only `drg` stamps `Surface:` at sprint dispatch (`Project Aegis Rubrics §2(b)-(c)`).
5. **Untouched code surfaces:** Zero edits to C# (`src/`), `unity/`, `DelegationBridge.cs`, `CatalogWriteGate`, or replay golden hashes (`17144800277401907079`).

---

## 3. Script-First Gates & Scoring Criteria

### 3.1 Deterministic Mechanical Pre-Checks (`P_*`)

Run deterministically in `tools/jev/backlog_gap_eval.py` **before** Jev evaluation. Jev can confirm a passing mechanical check, and **never overrides a failed mechanical check**.

| Gate ID | Scope | Mechanical Predicate | Failure Effect |
|---|---|---|---|
| `P_SRC` | Shared | `source_file` exists in repo (or is whitelisted Notion-only `Milsim Comparative Research`). | Blocks `complete` (marks `incomplete`). |
| `P_ISO` | Shared | Zero `OPEN-CRM-CPQ` / `P-DRG-6` / `docs/reference/cmano-db/` contamination in requirement or matched items. | Blocks `complete` (marks `incomplete`). |
| `P_LIN_EXISTS` | Linear | `len(linear_matches) > 0`, OR `lifecycle_state == "shipped"` with verified trunk code/test/gate evidence (`shipped_verified`). | If `false` → deterministic **`missing`**. |
| `P_LIN_CONTRACT` | Linear | (Unless `shipped_verified` exempt): (1) cites `source_file` or `req_id`; (2) has testable AC (or `[Phase N]` deferral marker); (3) NO `Surface:` line/label on `Backlog`/`Todo`; (4) honest lifecycle state (no premature `Done`; `Draft`/`Phase N` labeled); (5) cross-links Notion. | If `P_LIN_EXISTS` and `not P_LIN_CONTRACT` → deterministic **`incomplete`**. |
| `P_NOT_EXISTS` | Notion | `len(active_notion_matches) > 0` on active Notion surfaces (`Tasks` DB or `cmano-clone` wiki). Matches *only* in `Frozen Git mirror` do NOT satisfy `P_NOT_EXISTS`. | If `false` → deterministic **`missing`**. |
| `P_NOT_CONTRACT` | Notion | Active Notion match satisfies: (1) if `Tasks` DB, `Project = Project Aegis`, non-null page `icon`, and `Blocked` used as `Status` (not free-text workaround); (2) cites `source_file` or `req_id`; (3) cites Linear `DRG-*` ID/URL (or `shipped_verified`); (4) honest lifecycle state (`H2`: no `Done` before Linear; `Draft`/`Phase N` preserved). | If `P_NOT_EXISTS` and `not P_NOT_CONTRACT` → deterministic **`incomplete`**. |

### 3.2 `S1–S6` Backlog Intake Criteria (`>= 9/12` Equivalent)

Calibrated from `Backlog rubric pass 2026-10-29` (`0/1/2` per criterion, max `12`, pass `>= 9/12`). **Dispatch-only gates `G2` (`Surface:` file list) and `H6` (Play Mode owner sign-off) are explicitly excluded at Backlog intake.**

| Criterion | Description | 0 (Missing) | 1 (Partial) | 2 (Complete) |
|---|---|---|---|---|
| `S1` | Problem + evidence | No problem/context | Partial prose without source evidence | Clear problem statement grounded in source spec |
| `S2` | Testable AC (or deferral gate) | No AC or verification note | Vague goal only | Concrete testable AC checkboxes or explicit `[Phase N]` / `Draft` gate |
| `S3` | Dependencies & lifecycle honesty | Contradicts trunk/ADR state | Missing blocker/dependency note | Dependencies, `Draft`/`[Phase N]`/`active` state accurately marked |
| `S4` | Source citation | No repo/wiki path or `req_id` | Filename only without `req_id` | Exact `source_file`, `source_lines`, and `req_id` cited |
| `S5` | Bidirectional links | Unlinked | One-way link only | Linear ↔ active Notion ↔ source linked both ways |
| `S6` | Clear title & single outcome | Multi-epic dump or vague title | Broad bundle | Single cohesive outcome (`req_id_clause_cluster`) |

### 3.3 Jev System One Thresholds (`data/jev/backlog-gap-v1.questions.json`)

| Primitive | Questions | Threshold / Rule |
|---|---|---|
| `noul` | `linear_presence_and_traceability`, `linear_ac_and_scope_completeness`, `notion_presence_and_traceability`, `notion_completeness_and_honesty` | **Pass requires `noul >= 0.80`**. Probability *is* the uncertainty (no separate confidence field). |
| `score` | `linear_completeness_score`, `notion_completeness_score` | `0 = missing`, `1 = incomplete`, `2 = complete`. Rank/threshold only: **Require `score >= 1.50` for `complete`**. |
| `choice` | `linear_classification`, `notion_classification`, `recommended_remediation` | **Require `choice == "complete"` and `confidence >= 0.50`**. If `confidence < 0.50` or `choice == "other"`, set `needs_human_review = true` and conservatively classify **`incomplete`**. |

---

## 4. Script-First Classification Decision Table

For each side (`linear_status` and `notion_status`), evaluate strictly in order:

1. **Absence (`not P_LIN_EXISTS` / `not P_NOT_EXISTS`):** → **`missing`** (deterministic; Jev cannot override).
2. **Isolation / Source Failure (`not P_ISO` or `not P_SRC`):** → **`incomplete`** (cannot be `complete`; human/data fix required).
3. **Shipped Exemption (Linear only):** If `lifecycle_state == "shipped"` with verified trunk code/test/gate evidence (`shipped_verified_exempt == true`), `linear_status = "complete"` (`sub_status: "shipped_verified"`) without requiring a historical `Backlog` ticket, unless Jev explicitly flags traceability contradiction (`noul < 0.80`).
4. **Mechanical Contract Failure (`not P_LIN_CONTRACT` / `not P_NOT_CONTRACT`):** → **`incomplete`** (deterministic; Jev cannot override).
5. **All Mechanical Checks Pass → Apply Jev Thresholds:**
   - If `noul1 >= 0.80` AND `noul2 >= 0.80` AND `score >= 1.50` AND `choice == "complete"` AND `choice.confidence >= 0.50`: → **`complete`**.
   - If `choice == "other"` OR `choice.confidence < 0.50` OR `recommended_remediation.confidence < 0.50`: → **`incomplete`** + **`needs_human_review = true`**.
   - Otherwise: → **`incomplete`**.

### Derived `recommended_action` Enum
| `linear_status` | `notion_status` | `recommended_action` |
|---|---|---|
| `complete` | `complete` | `none` |
| `missing` | `complete` | `create_linear` |
| `incomplete` | `complete` | `update_linear` |
| `complete` | `missing` | `create_notion` |
| `complete` | `incomplete` | `update_notion` |
| any non-`complete` | any non-`complete` | `both` |

---

## 5. M3 Remediation Rules (Linear First, Active Notion Second)

1. **Linear First (`Project Aegis Rubrics §2(c)`):**
   - Create missing items in status **`Backlog`** (`3fcf0557-d032-48c7-95fb-efd198c700d9`) under the matching `DRG` project (`cmano-clone`, `Combat Interaction UX`, or `Aegis Horizons H4–H6`).
   - Include: `Git source of truth` (`source_file` + `source_lines`), canonical `req_id`s, concise problem statement, testable acceptance criteria (or explicit `[Phase N]`/`Deferred` / `Draft — owner acceptance pending` marker), and Notion cross-link.
   - **NEVER** add a `Surface:` line or `Surface:*` label.
   - Non-destructively append missing pointers/AC/links to existing `incomplete` `Backlog`/`Todo` issues; never delete existing content or invent `Done`.
2. **Active Notion Second:**
   - Create or update a row in **`Tasks` DB** (`Project = Project Aegis`, non-null page **icon**, appropriate `Status` [`Not started` / `Blocked` / etc.], and `Note` containing Linear `DRG-*` URL + `source_file` + `req_id`s + lifecycle tag).
   - For unlanded narrative requirement drafts (`23–27`, `SYM-*`, `CMD-38/39`, `2026-09-30` amendments), ensure `📘 cmano-clone / 📝 Requirements drafts` contains or links the draft cleanly with `Draft — canonical home pending owner land decision` framing.
   - **NEVER edit `🗄️ Frozen Git mirror` (`📌 Requirements` DB).**

---

## 6. Interface Contract JSON Schemas

### 6.1 `requirements-catalog.json` (`PROJECT.md § Interface Contracts #2`)
```json
{
  "generated_at": "ISO-8601",
  "source_files": [
    {
      "path": "repo-relative/path.md",
      "category": "A|B|C|NotionOnly",
      "lines": 123,
      "in_scope": true
    }
  ],
  "requirements": [
    {
      "req_id": "FR-01",
      "title": "...",
      "source_file": "Game-Requirements/requirements/01-Project-Overview.md",
      "source_lines": "45-62",
      "domain_project": "cmano-clone | Combat Interaction UX | Aegis Horizons H4-H6",
      "lifecycle_state": "active | draft | deferred_phase_n | shipped",
      "summary": "...",
      "acceptance_criteria": ["..."]
    }
  ]
}
```

### 6.2 `baseline-gap-matrix.json` & `final-gap-matrix.json` (`PROJECT.md § Interface Contracts #3`)
```json
{
  "evaluated_at": "ISO-8601",
  "rubric_version": "v1",
  "jev_model": "typesafe/jev-1.14",
  "summary": {
    "total_requirements": 0,
    "source_files_covered": 0,
    "linear": { "complete": 0, "incomplete": 0, "missing": 0 },
    "notion": { "complete": 0, "incomplete": 0, "missing": 0 }
  },
  "evaluations": [
    {
      "req_id": "...",
      "source_file": "...",
      "linear_matches": ["DRG-..."],
      "notion_matches": ["page-id-or-url"],
      "linear_status": "complete|incomplete|missing",
      "notion_status": "complete|incomplete|missing",
      "deterministic_checks": {},
      "jev_answers": {},
      "gaps": ["..."],
      "recommended_action": "none|create_linear|update_linear|create_notion|update_notion|both",
      "needs_human_review": false
    }
  ]
}
```
