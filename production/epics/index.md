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
| [platform-db-basepd-slice](platform-db-basepd-slice/EPIC.md) | Ready | Content | Catalog `basePd` → detection loop |
| [policy-engage-unification-slice](policy-engage-unification-slice/EPIC.md) | Ready | Core | ROE/EMCON denials in engage fingerprint |
| [mission-runtime-headless-slice](mission-runtime-headless-slice/EPIC.md) | Blocked (C4) | Gameplay | `fire_order`, mission order-log rows |
| [order-log-replay-checkpoints-slice](order-log-replay-checkpoints-slice/EPIC.md) | Ready | Core | Checkpoints + scrub-to-tick |
| [combat-outcomes-mvp-slice](combat-outcomes-mvp-slice/EPIC.md) | Not started | Simulation | Hit/miss after `Launched` |

**Sprint:** [sprint-1-headless-mvp](../sprints/sprint-1-headless-mvp.md)

**Next (engineering):** Platform DB `basePd` → policy–engage unification → replay checkpoints (parallel design gates for C1/C4)