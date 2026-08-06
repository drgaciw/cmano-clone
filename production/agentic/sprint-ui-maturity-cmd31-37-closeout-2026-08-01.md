# UI Maturity Closeout — CMD-31…CMD-37 Parallel — 2026-08-01

**Branch:** `stack/ui-maturity/cmd-31-37-parallel`  
**Base:** `a2c4c49` (main)  
**Kickoff:** `sprint-ui-maturity-cmd31-37-parallel-kickoff-2026-08-01.md`

## Dispatch

| Lane | Branch | Commit | Result |
|------|--------|--------|--------|
| A Command | `stack/ui-maturity/cmd-a-command` | `c26bcad` | CMD-31 + toolbar |
| B Picture | `stack/ui-maturity/cmd-b-picture` | `2206fc9` | CMD-21/32/34 |
| C Contact | `stack/ui-maturity/cmd-c-contact` | `d824fa3` | CMD-17 + CMD-29 |
| D Agent | `stack/ui-maturity/cmd-d-agent` | `071acb0` | CMD-37 |

Merge order: A → C → B → D (all clean; auto-merge only on `DelegationBridgeHost.cs`).

## Verification (RUN+READ)

```
dotnet build ProjectAegis.Delegation + UnityAdapter — 0 errors
dotnet test ProjectAegis.Delegation.Tests — 471/0f
dotnet test ProjectAegis.Delegation.UnityAdapter.Tests — 398/0f
dotnet test ProjectAegis.Sim.Tests — 344/0f
tools/copy-delegation-assemblies.sh — plugin DLL guardrail green
```

**Invariants:** `DelegationBridge.Tick` body not rewritten; CatalogWriteGate untouched; OrderKind only append (`SetEmcon`, `SetSensors`).

## Delivered surfaces

| CMD | Deliverable |
|-----|-------------|
| CMD-31 | `C2CommandIssuance` + `C2PlayerCommandBridge` + `TryIssuePlayerCommand` + order log path |
| CMD-16 | `UnitOrderToolbarHost` + UXML/USS |
| CMD-21/34 | `TacticalOverlayProjection` / envelope rings + map apply counts |
| CMD-32 | `DatalinkPictureProjection` |
| CMD-17 | `UnitCommsDisplay` → `UNKNOWN (Out of comms)` |
| CMD-29 | `ContactDetailProjection` + `ContactDetailPanelHost` |
| CMD-37 | `AgentRosterProjection` + directives + `AgentRosterPanelHost` |

## Not in this slice (still backlog)

- Live editing contract (CMD-35), perf benchmark (CMD-36), doctrine map viz (CMD-33 deep)
- Full scenario library (CMD-27), Air Ops Phase A (CMD-24), basemap layer model (CMD-28.2)
- Unity scene wiring of new hosts into DelegationSmoke (add components in Editor)
- Formal Approved asset path

## Follow-ups

1. Attach new UIDocuments in play-mode scenes for toolbar / contact / agent panels.
2. Wire real catalog weapon ranges into `TacticalOverlayProjection` callers.
3. Feed unit-pair datalink edges from sim/registry into map host (count currently 0 until feed exists).
4. Linear issues for remaining P0/P1 CMD rows.

---
*Closeout UI Maturity parallel 2026-08-01.*
