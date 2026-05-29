# Agent Delegation Framework — Design Spec

**Date:** 2026-05-28
**Project:** Project Aegis (cmano-clone)
**Status:** Approved design — ready for implementation planning
**Source requirement:** `Game-Requirements/requirements/04-Agent-Delegation.md`
**Related requirements:** 02 (Core Gameplay Loop), 03 (Simulation Modes), 07 (Agentic Infrastructure), 08 (Agentic Architecture)

---

## 1. Overview

This document specifies the **design blueprint** for Project Aegis's agent delegation
framework — the layer that lets players assign specialized AI agents to units and groups,
with human-like behavior and full player oversight.

It is a **conceptual + architectural blueprint only**. No runnable code is produced here,
and the underlying simulation core, ECS entity model, and AI decision engine do not yet
exist. This spec deliberately defines the framework as a layer that sits **above** the
(future) sim/ECS and **below** the player UI, and it defines the **contracts/seams** those
neighboring systems must satisfy.

### Scope decisions locked during brainstorming

| Decision | Choice |
|----------|--------|
| Output | Design blueprint/spec, no code |
| Delegation levels | **Unit + Group/Task Force** first-class; Side + System are future extensions |
| Attention/bandwidth | **Core mechanic** — overloaded agents degrade |
| Override semantics | **Detach-and-rejoin** default; "stay-and-suggest" is a future per-group option |
| Variability model | **Traits + stochastic weighted-choice + attention-load**, fully seeded/deterministic |
| Trust/experience | **Hook/seam only** — system designed later |

### Architectural approach

**Controller / Possession Layer** (chosen over a Chain-of-Command Tree and a
Blackboard/Policy-Module model). The order-propagation-down-a-group idea is borrowed from
the chain-of-command model for the group case; the blackboard model is treated as an
internal implementation detail *inside* an agent controller, not a framework-level concept.

Rationale: it makes seamless Human / Mixed / Agent-vs-Agent mode switching (doc 03) fall out
for free, keeps determinism natural, gives a clean override model, and has the smallest
conceptual surface to specify before the sim exists.

---

## 2. Core Architecture & Concepts

Four core concepts.

### 2.1 Commandable Target
Any entity the framework can direct. In v1:
- a **Unit** — single aircraft, ship, submarine, or drone; or
- a **Group** — task force, squadron, or swarm; an explicit collection of member targets.

Each target owns exactly **one Controller slot**.

### 2.2 Controller (the heart of the model)
A controller decides what its target does. Two kinds:
- **`HumanController`** — orders come from player input.
- **`AgentController`** — orders come from an AI policy (personality + traits).

Both emit the **same `Order` objects** through one shared **Order API**. The sim only ever
consumes orders; it never knows whether a human or an agent produced them.

**Consequence:** a *simulation mode* is just a configuration of which controllers are
attached to which targets — Human, Mixed, and Agent-vs-Agent modes require no special-casing
(satisfies doc 03).

### 2.3 Order / Intent
The common command vocabulary (e.g., *move to*, *engage*, *set EW posture*, *hold*, *RTB*).
Controllers produce intents; the sim executes them. Orders are timestamped and logged
(feeds §6).

### 2.4 Policy
The pluggable "brain" inside an `AgentController` (behavior tree / utility AI / NN, per
doc 08). The framework treats policy as a black box with one contract:

> given perceived state + traits + seed, return ordered/scored intents within an attention budget.

The decision-engine integration is doc 08's responsibility. This spec defines the **seam**,
not the AI.

### 2.5 Core invariant
Controllers are **pure functions of (observed state, traits, seed)** → deterministic and
replayable, satisfying doc 03's reproducibility requirement. There is no wall-clock timing in
controller logic.

---

## 3. Attention / Bandwidth Model

The core mechanic that makes delegation a trade-off rather than a free win.

### 3.1 The budget
Every `AgentController` has an **attention budget** — an abstract capacity for how much it
can effectively track and decide on per decision cycle. A unit-agent has a small budget; a
group-agent's budget is **shared across all members and tasks**.

### 3.2 What consumes attention (load)
Each thing the agent must care about draws load:
- tracked contacts / threats
- active engagements
- member units under command (group-agents)
- pending high-risk decisions

Load scales with situation intensity: a quiet patrol costs little; a saturation missile
attack costs a lot.

### 3.3 Overload behavior (load > budget)
The agent degrades gracefully and believably, in priority order:
1. **Slower reactions** — decision cadence stretches (reacts every N sim-seconds instead of every tick).
2. **Narrowed focus** — lowest-priority targets/tasks drop out of consideration (missed threats, ignored opportunities).
3. **Simpler decisions** — falls back to cheaper heuristics instead of full evaluation.

### 3.4 Gameplay purpose
A player cannot dump an entire side onto one super-agent — command must be distributed
sensibly. More agents = more total attention but more coordination overhead/cost. This is the
strategic heart of delegation (docs 02/04 "theater commander").

### 3.5 Determinism & tuning
Budget, load scoring, and degradation thresholds are deterministic functions of observed
state — no wall-clock timing. Budget size and per-item load costs are agent/personality
parameters exposed to the Balance Tuning Agent (doc 07).

---

## 4. Variability & Personalities

Delivers doc 04's "human-like, imperfect, personality-differentiated" agents via
**traits + stochastic + attention-load**, kept fully deterministic.

### 4.1 Trait vector (per agent)
A small set of tunable continuous parameters defining tendencies, e.g.:
- `aggression` — offensive vs. preservational bias
- `risk_tolerance` — willingness to commit under uncertainty
- `reaction_delay` — baseline lag before acting
- `error_rate` — chance of a suboptimal pick
- `situational_awareness` — how fast/accurately perceived state decays or sharpens
- `decisiveness` — tendency to hesitate/re-evaluate vs. commit

### 4.2 Personalities = trait presets + policy bias
The six personalities from doc 04 (Aggressive, Defensive, Cautious, Opportunistic, Swarm
Coordinator, EW Specialist) are **data-driven trait presets** layered over the pluggable
policy — *data, not hardcoded classes*. New personalities can be authored without code
(supports doc 04's modding / Claude-authoring goal).

### 4.3 Decision process (the imperfection engine)
1. Policy generates candidate intents, each scored.
2. **Attention load** modulates: under overload, fewer candidates are considered and scores are noisier.
3. **Stochastic choice** — sample from the top-weighted options via a soft-max tuned by `decisiveness`/`error_rate` (occasionally yields a believable mistake) rather than always taking the max.
4. **Reaction delay / hesitation** gates when the chosen intent is actually issued.

### 4.4 Determinism
All randomness comes from a **seeded per-agent RNG stream** derived from the global sim seed.
Same seed + same inputs → identical behavior, so Mixed and Agent-vs-Agent replays are
bit-reproducible (doc 03).

### 4.5 Tuning seam
The full trait vector and the soft-max temperature are exposed for the Balance Tuning Agent
and for Claude/Cursor real-time tuning (docs 04/07).

---

## 5. Override, Autonomy Levels & Group/Detach Semantics

### 5.1 Autonomy levels (per-controller state)
1. **Manual** — agent only *suggests* intents; nothing executes without player approval.
2. **Assisted** — agent auto-executes low-risk intents; queues high-risk ones for approval.
3. **Semi-Autonomous** — agent acts freely; player can override instantly.
4. **Full Autonomous** — agent acts with minimal oversight; can be recalled.

Risk classification of an intent (what counts as "high-risk") is a policy/ROE input, tunable
per scenario.

### 5.2 Override = controller swap
Taking direct control attaches a `HumanController` to the target and **suspends** its
`AgentController`. The suspended agent **retains its tasking state** so it can resume exactly
where it left off ("pause agent" / "resume agent", doc 04). Releasing swaps the
`AgentController` back in.

### 5.3 Group detach-and-rejoin
When the player overrides a unit inside an agent-commanded group:
- the unit **detaches** from the group's control envelope;
- the group-agent **re-plans around its absence** and **reclaims the attention** that member
  was consuming (ties into §3);
- the detached unit is **excluded from the group-agent's orders** while player-held; player
  orders are authoritative with no conflict;
- on release, the unit **rejoins** and the group-agent re-integrates it on its next decision
  cycle.

*Future per-group option (noted, not designed):* "stay-and-suggest", where the unit remains
in-group and the agent keeps advising.

### 5.4 Rules of engagement & constraints
Every controller — human-suggested or agent — is filtered through scenario ROE / player-defined
constraints before an order reaches the sim (doc 04 NFR). Agents cannot violate ROE; the
framework rejects or queues such intents.

### 5.5 Conflict rule
At any instant a target has exactly **one** active controller, so order conflicts are
structurally impossible (no "two bosses" problem).

---

## 6. Feedback, Decision Logging & Explainability

### 6.1 The Decision Record (core artifact)
Every time an agent issues (or suggests) an intent, it emits a structured record:
- `sim_time`, `agent_id`, `target_id`, `autonomy_level`
- `chosen_intent` + the **alternatives considered** and their scores
- `rationale` — the dominant factors (e.g., "engaged: high-value contact within range, risk acceptable per aggression trait")
- `attention_state` — load vs. budget at decision time, and whether degradation was active
- `rng_draw` — the seeded value used (makes the stochastic pick auditable/reproducible)

### 6.2 Two consumption surfaces
1. **Live feedback (player-facing):** concise notifications + an optional per-agent "comms
   log" / agent-voice line for immersion (doc 04), summarized from the rationale field.
2. **Post-game stream (analysis):** the full ordered Decision Record stream is the substrate
   the **AAR Agent** (doc 07) reads for kill chains, decision heatmaps, and agent-performance
   metrics. The framework guarantees the stream exists and is complete.

### 6.3 "Why did it do that?" on demand
Because alternatives + scores + rationale are captured per decision, the UI can answer this
for any past or live decision without re-simulating.

### 6.4 Cost control
Records are lightweight structs appended to a per-run log — **not** chat-LLM calls. At 1000x
headless this must stay cheap, so verbose natural-language rationale is generated
**lazily/on-demand** from the structured fields, not eagerly during the sim.

### 6.5 Determinism tie-in
Logging `rng_draw` + inputs means any decision can be independently re-derived — also the
mechanism for debugging agent behavior divergence.

---

## 7. Scope Boundaries

### In scope (this blueprint)
- Controller/Possession model (Human + Agent controllers, shared Order API)
- Unit + Group/Task Force delegation
- Attention/bandwidth mechanic with graceful degradation
- Trait + stochastic + attention-load variability, seeded/deterministic
- Six personalities as data-driven trait presets
- Four autonomy levels + override (controller swap) + group detach-and-rejoin
- ROE/constraint filtering
- Decision Record logging + live feedback + AAR stream

### Out of scope (future extensions)
- **System-level** and **Side-level** delegation (Side = the chain-of-command extension)
- **Trust/experience** system — *hook only* (see §8)
- "Stay-and-suggest" group override option
- Multi-agent sub-coordination (an agent commanding subordinate agents)
- The actual AI policy internals (doc 08's decision engine)
- The assignment **UI** (its own future spec)

---

## 8. Integration Seams (contracts for neighboring systems)

| Seam | Contract | Consumer / builder |
|------|----------|--------------------|
| **Sim/ECS** | The `Order`/intent API the sim consumes + the observed-state snapshot controllers read | Sim core (doc 08), built to this contract |
| **Policy** | `decide(observed_state, traits, attention_budget, seed) -> scored intents` | Decision engine (doc 08) plugs in here |
| **Trust/XP** | A per-agent mutable `experience` blob + a post-engagement performance signal the framework **emits but does not consume** yet | Future campaign/trust system |
| **Tuning** | Trait vectors, attention params, soft-max temperature exposed for adjustment | Balance Tuning Agent / Unity-MCP (docs 04/07) |
| **Logging** | The Decision Record stream | AAR Agent (doc 07) |

---

## 9. Open Questions (resolve during planning — non-blocking)

1. Exact attention-load scoring formula and degradation thresholds — needs tuning once a sim exists.
2. Risk-classification rules for the Assisted autonomy level (what auto-executes vs. asks).
3. Whether group re-planning on detach is immediate or next-cycle (leaning **next-cycle** for determinism).
4. Granularity of the observed-state snapshot: full vs. agent-filtered "perceived" state — interacts with `situational_awareness`.

---

## 10. Requirement Traceability

| Requirement (doc 04 and related) | Addressed in |
|----------------------------------|--------------|
| Unit + Group/Task Force delegation levels | §2.1, §5.3 |
| Agent personalities (6 initial) | §4.2 |
| Autonomy levels (Manual → Full Autonomous) | §5.1 |
| Override & instant intervention; pause/resume agent | §5.2 |
| Conflicting-orders handling on override | §5.3, §5.5 |
| Realistic human-like variability / imperfection | §4 |
| Attention/bandwidth limit (doc 04 OQ#1) | §3 |
| Trust/experience over a campaign (doc 04 OQ#3) | §8 (hook only) |
| Communication & feedback; explainability | §6 |
| ROE / player-defined constraints | §5.4 |
| Full decision logging for replay/analysis | §6 |
| Seamless mode switching (doc 03) | §2.2 |
| Determinism / reproducibility (doc 03) | §2.5, §3.5, §4.4, §6.5 |
| Pluggable decision system (doc 08) | §2.4, §8 |
| Balance/tuning via MCP (docs 04/07) | §3.5, §4.5, §8 |
