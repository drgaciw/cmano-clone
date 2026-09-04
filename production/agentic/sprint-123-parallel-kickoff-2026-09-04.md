# S123 Parallel Kickoff — 2026-09-04

**Linear:** DRG-231..DRG-241 · **Predecessor:** S122 (#618 / C2 visual bind) · **Epic:** Governance & Backlog Expansion  
**Stage:** **Release** · **Not Launch**  
**Skill:** `dispatching-parallel-agents` · [`linear-parallel-dispatch-playbook.md`](linear-parallel-dispatch-playbook.md)  
**Plan:** [`production/sprints/sprint-123-backlog-expansion.md`](../sprints/sprint-123-backlog-expansion.md)

---

## 1. Wave 1 Disjoint Surface Matrix

The four stories in Wave 1 have **zero file-level or symbol-level overlap**, permitting fully isolated parallel worktree execution:

| Story ID | Title | Lane | File Surface (Allowed) | Invariant Boundary |
|---|---|---|---|---|
| **AEGIS-304** (DRG-234) | Disambiguate `WraRange` from `OutOfEnvelope` | **WT-1** (`csharp-sim-specialist`) | `src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs`, `abort_reason_manifest.json`, `EngagementAbortReason.cs` | Do not alter kinematic DLZ calculations; strictly extend abort reason enum. |
| **AEGIS-301** (DRG-231) | `PendingApprovalQueue` Clock Auto-Pause & Reactive TTL | **WT-2** (`csharp-sim-specialist`) | `src/ProjectAegis.Sim/Time/WatchAutoPauseGate.cs`, `src/ProjectAegis.Delegation/Orchestration/PendingApprovalQueue.cs` | Zero edits to `DelegationBridge.cs` hotpath; auto-pause must remain configurable. |
| **AEGIS-308** (DRG-238) | Harmonize Test Floor Baseline (≥1924) & CI Honesty | **WT-3** (`devops-ci-steward`) | `tools/buildkite/dotnet-ci.sh`, `tools/verify-ci-local.ps1`, `AGENTS.md`, `Game-Requirements/requirements/01-Project-Overview.md` | Monotonic floor strictly holds; must pass on-disk suite without suppressing real failures. |
| **AEGIS-310** (DRG-240) | Formally Promote Draft Documents 23–27 | **WT-4** (`requirements-steward`) | `Game-Requirements/drafts/23..27` -> `Game-Requirements/requirements/23..27`, `00-Master-Index.md` | Docs-only; zero C# edits; preserve all MADR and requirement traceability links. |

---

## 2. Invariants & Gates

1. **Stage: Release**: No advance to Launch or Phase N without human sign-off.
2. **Hotpath Zero-Touch**: `DelegationBridge.cs` is strictly untouched across all lanes.
3. **Extend-Only**: `CatalogWriteGate.cs` is untouched.
4. **Replay Determinism**: Baltic Golden Replay hash `17144800277401907079` (6/6 tests) immutable.
5. **Monotonic Test Floor**: Post-Wave 1 floor must be `≥1924` passing tests with 0 failures.
