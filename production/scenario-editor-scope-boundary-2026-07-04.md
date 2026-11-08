# Scenario Editor Scope Boundary — S81–S88 Program (E11 / req 11)

**Date:** 2026-07-04  
**Status:** S81-01 deliverable (boundary first per `roadmap-execute-plan-07042026.md` §4/§5/§9; wave 1 local producer). Published before parallel S81 tracks dispatch. Supersedes `production/baltic-v3-scope-boundary-2026-06-25.md` for **S81+ only** (archive prior, do not delete; carry invariants only).  
**Authority:** [`docs/reports/future-sprint-roadpmap-07042026.md`](../docs/reports/future-sprint-roadpmap-07042026.md) §3/§6/§7/§10 + [`docs/reports/roadmap-execute-plan-07042026.md`](../docs/reports/roadmap-execute-plan-07042026.md) §3/§4/§5/§6 + [`production/qa/qa-plan-scenario-editor-2026-07-01.md`](../production/qa/qa-plan-scenario-editor-2026-07-01.md) + [`Game-Requirements/implementation-tracker-2026-07-04.md`](../Game-Requirements/implementation-tracker-2026-07-04.md) + [`Game-Requirements/requirements/11-Agentic-Mission-Editor.md`](../Game-Requirements/requirements/11-Agentic-Mission-Editor.md) + AGENTS.md + prior roadmap/execute-plan 062526.01.md  
**Lead:** E11 / req 11 (Agentic Mission Editor)  
**Sprints:** S81–S88 (serial sprints; 2–4 parallel tracks inside each; local coordinator owns boundary/closeout/merge/human gates/Unity AC-8; cloud agents for validation/CLI/schema/tests/docs)  
**Exit:** S88 program gate (human ack "scenario editor program complete" for the headless slice; stage remains **Release**; no mandatory `production/stage.txt` advance unless explicit separate decision)  
**In-flight branch (pre-boundary):** `fix-scenario-publish-cli-wiring` @ `17d426c` (Graphite PR #237 draft; validation tracks A–D partial +48 tests). Merge/integration plan produced in S81-02; stack re-submit after boundary + user ack.

---

## Purpose

S81–S88 delivers the **headless slice of the Scenario Editor** (req 11 / E11 lead): `ScenarioDocumentEditor` + `ScenarioValidationEngine` core, validation tracks A–D (AC-1…AC-12 minus deferred ADR-014 Lua), CLI/MCP surface, determinism/CI smoke (AC-2/AC-6), and Unity AC-8 round-trip evidence at S87. 

**Headless first.** Map placement, visual event graph editing, and full Unity edit-mode authoring are Phase 2 (post S88 unless re-planned).

Every story / artifact / commit / subagent dispatch **must** cite:
- This boundary (`scenario-editor-scope-boundary-2026-07-04.md`)
- `future-sprint-roadpmap-07042026.md` §3/§6/§7/§10
- `roadmap-execute-plan-07042026.md`
- `qa-plan-scenario-editor-2026-07-01.md`
- `implementation-tracker-2026-07-04.md`
- AGENTS.md
- `11-Agentic-Mission-Editor.md` (AME-* / AC-1…AC-12)
- Prior baltic boundary (invariants only)

**S81-01 deliverable only.** Wave order: S81-01 boundary → (parallel S81-02 branch plan ∥ S81-03 GitNexus re-index) → S81-04 closeout.

---

## Standing Invariants (carried forward; supersede for S81+)

From baltic-v3-scope-boundary + execute-plan §1/§5/§6/§7 + roadmap §0/§5/§7. Apply to all S81–S88 work.

- Test baseline **≥1232** (monotonic). Current measured 1308 pass / 2 known UA failures (pre-existing `BalticReplayHarnessPolicyEngageTests`). Floor never regress.
- **ReplayGolden 6/6** every sprint close; **C2 proxy (PlayModeSmokeHarnessTests) 18/18**.
- Production Baltic hash **`17144800277401907079`** immutable unless explicitly updated via golden ADR + determinism review.
- **ZERO `DelegationBridge`** edits (no .cs source changes to the bridge impl; hotpath refs in consumers/tests/README only; CRITICAL 127).
- **CatalogWriteGate extend-only** (CRITICAL 178). No destructive or replacement writes.
- **GitNexus discipline mandatory** before every edit/claim: `node .gitnexus/run.cjs status`, `impact --summary-only` on §5 CRITICALs + editor symbols (`ScenarioDocumentEditor`, `ScenarioValidationEngine`).
- `baltic-v2-*` / `baltic-v3-*` corpora and goldens are **frozen** for editor work. Editor uses `data/scenarios/examples/*.scenario.json` + schema.
- All editor work is additive to `editorState` / derived fields; no mutation of simulation core hashes without ADR.
- Single owner per sprint for `ScenarioDocumentEditor` (hot symbol). Coordinate with ValidationEngine owner.
- Stage policy: **Release** throughout S81–S88. S88 gate does **not** auto-advance stage.

**CRITICAL / HIGH symbols for editor program (from roadmap §5 + execute-plan §7; exact counts at plan authoring):**
- CatalogWriteGate (178 CRITICAL) — extend-only
- PatrolCandidateEngagePolicy (97 CRITICAL) — avoid
- DelegationBridge (127 CRITICAL) — ZERO touch
- BalticReplayHarness (52 CRITICAL) — read/test only; UA triage owner S86
- ScenarioDocumentEditor (20 CRITICAL) — single owner per sprint
- ScenarioValidationEngine (17 HIGH) — coordinate; export gate owner

---

## Scope (S81–S88)

**In scope (headless editor program):**
- E11 headless authoring spine: `ScenarioDocumentEditor`, command stack (export/undo/ferry/event trace), validation engine hardening.
- Validation tracks A–D per roadmap/execute-plan §4 and qa-plan 19 units (AC-1…AC-12 minus ADR-014 Lua deferral + supporting NFR/arch gates).
- CLI (`ProjectAegis.MissionEditor.Cli`) + MCP surface (`tools/mission-editor/mcp-tools.json` + manifest tests).
- Determinism (AC-2: byte-identical `fire_order` + world-state hash excl. `editorState`; `SEED=/HASH=` contract) and AC-6 CI smoke (`tools/ci/smoke-ac6.sh`).
- Schema/fixture conformance, save-vs-export gate, doctrine inheritance, event debugger JSON, stub-scope pins, event graph caps (ADR-016), no-Lua architecture gate.
- Unity AC-8 round-trip evidence (S87 only): headless-authored scenario load in PlayMode or manual Editor checklist. No map placement or visual editing.
- GitNexus impact/ownership discipline, sprint closeouts, gate verification artifacts.
- Production of sprint plans, kickoffs, boundary updates, and the S88 gate doc.
- Stage remains Release.

**Out of scope (this program):**
- E7 submission / commercial launch steps (already covered).
- E9 v3 promotion or Baltic content expansion.
- Multiplayer, DelegationBridge changes.
- Production hash changes without ADR.
- Full Unity edit-mode authoring, map placement (AME-4.x), visual event graph (Phase 2 post S88).
- Lua / dynamic execution surface (deferred per ADR-014; architecture gate only).
- NFR #18 5k ORBAT perf (opportunistic or waive at S88).
- AC-1 fuel reachability if already covered by existing reachability tests (opportunistic).

**Deferred / flagged:**
- AME-8.5 undo persistence decision (disk vs in-process) — design lock before S83-02.
- 2 UA failures (`Friendly_weapons_tight_surfaces_policy_abort_in_engagement_log`, `Restricted_engagement_scenario_fingerprint_is_deterministic`) — triage/fix/waive owner S86; do not block S81–S85.
- AC-11 TeleportUnit requires prior implementation of the action.

---

## File Ownership & Coordination (S81–S88)

Per execute-plan §7 (hot symbols matrix). One owner per sprint for contended files.

- `ScenarioDocumentEditor`: **single owner per sprint** (S81 read; S82–S83 owner; later read/load).
- `ScenarioValidationEngine`: S82/S84/S85 owner; coordinate with editor track.
- `ValidationRules`: single owner if edited (S84).
- `ScenarioUndoStackStore`: S83 track D owner.
- S83 completed (export polish S83-01, undo wiring+disk persistence note S83-02, ferry+AC-5 S83-03) per sprint-83-export-undo-ferry.md + kickoff + roadmap-execute-plan-07042026.md §3/§4 + qa-plan units #5/#13/#14 + implementation-tracker-2026-07-04.md. Cites added in code/tests/boundary. AME-8.5: disk-backed sidecar confirmed (cross-process). Editor subset + full gates + GitNexus pre/post + ZERO bridge + hash verified.
- Unity C2/editor hosts: S87 owner (additive load only).
- Never touch `DelegationBridge`, avoid `CatalogWriteGate`/`PatrolCandidateEngagePolicy` unless migration.

**GitNexus preflight (mandatory per track):** impact on editor symbols + any §5 CRITICAL touched. Report in closeout.

---

## Stage & Release Policy

- `production/stage.txt` stays **Release** for entire S81–S88.
- S88 gate is "headless scenario editor program complete".
- Any future stage advance or launch decision requires separate boundary + explicit user ack.

---

## Hard Gates (every sprint close + S88 program gate)

See execute-plan §6. Summary:

- Build: `dotnet build ProjectAegis.sln` → 0 errors
- Tests: `dotnet test ... -v minimal` → 0 failed (excl. known UA pair until S86); floor **≥1232**
- Replay: 6/6
- C2 proxy: 18/18
- Determinism hash preserved (or golden ADR)
- Bridge: ZERO edits
- AC-6 smoke: PASS when serialization touched
- GitNexus: fresh, §5 exact counts reported for CRITICALs + editor symbols
- Scope: boundary cite present
- S88 additional: all 19 qa-plan units addressed or waived with ack; AC-1…AC-12 evidence table; human ack

---

## Prerequisites & Handoff

Before S81 parallel tracks:
- This boundary published.
- S81-02 branch integration plan reviewed.
- Phase 0 baseline re-run (GitNexus + build + tests + goldens).
- User ack on plan + boundary.

**Recommended dispatch (per execute-plan §12):**
1. S81-01 (this file) — done.
2. Parallel S81-02 (branch plan) + S81-03 (re-index).
3. S81-04 closeout.
4. User ack + `gt submit` per merge plan.
5. `/sprint-plan` S82 → dispatch validation tracks.

**Worktree convention:** `.worktrees/stack/sprint81/{track-slug}` (or equivalent active path). Use Graphite `gt create` + `gt submit --stack --no-interactive`.

---

*This boundary is the single source of truth for scope for S81–S88. All agents, PRs, and closeouts must reference it explicitly.*
