# Spirit 1 Gap Remediation — Parallel Kickoff

**Date:** 2026-06-20  
**Trunk:** `main` @ post-S39 — **≥1215** tests, ReplayGolden **6/6**, C2 proxy **18/18+**  
**Gap analysis authority:** [`Game-Requirements/reviews/spirit1-vertical-slice-gap-analysis-2026-06-05.md`](../../Game-Requirements/reviews/spirit1-vertical-slice-gap-analysis-2026-06-05.md)  
**Routing authority:** [`local-cloud-agent-routing.md`](local-cloud-agent-routing.md)  
**Remediation log (track outcomes):** [`spirit1-gap-remediation-log-2026-06-20.md`](spirit1-gap-remediation-log-2026-06-20.md)  
**Program note:** Runs **orthogonal** to S40–S48 polish/release train — see [`s39-s48-program-execution-guide.md`](s39-s48-program-execution-guide.md).

## Goal (recap)

Close **traceability and tooling gaps** from the Vertical Slice MVP gap analysis without expanding MVP scope. Must-ship functionality is already present (~95 % weighted completeness); this program addresses **G1, G2, G5** and recommendations **R3, R5**. **G4** (full platform import) and **G3** (Unity Editor in CI) remain **deferred** per MVP cut scope.

| Gap / Rec | Severity | Track | Outcome |
|-----------|----------|-------|---------|
| **G5** — FTS/embeddings missing | Medium (tooling) | GitNexus index | Rebuilt index; semantic `query` restored |
| **G1** — `SensorC2PanelHost` graph isolation | Medium (structural) | SensorC2 seam | Explicit adapter seam; host → `IC2PresentationFeed` auditable via `impact`/`context` |
| **G2** — Classify “FSM” is scripted lifecycle | Low (by design) | Docs | Honest labeling + post-MVP tracker row for true FSM |
| **R3** — `SimulationSession` CRITICAL hub | Advisory | Docs | Frozen-hub ADR note; impact gate documented |
| **R5** — “Spirit 1” phantom milestone | Process | Docs | Artifacts re-labeled to **Vertical Slice MVP** vocabulary |
| **G4** — Full platform import | Low (cuttable) | **Deferred** | No dispatch; note in remediation log only |
| **G3** — Unity Editor not in CI | Low (mitigated) | **Out of scope** | Headless PlayMode harness remains gate |

## Parallel execution model

**Prerequisites (serial, blocking — local coordinator):**

1. Record baseline in remediation log: commit SHA, test count, ReplayGolden 6/6, Baltic hash.
2. Bootstrap worktrees (below) from current `main`.
3. Confirm **no track edits shared files** (ownership matrix).

After prereqs:

- **Three active tracks run fully parallel** — G4 is documentation-only deferral, not a dispatch.
- **One agent per active track** — isolated context; cite gap analysis + hard gates in every prompt.
- Use GitNexus `impact()` before any symbol edit; `detect_changes()` before commit.
- Coordinator owns merge order and remediation log updates.

**Emphasis:** Traceability and index health without touching `DelegationBridge.cs`, Baltic hash, or MVP feature scope.

## Wave plan

| Wave | Track | Gap / Rec | Est. | Agent env | Notes |
|------|-------|-----------|------|-----------|-------|
| W0 | **Coordinator baseline** | — | 0.25d | **Local** | Baseline + worktrees + remediation log header |
| W1 | **G5 — GitNexus index** | G5 | 0.5d | **Cloud** | `npx gitnexus analyze --force`; verify FTS/embeddings; record tip in log |
| W1 | **Docs — traceability** | G2, R3, R5 | 1d | **Cloud** | Milestone rename, FSM labeling, SimulationSession frozen-hub note |
| W1 | **G1 — SensorC2 seam** | G1 | 1.5d | **Local + Cloud** | Adapter interface wiring; tests; graph edge verification post-G5 |
| W2 | **G4 deferred note** | G4 | — | Coordinator | Log entry only — no branch, no agent |
| W3 | **Coordinator closeout** | — | 0.25d | **Local** | Merge stacks, full gate, remediation log COMPLETE |

**Parallelism:** W1 tracks **G5**, **Docs**, and **G1** dispatch together after W0. G1 may re-run `context(SensorC2PanelHost)` after G5 merge to confirm edges.

## Worktree manifest

**Parent repo:** `/home/username01/cmano-clone/cmano-clone`  
**Worktree root:** `/home/username01/cmano-clone/.worktrees/`  
**Stack workflow:** Graphite — `gt create`, `gt submit --stack --no-interactive`

```bash
# Coordinator bootstrap (local)
git worktree add /home/username01/cmano-clone/.worktrees/spirit1-gitnexus \
  -b stack/spirit1/gitnexus-index main

git worktree add /home/username01/cmano-clone/.worktrees/spirit1-docs-traceability \
  -b stack/spirit1/docs-traceability main

git worktree add /home/username01/cmano-clone/.worktrees/spirit1-sensor-c2-seam \
  -b stack/spirit1/sensor-c2-seam main

# Cloud agents: same branch names, fresh VM checkout at stack/spirit1/*
```

| Worktree dir | Stack branch | Track | Agent env |
|--------------|--------------|-------|-----------|
| `spirit1-gitnexus` | `stack/spirit1/gitnexus-index` | G5 GitNexus rebuild | **Cloud** |
| `spirit1-docs-traceability` | `stack/spirit1/docs-traceability` | Docs R3/G2/R5 | **Cloud** |
| `spirit1-sensor-c2-seam` | `stack/spirit1/sensor-c2-seam` | G1 SensorC2 seam | **Local + Cloud** |

**Rules:**

- One worktree + stack branch per active track.
- Never two agents on the same file (ownership matrix below).
- **Merge order:** docs → gitnexus → G1 code last (G1 depends on tests + optional post-G5 graph verify).
- `gt sync` + `gt restack` after each stack merge to `main`.
- Cleanup after closeout: `git worktree remove …`; delete merged branches.

## Track ownership

| Track | Sub-track owner | Stack prefix | Agent env |
|-------|-----------------|--------------|-----------|
| **G5 GitNexus** | hindsight-dev-memory-lead or gitnexus-cli skill | `stack/spirit1/gitnexus-index` | **Cloud** |
| **Docs R3/G2/R5** | technical-director + game-designer (docs) | `stack/spirit1/docs-traceability` | **Cloud** |
| **G1 SensorC2 seam** | team-unity + c-sharp-engineer | `stack/spirit1/sensor-c2-seam` | **Local + Cloud** |
| **Coordinator closeout** | c-sharp-devops-engineer | (local `main` or temp closeout branch) | **Local** |

**Dispatch rule:** One agent per track. Every prompt must cite:

- [`spirit1-vertical-slice-gap-analysis-2026-06-05.md`](../../Game-Requirements/reviews/spirit1-vertical-slice-gap-analysis-2026-06-05.md)
- Hard gates (below)
- Remediation log path for per-track `[OUTCOME:]` entries

## File ownership matrix (no cross-track edits)

> **Coordinator-only:** files marked ⛔ — merge conflicts resolved locally, never parallel-edited.

### Track: G5 GitNexus (`stack/spirit1/gitnexus-index`)

| Owns | Does not touch |
|------|----------------|
| `.gitnexus/**` (index artifacts, config) | `src/**`, `unity/**` |
| `production/agentic/spirit1-gap-remediation-log-2026-06-20.md` — **G5 section only** | Docs track markdown outside log |
| Optional: `docs/engineering/gitnexus-index-health.md` (create if missing) | `Game-Requirements/**` |

### Track: Docs R3/G2/R5 (`stack/spirit1/docs-traceability`)

| Owns | Does not touch |
|------|----------------|
| `Game-Requirements/reviews/spirit1-vertical-slice-gap-analysis-2026-06-05.md` — addendum footer only | `src/**`, `unity/**`, `.gitnexus/**` |
| `Game-Requirements/implementation-tracker-2026-06-04.md` — G2 FSM post-MVP row | SensorC2 / adapter code |
| `docs/architecture/**` — R3 SimulationSession frozen-hub note or ADR stub | `SimulationSession.cs` (read-only cite) |
| `production/milestones/vertical-slice-mvp.md` — R5 vocabulary cross-ref | ⛔ `production/sprint-status.yaml` |
| `production/agentic/spirit1-gap-remediation-log-2026-06-20.md` — **Docs section only** | G1/G5-owned paths |

### Track: G1 SensorC2 seam (`stack/spirit1/sensor-c2-seam`)

| Owns | Does not touch |
|------|----------------|
| `unity/ProjectAegis/Assets/Scripts/Runtime/SensorC2PanelHost.cs` | ⛔ `DelegationBridge.cs`, `DelegationBridgeHost.cs` behavior changes beyond feed wiring |
| `unity/ProjectAegis/Assets/Scripts/Runtime/SensorC2HudHost.cs` (if seam extended) | `C2PresentationController.cs`, `PlatformCatalogFilterProjection.cs` |
| `src/ProjectAegis.Delegation.UnityAdapter/Bridge/IC2PresentationFeed.cs` — extend-only | Core `ProjectAegis.Delegation/**` projection logic |
| `src/ProjectAegis.Delegation.UnityAdapter/Bridge/SensorC2Bridge.cs` | `.gitnexus/**` |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/SensorC2*.cs` | `Game-Requirements/**` |
| `src/ProjectAegis.Delegation.Tests/Projection/SensorC2*.cs` | Docs-only files |
| `production/agentic/spirit1-gap-remediation-log-2026-06-20.md` — **G1 section only** | |

### Coordinator-only (⛔)

| File | Reason |
|------|--------|
| `production/sprint-status.yaml` | Sprint program state — not Spirit1 scope |
| `production/agentic/spirit1-gap-remediation-log-2026-06-20.md` — header + closeout | Single writer at merge |
| Merge conflict resolution on any shared path | Local coordinator |

## Hard gates (every merge + closeout)

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal                    # ≥1215 (monotonic; no regression)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter ReplayGoldenSuiteTests -v minimal               # 6/6
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter PlayModeSmokeHarnessTests -v minimal            # 18/18+
```

| Gate | Requirement |
|------|-------------|
| Test baseline | **≥1215** (post-S39); no regression |
| ReplayGolden | **6/6** |
| C2 proxy | **18/18+** |
| Baltic hash | `17144800277401907079` **unchanged** |
| DelegationBridge | **ZERO** touch `DelegationBridge.cs` |
| CatalogWriteGate | **Extend-only** if touched (prefer no touch) |
| GitNexus | `impact()` before symbol edits; `detect_changes()` before commit; G5 tip recorded in remediation log |
| G1 acceptance | `context(SensorC2PanelHost)` shows incoming edge to adapter seam **or** documented `.asmdef` limitation with bridge test citation |

## Merge order

```
W0 baseline (local)
    │
    ├──► W1 parallel: G5 GitNexus │ Docs R3/G2/R5 │ G1 SensorC2 (dev starts; tests local)
    │
    ├──► Merge 1: stack/spirit1/docs-traceability
    ├──► Merge 2: stack/spirit1/gitnexus-index
    └──► Merge 3: stack/spirit1/sensor-c2-seam   ← last (needs full test suite + graph verify)

W2: G4 deferred note → remediation log only (coordinator)
W3: Closeout — full gate + remediation log COMPLETE + Hindsight retain
```

**Rationale:** Docs are markdown-only (low conflict). GitNexus index may shift graph metadata G1 uses for verification. G1 is code + tests — merge last after index stable.

## G4 deferred note (no dispatch)

**G4 — Full platform import** remains **out of MVP cut scope** per gap analysis and `vertical-slice-mvp.md`. Do not open a worktree or agent. Coordinator adds a single **DEFERRED** entry to [`spirit1-gap-remediation-log-2026-06-20.md`](spirit1-gap-remediation-log-2026-06-20.md) referencing tracker req 06 and S40+ catalog track ownership in [`s39-s48-worktree-manifest.md`](s39-s48-worktree-manifest.md).

## Prerequisites (dispatch order)

```bash
# 1. Serial — coordinator (local)
cd /home/username01/cmano-clone/cmano-clone
dotnet test ProjectAegis.sln -v minimal   # record count in remediation log
# Bootstrap worktrees (commands above)

# 2. Parallel dispatch (3 agents)
# G5 Cloud: "Rebuild GitNexus index per G5; verify embeddings>0; log tip. Own .gitnexus only."
# Docs Cloud: "R3 frozen-hub note, G2 FSM tracker row, R5 Vertical Slice vocabulary. Own docs paths only."
# G1 Local+Cloud: "Close G1 via IC2PresentationFeed seam; tests; impact before edit. Own G1 matrix only."

# 3. Serial merge (order above) + closeout gate
# Update spirit1-gap-remediation-log-2026-06-20.md → COMPLETE
# Hindsight retain: dev-cmano-clone bank, [OUTCOME:] per track
```

## Cut line / minimum

**Must ship:** G5 index rebuild verified, Docs R3/G2/R5 merged, G1 seam + tests green, all hard gates PASS, remediation log COMPLETE.

**Optional:** Post-merge `context(SensorC2PanelHost)` screenshot in remediation log; G3 Unity-in-CI remains future work.

## Verification at end of session

- All three active tracks report COMPLETE in remediation log.
- Full gate green (1215+, Replay 6/6, proxy 18/18+).
- G4 DEFERRED note present; G3 explicitly out of scope.
- No edits to `DelegationBridge.cs`; Baltic hash unchanged.
- Worktrees removed or marked idle; stacks merged via Graphite.

---

*Created per dispatching-parallel-agents + spirit1 gap analysis (2026-06-05) + S39 kickoff template. Orthogonal to S40–S48 program.*
