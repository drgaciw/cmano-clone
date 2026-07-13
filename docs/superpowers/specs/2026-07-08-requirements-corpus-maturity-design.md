# Requirements Corpus Maturity (docs 01–21) — Design Spec

**Date:** 2026-07-08  
**Project:** Project Aegis (cmano-clone)  
**Status:** Approved — 2026-07-08 (user); Wave 0 plan ready  
**Authority:** User decisions 2026-07-08 (whole-corpus scope; Approach A wave program)  
**Origin story:** [story-001-project-overview.md](../../../production/epics/requirements-maturity-slice/story-001-project-overview.md) (S12 Complete — process handoff only)  
**Prior pattern:** [mission-editor-requirements-improvement-plan.md](../../../Game-Requirements/mission-editor-requirements-improvement-plan.md)  
**Active code train:** [future-sprint-roadpmap-07042026.md](../../reports/future-sprint-roadpmap-07042026.md) (S81–S88 scenario editor / req 11) — **not superseded** by this program

---

## 1. Goal

Make every requirement document under `Game-Requirements/requirements/` (**01–21**) **trustworthy**:

1. **Shipped claims match code/tests/gates** (or are explicitly marked unmeasured).
2. **Aspirational work is phased** (v1 / Phase N IDs), not mixed into “current bar” language.
3. **Hub doc 01 + master index + RTM stay consistent** with on-disk corpus and the implementation tracker.
4. **Draft Template B docs (13–20)** reach design-reviewable maturity using the mission-editor reconcile rubric.

**Program exit:** Consistency report **0 BLOCKER**; hub Related Index lists all on-disk req docs; each of 13–20 has implementation mapping **or** explicit “no runtime — Phase N”; design-review memo for hub + sample per cluster.

**Not this program’s job:**

- Deliver scenario editor features (S81–S88 / req 11 remains the **code lead**).
- Re-litigate S56 **21/21** MVP grades (additive evidence only).
- Commercial rename, multiplayer, global campaigns, engine rewrite.
- Reopen locked product pillars without **new ADR + explicit user decision**.

---

## 2. Problem statement (from review)

### 2.1 Story-001 (S12)

| Aspect | Finding |
|--------|---------|
| Process handoff | Adequate: Template A, Related 02–20, design-review LOCKED |
| Durable story quality | Thin: no TR-IDs, ADR matrix, Given/When/Then, verify commands, per-AC evidence |
| Sibling stories 002–007 | Same thin pattern (systemic epic quality, not unique to 001) |

**Do not reopen** the completed epic bar. Open a **new corpus maturity program** with modern story structure for Wave 0+.

### 2.2 Doc 01 (hub) P0 gaps

| ID | Gap |
|----|-----|
| P0-1 | Related Requirements Index ends at **20**; **missing 21 Platform Editor** |
| P0-2 | FR table FR-01…18 only; no **FR-19** for platform/catalog authoring |
| P0-3 | Success criteria (5k@60 FPS, ≥256× AvA, blind AAR, researcher API) read as **current bar**, not north-star |
| P0-4 | Header/status narrative stuck at Sprint 12; no Release / S80 / S81+ editor program framing |
| P0-5 | “First release” multi-theater language vs Baltic vertical-slice reality |

### 2.3 Corpus maturity map

| Cluster | Docs | Doc status (headers) | Impl (tracker 2026-07-04) | Gap class |
|---------|------|----------------------|---------------------------|-----------|
| Hub | 01 | Locked (stale index) | MVP-done | Catalog + honesty |
| Locked Template A | 02–08, 12 | Locked (Jun 2026) | Partial / Partial+ | Mapping + evidence; **no vision reopen** |
| Research content | 09–10 | Research-integrated (May 29) | Partial / Partial+ | TRL vs shipped content |
| Authoring (gold) | 11 | **Revised 2026-07-01** (`AME-*`) | Partial — **active S81–S88** | Residual only; **do not re-do** |
| Template B drafts | 13–20 | Draft (May 29) | Partial / Partial+ | **Largest debt** — IDs, ACs, phase honesty, reconcile |
| Platform editor | 21 | Draft (Jun 17) | MVP-done / Partial+ | Promote toward 11-style (`PLE-*`) |

### 2.4 Consistency / RTM debt

- Architecture RTM still frames **01–12 + MVP 13–20**; **C-05** (req 21 outside original RTM) open.
- Research RTM for doc 01 is thin (clean-room row only).
- Last consistency report (`docs/reports/requirements-consistency-2026-06-04.md`, refresh 2026-06-19) **0 BLOCKER** but pre-dates doc 11 revision and editor program.
- RTM numeric baselines (old test/smoke counts) lag current floors (**≥1232**, C2 **18/18**, hash **`17144800277401907079`**).

---

## 3. Approach selection

| Option | Summary | Decision |
|--------|---------|----------|
| **A — Wave program + mission-editor rubric** | 5 waves; parallel agents per cluster; docs-first | **Selected** |
| B — Demand-driven only | Deepen docs only when code lands | Rejected for whole-corpus goal (too slow/uneven) |
| C — Shallow lint only | Metadata/link sweep, deep only on FAIL | Rejected as sole mode (13–20 need deep pass); **may be used as W2 pre-flight** |

**Coordination rule:** S81–S88 scenario editor remains the **primary code train**. This program is a **parallel docs train** (worktrees, docs-only by default). Doc 11 residual implementation gaps stay owned by the editor program.

---

## 4. Architecture

### 4.1 Program shape

| Dimension | Value |
|-----------|-------|
| Waves | **W0 → W4** (serial wave exits; parallel tracks inside each wave) |
| Default mode | **Docs-only** |
| Reconcile-with-code | Only when a claim is written as shipped and is false — then **fix the doc** (prefer) or file a code story (out of this program unless user expands scope) |
| Stage | **Release** throughout |
| MVP grades | **Frozen** @ S56; tracker Post-S56 notes may add “doc maturity re-baseline YYYY-MM-DD” only |
| Worktrees | `.worktrees/stack/req-maturity/{wave}-{slug}/` (or sprint-aligned if folded into a numbered sprint) |
| Dispatch | Fan-out: one agent per doc or paired cluster; local closeout gate |

### 4.2 Mission-editor reconcile rubric (normative per doc)

Every touched requirement doc must end the wave with:

1. **Metadata** — `Last Updated`, `Related`, `Status` (per-doc header is authoritative for design status).
2. **Addressable IDs** — for Template B / authoring feature rows (`XXX-n.m`); hub keeps high-level **FR-nn** capability map only.
3. **v1-shipped vs Phase N** — any overclaim becomes a Phase N ID; shipped slice states reality.
4. **AC checkboxes** — check only with green automated test or cited gate artifact; otherwise leave unchecked + note.
5. **Implementation mapping** — table: capability → code/data/test path → status (Shipped / Partial / Gap / Phase N).
6. **Open questions** — ADR status visible (Proposed vs Accepted); no silent “deferred forever.”
7. **Verification** — relative links resolve; IDs unique within doc; cited paths exist; no new BLOCKER in consistency pass for that cluster.

### 4.3 Template rules (do not flatten)

| Template | Docs | Do |
|----------|------|-----|
| **A** — Strategic / agentic | 01–08 | Keep Purpose/Vision/FR/NFR/Technical/Agentic/Future/Open-or-Resolved. Add mapping + honesty; **do not** force CMO parity tables. |
| **B** — CMO simulation slice | 11, 13–20 | Keep CMO basis, P0/P1/P2 tables, ACs, phased delivery, manual traceability. |
| **Hybrid authoring** | 11, 21 | 11 already `AME-*`; 21 moves toward `PLE-*` + Excel write-gate honesty. |
| **Glossary** | 12 | Locked vocabulary; additive terms only with cross-links. |
| **Research** | 09–10 | Research-integrated; TRL/content alignment; no new speculative scope without user decision. |

### 4.4 Locked vision — do not reopen without ADR + user decision

Protect unless explicitly reopened:

1. Hardcore military simulation first (CMO-class depth).
2. Agentic command as core fantasy (delegate units / task forces / sides).
3. Near-future focus 2028–2035 + optional Future Combat / speculative layer.
4. Deterministic, headless-capable sim with extreme time compression as architectural goal.
5. Clean-room discipline (no proprietary CMO DB/scenarios/code/manuals).
6. Primary audience = hardcore wargamers.
7. Out of first commercial release: global campaigns; full multiplayer (hotseat/async later; live unscheduled); battalion-and-below land warfare.
8. Working title **Project Aegis** until commercial rename.
9. Platforms: Windows primary Steam client; Linux headless farm; no macOS/console/Deck in v1 targets.
10. English-only v1 with externalized strings; data modding yes, code/plugin modding no.
11. S56 MVP grades frozen — post-MVP work is additive evidence only.
12. Engineering law: production replay hash **`17144800277401907079`**, **DelegationBridge zero-touch**, **CatalogWriteGate extend-only**, Baltic v3 isolation (`baltic-v3-*`).
13. Doc 01 remains charter/index/NFR spine — detailed mechanics stay in child docs.

### 4.5 Standing engineering invariants (must appear on hub NFR after W0)

| Invariant | Reference |
|-----------|-----------|
| Solution test floor ≥1232, 0 gate failures (known UA exclusions documented) | AGENTS.md |
| ReplayGolden 6/6 (v2); production hash `17144800277401907079` | tests/regression, AGENTS.md |
| PlayModeSmokeHarness 18/18 | UnityAdapter tests |
| DelegationBridge zero-touch through Release v1 | AGENTS.md |
| CatalogWriteGate extend-only | ProjectAegis.Data |
| Baltic v3 policies/goldens isolated from v2 | baltic-v3 scope boundary |

---

## 5. Wave map

### Wave 0 — Hub catalog + charter honesty *(first sub-project)*

**Goal:** Doc 01 is a trustworthy front door for the corpus and program stage.

**Primary files:**

- `Game-Requirements/requirements/01-Project-Overview.md`
- `Game-Requirements/Game-Requirements-Index.md` (reading order + program note)
- Optional light: architecture RTM C-05 note; consistency addendum stub

**Deliverables:**

| # | Deliverable |
|---|-------------|
| W0-1 | **FR-19** Platform Editor → [21](../../../Game-Requirements/requirements/21-Platform-Editor.md) |
| W0-2 | Related Requirements Index covers **all on-disk** req docs (02–21); working relative links |
| W0-3 | Status column refresh: doc **11 = Revised**; implementation grades → point at tracker (do not invent new “Draft means unfinished code”) |
| W0-4 | **Scope tiers:** (A) Shipped vertical slice (Release through S80 Baltic content), (B) Active program (scenario editor / req 11), (C) Product ambition (multi-theater scale, etc.) |
| W0-5 | **Success criteria split:** gate-backed (tests, ReplayGolden, hash, C2 smoke) vs north-star (5k entities, 256×, blind AAR, external researcher API) tagged Measured \| Proxy \| Deferred + evidence path |
| W0-6 | **Standing invariants** subsection in NFR/Technical |
| W0-7 | Future Extensibility leads with **scenario editor program (req 11)** |
| W0-8 | Header: Last Updated = wave close; Related includes index + tracker; commercial name remains **Open** |
| W0-9 | Story file for Wave 0 (modern structure: Context, ACs, verify commands, evidence matrix) — **not** rewrite of completed story-001 |

**Story seed — Wave 0**

- **Title:** Req 01 charter re-baseline + corpus hub (post-S80)
- **Type:** Documentation + traceability
- **Acceptance (testable):**
  1. FR-19 present; Related Index lists every file under `Game-Requirements/requirements/` except 01 itself (or includes 01 as self-row — pick one convention and document it: **recommend 02–21 only**).
  2. `grep -E 'FR-19|21-Platform' 01-Project-Overview.md` hits; `test -f` for every Related link target.
  3. Each of the three original success bullets is tagged Measured|Proxy|Deferred with one evidence path or explicit “not measured.”
  4. Invariants subsection cites hash + DelegationBridge + test floor.
  5. Design-review memo under `production/qa/` (full mode for hub, not lean-only four checks).
  6. Tracker Post-S56 note for req 01 may add re-baseline date only — **no MVP regrade**.

**Non-goals for W0:** Full AME-style ID soup on hub; rewriting Vision/Goals tone; GDD expansion (optional later).

---

### Wave 1 — Locked Template A honesty *(02–08, 12)*

**Goal:** Locked docs stay locked on decisions; gain implementation mapping + post-Baltic evidence pointers.

**Docs:** 02, 03, 04, 05, 06, 07, 08, 12  

**Rules:**

- Preserve **Resolved Design Decisions** and locked superpowers specs (e.g. core loop, simulation modes, delegation).
- **Do not** reopen phase gates, autonomy tiers, or determinism contracts.
- Add/refresh: implementation mapping rows, links to Baltic v2/v3 policies or tests where relevant, FR reverse-ref accuracy, “Partial / Partial+” honesty footer pointing at tracker.
- Doc **07**: clarify product vs process agentic infrastructure without demoting either.

**Parallel dispatch suggestion:**

| Track | Docs |
|-------|------|
| W1-a | 02 + 03 (loop + modes) |
| W1-b | 04 + 05 (delegation + dynamic systems) |
| W1-c | 06 (database intelligence) |
| W1-d | 07 + 08 (agentic infra + architecture) |
| W1-e | 12 (glossary additive pass only) |

**Exit:** Each doc has Last Updated ≥ wave date; mapping table or equivalent; 0 broken internal links in touched files.

---

### Wave 2 — Template B drafts *(13–20)* — largest debt

**Goal:** Bring sim/C2 requirement docs to design-reviewable, implementation-honest maturity.

**Docs:** 13–20  

**Order (dependency-friendly):**

1. **13** Doctrine / ROE / EMCON / WRA  
2. **15** Sensors / detection / EW  
3. **14** Engagement & fire control  
4. **17** Replay / AAR / order log  
5. **16** Logistics & magazines  
6. **18** Combat domains  
7. **19** Cyber & comms  
8. **20** Command & control UI  

**Pre-flight (optional, cheap):** Corpus link/metadata lint across 13–20 (Approach C as W2 day-0).

**Rules:**

- Prefer **GDD + Baltic policies + named tests** as truth for P0.
- Unbuilt CMO parity → P1/P2 with stable IDs; do not delete parity tables.
- Introduce or normalize addressable IDs per doc family (e.g. `ROE-*`, `ENG-*`, `SEN-*`, `LOG-*`, `RPL-*`, `CMD-*`, `CYB-*`, `DOM-*`) — **pick prefixes in plan; no collisions across corpus**.
- AC sections: check only with evidence; document known UA engage failures as excluded gate items where relevant (req 14).
- Cross-link hub FR-11…18 and GDDs (`policy-roe-emcon-wra`, `sensor-detection-ew`, `order-log-replay`, etc.).

**Parallel dispatch suggestion:**

| Track | Docs |
|-------|------|
| W2-a | 13 + 14 |
| W2-b | 15 + 16 |
| W2-c | 17 |
| W2-d | 18 + 19 |
| W2-e | 20 |

**Exit:** Each of 13–20 has Template B completeness checklist PASS (metadata, P0 table honesty, ≥1 mapping row or Phase N note, AC evidence policy, open questions statused).

---

### Wave 3 — Research content + platform editor *(09, 10, 21)*

**Goal:** Align content docs with shipped Baltic / S54+ evidence; raise platform editor to authoring-doc maturity.

| Doc | Actions |
|-----|---------|
| **09** Near-Future | TRL claims vs catalog/content shipped; no new systems without user decision |
| **10** Speculative | S54 Kessler/orbital notes; escalation ladder remains Phase N if unshipped |
| **21** Platform Editor | `PLE-*` IDs, ACs, Excel write-gate honesty, ADR-011 status, link hub FR-19 |

**Doc 11:** **Out of Wave 3 rewrite.** Residual AME gaps owned by S81–S88. Only allow **pointer fixes** if hub/index changes require them.

**Exit:** 09/10/21 pass rubric; FR-19 reverse-link from 21 → 01.

---

### Wave 4 — Corpus gate

**Goal:** Prove the corpus is consistent and reviewable.

**Deliverables:**

| # | Deliverable |
|---|-------------|
| W4-1 | New `docs/reports/requirements-consistency-YYYY-MM-DD.md` — **0 BLOCKER** |
| W4-2 | Architecture RTM: include **req 21**; refresh maturity/GDD notes; update numeric baselines to current floors |
| W4-3 | Research RTM: optional enrichment for doc 01 vision/goals (not required for exit if clean-room row remains + hub is honest) |
| W4-4 | Full design-review memo: hub + one locked Template A + one Template B + 21 |
| W4-5 | Implementation tracker: Post-S56 notes for touched req rows (“doc maturity re-baseline”) — **no MVP regrade** |
| W4-6 | Master index Last Updated + program note (corpus maturity complete; editor still active) |

**Known open allowed as CONCERN only:** commercial product name (**C-03**).

---

## 6. Coordination with S81–S88 (scenario editor)

| Rule | Detail |
|------|--------|
| Lead train | Scenario editor code/tests/QA plan (req 11) |
| This train | Docs maturity waves; default no `.cs` / schema / golden edits |
| Doc 11 ownership | Editor program owns residual AME gaps and AC checkboxes tied to new code |
| Merge discipline | Graphite stacks preferred; no force-push; closeout re-runs gates only if code was touched (should be rare) |
| Conflict resolution | Code train wins on schedule; docs train yields or splits PR |

---

## 7. Story / epic packaging

### 7.1 New epic (recommended)

**Slug:** `requirements-corpus-maturity`  
**Path:** `production/epics/requirements-corpus-maturity/EPIC.md`  
**Not** a reopen of `requirements-maturity-slice` (S12–15 Complete).

| Story | Wave | Title |
|-------|------|-------|
| 001 | W0 | Hub re-baseline (req 01 + index) |
| 002 | W1 | Locked Template A honesty (02–08, 12) — may split into 002a–e if capacity requires |
| 003 | W2 | Template B drafts (13–20) — prefer sub-stories per track W2-a…e |
| 004 | W3 | Content + platform (09, 10, 21) |
| 005 | W4 | Consistency gate + RTM + design-review |

### 7.2 Story quality bar (all new stories)

Unlike S12 story-001, each story **must** include:

- Context (stage, tracker, non-goals)
- Governing ADRs / locked specs cited
- Acceptance criteria that are **testable** (verify commands or checklists)
- Completion notes with **per-AC evidence paths**
- Code Review: N/A (docs) **or** doc review full mode — not silent skip without QA path

---

## 8. Verification (program-level)

### 8.1 Per-wave commands (docs)

```bash
# Related targets exist for hub (after W0)
ls Game-Requirements/requirements/*.md | wc -l   # expect 21

# No orphan FR-19 claim without file
test -f Game-Requirements/requirements/21-Platform-Editor.md

# Broken relative links (spot-check pattern — expand in plan)
# Prefer existing repo link checkers if present; else manual path test for each Related row
```

### 8.2 Gates if any code is touched (exception path)

Full AGENTS verification block: `dotnet build`, `dotnet test` (≥1232), PlayModeSmoke 18/18, hash grep, ZERO DelegationBridge hotpath edits. Default waves **must not** require this.

### 8.3 Consistency exit

- `docs/reports/requirements-consistency-YYYY-MM-DD.md` verdict: **0 BLOCKER**
- Commercial name Open → CONCERN only

---

## 9. Risks

| Risk | Mitigation |
|------|------------|
| Scope expands into rewriting all GDDs | Explicit non-goal; GDD STUB for overview optional only |
| Agents reopen locked decisions | Rubric §4.4; reject PRs that change Resolved Design Decisions without user ADR |
| Doc 11 thrash with S81–S88 | Wave 3/4 exclude 11 rewrite; pointer-only |
| Wave 2 too large for one sprint | Split into sub-stories W2-a…e; exit per track |
| Success criteria “downgrade” politics | Frame as **honesty tiers**, not removing north-stars |
| Parallel agents conflict on glossary/terms | W1-e owns 12; W2 agents propose terms, do not edit 12 without W1-e or closeout |

---

## 10. Success criteria (program)

1. Every file in `Game-Requirements/requirements/` has current metadata and resolving Related links.  
2. Doc 01 Related Index includes **21**; FR-19 present.  
3. Success criteria on hub are tiered (gate vs north-star) with tags.  
4. Standing invariants documented on hub.  
5. Each of **13–20** has implementation mapping **or** explicit Phase N for unbuilt P0-looking claims.  
6. Doc **21** has addressable IDs + AC/evidence policy.  
7. Consistency report **0 BLOCKER**.  
8. S56 MVP grades unchanged; tracker notes additive only.  
9. S81–S88 code train not blocked by this program.

---

## 11. First implementation plan target

After this spec is **user-approved**:

1. Invoke **writing-plans** for **Wave 0 only** (hub re-baseline) — smallest valuable slice.  
2. Do **not** plan all five waves in one mega-plan; each wave gets its own plan after prior wave exit.  
3. Collaborative protocol: show draft diffs for `01-Project-Overview.md` and index before Write apply if user still requires path-level approval at execution time.

**Spec → plan path:** `docs/superpowers/plans/2026-07-08-requirements-corpus-maturity-wave0-plan.md` (name may adjust to actual plan date).

---

## 12. Open decisions (spec-level)

| Topic | Default if user silent | Notes |
|-------|------------------------|-------|
| Epic packaging path | New epic `requirements-corpus-maturity` | Do not reopen S12 epic |
| Wave 0 Related Index includes 01 self-row? | **No** — index 02–21 only | Matches current pattern |
| Fold waves into numbered sprints? | Optional; can run as docs train beside S81+ | User/sprint-plan choice |
| Expand research RTM for 01 | Optional for W4 | Not program-blocking |
| GDD `game-concept.md` expansion | Out of W0; optional later | RTM GDD STUB remains acceptable if hub is charter of record |

---

## 13. References

- `Game-Requirements/requirements/01-Project-Overview.md`
- `Game-Requirements/Game-Requirements-Index.md`
- `Game-Requirements/implementation-tracker-2026-07-04.md`
- `Game-Requirements/mission-editor-requirements-improvement-plan.md`
- `production/epics/requirements-maturity-slice/EPIC.md` + `story-001-project-overview.md`
- `production/qa/sprint-12-design-review-2026-06-04.md`
- `Agentic-Development-Plan.md` (Template A/B)
- `docs/reports/requirements-consistency-2026-06-04.md`
- `docs/architecture/requirements-traceability.md`
- `docs/reports/future-sprint-roadpmap-07042026.md`
- `AGENTS.md` (invariants)

---

## 14. Spec self-review log (2026-07-08)

| Check | Result |
|-------|--------|
| Placeholder scan | No TBD/TODO left in normative wave exits; open decisions table is explicit defaults |
| Internal consistency | W0 first; doc 11 excluded from rewrite; S81–S88 lead reiterated |
| Scope | Whole corpus via waves; first plan = W0 only (decomposition OK) |
| Ambiguity | FR-19 ID fixed; Related Index 02–21; Measured\|Proxy\|Deferred tags defined for hub success criteria |

---

**End of design spec.**  
Next: user reviews this file → on approval, writing-plans for Wave 0 only.
