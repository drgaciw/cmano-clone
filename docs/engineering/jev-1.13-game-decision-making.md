# Jev 1.13 for Project Aegis game decision making

**Date:** 2026-09-18  
**Status:** recommendation (no implementation)  
**Model:** [TypeSafe Jev 1.13 on OpenRouter](https://openrouter.ai/typesafe/jev-1.13) (`typesafe/jev-1.13` / native `jev-1.13.0`)  
**Related:** [agent-decision-pipeline.md](agent-decision-pipeline.md), [determinism-and-replay.md](determinism-and-replay.md), [autonomy-roe-gating.md](autonomy-roe-gating.md), [hindsight-session-memory-sidecar.md](hindsight-session-memory-sidecar.md), [scenario-ai-authoring-and-adjudication.md](scenario-ai-authoring-and-adjudication.md), [qa-gauntlet.md](qa-gauntlet.md)

## Verdict

Use Jev **as an off-tick judgment sidecar**, not as a replacement for `DecisionPipeline.Choose`.

Jev is a [System One](https://docs.typesafe.ai/concepts/system-one) model: it takes structured state plus typed questions and returns calibrated probabilities (`noul` / `choice` / `score`), not prose. That shape is a good match for Aegis **advisory** work — AAR tagging, pending-approval ranking, gauntlet triage, authoring assist, C2 intent routing — and a **bad** match for the live delegation tick.

The live path already owns the job Jev would try to steal: policy-scored candidates, trait-weighted softmax, seeded RNG, ROE/autonomy gates, and a bit-identical order log. A 70–500 ms network call cannot sit inside that loop without breaking replay goldens and the Baltic v2 hash invariant (`17144800277401907079`).

**Recommended posture:** keep code as the workflow (same pattern as Hindsight: optional, fire-and-forget or post-run, never in `Tick()`). Let Jev answer narrow judgments over already-computed state. Let C# own ranges, counts, ROE, magazines, and the final act.

---

## What Jev 1.13 actually is

From the [OpenRouter listing](https://openrouter.ai/typesafe/jev-1.13) and [TypeSafe launch note](https://typesafe.ai/blog/introducing-system-one-models-and-jev):

| Property | Value |
|----------|--------|
| Class | System One — fast structured decisions, not a chat model |
| Input | Text only: string, JSON object, or array of text. No image / audio / video |
| Output | Typed answers + probabilities. Cannot write prose, code, or explanations |
| OpenRouter slug | `typesafe/jev-1.13` |
| Native pin | `jev-1.13.0` (aliases `jev-latest` / `jev-preview` currently point here) |
| Price | $0.042 / 1M input tokens; output tokens free |
| Context | 32k on OpenRouter; TypeSafe documents 64k per request and 32k for state + longest question |
| Latency (vendor) | ~70–500 ms end-to-end; most calls ~100 ms from US West |
| Rate limits (early access) | 250k tokens/s, 1,200 req/min (can change) |
| Released | 2026-09-18 on OpenRouter; TypeSafe announced Jev 2026-09-15 |
| Training | RLCD (Reinforcement Learning for Calibrated Decisions), not RLHF chat preference |
| Type errors | Schema-constrained: a successful answer cannot invent an option you did not supply |
| Cardinality | Choice ≤ 255 options; Score 2–10 levels |

Three primitives only ([TypeSafe primitives](https://docs.typesafe.ai/primitives)):

| Primitive | Question | Returns | Use when |
|-----------|----------|---------|----------|
| **Noul** | Is this true? | `noul` in `[0, 1]` | Risk / eligibility / “does this look like X?” |
| **Choice** | Which of these? | winning option + full distribution + `confidence` | Closed set you already enumerated in code |
| **Score** | Where on this rubric? | weighted mean + per-level probabilities + `confidence` | One qualitative dimension with situation-described levels |

Questions in one request run **in parallel** against the same state. Adding questions is cheap; serial round-trips are the expensive anti-pattern ([speculative fan-out](https://docs.typesafe.ai/patterns/fan-out)).

Jev does **not** generate text. On OpenRouter that means the Decisions / System One API, not `chat/completions`. Do not wrap it as “an LLM with JSON mode.”

---

## Why it looks like a fit — and why it is not the tick

Aegis already has a closed, typed decision graph:

```text
ObservedState
  → PerceivedState (SA fog)
  → IPolicy.GenerateCandidates → ScoredIntent[]
  → DecisionPipeline.Choose (softmax + SeededRng)
  → AutonomyGate (ROE first, then autonomy)
       ├─ ExecuteNow
       ├─ QueueForApproval
       └─ Rejected (PolicyDenialRecord)
  → DecisionLog / IOrderSink
```

That is already “state in, typed choice out.” Jev’s marketing line — *unstructured state in, typed probabilistic decisions out* — sounds like `DecisionPipeline`. The difference that matters here:

| Existing Aegis path | Jev |
|---------------------|-----|
| Pure C#, in-process, microseconds | HTTP, 70–500 ms, network + vendor |
| Bit-identical given `(scenario, seed)` | Self-consistent, **not** replay-bitwise; sampling noise exists |
| Candidates come from policy + geometry you computed | Can only pick from options **you** send |
| ROE / WRA / magazine / EMCON are hard gates | Common-sense judgment; weak at math, counts, dates |
| Order-log fingerprint is a release invariant | Any influence on `Choose` moves goldens |

So Jev is closer to a **calibrated classifier over an already-legal option set** than to an agent that “plays the game.” TypeSafe’s own Doom demo is explicit about this: the bot is reactive to structured state and “following instructions,” not a stronger-than-scripted player ([launch note](https://typesafe.ai/blog/introducing-system-one-models-and-jev)).

The HighwayEnv experiment is the pattern we should copy: **do not ask Jev “what should I do?”** Ask one Noul per legal action (“will this collide?”) and let **code** pick the action and apply thresholds ([Jev × HighwayEnv](https://dev.to/trknhr/jev-x-highwayenv-60-seconds-without-a-crash-30ig)). In Aegis terms: Jev estimates *judgment*; `DecisionPipeline`, `AutonomyGate`, and `IPolicyEvaluator` still decide *act*.

---

## Hard rules (do not violate)

These are project invariants, not style preferences.

1. **Never call Jev from `DelegationOrchestrator.Tick`, `AgentController.TryDecide`, `DecisionPipeline.Choose`, `AutonomyGate`, `RoePolicyAdapter`, or `MvpEngagementResolver`.** Those paths must stay functions of `(observed state, traits, seed)`.
2. **Never write Jev answers into fingerprinted `DecisionLog` rows** (`AgentDecision`, `PolicyDenial`, combat outcomes). A sidecar annotation store or Hindsight bank is fine; the order-log fingerprint is not.
3. **Never let Jev override ROE, WRA, validation, or gauntlet oracle gates.** Player approval cannot override ROE today; a model must not either. Gauntlet scripts remain authority ([qa-gauntlet.md](qa-gauntlet.md)).
4. **Never put Jev in `ProjectAegis.Data` blocking authoring/export.** v1 AI authoring is a deterministic stub by contract ([scenario-ai-authoring-and-adjudication.md](scenario-ai-authoring-and-adjudication.md)). A model layer is Phase 2/3, propose-only, human-gated.
5. **Do not touch `DelegationBridge` hotpath** to add this. Follow the Hindsight pattern: optional constructor options, null when unset, CI/replay unchanged.
6. **Do not send raw catalogs, proprietary CMO `.db3` extracts, or secrets in `state`.** State is treated as data and is not hardened against adversarial steering ([jev-1.13 jaggedness](https://docs.typesafe.ai/model-jaggedness/jev-1.13)).
7. **Pin `jev-1.13.0` (or `typesafe/jev-1.13`) once thresholds are tuned.** Aliases move; confidence floors tuned on 1.13 will drift if `jev-latest` advances ([TypeSafe models](https://docs.typesafe.ai/models)).

Hindsight already documents the correct sidecar contract: retain-only / fire-and-forget on the sim path; recall and judgment **after** the run ([Hindsight README](../../src/ProjectAegis.Delegation/Hindsight/README.md)).

---

## Recommended placement

```text
                    ┌─────────────────────────────────────┐
   Live tick        │ DecisionPipeline + AutonomyGate     │  NEVER Jev
   (deterministic)  │ IPolicy / IPolicyEvaluator / ROE    │
                    └─────────────────┬───────────────────┘
                                      │ append-only DecisionLog
                                      ▼
                    ┌─────────────────────────────────────┐
   Sidecar          │ Hindsight retain (existing)         │
   (optional)       │ JevIntegration (proposed)           │  AFTER or ASYNC
                    │   state = filtered projection       │
                    │   questions = pinned rubric file    │
                    └─────────────────┬───────────────────┘
                                      │ answers + confidence
                                      ▼
                    C2 advisory · AAR · approval ranking ·
                    gauntlet triage · authoring propose
```

Three integration surfaces, in priority order:

| Priority | Surface | Why first |
|----------|---------|-----------|
| P0 | **Post-run AAR / Hindsight** | State is already logged; no tick risk; high judgment value |
| P1 | **C2 advisory (pending approvals, watch queue)** | Human remains the actuator; Jev only ranks / flags |
| P2 | **Tooling (gauntlet triage, authoring, catalog QA)** | Already LLM-adjacent and explicitly non-blocking |

Do not start with “Jev-controlled agents in Baltic replay.” That is the highest-risk, lowest-leverage experiment.

---

## Concrete Aegis use cases

### 1. After-action judgment (do this first)

**State (filter in code first):** a compact projection of one engagement or one agent window — chosen `OrderKind`, candidate scores, ROE verdict, abort reason, magazine band, attention load, personality preset. Do **not** dump the full order log.

**Questions (one request, fan-out):**

| ID | Type | Instructions (sketch) |
|----|------|------------------------|
| `doctrine_fit` | Score | How well does this decision fit the unit’s effective ROE and mission type? Levels: contradicts doctrine / stretch but legal / textbook |
| `attention_overload_explains` | Noul | Does `attention.load` vs `attention.budget` explain the narrowed-focus rationale? |
| `wasted_salvo` | Noul | Given Winchester/Shotgun band and outcome, was this a wasteful expend? |
| `aar_tag` | Choice | `sound` / `doctrine_stretch` / `attention_failure` / `logistics_forced` / `other` |

**Code then:** write tags into an AAR sidecar or Hindsight `aar-*` bank. The [after-action ledger](after-action-ledger-projection.md) stays a verbatim combat-event projection; Jev annotations sit beside it, never rewrite facts.

### 2. Pending-approval ranking (Assisted / Manual)

`PendingApprovalQueue` already holds agent-decided High-risk `Engage` orders until a human clicks approve ([pending-approval-queue.md](pending-approval-queue.md)). Replay never auto-approves.

**Jev’s job:** score the queue for the commander, not approve it.

| ID | Type | Purpose |
|----|------|---------|
| `recommend_approve` | Noul | Given mission, ROE label, track ID quality, and remaining rounds, should a human **consider** approval? |
| `urgency` | Score | Can wait / decide this compression step / decide now |
| `why_queued` | Choice | `assisted_high_risk` / `manual_all` / `uncertain_id` / `other` |

**Code then:** sort the C2 approval list; badge low-confidence rows. `TryApprovePendingOrder` stays a human (or later, a separate deterministic auto-approve policy you write and test). Headless replay continues to ignore the sidecar.

### 3. Watch / attention commander assist

Watch attention already pauses the player when the picture is overloaded. Jev can classify **why** a watch item matters once the deterministic queue has selected it:

- Noul: “Is this contact a likely leaker vs the current screen?”
- Choice: `retask_escort` / `tighten_emcon` / `hold` / `other`
- Score: commander cognitive urgency (calm / notable / interrupt)

Keep geometry, Pd, and CEC mesh in C#. Jev only labels the already-computed picture for the HUD.

### 4. Gauntlet / QA triage (never the oracle)

The gauntlet skill already forbids LLM override of script gates. Jev is useful **after** `gauntlet_oracle_eval` and `forge_scorecard.py` have produced machine authority:

| ID | Type | Options / meaning |
|----|------|-------------------|
| `failure_class` | Choice | `sim-code` / `oracle-expect` / `content-policy` / `harness` / `flaky-infra` / `other` |
| `needs_human_tl` | Noul | Does this need a qa-lead promotion write-up? |
| `severity` | Score | cosmetic pin / blocks one tier / blocks the train |

Use confidence: `< 0.5` → human; never flip `Passed=false` to green.

### 5. Mission / policy authoring assist (Phase 2+, propose only)

`AiAuthoringServices` is a keyword stub on purpose. Jev is a better *classifier* than a generator for the next increment:

- Choice: mission archetype (`patrol` / `strike` / `escort` / `support` / `ferry` / `other`) from a commander brief
- Noul: “Does this brief imply weapons-free on first recon?”
- Score: brief specificity (template / named AO / named ORBAT + trigger)

Then **existing deterministic editors** (`ScenarioDocumentEditor`, validation engine) materialize the draft. Jev does not emit JSON scenario files. That preserves ADR-008 export gates and the no-dynamic-execution contract.

Pair with an LLM only where text is required (briefing prose, red-force flavor). Jev routes; the LLM writes; the validation engine still blocks export.

### 6. Catalog / fixture quality (Track B)

Importer waves already split Curator / Quality / Write Gate. Jev can sit in Quality as a rubric, not a write:

- Noul: “Does this markdown fixture name a public citation for `basePd`?”
- Choice: `osint-ok` / `needs-curator` / `proprietary-risk` / `other`
- Score: completeness of sensors / mounts / weapons vs the curator checklist

Write Gate approve stays human. Do not send licensed CMO inspector dumps.

### 7. MCP / studio tool routing

The repo has a large agent/skill surface. TypeSafe’s [skill-suggestion cookbook](https://docs.typesafe.ai/cookbooks/skill_suggestion.md) is the pattern: Choice over a shortlist (≤255), plus a Noul “does this turn need a skill at all?”

Useful for Mission Editor CLI / MCP verb selection and for gauntlet agent routing (`haiku` vs `sonnet` vs script-only). Keep the shortlist built in code (never let Jev invent a verb).

### 8. What we should *not* ask Jev to do in this game

| Tempting idea | Why not |
|---------------|---------|
| Per-tick “pick Engage vs Patrol vs Hold” | Replaces softmax + seed; breaks replay; 100 ms × N agents × compression 256× is unbounded cost and latency |
| “Will this shot hit?” / Pd / DLZ | Arithmetic and catalog envelopes; Jev is weak at numbers ([jaggedness](https://docs.typesafe.ai/model-jaggedness/jev-1.13)) |
| Count contacts, salvo remaining, time-to-bingo | Count and date/time in code |
| Generate orders, briefings, or Lua | Not a generative model; chaining Choice-over-characters is explicitly a bad idea |
| Read the Cesium / Game View image | Text-only |
| Auto-approve lethal `Engage` on Full Autonomous | Even if autonomy allows execute-now, adding a model here creates a non-deterministic weapon-release path |
| Replace `SwarmContactClassifier` | Already a pure confidence-banded classifier; adding HTTP would not improve replay |

TypeSafe’s Doom bot at ~10 queries/s (~$7/hour) is a **demo cost**, not a design target for a theater sim that already compresses time and ticks many units.

---

## How to ask Jev so Aegis stays in control

Follow [How to build with System One](https://docs.typesafe.ai/concepts/how-to-build-with-system-one) and the milsim constraints together.

### 1. Legal set in code, judgment in Jev

```text
legal = Policy.GenerateCandidates(perceived, traits)
        .Where(score > 0)
        .Where(AutonomyGate would not ROE-reject)

ask Jev:
  for each legal[i]: noul "Is this a sound choice given mission + ROE + logistics?"
  plus one choice over the same ids + "none"

code:
  if all noul < threshold or choice.confidence < 0.5 → keep DecisionPipeline result
  else → advisory only (AAR / UI), do not swap the issued order on the live tick
```

On a live tick the last line is **advisory only**. A future experimental “Jev-tempered softmax” would still need a recorded, seeded blend and a **new isolated golden** (never Baltic v2).

### 2. Atomic questions, composite in C#

Do not ask “what should the destroyer do?” Split:

- Noul: track ID quality sufficient for Weapons Tight?
- Noul: magazine band allows a doctrinal salvo?
- Score: mission priority of this contact (picket / leaker / irrelevant)
- Choice: recommend `shadow` / `engage` / `break` / `other`

Then `0.5 * id + 0.3 * magazine + 0.2 * priority` (your weights, your file). This is [composite scoring](https://docs.typesafe.ai/patterns/composite-scoring). It maps cleanly onto existing `TraitVector` / policy knobs: Jev supplies features; you keep coefficients in `data/` or a pinned rubric JSON.

### 3. Confidence-gated routing

Use [confidence-gated routing](https://docs.typesafe.ai/patterns/confidence-routing). Starting floors (tune on labeled Aegis traces; these are not vendor defaults):

| Confidence | Action in Aegis |
|------------|-----------------|
| `< 0.5` | Hide or mark “model unsure”; do not change UI priority |
| `0.5–0.8` | Show as advisory; require human for anything that looks like release authority |
| `> 0.8` | Auto-tag AAR / auto-sort a list; still no weapon release |

Noul has **no** separate confidence field — the probability *is* the uncertainty. Do not carry a Choice-tuned threshold onto a Noul ([jaggedness § structural invariants](https://docs.typesafe.ai/model-jaggedness/jev-1.13)).

### 4. State hygiene

Jev accuracy falls as unrelated state grows. Build a dedicated `JevDecisionState` DTO:

- Include: unit id, mission type, effective ROE label, autonomy, personality name, attention load/budget, magazine band, fuel band, contact id + classification label + range **bucket** (not raw meters if you can avoid it), abort/denial reason enums as names.
- Exclude: full catalogs, raw lat/lon grids, entire order logs, Unity snapshots, player PII, API keys.
- Pre-compute numbers in C# (`rangeNm`, `secondsToBingo`, `livingEscorts`) and pass **named buckets** (`inside_dlz` / `edge` / `outside`) because Jev is better on semantic labels than raw quantities.

Point questions at fields with backtick paths: ``Does `roe.label` allow engaging `contact.classification`?``

### 5. Rubrics live in one file

Put every question, criterion, and threshold in a single versioned artifact, e.g. `data/jev/aar-v1.questions.json`, and log `model: jev-1.13.0` + rubric hash next to each answer. That is the reviewable surface — not a prompt buried in a C# string.

Describe **situations**, not degrees. Bad Score level: “moderately aggressive.” Good: “Would fire under Weapons Tight on a probable hostile without a visual ID.”

Always give Choice an `other` / `none_of_the_above` escape. Aegis enums are closed; the world is not.

---

## Integration sketch (when we implement)

Not a build plan — a boundary sketch so the first spike does not land in the wrong assembly.

| Layer | Recommendation |
|-------|----------------|
| Assembly | New optional `ProjectAegis.Delegation.Jev` (or a `Tools/` console) **referenced only by CLI / AAR / C2 hosts**. Do not reference it from `ProjectAegis.Sim` or `Delegation` core. |
| HTTP | Prefer TypeSafe `POST https://api.typesafe.ai/v1/systemone` for native confidence fields. OpenRouter `typesafe/jev-1.13` is the fallback if we already bill there — use their **Decisions** API, not chat completions ([OpenRouter model page](https://openrouter.ai/typesafe/jev-1.13)). |
| SDK | No official C# SDK. Thin `HttpClient` wrapper + records for `Noul`/`Choice`/`Score`. Official SDKs are JS (`@typesafe-ai/sdk`) and Python (`typesafe-sdk`) — acceptable for CLI/qa scripts, not for the sim. |
| Secrets | `TYPESAFE_API_KEY` or `OPENROUTER_API_KEY` from the environment. Never commit. Never call from Unity player builds. |
| Timeouts | 500–800 ms budget; on 429/529 exponential backoff; on failure **degrade to “no advisory.”** Never stall `Tick`. |
| Determinism tests | Wrapper tests use recorded HTTP fixtures. Solution test floor must stay green with the integration **disabled**. |
| Observability | Log `model`, `usage.input_tokens`, question ids, confidence — not full state if it includes scenario internals you would not ship. |

TypeSafe is early-access (waitlist or Vercel AI Gateway `typesafe-ai/jev`). Assume rate limits move. Zero-data-retention is an enterprise / Gateway option, not a default ([Flavio Copes deep dive](https://flaviocopes.com/jev/)).

---

## Cost and latency budget (order of magnitude)

Vendor numbers ([TypeSafe models](https://docs.typesafe.ai/models), [OpenRouter](https://openrouter.ai/typesafe/jev-1.13)):

- ~300-token AAR call ≈ $0.0000126
- 100,000 such calls ≈ $1.26
- Doom-style 10 qps ≈ $7/hour (their demo)

Aegis-relevant math:

| Cadence | Why it breaks or works |
|---------|------------------------|
| Every agent, every tick, 60 Hz, 256× compression | Unusable: latency + cost + non-determinism |
| Every pending approval (human time) | Fine: one call per queued `Engage` |
| Once per finalized scenario (AAR pack) | Fine: one fan-out over a filtered ledger |
| Gauntlet failure (already failed the oracle) | Fine: tens of calls per bad run |
| Keystroke / Game View | Wrong modality; we have no image path |

Measure from this machine before promising HUD latency. Vendor 100 ms is from US West.

---

## Jaggedness that will bite a milsim

From [Jev 1.13 jaggedness](https://docs.typesafe.ai/model-jaggedness/jev-1.13) (reviewed 2026-09-17), mapped to this codebase:

| Failure mode | Aegis example | What we do instead |
|--------------|---------------|--------------------|
| Literal reading | “Do not engage unless hostile” vs implied ID quality | Write the exact ID/ROE condition; put boundary cases in criteria |
| Math / counting | “How many escorts remain?” | `MemberAlive` count in C# |
| Numeric representations | Raw heading, hex colors, raw Pd | Named buckets (`hot`, `crossing`, `cold`) |
| Score interpolation | Treat `score: 1.4` as “40% more aggressive” | Threshold / rank only |
| Date and time | “Is this before the EMCON phase window?” | Compare ticks in C#; Jev only extracts labels if needed |
| Indirection | “Would the commander who wrote this ROE want…” | Ask about the written ROE label, not imagined intent |
| Large irrelevant state | Whole `ObservedState` + catalog | Filtered DTO |
| Adversarial content | Injected briefing / imported CMO flavor text | Curate state; test hostile fixtures |
| Structural invariants | Noul “engage?” vs Choice yes/no disagree | Ask one way; do not equate primitives |
| Generation | “Write the order rationale” | Keep `DecisionPipeline` rationale strings; LLM if you need prose |

English is the best-supported language. Keep rubrics in English even if the UI localizes later.

---

## Recommended adoption sequence

1. **Playground spike (no repo code).** Paste 10 real `AgentDecisionPayload` + `PolicyDenialRecord` JSON excerpts into the TypeSafe Playground. Write the AAR rubric. Measure agreement vs a human rater.
2. **Offline CLI verb.** `dotnet run --project src/ProjectAegis.MissionEditor.Cli -- jev_aar_tag --log <order-log>` behind a feature flag and API key. Fixture-test the HTTP wrapper. Does not reference `Tick`.
3. **Hindsight bank consumer.** After `FinalizeScenario`, tag `aar-*` with Jev answers. Compare to existing trust signals (`roe_violations`, `player_override_rate`).
4. **C2 pending-approval sort (advisory).** Presentation-only. Replay and PlayModeSmoke unchanged.
5. **Gauntlet triage assistant.** After scorecard, never before.
6. **Only then** consider an *experimental* isolated policy that uses Jev scores as extra `ScoredIntent` features — new `jev-*` scenario + new golden family, DelegationBridge still untouched, Baltic v2 hash grepped unchanged.

Do not skip step 1. Most of the value is in the rubric, not the client.

---

## Decision for the project

| Question | Recommendation |
|----------|----------------|
| Should Jev replace `DecisionPipeline`? | **No.** |
| Should Jev sit in the Unity/sim hot path? | **No.** |
| Is Jev useful for game decision making *around* the sim? | **Yes** — AAR, approval ranking, triage, authoring classification, tool routing. |
| LLM vs Jev for those jobs? | Jev for typed judgment + confidence; LLM only when we need generated text (briefs, patch notes). |
| First implementation shape? | Hindsight-class optional sidecar + CLI, pinned `jev-1.13.0`, rubrics in `data/jev/`. |

Jev is a fast, cheap, schema-safe **judgment function**. Project Aegis already has a faster, cheaper, replay-safe **decision function**. Use the first to explain, rank, and route; keep the second as the authority that fires.

---

## Sources

- [TypeSafe: Jev 1.13 — OpenRouter](https://openrouter.ai/typesafe/jev-1.13) (Sep 2026)
- [Introducing System One Models and Jev](https://typesafe.ai/blog/introducing-system-one-models-and-jev) (Sep 15, 2026)
- [System One concept](https://docs.typesafe.ai/concepts/system-one) (TypeSafe docs)
- [How to build with TypeSafe](https://docs.typesafe.ai/concepts/how-to-build-with-system-one)
- [Jev 1.13 jaggedness](https://docs.typesafe.ai/model-jaggedness/jev-1.13) (reviewed 2026-09-17)
- [TypeSafe models (pricing, aliases, context)](https://docs.typesafe.ai/models)
- [TypeSafe HTTP API](https://docs.typesafe.ai/api)
- [A deep dive into Jev](https://flaviocopes.com/jev/) (Sep 17, 2026)
- [Jev × HighwayEnv](https://dev.to/trknhr/jev-x-highwayenv-60-seconds-without-a-crash-30ig)
- [Generating levels in real time with Jev](https://vuink.com/post/fcevgrshfvba-d-dpbz/blog/generating-game-level-in-real-time-with-jev) (Sep 2026)
