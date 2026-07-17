# Hindsight session-memory sidecar — runtime agent memory & trust signals

The **Hindsight session-memory sidecar** is the optional runtime feedback loop that streams a
simulation run's *agent decisions*, *session AAR*, and *campaign trust signals* into a local
[Hindsight](https://hindsight.vectorize.io/) memory server. It lives in
[`ProjectAegis.Delegation/Hindsight/`](../../src/ProjectAegis.Delegation/Hindsight/) and
[`ProjectAegis.Delegation/Trust/`](../../src/ProjectAegis.Delegation/Trust/), and it hangs off the
[`DelegationOrchestrator`](../../src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs)
by two thin seams.

It is **off by default and determinism-safe**: with no options passed the orchestrator wires a
null-object hook, nothing touches the network, and the order-log fingerprint / replay hash are
bit-for-bit identical to a run with the sidecar disabled.

> **Scope — not to be confused with the *dev* loop.** This page documents the *runtime* sidecar
> that a running simulation uses to record what its agents did. The separate
> [hindsight-agentic-dev.md](hindsight-agentic-dev.md) covers the *engineering* use of Hindsight
> (session memory for coding agents, paired with GitNexus). Different banks, different lifecycle,
> different audience. See [`AGENTS.md`](../../AGENTS.md) for the dev-bank conventions
> (`dev-cmano-clone`, `dev-story-*`, `dev-pr-*`) — those are populated by tooling, not by this
> runtime.

---

## Where it runs

The sidecar has exactly **two integration points**, both on the delegation core — nothing in the
deterministic tick math is aware of it:

```
DelegationOrchestrator(globalSeed, policyEvaluator, hindsight: HindsightOptions?)
  │  HindsightIntegration.TryCreate(options)          # null unless options.Enabled
  │  DecisionLog.HindsightHook = integration.OrderLogHook
  │
  ├─ (per decision) DecisionLog append  ── NotifyHindsight(entry, sequenceId)
  │       └─ IHindsightOrderLogHook.OnAppended(entry)
  │             AgentDecision entries only → retain → agent-{personality}-{agentId}
  │
  └─ (end of run) FinalizeScenario(missionSucceeded, objectivesMetRatio)
        ├─ TrustSignalEmitter.EmitFromSession(DecisionLog, …)   → 5 metrics/agent
        └─ HindsightIntegration.OnScenarioFinalized(...)
              ├─ retain AAR summary        → aar-{scenario}-{runId}
              └─ retain each trust signal  → agent-xp-{agentId}
```

- **Point 1 — order-log append hook.** When the sidecar is enabled,
  `DelegationOrchestrator` sets `DecisionLog.HindsightHook`. Every `DecisionLog` append routes
  through `NotifyHindsight` ([`DecisionLog.cs`](../../src/ProjectAegis.Delegation/Decision/DecisionLog.cs)),
  which is a null-safe no-op when no hook is set and *does not affect append semantics or the
  fingerprint*. The hook fires only for `OrderLogEntryKind.AgentDecision` entries.
- **Point 2 — scenario finalize.** `DelegationOrchestrator.FinalizeScenario` (also surfaced via
  `DelegationBridge.FinalizeScenario`) is a post-run call. It always computes trust signals
  (`TrustSignalEmitter`), and — only if the sidecar is enabled — retains an AAR summary and the
  per-agent trust signals.

`FinalizeScenario` is a teardown call, **not** part of `Tick()`. The trust metrics themselves are
computed unconditionally (they are cheap, deterministic, and exposed via
`DelegationOrchestrator.TrustSignals`); the network retain only happens when Hindsight is on.

---

## Enabling the sidecar

The default `DelegationOrchestrator(int globalSeed, …)` constructor passes `hindsight: null`, so
CI, replay goldens, and the QA Gauntlet never touch Hindsight. Opt in by passing
`HindsightOptions` to the extended constructor:

```csharp
var hindsight = new HindsightOptions
{
    BaseUrl = "http://localhost:8888",
    ScenarioSlug = "baltic",
    RunId = "run-001", // optional; defaults to the order-log fingerprint prefix
};

var orchestrator = new DelegationOrchestrator(globalSeed: 42, policyEvaluator: null, hindsight);
```

Requires a running Hindsight server (see
`.claude/skills/hindsight/hindsight-local-setup/SKILL.md`). If the server is unreachable, retains
fail silently (see [Determinism & safety](#determinism--safety)).

### `HindsightOptions`

Source: [`HindsightOptions.cs`](../../src/ProjectAegis.Delegation/Hindsight/HindsightOptions.cs).

| Field | Default | Effect |
|-------|---------|--------|
| `Enabled` | `true` | Master switch checked by `TryCreate` and the finalizer. **Off-by-default lives one level up:** the orchestrator only builds the integration when you pass a non-null `HindsightOptions`, so the default constructor never enables it regardless of this flag. |
| `BaseUrl` | `http://localhost:8888` | Hindsight server root; trailing slash normalized. |
| `ApiKey` | `null` | Sent as `Authorization: Bearer …` when set. |
| `ScenarioSlug` | `session` | Slug for the AAR bank id. |
| `RunId` | `null` | Run identifier; when null, derived from the order-log fingerprint (first 16 chars) at finalize. |
| `RetainAgentDecisions` | `true` | When false, `TryCreate` wires `NullHindsightOrderLogHook` — finalize banks still fire, but per-decision memory is skipped. |
| `FinalizeAarBank` | `true` | Retain the session AAR summary at finalize. |
| `FinalizeCampaignExperience` | `true` | Retain per-agent trust signals at finalize. |
| `AarReflectQuery` | `null` | Optional `reflect` call after the AAR retain — **tooling only**, never used in the tick loop. Failures are swallowed. |

`HindsightIntegration.TryCreate` returns `null` when `options` is null or `!options.Enabled`;
otherwise it builds a 30 s-timeout `HttpClient`, a `HindsightHttpMemoryClient`, the order-log hook
(real or null depending on `RetainAgentDecisions`), and the session finalizer
([`HindsightIntegration.cs`](../../src/ProjectAegis.Delegation/Hindsight/HindsightIntegration.cs)).

---

## Memory banks

Bank ids are built by [`HindsightBankIds`](../../src/ProjectAegis.Delegation/Hindsight/HindsightBankIds.cs).
All segments pass through `Slug` (trim → lowercase → spaces to hyphens; empty → `unknown`).

| Bank id pattern | Written when | Content |
|-----------------|--------------|---------|
| `agent-{personality}-{agentId}` | each `AgentDecision` append | One NL memory per decision (see [formatter](#decision-memory-format)). Personality comes from `RegisterAgent`; defaults to `custom`. |
| `aar-{scenario}-{runId}` | `FinalizeScenario` (if `FinalizeAarBank`) | One session AAR summary. |
| `agent-xp-{agentId}` | `FinalizeScenario` (if `FinalizeCampaignExperience`) | One memory per emitted trust signal, tagged `context="campaign-trust"`. |
| `balance-tuning` | — | Reserved constant for the balance-tuning tooling agent; not written by this runtime. |
| `dev-cmano-clone`, `dev-story-{slug}`, `dev-pr-{n}` | — | Dev-loop banks (see [hindsight-agentic-dev.md](hindsight-agentic-dev.md)); helpers only, not written here. |

### Decision memory format

[`AgentDecisionMemoryFormatter.Format`](../../src/ProjectAegis.Delegation/Hindsight/AgentDecisionMemoryFormatter.cs)
turns an [`AgentDecisionPayload`](../../src/ProjectAegis.Delegation/Decision/AgentDecisionPayload.cs)
into a compact natural-language string (GDD §6.4 "lazy NL") using `InvariantCulture` throughout:

```
sim_time={SimTime} sim_tick={SimTick} agent={AgentId} personality={personality} target={TargetId} autonomy={AutonomyLevel}.
Chose {ChosenOrderKind} over alternatives [{Kind score=… risk=…}, …].
Rationale: {Rationale}
Attention load {pct}% ({AttentionLoad}/{AttentionBudget}).
rng_draw={RngDraw}.
```

`attentionPct` is `AttentionLoad / AttentionBudget` (0 when budget ≤ 0), and each alternative comes
from the payload's `ScoredIntents` (kind, softmax score, risk level). The hook accepts both the
canonical `AgentDecisionPayload` and legacy `DecisionRecord` entries (via
`AgentDecisionPayload.FromDecisionRecord`)
([`HindsightOrderLogHook.cs`](../../src/ProjectAegis.Delegation/Hindsight/HindsightOrderLogHook.cs)).

---

## Trust signals

At finalize, [`TrustSignalEmitter.EmitFromSession`](../../src/ProjectAegis.Delegation/Trust/TrustSignalEmitter.cs)
walks the `DecisionLog` and emits a fixed set of `TrustSignal(AgentId, Metric, Value)` records
([`AgentExperienceBlob.cs`](../../src/ProjectAegis.Delegation/Trust/AgentExperienceBlob.cs)).

- **Agent id set** = union of agents that produced a `DecisionRecord` **and** agents named in a
  `ControllerChange` (so an agent overridden *before* it ever decided still gets signals). If the
  set is empty, it falls back to a single `session` pseudo-agent that aggregates the whole run.
- **Five metrics per agent** (`session` fallback aggregates across all agents):

  | Metric | Value |
  |--------|-------|
  | `missions_succeeded` | `1.0` / `0.0` from `missionSucceeded` |
  | `objectives_met_ratio` | pass-through of `objectivesMetRatio` (default `1.0`) |
  | `roe_violations` | count of `PolicyDenials` attributed to the agent |
  | `friendly_fire_incidents` | `0.0` (MVP placeholder — not yet derived) |
  | `player_override_rate` | `overrides / decisions` (`Human` controller changes over decision count; `0.0` when no decisions) |

The [`HindsightSessionFinalizer`](../../src/ProjectAegis.Delegation/Hindsight/HindsightSessionFinalizer.cs)
then retains each signal as `trust_metric=… value=… mission_succeeded=… objectives_met_ratio=…`
into the agent's `agent-xp-{agentId}` bank, and builds the AAR summary (mission outcome,
decision / policy-denial / engagement counts, order-log fingerprint, and a per-agent
decisions-and-overrides breakdown) for the `aar-{scenario}-{runId}` bank.

---

## HTTP transport

[`HindsightHttpMemoryClient`](../../src/ProjectAegis.Delegation/Hindsight/HindsightHttpMemoryClient.cs)
speaks the Hindsight v1 API with camelCase JSON (null fields omitted):

| Operation | Request |
|-----------|---------|
| Retain | `POST /v1/default/banks/{bank}/memories/retain` — `{ items: [{ content, context }], async: true }` |
| Reflect | `POST /v1/default/banks/{bank}/reflect` — `{ query, budget: "mid", includeFacts: true }` |

`RetainFireAndForget` dispatches the retain on a background `Task.Run` and **swallows every
exception** — Hindsight is optional infrastructure and must never fault the simulation. The
runtime only ever calls `retain` on the hot path; `reflect` is reachable solely through the
opt-in `AarReflectQuery` teardown path.

---

## Determinism & safety

This is the reason the sidecar can exist inside a determinism-critical engine:

- **Null-object default.** No options → `DecisionLog.HindsightHook` is unset and every notify is a
  no-op; `NullHindsightMemoryClient` / `NullHindsightOrderLogHook` provide inert fallbacks.
- **Read-only observation.** The hook only *reads* order-log entries after they are appended; it
  never mutates the log, and `NotifyHindsight` explicitly leaves append semantics and the
  fingerprint untouched.
- **Fire-and-forget, failure-isolated.** Retains run on background tasks and swallow all errors;
  reflect failures are caught in the finalizer. An unreachable or slow server cannot stall or
  diverge a run.
- **No recall/reflect in the tick loop.** `IHindsightMemoryClient` exposes `retain` for the hot
  path; recall/reflect are for AAR tooling only. Do not call them from `Tick()` or policy code.

Net effect: order-log fingerprints and replay hashes (including the Baltic v2 hash
`17144800277401907079`) are identical whether the sidecar is off, on-and-reachable, or
on-and-broken.

---

## Extending it

- **New per-decision field** → extend `AgentDecisionMemoryFormatter.Format` (and, if it is a new
  payload field, `AgentDecisionPayload`). Keep it retain-only and `InvariantCulture`.
- **New trust metric** → add it in `TrustSignalEmitter.EmitFromSession` for both the per-agent and
  `session`-fallback branches; it flows to `agent-xp-*` automatically.
- **New bank** → add a builder to `HindsightBankIds` (route every segment through `Slug`) rather
  than hand-formatting ids at the call site.
- **Never** add a recall/reflect call to a hot path, and never let a Hindsight failure surface to
  the caller.

---

## Tests

Behavior is pinned by co-located NUnit tests in
[`ProjectAegis.Delegation.Tests`](../../src/ProjectAegis.Delegation.Tests/):

| Test | Guards |
|------|--------|
| [`HindsightOrderLogHookTests`](../../src/ProjectAegis.Delegation.Tests/Hindsight/HindsightOrderLogHookTests.cs) | Only `AgentDecision` entries retain; bank id + formatted content. |
| [`HindsightSessionFinalizerTests`](../../src/ProjectAegis.Delegation.Tests/Hindsight/HindsightSessionFinalizerTests.cs) | AAR + `agent-xp` banks retained with the expected ids. |
| [`TrustSignalEmitterTests`](../../src/ProjectAegis.Delegation.Tests/Trust/TrustSignalEmitterTests.cs) | Finalize emits the MVP trust metrics without perturbing ticks. |
