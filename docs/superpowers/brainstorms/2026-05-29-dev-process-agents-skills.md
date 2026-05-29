# Brainstorm — Additional Agents & Skills for the Development Process

**Date:** 2026-05-29
**Project:** Project Aegis (cmano-clone) — a deterministic Command: Modern Air/Naval Operations–style wargame
**Input:** `Tech-Stack.md`
**Method note:** GitNexus MCP tools were **not reachable** in this session (no MCP server
connected; `gitnexus` CLI not installed and offline). I substituted direct inspection of
`Tech-Stack.md`, the 39-agent roster (`.claude/docs/agent-roster.md`), the 73 skills in
`.claude/skills/`, the delegation source namespaces (`src/ProjectAegis.Delegation/*`), the
requirement set (`Game-Requirements/requirements/01–10`), and the delegation design spec.
Re-run this review with `npx gitnexus analyze` + `gitnexus_query` once the index is live to
confirm the code-level gaps below.

---

## 1. What makes this project unusual (and why the stock template under-serves it)

The Claude-Code-Game-Studios template (39 agents, 73 skills) is tuned for a **general indie
game**. Project Aegis is not that. Per the Tech-Stack and requirements, it is:

- **A deterministic, seeded, replayable military simulation** (req 03) — not a real-time
  action game. Reproducibility is a *correctness* property, not a nicety.
- **An agentic-delegation wargame** (req 04, 05, 08) — the headline feature is *in-game AI
  agents* assigned to units/groups, with attention/bandwidth, traits, RoE, and trust seams.
- **Data-driven at CMANO scale** (req 06) — huge platform/sensor/weapon/loadout databases
  are the content, and "database intelligence" is its own requirement.
- **Unity DOTS/ECS + C#** with a **live Unity-MCP editor bridge** and a **Cursor-first**
  agentic workflow (Tech-Stack).
- **Simulation-heavy performance** — thousands of entities, sensor/EW resolution, ballistics.

The stock roster has no agent that owns *determinism*, *simulation domain modeling*, *the
military/doctrine domain*, *scenario authoring*, *the in-game agent-AI design*, or the
*data-pipeline for the equipment DB*. Those are the gaps below.

---

## 2. Proposed NEW AGENTS

Format: `name` · tier · model · domain · why this project needs it · owned dirs.

### P0 — fills a hole the project will hit immediately

| Agent | Tier | Model | Why Project Aegis needs it |
|-------|------|-------|----------------------------|
| `simulation-architect` | Lead | opus | Owns the ECS/DOTS entity model, the fixed-timestep tick, and the sim/controller/UI seam the delegation spec keeps referring to. No current agent owns "the sim core." Sits between `technical-director` and the engine specialists. Dirs: `src/**/Sim/`, `src/**/Core/`, ECS systems. |
| `determinism-engineer` | Specialist | sonnet | Guards the spec's **core invariant**: controllers are pure functions of (state, traits, seed). Hunts wall-clock reads, unordered collection iteration, float non-determinism, and unseeded RNG — the things that silently break replay. Nothing in the stock roster does this. |
| `wargame-domain-expert` | Specialist | sonnet | The military/doctrine SME: sensors (radar/sonar/ESM), EW, weapons envelopes, RoE doctrine, detection math, datalinks. Validates that mechanics are *plausible*, the way `world-builder` validates lore. Pairs with `systems-designer` on formulas. Dirs: `design/gdd/`, `src/**/Roe/`, `src/**/Targets/`. |
| `agent-ai-designer` | Specialist | sonnet | Designs the **in-game** delegation AI: policies, traits, attention budgets, stochastic weighted-choice, trust/experience. Distinct from `ai-programmer` (who implements) and `game-designer` (who owns the player loop). Owns `src/**/{Policy,Decision,Traits,Attention,Trust}/` at the design level. |

### P1 — strongly valuable given the stack

| Agent | Tier | Model | Why |
|-------|------|-------|-----|
| `scenario-designer` | Specialist | sonnet | CMANO-style games live or die on scenarios. Authors mission/scenario layouts, victory conditions, OOBs, triggers, scripted events. The stock `level-designer` thinks in spatial levels, not order-of-battle scenarios. |
| `data-pipeline-engineer` | Specialist | sonnet | Owns the platform/sensor/weapon database (req 06): schema, import/validation, versioning, the ScriptableObject/Addressables pipeline. Distinct from `tools-programmer`. |
| `unity-mcp-operator` | Specialist | sonnet | Drives the live Unity Editor over Unity-MCP (`localhost:8080`, 100+ tools): scene setup, runtime inspection, play-mode verification. Turns "verify with screenshots" (coding-standards) into a real capability once the Editor is up. |
| `replay-qa-specialist` | Specialist | haiku | A QA-tester variant whose entire job is **golden-replay regression**: run a seeded scenario, diff the order log / end-state against a recorded baseline, flag divergence. Operationalizes determinism in CI. |

### P2 — nice-to-have / later phases

| Agent | Tier | Model | Why |
|-------|------|-------|-----|
| `balance-analyst-mil` | Specialist | sonnet | Wargame balance is asymmetric (red vs blue capability, not loot curves). A domain-aware balance agent beats the generic `economy-designer` here. |
| `prompt-engineer` | Specialist | sonnet | If the shipping game uses LLM-backed agents (req 09/10 "near-future/speculative"), someone must own prompt design, eval harnesses, and cost/latency budgets for runtime model calls. |
| `accessibility-specialist` (already exists) | — | — | Note: keep, but extend brief for dense sim UI (data tables, map symbology, colorblind NATO/APP-6 symbol sets). |

---

## 3. Proposed NEW SKILLS

Format: `/skill` — purpose · model tier · trigger.

### P0 — determinism & simulation (the project's defining risk)

- **`/determinism-audit`** — Scan C# for non-deterministic patterns (DateTime.Now, unordered
  Dictionary/HashSet iteration, `float` accumulation order, unseeded `System.Random`,
  thread races in Jobs). Produces a ranked report. *sonnet.* Trigger: before any sim merge,
  before a release gate.
- **`/replay-verify`** — Run a seeded scenario twice (and against a stored golden) and diff
  the order log + final world-state hash. PASS/FAIL gate. *sonnet.* Trigger: "is replay still
  deterministic", CI sim gate.
- **`/sim-perf-budget`** — Like `/perf-profile` but sim-specific: entity counts, tick budget,
  Burst/Job allocation, sensor-resolution cost at N contacts. *sonnet.* Trigger: scaling tests.

### P0 — the delegation framework itself

- **`/design-agent-policy`** — Guided authoring of an `AgentController` policy: traits,
  attention budget, intent scoring, RoE coupling, seed handling — written against the design
  spec's contracts. *sonnet.* Trigger: new agent behavior/personality.
- **`/team-delegation`** — Orchestration skill (analogous to `/team-combat`): coordinates
  `agent-ai-designer` + `wargame-domain-expert` + `ai-programmer` + `determinism-engineer` +
  `replay-qa-specialist` to take one delegation behavior from design → C# → deterministic
  test, end-to-end. *sonnet.*

### P1 — content & data

- **`/scenario-author`** — Section-by-section scenario spec: OOB, sides, victory conditions,
  triggers, briefing. Produces a scenario doc + data stub. *sonnet.*
- **`/db-validate`** — Validate the equipment database: schema conformance, required fields,
  cross-references (weapon→platform→sensor), unit consistency, duplicate detection. *haiku.*
  Trigger: after any DB edit. (Mirrors `/consistency-check` but for the data layer.)
- **`/db-import`** — Structured import/normalization of a new platform/weapon set into the
  project schema with a validation pass. *sonnet.*

### P1 — Unity-MCP live verification

- **`/unity-verify`** — Use Unity-MCP to load a scene, enter play mode, run a scenario, and
  capture screenshots / runtime state as test evidence (satisfies coding-standards'
  "verify with screenshots"). *sonnet.* Trigger: visual/UI/feel story sign-off.
- **`/unity-scene-scaffold`** — Generate a scene skeleton (cameras, map, HUD anchors,
  controller wiring) via Unity-MCP from a UX/scenario spec. *sonnet.*

### P2 — process & maintenance

- **`/csharp-modernize`** — Sweep for C#/.NET idiom upgrades (nullable refs, records,
  `readonly struct` for ECS data, `Span<T>` on hot paths). *sonnet.*
- **`/sim-trace`** — Given a divergent replay, bisect the order log to the first diverging
  tick/decision and report the responsible controller. A debugging skill. *sonnet.*

---

## 4. Adjustments to EXISTING agents/skills (cheap wins)

- **Configure the engine.** `technical-preferences.md` still says `[TO BE CONFIGURED]`.
  Run `/setup-engine` to pin Unity LTS + C# + DOTS so engine-specialist routing actually
  fires. Several skills (`/dev-story`, `/team-ui`, `/test-setup`) read that file.
- **Wire `csharp-team.yaml` into the roster.** The team manifest added on this branch should
  gain the P0 agents above once they exist (`simulation-architect`, `determinism-engineer`).
- **`/test-setup` should target the existing .NET layout** (`dotnet test ProjectAegis.sln`),
  not the engine-generic Godot/Unity headless runners in coding-standards.

---

## 5. Recommended first batch (if you only do three things)

1. **`determinism-engineer` agent + `/replay-verify` + `/determinism-audit`** — protects the
   single most fragile, most defining property of the whole game. Cheap to add, expensive to
   retrofit after non-determinism creeps in.
2. **`agent-ai-designer` agent + `/team-delegation`** — the headline feature has design
   ownership and an end-to-end pipeline.
3. **`/setup-engine`** — unblocks the engine-aware routing the rest of the template assumes.

---

## 6. Open questions for the user

- **Runtime LLM agents?** Reqs 09/10 hint at "near-future/speculative" tech. If the *shipping*
  game calls models at runtime, we need `prompt-engineer` + eval/cost skills (P1, not P2).
- **Scope of Unity-MCP now?** The Editor session is "pending." Should `/unity-verify` and
  `unity-mcp-operator` be built now (so they're ready) or deferred until the Unity project
  exists?
- **Where should new agents/skills live** — vendored into `.claude/` like the studio set, or
  in a project-specific overlay so template updates don't clobber them?
