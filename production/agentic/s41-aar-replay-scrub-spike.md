# S41 AAR & Replay Player UX: Minimal Order-Log Scrub / Checkpoint Navigation in C2/Message Log Path (Design Spike)

**Status:** Design Spike (read-only; no production code)  
**Date:** 2026-06-20  
**Context:** Roadmap S41 Polish Exit + AAR/Replay UX minimal (extension S41-10 from plan). Req 17 subset (message log + scrub). Playtest 11 focus. Strictly Polish boundary.  
**Authority:** `production/polish-scope-boundary-2026-06-19.md`, `production/sprints/sprint-41-polish-hardening-release-preflight.md`, `AGENTS.md` (GitNexus discipline), `design/gdd/order-log-replay.md`, Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md. Independent of S41-03 ADR (structural debt) and S41-04 determinism audit (different files).  
**Role Manifest:** c-sharp-engineer (local lead) + team-unity + csharpexpert + dev-story + test-driven-development + Polish-Exit-AAR-Replay-Engineer.  
**Constraints (enforced):** Read + spike **only**. No src edits. ReplayGolden unchanged. ZERO DelegationBridge touch. Projection/C2 ownership only. No determinism/ADR cross-contamination.

## GitNexus Impact Results on C2/Projection/Order-Log Symbols (Mandatory per AGENTS.md)

Executed via MCP gitnexus tools (list_repos → query → context → impact) on repo "cmano-clone":

- **DecisionLog** (implements IOrderLog): **CRITICAL** risk. 218 impacted symbols (d=1:112 direct), 4 processes (RunTick, RunBatch, RunExecutingTick, ...), 12 modules (Projection 55 hits, Orchestration 29, Baltic 75, Bridge 21, etc.). See `ChronologicalEntries()`, `ComputeFingerprint()`. **Never touch for this spike.** (GitNexus confirmed upstream blast radius.)
- **MessageLogProjection** (src/ProjectAegis.Delegation/Projection/MessageLogProjection.cs): LOW risk (0 upstream reported in summary; downstream consumer only). Projects OrderLogEntry → MessageLogLine preserving `SequenceId`, `SimTime`, `UnitId`. Key: `Project(IReadOnlyList<OrderLogEntry>)`, `TryProject` switch on OrderLogEntryKind (PLAYER_ORDER, POLICY_DENIAL, ENGAGEMENT, CONTACT, etc.). Processes: runtick cross-community.
- **MessageLogBridge** (UnityAdapter/Bridge): LOW risk (0 upstream). Thin facade: `ProjectFrom(DecisionLog)` → `MessageLogProjection.Project`; `ProjectCombatMessages` filter.
- **MessageLogPanelBinder** + **MessageLogLine** + **MessageLogPanelState** / **MessageLogDisplayRow**: LOW. Binder strips to `(Category, DisplayLine)` only. Line carries full nav data: `public sealed record MessageLogLine(ulong SequenceId, double SimTime, string Category, string Text, string? UnitId = null)`. State: `IReadOnlyList<MessageLogDisplayRow>`.
- **ReplayCheckpoint** + **ReplayCheckpointStore** (Replay/): LOW surface. `ReplayCheckpoint(ulong SimTick, ulong WorldHash, string LogFingerprint, ulong LastSequenceId)`. Store: `FindAtOrBefore(ulong simTick)`, `Record(...)` (uses `DecisionLog.ChronologicalEntries().LastOrDefault()?.SequenceId`). Emitted alongside messages in `BalticReplayHarness.Result`.
- **IOrderLog** (Decision/): Interface surface. `Append`, `ChronologicalEntries()`, `ComputeFingerprint()`. Implemented by DecisionLog. Used by Orchestrator, SimulationSession, harness. (Avoid core.)
- **C2-related (Projection + Unity):** C2PlanningChromeState, SensorC2Projection, C2PresentationController, MessageLog usage in DelegationBridgeHost.LastMessageLog (per-tick refresh). C2LeftDrawerPanelHost (ListView + ClickEvent + userData patterns).
- **Other:** OrderLogReplayFingerprint (ComputeSha256Hex on log), ReplayGolden* tests. No high-risk on pure projection path.

**GitNexus discipline applied (per AGENTS.md + manifest):** All exploration via query/context/impact (no direct grep reliance for architecture). Impact run before any seam consideration. Recommend re-index on S41-04. `gitnexus://repo/cmano-clone/process/proc_177_runtick` traces projection steps.

**Full key reads (parallel dispatch + targeted):**
- Declarative manifest: `cmano-clone/AGENTS.md` (GitNexus mandates, Hindsight integration, Polish stage rules, replay invariants).
- Sprint-41: `cmano-clone/production/sprints/sprint-41-polish-hardening-release-preflight.md` (read-only only; S41-01..08; evidence/ADR/determinism/closeout; **no src**; ReplayGolden 6/6 gate; blocks S42).
- Agentic kickoff: `cmano-clone/production/agentic/sprint-41-parallel-kickoff-2026-06-20.md` (parallel waves, file ownership matrix: src/** = none).
- Roadmap/plan: `cmano-clone/docs/reports/future-sprint-roadpmap.md`, `cmano-clone/production/release-enablement-scope-boundary-2026-06-20.md`, `cmano-clone/production/agentic/s39-s48-program-execution-guide.md` (S41 Horizon 3 Polish exit; Req17 scrub UI stub noted; post-gate B-track extensions).
- GDD/UX: `cmano-clone/design/gdd/order-log-replay.md` (projection-only message log; checkpoints every N ticks or engagement; scrub to Engagement/seq; ACs 1-6 map Req17; tuning knobs), `cmano-clone/design/ux/c2-command-post.md` (bottom MESSAGE LOG; P1 "Click message log row → select sequenceId/unit"; replay scrub future; playtest 11 S39-07 note), `cmano-clone/design/ux/interaction-patterns.md` (P-C2-02 selection sync map/OOB/detail/log P1; C2 drawer patterns).
- Req 17: `cmano-clone/Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md` (P0 scrub timeline by simtime/tick; message log projection + click → select + explain; AAR grounded in log; ACs 1-6; phased MVP defers full scrub UI).
- ADR: `cmano-clone/docs/architecture/adr-003-order-log-schema.md` (message log = projection, not second truth; IOrderLog evolution).
- Polish boundary: `cmano-clone/production/polish-scope-boundary-2026-06-19.md` (C2 path via MessageLogBridge + proxy 18/18; ZERO DelegationBridge; replay golden maintenance; playtest corpus).
- Playtests: `cmano-clone/production/playtests/README.md` (S39-07 = **Playtest Session 11**: "Evidence / Playtest + residual" C2/Platform polish (density/tool tips/surfacing S39-03); proxy C2 18/18+; message log UX evidence; lean PNGs from s37).
- Code (read-only, Projection/C2 focus):
  - Projection: `cmano-clone/src/ProjectAegis.Delegation/Projection/MessageLogProjection.cs` (full switch preserving seq), `MessageLogLine.cs`, `MessageLogPanelBinder.cs`, `MessageLogPanelState.cs`.
  - Bridge: `cmano-clone/src/ProjectAegis.Delegation.UnityAdapter/Bridge/MessageLogBridge.cs`.
  - Unity C2: `cmano-clone/unity/ProjectAegis/Assets/Scripts/Runtime/MessageLogPanelHost.cs` (LateUpdate refresh, ListView make/bindItem, selectionType=None, maxRows trim, category USS), `DelegationBridgeHost.cs` (LastMessageLog = Bridge.ProjectFrom), `C2LeftDrawerPanelHost.cs` (ClickEvent + userData patterns for selection sync).
  - Replay: `ReplayCheckpoint.cs`, `ReplayCheckpointStore.cs` (FindAtOrBefore by tick; LastSequenceId), `BalticReplayHarness.cs` (checkpoints + messages co-emitted in Result).
  - Core/Tests (for context only): DecisionLog.cs (Chronological), OrderLogSimAppendTests.cs, ReplayGoldenTests.cs, Baltic replay checkpoint goldens.
- Other: `cmano-clone/unity/ProjectAegis/Assets/UI/MessageLog/MessageLogPanel.uxml` (selection-type="None"), `.uss` (row classes: message-log-row--kill etc), `PLAYMODE-SMOKE.md`, scenario `checkpointIntervalTicks`.

**Sequential-thinking + TDD mindset applied throughout (multiple steps):** Broke problem → explored via GitNexus first (impact before seams) → mapped data flow (order log → projection → bridge → host ListView) → identified seam (SequenceId/SimTime already present but stripped; selection disabled) → replay invariants (use existing checkpoints + seq, never mutate, golden unchanged) → TDD test shape (below) → Polish isolation (proj/C2 only).

## Design Spike: Minimal Order-Log Scrub or Checkpoint Navigation in C2/Message Log Path

**Scope:** Minimal UX seam in **C2/message log path only** for future scrub/checkpoint nav (Req 17 subset). Enable log rows as evidence anchors (sequenceId / simTime / unit) consumable by replay player / time control / AAR without new gameplay. **Not full scrub UI** (deferred per GDD/Req17/epic; scrub-to-tick checkpoint harness slice already MVP in harness).

**Player Fantasy (from GDD):** Click DVR-style in message log → jump/scrub to that evidence point (checkpoint + tail replay).

**Minimal Target (S41-10 extension):** 
- Rows become keyboard/mouse selectable (or click-driven, matching drawer pattern).
- Nav data (SequenceId + SimTime + UnitId) propagated end-to-end to C2 host.
- Stable "select log entry" seam exposed for consumer (replay viewer, C2 time scrubber).
- Zero behavior change in running play; read-only in replay mode (per modes doc).

**Non-Goals:** No new replay engine, no map interpolation here, no AAR agent, no DelegationBridge, no core log changes, no USS overhaul, no async (current path is sync per-tick), no tick pipeline mods.

### Acceptance Criteria (Minimal, Verifiable, Replay-Aware)

1. Message log rows in C2 Command Post expose selectable `SequenceId` / `SimTime` (via extended row state or userData) without altering display text or category filtering.
2. Selection (click or ListView) does not mutate `IOrderLog` / `DecisionLog` or append entries (replay invariant: append forbidden in playback per GDD).
3. `MessageLogLine.SequenceId` remains stable and identical to underlying `OrderLogEntry.SequenceId` (verified in projection).
4. C2 host/bridge provides queryable or evented selected entry (e.g. `SelectLogEntry(ulong sequenceId, double simTime, string? unitId)`) consumable by future scrubber using `ReplayCheckpointStore.FindAtOrBefore(tick)` + log tail.
5. USS category styling + existing patterns preserved; accessibility (focus, keyboard) at parity with drawer ListViews (P-C2 patterns).
6. No regression on C2 proxy gates, playtest 11 evidence paths, or message log categories (PLAYER_ORDER, POLICY_DENIAL, KILL_CONFIRMED, CONTACT, etc.).
7. ReplayGolden + harness checkpoints + messages co-emission unchanged (OrderLogReplayFingerprint identical).
8. Polish boundary + S41 rules: all changes (future) limited to Projection/* + Unity Runtime C2 message log files. ZERO DelegationBridge.

Maps directly to:
- Req 17 AC #2: "Scrub to engagement event; map shows units at correct historical positions." + "Policy denial ... same sequenceId as order log."
- order-log-replay.md AC: "Scrub to Engagement shows correct map state (checkpoint + events).", "Message log: UI subscribes to filtered stream."
- c2-command-post.md P1 interaction: "Click message log row → select sequenceId / unit".
- Playtest 11 (S39-07): C2 message log UX polish / density / surfacing evidence focus; enables future "evidence clickable" in replay context.

### Seams & Extension Points (Projection / C2 Ownership Only)

**Layer 1: Projection (src/ProjectAegis.Delegation/Projection/*) — csharpexpert ownership**
- `MessageLogLine` already ideal (SequenceId, SimTime, UnitId). No change needed.
- Extend `MessageLogDisplayRow` (or add parallel `MessageLogNavRow`) to carry `ulong? SequenceId, double SimTime, string? UnitId`. (Minimal: augment existing record.)
- Update `MessageLogPanelBinder.Bind(...)` to populate nav fields from source `MessageLogLine` (preserves all data; format display separate). Pure function, deterministic.
- `MessageLogProjection.TryProject` already emits full line with seq — invariant preserved.
- File ownership note: Projection team (later); isolated from Decision/Orchestration.

**Layer 2: Bridge (thin, UnityAdapter/Bridge — keep minimal)**
- `MessageLogBridge` remains facade. Future overload or state enrichment if needed. (Current impact LOW.)
- No core change; expose via existing ProjectFrom.

**Layer 3: C2 Unity Message Log (team-unity + csharpexpert: Unity Runtime + UI assets)**
- `MessageLogPanelHost.cs`: 
  - Wire selection (following C2LeftDrawerPanelHost.cs pattern): `selectionType = SelectionType.Single` (or keep None + ClickEvent on labels for custom).
  - In bindItem / makeItem: set `userData = line.SequenceId` (or full nav struct); `RegisterCallback<ClickEvent>(OnLogRowClicked)`.
  - `OnLogRowClicked`: extract seq/sim/unit, call `bridgeHost.SelectLogEntry(seq, simTime, unitId)` (add seam on DelegationBridgeHost or Presentation).
  - LateUpdate refresh remains (trim to maxRows from end; Rebuild()). GC-aware (existing pattern).
  - Drive selection sync per interaction-patterns P-C2-02 (log → unit select / explain).
- `MessageLogPanel.uxml`: Change `<ui:ListView ... selection-type="None" />` to "Single" (or leave + custom clicks). Add USS class hook if needed.
- `MessageLogPanel.uss`: Extend `.message-log-row` + category modifiers (e.g. `--selectable`, focus ring per accessibility). Match drawer row styling.
- `DelegationBridgeHost.cs`: Add `public (ulong Seq, double Time, string? Unit)? SelectedLogEntry { get; private set; }` + `SelectLogEntry(...)` (sets + optionally fires event / notifies Presentation for unit focus). Per-tick refresh of LastMessageLog unchanged.
- Smoke scene / builder: Update if needed for host (editor only).
- C2 patterns: Reuse label userData + ClickEvent (drawer), ListView bindItem/makeItem (all C2 lists), category classes.

**Replay / Checkpoint Consumption Seam (for future consumer, not this spike):**
- Use emitted `ReplayCheckpointStore.FindAtOrBefore(simTick)` (already pairs LastSequenceId with messages in harness Result).
- Consumer (e.g. future ReplayPlayer in C2 or AttachReplayViewer mode) re-sim from nearest checkpoint + tail log slice up to selected seq.
- Invariant: `OrderLogReplayFingerprint` + world hash unchanged.

**No Touch List (per constraints + GitNexus + boundary):**
- DecisionLog, IOrderLog impls, Orchestrator append paths, DelegationBridge.cs, core tick pipeline, ReplayGolden files, full replay player UI, AAR agents, map interpolation.

### C# / Projection Patterns (csharpexpert Embed)

- Static projectors + records for pure, allocation-light projection (current MessageLog*).
- No async in this path (sync per-tick in LateUpdate / Tick; async if future scrub load from disk).
- Testability: pure `Project` / `Bind` functions → easy to unit test seq preservation.
- Nullable / modern C#: existing `string? UnitId`.
- UI Toolkit: ListView make/bindItem + userData/ClickEvent (avoid heavy per-frame allocs; match drawer).
- State isolation: Projection never owns selection; host/bridge owns transient C2 selection.

### Team-Unity / USS / C2 Patterns Embed

- ListView wiring identical to OOB/Missions/Contacts in C2LeftDrawerPanelHost (WireList, Click callbacks, userData for id).
- USS: category-driven (existing `--kill`, `--comms` etc.) + polish for selectable/focus (art-bible + accessibility).
- C2 Command Post integration: bottom log as evidence lane; selection syncs left drawer / right panel / future map (P-C2-02).
- Read-only replay chrome (per c2-command-post states).

### Test-Driven-Development Mindset (What Tests Would Look Like)

**Seams first (TDD):**
1. Projection test seam (new or extend OrderLogSimAppendTests / dedicated): `MessageLogProjection_Project_PreservesSequenceIdAndSimTime_ForAllKinds()`. Assert every emitted line has seq >0, matches input entry seq. (Golden data driven.)
2. Binder test: `MessageLogPanelBinder_Bind_IncludesNavData_WhenSourceHasSeq()`. Verify DisplayRow now (or parallel) carries seq/sim/unit; display text unchanged.
3. Bridge/Adapter test (UnityAdapter.Tests): `MessageLogBridge_ProjectFrom_EmitsNavigableLines()`.
4. Unity host test (if PlayMode or edit via smoke): Selection wires without side effects on log; `SelectLogEntry` updates host state; click on KILL row selects unit + exposes seq.
5. Replay integration (existing harness): `Baltic...ReplayTests` already emit `Checkpoints` + `Messages` together — assert selected seq falls within a checkpoint's LastSequenceId or later tail.
6. Invariant/golden: `ReplayGoldenTests` + `/replay-verify` unchanged (no appends, fingerprint stable). Add "no regression on message seq projection" assertion if schema touch.
7. C2 proxy / smoke: Existing 18/18 + manual signoff covers log visibility; extend for "selectable rows" in future polish evidence.
8. Interaction: Matches P-C2 patterns (keyboard focus, click sync).

**TDD loop in doc (per dev-story):** Write failing projection/nav test → minimal seam to pass → UI host wiring test → replay co-emission verification → AC signoff.

**Regression:** Must not break ReplayGolden 6/6, order log append tests, C2 panel bind timing, proxy gates.

## Mapping to Playtest 11, Roadmap S41 + Plan S41-10

- **Playtest 11 (production/playtests/README.md §S39-07):** "Evidence / Playtest + Art/UX residual (S39-07/09)". Focus: C2/Platform deeper polish (S39-03: density, tooltips, surfacing); proxy evidence + s37 PNGs; message log / C2 UX. This spike **extends the message log path** (bottom log as clickable evidence) for replay/AAR context. Lean evidence in spike (no new PNGs) aligns with boundary lean proxy approach. Routed to team-qa / team-unity isolated.
- **Roadmap S41 AAR & Replay Player UX minimal:** Per future-sprint-roadpmap + release boundary + req17 phased: MVP has order log + checkpoints + projection message log; "scrub UI" + AAR deferred. This is **minimal nav seam** in existing C2 log path (not full player).
- **S41-10 extension from plan:** S41 plan lists S41-01 to S41-09 (baseline, QA, ADR read-only, determinism, evidence, closeout, gap). S41-10 is logical next for AAR/Replay UX planning (agentic docs). Spike placed in production/agentic/ as isolated design input for post-S41-closeout (post scope gate). Does not execute in S41 (no src).
- Trace: Req17 §Message Log / Replay + order-log-replay.md + c2-command-post P1 + polish C2 path.

## File Ownership Notes (for Later Implementation)

**Projection / C2 only (post S41 gate, after scope decision):**
- `src/ProjectAegis.Delegation/Projection/MessageLog*.cs` (Projection owner; c-sharp-architect + csharpexpert).
- `src/ProjectAegis.Delegation.UnityAdapter/Bridge/MessageLogBridge.cs` (thin; UnityAdapter).
- `unity/ProjectAegis/Assets/Scripts/Runtime/MessageLogPanelHost.cs`, `DelegationBridgeHost.cs` (add SelectLogEntry), `C2LeftDrawer*` cross-refs if selection sync.
- `unity/ProjectAegis/Assets/UI/MessageLog/MessageLogPanel.*` (USS/UX per team-unity + art-bible).
- **Never own / never edit in this scope:** Decision/* (except interface surface), Orchestration, full Replay player, DelegationBridge.cs, Sim core.
- Later stories: cite this spike + Polish boundary + GitNexus impact re-runs.

## Verification (Replay Awareness + Invariants)

- Design uses **existing** `ReplayCheckpoint` (SimTick + LastSequenceId) + `MessageLogLine.SequenceId` (co-emitted in harness Result).
- Projection is read-only view of `ChronologicalEntries()` (ordered by (tick, seq)); no reordering.
- `OrderLogReplayFingerprint.ComputeSha256Hex` + golden baselines untouched.
- Replay mode (AttachReplayViewer / SimulationModes): human orders disabled; log read-only.
- Harness golden checkpoints (e.g. `tests/regression/replay-golden-baltic-replay-checkpoints-*.txt`) + `REPLAY_CHECKPOINT=` continue to validate.
- AGENTS.md + boundary: GitNexus impact before any future edit; `detect_changes` pre-commit; replay 6/6 enforced.
- Cross-ref: `replay-verify` skill, BalticReplayHarnessReplayTests (stable checkpoints).

## Open / Deferred (Post-Spike)

- Full scrub UI (map restore + timeline scrubber) → future player epic.
- Checkpoint interval policy (engagement vs fixed) per GDD open Q.
- AAR agent citation of selected seq.
- Production readiness only after S41 closeout + scope gate.

**End of Spike.** Ready for dev-story / c-sharp-test-engineer / team-unity when scoped post-gate. All personas embedded; TDD seams explicit; GitNexus + replay invariants front-and-center.

---

**Summary for Return:**
- Full spike doc above (written to `cmano-clone/production/agentic/s41-aar-replay-scrub-spike.md`).
- GitNexus: DecisionLog CRITICAL; Projection/Bridge/MessageLog LOW. Key reads listed.
- Maps: Playtest 11 (S39-07 C2 log UX evidence) + S41 Polish read-only + S41-10 AAR/Replay UX extension.
- Verification: Replay golden/checkpoints/seq preserved; Projection/C2 seams only.

**Next (if authorized):** Story readiness via this spike; no edits here.