# Support Runbook Draft — S71-03/04 Launch Doc Pack (E7)

**Date:** 2026-06-25  
**Track:** S71-03/04 Launch doc pack — support-runbook-draft (Cloud, execute-plan §4 S71 table)  
**Authority / MANDATORY CITES:**  
- production/commercial-launch-scope-boundary-2026-06-25.md (E7 prep docs scope; support runbook IN; no live ops yet)  
- docs/reports/future-sprint-roadpmap-062526.md §3/§6/§7/§10  
- docs/reports/roadmap-execute-plan-062526.md §3/§4 (S71-03/04: support-runbook-draft.md)  
- AGENTS.md (GitNexus + verification-before)  
- S69/S70 complete/partial: sprint-*-kickoff-*.md, smoke-*-closeout-*.md, release-checklist-v3.md, community-templates.md  
- Prior: release-checklist-v2.md, baltic-v2-playtest-index.md, release-train-scope-boundary-2026-06-24.md (invariants)  

**GitNexus pre + gates verification-before:** list 19962/37627/2462; detect low 24/0 docs; impacts 178/97/127/52 CRIT exact. Build 0e/0w, 1232/0f, 6/6, 18/18, hash preserved, ZERO bridge (RUN+READ full). Stage Release.  

**Scope:** Internal draft runbook for future support triage. Prep only (S72 gate onward). Not live. Cite all authorities. Docs-only.  

---

## Support Runbook — Project Aegis (Baltic v2 Prep)

### Triage Priorities (P0–P3)

**P0 — Critical (immediate escalation)**  
- Determinism failure: replay diverges from golden (hash != 17144800277401907079 or order log mismatch).  
  - Action: Reproduce with exact seed/policy; capture full order log + world snapshot; attach to issue. Run `dotnet test ... --filter ReplayGoldenSuiteTests`.  
  - Owners: sim-lead + c-sharp-devops.  
- Crash / NRE in core tick or C2 delegation path (hotpath).  
  - ZERO DelegationBridge edits invariant — confirm no source drift.  

**P1 — High**  
- C2 proxy failures (PlayModeSmokeHarnessTests <18/18).  
- Catalog import/export roundtrip corrupt (order-indep or stable hash drift).  
- Playtest blocker in Baltic v2 bands (A/B/C policies from manifest).  

**P2 — Medium**  
- UI/UX polish gaps (panels, graph, tooltips, doctrine).  
- Performance regression outside budgets (use perf-profile artifacts).  
- Documentation drift vs evidence-index or release-checklist-v3.  

**P3 — Low**  
- Cosmetic, community template feedback, FAQ wording.  

### Common Issues + Resolution Paths

| Symptom | Likely Cause | First Steps | Evidence Link |
|---------|--------------|-------------|---------------|
| Replay hash mismatch | Seeded RNG / tick order / wall-clock leak | Re-run golden filter; rg WORLD_HASH; inspect order-log | tests/regression/replay-golden-baltic-v2-*.txt + replay-2026-*.md | 
| C2 18/18 gate fail | Panel bind / graph update / smoke harness change | Run PlayModeSmokeHarnessTests filter; review C2 harness | production/qa/*c2* + smoke closeouts | 
| Catalog write rejected | WriteGate / provenance / quarantine | Check extend-only policy; review import logs | src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs + qa/evidence | 
| Delegation trust signals odd | Policy eval / AAR | Review PatrolCandidateEngagePolicy + PerceivedState | S57 AAR + baltic-v2 policies + playtest index | 
| Data import drift | Markdown/Excel roundtrip | Validate with release diff command + golden | sprint-66 manifest + S65 hardening | 

### Escalation & Tools
- GitNexus: always run search_tool + use_tool (list_repos canonical, detect_changes, impact on CRITICALs) before diagnosing code paths.  
- Replay verify: `dotnet test ... --filter ReplayGoldenSuiteTests`  
- Full gates: build + test + filters + hash grep + bridge grep (see execute-plan §6).  
- Logs: order logs, decision logs, replay checkpoints.  
- Internal: production/qa/ , production/gate-checks/ , production/playtests/ , production/release/  
- Community handoff: use community-templates.md + faq-draft.md + patch-notes-template.md.  

### Post-S72 / Future
- After S72 prep-complete gate + human ack: activate runbook for live support.  
- Link to evidence-index.md for all referenced artifacts (S57–S71).  
- Update with real tickets after external exposure.  

**Draft self-contained. Pre-verified via GitNexus pre + full gates RUN+READ. All cites present.**

*Independent subagent S71-03/04 cloud launch doc pack track. Ready for parallel i18n + l10n-qa + closeout.*
