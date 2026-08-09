# QA Plan — S111 IR/Visual Detection Spine

| TC | Check |
|----|-------|
| TC-IR-1 | Day visual env mask > night visual env mask (same base) |
| TC-IR-2 | IR env mask independent of day/night or uses thermal contrast param |
| TC-IR-3 | Radar trial still EMCON-gated; IR/Visual with RequiresActiveRadar=false roll without Active radar |
| TC-IR-4 | RF jam reduces radar Pd; same jam list does not suppress IR/Visual trial Pd |
| TC-IR-5 | Catalog fixture sensors modality Infrared/Visual load |
| TC-IR-6 | Determinism: same seed → same rolls with mixed modalities |

Sign-off: smoke-sprint-111 when TC-IR-1…6 pass.
