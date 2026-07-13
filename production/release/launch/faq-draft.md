# FAQ Draft — S71-03/04 Launch Doc Pack (E7)

**Date:** 2026-06-25  
**Track:** S71-04 Launch doc pack — faq-draft (Cloud per roadmap-execute-plan-062526.md §3/§4)  
**Authority / MANDATORY CITES:** production/commercial-launch-scope-boundary-2026-06-25.md ; docs/reports/future-sprint-roadpmap-062526.md §3/§6/§7/§10 ; docs/reports/roadmap-execute-plan-062526.md §3/§4 (S71-03/04 deliverables in production/release/launch/); AGENTS.md ; S69/S70 artifacts (sprint-69-commercial-launch-foundation.md, smoke-sprint-69-closeout-2026-06-25.md, sprint-70-store-community-prep.md + kickoffs, release-checklist-v3.md, store/*, community-templates.md); prior S57–S68 (baltic-v2-playtest-index.md, release-checklist-v2.md, baltic-v2-scope-boundary-2026-06-22.md, release-train-scope-boundary-2026-06-24.md invariants only).  

**GitNexus pre (this track):** list_repos canonical 19962/37627/2462; detect 24/0 low (docs); impact Catalog 178 / Patrol 97 / Bridge 127 / Baltic 52 CRITICAL exact. Low risk.  

**Verification-before (RUN+READ 2026-06-25):** build 0e/0w; tests 1232/0f; Replay 6/6; C2 18/18; hash 17144800277401907079 preserved; ZERO DelegationBridge source edits. Full logs READ. Stage Release. S69/S70 context confirmed.  

**Scope:** Player-facing FAQ draft for E7 prep / Baltic v2. Internal draft; update post S72 prep-complete. No public post yet. Cites required. Docs-only.  

---

## Project Aegis Baltic v2 — Frequently Asked Questions (Draft)

### General
**Q: What is Project Aegis?**  
A: A deterministic, agentic military simulation focused on Baltic operations (v2 content expansion). Features C2 delegation, full replay verification, catalog-driven scenarios, and policy-driven engagements. Prep for commercial aspects in S69–S72 (E7).

**Q: What is Baltic v2?**  
A: 10 policies + 9 golden replays (baltic-v2-* variants covering patrol bands A/B/C, comms/jammed, mission, ROE, combat-domains, spoof, intercept, datalink, theater). Verified 6/6 replay with hash 17144800277401907079. See production/playtests/baltic-v2-scenario-manifest.yaml + qa/evidence/baltic-v2-playtest-index.md.

**Q: Is the game released?**  
A: Commercial launch prep (S69–S72) underway. Store drafts, i18n pipeline spec, launch doc pack, checklist v3, and evidence index in progress. Stage remains Release. Full submission and revenue launch out of scope for this prep program. See commercial-launch-scope-boundary-2026-06-25.md and evidence-index.md.

### Gameplay & Features
**Q: How does replay determinism work?**  
A: Seeded RNG + fixed tick pipeline + order-independent state. ReplayGoldenSuiteTests 6/6 (core + v2) across builds. Every scenario produces identical logs and final world hash. See tests/regression/ and replay-verify skill.

**Q: What is agentic C2 / delegation?**  
A: Players issue high-level orders; agents (delegation) handle low-level execution with trust signals and policy evaluation. Catalog integration for platforms/loadouts. C2 proxy harness 18/18 validated headless.

**Q: Difficulty and scenarios?**  
A: Bands A (NPE/easy), B (mid), C (stress). 10+ variants in manifest. Full playtest corpus from S57–S64 + polish. See difficulty-curve.md + baltic-v2 manifest.

### Technical / Verification
**Q: What verification exists?**  
A: Full gates every sprint: build 0e, 1232 tests 0f (Sim+Data+Del+UA+...), Replay 6/6, C2 18/18, hash preserved, ZERO hotpath DelegationBridge edits. GitNexus code intelligence preflights on all changes. See release-checklist-v3.md + gate-checks/ + sprint closeouts.

**Q: Can I mod or inspect data?**  
A: Catalog + markdown import/export + Excel roundtrip for platforms/sensors. WriteGate extend-only for safety. Deterministic data access enforced.

### Launch Prep & Support
**Q: When will it launch on Steam?**  
A: Prep artifacts (this pack + store drafts + checklist) target S72 prep-complete human ack. Submission decision separate/future. No dates committed.

**Q: Will there be multiplayer?**  
A: Out of current E7 prep scope (ZERO DelegationBridge edits). See boundary docs.

**Q: Localization / languages?**  
A: P0 en-US for prep. i18n pipeline spec + string inventory planned in S71 parallel track. Production translations future.

**Q: How do I report issues or get support?**  
A: See production/release/launch/support-runbook-draft.md (internal triage) and community templates. GitHub issues or in-game logs preferred.

---

## Update Plan
- Sync with patch-notes-template + evidence-index after S71 l10n-qa and S72 gate.  
- Expand with actual player questions post any playtest or announcement.  
- Keep factual, cite evidence-index for all claims.  

**Self-contained draft. All preflight + gates RUN+READ. Cites: commercial-launch-scope-boundary-2026-06-25.md + roadmap-execute-plan-062526.md §3/§4 + future-sprint-roadpmap-062526.md + AGENTS.md + S69/S70 + S57–S68.**

*Cloud subagent S71-03/04. Docs only. Ready for S71-06 closeout.*
