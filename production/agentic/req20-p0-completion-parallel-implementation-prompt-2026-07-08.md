# Implement Req 20 P0 Completion — C2 Command Post Core (Parallel Program)

## Mission
Implement the remaining P0 scope of `Game-Requirements/requirements/20-Command-And-Control-UI.md`
(rev 2) not covered by the rev 2 delta program (`req20-rev2-parallel-implementation-prompt-2026-07-08.md`,
landed on `c2-req20-integration`): globe map, context menus, doctrine/EMCON/WRA access,
delegation overlays, multitasker mode, mission runtime entry — plus the four Phase 2b
residuals recorded in `design/gdd/command-and-control-ui.md` §Implementation status.

Already shipped (do NOT rebuild): six panel hosts, multi-select/group orders (TR-c2-005),
order lifecycle projection + OrderStateChip + weapons gate (TR-c2-006 partial),
severity mapping + ToastStack (TR-c2-007 partial), log filters (TR-c2-008), top-bar
mode indicator.

Source docs (read all before any code):
- Requirement: `Game-Requirements/requirements/20-Command-And-Control-UI.md` (rev 2)
- GDD:         `design/gdd/command-and-control-ui.md` (incl. §Implementation status residuals)
- UX spec:     `design/ux/c2-command-post.md`; placeholder map: `design/ux/c2-map-placeholder.md`
- Delegation:  doc 04 + `docs/superpowers/specs/2026-05-28-agent-delegation-framework-design.md`
- Doctrine:    doc 13 (Policy Snapshot, EMCON, WRA); doc 03 (time/pause); doc 11 (missions)
- Globe:       `docs/engineering/cesium-phase-b-spike-checklist.md`; open ADR (req 20 open question 3)
- Traceability: `docs/architecture/requirements-traceability.md#c2-ui-rev-2-tr-c2-005008`

## Non-negotiable ground rules
1. ADR-010: UI binds projections only; every player action is a logged intent via the
   bridge command API.
2. GitNexus: `impact({target, direction: "upstream"})` before editing any symbol; report
   blast radius; STOP on HIGH/CRITICAL and ask. `detect_changes()` before every commit.
   Re-index (`node .gitnexus/run.cjs analyze`) after the globe assembly lands.
3. Graphite: one stack per track; `gt create` / `gt submit --stack --no-interactive`; no raw pushes.
4. Baseline gates: full sln tests (current baseline 1551) green; C2 headless proxy green
   incl. rev 2 seams; ReplayGolden 6/6; production Baltic hash `17144800277401907079` unchanged.
5. **DelegationBridge exception:** the standing zero-diff rule is LIFTED ONLY for Track T4,
   only via the Phase 0 delegation-surface ADR, and only after explicit user approval of
   that ADR. All other tracks: zero diff, verified at integration.
6. Collaboration protocol: draft + ask before writing; no commits without user instruction.

## Phase 0 — Decisions and contracts (single agent; blocks all tracks)
Two user decisions, then one contracts PR:
- **D1 Globe ADR** (req 20 open question 3): Cesium vs custom URP terrain. Present the
  Phase B spike checklist evidence and a recommendation; user decides. T1 blocked until accepted.
- **D2 Delegation surface ADR**: read-projections (`DelegationStateProjection`: owner,
  autonomy level, personality, paused) + command set (`AgentPauseRequested`,
  `AgentResumeRequested`, `AutonomyLevelChangeRequested`) as the ONLY DelegationBridge
  extension. User must approve before T4 starts (rule 5).
Contracts PR (zero behavior change):
- `IContextActionProvider` registry: tracks register menu actions (id, label, eligibility
  predicate over projections, intent factory). The menu shell (T2) renders whatever is
  registered — this is the seam that keeps T2/T3/T4/T6 conflict-free.
- **Pause-reason stack** contract in sim control (doc 03 semantics): push/pop reasons
  (user, multitasker bookmark, auto-pause severity, agent gate); sim runs only when empty.
- `PositiveControlRequired` policy projection flag (replaces the `WeaponsTight` proxy noted
  in TR-c2-006 status).
Gate: compiles, tests green, GitNexus impact on `IRoeFilter`/orchestrator paths reported.

## Phase 1 — Parallel tracks (concurrent subagents after Phase 0 merges)

| Track | Scope | Req 20 anchor | Closes |
|-------|-------|---------------|--------|
| **T1 Globe** | Per D1: WGS84 globe, pan/zoom/rotate, theater quick-jump; unit/contact symbols + APP-6 ladder ported from placeholder; map pick → selection set; drag-box on globe; isolate in its own asm/def | §Map and Symbology | TR-c2-004 |
| **T2 Context menus** | Unit + map context menu shells consuming `IContextActionProvider`; core actions registered: attack options, plot course, formation, assign mission (stub to T6), measure distance, add reference point; all emit logged intents; no modal-only paths (AC-6) | §Context Menus | — |
| **T3 Doctrine/EMCON/WRA** | Right-panel doctrine tab with inheritance chain (doc 13); EMCON + WRA read/edit via policy snapshot commands; wire `PositiveControlRequired` into the shipped weapons gate; "Why can't I fire?" explain already partial — verify + extend to WRA reasons | §Unit Detail Panel, parity rows §3.3.12–15 | TR-c2-006 residual (flag) |
| **T4 Delegation overlays** | Per D2 ONLY: badge (human/agent/mixed, autonomy color), badge click → personality + autonomy slider + pause/resume agent, intent-preview ghost (Assisted) before engage commit, OOB human/agent filter; registers "delegate agent" context action | §Delegation Overlays | — |
| **T5 Sim control + multitasker** | Implement pause-reason stack per Phase 0 contract; wire shipped `AutoPauseCommand` to it (closes TR-c2-007 residual); multitasker bookmarks (camera + selection save/restore, agent state restore); bridge cancel affordance emitting `PlayerOrderCancelled` (closes TR-c2-006 residual) | §Simulation Controls, §Alerting | TR-c2-006/007 residuals |
| **T6 Mission runtime entry** | Mission list activate/deactivate as logged intents (doc 11 runtime); edit-mode toggle → Mission Board entry; registers "assign mission" context action | §Mission and Editor Entry | — |

Track isolation:
- T2 owns menu shells; T3/T4/T6 contribute actions only through the registry — never edit
  menu shell files.
- T5 owns sim-control/pause code; T4's agent pause routes through T5's stack via command.
- T1 is asm-isolated; may merge independently whenever green.
- Headless-first: every behavior assertable in the C2 headless proxy before Editor visuals.
  T1 visual work additionally evidenced by `ui_capture_state` PNGs.

## Phase 2 — Integration (single agent)
1. `gt sync`; merge order: T5 → T3 → T4 → T6 → T2 → T1 (T2 late: consumes all registries;
   T1 independent).
2. `detect_changes({scope: "compare", base_ref: "main"})` — affected symbols must match
   track manifests; DelegationBridge diff must contain ONLY the D2-approved surface.
3. Full gates (rule 4) + proxy extensions: context-intent logging, pause-stack semantics,
   badge/ghost projections, mission activate.
4. **Editor PlayMode pass** — the rev 2 finisher noted in the GDD: full smoke of all zones,
   menus, globe on `baltic-patrol` / `baltic-patrol-mission`; capture PNG evidence.
5. Update GDD zone/TR status tables, `requirements-traceability.md`, and req 20 open
   questions (mark #3 resolved per D1). Lean playtest report per `production/playtests/`.

## Out of scope
P1/P2: legend overlay, agent activity digest, saved selection sets, order queue view,
bookmark alert routing, ultrawide, gamepad, custom overlays, Tacview, side-level agent
commander panel, detachable windows (open question 2).
