# Agent Coordination and Delegation Map

## Organizational Hierarchy

```
                            [Human Developer]
                                  |
                  +---------------+---------------+
                  |               |               |
          creative-director  technical-director  producer
                  |               |               |
         +--------+--------+     |        (coordinates all)
         |        |        |     |
   game-designer art-dir  narr-dir  lead-programmer  qa-lead  audio-dir
         |        |        |         |                |        |
      +--+--+     |     +--+--+  +--+--+--+--+--+   |        |
      |  |  |     |     |     |  |  |  |  |  |  |   |        |
     sys lvl eco  ta   wrt  wrld gp ep  ai net tl ui qa-t    snd
                                  |
                              +---+---+
                              |       |
                           perf-a   devops   analytics

**S36-13 refinement (coordinator / c-sharp-devops-engineer isolated, QA/DevOps/Hygiene):** dispatching-parallel-agents pattern: 
- One sub-agent per domain/track (Data/Unity/Sim/QA-DevOps-Hygiene).
- Isolated context ONLY: story list + sprint excerpts + gates; NO cross-track file history or mutable state.
- Dispatch rule: prereqs (S36-01/02) serial; then parallel waves.
- Evidence: sprint-36 kickoff dispatch example + stack prefixes (e.g. stack/sprint36/qa-devops-hygiene-closeout).
- Update: added explicit "no shared mutable" + "one agent per sub-track" to prevent drift in Polish.

**S37-07 refinement (coordinator + c-sharp-devops-engineer + team-qa + team-ui isolated, QA/Process/Hygiene Track):** dispatching-parallel-agents wave tracking + status sync + templates:
- Wave tracking: Explicit waves (W1 Data Track, W2 UI/C2+Editor Track, W3 Sim/Perf Track, W4 QA/Process/Hygiene Track [S37-07 dispatching, S37-10 playtest, S37-11 tests hygiene], Closeout S37-08 after waves green). See sprint-37 plan §Parallel Execution Waves & Tracks.
- Status sync: Per-track independent updates to `production/sprint-status.yaml` (status: in_progress/done) + appends to `production/session-state/active.md` (Session Extract — /dev-story). Coordinator aggregates without cross-track mutation. Lean mode primary.
- Multi-track examples: 4 isolated tracks + closeout. Dispatch rule: prereqs (S37-01 rebaseline + S37-02 QA plan) serial; then ` /story-readiness` + `/dev-story dispatch` per sub-track only. Stack prefix: `stack/sprint37/qa-process-hygiene` for this track. Kickoff artifact validates.
- Kickoff templates: Canonical sections required in production/agentic/sprint-*-parallel-kickoff-*.md : header (date/trunk/sprint/qa-plan refs), sprint goal, wave plan table, carryovers, risks, hard gates list (replay 6/6, C2 18/18+, Baltic hash immutable, extend-only CatalogWriteGate, ZERO DelegationBridge), dispatch order (bash blocks with comments), post-dispatch notes, lean + polish-boundary enforcement note.
- Backward compat: S36/S35 patterns remain valid (additive only). Existing kickoffs continue to work; S37-07 docs + examples are refinements only. No change to core "one agent per sub-track + isolated context" rule.
- Enforced for this track: polish-scope-boundary-2026-06-19.md + lean (production/review-mode.txt) + qa-plan-sprint-37-2026-06-20.md. All stories cite boundaries. No out-of-scope.

**S38-10 refinement (coordinator + team-qa isolated, QA/Process Track for dispatching QA integration):** dispatching QA patterns + team-qa integration + lean validation examples:
- QA/Process track isolation: Dedicated coordinator + team-qa routing for meta-QA and dispatching stories (e.g. S38-10 itself validates examples in kickoff + coordination-map). One focused sub-agent; no shared state with C2/Art/Hygiene/Perf tracks. Stack prefix example: `stack/sprint38/qa-process`.
- Dispatching QA specifics: 
  - /qa-plan sprint always precedes any parallel waves (blocks dispatch).
  - In QA/Process track: run `/story-readiness` + `/dev-story` for process stories; optionally invoke `/team-qa [sprint]` (lean) inside the dispatched context for QA sign-off integration and dispatching validation.
  - Wave tracking specific to QA: W6 (S38-10 dispatching QA) parallel to Perf/Playtest after prereqs + core hygiene. Closeout aggregates only after all tracks (incl. QA) report green.
  - Validation patterns: Kickoff artifact + coordination-map serve as executable example. Check: backward-compat (S36–S37 kickoffs unaffected), no cross-track file writes, smoke/replay/C2-proxy gates still enforced independently per track, /team-qa output isolated to qa/ + session-state/.
- Status sync for QA: Independent updates via sprint-status.yaml (per-track keys) + active.md appends (e.g. `<!-- QA DISPATCH: ... -->`). Coordinator monitors; no mutation of other tracks' state.
- Lean mode enforcement: production/review-mode.txt=lean; no PRs; headless proxy primary for evidence; polish-boundary + S37/S38 gates cited in all dispatched work. /team-qa uses lean review mode automatically.
- Multi-example validation: S38 kickoff contains explicit # QA/Process (dispatching QA) dispatch block + post-dispatch notes. Prior tracks (S37-07, S36-13) remain valid without change.
- Backward compat: Additive only. Core rule unchanged: one agent/sub-track + isolated context. S38-10 adds QA-specific orchestration detail (team-qa delegation inside dispatched QA process track) and validation checklist for dispatching integration.
- Enforced for this track: polish-scope-boundary-2026-06-19.md + lean + qa-plan-sprint-38-2026-08-03.md. S38-10 AC: updated examples in kickoff + coordination-map (this doc). No out-of-scope.

**S39-08 refinement (coordinator isolated, Dispatching + Art/UX residual track):** deeper polish dispatch pattern validated against S39 kickoff:
- Serial prereqs enforced: S39-01 baseline + S39-02 QA plan before C2/Platform/Hygiene/Perf waves (see sprint-39-parallel-kickoff-2026-06-20.md).
- Stack prefixes: `stack/sprint39/c2-platform-deeper`, `stack/sprint39/hygiene-ci`, `stack/sprint39/perf-replay`, `stack/sprint39/closeout` (Graphite-first per AGENTS.md).
- Hygiene track dispatch example (S39-04): isolated prompt for c-sharp-devops-engineer with "Hygiene / tests layout + docs refinement — S39-04" + cite sprint-39-*.md + polish-scope-boundary-2026-06-19.md; tasks: append sign-off to directory-structure.md, refine this map for dispatching, minor AGENTS/CLAUDE layout polish; verify hybrid retained + CI would pass; output summary + "S39-04 COMPLETE - isolated track". No src edits.
- One agent per sub-track; isolated context; no shared mutable state across tracks.
- Closeout aggregates only after must-have tracks green (S39-06 smoke + sprint-status).
- Backward compat: S37/S38 kickoff patterns remain valid (additive). Enforced: polish-scope-boundary + lean + qa-plan-sprint-39-2026-06-20.md.

**S40–S48 program wave tracking (coordinator additive, 2026-06-20):** 10-sprint Release train S39→S48:
- **Serial sprints, parallel tracks inside each sprint only.** Never two full sprints concurrently.
- **Track A (Polish):** S39 COMPLETE (1215 tests, Replay 6/6, C2 18/18) → S40 → S41. Cites `polish-scope-boundary-2026-06-19.md`.
- **Scope gate:** After S41; template `production/gate-checks/scope-expansion-decision-template-2026-06-20.md`. No S42 agents until signed.
- **Track B (S42–S48):** **READY TO DISPATCH (docs only)** — blocked at scope gate. Epics B1–B6 per roadmap §9.
- **Coordinator docs:** `s39-s48-program-execution-guide.md`, `s39-s48-worktree-manifest.md`, `local-cloud-agent-routing.md`, `sprint-42-48-readiness-checklist.md`.
- **Kickoffs:** `production/agentic/sprint-NN-parallel-kickoff-2026-06-20.md` (N=40..48). Stack prefix `stack/sprintNN/<track-slug>`.
- **Wave caps:** S40=4 (Catalog single-agent), S41=4, S42=5, S43=5, S44=3 code + replay, S45=4, S46=4, S47=4, S48=1–2 serial.
- **Local vs cloud:** Editor evidence + closeout = local; code/test/hygiene = cloud-eligible (`local-cloud-agent-routing.md`).
- **Standing gates:** Baltic hash immutable; Replay 6/6; C2 18/18+; DelegationBridge ZERO (Polish) / ADR (Release); CatalogWriteGate extend-only.
- **Backward compat:** S36–S39 patterns valid; additive program tracking only.

   Additional Leads (Tier 2 - report to technical-director or game-designer):
     military-simulation-architect  -- Military simulation architecture, tactical systems, combat mechanics
     requirements-analyst          -- Requirements feasibility, technical constraints, implementation complexity

   Military Simulation Specialists (Tier 3 - report to military-simulation-architect or requirements-analyst):
     simulation-parameter-analyst    -- Weapon systems, platform capabilities, sensors, engagement calculations
     gameplay-mechanics-analyst     -- Combat balance, progression, tactical depth, genre conventions
     technical-constraints-analyst   -- Performance budgets, computational complexity, scalability
     military-research-specialist    -- Military tech, historical ops, tactical doctrines, platform specs
     simulation-genre-analyst       -- Genre conventions, market trends, competitor analysis, innovation
     user-experience-military-analyst -- Information density, command complexity, cognitive load, UX patterns

  Additional Leads (report to producer/directors):
    release-manager         -- Release pipeline, versioning, deployment
    localization-lead       -- i18n, string tables, translation pipeline
    prototyper              -- Rapid throwaway prototypes, concept validation
    security-engineer       -- Anti-cheat, exploits, data privacy, network security
    accessibility-specialist -- WCAG, colorblind, remapping, text scaling
    live-ops-designer       -- Seasons, events, battle passes, retention, live economy
    community-manager       -- Patch notes, player feedback, crisis comms

  Engine Specialists (use the SET matching your engine):
    unreal-specialist  -- UE5 lead: Blueprint/C++, GAS overview, UE subsystems
      ue-gas-specialist         -- GAS: abilities, effects, attributes, tags, prediction
      ue-blueprint-specialist   -- Blueprint: BP/C++ boundary, graph standards, optimization
      ue-replication-specialist -- Networking: replication, RPCs, prediction, bandwidth
      ue-umg-specialist         -- UI: UMG, CommonUI, widget hierarchy, data binding

    unity-specialist   -- Unity lead: MonoBehaviour/DOTS, Addressables, URP/HDRP
      unity-dots-specialist         -- DOTS/ECS: Jobs, Burst, hybrid renderer
      unity-shader-specialist       -- Shaders: Shader Graph, VFX Graph, SRP customization
      unity-addressables-specialist -- Assets: async loading, bundles, memory, CDN
      unity-ui-specialist           -- UI: UI Toolkit, UGUI, UXML/USS, data binding
      Unity C# SDLC specialists (role-oriented, span the C# code lifecycle):
        c-sharp-architect      -- Design: asmdef/namespace layout, DI, C# contracts, patterns
        c-sharp-engineer       -- Build: MonoBehaviour/ScriptableObject/plain C# implementation
        c-sharp-test-engineer  -- Test: EditMode/PlayMode (NUnit), fixtures, mocks, dotnet test
        c-sharp-devops-engineer -- CI/CD: dotnet/MSBuild, Unity batchmode, game-ci, packaging
        c-sharp-reviewer       -- Review: GC/async/nullable/lifecycle/SOLID, contract adherence

    godot-specialist   -- Godot 4 lead: GDScript, node/scene, signals, resources
      godot-gdscript-specialist    -- GDScript: static typing, patterns, signals, performance
      godot-csharp-specialist      -- C#: .NET patterns, [Signal] delegates, async, type-safe node access
      godot-shader-specialist      -- Shaders: Godot shading language, visual shaders, VFX
      godot-gdextension-specialist -- Native: C++/Rust bindings, GDExtension, build systems
```

### Legend
```
sys  = systems-designer       gp  = gameplay-programmer
lvl  = level-designer         ep  = engine-programmer
eco  = economy-designer       ai  = ai-programmer
ta   = technical-artist       net = network-programmer
wrt  = writer                 tl  = tools-programmer
wrld = world-builder          ui  = ui-programmer
snd  = sound-designer         qa-t = qa-tester
narr-dir = narrative-director perf-a = performance-analyst
art-dir = art-director
```

## Delegation Rules

### Who Can Delegate to Whom

| From | Can Delegate To |
|------|----------------|
| creative-director | game-designer, art-director, audio-director, narrative-director |
| technical-director | lead-programmer, devops-engineer, performance-analyst, technical-artist (technical decisions), military-simulation-architect, requirements-analyst |
| producer | Any agent (task assignment within their domain only) |
| game-designer | systems-designer, level-designer, economy-designer, military-simulation-architect (military simulation features) |
| lead-programmer | gameplay-programmer, engine-programmer, ai-programmer, network-programmer, tools-programmer, ui-programmer |
| art-director | technical-artist, ux-designer |
| audio-director | sound-designer |
| narrative-director | writer, world-builder |
| qa-lead | qa-tester |
| release-manager | devops-engineer (release builds), qa-lead (release testing) |
| localization-lead | writer (string review), ui-programmer (text fitting) |
| prototyper | (works independently, reports findings to producer and relevant leads) |
| security-engineer | network-programmer (security review), lead-programmer (secure patterns) |
| accessibility-specialist | ux-designer (accessible patterns), ui-programmer (implementation), qa-tester (a11y testing) |
| [engine]-specialist | engine sub-specialists (delegates subsystem-specific work) |
| [engine] sub-specialists | (advises all programmers on engine subsystem patterns and optimization) |
| c-sharp-architect | c-sharp-engineer (implementation), c-sharp-test-engineer (test strategy for exposed seams) |
| c-sharp-engineer | (implements to contract; escalates structure questions to c-sharp-architect) |
| c-sharp-test-engineer | (authors tests; requests seams from c-sharp-engineer/c-sharp-architect) |
| c-sharp-devops-engineer | (implements build/CI; routes code fixes to c-sharp-engineer/c-sharp-architect) |
| c-sharp-reviewer | (read-only; routes required changes to c-sharp-engineer, escalates violations to c-sharp-architect) |
| live-ops-designer | economy-designer (live economy), community-manager (event comms), analytics-engineer (engagement metrics) |
| community-manager | (works with producer for approval, release-manager for patch note timing) |
| military-simulation-architect | simulation-parameter-analyst, gameplay-mechanics-analyst, technical-constraints-analyst, military-research-specialist, simulation-genre-analyst, user-experience-military-analyst |
| requirements-analyst | simulation-parameter-analyst, gameplay-mechanics-analyst, technical-constraints-analyst |

### Escalation Paths

| Situation | Escalate To |
|-----------|------------|
| Two designers disagree on a mechanic | game-designer |
| Game design vs narrative conflict | creative-director |
| Game design vs technical feasibility | producer (facilitates), then creative-director + technical-director |
| Art vs audio tonal conflict | creative-director |
| Code architecture disagreement | technical-director |
| Cross-system code conflict | lead-programmer, then technical-director |
| Schedule conflict between departments | producer |
| Scope exceeds capacity | producer, then creative-director for cuts |
| Quality gate disagreement | qa-lead, then technical-director |
| Performance budget violation | performance-analyst flags, technical-director decides |

## Common Workflow Patterns

### Pattern 1: New Feature (Full Pipeline)

```
1. creative-director  -- Approves feature concept aligns with vision
2. game-designer      -- Creates design document with full spec
3. producer           -- Schedules work, identifies dependencies
4. lead-programmer    -- Designs code architecture, creates interface sketch
5. [specialist-programmer] -- Implements the feature
6. technical-artist   -- Implements visual effects (if needed)
7. writer             -- Creates text content (if needed)
8. sound-designer     -- Creates audio event list (if needed)
9. qa-tester          -- Writes test cases
10. qa-lead           -- Reviews and approves test coverage
11. lead-programmer   -- Code review
12. qa-tester         -- Executes tests
13. producer          -- Marks task complete
```

### Pattern 2: Bug Fix

```
1. qa-tester          -- Files bug report with /bug-report
2. qa-lead            -- Triages severity and priority
3. producer           -- Assigns to sprint (if not S1)
4. lead-programmer    -- Identifies root cause, assigns to programmer
5. [specialist-programmer] -- Fixes the bug
6. lead-programmer    -- Code review
7. qa-tester          -- Verifies fix and runs regression
8. qa-lead            -- Closes bug
```

### Pattern 3: Balance Adjustment

```
1. analytics-engineer -- Identifies imbalance from data (or player reports)
2. game-designer      -- Evaluates the issue against design intent
3. economy-designer   -- Models the adjustment
4. game-designer      -- Approves the new values
5. [data file update] -- Change configuration values
6. qa-tester          -- Regression test affected systems
7. analytics-engineer -- Monitor post-change metrics
```

### Pattern 4: New Area/Level

```
1. narrative-director -- Defines narrative purpose and beats for the area
2. world-builder      -- Creates lore and environmental context
3. level-designer     -- Designs layout, encounters, pacing
4. game-designer      -- Reviews mechanical design of encounters
5. art-director       -- Defines visual direction for the area
6. audio-director     -- Defines audio direction for the area
7. [implementation by relevant programmers and artists]
8. writer             -- Creates area-specific text content
9. qa-tester          -- Tests the complete area
```

### Pattern 5: Sprint Cycle

```
1. producer           -- Plans sprint with /sprint-plan new
2. [All agents]       -- Execute assigned tasks
3. producer           -- Daily status with /sprint-plan status
4. qa-lead            -- Continuous testing during sprint
5. lead-programmer    -- Continuous code review during sprint
6. producer           -- Sprint retrospective with post-sprint hook
7. producer           -- Plans next sprint incorporating learnings
```

### Pattern 6: Milestone Checkpoint

```
1. producer           -- Runs /milestone-review
2. creative-director  -- Reviews creative progress
3. technical-director -- Reviews technical health
4. qa-lead            -- Reviews quality metrics
5. producer           -- Facilitates go/no-go discussion
6. [All directors]    -- Agree on scope adjustments if needed
7. producer           -- Documents decisions and updates plans
```

### Pattern 7: Release Pipeline

```text
1. producer             -- Declares release candidate, confirms milestone criteria met
2. release-manager      -- Cuts release branch, generates /release-checklist
3. qa-lead              -- Runs full regression, signs off on quality
4. localization-lead    -- Verifies all strings translated, text fitting passes
5. performance-analyst  -- Confirms performance benchmarks within targets
6. devops-engineer      -- Builds release artifacts, runs deployment pipeline
7. release-manager      -- Generates /changelog, tags release, creates release notes
8. technical-director   -- Final sign-off on major releases
9. release-manager      -- Deploys and monitors for 48 hours
10. producer            -- Marks release complete
```

### Pattern 8: Concept Prototype (early — before GDDs)

```text
1. game-designer        -- Defines the hypothesis and success criteria
2. prototyper           -- Scaffolds concept prototype with /prototype
3. prototyper           -- Builds minimal implementation (1-3 days)
4. game-designer        -- Evaluates prototype against criteria
5. prototyper           -- Documents findings in REPORT.md
6. creative-director    -- PROCEED / PIVOT / KILL decision (full mode only)
7. game-designer        -- Informs GDD writing with prototype learnings if PROCEED
```

### Pattern 8b: Vertical Slice (pre-production — after GDDs and architecture)

```text
1. game-designer        -- Confirms slice scope against GDDs
2. prototyper           -- Builds production-quality end-to-end build with /vertical-slice
3. prototyper           -- Conducts internal playtest sessions (minimum 1)
4. prototyper           -- Documents findings in REPORT.md
5. creative-director    -- Go/no-go decision on proceeding to Production (full mode)
6. producer             -- Schedules Production epics/sprints if PROCEED
```

### Pattern 9: Live Event / Season Launch

```text
1. live-ops-designer     -- Designs event/season content, rewards, schedule
2. game-designer         -- Validates gameplay mechanics for event
3. economy-designer      -- Balances event economy and reward values
4. narrative-director    -- Provides seasonal narrative theme
5. writer                -- Creates event descriptions and lore
6. producer              -- Schedules implementation work
7. [implementation by relevant programmers]
8. qa-lead               -- Test event flow end-to-end
9. community-manager     -- Drafts event announcement and patch notes
10. release-manager      -- Deploys event content
11. analytics-engineer   -- Monitors event participation and metrics
12. live-ops-designer    -- Post-event analysis and learnings
```

## Cross-Domain Communication Protocols

### Design Change Notification

When a design document changes, the game-designer must notify:
- lead-programmer (implementation impact)
- qa-lead (test plan update needed)
- producer (schedule impact assessment)
- Relevant specialist agents depending on the change

### Architecture Change Notification

When an ADR is created or modified, the technical-director must notify:
- lead-programmer (code changes needed)
- All affected specialist programmers
- qa-lead (testing strategy may change)
- producer (schedule impact)

### Asset Standard Change Notification

When the art bible or asset standards change, the art-director must notify:
- technical-artist (pipeline changes)
- All content creators working with affected assets
- devops-engineer (if build pipeline is affected)

**S39–S48 program wave tracking (10-sprint agent program — additive, backward compat):**

- **Program shape:** Serial sprints S39→S48; parallelism **inside** each sprint only (4–6 tracks). Track A (S39–S41) in-boundary Polish; scope-expansion gate after S41; Track B (S42–S48) Release Enablement — **planning artifacts exist; dispatch blocked until gate**.
- **Wave tracking:** Each sprint uses canonical kickoff sections (header, goal, wave plan table, track ownership + stack prefixes `stack/sprintNN/<slug>`, file ownership matrix, local vs cloud column, hard gates, dispatch order). See `production/agentic/sprint-39-parallel-kickoff-2026-06-20.md` through `sprint-48-*` and `production/agentic/local-cloud-agent-routing.md`.
- **Worktrees:** One worktree per track under `/home/username01/cmano-clone/.worktrees/` — manifest at `production/agentic/s39-s48-worktree-manifest.md`. Bootstrap via `git worktree add`; cloud agents use same branch, fresh VM checkout.
- **Local vs cloud:** Editor PNG evidence + coordinator merge = **local only**; code/test/hygiene/docs/perf/replay = cloud-eligible. Catalog cluster (S40–S42): **single agent per symbol cluster**, local lead preferred.
- **Status sync:** Per-track independent `production/sprint-status.yaml` keys + `production/session-state/active.md` appends; coordinator merges at closeout only.
- **Scope gate:** S41 produces scope packet; human fills `production/gate-checks/scope-expansion-decision-template-2026-06-20.md` → signed record before S42 bootstrap.
- **Roadmap decomposition:** `docs/reports/future-sprint-roadpmap.md` §9 maps B1–B6 epics to S42–S48 sprint numbers.
- **Backward compat:** S36–S39 patterns unchanged; this block extends coordination for the full release train only.

## Anti-Patterns to Avoid

1. **Bypassing the hierarchy**: A specialist agent should never make decisions
   that belong to their lead without consultation.
2. **Cross-domain implementation**: An agent should never modify files outside
   their designated area without explicit delegation from the relevant owner.
3. **Shadow decisions**: All decisions must be documented. Verbal agreements
   without written records lead to contradictions.
4. **Monolithic tasks**: Every task assigned to an agent should be completable
   in 1-3 days. If it is larger, it must be broken down first.
5. **Assumption-based implementation**: If a spec is ambiguous, the implementer
   must ask the specifier rather than guessing. Wrong guesses are more expensive
   than a question.
