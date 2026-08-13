# Sprint 120 — C2 command issuance residual-scope (DRG-155)

**Status:** **In Progress** — residual-scope audit/docs only; **not** a new CMD-31 pipeline  
**Dates:** 2026-08-13  
**Predecessor:** S119 hygiene **COMPLETE** ([DRG-154](https://linear.app/drgamtd-workspace/issue/DRG-154) Done · [#485](https://github.com/drgaciw/cmano-clone/pull/485); evidence [`production/qa/s119-hygiene-2026-08-12.md`](../qa/s119-hygiene-2026-08-12.md))  
**Linear:** [DRG-155](https://linear.app/drgamtd-workspace/issue/DRG-155) — **In Progress**  
**Epic:** [DRG-149](https://linear.app/drgamtd-workspace/issue/DRG-149) — In Progress  
**Stage:** **Release** · **Not Launch**  
**Kickoff:** [`production/agentic/sprint-120-parallel-kickoff-2026-08-13.md`](../agentic/sprint-120-parallel-kickoff-2026-08-13.md)  
**Doctrine:** ADR-010 / 007 / 001 · unity-csharp-architect pr-finish (cite only — no C# this sprint)

> **Residual-scope only. Do not rebuild CMD-31. Do not touch DelegationBridge hotpath.**  
> CMD-31…36 core already landed in **S108** (`C2CommandIssuance`, `C2PlayerCommandBridge`, Unity toolbar, headless tests). Confirm and document gaps — do not re-implement.

## Goal

Residual-scope **audit/docs only**. Confirm the existing **S108** issuance path is the product path; list leftover docs/tests gaps. **No new command pipeline.** CMD-31…37 core already landed in S108 (`C2CommandIssuance`, `C2PlayerCommandBridge`, Unity toolbar, tests).

## Must Have

| ID | AC |
|----|-----|
| S120-01 | Publish residual-scope kickoff (`sprint-120-parallel-kickoff-2026-08-13.md`) — **docs-only surfaces; no C# lanes** |
| S120-02 | Residual inventory of the landed S108 issuance path + leftover docs/tests gaps (this plan). Confirm no rebuild. |

## Should Have (optional tiny doc hygiene only)

| ID | AC |
|----|-----|
| S120-03 | Tracker/doc honesty: DRG-155 / Hub language says residual-scope, not “rebuild CMD-31”. Do not invent a new issuance API. | **Done** — S119 DoD checkboxes + S120 residual-scope headers aligned to merged reality |

## Landed path (confirm — do not rewrite)

S108 #382/#390 + closeout [`sprint-108-closeout-2026-08-04.md`](../agentic/sprint-108-closeout-2026-08-04.md):

```text
UnitOrderToolbarHost / DelegationBridgeHost
  → C2CommandIssuance.Validate / TryResolve   (pure; no side effects)
  → C2PlayerCommandBridge.TryIssue            (reasons + HumanController gate)
  → DelegationBridge.TryEnqueueHumanOrder     (existing enqueue; not Tick)
  → DecisionLog.PlayerOrders
```

Thin `DelegationBridge.TryIssuePlayerCommand` is a **non-Tick** wrapper already on trunk. **Out of this sprint.**

Tests already on trunk:

| Assembly | Fixture | Covers |
|----------|---------|--------|
| `Delegation.Tests` | `Input/C2CommandIssuanceTests` | known ids, aliases, unknown, no selection, OrderKind ordinals |
| `UnityAdapter.Tests` | `Bridge/C2PlayerCommandBridgeTests` | enqueue Hold/EMCON/sensors/plot_course; UNKNOWN_COMMAND / UNKNOWN_UNIT / NOT_HUMAN_CONTROL / REPLAY_ATTACHED |

## Residual inventory (leftovers — not a new pipeline)

| Item | Status | S120 disposition |
|------|--------|------------------|
| CMD-31 resolve + enqueue + toolbar | **Landed S108** | Confirm only. **Do not rebuild.** |
| Headless issuance tests | **Present** (table above) | No new test classes this sprint |
| Play Mode sign-off row 2 (`UnitOrderToolbar` issue/refuse) | Checklist exists; human Editor box still open | Document gap; **do not** add C# / PlayMode harness work |
| PlayModeSmokeHarness named issuance cases | **None** (headless suite covers path) | Leave; not a rebuild trigger |
| S109 smoke “CMD-31 path unchanged” checkbox | Unchecked honesty line | Cite; do not reopen S109 |
| `DelegationBridge.cs` facade-on-hub P1 (S108 closeout) | Honesty item for a later cleanup PR | **Out** — zero-touch hotpath |
| Unity scene host attach (`Ensure UI Maturity Hosts`) | Editor follow-up from CMD-31…37 closeout | **Out** — not docs residual |
| CMD-35 live edit · CMD-36 perf · CMD-33 doctrine viz | Backlog (different CMDs) | **Out** |
| S121 / CMD-32 overlays (`DatalinkPictureProjection`) | Already landed with S108; residual-scope there too | File-disjoint; do not start S121 here |

## Non-goals

Rebuild CMD-31 · rewrite `C2CommandIssuance` / `C2PlayerCommandBridge` · any C# under `src/` · DelegationBridge Tick / hotpath · new command pipeline · S121 overlay product work · Launch · Phase N · CatalogWriteGate · invent Approved · reopen S108–S119 product lands

## Invariants held

- Stage **Release**
- ZERO DelegationBridge / CatalogWriteGate / locked-eval
- ReplayGolden 6/6 · Baltic hash `17144800277401907079`
- Suite floor ≥1638/0f (cite last gate; **no C# → no suite rerun required**)

## Definition of Done

- [x] Kickoff published (S120-01)
- [x] Residual inventory written (S120-02)
- [x] S120-03 tracker/doc honesty (S119 DoD + residual-scope headers; no issuance rebuild language)
- [ ] DRG-155 closed or explicitly left **In Progress** with “residual-scope complete / no rebuild” comment (owner gate — do not close until AC signed)
- [x] Stage remains **Release**

---
*S120 opened 2026-08-13 as residual-scope only. Do not rebuild CMD-31. Do not touch DelegationBridge hotpath. Stage **Release**. Not Launch.*
