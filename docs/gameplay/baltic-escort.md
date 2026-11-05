# Baltic Passage: player mission

Open `unity/ProjectAegis` in Unity **6000.3.22f1**, open `Assets/Scenes/BalticEscort.unity`, and press Play. The **Project Aegis → Play Baltic Escort** menu rebuilds and opens the same scene. The existing smoke scene remains separate. The laptop's existing Build Settings were preserved; select BalticEscort in Build Settings when creating a player build.

A valid Unity Editor license and the project's pinned Editor version are required to import and run the scene. On DRG-Flo-Laptop, Unity 6000.3.22f1 must be installed before the intended visual playthrough.

## First mission

You command an escort protecting a civilian convoy through a five-nautical-mile passage. The convoy needs about **16 minutes 40 seconds of simulation time** to transit at 18 knots; the deadline is 20 simulation minutes. Identify the surface contact, protect the convoy, and reach the exit alive.

1. Read the briefing while the mission is paused in Planning.
2. Choose manual control or an officer's reserve policy. Balanced is a reasonable first plan.
3. Choose emissions posture. Active radar identifies quickly but contributes to hostile acquisition. Passive observation takes longer.
4. Select **Begin execution**. With All contacts interruptions enabled, the first detection pauses the mission. Review the contact, acknowledge the alert, and select Resume.
5. Observe your officer's decisions or issue an Engage order under Manual control after identification and fire control permit it.
6. Keep the convoy moving. Increase time acceleration during transit; auto-pause interrupts the current frame when attention is required.
7. Review the mission outcome, rounds expended and three decisive moments. Restart uses fresh state and the same seed.

## Controls and consequences

| Control | Behavior |
| --- | --- |
| Manual | Direct weapon orders enter the existing player order queue. |
| Cautious / Balanced / Assertive | Officers use identified tracks, live range and the selected ammunition reserve. |
| Radar active / Investigate | Accelerates identification and increases cumulative emissions exposure; does not authorize a shot. |
| Radar passive | Reduces further emissions exposure. Known identity persists while fire-control freshness expires independently. |
| Hold convoy / Resume convoy | Queued convoy movement orders; the escort, threat and deadline continue while the convoy holds. |
| All contacts / Critical only / Manual pause | Controls tactical auto-pause. Pending approvals remain approval-gated. |
| Acknowledge alerts | Clears visible attention cards; resume is a separate action. |
| Focus contact | Highlights a detected unknown or identified hostile contact without changing mission state. |
| Slower / Faster | 1–256× simulated time; elapsed render time is accumulated into fixed steps. |
| Restart | New world, log, magazines, controller RNG, clock and attention state with the original seed. |

Score rewards completing the escort objective and preserving friendly units, deducts actual ammunition expenditure and unauthorized releases, and does **not** penalize refused orders. Policy denials remain available to diagnostic tooling. The legacy kill/denial score is retained only for existing replay/smoke compatibility.

The player map omits undetected opposing units. Opponent sensor state and hidden attacker identity are not included in the player debrief. Friendly and hostile fire resolve through the existing combat resolver and authoritative magazine ledger; demonstration damage and timed smoke events are not used.

## Implementation and tuning

- `data/scenarios/baltic-escort.mission.json` owns briefing, timing, speeds, geometry, ranges, observation windows, probabilities, ammunition reserves and scoring values. It is validated and embedded in the Delegation assembly, so player builds do not need a repository data directory.
- `src/ProjectAegis.Delegation/Gameplay/` owns the mission definition, authoritative world rules, policy and immutable presentation contracts.
- `src/ProjectAegis.Delegation.UnityAdapter/Gameplay/EscortMissionSession.cs` connects rules, player commands, actual combat and read-only snapshots.
- `FixedStepFrameRunner` applies acceleration once. `SimulationSession.ExternallyScheduledSteps` is opt-in; old replay fixtures retain their behavior.
- Unity's `EscortMissionHost` is the composition/scheduling root. `EscortMissionPanelHost` binds immutable views and submits validated mission commands.
- `BalticEscortSceneBuilder` is the canonical Editor scene generator. Scene source and metadata are also included for direct opening after import.
- Refresh local plugin DLLs after changing headless code: `tools/copy-delegation-assemblies.ps1` publishes **netstandard2.1**, never net8.0, into `Assets/Plugins/ProjectAegis`.

Architecture follows ADR-010, ADR-007 and ADR-001. There are no `DelegationBridge.cs` hotpath edits, new packages, production database writes or modifications to the legacy smoke scene.

## Validation limits

The headless suite covers frame-rate/acceleration equivalence, pause interruption and resume, real success/failure outcomes, limited magazines and reserves, command queue ordering, contact knowledge and identity, same-seed restart, mission score incentives, grouped alerts and complete default-data runs across seeds 1, 42, 123 and 789.

Runtime scripts, scene builders and new Play Mode test sources have additionally been compiled against the installed Unity 6.3 assemblies for netstandard2.1 API compatibility. This is **not** Unity asset-import, UXML-layout or Play Mode proof. The two new Unity lifecycle tests are ready to run after license activation. Visual layout, live input feel and human pacing still need a licensed Editor and actual player observation.

Campaign attrition and multi-mission branching remain a later extension. This delivery implements the complete escort mission and same-seed learning loop without changing the campaign document schema.
