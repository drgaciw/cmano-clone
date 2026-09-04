# Slice C UI Bind Architecture Map (Pre-Planning & Host Traceability)

**Document**: `production/qa/evidence/s124-slice-c-bind-map.md`  
**Epic**: [DRG-185](https://linear.app/drgamtd-workspace/issue/DRG-185) Slice C — Plan, Coordinate, Command  
**Status**: **PARKED** (Analysis and Host Mapping Only — Zero Scene Mutations, Zero New UIDocuments)  
**Stage**: **Release** · **Not Launch** · **Not Phase N**  

---

## 1. Architectural Guardrails
1. **UIDocument Budget**: Strict cap at 29 roots. Slice C must NOT introduce a 30th root; it must bind into existing host panels (`PendingApproval`, `TopBar`, `RightUnitDetail`, `MessageLog`, and `C2Menu`).
2. **Headless Separation**: All 6 underlying headless capabilities are already implemented and tested (`DRG-212`, `DRG-221`, `DRG-227`, `DRG-223`, `DRG-218`, `DRG-217`).
3. **No Hotpath Touch**: Zero edits to `DelegationBridge.cs` hotpath.

---

## 2. Headless-to-Host Traceability Matrix

| Slice C Story | Title | Headless Contract (Done) | Target Existing Host | Bind Mechanism |
| :--- | :--- | :--- | :--- | :--- |
| **DRG-172** | Threat Assessment & Weapon Recommendation | `DRG-212` (`ThreatAssessDto`) | `PendingApprovalPanelHost` | Renders recommended munitions and intercept geometries inside pending lethal proposals. |
| **DRG-173** | EMCON & EW Coordination | `DRG-221` (`EmconCoordinationState`) | `C2TopBarPanelHost` / `SensorC2` | Surfaces fleet emissions posture (Silent, Surveillance, Active Target Acquisition) on top-bar drawer. |
| **DRG-174** | System Degradation & Damage Control | `DRG-227` (`DamageControlState`) | `RightUnitPanelHost` | Appends subsystem casualty and repair progress bars onto existing unit detail drawer. |
| **DRG-175** | Task Group Mission Coordination | `DRG-223` (`TaskGroupPackageDto`) | `C2LeftDrawerPanelHost` (OOB Tree) | Displays multi-element mission groupings and command nodes hierarchically. |
| **DRG-171** | After-Action Reporting & Kill Ledger | `DRG-218` (`AfterActionLedger`) | `MessageLogPanelHost` | Formats final engagement casualty summaries and CSV export link into log stream upon scenario completion. |
| **DRG-191** | Munitions Scarcity & Allocation | `DRG-217` (`ResourceRankingDto`) | `PendingApprovalPanelHost` | Highlights reserve exhaustion warnings when proposed salvo depletes local magazine below threshold. |

---

## 3. Boundary Verification
- [x] No Unity scene assets (`.unity`) modified for Slice C.
- [x] No new UIDocument components created.
- [x] Slice C remains parked until explicit un-parking and human sign-off.
