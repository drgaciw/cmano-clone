# Requirement 20 (Command & Control UI) — UX / GUI Expert Review

**Date:** 2026-07-08
**Reviewer:** Unity game UX review (user-directed session)
**Scope:** `Game-Requirements/requirements/20-Command-And-Control-UI.md` rev 1 (2026-05-29)
**Evidence base:** `design/gdd/command-and-control-ui.md` (2026-06-02), `design/ux/c2-command-post.md`, `design/accessibility-requirements.md` (S35-03), `production/playtests/playtest-s37-session-9-graph-ux-2026-07-20.md`
**Outcome:** Requirement revised in place to **rev 2** (same date). Cascade to GDD / UX spec deferred by user decision — see Follow-ups.

---

## Verdict

**Strong foundation, approved with revisions.** The command-post vision, CMO parity matrix with manual traceability, headless-first projection binding (ADR-010), and the committed WCAG AA accessibility tier are well above genre baseline. The gaps below are concentrated in interaction fidelity (selection, command feedback), session-scale UX (alerting, agent attribution), and document hygiene (stale open questions, contradictory NFRs).

## Findings

| # | Finding | Severity | Disposition (rev 2) |
|---|---------|----------|---------------------|
| F1 | Selection model single-unit only (`TargetId` in GDD). No multi-select, group orders, or unit cycling — core CMO parity for theater-scale play. | **High** | Fixed — new §"Selection and Command Model" (P0) |
| F2 | No command feedback / undo taxonomy: order lifecycle states, weapons-release confirmation, cancel plotted course all unspecified. Only "Why can't I fire?" existed. | **High** | Fixed — order state taxonomy + confirmation gate + cancel/replan added; new acceptance criteria |
| F3 | No alerting / interruption model: no event severity tiers, auto-pause rules (CMO §6 game options), toast vs log routing, or per-category log filters. Undermines the hours-long-session vision and multitasker mode. | **High** | Fixed — new §"Alerting and Interruption" |
| F4 | Contradictory NFRs: 60 FPS pan @5k symbols (NFR table) vs "above 30 FPS" (acceptance criterion 4); panel "<100 ms" vs "within one frame budget" (criterion 1). No UI Toolkit perf guidance (list virtualization). | **Medium** | Fixed — reconciled to 60 target / 30 floor on min-spec; first-paint ≤1 frame, full refresh ≤100 ms; virtualized ListView mandated for OOB + message log |
| F5 | DPI/scaling underspecified: only 1920×1080 floor. No PanelSettings scale mode, 4K / OS-scaling behavior, or ultrawide rules beyond "collapsible P1". | **Medium** | Fixed — new §"Resolution, Scaling, and Layout Adaptation" aligned to `C2AccessibilitySettings.ScalePercent` |
| F6 | Symbology loosely specified: "NATO/APP-6 style" without frame subset, icon size ladder per zoom, label declutter rules, or in-UI legend. Legend flagged twice (S37 playtest confusion point; accessibility lean review note #3) without landing in the requirement. | **Medium** | Fixed — APP-6(D) subset committed; size ladder, declutter, legend (P1) added with traceability |
| F7 | Delegation UX lacks post-absence attribution: no "what did the agent do while I was away" digest; in-fight attribution overlay is an untracked P1 residual. Trust-critical for long sessions. | **Medium** | Fixed — P1 agent activity digest added to Delegation Overlays |
| F8 | Stale open question #1 (UI Toolkit vs UGUI) — decided in practice: six UI Toolkit panel hosts shipped per GDD. Requirement status still "Draft" from 2026-05-29 despite most P0 zones shipped. | Hygiene | Fixed — UI Toolkit committed in NFR table; open question removed; status re-baselined to Committed rev 2 |
| F9 | Playtest feedback not fed back: S37 session 9 flagged narrow-panel kill-chain density, icon legend, empty-state hints — absent from requirement. | Hygiene | Fixed — legend + density guidance captured; empty-state hint noted as Editor-side (out of doc 20 scope) |
| F10 | Keyboard parity scheduled too late: CMO §10.1 is keyboard-heavy but shortcuts sat wholly at P1. Accessibility doc already defines remap stub IDs. | Hygiene | Fixed — selection cycling, engage, center-on-threat promoted to MVP alongside pause/compression |

## Not changed (deliberate)

- **GDD / UX spec / accessibility doc untouched** — user decision: requirement-only pass. See Follow-ups.
- **Gamepad** remains deferred (Sprint 5+ per UX spec) — correct call for a keyboard/mouse-primary genre.
- **Globe technology** (Cesium vs custom URP) remains an open ADR — rewritten, not resolved, in rev 2.
- Date anomalies noted but not corrected here: `c2-command-post.md` "Last Updated 2026-08-03" and the S37 playtest filename date `2026-07-20` are in the future relative to this review.

## Follow-ups (recommended stories — not created)

1. Cascade rev 2 deltas into `design/gdd/command-and-control-ui.md` (selection model beyond single `TargetId`; alerting projections) and `design/ux/c2-command-post.md` (interaction map rows for multi-select, order states, toasts).
2. UX spec open item: create `design/player-journey.md` (already flagged in c2-command-post.md §11).
3. Promote S37 P1 residual "in-fight graph attribution overlay" into the agent activity digest story.
4. COMMS/symbology legend tooltip copy story (accessibility lean review note #3).

## Traceability

| Ref | Relationship |
|-----|--------------|
| `requirements-13-20-design-review-2026-05-29.md` | Prior design review of rev 1 |
| ADR-010 | Headless-first, command-driven UI — unchanged by this review |
| S35-03 `design/accessibility-requirements.md` | Scaling enum + remap stub IDs reused, not duplicated |
| S37-10 playtest session 9 | Source of F6/F9 evidence |
