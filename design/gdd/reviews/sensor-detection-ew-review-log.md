# Design Review Log — sensor-detection-ew

## 2026-06-02 — Lean re-review (headless MVP gate)

**Reviewer:** agent (lean `/design-review`)  
**Verdict:** **APPROVED WITH CONDITIONS**  
**Scope:** Headless simulation + order-log replay (not Unity C2 UI)

### Completeness (8/8)

| Section | Pass |
|---------|------|
| Overview | Yes |
| Player Fantasy | Yes |
| Detailed Rules | Yes |
| Formulas | Yes |
| Edge Cases | Yes (stale/lost in contact-stale epic) |
| Dependencies | Yes |
| Tuning Knobs | Yes |
| Acceptance Criteria | Yes |

### Implementation traceability (headless)

| AC / TR | Evidence | Status |
|---------|----------|--------|
| AC-1 deterministic contacts | `BalticReplayHarnessPdDetectionTests`, golden fingerprints | **Met** |
| AC-2 sorted iteration | `DetectionPdLoop` + scenario JSON ordering tests | **Met** |
| AC-3 EMCON gates radar | `BalticReplayHarnessEmconTests` | **Met** |
| AC-4 ContactChange log | `ContactChangeOrderLogTests`, harness contact tests | **Met** |
| AC-5 NO_FIRE_CONTROL_TRACK | Policy/engage denial codes — partial via ROE | **Partial** |
| TR-sensor-002 Pd loop | `pd-detection-loop` epic complete | **Met** |
| TR-sensor-003 EW jam | `ew-jam-headless-slice` epic complete | **Met** |
| TR-sensor-001 Contact FSM | Detected→Lost MVP only (2026-06-02); Classify/Identify + C2 projection deferred to Sprint 2 | **Partial** |
| TR-sensor-004 Side picture | Organic contacts only; datalink P1 | **Deferred** |

### Conditions (non-blocking for headless merge)

1. **Classify / Identify** promotion remains Sprint 2+ (see `production/epics/index.md`).
2. **C2 UI** contact presentation requires Unity slice — out of scope until GDD Approved for UI team.
3. Re-run `npx gitnexus analyze` after next sensor symbol batch (GDD GitNexus note is stale).

### Blocking issues

None for headless engineering.

## 2026-06-08 — Sprint 2 closeout re-review

**Reviewer:** agent (Sprint 2 closeout)  
**Verdict:** **APPROVED** (headless classify FSM + Unity C2 projection)  
**Scope:** TR-sensor-001 MVP slice — Classify/Identify FSM and sensor C2 HUD

### Implementation traceability (Sprint 2)

| AC / TR | Evidence | Status |
|---------|----------|--------|
| TR-sensor-001 Contact FSM | Classify/Identify FSM + C2 projection; [sensor-classify-slice](../../../production/epics/sensor-classify-slice/EPIC.md), [sensor-c2-ui-slice](../../../production/epics/sensor-c2-ui-slice/EPIC.md) | **Met** |
| TR-sensor-001 Classify golden | `PdContactClassifyTests`, `ReplayGoldenBalticClassifyTests` | **Met** |
| TR-sensor-001 C2 bridge | `SensorC2BridgeTests`, `SensorC2PanelBinderTests`, `SensorC2PanelHost` | **Met** |
| TR-sensor-004 Side picture | Organic contacts only; datalink P1 | **Deferred** |

### Notes

- Parent **TR-sensor-001** promoted to **Covered** in [tr-registry.yaml](../../../docs/architecture/tr-registry.yaml) (MVP slice complete).
- **TR-sensor-004** (side picture / datalink sharing) remains explicitly deferred.

### Blocking issues

None for Sprint 2 sensor/C2 merge gate.