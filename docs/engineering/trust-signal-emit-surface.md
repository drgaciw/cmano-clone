# Trust / XP signal emit surface — post-scenario campaign feedback

The **trust / experience emit surface** is the small, deterministic, **emit-only** seam that
turns a finished scenario's order log into per-agent campaign feedback signals. It lives in
[`ProjectAegis.Delegation/Trust/`](../../src/ProjectAegis.Delegation/Trust/) and fires from a
single teardown call — `FinalizeScenario` — after the last tick.

Its defining property is **emit-only**: computing trust signals *reads* the order log and
*returns* a list; it never mutates agent traits, never touches the tick math, and never writes
into the fingerprinted `DecisionLog`. This is the load-bearing rule from req 04 / req 13 — an
agent's behaviour inside a run is a pure function of `(observed state, traits, seed)`, so trust
can only ever influence a *future* run at scenario load (the Phase-3 campaign layer), never
mid-tick.

> **Scope — contract, not transport.** This page documents *what the signals are and how they
> are computed* — the contract. The optional
> [Hindsight session-memory sidecar](hindsight-session-memory-sidecar.md) is one *consumer* that
> ships each signal to a local memory bank; that page covers the network transport and the
> null-object design. If you only want "what does `player_override_rate` mean", you are on the
> right page.

---

## Source of truth

Verified against these files (docs-only page — no source changes):

| Concern | File |
|---------|------|
| Emit algorithm | [`Trust/TrustSignalEmitter.cs`](../../src/ProjectAegis.Delegation/Trust/TrustSignalEmitter.cs) |
| Signal DTO + Phase-3 container | [`Trust/AgentExperienceBlob.cs`](../../src/ProjectAegis.Delegation/Trust/AgentExperienceBlob.cs) (defines `TrustSignal` **and** `AgentExperienceBlob`) |
| Finalize seam | [`Orchestration/DelegationOrchestrator.cs`](../../src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs) (`FinalizeScenario`, `TrustSignals`) |
| Unity/headless passthrough | [`UnityAdapter/Bridge/DelegationBridge.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs) (`FinalizeScenario`) |
| Per-agent reserved container | [`Controllers/AgentController.cs`](../../src/ProjectAegis.Delegation/Controllers/AgentController.cs) (`Experience`) |
| Optional consumer (memory) | [`Hindsight/HindsightSessionFinalizer.cs`](../../src/ProjectAegis.Delegation/Hindsight/HindsightSessionFinalizer.cs) |
| Order-log inputs | [`Decision/DecisionLog.cs`](../../src/ProjectAegis.Delegation/Decision/DecisionLog.cs) (`Records`, `ControllerChanges`, `PolicyDenials`) |
| Test pin | [`Tests/Trust/TrustSignalEmitterTests.cs`](../../src/ProjectAegis.Delegation.Tests/Trust/TrustSignalEmitterTests.cs) |

Requirement anchors: [req 04 §3 "Trust / experience (campaign)"](../../Game-Requirements/requirements/04-Agent-Delegation.md)
(emit-only in tactical MVP, campaign aggregation Phase 3), aligned with
[req 13 (Doctrine / ROE)](../../Game-Requirements/requirements/13-Doctrine-ROE-EMCON-WRA.md)
("ROE violations feed **emit-only** `TrustSignal` records — no mid-run trait mutation") and
surfaced in the [req 17](../../Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md)
canonical order-log stream.

---

## Object model

Two tiny types, both in `AgentExperienceBlob.cs`:

```csharp
public sealed record TrustSignal(string AgentId, string Metric, double Value);

public sealed class AgentExperienceBlob
{
    public Dictionary<string, double> Metrics { get; } = new();
}
```

- **`TrustSignal`** — an immutable `(agentId, metric, value)` triple. This is what
  `EmitFromSession` returns and what consumers persist. `Value` is always a `double`; boolean
  outcomes are encoded as `1.0` / `0.0`.
- **`AgentExperienceBlob`** — a **reserved, currently-empty** per-agent bag. Every
  [`AgentController`](../../src/ProjectAegis.Delegation/Controllers/AgentController.cs) constructs
  one (`Experience`), but the tactical-MVP emit path does **not** populate it. It is the intended
  landing spot for the **Phase-3 campaign layer** that will aggregate `TrustSignal`s across runs
  (req 04 §3). Do not read behaviour from it today — it is a forward hook, not live state.

---

## Where it runs — the `FinalizeScenario` seam

Trust emission is a **post-run teardown call**, never part of `Tick()`:

```text
… deterministic tick loop … (Tick × N)          # never sees Trust
        │
        ▼
DelegationOrchestrator.FinalizeScenario(missionSucceeded, objectivesMetRatio)
  ├─ _trustSignals.Clear()
  ├─ _trustSignals.AddRange(TrustSignalEmitter.EmitFromSession(DecisionLog, …))   # 5 metrics/agent
  ├─ Hindsight?.OnScenarioFinalized(DecisionLog, _trustSignals, …)                # null in CI/replay
  └─ return _trustSignals                                                         # also on .TrustSignals
```

- [`DelegationOrchestrator.FinalizeScenario(bool missionSucceeded = false, double objectivesMetRatio = 1.0)`](../../src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs)
  clears and recomputes `_trustSignals`, forwards to the optional Hindsight hook, and returns the
  list. The most recent result is also exposed on the read-only `TrustSignals` property. Calling
  it twice is idempotent for a given log (the `Clear()` prevents accumulation).
- [`DelegationBridge.FinalizeScenario(…)`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs)
  is a thin passthrough so the Unity host and the headless
  [Baltic replay harness](baltic-replay-harness.md) call the exact same code.
- The signals are **returned and cached, not appended to the fingerprinted `DecisionLog`** —
  there is no `AppendTrustSignal`. That is exactly why they are replay-safe (see
  [Determinism](#determinism--replay-safety)).

---

## The emit algorithm

[`TrustSignalEmitter.EmitFromSession`](../../src/ProjectAegis.Delegation/Trust/TrustSignalEmitter.cs)
is a **pure static** function over the order log. Two steps:

### 1. Seed the agent set

```text
agentIds = distinct-ordinal(
    DecisionLog.Records.AgentId               // every agent that decided
  ∪ DecisionLog.ControllerChanges.AgentId )   // + agents overridden before deciding
if agentIds is empty → ["session"]            // whole-run fallback bucket
```

Seeding from **both** decision records and controller-change events is deliberate: an agent that
the player took over *before* it ever produced a `DecisionRecord` (e.g. immediate direct
control) still gets a trust row. When the log has no agent identity at all, a single synthetic
`"session"` id aggregates the whole run.

### 2. Emit five metrics per agent

For each `agentId` (with `isSessionFallback = agentId == "session"`, which aggregates *all*
records rather than filtering by id):

| Metric (`TrustSignal.Metric`) | Value | Source |
|-------------------------------|-------|--------|
| `missions_succeeded` | `1.0` if `missionSucceeded` else `0.0` | caller argument |
| `objectives_met_ratio` | `objectivesMetRatio` verbatim (default `1.0`) | caller argument |
| `roe_violations` | count of `PolicyDenials` attributed to this agent | order log |
| `friendly_fire_incidents` | **`0.0` — hardcoded stub** (no fratricide model yet) | reserved |
| `player_override_rate` | `overrides / decisions`, or `0.0` when `decisions == 0` | order log |

where, for a non-fallback agent:

- `decisions` = `Records` count with `AgentId == agentId`
- `overrides` = `ControllerChanges` count with `NewKind == "Human"` and `AgentId == agentId`
- `roeViolations` = `PolicyDenials` count with `AgentId == agentId`

So a run with two agents emits **10** signals; a decisionless run emits **5** on the `session`
bucket. `friendly_fire_incidents` is intentionally inert until a fratricide/blue-on-blue model
exists — treat it as a declared-but-not-wired metric, the same way
[some `TraitVector` scalars are declared-only](agent-traits-and-attention.md).

---

## Emit-only contract & the Phase-3 campaign plan

The whole point of this surface is what it **does not** do:

- **No trait mutation during a run.** Trust signals never feed back into `TraitVector`,
  `DecisionPipeline`, or the `AttentionCalculator`. An agent's in-run behaviour stays a pure
  function of `(observed state, traits, seed)` (req 04 / req 13).
- **No mid-tick effect at all.** Emission happens once, at `FinalizeScenario`, after the last
  tick.
- **The campaign layer is deferred (Phase 3).** Per req 04 §3, a future campaign layer will
  aggregate `TrustSignal`s into the per-agent `AgentExperienceBlob` and *may* adjust trait
  snapshots **at scenario load only** — never mid-run — preserving replay determinism. The
  shipped tactical MVP is emit-only; `AgentExperienceBlob` is the empty container waiting for
  that work.

This split is why "player approval / trust can't change what an agent does *this* run" holds,
the same family of invariant as
[player-approval-can't-override-ROE](autonomy-roe-gating.md).

---

## Determinism & replay safety

`EmitFromSession` is safe to run in CI and replay because:

- It is **pure over the order log** — same log in, same signals out.
- It uses **ordinal** string comparison for the distinct agent set and deterministic LINQ
  `Count` / `Select` folds — no hash-ordered iteration, no culture-sensitive compares.
- It uses **no RNG, no `DateTime`, no I/O** in the computation itself.
- It runs **outside the tick loop** and **does not append to `DecisionLog`**, so it contributes
  nothing to `DecisionLog.ComputeFingerprint()` and cannot move the Baltic v2 replay hash
  (`17144800277401907079`). See [determinism & replay](determinism-and-replay.md).

The only side-effecting consumer, the Hindsight sidecar, is **null in CI/replay** and
fire-and-forget when enabled — it cannot affect sim teardown or the hash either.

`TrustSignalEmitterTests.FinalizeScenario_emits_mvp_trust_metrics_without_affecting_ticks`
pins the behaviour: a run with one decision and one override emits `missions_succeeded`,
`objectives_met_ratio`, and `player_override_rate` with the expected values.

---

## Consumers

| Consumer | How it uses the signals |
|----------|-------------------------|
| `DelegationOrchestrator.TrustSignals` | Read-only snapshot of the last `FinalizeScenario` result — available to any host for AAR / debrief UI. |
| `DelegationBridge.FinalizeScenario` | Unity/headless passthrough returning the same list. |
| [`HindsightSessionFinalizer`](hindsight-session-memory-sidecar.md) | When `FinalizeCampaignExperience` is set, retains each signal to the per-agent `agent-xp-{agentId}` memory bank as `trust_metric=… value=… mission_succeeded=… objectives_met_ratio=…` (context `campaign-trust`), fire-and-forget. Off by default. |
| Campaign layer (Phase 3, **not yet built**) | Will aggregate into `AgentExperienceBlob` and adjust load-time trait snapshots. |

---

## Extension runbook

**To add a new trust metric**, add one `signals.Add(new TrustSignal(agentId, "<name>", <value>))`
inside the per-agent loop in `TrustSignalEmitter.EmitFromSession`:

1. Derive `<value>` **only** from the `DecisionLog` (or the two caller arguments) — keep the
   function pure. Attribute to the agent with the `isSessionFallback || <record>.AgentId == agentId`
   pattern already used for `roe_violations`.
2. Encode booleans as `1.0` / `0.0`; guard divisions against a zero denominator (see
   `player_override_rate`).
3. Extend `TrustSignalEmitterTests` with the new metric — it is the behavioural pin.
4. No order-log schema change is needed (signals are not logged), so **no replay-golden
   regeneration** is required. Confirm the Baltic v2 hash is untouched anyway before submitting.

**To consume the signals** in a new host surface, read
`DelegationOrchestrator.TrustSignals` (or the `FinalizeScenario` return value) after the run.
**Do not** wire them back into anything the tick reads — that would break the emit-only
invariant and replay determinism.

---

## See also

| Topic | Doc |
|-------|-----|
| Runtime memory transport for these signals | [hindsight-session-memory-sidecar.md](hindsight-session-memory-sidecar.md) |
| The decision tick that produces the order log | [agent-decision-pipeline.md](agent-decision-pipeline.md) |
| Trait inputs (and declared-vs-wired precedent) | [agent-traits-and-attention.md](agent-traits-and-attention.md) |
| ROE denials that feed `roe_violations` | [autonomy-roe-gating.md](autonomy-roe-gating.md) |
| Controller changes that feed `player_override_rate` | [direct-control-override-runtime.md](direct-control-override-runtime.md) |
| Determinism rules, hashing, golden workflow | [determinism-and-replay.md](determinism-and-replay.md) |
| Agent delegation framework overview | [`ProjectAegis.Delegation/README.md`](../../src/ProjectAegis.Delegation/README.md) |
| Design intent (emit-only, Phase-3 campaign) | [req 04 §3](../../Game-Requirements/requirements/04-Agent-Delegation.md) |
