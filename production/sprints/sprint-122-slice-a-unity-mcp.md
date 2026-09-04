# Sprint 122 — Slice A C2 visual bind + Unity-MCP evidence

**Dates:** 2026-09-08 to 2026-09-12  
**Predecessor:** S121 residual-scope ([DRG-156](https://linear.app/drgamtd-workspace/issue/DRG-156) Done; overlay DRAW/CESIUM/PLAY tickets Done)  
**Epic:** [DRG-178](https://linear.app/drgamtd-workspace/issue/DRG-178) Slice A — Find, Fix, Track, Target  
**Stage:** **Release** · **Not Launch** · **Not Phase N**  
**Review mode:** lean (`production/review-mode.txt`)  
**Unity-MCP:** [PR #618](https://github.com/drgaciw/cmano-clone/pull/618) · `/team-unity` MCP gate  
**Doctrine:** ADR-010 / 007 / 001 · unity-csharp-architect pr-finish on presentation

> **Policy:** Notion Hub (2026-09-04) said “UI, Skills, Unity stay parked. No S122.” This sprint **un-parks Unity** because Grok Unity-MCP is now wired. Do not rebuild CMD-32/34 projections. Do not touch `DelegationBridge` hotpath.

> **QA plan:** `production/qa/qa-plan-sprint-122-2026-09-04.md` (written 2026-09-04; `/qa-plan` un-parked for S122). Gauntlet: `/team-qa-gauntlet --mode ui` + `/qa-gauntlet-combat-ui` (Slice A gates). Signoff ×5 still required for S122-05 Complete.

## Sprint Goal

Bind Slice A contact-quality C2 in Unity (provenance / chain / network health) and prove it with Unity-MCP Play Mode screenshots — without touching `DelegationBridge` or rebuilding CMD-32/34 projections.

## Capacity

| | Value |
|---|---|
| Total days | 5 (Mon–Fri) |
| Buffer (20%) | 1 day (Editor flakiness / `:8080` down) |
| Available wall-clock | 4 days |
| Agent-days (max points) | **~12** (3 file-disjoint worktrees + 1 serial Editor MCP lane) |
| Must-have SP | 18 (original) + **5** MCP follow-up (S122-12/13) = **23** |
| Should-have SP | 13 + **2** (S122-14) = **15** |
| Nice-to-have SP | 8 + **2** (S122-15) = **10** |

**Unity Editor lock:** one Play Mode / MCP mutation stream. Headless C# and tests do not share that lock.

## Sources

| Source | Finding |
|---|---|
| Linear | In progress: DRG-178, [DRG-180](https://linear.app/drgamtd-workspace/issue/DRG-180). Todo: [DRG-208](https://linear.app/drgamtd-workspace/issue/DRG-208). Slice A/B headless **DRG-206–230 Done**. Remaining points are UI bind. |
| Notion Hub | Headless Combat UX parked Unity. Design: [Unity UI Maturity Plan](https://www.notion.so/3abf7cb4e4df812f8128c528ce3f3f7b). |
| GitNexus | C2 selection flows through `DelegationBridgeHost` (do not edit). `ContactDetailPanelHost` already has confidence/provenance/staleness label names. `CommsStateProjection` is headless-ready. |
| Repo | C2 zone hosts shipped; quality cues not proven in Play Mode. |

## Agent / subagent / skill routing

```
Orchestrator
├── Wave 0  /team-unity          Editor MCP live (serial)
├── Wave 1  3 worktrees, parallel, file-disjoint
│     A  /team-csharp + /c-sharp-engineer + /c-sharp-test-engineer
│        ContactDetailPanelHost + tests  (DRG-180)
│     B  /team-csharp + TDD
│        sensor-to-shooter surface  (DRG-181)
│     C  /team-csharp + TDD
│        C2 network-health HUD  (DRG-190) — Should Have if A/B slip
├── Wave 1b /team-unity (serial, after each WT lands C#)
│     Unity-MCP: assets-modify UXML/USS → scene-save
│     screenshot-game-view + console-get-logs
├── Wave 2  /team-unity + /qa-gauntlet-ui
│     DRG-208 Play Mode re-signoff + overlay count labels
└── Wave 3  /qa-gauntlet-remediation (only on fail) + /story-done
            then REMIND: /qa-plan sprint (parked)
```

**Always:** GitNexus `impact()` before C#; `detect_changes()` before commit; `/team-unity` MCP probe (`curl :8080`) before any `.unity` / `.uxml` work.

**Never:** `DelegationBridge.Tick`; CatalogWriteGate; rebuild `TacticalOverlayProjection` / `DatalinkPictureProjection`; two Unity Editors on one `unity/ProjectAegis`.

## Tasks

### Must Have (Critical Path) — 18 SP

| ID | Task | Linear | Agent/Owner | Est. Days | SP | Dependencies | Acceptance Criteria |
|----|------|--------|-------------|-----------|----|--------------|-------------------|
| S122-00 | Unity-MCP live on this machine | PR #618 | Orchestrator + `/team-unity` | 0.5 | 2 | Editor 6000.3.22f1 | `./tools/pin-unity-mcp-8080.sh`; `:8080` 2xx; `ping` / `unity-tool-list` work; if down, stop visual lanes |
| S122-01 | Contact quality bind | [DRG-180](https://linear.app/drgamtd-workspace/issue/DRG-180) | WT-A `/c-sharp-engineer` + `/c-sharp-test-engineer`; Editor `/team-unity` | 1.5 | 5 | S122-00 for visual | Source / confidence / age / last-known / out-of-comms unknown; non-color cues; TDD on host; MCP Game View screenshot |
| S122-02 | Sensor-to-shooter chain UI | [DRG-181](https://linear.app/drgamtd-workspace/issue/DRG-181) | WT-B same skills | 1.5 | 5 | Headless DRG-207 Done; file-disjoint from ContactDetail | Chain visible from contact; deep-link to DRG-180; headless contract test + MCP screenshot |
| S122-03 | Overlay count labels in map UXML | S121 leftover | Editor `/team-unity` MCP `assets-modify` | 0.5 | 2 | No projection rewrite | `envelope-ring-count` / `datalink-edge-count` present; MCP screenshot of counts; no new overlay API |
| S122-04 | Play Mode re-signoff | [DRG-208](https://linear.app/drgamtd-workspace/issue/DRG-208) | Serial Editor `/team-unity` | 1.0 | 3 | S122-00, after 01–03 bind | MCP `screenshot-game-view` + `console-get-logs` evidence pack (or 5 C2 signoffs); 0 console errors |
| S122-05 | UI gauntlet AAR | `/qa-gauntlet-ui` | QA subagent | 1.0 | 3 | S122-01..04 | `production/qa/gauntlet/gauntlet-<ts>-ui/` AAR; 0 failures on UI/C2 filter; failures → `/qa-gauntlet-remediation` |

### Should Have — 13 SP

| ID | Task | Linear | Agent/Owner | Est. Days | SP | Dependencies | Acceptance Criteria |
|----|------|--------|-------------|-----------|----|--------------|-------------------|
| S122-06 | C2 network health HUD | [DRG-190](https://linear.app/drgamtd-workspace/issue/DRG-190) | WT-C `/c-sharp-engineer` | 1.0 | 5 | Headless DRG-214 Done | Partition / degrade / graceful-fail shown; not a sim owner |
| S122-07 | ROE / authority chrome | [DRG-182](https://linear.app/drgamtd-workspace/issue/DRG-182) | `/team-ui` + `/team-csharp` | 1.0 | 5 | Headless DRG-209 Done | Authority + ROE readable on selected unit; MCP screenshot |
| S122-08 | Engagement explain bind | [DRG-168](https://linear.app/drgamtd-workspace/issue/DRG-168) | `/team-unity` on `EngageExplainPanelHost` | 1.0 | 3 | Headless DRG-215 Done | Deep-link from DRG-180; Slice B UI start only |

### Nice to Have — 8 SP

| ID | Task | Linear | Agent/Owner | Est. Days | SP | Dependencies | Acceptance Criteria |
|----|------|--------|-------------|-----------|----|--------------|-------------------|
| S122-09 | Slice A targetability harness UI | [DRG-183](https://linear.app/drgamtd-workspace/issue/DRG-183) | `/qa-gauntlet-ui` | 1.0 | 3 | S122-01..02 | Scenario + AC harness documented; not a new sim |
| S122-10 | Missing Unity `.meta` GUIDs on main | asset hygiene | `/team-unity` — copy GUIDs from `origin/feat/unity-c2-playmode-picture`, never regenerate | 0.5 | 2 | GitNexus | Metas match feature-branch GUIDs |
| S122-11 | C2 nodes visual | [DRG-189](https://linear.app/drgamtd-workspace/issue/DRG-189) | `/team-ui` | 1.0 | 3 | Headless DRG-213 Done | Read-only node list; no new architecture |

## MCP follow-up (2026-09-04 probe `slice-rec-2`)

Fresh Unity-MCP Play Mode on `DelegationSmoke` (Editor 6000.3.22f1, **29** roots, `ping slice-rec-2`):

| Probe | Finding |
|---|---|
| TopBar | `NET: HEALTHY` with `COMMS: NOMINAL` (S122-06 live) |
| Right rail | Contact Detail `c1` + Sensor To Shooter `CHAIN: BROKEN` stacked |
| Left CONTACTS | Still **No contacts** while right rail shows `CONTACT: c1` |
| Unit ROE/AUTH | `ROE: —` / `AUTH: —` — smoke `ScenarioPolicy` / `LastUnitDetail` null at paused t=0 |
| EngageExplain | GO in scene; panel `ENGAGE: —` (deep-link `EXPLAIN: engage/c1` is on contact/chain only) |
| C2 Menu | Honest empty-state `No C2 nodes — no mission package bound` |
| `CombatDomains` | **Not in scene** (`gameobject-find` 500) — carry to S123 |
| Console | 0 Error / 0 Exception on this Play enter |
| Heap | Mono ~1.2 GB idle — 29 UIDocuments (AirOps/BoatOps/DeckHangar/GroundOps still loaded) |

Do **not** add a 30th UIDocument. Slice B/C UI is **S123**, not this sprint. Slice C ([DRG-185](https://linear.app/drgamtd-workspace/issue/DRG-185)) stays Backlog.

### Must Have follow-up — +5 SP

| ID | Task | Linear | Agent/Owner | Est. Days | SP | Dependencies | Acceptance Criteria |
|----|------|--------|-------------|-----------|----|--------------|-------------------|
| S122-12 | Smoke policy + unit AUTH/ROE | [DRG-182](https://linear.app/drgamtd-workspace/issue/DRG-182) leftover | `/team-csharp` + `/team-unity` | 0.5 | 3 | S122-07 formatters | `SelectUnit(u1)` shows `ROE:` + `AUTH: Permitted\|Withheld` (not `—`); MCP screenshot + host probe. No `DelegationBridge.Tick`. |
| S122-13 | CONTACTS list honesty | [DRG-180](https://linear.app/drgamtd-workspace/issue/DRG-180) leftover | `/team-unity` | 0.5 | 2 | S122-01 | Left CONTACTS lists `c1` whenever Contact Detail shows `CONTACT: c1`. MCP Game View. |

### Should Have follow-up — +2 SP

| ID | Task | Linear | Agent/Owner | Est. Days | SP | Dependencies | Acceptance Criteria |
|----|------|--------|-------------|-----------|----|--------------|-------------------|
| S122-14 | Hide unused Wave4/5 smoke hosts | heap | `/team-unity` | 0.5 | 2 | S122-00 | Smoke hides `AirOps` / `BoatOps` / `DeckHangar` / `GroundOps` / `MagazineLoadout` (and peers) unless the scenario needs them. No new GOs. Note Mono heap. |

### Nice to Have follow-up — +2 SP

| ID | Task | Linear | Agent/Owner | Est. Days | SP | Dependencies | Acceptance Criteria |
|----|------|--------|-------------|-----------|----|--------------|-------------------|
| S122-15 | Live C2 nodes if package exists | [DRG-189](https://linear.app/drgamtd-workspace/issue/DRG-189) leftover | `/team-ui` | 0.5 | 2 | S122-11 | C2 Menu shows read-only `c2-node-*` rows **iff** a real `MissionPackageSnapshot` is on the presentation feed; else keep `No C2 nodes — no mission package bound`. Do not fake Baltic. |

S123 (2026-09-15): Slice B bind + `CombatDomains` scene-save. See `production/sprints/sprint-123-slice-b-mcp.md`.

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|-------------|
| S121 overlay visual (DRAW Done in Linear; residual sprint did not prove canvas) | Verify with MCP rather than rebuild | S122-03 + S122-04 |
| DRG-180 In Progress since 2026-08-19 | UI parked; host labels exist, Play Mode proof does not | S122-01 1.5d |
| DRG-208 Play Mode re-signoff | Was not agent-dispatchable until MCP | S122-04 1.0d |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| `:8080` down / Cloud hashed port | High without pin | Visual lanes stall | S122-00 first; headless tests still land; 1d buffer |
| Unity Editor lock serializes UXML | Medium | Parallel C# waits on screenshots | C# in worktrees; Editor only after compile green |
| Touching `DelegationBridgeHost` selection path | Medium | CRITICAL GitNexus | Bind through existing presentation feed only |
| Wrong `.meta` GUIDs | Medium | Broken refs | Copy GUIDs from feature branch, never regenerate |

## Dependencies on External Factors

- Unity Editor **6000.3.22f1**; Custom MCP `:8080`
- Merge or usable checkout of [PR #618](https://github.com/drgaciw/cmano-clone/pull/618)
- Linear DRG-178 remains the epic; do not open Slice C ([DRG-185](https://linear.app/drgamtd-workspace/issue/DRG-185))
- No Launch / Phase N

## Definition of Done for this Sprint

- [x] All Must Have tasks completed (00–05 + 12–13)
- [x] Must Have tasks pass acceptance criteria (Should/Nice 14–15 still backlog)
- [x] QA plan exists (`production/qa/qa-plan-sprint-122-2026-09-04.md`)
- [x] Logic/Integration stories have passing unit/integration tests (`src/ProjectAegis.*.Tests`)
- [x] Smoke check passed (`production/qa/smoke-2026-09-04.md`)
- [x] QA sign-off: APPROVED WITH CONDITIONS (`/qa-gauntlet-ui` PASS including signoff ×5; `/team-qa` human UAT not a second loop)
- [x] No S1 or S2 bugs in delivered Must Have features
- [x] Design documents updated for any deviations (EngageExplain panel body leftover S123-01; S122-14 heap)
- [x] Code reviewed and merged — lean: `/code-review` skipped at user `/story-done` close; `DelegationBridge.cs` untouched
- [x] MCP evidence: Game View shots + console log for Must Have visual stories
- [x] ZERO `DelegationBridge` hotpath edits

## Next steps

1. Should Have remaining: S122-06–08 (in review) and S122-14 (backlog) if pulling more work
2. Nice: S122-09/11 in review; S122-10/15 backlog
3. Do not start Slice B implementation from this close unless a later sprint plan is explicitly started
4. Sprint close-out sequence if stopping at Must Have: `/team-qa sprint` (human UAT) → `/retrospective` → `/sprint-plan new`

## QA Test Cases

Canonical plan: [`production/qa/qa-plan-sprint-122-2026-09-04.md`](../qa/qa-plan-sprint-122-2026-09-04.md). Types inferred (no per-story `Type:` headers).

| ID | Automated | Gauntlet / MCP |
|----|-----------|----------------|
| 00 | Pin + `ping` | MCP preflight |
| 01 | `ContactDetailProjectionTests` | `s122-01/` shot; gauntlet-ui |
| 02 | `SensorToShooterApplyStateTests` | `s122-02/` shot; gauntlet-ui + combat-ui |
| 03 | `MapOverlayCountUxmlContractTests` | `s122-03/` ENVELOPES/DATALINKS |
| 04 | Evidence pack / signoff ×5 | 0 C2 console errors |
| 05 | UI/C2 ≥118, UiIa ≥11, ReplayGolden ≥6 | `/team-qa-gauntlet --mode ui`; AAR |
| 06 | `C2NetworkHealthHudApplyTests` | `NET:` on TopBar |
| 07 | `UnitDetailRoeAuthorityApplyStateTests` | `ROE:` / `AUTH:` |
| 08 | `EngageExplainPanelHostContractTests` | Deep-link; panel body leftover S123-01 |
| 09 | `FullyQualifiedName~TargetabilityAccept` | Docs only; not a new sim |
| 10 | GUID copy, never regenerate | — |
| 11 | `C2MenuProjectionTests` + host contract | Honest empty-state |
| 12 | Extend AUTH tests + smoke | `s122-12/unit-roe-auth.png` |
| 13 | Contact-list contract | `s122-13/contacts-list-c1.png` |
| 14 | Host visibility (optional) | Heap note |
| 15 | Snapshot vs empty-state | No fake Baltic |

Red gates → `/qa-gauntlet-remediation` (+ UCA on presentation). GitNexus CRITICAL → `/qa-gauntlet-agentic-resilience`. Ladder/forge/stress **out** of S122 Must Have.

## Completion Notes

**Completed**: 2026-09-04
**Must Have closed**: S122-00, 01, 02, 03, 04, 05, 12, 13
**Criteria**: Must Have ACs covered by headless tests + MCP shots + gauntlet-ui (145/11/17 + signoff ×5 PASS) + combat-ui (22/22, CombatDomains GO present)
**Deviations (advisory)**: EngageExplain panel body still `ENGAGE: —` (S123-01 leftover); Wave4/5 hosts still loaded (S122-14); live C2 node rows only if package feed exists (S122-15)
**Test Evidence**: `production/qa/evidence/s122-12/unit-roe-auth.png`, `production/qa/evidence/s122-13/contacts-list-c1.png`, `production/qa/smoke-2026-09-04.md`, `production/qa/gauntlet/gauntlet-20260904-1450-ui/`, `production/qa/gauntlet/gauntlet-20260904-1450-combat-ui/`
**Code Review**: Skipped — lean mode; user approved close without `/code-review`

---
*S122 opened 2026-09-04. MCP follow-up `slice-rec-2` added 2026-09-04. QA plan 2026-09-04. Must Have closed 2026-09-04. Stage **Release**. Not Launch.*
