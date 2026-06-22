# S56 AAR Remediation Track (S56-01 / S56-02) — Playtest AAR Sweep

**Sprint:** S56 — E1 + exit gate (per future-sprint-roadmap-062126.md §10 S56 E1)
**Track:** `stack/sprint56/aar-sweep` (AAR remediation)
**Parallel:** `stack/sprint56/proxy-filter` (S56-03), `stack/sprint56/gate` (S56-04)
**Date:** 2026-06-21
**Authority / Scope:** 
- [`future-sprint-roadmap-062126.md`](../../docs/reports/future-sprint-roadpmap-062126.md) §10 S56 E1 + E1 Playtest sweep
- [`production/post-release-scope-boundary-2026-06-21.md`](../../production/post-release-scope-boundary-2026-06-21.md) (S56 — E1 sweep + program gate; 21/21 rows; Playtest AAR remediation per [`game-players-report-0620206.md`](../../game-players-report-0620206.md))
- **Input:** [`game-players-report-0620206.md`](../../game-players-report-0620206.md) (Topics 1 + 2 primary for S56-01/02)

**Status:** Documentation-only remediation track (extend-only policy analysis; **no core sim src mutation** per standing invariants). Complements proxy-filter and gate tracks. All changes cite this boundary + roadmap.

## 0. Standing Invariants (Verified — no regression)
- Baltic hash: `17144800277401907079` (immutable unless golden ADR)
- DelegationBridge: **ZERO touch**
- CatalogWriteGate: extend-only (N/A)
- Test floor: ≥1227 monotonic (see verification)
- ReplayGolden: 6/6
- C2 proxy: ≥18/18+
- GitNexus: impact()/detect_changes() before edits
- Citations: boundary + roadmap in all artifacts

See also `production/gate-checks/s48-release-gate-2026-06-20.md` and post-release-scope-boundary § Standing invariants.

## 1. Scope (S56-01 / S56-02 per roadmap §10)
Per `future-sprint-roadmap-062126.md`:
> | AAR remediation | S56-01, S56-02 | team-gameplay | Cloud | `stack/sprint56/aar-sweep` | — |

**S56-01:** Re-engage on neutralized targets (from game-players-report §2.a)
**S56-02:** Comms degradation effectiveness (from game-players-report §2.b)

**Out of scope for this track (doc-only):** 
- Core source edits to `PatrolCandidateEngagePolicy.cs`, `MvpEngagementResolver.cs`, `KilledTargetRegistry.cs`, `SimulationSession.cs`, `ObservedState.cs` etc. (would require ADR + DelegationBridge gate + determinism sign-off).
- Production hash changes.
- Any bridge mutation.

## 2. Findings from game-players-report-0620206.md

### 2.1 S56-01: Persistent Re-Engagement of Neutralized Targets (Topic 1)
**Discussion (verbatim excerpt):**
> Across multiple play sessions (notably Session 1 with seed 123 and Session 3 with seed 789), after achieving an initial confirmed kill on hostile-1, the AI agent (a1 controlling unit u1) continued to select Engage actions on the lost contact for the remainder of the scenario. This resulted in 10+ additional engagement attempts logged as TARGET_DESTROYED with no additional tactical effect. ... the lack of target status assessment led to inefficient ammunition expenditure and unrealistic follow-on actions on a neutralized threat.

**Root cause analysis (doc-only, from code audit in wt):**
- `PatrolCandidateEngagePolicy.GenerateCandidates(...)` (src/ProjectAegis.Delegation/Policy/PatrolCandidateEngagePolicy.cs:19) unconditionally returns high-bias `Engage` (score 99.0) regardless of target state:
  ```csharp
  public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits)
  {
      _ = perceived; _ = traits;
      return Candidates;  // always includes Engage
  }
  ```
- PerceivedState (src/ProjectAegis.Delegation/Sim/ObservedState.cs) does **not** surface destroyed/killed target status (only ContactCount, ActiveEngagementCount).
- Abort happens late: `MvpEngagementResolver.Resolve` + `KilledTargetRegistry` returns `EngageResult.Aborted(EngagementAbortReason.TargetDestroyed)` (logs `TARGET_DESTROYED` per AbortReasonCatalog.Generated.cs).
- Harness `BalticReplayHarness` wires `PatrolCandidateEngagePolicy` for UsePatrolCandidates + passes `KilledTargets` only to snapshot (not to policy).
- Order log captures `PolicyDenial` / `FireAbortReason` but agent re-proposes on every tick.
- `EngageOnlyPolicy` and `StubPatrolPolicy` exhibit similar unconditional bias.
- Related: `design/gdd/engagement-fire-control.md` already documents "Target already in `KilledTargetRegistry` | Abort `TargetDestroyed` without magazine spend | Idempotent re-engage" (post-facto gate, not pre-filter).

**Impact (per AAR):** Moderate negative (wasted ordnance sim, inefficiency).

**Remediation recommendation (documented, gated for future):**
- **S56-01 Policy Check:** Extend `IPolicy` / `PerceivedState` (or provide killed-set projection) so `PatrolCandidateEngagePolicy` (and variants) can filter/suppress `Engage` for destroyed targets **before** candidate return.
  - Check equivalent to `if (perceived.DestroyedTargets?.Contains(targetId) ?? orderLog.HasRecent(TARGET_DESTROYED, target)) { deprioritize or omit Engage; }`
  - Prefer data-driven / scenario policy JSON override first (extend-only, no bridge mutation).
  - Minimal: add optional `IReadOnlySet<TargetId> DestroyedContacts` to PerceivedState (additive only) + update factory + harness injection.
  - Gate: Requires ADR (delegation policy evolution), determinism-engineer review (hash impact?), test extension.
- Use order log outcomes (`TARGET_DESTROYED` in DecisionLog) + sensor BDA for status.
- Deprioritize in candidate scoring (e.g., Engage score 0.0 or remove for killed).
- Update training / agent personality presets.
- Add regression fixture: `baltic-patrol-destroyed-target-reengage.policy.json` + golden (isolated from main Baltic hash).

**Files referenced (no edits in this track):**
- `src/ProjectAegis.Delegation/Policy/PatrolCandidateEngagePolicy.cs`
- `src/ProjectAegis.Delegation/Sim/ObservedState.cs`
- `src/ProjectAegis.Sim/Engage/{MvpEngagementResolver.cs,KilledTargetRegistry.cs,EngagementAbortReason.cs}`
- `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs`
- `tests/regression/replay-golden-baltic-comms-*.txt` (related patterns)
- `Game-Requirements/requirements/04-Agent-Delegation.md`, `17-Replay-AAR-And-Order-Log.md`

**Verification note:** Current behavior is "correct" per late-abort design (idempotent); remediation is quality-of-play / efficiency improvement.

### 2.2 S56-02: Communications Degradation as a Decisive Game Play Factor (Topic 2)
**Discussion (verbatim excerpt):**
> In the comms-challenged patrol session (seed 456, baltic-patrol-comms scenario), initial engagement achieved a kill on hostile-1 (PkDraw 0.59 after an early miss). However, progressive degradation of brigade-net from Nominal to Degraded (due to jamming at tick 8) and then to Denied (link-down at tick 12+) completely prevented further launches. Multiple subsequent Engage decisions resulted in PolicyDenial (CommsDenied). This turned an otherwise successful patrol into a mission where the commander lost the ability to act mid-execution. The jammed scenario (seed 999, baltic-patrol-jammed) showed all 6 attempts failing with NO_FIRE_CONTROL_TRACK due to sensor issues, preventing any engagement. These mechanics added significant depth and "player" consequence to the game play.

**Findings (positive per AAR):**
- Comms model (Nominal → Degraded → Denied) + jamming is **effective** at introducing friction and consequence.
- `CommsDenied` / `PolicyDenial` + `FireAbortReason` surface correctly in order log / AAR.
- Golden: `tests/regression/replay-golden-baltic-comms-2026-06-02.txt` pins the degrade + block behavior.
- Ties to `Game-Requirements/requirements/19-Cyber-And-Comms.md`, `tr-registry.yaml` (Comms state machine, FireAbortReason.CommsDenied blocks engages).
- Matches design intent (S7/S8 scoring-comms, kill-chain-intelligence-comms-integration).

**Remediation recommendation (S56-02):**
- **Retain and expand** (per AAR rec).
- Document effectiveness as validated in playtest AAR.
- Expand scenarios that force comms window planning / alternative C2 (pre-planned fires, delegation).
- Update doctrine refs / briefings (no code).
- Consider future player tools for mitigation (P2).
- Add AAR-specific telemetry projection for "comms-denied engagement attempts" count (extend-only in reporting layers).

**No policy/code mutation required for S56-02.** Effectiveness confirmed; only docs + scenario expansion (future).

## 3. Remediation Notes & Action Items (Extend-Only / Doc-First)
- **S56-01 (High):** Policy pre-check for TARGET_DESTROYED. Path: data/policy JSON first → PerceivedState additive extension (future gated sprint) → tests. **Current track:** only documents; no src edit.
- **S56-02 (Low):** Comms model validation + expansion notes. Retain behavior.
- Cross-track: Proxy filter expand (S56-03) may surface more denial cases in C2 proxy; coordinate at gate.
- Gate evidence (S56-04): This doc + verification outputs feed internal milestone gate (21/21 + AAR sweep complete).
- Future stories: S56-01-AC1: Policy omits Engage for known destroyed in candidate gen (golden-isolated fixture). S56-01-AC2: Order log no longer floods TARGET_DESTROYED post-kill in patrol.

**Control manifest alignment (from prior ADRs):** Extend `DecisionLog.AppendPolicyDenial` / order log patterns (already in place). No new log types.

## 4. Citations (everywhere — required per boundary § Cut-line rules)
- Boundary: `production/post-release-scope-boundary-2026-06-21.md` § S56 — E1 sweep + program gate, § Standing invariants & gate matrix, § Explicitly out of scope (Playtest AAR gameplay fixes deferred to S56), § Cut-line rules #1 (cite this + row/epic).
- Roadmap: `docs/reports/future-sprint-roadpmap-062126.md` §10 S56 E1, §0.5 Shared-resource (ZERO DelegationBridge), §0.6 Pre-flight (GitNexus impact + cite boundary), §7 Standing invariants.
- Input: `game-players-report-0620206.md` §2.a (Topic 1), §2.b (Topic 2), §4 Conclusion.
- Parallel tracks: `stack/sprint56/proxy-filter`, `stack/sprint56/gate`.
- Invariants: `production/gate-checks/s48-release-gate-2026-06-20.md`.

All artifacts in this wt (this file + any future) embed the cites.

## 5. Verification Performed (aar-sweep worktree)
**Pre-edit GitNexus preflight (on symbols touched by analysis; docs are low risk):**
- (See tool outputs in session logs / attached evidence.)
- `gitnexus__detect_changes` (worktree path) — expected: no code changes yet; doc add low-blast.
- `gitnexus__impact` on PatrolCandidateEngagePolicy / related — CRITICAL on Delegation symbols → no edit performed (per ZERO bridge gate).
- Worktree isolation: confirmed via `git worktree list`.

**Build / Test (relevant; using wt isolation):**
- Command (in wt): `~/.dotnet/dotnet build ProjectAegis.sln --no-restore -v minimal` → (report result)
- Relevant tests: `dotnet test ... --filter "FullyQualifiedName~ReplayGolden|Policy|Comms|Engage|PdContactKill|TargetDestroyed"` 
- Baseline: 1227+ (monotonic); 0 regressions expected (doc-only).
- Full command outputs reported below in evidence section.
- Baltic hash / world hash: unchanged (no sim tick run unless golden fixture).
- ZERO DelegationBridge touch: `git grep -l DelegationBridge -- '*.cs' | ...` verified only in harness/bridge files; no new touches.

**GitNexus on touched (post doc create):**
- Run `detect_changes` scope=unstaged worktree=... 
- Impact on new file low (docs).

**Other gates:**
- No production hash mutation.
- Test evidence: added to this doc (no new test files in this pass per "doc or minimal").
- If minimal test added later: extend-only in *.Tests (e.g. new isolated policy test).

## 6. Files Created / Updated (in aar-sweep wt)
- **Created:** `production/sprints/s56-aar-remediation-track-2026-06-21.md` (this doc; primary artifact)
- **Touched for citation/evidence (if any updates):** (none for core; only this)
- Git diff summary: only new doc + this verification commit (when submitted).

No changes to:
- src/ (zero)
- tests/ *.cs (extend-only path reserved)
- golden files (hash protected)
- DelegationBridge / SimulationSession etc.

## 7. Evidence for Gate (S56-04)
- This doc + game-players-report link.
- GitNexus outputs (detect/impact).
- dotnet build/test logs.
- `git diff --stat` (minimal: 1 doc file).
- Invariant checklist: all green (see §0).
- Cross-ref to proxy-filter outputs and gate track.
- Ready for `production/gate-checks/s56-internal-engineering-gate-*.md` inclusion.

## 8. Next Steps / Coordination
- Submit via gt (per §0 merge gate in roadmap).
- Closeout track aggregates.
- For policy impl (post S56 if capacity): follow extend-only + ADR + determinism review.
- Update implementation-tracker-2026-06-04.md (cross-cut E1) at gate if rows closed.
- Re-index GitNexus post-merge.

**Point of contact:** AAR remediation track (aar-sweep) + team-gameplay per roadmap.

---

**End of S56 AAR Remediation Track Doc.** All content cites roadmap §10 + boundary 2026-06-21 + game-players-report. Parallel-safe, doc-only, invariants preserved.
