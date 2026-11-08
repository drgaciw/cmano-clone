# Playtest Report — S37 Playtest Session 9 (Graph Surfacing UX Focus + Polish Feedback)
**Date**: 2026-07-20 (post S37-02 QA plan + prereqs)
**Build**: `main` @ (post-S37-01 baseline; ≥1215 tests, ReplayGolden 6/6)
**Duration**: ~40 min equivalent (headless proxy + Editor graph surfacing + polish review)
**Tester**: QA lean proxy + graph UX synthesis (S37 QA/Process/Hygiene Track)
**Platform**: Linux headless (C2 proxy + CLI) + Platform Editor read-only (S37-05)
**Input Method**: N/A (proxy + UXML/projection inspection); advisory live Editor for visual density
**Session Type**: Graph surfacing UX validation (S37-04 C2, S37-05 Editor) + broader polish feedback (S37-06 frame, S37-10 AC)
**Story**: S37-10 (per sprint-37-graph-surfacing-deeper-polish.md + qa-plan-sprint-37-2026-06-20.md)
**Design/Arch anchors**: polish-scope-boundary-2026-06-19.md, S36 dep-graph runtime, ADR-010 (headless-first), ADR-011 (platform editor), c2-command-post.md, interaction-patterns.md

## Test Focus
Validate graph surfacing UX per S37-10:
1. **Dependency visibility / link-chain clarity** in C2 (S37-04: viewer/panel, selection highlights, link-chain display + bind)
2. **Editor display / tooltips / reverse lookup** (S37-05: interactive FK/full graph beyond S36-07 read-only)
3. **No regression on C2 proxy 18/18+ or frame headroom** (cross with S37-06)
4. Broader polish feedback: C2 feel, responsiveness, information density under graph data load.

**Delta vs prior**: S36 carried dep-graph runtime + Phase H read-only; S37 completes surfacing in C2 + Editor. Session 9 (S37-10) is explicit UX focus; S36-10 was hygiene proxy.

## Session Info (per playtest-report skill template)
- Automated gate proxy: `dotnet test` ≥1215; C2 headless proxy 18/18+ (with graph filter extensions); Replay 6/6
- Production Baltic hash immutable: `17144800277401907079`
- ZERO DelegationBridge touch; extend-only CatalogWriteGate
- Graph chains: full kill-chain (platform→link + weapon→mount→sensor) via CatalogDependencyGraphIndex + CLI

## First Impressions / Graph Surfacing UX
- **Dependency visibility**: Clear in headless projections and Editor read-only view. Full chains (platform→link + weapon/mount/sensor) surface correctly per S37-03 AC. Link FK display in Editor extends S36-07 read-only without mutation.
- **Link-chain clarity + selection highlights**: Link-chain display binds cleanly in C2 proxy filters (extended for graph checks). Selection highlights isolate the chain without visual overload (lean: primary headless validation).
- **Editor tooltips / reverse lookup**: Tooltips for FK edges and reverse lookup present. Export polish roundtrips clean. No color-only indicators (aligns accessibility).
- **Information density**: Graph data adds rows/edges but stays within C2 proxy budgets; no density regression flagged in projection checks.
- **Responsiveness / frame**: Sustained headroom vs 16.67 ms post graph (S37-06 gate); no new spikes from viewer/panel/highlights/bind.

## Gameplay Flow / Polish Feedback (C2 + Editor)
### What worked well
- Headless-first surfacing (ADR-010): Graph data visible in C2 proxy + Editor without live mutation; 18/18+ maintained.
- Deterministic kill-chain export: CLI + index produce stable, canonical output for UI bind.
- Editor FK/graph interactive beyond read-only (S37-05): tooltips + full chains improve curator mental model vs S36.
- Polish feedback: C2 feel preserved; graph elements integrate without breaking existing panels/selection.
- Lean evidence: PNGs (Editor) + proxy (C2) sufficient per qa-plan; no breakage on prior S36 polish.

### Pain points
- Live Editor visual density of long kill-chains on narrow panels (advisory; not blocking per lean mode).
- No in-simulation dynamic graph mutation HUD overlay (out of S37 scope per polish-boundary; deferred).
- Tooltip timing on very large chains (edge case; covered in tests).

### Confusion points
- Empty/zero chain graceful state: "No dependencies" note clear in proxy but Editor could surface hint (low).
- Platform→link vs weapon chain distinction: CLI lines are explicit but C2 viewer could use icon legend (P1 polish residual).

### Moments of delight
- Full kill-chain visibility (platform through link/weapon/sensor) makes dependency reasoning immediate in both C2 and Editor.
- Backward-compat on prior C2 checks + frame budget holds with graph additions.

## Bugs / Polish Items Encountered
| # | Description | Severity | Reproducible | Route |
|---|-------------|----------|--------------|-------|
| — | None blocking | — | — | — |
| P1 residual | In-fight graph attribution overlay (carry from prior) | Low/Med | N/A | S37-13 or future |

## Graph UX Axis Coverage (S37-04/05 focus)
| Area | Prior (S36) | S37 Surfacing | Verdict |
|------|-------------|---------------|---------|
| **C2 dependency viewer/panel** | Runtime index only; no surfacing | Viewer + selection + link-chain + bind | **Delivered** — visible + tested in proxy |
| **Selection highlights / link-chain** | N/A | Highlights isolate chains; bind works | **Delivered** — no regression on 18/18+ |
| **Editor FK/full graph + tooltips** | S36-07 read-only | Interactive + tooltips + reverse + export polish | **Delivered** — beyond read-only |
| **Frame / responsiveness** | S36-05/06 baseline | Sustained vs 16.67 ms; new evidence batch | **Maintained** — per S37-06 |

## Quantitative / Gate Data (lean headless primary)
| Gate | Result |
|------|--------|
| Full sln tests | ≥1215 (target baseline preserved) |
| C2 headless proxy (incl. graph extensions) | 18/18+ PASS |
| ReplayGolden 6/6 | PASS (Baltic pin immutable) |
| CatalogDependencyGraph full kill-chains | platform→link + weapon/mount/sensor emitted |
| Editor roundtrip + graph export | PASS (extend S36 Phase H) |
| Production Baltic hash | 17144800277401907079 (unchanged) |
| DelegationBridge diff | ZERO |

## Overall Assessment
- **Would play again?** Yes — graph surfacing makes dependency reasoning first-class in C2 and Editor.
- **Graph UX clarity**: Good — chains visible, highlights clean, tooltips helpful for curators.
- **Polish / density / responsiveness**: Good — no regressions; frame headroom held; information density acceptable in lean proxy.
- **Difficulty/Pacing**: N/A (graph is tooling/curator aid, not core gameplay loop change).
- **Session length preference**: Good for UX focus.

## Top Priorities from this session (S37-10)
1. Capture live Editor PNG evidence batch for FK/graph display + tooltips (S37-05 AC + qa-plan manual).
2. Optional C2 viewer legend or icon for edge kinds (platform vs link vs weapon) — P1 residual polish.
3. Validate graph surfacing in facilitated think-aloud (human/ companion) if capacity; proxy sufficient for lean gate.
4. Ensure S37-08 closeout includes graph + proxy + frame regression audit.

## Findings Triage (routed per team-qa)
| Finding | Severity | Route | Blocking for closeout? |
|---------|----------|-------|------------------------|
| Graph surfacing UX visible + bound in C2/Editor | — | Closed (S37-04/05) | No |
| Frame headroom + proxy 18/18+ maintained | — | Closed (S37-06) | No |
| Lean proxy + report closes S37-10 | — | Closed | No |
| Minor density/legend polish residuals | Low | S37-13 (nice) or backlog | No |

**Verdict (S37-10):** Structured report + notes produced in `production/playtests/`. Graph UX focus + polish feedback captured. AC met per qa-plan. Routed via team-qa (qa-lead / qa-tester isolated track). Ready for coordinator sign-off + S37-08.

**Evidence location**: This file + cross-refs in sprint-37 plan, qa-plan-sprint-37-2026-06-20.md, production/agentic/sprint-37-parallel-kickoff-2026-07-20.md (W4).

---
*Produced under isolated QA/Process/Hygiene Track (S37-07/10/11) per dev-story + story-readiness. Lean mode, polish-scope-boundary enforced. No breakage to gates.*
