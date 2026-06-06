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
| [requirements-maturity-slice](requirements-maturity-slice/EPIC.md) | **Planned** | Documentation | Req docs 01–06, 12 + RTM |

**Sprint 1:** **Complete** on `main` @ `1f7423e` (20/20 stories, PR #36).

**Sprint 2:** **Complete** — [sprint-2-sensor-c2](../sprints/sprint-2-sensor-c2.md). **Replay gate:** [PASS](../determinism/replay-2026-06-02.md).

**Sprint 3:** **Complete** — [sprint-3-c2-shell](../sprints/sprint-3-c2-shell.md).

**Sprint 4:** **Complete** — [sprint-4-c2-map-prep](../sprints/sprint-4-c2-map-prep.md).

**Sprint 5:** **Complete** — [sprint-5-c2-map-globe](../sprints/sprint-5-c2-map-globe.md). **ADR:** [007 map presentation](../../docs/architecture/adr-007-c2-map-presentation.md).

**Milestone:** [vertical-slice-mvp](../milestones/vertical-slice-mvp.md)

**Post-MVP program (Sprints 11–15):** [post-mvp-requirements-program](../milestones/post-mvp-requirements-program.md)

| Sprint | Doc | Focus |
|--------|-----|-------|
| 11 | [sprint-11-program-kickoff](../sprints/sprint-11-program-kickoff.md) | Baseline + epics |
| 12 | [sprint-12-requirements-foundation](../sprints/sprint-12-requirements-foundation.md) | Req 01–03 |
| 13 | [sprint-13-wave5-spoof-readiness](../sprints/sprint-13-wave5-spoof-readiness.md) | Spoof + readiness |
| 14 | [sprint-14-wave5-attack-menu](../sprints/sprint-14-wave5-attack-menu.md) | Attack menu |
| 15 | [sprint-15-requirements-rtm-gate](../sprints/sprint-15-requirements-rtm-gate.md) | Req 04–06, 12, RTM |