# ADR-019: Agentic AAR Infrastructure — Read-Only Order Log Boundary

## Status

**Accepted** (`IReadOnlyOrderLog` introduced; `IOrderLog` extends it; AAR-facing signatures retyped)

## Date

2026-07-24

## Last Verified

2026-07-24 (Release build 0 errors; 1791 tests pass; ReplayGolden 6/6; PlayModeSmoke 21/21; Hindsight 7/7)

## Decision Makers

Owner sign-off 2026-07-24; DRG-45 research brief; `agentic-infrastructure.md` GDD

## Summary

Makes the AAR read-only guarantee **structural rather than conventional**. Before this ADR, After-Action-Review code was read-only only because `.claude/agents/hindsight-aar-analyst.md` grants no `Write` tool — nothing in C# prevented an AAR-adjacent consumer from appending to the order log and desyncing `ComputeFingerprint()`, the replay determinism invariant.

`IReadOnlyOrderLog` now carries the read surface; `IOrderLog` adds only `Append`. AAR-facing APIs take the read-only type.

## Engine Compatibility

| Field | Value |
|-------|-------|
| Engine | Unity 6.3 LTS + .NET 8 headless |
| Unity APIs | None — `ProjectAegis.Delegation`, plain C# |
| Targets | Builds clean for both `net8.0` and `netstandard2.1` (Unity plugin target) |
| Risk | **CRITICAL blast radius, LOW change risk** — additive interface split, see Consequences |

## ADR Dependencies

| Relationship | ADR / artifact |
|--------------|----------------|
| **Depends on** | ADR-001 (sim assembly boundary — the precedent for a structural wall), ADR-003 (order log schema) |
| **Precedent** | ADR-006 (`ICatalogReader` / `IWriteGate` read-write split) |
| **Enables** | `TR-agentic-002`, `TR-agentic-003` |
| **Conflicts with** | None — additive |

## GDD Requirements Addressed

| TR-ID | GDD | Requirement |
|-------|-----|-------------|
| TR-agentic-002 | agentic-infrastructure.md | Hindsight hook (P1) |
| TR-agentic-003 | agentic-infrastructure.md | AAR read-only agents (P1) |

## Decision

### Introduce `IReadOnlyOrderLog`; `IOrderLog` extends it

```csharp
public interface IReadOnlyOrderLog
{
    IReadOnlyList<OrderLogEntry> ChronologicalEntries();
    string ComputeFingerprint();
    IReadOnlyList<DecisionRecord> Records { get; }
    IReadOnlyList<PolicyDenialRecord> PolicyDenials { get; }
    IReadOnlyList<EngagementRecord> Engagements { get; }
    IReadOnlyList<ControllerChangeRecord> ControllerChanges { get; }
}

public interface IOrderLog : IReadOnlyOrderLog
{
    void Append(OrderLogEntry entry);
}
```

**Design correction against the original proposal.** The DRG-45 research brief proposed `IReadOnlyOrderLog { ChronologicalEntries(); ComputeFingerprint(); }` — **that shape does not compile.** `HindsightSessionFinalizer` also reads four typed collections (`Records`, `PolicyDenials`, `Engagements`, `ControllerChanges`). The read surface must include them or the boundary cannot be adopted at the one call site that matters. Verified by build failure, not by inspection.

### AAR-facing signatures take the read-only type

- `HindsightSessionFinalizer.OnScenarioFinalized(IReadOnlyOrderLog log, …)` — was `DecisionLog`
- `HindsightSessionFinalizer.BuildAarSummary(IReadOnlyOrderLog log, …)` — was `DecisionLog`
- `HindsightIntegration.OnScenarioFinalized(IReadOnlyOrderLog log, …)` — was `DecisionLog`

`DelegationOrchestrator` continues to pass its concrete `DecisionLog`; it satisfies the narrower type implicitly.

### `DelegationBridge` keeps the mutable type

The bridge legitimately appends on the hot path (`AppendPlayerOrder`, `AppendFuelBurn`, …). It is inside the trust boundary and is separately governed by the ZERO-hotpath-edit invariant. **The rule this ADR establishes is directional:** any *new* AAR-, export-, or analysis-facing API must take `IReadOnlyOrderLog`, never `IOrderLog` or the concrete log.

### Scope boundary

This ADR governs code paths reachable from AAR tooling and the Hindsight banks. Existing internal consumers that legitimately read the log for export (`LossesScoringCsvExporter`, `BalticBatchRunner`) are **not** retrofitted — they are inside the trust boundary and no evidence suggests otherwise.

## Alternatives Considered

| Option | Why rejected |
|--------|--------------|
| **B — immutable snapshot DTO** | Strongest guarantee, but more churn (new type + mapping) and duplicates entry data for long scenarios. `IReadOnlyOrderLog` already removes the mutation vector |
| **C — separate assembly + `internal` + `InternalsVisibleTo`** | Compiler-enforced at the strongest level, but `DecisionLog` and `DelegationBridge` live in *different* assemblies, so this needs friend grants that must then be audited. Solves a problem that does not exist yet: **AAR today is HTTP/CLI-only with no C# consumer.** Recorded as the future direction if one ever appears |
| **D — Roslyn analyzer / banned-symbols rule** | The repo has **no** analyzers today; adding tooling needs its own approval, and it enforces by CI discipline rather than by the compiler — less guarantee for more effort than Option A |

## Consequences

### Positive

- The read-only guarantee is now enforced by the type system, not by an agent-definition tool grant
- Matches two already-Accepted precedents (ADR-001, ADR-006), so the codebase gains no novel pattern
- Zero runtime cost; no new dependency; no assembly restructuring
- `DecisionLog` required **no changes** — it already satisfied the new interface, which is the clearest evidence the split is additive

### Negative

- Does not stop a caller that *already* holds `IOrderLog` or `DecisionLog` from passing it into AAR code by mistake. It constrains new signatures, not existing internal holders
- The `DecisionLog.HindsightHook` setter remains public and mutable (LOW risk, 0 upstream impact). Narrowing it to `init` was considered but deferred — the constructor ordering in `DelegationOrchestrator` needs checking first

## Validation Criteria

- [x] `IOrderLog : IReadOnlyOrderLog` compiles with **no changes to `DecisionLog`** — proves the split is additive and backward-compatible
- [x] Builds clean for `net8.0` **and** `netstandard2.1` — Release, 0 warnings, 0 errors
- [x] Full Release suite: **1791 passed**; the single failure is unrelated uncommitted `ProjectSettings.asset` churn (`Player_scripting_defines_do_not_enable_unity_mcp_ready`)
- [x] `ReplayGoldenSuiteTests` **6/6**; `PlayModeSmokeHarnessTests` **21/21**
- [x] Hindsight tests **7/7** — the suite covering the retyped call sites
- [x] `detect_changes` before commit: **risk medium**, 4 affected processes, all `OnScenarioFinalized` variants at step 1 — exactly the intended surface, no unexpected blast
- [x] GitNexus impact before edit: `IOrderLog` CRITICAL/211 (epistemic **exact**, 7 direct, **1 implementer**), `DecisionLog` CRITICAL/341

> The CRITICAL ratings reflect these hubs' existing centrality, **not** this edit's risk. Adding a narrower interface that `IOrderLog` extends changes no existing implementer or caller. Reviewers should not conflate the two.

## Migration Plan

1. Introduce `IReadOnlyOrderLog`; make `IOrderLog` extend it — done.
2. Retype the three AAR-facing signatures — done.
3. **Rule going forward:** new AAR/export/analysis APIs take `IReadOnlyOrderLog`. Code review enforces this; there is no analyzer.
4. If a genuine separate AAR **C# consumer** ever appears, revisit Option C (assembly boundary) — not before.
5. Hindsight client interfaces stay retain/reflect-only. Any bidirectional or command-style API requires a new ADR.
