# 03 - Simulation Modes

**Last Updated:** 2026-06-04  
**Related:** 01, 02, 04, 13, 17, 19, 20  
**Status:** Ready for design review  
**Locked spec:** [2026-05-30-simulation-modes-decisions-design.md](../../docs/superpowers/specs/2026-05-30-simulation-modes-decisions-design.md)

## Purpose
Define the three core simulation modes that the game must support, enabling seamless transitions between full human control, mixed human+agent play, and fully autonomous agent-versus-agent execution at extreme speeds.

## Vision
A single, unified simulation engine that can operate across the full spectrum of human involvement — from pure manual command to fully autonomous high-speed agent battles — while maintaining perfect determinism, replayability, and performance.

## Supported Simulation Modes

### 1. Human Mode (Classic Command-Style)
- Full manual control of all friendly forces
- No AI agents controlling any units unless explicitly assigned
- Traditional real-time or paused command interface
- Best for players who want complete tactical control
- **Session phase:** starts in **Planning** (RTwP); execution begins on explicit **Begin Execution** (req 02)

### 2. Mixed Mode (Human + Agent)
- Player commands one side, a specific task force, or selected units
- AI agents command the opposing force and/or any delegated friendly units
- Player can dynamically change autonomy levels during play
- Ideal for learning, testing new strategies, or playing against stronger opposition
- **Session phase:** starts in **Planning**; **Begin Execution** required before ticks advance
- **Dual-side control (testing):** off by default; scenario policy `allowDualSideControl: true` permits human control on both sides (TEST SANDBOX banner). See [Resolved Design Decisions](#resolved-design-decisions-may-30-2026).

### 3. Agent vs Agent Mode (Fully Autonomous)
- No human input required after scenario start
- All forces on both sides controlled by specialized AI agents
- Supports extreme time compression; **targets** 1000×+ headless; **requires** ≥256× effective for batch-analysis readiness
- Designed for massive batch runs, balance testing, and research
- **Session phase:** **auto-starts execution** when mode is configured (`SimulationModeConfigurator` calls `BeginExecution()`)
- **Observer attach:** optional read-only replay viewer (`AttachReplayViewer`) — not a separate mode enum

## Functional Requirements

### Mode Switching
- Seamless transition between all three modes at any point in a scenario
- Player can pause, take control of specific units, or hand control back to agents instantly
- When **`AttachReplayViewer`** is enabled on an AvA session, human orders are disabled (replay/scrub UI only)
- State remains fully consistent when switching modes

### Session Phase Integration (req 02)

| Mode | Initial phase | Begin Execution |
|------|---------------|-----------------|
| Human | `Planning` | Player action (RTwP) |
| Mixed | `Planning` | Player action (RTwP) |
| Agent vs Agent | `Executing` | Automatic on mode configure |

- While `Planning`, delegation and sim ticks are no-ops; order log stays empty
- Unity interactive hosts use `DelegationBridgeHost.BeginExecution()`; smoke harness may set `autoBeginOnStart`
- Implementation: `DelegationOrchestrator.Phase`, `SimulationSession`, `DelegationBridge`

### Player Information & Loop Policy (req 02)

- Scenario policy JSON carries `playerInfoModel` and `personalityEditPolicy`
- **Live HUD** uses `GetLiveOrderLogView()` — may filter agent decisions per scenario policy
- **Replay/AAR** always uses full `DecisionLog` (no filtering)
- Personality hot-swap enforced via `TryRebindAgentTraits` and `LoopPolicyGate`

### Performance Targets
- Human Mode: 60+ FPS with up to 5,000 entities
- Mixed Mode: 30+ FPS with up to 8,000 entities
- Agent vs Agent Mode (headless, no rendering):
  - **Minimum (batch analysis ready):** ≥256× effective simulation speed
  - **Target (farm / profile gate):** 1000×+ effective simulation speed

### Determinism & Reproducibility
- All modes must be 100% deterministic when using the same seed
- Full replay support with timeline scrubbing
- Identical results across Human, Mixed, and Agent vs Agent runs when using identical inputs

### Headless Execution
- Agent vs Agent mode must support completely headless runs (no Unity Editor or graphics required)
- Suitable for cloud-based batch simulation farms

## Non-Functional Requirements

- All modes must share the same underlying simulation core
- No loss of fidelity when running in headless mode
- Clear visual indicators of which units are under human vs. agent control
- Logging of all agent decisions for post-game analysis

## Agentic Capabilities

- Claude/Cursor (via Unity-MCP) can:
  - Switch simulation modes programmatically
  - Run thousands of Agent vs Agent simulations in batch
  - Analyze results across different agent personalities and force compositions
  - Generate new scenarios optimized for specific modes

## Technical Considerations

- Built on Unity DOTS + custom deterministic event system
- Time compression handled at the simulation core level
- Separate rendering and simulation threads for maximum headless performance
- State serialization optimized for both interactive and batch use cases
- Phase gate and loop policy: `docs/superpowers/specs/2026-05-30-phase-gate-loop-policy-design.md`

## Inbound References (docs 13, 19, 20)

| Source doc | How modes consume it |
|------------|---------------------|
| [13](13-Doctrine-ROE-EMCON-WRA.md) | Scenario policy JSON (`playerInfoModel`, `personalityEditPolicy`, `allowDualSideControl`) loaded at mode configure |
| [19](19-Cyber-And-Comms.md) | Comms degradation affects live HUD message log; does not change mode enum — applies in all modes during execution |
| [20](20-Command-And-Control-UI.md) | Time compression UI, TEST SANDBOX banner for dual-side control, `AttachReplayViewer` read-only chrome in AvA |

## Open Questions / Decisions Needed

All charter questions for simulation modes are **locked**. See [Resolved Design Decisions](#resolved-design-decisions-may-30-2026) and the [locked spec](../../docs/superpowers/specs/2026-05-30-simulation-modes-decisions-design.md). No reopen without user approval.

## Future Extensibility

- Multiplayer hotseat and asynchronous modes (future release)
- Live co-simulation with external agents (e.g., reinforcement learning models)
- Cloud-based simulation cluster support for massive agent-vs-agent campaigns
- **Spectator Mode** (see open questions) — Agent vs Agent with visualization, no control

## Resolved Design Decisions (May 30, 2026)

Full rationale: `docs/superpowers/specs/2026-05-30-simulation-modes-decisions-design.md`

| Topic | Decision |
|-------|----------|
| Dual-side Mixed control | Scenario-gated via `allowDualSideControl: true` (default **false**) |
| AvA batch speed floor | **256×** headless effective minimum |
| AvA speed target | **1000×+** headless performance target |
| Spectator | **Not** a fourth mode enum — AvA + `AttachReplayViewer` read-only replay UI |

---

**Status:** Core mode architecture approved; Template A complete (Sprint 12). Open questions resolved May 30, 2026.
