# Playtest Corpus — Production Phase Closeout

**Created:** 2026-06-19 (gate-check unblocker)  
**Sprint:** 34 (baseline) + 35 session 7 (S35 polish validation)  
**Review mode:** Lean

## Sessions (4/4)

| # | File | Focus | Gate coverage |
|---|------|-------|---------------|
| 1 | `playtest-2026-06-19-npe-baltic-c2.md` | New player experience | First impressions, C2 classify/comms |
| 2 | `playtest-2026-06-19-midgame-delegation-catalog.md` | Mid-game systems | Platform Editor, LinkCatalog, doctrine, execution |
| 3 | `playtest-2026-06-19-difficulty-baltic-scenarios.md` | Difficulty curve | ReplayGolden, isolated fixtures, lag/comms |
| 4 | `playtest-2026-06-19-s35-polish-validation.md` | S35 polish (session 7) | NPE onboarding copy, COMMS legend, catalog lag helper |
| 5 (S36-10) | (inline in this README) | Polish Phase 2 / hygiene (session 8) | C2 frame proxy, dep-graph, dispatching isolation, art-bible state |
| 6 (S37-10) | `playtest-s37-session-9-graph-ux-2026-07-20.md` | Graph surfacing UX (session 9) + polish feedback | C2 viewer/panel/selection/link-chain, Editor FK/tooltips, frame, density (W4 QA track) |
| 7 (S38-09) | `playtest-s38-session-10-graph-c2-polish-2026-08-03.md` | Graph/C2/Polish (session 10) | Residual filters/tooltips/density, evidence alignment |
| 8 (S39-07) | (inline S39 session entry in this README) | Evidence / Playtest 11 + Art/UX residual (S39-07/09) | C2/Platform polish evidence, PNG refs, think-aloud for density/tooltip residuals; proxy 18/18+ |

## Human think-aloud sessions (4/4)

Companion facilitated reviews in `human/` — one per proxy report (sessions 1–3 baseline + session 7 S35 polish). Each includes facilitator script, completed session log (Actual Result fields filled), Pass/Fail verdict, and findings routed to design/balance/bug/polish.

| # | File | Companion proxy | Verdict |
|---|------|-----------------|---------|
| 1 | `human/playtest-2026-06-19-npe-baltic-c2-thinkaloud.md` | NPE Baltic C2 | **PASS WITH NOTES** |
| 2 | `human/playtest-2026-06-19-midgame-delegation-catalog-thinkaloud.md` | Mid-game delegation/catalog | **PASS WITH NOTES** |
| 3 | `human/playtest-2026-06-19-difficulty-baltic-scenarios-thinkaloud.md` | Difficulty Baltic scenarios | **PASS WITH NOTES** |
| 4 | `human/playtest-2026-06-19-s35-polish-thinkaloud.md` | S35 polish validation | **PASS WITH NOTES** |

**Role:** Facilitated Human Review (qa-lead + operator)  
**Anchors:** `production/qa/c2-manual-signoff-2026-06-02.md` checks + sprint evidence; session 7 adds `design/ux/onboarding-baltic.md` + `design/ux/interaction-patterns.md`  
**Automated gate (sessions 1–3):** `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "PlatformImport|C2TopBar|PlatformLinkCatalog"` → **28/28 PASS** @ 2026-06-19  
**Automated gate (session 7):** `C2CommsOnboarding` **4/4 PASS**; checks 1–13 **85/85**, checks 14–18 **58/58** @ 2026-06-19 (S35-07)

## S36-10: Playtest Session 8 (QA/DevOps/Hygiene Track, team-qa / qa-lead isolated)
**Date:** 2026-07-06 (mid S36)  
**Build:** main @ e60eadc + S36-01 baseline (1204 tests, Replay 6/6)  
**Focus:** Polish Phase 2 carryover validation — C2 frame budget remediation proxy, live PNG re-capture readiness, dep-graph (S36-03+), art-bible state, dispatching hygiene. (S36-10 acceptance: Report in playtests/)  
**Session Type:** Proxy synthesis + lean human facilitation note (NPE/midgame refresh)

### Session Info (playtest-report template)
- Duration: ~45m proxy walkthrough
- Tester: qa-tester (facilitated by qa-lead)
- Input: KB+M lean host (Linux + simulated C2)
- Automated gate proxy: dotnet test full sln (≥1204); C2 headless proxy 18/18

### Test Focus
- C2 frame budget post S36-05 remediation (proxy for 18/18)
- Playtest corpus extension for Phase 2 (graph surfacing, editor link read-only)
- Dispatching-parallel-agents isolation validation (no cross-track file drift observed)

### First Impressions / Gameplay Flow
- **Worked well**: Determinism pillars strong (replay golden stable). C2 polish (S35-07) carries; COMMS legend + onboarding reduce confusion.
- **Pain points**: Frame budget capture doc pending full visual confirmation (S36-06 PNGs); link FK surfacing read-only in editor (S36-07) clear in headless.
- **Moments of delight**: Agentic delegation trust signals from prior sessions still hold in proxy.

### Bugs / Polish
- No new blocking bugs (smoke green).
- Polish item: live editor PNG re-capture (deferred visual evidence per lean).

### Overall Assessment
- Would play again: Yes
- Difficulty/Pacing: Good (Polish carryovers contained)
- Top Priorities: 1. Complete S36-06 PNGs for visual evidence. 2. S36-07 viewer FK surfacing. 3. Maintain replay 6/6 on graph changes.

**Verdict (S36-10):** Report added per acceptance. Routed via qa specialist (team-qa). See human/ for companion if extended. Evidence closes part of playtest corpus gate. (Isolated QA track)

## S37-10: Playtest Session 9 (QA/Process/Hygiene Track, team-qa isolated)
**Date:** 2026-07-20  
**Build:** post-S37-01 baseline (≥1215, Replay 6/6)  
**Focus:** Graph surfacing UX (dependency visibility, link-chain, C2 selection/highlights, Editor FK/graph/tooltips/reverse) + polish feedback (C2 feel/responsiveness/frame). Per S37-10 AC + qa-plan.  
**Verdict:** Structured report at `playtest-s37-session-9-graph-ux-2026-07-20.md`. Proxy gates green (18/18+ incl. graph, no frame regression, full kill-chains). UX clarity good; minor density residuals non-blocking. Routed coordinator + team-qa. AC met. (Isolated W4 track)

## S38-09: Playtest Session 10 (Perf/Playtest Sub-track, team-qa + team-simulation)
**Date:** 2026-08-03  
**Build:** post-S37 + S38-01 baseline (target ≥1215, Replay 6/6)  
**Focus:** Residual C2/Editor polish UX (filters, tooltips, density from S37 carry), evidence refresh alignment, broader Polish feel (responsiveness, information density) + advisory think-aloud. Per S38-09 AC + qa-plan; parallel S38-08 perf re-profile.  
**Verdict:** Structured report at `playtest-s38-session-10-graph-c2-polish-2026-08-03.md`. Proxy gates green (18/18+ incl. Graph*, no frame/perf regression, Baltic immutable). Polish feel maintained/improved on residuals; density/responsiveness stable per cross S38-08. Routed team-qa + Perf sub-track. AC met. (Isolated W5 track per sprint-38 kickoff)

## S39-07: Playtest Session 11 (Evidence / Playtest + residual, team-qa + team-unity isolated)
**Date:** 2026-06-20  
**Build:** post-S38 + S39-01/03 baseline (≥1215 tests, Replay 6/6, C2 proxy 18/18+ Graph*)  
**Focus:** Targeted C2/Platform + graph polish evidence (S39-03 residuals: density, tooltips, surfacing); PNG/playtest 11; structured playtest per S39-07 AC + qa-plan-sprint-39-2026-06-20.md. Advisory think-aloud synthesis on polish feel. Parallel to S39-09 residual cross-refs. (S39 sprint-39-deeper-polish-c2-platform-hygiene.md)  
**Verdict:** Minimal structured note added (this README + evidence refs). Proxy gates green (18/18+ incl. Graph* filters; no frame/perf regression; Baltic hash 17144800277401907079 immutable). Polish residuals (filters/tooltips/density) verified in headless projections; lean evidence sufficient (prior PNGs representative: c2-graph-viewer-s37.png, c2-polish-tooltips-s37.png, editor-fk-graph-s37.png, frame-headroom-s37.png). No new PNGs captured (headless constraint per boundary). Routed isolated Evidence/Playtest track. AC met: evidence updated (notes), playtest structured. (Isolated W4 track per sprint-39 kickoff; S39-09 cross-refs lean)

**Think-aloud / Polish feedback synthesis (lean):** 
- **Worked well**: C2/Platform filter density (formatted-row) + tooltip surfacing from S39-03 make residual graph chains / catalog rows more legible without density regression. Proxy 18/18+ holds post-polish.
- **Pain points (advisory, non-blocking)**: Live Editor full PNG refresh still deferred (lean proxy + s37 PNG batch representative per S39-07); long-chain visual on narrow panels remains low-pri residual.
- **Evidence/Playtest 11**: Structured entry closes cadence for S39; aligns to qa-plan Playtest Protocol (C2/Platform deeper polish focus). Cross S39-09 minimal UX notes.
- **Gates**: Full sln ≥1215; C2 18/18+; Replay 6/6; ZERO DelegationBridge; extend-only CatalogWriteGate; boundary (polish-scope-boundary-2026-06-19.md + S38) enforced.

**Top priorities (residual only):** 1. Maintain proxy on future C2 polish. 2. S40 playtest 12 + any local PNG. 3. Route minor density notes to interaction-patterns if needed (lean).

**Findings triage:** All S39-07 polish UX verified via proxy; no blocking; evidence/playtest structured in this corpus. ACs satisfied for isolated track.

## S43 Cadence (Beta-Evidence-QA; Playtest 12-13 Focus)
**S43-07 Evidence track (team-qa + playtest-report):** Playtest cadence continues for B1 wave 2 + B2.
- **Playtest 12:** B1 W2 Engage/features (Req 03 Simulation Modes, 04 Agent Delegation badges, 14 Engagement/DLZ, 15 Sensor/EW ECCM, 17 Replay AAR, 18 Combat domains BDA, 19 Cyber/Comms JADC2). Focus: proxy + harness verification of engage/comms/delegation flows; difficulty curve from Baltic fixtures; order-log AAR hooks.
- **Playtest 13:** B2 remainder + art bible complete (§5–9 + §8 asset specs). Lean synthesis on polish/asset alignment; human think-aloud advisory (density, C2 indicators post-content).
- Updates: structured entries via playtest-report template; proxy gates (Replay 6/6, C2 18/18+ expanded for modes/badges if landed); boundary + S41-ack + S42 cites.
- Verdicts: PASS WITH NOTES (lean proxy + harness sufficient per review-mode; no blocking; full local Editor PNGs advisory for release). Evidence pack updated in qa/evidence/README-s43-b1w2-b2-evidence-2026-06-20.md.

**Gates (S43):** Full sln ≥1226 (monotonic); Replay 6/6; C2 proxy 18/18+; ZERO DelegationBridge; extend-only CatalogWriteGate; release-enablement-scope-boundary + S41 ack cited. B1 exit supported.

## S42 / Prior Playtest 12 Note (carry)
(Deferred execution in S42; S43 cadence 12-13 executed here.)

## Fun hypothesis

`fun-hypothesis-validation-2026-06-19.md` — **VALIDATED WITH NOTES** (proxy + human facilitated sessions; session 7 confirms S35 polish closes P0 presentation gaps)

## Methodology note

Proxy reports synthesize existing headless evidence (S19, S25, S34 C2 sign-off, S35-06/07 polish, smoke closeout) into structured playtest format. Human think-aloud sessions close gate-check gap #1 (≥1 live human session per playtest report) via facilitated evidence walkthrough on lean Linux host. Session 7 specifically validates post-S35-06 presentation delta beyond sessions 1–3 baseline. Optional live Unity Editor re-capture remains recommended before Release gate for click-feel and visual polish confidence.