# Track A — Live telemetry / message log (2026-08-17)

**Owner:** Track A (this session)  
**Audit:** [2026-08-17-playmode-visual-audit.md](2026-08-17-playmode-visual-audit.md)  
**Reqs:** CMD-05 (message log projection), RPL-14 (projection-only feed)  
**File-disjoint from:** DRG-162 overlay, toast/compression hosts, Track C VFX

---

## Root cause

Play Mode looked dead because two layers stacked:

1. **Thin projection.** `MessageLogProjection.TryProject` mapped CONTACT / MAGAZINE / MODE / combat / COMMS / FUEL / PLAYER_ORDER and fell through to `null` for `PolicyUpdate`, `AgentDecision`, `MissionTransition`, `EventFired`, and `PlatformDamageChange`. Doctrine overrides and orchestrator ROE snapshots already wrote `PolicyUpdate` rows; the HUD never showed them.
2. **Stub feed.** `SimplePlayModeSimHost` ticks a snapshot with `ActiveEngagementCount => 0`. `PlayModeSmokeOrbatSeeder.SeedDecisionLog` appended only two CONTACT rows + one MAGAZINE row. Baltic classify/engage firehose lives in `BalticReplayHarness` (`dotnet test`), not Editor Play Mode.

`DelegationBridgeHost.LastMessageLog` already binds `MessageLogBridge.ProjectFrom` — no Tick rewrite required. Once kinds project and the seeder emits them, the Unity strip updates.

---

## GitNexus impact (pre-edit)

| Symbol | Direction | Risk | Notes |
|--------|-----------|------|-------|
| `MessageLogProjection` | upstream | **LOW** | Index reported 0 direct callers (stale); known callers: `MessageLogBridge`, `BalticReplayHarness`, tests. Additive switch arms only. |
| `MessageLogCategoryClassMap` | upstream | **LOW** | USS class lookup; existing `--policy` / `--mission` / `--kill` reused. |
| `PlayModeSmokeOrbatSeeder` | upstream | **LOW** | Play Mode + headless smoke only. Not on Baltic replay golden path. |
| `SimplePlayModeSimHost` | upstream | **LOW** | Post-`RunTick` feed advance only. |

**Not edited:** `DelegationBridge` Tick / hotpath, `CatalogWriteGate`, toast hosts, `C2TopBarPanelHost`, VFX, kinematics, map overlay.

Replay golden hash `17144800277401907079` untouched (seeder is Play Mode-only; projection does not mutate `DecisionLog`).

---

## Changes

### 1. `MessageLogProjection` — player-facing kinds

| `OrderLogEntryKind` | Category | Justification |
|---------------------|----------|----------------|
| `PolicyUpdate` | `POLICY_UPDATE` | CMD-05 / doctrine + mission ROE (audit primary miss) |
| `AgentDecision` | `AGENT_DECISION` | Live delegation choices (payload + legacy `DecisionRecord`) |
| `MissionTransition` | `MISSION` | Mission phase (USS `--mission` already existed) |
| `EventFired` | `EVENT` | Scenario cue / recon events |
| `PlatformDamageChange` | `DAMAGE` | HP% combat telemetry |
| `ControllerChange` | `CONTROLLER` | Human ↔ agent handoff |

Still dropped: `GroupMemberDetach` / `GroupMemberRejoin` (AAR group bookkeeping, not CMANO radio traffic).

### 2. Category tint (existing USS only)

`POLICY_UPDATE` → `--policy`; `EVENT` → `--mission`; `DAMAGE` → `--kill`. No new USS files (toast/VFX disjoint).

### 3. Play Mode DecisionLog feed (no Tick rewrite)

`PlayModeSmokeOrbatSeeder.SeedDecisionLog` now seeds 8 rows (was 3): CONTACT×2, MAGAZINE, POLICY_UPDATE (ROE), MISSION START, EVENT recon-detect, AGENT_DECISION Hold, DAMAGE 100→85.

`AdvanceDecisionLog(log, simTime)` appends more at 5 / 8 / 12 / 15 / 20 / 30 sim-seconds (EMCON, ON_STATION, classify cue, Engage decision, second hit, maxSalvo). Idempotent per threshold. Called from `SimplePlayModeSimHost.Update` **after** `RunTick` — does not enter `DelegationBridge.Tick`.

---

## Tests

| Suite | Result |
|-------|--------|
| `dotnet test …Delegation.Tests --filter FullyQualifiedName~MessageLog` | **33/33** |
| `dotnet test …UnityAdapter.Tests --filter PlayModeSmokeHarnessTests` | **24/24** (was 23; + feed-growth test) |

New / extended cases:

- `PolicyUpdate_projects_field_transition` (sequenceId identity)
- `AgentDecision_mission_event_and_damage_project_player_facing_lines`
- `Group_member_rows_remain_unprojected`
- Smoke seed asserts `Count > 3` plus POLICY_UPDATE / MISSION / EVENT / AGENT_DECISION / DAMAGE
- `Smoke_decision_log_feed_grows_with_sim_time_without_bridge_tick`

---

## Remaining gaps (not this track)

| Gap | Why still open |
|-----|----------------|
| Editor Game View pixel signoff | Headless/cloud; visual ACs stay UNKNOWN until owner runs Play Mode |
| Live Baltic engage in Editor | Stub host still has `ActiveEngagementCount => 0`; full firehose remains `BalticReplayHarness` |
| PlayerInfoFilter fog on AgentDecision | Projection stays full-log (RPL-14). Seeded decisions use `Assisted` so they survive if fog is wired later |
| Toast / auto-pause / CMD-04 compression | Sibling toast/compression ownership |
| Weapon tracks / VFX | Track C; art-bible §7 N/A |
| Hash-placed map icons | Track B / CMD-38 |
| `maxRows = 12` on `MessageLogPanelHost` | Older seed lines scroll off; not a projection bug |
| Group detach/rejoin in HUD | Intentionally omitted |

**CMD-05 / RPL-14 after this track:** projection covers the high-value kinds the audit named; Play Mode seed + time-gated feed is richer than three lines. Residual is stub-vs-Baltic authority, not dropped categories.
