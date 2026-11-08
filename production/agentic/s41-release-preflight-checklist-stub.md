# S41 Release Pre-Flight Checklist Stub (Analysis Only — S41-08)

**Date:** 2026-06-20  
**Sprint:** 41 (S41-08)  
**Status:** Analysis / stub only. **No artifacts produced.** Feeds S41 scope packet + S46/S47.  
**Authorities (cited):**  
- `production/sprints/sprint-41-polish-hardening-release-preflight.md` (S41-08)  
- `docs/reports/future-sprint-roadpmap.md` §5 B5/B6 + §9  
- `production/release-enablement-scope-boundary-2026-06-20.md` §B5/B6 + gate matrix + cut-lines  
- `production/gate-checks/scope-expansion-decision-2026-06-20.md` (APPROVED)  
- `.claude/skills/release-checklist/release-checklist/SKILL.md` (B5 template)  
- `.claude/skills/gate-check/gate-check/SKILL.md` §Gate: Polish → Release (B6)  
- `production/polish-scope-boundary-2026-06-19.md` (S41 in-boundary only)  
- `Game-Requirements/implementation-tracker-2026-06-04.md` + S41 gap analysis (s41-track-b-gap-analysis.md)  
- Determinism audit 2026-06-20 + ADR s41-structural-debt... + GitNexus discipline  

**Constraints:** Pure enumeration of *what B5/B6 require*. Read-only. Prereqs: S41 Polish-exit + gap analysis + clean determinism + S41 ADR + re-index. B5 requires B1+B2 complete. B6 requires B1–B5. No S42 dispatch. Cite boundaries.

## B5 — Launch Artifacts (S46)

**What B5 requires (analysis stub; full execution S46 only):**
- Release checklist (per release-checklist skill + boundary table): `production/release/release-checklist-v1.md` (build verification, quality gates, content complete, platform reqs, store/distribution, launch readiness, Go/No-Go).
- Store page drafts: `production/release/store/` (short/long desc, features, screenshots/trailers per platform, age ratings ESRB/PEGI, legal/EULA/privacy, attributions, pricing).
- Localization pipeline spec: `production/release/i18n-pipeline-spec.md` (externalized strings, pipeline, RTL, cultural review).
- Launch evidence index: `production/qa/evidence/README-release-evidence-*.md` (consolidated post-B1/B2).
- Additional enumerated reqs (from gate Polish→Release + release skill): 
  - B1 content complete (13 tracker rows MVP-done/Partial+ Baltic AC).
  - B2 full 9-section art bible + asset specs + AD-ART-BIBLE verdict.
  - Zero S1/S2 bugs; full QA sign-off; smoke PASS + no regressions (baseline ≥ S41 post-closeout).
  - Perf within budgets + soak (4h+).
  - Analytics/crash reporting; day-one patch plan; on-call 72h; community/press keys; rollback plan.
- GitNexus / invariants: impact() on any Catalog/Platform; cite release-enablement-scope-boundary + committed row IDs; Replay 6/6; hash `17144800277401907079` pinned; C2 proxy expanded if new UI.
- Prereqs for dispatch S46: B1 S43 closeout + B2 complete.

**Stub only — do not create the above files in S41.**

## B6 — Release Gate (S47–S48)

**What B6 requires (analysis stub; gate in S48 only):**
- Per gate-check skill Polish→Release:
  - All milestone features/content (B1 13 rows closed per tracker).
  - QA test plan + /team-qa sign-off (APPROVED).
  - Test evidence for Must-Have + smoke-check PASS (full suite no regressed; Replay 6/6; C2 18/18+).
  - Balance-check run.
  - Release checklist + (if applicable) launch-checklist complete.
  - Store metadata prepared + localization verified + legal (EULA/privacy/age ratings).
  - Build clean + packages; perf targets met; no critical/high/medium bugs; accessibility + i18n basics.
- S47 prep (per readiness-checklist + roadmap): full test sweep, gate-check draft, Buildkite preflight, Go/No-Go checklist, consolidated evidence.
- S48: `/gate-check` (Polish→Release) verdict PASS; human (user + TD) sign-off; write "Release" to `production/stage.txt`; program closeout + retro.
- Standing gates (release-boundary §Standing invariants & gate matrix): tests ≥ post-S41 floor; Replay 6/6 (esp. post B3); C2 proxy floor (expand filters for new checks); hash pinned; DelegationBridge ZERO (ADR only); WriteGate extend-only (ADR only); GitNexus impact() mandatory; monotonic test growth.
- S41 pre-reqs for packet: Polish-exit evidence + this gap analysis (S41-07/08) + determinism-audit clean (0 issues) + S41 ADR + GitNexus current + smoke closeout.
- Full director panel + chain-of-verification per gate skill.

**Stub only — no gate execution or stage change in S41.**

## Cross-Refs + AC

- Full B1–B6 table + GitNexus: see `s41-track-b-gap-analysis.md`.
- New ADR + determinism: cross-ref in gap doc.
- AC for S41-08: "Checklist stub in production/; read-only" → satisfied (this stub + embedded in gap doc). Analysis only.
- Verification: all from reads of sprint-41, boundaries, roadmap §5, tracker, skills, ADR, audit. Sequential decomp used. Scope-check: no creep (enumeration only, cites boundaries, no B impl).

**Paths:** Gap doc `production/agentic/s41-track-b-gap-analysis.md`; this stub `production/agentic/s41-release-preflight-checklist-stub.md`.

*Analysis complete. Pure enumeration. Boundaries + tracker + roadmap cited. Stay scoped.*
