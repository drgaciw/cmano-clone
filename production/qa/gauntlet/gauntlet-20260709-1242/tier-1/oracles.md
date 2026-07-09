# Tier 1 Scenario Oracles
# RUN_ID: gauntlet-20260709-1242
# Generated: 2026-07-09

## Scenarios

### tier1-patrol-01 — Baltic Frigate Sweep
**OOB:** 1 Blue surface unit (u1) vs 1 Red surface unit (hostile-1). Baltic northern approach sector.
**Intent:** Baseline 1-vs-1 symmetric patrol. Weapons free both sides, unrestricted EMCON, no events.
**Oracle:** Both sides survive all 6 ticks unless one side expends its full magazine in the engagement envelope; no victory condition is declared (survive-N-ticks); sim must complete without runtime error or assertion fault. ROE doctrine resolves to WeaponsFree for both missions.
**Validation status:** PASS (first try) — reportHash: 3118aad2212f9659186a20bb94df8e35dd200724174c1da998dd0017f19ef450

---

### tier1-patrol-02 — Gotland Corridor Standoff
**OOB:** 1 Blue surface unit (u1) vs 1 Red surface unit (hostile-far). South-central Baltic; Red positioned at extended range (~600 nm from Blue start).
**Intent:** 1-vs-1 patrol at long inter-unit separation to stress sensor detection probability at range and confirm that a scenario where adversaries may never enter engagement envelope still runs stably for 6 ticks.
**Oracle:** Both sides survive all 6 ticks; no engagement occurs if units remain outside rangeMeters; sim must complete cleanly with no crossing state exceptions. ROE WeaponsFree on both missions.
**Validation status:** PASS (first try) — reportHash: ed2e9e86d0318553b645307ae24e0bd88452f8486b80d745dded6f161a7d6418

---

### tier1-patrol-03 — Bornholm Picket Line
**OOB:** 2 Blue surface units (u1, hostile-far acting as Blue-2) vs 1 Red surface unit (hostile-1). South Baltic near Bornholm.
**Intent:** 2-vs-1 surface patrol tests asymmetric force size within Tier-1 envelope; confirms the sim handles multiple concurrent patrol missions on one side without state corruption over 6 ticks.
**Oracle:** All three units survive all 6 ticks (outnumbering does not guarantee a kill within 6 ticks at these engagement parameters); sim must not deadlock or produce NaN damage values; all three missions resolve ROE WeaponsFree.
**Validation status:** PASS (first try) — reportHash: 6daa2c4a7ec81a35bdb8714f456fd446280a5942a70415c580e826b84e4bb059

---

### tier1-patrol-04 — Gulf of Finland Mutual Patrol
**OOB:** 2 Blue surface units (u1, hostile-far acting as Blue-2) vs 1 Red surface unit (hostile-1) in the eastern Baltic / Gulf of Finland. Maximum Tier-1 platform count (3 total, 2 missions Blue, 1 Red).
**Intent:** Maximum Tier-1 platform density with a symmetric geographic arrangement tests concurrent patrol tick processing and multi-mission ROE resolution at peak Tier-1 complexity; 6-tick survive-N oracle.
**Oracle:** All units survive all 6 ticks unless magazine is exhausted within the engagement envelope; all three missions independently resolve ROE WeaponsFree from per-mission override; sim must not raise unhandled exceptions or produce indeterminate ordering of tick events across missions.
**Validation status:** PASS (first try) — reportHash: ef0ba1258648216ae1003a99c15b9966e80e61693fafd849a1b903c32fa1ecec
