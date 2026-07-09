# Implement Req 20 rev 2 — C2 UI P0 Delta (Parallel Program)

## Mission
Implement the net-new P0 requirements from
`Game-Requirements/requirements/20-Command-And-Control-UI.md` (rev 2, 2026-07-08).
Already shipped (do NOT rebuild): six UI Toolkit panel hosts, single-select via OOB,
message log with full categories, top bar, sensor strip.

Source docs (read all before any code):
- Requirement: `Game-Requirements/requirements/20-Command-And-Control-UI.md`
- Rationale:   `Game-Requirements/reviews/requirements-20-ux-review-2026-07-08.md`
- GDD:         `design/gdd/command-and-control-ui.md` (selection set, projections, TR-c2-005..008)
- UX spec:     `design/ux/c2-command-post.md` (components, interaction map, AC 6–8)
- A11y:        `design/accessibility-requirements.md` (§3 ScalePercent, §6.3 remap stub IDs)

## Non-negotiable ground rules
1. ADR-010: UI binds projections only. UI never mutates sim state — all player actions
   go through the bridge command API as logged intents.
2. GitNexus: run `impact({target, direction: "upstream"})` BEFORE editing any symbol;
   report blast radius; STOP and ask on HIGH/CRITICAL. Run `detect_changes()` before
   every commit.
3. Graphite: one stack per track, `gt create` / `gt submit --stack --no-interactive`.
   No raw `git push`, no `gh pr create`.
4. Zero diff to `DelegationBridge`. Production Baltic hash `17144800277401907079`
   must remain unchanged. Full sln tests ≥ 1215 pass; C2 headless proxy 18/18+;
   ReplayGolden 6/6.
5. Collaboration protocol: show draft + ask before writing files; no commits without
   user instruction (`docs/COLLABORATIVE-DESIGN-PRINCIPLE.md`).

## Phase 0 — Contracts (single agent, blocks everything)
One small PR defining shared surface so Tracks 1–5 never touch each other's files:
- `SelectionSet` (ordered TargetIds) API on the presentation controller; single select = set of one.
- `OrderLifecycleState` enum: accepted → queued → executing → completed | denied | aborted.
- `AlertSeverity` (Critical/Notable/Routine) + mapping table from MessageLogProjection categories.
- USS custom-property tokens for OrderStateChip, ToastStack (AA contrast per a11y doc §2).
- Remap stub IDs wired as constants: `input.cycle_unit`, `input.focus_primary_threat`,
  `input.cancel` (a11y §6.3).
Gate: compiles, tests green, zero behavior change.

## Phase 1 — Parallel tracks (launch as concurrent subagents after Phase 0 merges)

| Track | Scope | TR | Key ACs (req 20) |
|-------|-------|----|------------------|
| **T1 Selection** | Drag-box `SelectionBox` on map placeholder; shift-click add/remove; ctrl-click OOB rows; `N`/`P` cycle; center-on-selection; group-order fan-out (one intent per eligible unit) with pre-commit eligibility list | TR-c2-005 | AC-7 |
| **T2 Order lifecycle** | Projection from order log records → `OrderStateChip` in right panel + log rows; weapons-release confirmation gate (Enter/Esc) when policy projection flags positive control; `PlayerOrderCancelled` command | TR-c2-006 | AC-8 |
| **T3 Alerting** | Severity mapping consumer; `AutoPauseRequested` onto existing pause-reason stack (command, not mutation); `ToastStack` max 3 + `+N` overflow, click focuses unit/`sequenceId`; per-category log filters; suppress all in replay | TR-c2-007/008 | AC-9 |
| **T4 Symbology** | APP-6(D) frame subset on placeholder map; icon size ladder per zoom band; label declutter priority (selected > engaged > hostile > friendly) with leader lines; shape-primary per a11y §5 | — | AC grayscale check |
| **T5 Perf/scaling** | Virtualized ListView/TreeView audit for OOB + message log (5k-row scroll, no frame drop); PanelSettings scale-with-screen-size @1920×1080; wire `C2AccessibilitySettings.ScalePercent` {100,125,150} to USS font-size properties; 10px floor | — | AC-1, AC-10 |

Track isolation rules:
- T1 owns the presentation controller selection code; T2/T3 consume Phase 0 contracts only.
- T3 owns the message log host; T5 may only touch its virtualization/USS, coordinate via
  contract file if both need edits — flag conflict to user instead of merging silently.
- All tracks: headless-first (ADR-010) — every feature must be assertable in the C2
  headless proxy before any Editor visual work.

## Phase 2 — Integration (single agent, after all tracks merge)
1. `gt sync`; merge order T5 → T4 → T2 → T3 → T1 (T1 last: touches most shared UI).
2. `detect_changes({scope: "compare", base_ref: "main"})` — verify affected symbols
   match track manifests; investigate anything unexpected.
3. Run full gates (rule 4). Extend C2 headless proxy with: multi-select fan-out check,
   order-state transition check, auto-pause stack check.
4. Update `design/gdd/command-and-control-ui.md` zone table statuses and
   `docs/architecture/requirements-traceability.md` for TR-c2-005..008.
5. Produce a lean playtest report per `production/playtests/` template covering AC-7/8/9/10.

## Out of scope (do not implement)
P1/P2 items: legend overlay, agent activity digest, saved selection sets, order queue
view, bookmark alert routing, ultrawide, gamepad, globe/Cesium (ADR pending).
