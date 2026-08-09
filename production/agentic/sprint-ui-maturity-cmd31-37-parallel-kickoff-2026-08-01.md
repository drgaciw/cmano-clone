# UI Maturity Parallel Kickoff — CMD-31…CMD-37 — 2026-08-01

**Branch:** `stack/ui-maturity/cmd-31-37-parallel`  
**Program:** Unity UI Maturity Plan (Notion) — C2 command surface + tactical picture  
**Stage:** Release. No Launch. Zero `DelegationBridge` hotpath edits (Tick path). CatalogWriteGate untouched.

## Lanes (surface-disjoint)

| Lane | CMDs | Surface (allowed) | Forbidden |
|------|------|-------------------|-----------|
| **A Command** | CMD-31, CMD-16 shell | `Delegation/Input/*` (new), `Core/Order.cs` (enum append only), `UnityAdapter/Bridge/*Command*` (new + thin bridge methods), `Unity …/UnitOrderToolbar*`, tests under `Delegation.Tests/Input` | MapPanel*, Contact*, Agent*, Catalog*, Sim hotpath |
| **B Picture** | CMD-21, CMD-32, CMD-34 | `Projection/*Envelope*`, `*Datalink*`, `*TacticalOverlay*`, `MapPanelApplyState` (additive only), `MapPictureProjection` (additive), `MapPlaceholderPanelHost` overlays, tests | Order.cs, PlayerOrder*, ContactDetail*, Agent* |
| **C Contact+Comms** | CMD-17, CMD-29 | `ContactDetail*`, `ContactPictureEntry` (additive), `UnitDetailProjection` status/comms labels only, `UnitDetailApplyState` additive, UnitDetail UXML labels, tests | Map overlays, Agent*, OrderKinds |
| **D Agent** | CMD-37 | `AgentRoster*`, `AgentDirective*`, Unity `AgentRosterPanelHost`, tests | Map*, ContactDetail*, Order.cs |

## Standing gates

- [ ] `dotnet build ProjectAegis.sln` 0 errors
- [ ] `dotnet test` ≥ baseline, 0 fail (or scoped test projects green if full suite too heavy)
- [ ] ReplayGolden 6/6 if full suite run
- [ ] No `DelegationBridge.Tick` body rewrite
- [ ] Disabled controls state a reason string

## Acceptance (program slice)

1. **CMD-31:** Player can issue Hold / RTB / Move / EMCON-style orders via issuance facade → order log `PlayerOrder` + human queue; invalid selection returns reason.
2. **CMD-21/34:** Selected-unit envelope rings appear as projection rows; apply-state exposes ring count.
3. **CMD-32:** Datalink edges project from catalog/link state for map host.
4. **CMD-17:** Unit status can show `UNKNOWN (Out of comms)` when comms denied/degraded per projection contract.
5. **CMD-29:** Contact detail presentation distinct from unit detail (classification, detector, BDA).
6. **CMD-37:** Agent roster presentation + directive issuance hook (enqueue path reused).

## Merge order

A → C → B → D (command facade first; contact uses unit labels; map last before agent chrome).

---
*Kickoff UI Maturity CMD-31–37.*
