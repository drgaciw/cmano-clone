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

**Sprint 1:** **Complete** on `main` @ `1f7423e` (20/20 stories, PR #36).

**Sprint 2:** **Complete** — [sprint-2-sensor-c2](../sprints/sprint-2-sensor-c2.md). **Replay gate:** [PASS](../determinism/replay-2026-06-02.md).

**Sprint 3:** **Complete** — [sprint-3-c2-shell](../sprints/sprint-3-c2-shell.md).

**Sprint 4:** **Complete** — [sprint-4-c2-map-prep](../sprints/sprint-4-c2-map-prep.md).

**Sprint 5:** **Complete** — [sprint-5-c2-map-globe](../sprints/sprint-5-c2-map-globe.md). **ADR:** [007 map presentation](../../docs/architecture/adr-007-c2-map-presentation.md).

**Milestone:** [vertical-slice-mvp](../milestones/vertical-slice-mvp.md)