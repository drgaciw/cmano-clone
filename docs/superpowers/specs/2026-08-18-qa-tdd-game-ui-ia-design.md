# QA TDD for Game UI Quality and Information Architecture — Design

**Date:** 2026-08-18
**Status:** Draft (design approved in chat; pending spec review)
**Origin:** Brainstorming; deliverable **C** (automated contract/TDD first, then skill/runbook); approach **1** (contract-first wave)
**Notion (tracking only, not a gate):** [QA TDD for game UI quality — Plan](https://app.notion.com/p/3bff7cb4e4df8101bb0efb0caf503a02)
**Related:** `.claude/skills/qa-gauntlet-ui/SKILL.md`, ADR-010 §2–3, ADR-007, ADR-001

## Problem

Automated game-UI pressure (`qa-gauntlet-ui`) already runs a headless UnityAdapter filter (~118), ReplayGolden, and five C2 Play Mode signoffs. Manual UAT stays on `/team-qa` and `/smoke-check`.

What is missing is a **proactive TDD loop for information architecture (IA)**: whether C2 surfaces stay consistent with each other. Highest observed risks:

1. **Selection desync** — map, OOB, contact summary, and unit chrome do not share one contact/unit id.
2. **COMMS triad mismatch** — a surface re-projects comms per frame instead of binding `LastCommsState`.
3. **Planning-phase gating** — a host ignores planning chrome (dim / read-only / Begin Execution clear).
4. **PanelSettings null** — missing settings produce a throw or a live tick instead of an empty Game view.

Today those checks are partial: a few source contracts (`MapPlaceholderPanelHostContractTests`), binder tests (`C2ContactsOverlapTests`, `PlayModeSmokeHarnessTests` Baltic classify selection), and planning projection tests (`C2PlanningChromeTests`). Coverage is not a named, mandatory gauntlet-ui family.

## Solution overview

Two sequenced deliverables (C):

1. **Tests first.** Expand headless binder/projection oracles and Unity Runtime source contracts in `ProjectAegis.Delegation.UnityAdapter.Tests` for the four IA risks. RED → GREEN per family. Presentation client only.
2. **Then skill/runbook.** Fold a dedicated filter token into `qa-gauntlet-ui` / `--mode ui`, add an AAR/manifest row, document failure routing and non-coverage.

No new test harness. No Demo ladder changes. No Play Mode signoff expansion.

## Decisions (locked)

| # | Decision |
|---|----------|
| D1 | Contract-first headless `dotnet test`; Play Mode signoffs remain secondary (existing ×5). |
| D2 | Reuse existing patterns: binder/projection tests + source contracts that read Runtime `.cs` / UXML / USS. No UI Toolkit instantiation in headless tests. |
| D3 | Cover only hosts that participate in the four oracles — not all ~20 PanelHosts. |
| D4 | Wave6 binders are in scope only when they are the bind path for selection, COMMS, or planning. |
| D5 | Every **new** IA test type **must** include `UiIa` in the class name. Do not add new IA assertions only into pre-existing `*ContractTests` files (those already match the broad UI filter but would miss the dedicated IA row). Gauntlet-ui filter **must** add `FullyQualifiedName~UiIa`. |
| D6 | Skill packaging happens **after** the four families are green. |
| D7 | Manual UAT remains `/team-qa` / `/smoke-check`. No second human loop inside gauntlet. |
| D8 | Zero-touch `DelegationBridge.cs` and CatalogWriteGate write paths. Baltic v2 hash `17144800277401907079` unchanged. |

## Scope

**In scope**

- IA oracle tests and host/binder source contracts for selection, COMMS triad, planning-phase gating, PanelSettings-null.
- Named `UiIa` filter token and AAR/manifest row in `qa-gauntlet-ui`.
- Thin runbook: run order, failure → `/qa-gauntlet-remediation` + UCA, what this does not cover.

**Out of scope**

- Rewriting `/qa-gauntlet` ladder prose or `run-gauntlet.sh`.
- Pixel/layout tests, new Play Mode scenes, or extra signoff batches.
- CatalogWriteGate / `DelegationBridge` hotpath edits.
- Notion as runtime authority.

## Architecture

Presentation is a **client** (ADR-010). Oracles assert UI binding and IA consistency against fixtures and source text, not sim authority.

```
BalticReplayHarness (seeded policy)
        │
        ▼
Projections / binders (MapPanelBinder, OobTreePanelBinder,
ContactSummaryProjection, SensorC2PanelBinder,
C2PlanningChromeProjection, LastCommsState feed)
        │
        ▼
UiIa oracle tests (same id / flags / empty path)
        │
        ▼
Source contracts on participating PanelHosts
(must call those binders; must not call Tick / WriteGate /
per-frame CommsStateProjection.Project)
        │
        ▼
qa-gauntlet-ui filter includes FullyQualifiedName~UiIa
```

## Components

| Layer | Role | Location |
|-------|------|----------|
| IA oracle tests | Cross-surface consistency | New classes named `*UiIa*` under `src/ProjectAegis.Delegation.UnityAdapter.Tests/` |
| Host family contracts | Participating PanelHosts bind the oracle paths | New `*UiIa*` classes (siblings to existing contracts). Leave old `*ContractTests` unchanged except bugfix. |
| Gauntlet package | Mandatory filter + AAR row | `.claude/skills/qa-gauntlet-ui/SKILL.md` only |

### Participating surfaces (minimum set)

**Selection:** `MapPlaceholderPanelHost`, `OobTreePanelHost`, `ContactDetailPanelHost`, `RightUnitPanelHost`, `SensorC2PanelHost`, `C2LeftDrawerPanelHost`. Binders already covered by `C2ContactsOverlapTests` / Baltic classify selection remain; new `UiIa` tests close host-source gaps (no second selection store).

**COMMS triad (locked three):** `DelegationBridgeHost.LastCommsState` feed; `MapPlaceholderPanelHost`; `C2TopBarPanelHost`. Existing Globe/`LastCommsState` contracts stay as regression under the broad UI filter, not copied into `UiIa` unless they currently miss the triad. Do not add MessageLog or Attention toast unless they bind `LastCommsState` (they are not the triad).

**Planning (locked hosts):** `C2PlanningChromeProjection` (existing unit tests stay); `UiIa` source contracts for `MapPlaceholderPanelHost`, `C2LeftDrawerPanelHost`, and `C2MenuPanelHost` dim / read-only / Begin Execution clear. Do not invent a second projection.

**PanelSettings-null:** scene builder / host `OnEnable`/`Refresh` — null or missing PanelSettings → empty visual tree or placeholder, no throw, no `BeginExecution` / Tick.

## Data flow and TDD

Two oracle styles — **do not mix in one test method**.

| Style | Input | Asserts | Families |
|-------|--------|---------|----------|
| Binder/projection | `BalticReplayHarness.Run` with fixed seed/policy (`baltic-patrol-classify` unless a family needs planning phase) | Same contact/unit id or chrome flags | Selection, planning projection truth |
| Source contract | Read `unity/ProjectAegis/Assets/Scripts/Runtime/*.cs` (+ UXML/USS when the class name is the bind) | Required tokens present; forbidden APIs absent | COMMS, planning CSS bind, PanelSettings-null |

**TDD order (one family at a time):**

1. Selection sync
2. COMMS triad
3. Planning-phase gating
4. PanelSettings-null
5. Skill filter + AAR row (last)

Each family: write failing `UiIa` oracle → pass in PanelHost / binder / USS only → run the UI filter locally → next family.

Determinism: seeded replay fixtures only. No `DateTime.UtcNow` or `Random.Shared` in new tests.

## Failure routing and floors

| Red gate | Route |
|----------|--------|
| `UiIa` or existing UI filter | `/qa-gauntlet-remediation` (TDD); UCA if Surface is Presentation / C2 / UnityAdapter |
| ReplayGolden | Same; do not retune v2 hash |
| C2 Play Mode signoff | Same; prefer a failing headless oracle first |
| Layout / feel with oracles green | Stop — `/team-qa` / `/smoke-check` |

**Floors**

- `UiIa` families: 0 failures (count may exceed the 118 reference).
- Existing UI filter green; ReplayGolden family + 6/6 subset; PlayModeSmokeHarness ≥23/23.
- Hash `17144800277401907079` present; zero `DelegationBridge.cs` edits.
- `--skip-signoff` → AAR **BLOCKED (signoff)**, not full UI PASS.

## Skill packaging (after tests green)

1. Add `FullyQualifiedName~UiIa` to the hard-coded `dotnet test` filter in `qa-gauntlet-ui`.
2. AAR and `manifest.yaml`: extra row **IA oracles** with N/N; `track: game-ui` unchanged.
3. Document non-coverage: layout/visuals, Demo ladder, manual UAT.
4. Cross-link only from `team-qa-gauntlet` (`--mode ui` / `ui-smoke`). Do not rewrite ladder skill prose.
5. Failures still dispatch `/qa-gauntlet-remediation` + UCA.

## Non-goals (explicit)

- Saboteur/calibration of UI oracles (ladder calibrate is a different skill).
- Expanding the five Editor signoff methods.
- Contracting every PanelHost “for completeness.”

## Success

Four IA families green in headless `dotnet test`; `qa-gauntlet-ui` package includes `UiIa`; skill runbook updated; no DelegationBridge or CatalogWriteGate writes.

## Implementation notes (for the later plan, not this spec’s execution)

GitNexus `impact` before editing existing symbols. Tests land in UnityAdapter.Tests (hybrid layout retained). Do not implement until this spec is reviewed and an implementation plan exists.
