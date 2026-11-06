# Gameplay Improvements Implementation Plan

> For agentic workers: use superpowers:subagent-driven-development. User approved implementation of the seven recommendations on 2026-10-31. Do not commit or publish.

**Goal:** Deliver a deterministic, playable Baltic escort mission with consistent timing, fair scoring, actionable feedback, meaningful delegation, configurable interruptions, and a replayable debrief.

**Architecture:** Authoritative mission state and rules are headless C#. An engine-free escort session connects those rules to existing delegation, command queues, combat resolver, and snapshots. Unity hosts bind presentation and forward commands. Respect ADR-010, ADR-007, ADR-001. Existing Baltic v2 fixtures and DelegationBridge hotpath remain unchanged.

**Tech Stack:** Existing .NET 8 / netstandard2.1 C#, xUnit, Unity 6.3, UI Toolkit. No new dependencies.

**Spec:** Approved recommendations in this chat; implementation contract below.

## Global constraints
- Preserve replay hash 17144800277401907079, existing fixture behavior and all baseline tests.
- No DelegationBridge.cs edits, production DB writes, commits, or publishing.
- Values for the new escort mission come from versioned JSON, loaded and validated with embedded fallback for player builds.
- Default player mission starts in planning; smoke tooling stays available.
- Public commands and mission state must be testable without Unity.

## Review focus
- At different render cadences and acceleration, positions, simulation clocks and mission outcome agree.
- Pausing during a frame interrupts subsequent steps; resume never consumes time spent paused.
- Correctly withheld fire has no mission-score penalty; wrong-side diagnostic counts do not alter the player's mission result.
- Contact uncertainty and ROE are respected by the new mission policy; limited rounds cannot be spent indefinitely.
- Mission terminal state is stable, clean restart uses the same seed, and debrief only reports facts visible at each moment.

## Implementation tracks

### 1. Fixed-step scheduling
- [x] Add engine-free FixedStepFrameRunner (fixed delta, bounded per-frame work, retained running backlog, no paused backlog).
- [x] Add SimulationSession.ExternallyScheduledSteps, default false, to avoid applying acceleration twice in externally scheduled play.
- [x] Add optional SimulationSession.ResolveEngagementRangeMeters shooter callback. When supplied, use validated live range in context before resolver; legacy default is unchanged.
- [x] Update smoke host timing to the runner, check pause between steps and opt into external scheduling; keep synthetic seeding explicit to smoke only.
- [x] Test frame cadence, acceleration, invalid inputs, pause interrupt/resume and legacy session behavior.

### 2. Player guidance and interruption preferences
- [x] Extend EngageNextActionProjection with track acquisition, emissions, repositioning, communications and repair advice, plus plain player copy. Preserve existing codes.
- [x] Expose new advisory copy in EngageExplain projection/panel without automatically issuing fire.
- [x] Add watch interruption preferences with legacy defaults, allow critical-only and manual modes, and group related contact cards in presentation.
- [x] Test guidance mappings, unknown reasons, preference changes, and alert grouping.

### 3. Baltic escort mission core
- [x] Add data-driven validated mission definition (duration, distances, unit speeds, rounds, radar/track windows, scoring).
- [x] Add authoritative mission runtime and engine-free EscortMissionSession owning a fresh DelegationBridge, snapshot, order sink, live ranges and real combat results. No smoke seed events.
- [x] Expose Bridge, Snapshot, SimTime, IsComplete, Status, Briefing, Guidance, Debrief and objective score as read-only DTOs; use commands for begin/pause/resume, radar, delegation posture, hold/proceed, restart.
- [x] Route unit orders through existing command/enqueue paths. Use a new contextual escort policy, not PatrolCandidateEngagePolicy fixture changes.
- [x] Success means convoy reaches exit alive within deadline; convoy destruction / deadline means failure. Score objectives, preservation and expenditure; denials remain diagnostics.
- [x] Show contextual onboarding, three fact-based decisive moments, same-seed restart. Define completion as mission outcome, not merely running the scenario.
- [x] Test successful escort, no-action loss/deadline, radar/track and reserve choices, deterministic restart, terminal stability, actual command-to-outcome effects and legacy replay preservation.

### 4. Unity mission integration
- [x] Add thin EscortMissionHost and mission UI binding the session DTOs, commands, briefing, live objectives, posture/EMCON tradeoffs, alerts and debrief/restart.
- [x] Add explicit external-session binding to DelegationBridgeHost; refresh existing C2 projections through RunTick without touching DelegationBridge.
- [x] Add a reproducible Editor menu / batch builder for BalticEscort scene and player build entry point. Do not hand edit serialized scenes.
- [x] Refresh netstandard2.1 plugin assemblies and validate C# against installed Unity assemblies. Editor execution was attempted and rejected for missing license; Play Mode/import/visual checks remain blocked and explicitly reported.

### 5. Verification and handoff
- [x] Review independent tracks, integrate, and resolve issues.
- [x] Run solution build, complete solution tests, headless PlayModeSmokeHarnessTests, replay goldens, diff checks, hash/zero-touch checks and architecture finish checklist.
- [x] Document launch instructions, exact implementation coverage, tests and remaining live playtest work. Do not claim live gameplay validation from headless tests.

## Execution notes
- User approval already covers implementation; no additional approval checkpoint is needed for reversible local edits.
- Parallel work uses disjoint file ownership in the current clean checkout. Each worker must preserve concurrent changes.
- Campaign attrition was a later extension in the recommendations; this delivery provides the complete escort experience and evidence-based replay loop first, with no unrequested global campaign redesign.

## Final review rulings

- New player mission scoring replaces the old kill/denial tally only in the new player path; legacy fixtures keep their replay-compatible behavior.
- The licensed Editor was unavailable (exit198), so scene source was prepared with valid existing serialization and GUID references; canonical Editor builder remains available for regeneration. No asset import or rendered validation is claimed.
- Reviews corrected identity persistence, command-before-motion semantics, observation-before-release ordering, player knowledge filtering, grouped alert publication, exit placement, marker caption overlap, and post-kill command feedback.
- Unity-specific final verification is still blocked by license activation; headless implementation and source compilation are complete. See docs/gameplay/gameplay-improvements-verification-2026-10-31.md.
