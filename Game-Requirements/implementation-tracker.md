# Game Requirements — Implementation Tracker (Stable Alias)

> **Historical Snapshot Note:** This file serves as the canonical stable alias for the implementation tracker. The historical baseline snapshot from 2026-07-04 is preserved in [`implementation-tracker-2026-07-04.md`](implementation-tracker-2026-07-04.md).

**Base:** `origin/main`  
**Last Updated:** 2026-09-02 (Audit Remediation W1-HUB)  
**Historical Snapshots:** [2026-07-04](implementation-tracker-2026-07-04.md) | [2026-07-01](implementation-tracker-2026-07-01.md) | [2026-06-30](implementation-tracker-2026-06-30.md) | [2026-06-04 (S56 evidence)](implementation-tracker-2026-06-04.md)  
**Index:** [Game-Requirements-Index.md](Game-Requirements-Index.md) | [00-Master-Index.md](../00-Master-Index.md)  
**Live Status Pointers:** Latest sprint reports under [`docs/reports/`](../docs/reports/) and QA closeouts under [`production/qa/`](../production/qa/).

---

## Post-2026-07-09 Delta & Forward Trunk Landings

The table below documents major engineering landings, ticketed DRG deliveries, and headless capabilities added to `origin/main` after the 2026-07-09 post-editor baseline:

| DRG / Ref | Area / Shipped Capability | Key Symbols & Artifacts | Target Doc(s) |
|---|---|---|---|
| DRG-66/67 | Human-on-the-loop approval queue & UI host | `PendingApprovalQueue`, `PendingApprovalPanelHost` | Doc 24 (HOL) |
| DRG-73 | Platform Design Assistant & relative scaler | `PlatformDesignAssistant`, `PlatformRelativeScaler`, `platform_design_propose` | Doc 21 (PLE) |
| DRG-196 | Agent-callable C2 skill contract | `SkillCatalog`, `SkillEnvelope`, `SkillIds`, `C2CommandIssuance` | Doc 24 (HOL) / 07 / 08 |
| DRG-206 | Contact provenance ledger | `ContactProvenance*` | Doc 15 (SEN) / Doc 23 (KCX) |
| DRG-207 | Sensor-to-shooter chain | `SensorToShooter*` | Doc 23 (KCX) |
| DRG-209 | C2 authority disposition projector | `C2AuthorityProjector` | Doc 24 (HOL) |
| DRG-211 | Combat event stream | `CombatEvents` | Doc 17 (RPL) |
| DRG-212 | Threat assessment & weapon recommendation | `ThreatAssessmentProjection` | Doc 14 / Doc 24 |
| DRG-213 | Headless C2 nodes and mission packages | `C2Node*`, `MissionPackage*` | Doc 25 (C2N) |
| DRG-214 | Headless C2 network health projection | `C2NetworkHealth*` | Doc 25 (C2N) |
| DRG-215 | Engage-explain positive path contract | `EngageExplainContract` | Doc 17 (RPL) / Doc 23 (KCX) |
| DRG-217 | Scarcity / resource ranking projection | `ResourceRankProjection` | Doc 14 / Doc 24 |
| DRG-218 | Headless after-action ledger | `AfterAction` | Doc 17 (RPL) |
| DRG-219 | Targetability acceptance composition | `TargetabilityAccept*` | Doc 23 (KCX) |
| DRG-220 | Headless collateral & CDE assessment | `CdeAssessProjection` | Doc 13 / 18 / Doc 24 |
| DRG-221 | Headless EMCON / emissions posture | `EmconState*` | Doc 13 / 19 |
| DRG-222 | Track custody ledger | `TrackCustody*` | Doc 23 (KCX) |
| DRG-223 | Task group coordination & gaps | `TaskGroupCoord*` | Doc 25 (C2N) |
| DRG-224 | Ordnance state bands & employment ledger | `OrdnanceStateBands`, `EmploymentLedger*` | Doc 16 (LOG) |
| DRG-225 | Identity / classification ledger | `IdentityClass*` | Doc 15 (SEN) / Doc 23 (KCX) |
| DRG-226 | Engage next-action projection | `EngageNextActionProjection` | Doc 14 / Doc 24 |
| DRG-227 | Own-unit degradation / damage control | `PlatformDegrade*` | Doc 18 (DOM) |
| DRG-228 | Escalation gate ledger & disposition codes | `EscalationGateProjection` (`HOLD_FIRE`, `WEAPONS_TIGHT`, `HIGHER_HQ`) | Doc 24 (HOL) |
| DRG-229 | Mission-command intent structure | `MissionIntent*` | Doc 25 (C2N) |
| QA Gauntlet | Stress axes, ladder, saboteur mutants, oracle | `tools/qa-gauntlet`, `GauntletOracleEvaluator`, `UiIa*` | Doc 26 (VER) |
| Campaign Lib | Scenario and campaign library package loader | `CampaignDocument*`, `ScenarioLibraryProjection`, `ScenarioPackageLoader` | Doc 27 (LIB) |

---

## Verdict Summary

| Scope | Status |
|-------|--------|
| Requirements **documentation** (01–21) | **Complete** — baseline established 2026-07-08; undergoing audit remediation (2026-09-02) |
| **MVP / Phase 1 gameplay** implementation | **COMPLETE 21/21** (S56 gate PASS 2026-06-21) — grades frozen; Baltic ACs (replay 6/6, proxy ≥20/20, hash `17144800277401907079`) |
| **Post-MVP content programs** (S57–S80) | **COMPLETE** — Baltic v2/v3, release train, E7 prep; S80 ack **"Baltic v3 content-complete"** (2026-06-26); stage **Release** |
| **Forward engineering** | S81–S88 (Scenario Editor), ME Phase 2, PE (req 21), S89–S92 (Post-Editor Hygiene), S93–S121+ continuous development. Stage **Release**. |

Completing the full requirements corpus as *shipped game features* is multi-year work (req 07 phases 1–5). **Post-MVP tracks add evidence additively; they do not re-litigate S56 MVP row grades** ([baltic-v3-scope-boundary-2026-06-25.md](../production/baltic-v3-scope-boundary-2026-06-25.md)).

## Verification baseline

Current solution test-floor and standing invariant specifications are governed exclusively by **[`AGENTS.md`](../AGENTS.md) §Hard Invariants**.

- Test floor: see `AGENTS.md` (baseline floor ≥1638 / 0 failures post S95 gauntlet land).
- Invariants: ReplayGolden 6/6, PlayModeSmokeHarness ≥20/20, Baltic v2 hash `17144800277401907079` preserved, ZERO `DelegationBridge` hotpath edits.

## GitNexus intelligence

For live graph metrics and symbol index details, see `.gitnexus/meta.json`.

---

## MVP status by requirement (Historical Baseline)

**S56 gate: 21/21 MVP-done / documented Partial+.** Evidence @ S56: [implementation-tracker-2026-06-04.md](implementation-tracker-2026-06-04.md). **Post-S56 note** is additive only.

**Program note (corpus maturity W0–W4, 2026-07-08):** Waves 0–4 corpus honesty complete; **no MVP regrade**. Req **10b** remains **Phase N / not on main**.

| Req | Title | MVP status (S56) | Post-S56 note | Next stack task |
|-----|-------|------------------|---------------|-----------------|
| 01 | Project Overview | **MVP-done (S56)** | S72 commercial launch prep complete; **doc charter re-baseline 2026-07-08** | Complete @ S56 |
| 02 | Core Gameplay Loop | **Partial** | S74/S76 v3 policies + contact-triggered ROE; doc honesty Wave 1 2026-07-08 | Execution UX polish |
| 03 | Simulation Modes | **Partial+** | S76 mission-event policies on v3 fixtures; doc honesty Wave 1 2026-07-08 | Mode UI on C2 top bar |
| 04 | Agent Delegation | **Partial+** | Doc honesty Wave 1 2026-07-08 (expanded mapping) | C2 delegation badges |
| 05 | Dynamic Speculative Systems Agent | **Partial+** | Doc honesty Wave 1 2026-07-08 (OSINT mapping) | MCP polish; Data P1 |
| 06 | Database Intelligence | **Partial** | Scenario schema (AME-2.6), validation engine extensions, save-vs-export gate; doc honesty Wave 1 2026-07-08 | Corpora in CI |
| 07 | Agentic Infrastructure | **Partial** | Expanded `scenario_*` + ferry MCP/CLI; AC-6 smoke script; doc honesty Wave 1 2026-07-08 | Experiment workers |
| 08 | Agentic Architecture | **Partial** | ADR-017 editor topology; doc honesty Wave 1 2026-07-08 | Architecture honesty sweep |
| 09 | Near-Future Technologies | **Partial** | Doc honesty Wave 3 2026-07-08 (FR-08; headless archetype/TL/spawn-plan spine) | Headless NF spawn |
| 10 | Speculative Systems | **Partial+** | Wave 3 honesty: Partial+ = TL/black-project SpeculativeEngageGate + catalog metadata | Escalation ladder |
| 10b | (KESSLER) | **Phase N / not on main** | Wave 3: `OrbitalDewPlatform` / `KesslerRiskMeter` / `EscalationTier` absent from `src/` | Phase N |
| 11 | Agentic Mission Editor | **Partial+** | Headless ACs honest; ferry/undo shipped; AC-8 dual-fixture host load | Phase 2 map/GUI |
| 12 | Terms Glossary | **Partial** | Doc honesty Wave 1 2026-07-08 | UI tooltips |
| 13 | Doctrine ROE EMCON WRA | **Partial** | `DoctrineInheritanceValidateTests` + fixture; v3 mission-roe policy | Unity doctrine panel |
| 14 | Engagement & Fire Control | **Partial+** | v3 contact-triggered engage; UA engage tests green | DLZ Phase 2 |
| 15 | Sensor Detection & EW | **Partial** (MVP **COVERED**) | v3 classify + catalog sensor slices | ECCM Phase 2 |
| 16 | Logistics & Magazines | **Partial** | Doc honesty Wave 2 2026-07-08 | UNREP; live magazines |
| 17 | Replay AAR & Order Log | **Partial** | 6 v3 goldens; event debugger aligned to order-log projection | Scrub UI; AAR agent |
| 18 | Combat Domains | **Partial+** | S75/S79 v3 theater; doc honesty Wave 2 2026-07-08 | Mine warfare |
| 19 | Cyber & Comms | **Partial** | v3 comms policies; doc honesty Wave 2 2026-07-08 | JADC2 node damage |
| 20 | Command & Control UI | **Partial** | S78 v3 picker + bands; doc honesty Wave 2 2026-07-08 | C2 UX polish |
| 21 | Platform Editor | **MVP-done / Partial+ (S56)** | S77 v3 Excel slices; doc honesty Wave 3 2026-07-08 | Platform Editor polish |

## Related

- Historical Trackers: [`implementation-tracker-2026-07-04.md`](implementation-tracker-2026-07-04.md) | [2026-07-01](implementation-tracker-2026-07-01.md) | [2026-06-30](implementation-tracker-2026-06-30.md) | [2026-06-04 (S56 evidence)](implementation-tracker-2026-06-04.md)
- Requirements Corpus Index: [Game-Requirements-Index.md](Game-Requirements-Index.md)
- Master Index: [00-Master-Index.md](../00-Master-Index.md)
