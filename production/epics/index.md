# Epics Index — Project Aegis

| Epic | Status | Layer | GDD focus |
|------|--------|-------|-----------|
| [baltic-headless-slice](baltic-headless-slice/EPIC.md) | **Complete** | Core | Engage + order log + replay |
| [sensor-headless-slice](sensor-headless-slice/EPIC.md) | **Complete** | Core | Contacts + ContactChange log |
| [emcon-headless-slice](emcon-headless-slice/EPIC.md) | **Complete** | Foundation | Radar EMCON gates contacts + engage |
| [pd-detection-loop](pd-detection-loop/EPIC.md) | **Complete** (#23) | Core | Pd + SeededRng detection tick |
| [ew-jam-headless-slice](ew-jam-headless-slice/EPIC.md) | **Complete** (#24) | Core | Noise jam → Pd |
| [world-state-hash-slice](world-state-hash-slice/EPIC.md) | **Complete** (#25) | Core | Unified WORLD_HASH |
| [contact-stale-slice](contact-stale-slice/EPIC.md) | **Complete** (#25) | Core | Detected → Lost |
| [platform-db-basepd-slice](platform-db-basepd-slice/EPIC.md) | **Complete** (PR `stack/data/basepd`) | Content | Catalog `basePd` → detection loop |
| [policy-engage-unification-slice](policy-engage-unification-slice/EPIC.md) | **Complete** (same PR) | Core | ROE/EMCON denials in engage fingerprint |
| [mission-runtime-headless-slice](mission-runtime-headless-slice/EPIC.md) | **MVP Complete** | Gameplay | `fire_order`, mission order-log rows |
| [order-log-replay-checkpoints-slice](order-log-replay-checkpoints-slice/EPIC.md) | **MVP Complete** | Core | Checkpoints + scrub-to-tick |
| [combat-outcomes-mvp-slice](combat-outcomes-mvp-slice/EPIC.md) | **Complete** (PR `stack/combat-followup`) | Simulation | Hit/miss/kill + destroy persistence |
| [sensor-classify-slice](sensor-classify-slice/EPIC.md) | **Complete** | Core | Detected→Classified→Identified FSM |
| [sensor-c2-ui-slice](sensor-c2-ui-slice/EPIC.md) | **Complete** | Presentation | Contact picture + sensor C2 HUD |
| [c2-left-drawer-slice](c2-left-drawer-slice/EPIC.md) | **Complete** | Presentation | OOB + missions + full message log |
| [wave5-engage-cyber-logistics-slice](wave5-engage-cyber-logistics-slice/EPIC.md) | **Complete** (S13-14 + S18 C2 + S20 map base) | Sim + Delegation + UI | Spoof, readiness, attack menu (req 14/16/19/20) |
| [requirements-maturity-slice](requirements-maturity-slice/EPIC.md) | **Complete** (S12–15, closeout 2026-06-08) | Documentation | Req docs 01–12 FULL + RTM gate |

**Sprint 1:** **Complete** on `main` @ `1f7423e` (20/20 stories, PR #36).

**Sprint 2:** **Complete** — [sprint-2-sensor-c2](../sprints/sprint-2-sensor-c2.md). **Replay gate:** [PASS](../determinism/replay-2026-06-02.md).

**Sprint 3:** **Complete** — [sprint-3-c2-shell](../sprints/sprint-3-c2-shell.md).

**Sprint 4:** **Complete** — [sprint-4-c2-map-prep](../sprints/sprint-4-c2-map-prep.md).

**Sprint 5:** **Complete** — [sprint-5-c2-map-globe](../sprints/sprint-5-c2-map-globe.md). **ADR:** [007 map presentation](../../docs/architecture/adr-007-c2-map-presentation.md).

**Sprint 6:** **Complete** — C2 selection sync (map, OOB, contacts, unit detail).

**Sprint 7:** **Complete** — [sprint-7-scoring-comms](../sprints/sprint-7-scoring-comms.md). Scoring CSV export + cyber/comms GDD + Cesium prep.

**Sprint 8:** **Complete** — [sprint-8-comms-fuel](../sprints/sprint-8-comms-fuel.md). Comms degradation order-log + C2 COMMS indicator + fuel readout.

**Sprint 9:** **Complete** — [sprint-9-batch-map](../sprints/sprint-9-batch-map.md). BalticBatchRunner + comms-degraded map symbology.

**Sprint 10:** **Complete** — [sprint-10-fuel-replay-qa-prep](../sprints/sprint-10-fuel-replay-qa-prep.md). FuelLedger + replay SHA-256 fingerprint + headless QA evidence.

**Milestone:** [vertical-slice-mvp](../milestones/vertical-slice-mvp.md)

**Post-MVP program (Sprints 11–15):** [post-mvp-requirements-program](../milestones/post-mvp-requirements-program.md)

| Sprint | Doc | Focus |
|--------|-----|-------|
| 11 | [sprint-11-program-kickoff](../sprints/sprint-11-program-kickoff.md) | Baseline + epics |
| 12 | [sprint-12-requirements-foundation](../sprints/sprint-12-requirements-foundation.md) | Req 01–03 |
| 13 | [sprint-13-wave5-spoof-readiness](../sprints/sprint-13-wave5-spoof-readiness.md) | Spoof + readiness |
| 14 | [sprint-14-wave5-attack-menu](../sprints/sprint-14-wave5-attack-menu.md) | Attack menu |
| 15 | [sprint-15-requirements-rtm-gate](../sprints/sprint-15-requirements-rtm-gate.md) | Req 04–06, 12, RTM |

**Sprint 27 (planned):** [sprint-27-cmo-corpus-combat-bounded](../sprints/sprint-27-cmo-corpus-combat-bounded.md)

| Epic | Stories | Focus |
|------|---------|-------|
| [sprint-27-cmo-corpus-import](sprint-27-cmo-corpus-import/EPIC.md) | 6 | Nightly corpus + loadout/magazine import |
| [sprint-27-adr009-bounded](sprint-27-adr009-bounded/EPIC.md) | 3 | ADR-009 validators + BDA slice |
| [sprint-27-phase-c-presentation](sprint-27-phase-c-presentation/EPIC.md) | 5 | Addressables + platform viewer UX |
| [sprint-27-closeout-devops](sprint-27-closeout-devops/EPIC.md) | 2 | CI hygiene + closeout |

**Sprint 30 (complete):** [sprint-30-tl-bind-corpus-scale](../sprints/sprint-30-tl-bind-corpus-scale.md) — **13/13 done** (956/956 closeout); QA sign-off `production/qa/qa-signoff-sprint-30-2026-06-18.md`

| Epic | Stories | Focus |
|------|---------|-------|
| [sprint-30-closeout-devops](sprint-30-closeout-devops/EPIC.md) | 3 | Day-1 baseline + CI hygiene + closeout |
| [sprint-30-tl-export-phase34](sprint-30-tl-export-phase34/EPIC.md) | 2 | TL Phase 3 export filters + Phase 4 `tlBranch` binding |
| [sprint-30-corpus-approve-scale](sprint-30-corpus-approve-scale/EPIC.md) | 2 | `ship.md` approve at scale + CMO entity slices |
| [sprint-30-combat-domains-phase4](sprint-30-combat-domains-phase4/EPIC.md) | 4 | Land validator + hot-tick hits + Baltic flip + datalink lag |
| [sprint-30-c2-planning-chrome](sprint-30-c2-planning-chrome/EPIC.md) | 2 | Presentation evidence + planning chrome |

**Sprint 31 (complete):** [sprint-31-corpus-combat-polish](../sprints/sprint-31-corpus-combat-polish.md) — **12/13** (S31-12 deferred); QA sign-off `production/qa/qa-signoff-sprint-31-2026-06-18.md`

**Sprint 32 (in-progress):** [sprint-32-release-train-combat-phase6-platform-phase-f](../sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md) — **1/13** (S32-01); QA plan `production/qa/qa-plan-sprint-32-2026-11-13.md`

| Epic | Stories | Focus |
|------|---------|-------|
| [sprint-32-closeout-devops](sprint-32-closeout-devops/EPIC.md) | 3 | Day-1 baseline + CI hygiene + closeout |
| [sprint-32-release-train-ops](sprint-32-release-train-ops/EPIC.md) | 3 | Unified manifest + quarantine + release diff |
| [sprint-32-combat-domains-phase6](sprint-32-combat-domains-phase6/EPIC.md) | 4 | Facility validator + ECCM + mine/BDA hooks |
| [sprint-32-platform-editor-phase-f](sprint-32-platform-editor-phase-f/EPIC.md) | 2 | Damage Unity surfacing + live Editor evidence |
| [sprint-32-presentation-qa](sprint-32-presentation-qa/EPIC.md) | 1 | C2 sign-off upgrade |

**Sprint 33 (complete):** [sprint-33-kill-chain-intelligence-comms-integration](../sprints/sprint-33-kill-chain-intelligence-comms-integration.md) — **13/13** @ 1143/1143 closeout 2026-06-19

| Epic | Stories | Focus |
|------|---------|-------|
| [sprint-33-closeout-devops](sprint-33-closeout-devops/EPIC.md) | 3 | Day-1 baseline + CI hygiene + closeout |
| [sprint-33-kill-chain-intelligence](sprint-33-kill-chain-intelligence/EPIC.md) | 4 | DBI-1.5 graph + DBI-3.5 rules + orchestrator + CLI |
| [sprint-33-cyber-comms-datalink](sprint-33-cyber-comms-datalink/EPIC.md) | 3 | Comms share gate + fixtures + Phase 6 integration |
| [sprint-33-platform-editor-phase-g](sprint-33-platform-editor-phase-g/EPIC.md) | 2 | Comms/datalink Unity + live evidence |
| [sprint-33-presentation-qa](sprint-33-presentation-qa/EPIC.md) | 1 | C2 sign-off Check 17 |

| Epic | Stories | Focus |
|------|---------|-------|
| [sprint-31-closeout-devops](sprint-31-closeout-devops/EPIC.md) | 3 | Day-1 baseline + CI hygiene + closeout |
| [sprint-31-corpus-approve-complete](sprint-31-corpus-approve-complete/EPIC.md) | 4 | Sensor approve + balance/weapon/entity scale |
| [sprint-31-tl-release-train](sprint-31-tl-release-train/EPIC.md) | 1 | TL snapshot resolution at scenario load |
| [sprint-31-combat-domains-phase5](sprint-31-combat-domains-phase5/EPIC.md) | 3 | Mine validator + facility hot-tick + BDA hot path |
| [sprint-31-presentation-polish](sprint-31-presentation-polish/EPIC.md) | 2 | Live Editor evidence + C2 sign-off refresh |

**Sprint 36 (planning):** [sprint-36-perf-determinism](../epics/sprint-36-perf-determinism/EPIC.md) — 5 stories (P1 perf follow + determinism audit + replay maint + GitNexus planning). **Authority:** post-S35 perf-profile + polish-boundary. **Gate:** replay-verify + hash immutable + 6/6 golden. ZERO Delegation.

| Epic | Stories | Focus |
|------|---------|-------|
| [sprint-36-perf-determinism](sprint-36-perf-determinism/EPIC.md) | 5 | Determinism audit P1, replay/harness maint, DecisionLog hash, Datalink GitNexus plan (no change), re-profile + verify gate |