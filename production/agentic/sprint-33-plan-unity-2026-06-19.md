# Sprint 33 — Unity + DevOps Track Plan

**Date:** 2026-06-19 (refreshed post-S32 closeout)  
**Owners:** team-unity, c-sharp-devops-engineer  
**Predecessor:** Sprint 32 **COMPLETE** — S32-10 lean presentation evidence; S32-11 C2 sign-off **PASS WITH NOTES** (headless 33/33)  
**Sprint gate:** S33-06 Platform Editor Phase G comms surfacing  
**Epics:** `sprint-33-platform-editor-phase-g`, `sprint-33-closeout-devops`  
**Review mode:** **lean** — Buildkite = merge authority; headless proxy tests mandatory; live Unity Editor evidence advisory

---

## Sprint 33 Unity goal

Surface **comms/datalink fitting rows** (`LinkId`, `Role`, `SatcomCapable`) in `PlatformCatalogViewerHost` and import staging diff with headless propose→approve round-trip. Schema-only — sim datalink **behavior** stays in S33-04; kill-chain **rules context** from S33-03 informs staging validation only.

---

## S32 carryover (presentation pipeline — extend, do not rebuild)

S32 closed the Phase F presentation pipeline. S33-10 **extends** that pipeline; it does **not** recreate signoff scripts, evidence doc templates, or headless proxy patterns.

| S32 artifact | Status @ closeout | S33 reuse |
|--------------|-------------------|-----------|
| S32-10 presentation evidence | **DONE** — protocol placeholders `*-s32-*.png`; headless **47/47**; `production/qa/sprint-32-presentation-evidence-2026-06-19.md` | S33-10 maps S32 → S33 PNGs; same lean PASS WITH NOTES verdict pattern |
| `Invoke-C2PlayModeSignoffBatch.ps1` | Import + doctrine scenarios verified | Extend with `-Scenario platform-comms` (advisory); no script rewrite |
| `README-presentation-evidence-s32.md` | Protocol steps for damage viewer + import staging | Clone section for Phase G comms viewer + comms staging diff |
| Headless filter baseline | `PlatformImport\|Doctrine\|C2TopBar\|PlayModeSmoke\|PlatformCatalogViewer` → 47/47 | Add `PlatformComms`; S33-10 target **≥38/38** on narrowed filter |
| S32-11 C2 sign-off | **PASS WITH NOTES** — checks 14–16 upgraded; headless **33/33** | S33-11 adds Check 17; refreshes 14–16 for Phase G |

**S33-10 scope delta vs S32-10:** replace/extend S32 Phase F PNGs with Phase G comms captures (`*-s33-*.png`); file evidence doc `sprint-33-presentation-evidence-*.md`; bump headless comms tests — **no** new evidence infrastructure.

---

## Must-have

| ID | Story | Est. | Owner | Dependencies |
|----|-------|------|-------|--------------|
| S33-01 | Full-sln re-baseline ≥1046 | 1d | devops | S32-13 closeout |
| S33-06 | Phase G comms/datalink Unity (viewer + staging diff + headless round-trip) | 2d | unity | **S33-01**, **S33-04** (sim share gate), **S33-03** (kill-chain rules context) |

### S33-06 — dependency rationale

| Upstream | Why Unity waits |
|----------|-----------------|
| **S33-04** (sim gate) | `CommsState` Nominal/Degraded/Denied semantics must be stable before Unity surfaces comms workbook rows; avoids schema/behavior drift during parallel merges |
| **S33-03** (rules context) | `CatalogRulesValidationAgent` `KILL_CHAIN_*` findings inform staging diff messaging and orchestrator gate expectations; viewer shows comms rows that rules pack validates |

**Hard boundary:** S33-06 is **schema surfacing only** — no `DatalinkSidePictureMerger` wiring, no `DelegationBridge.cs` edits, no new migrations.

---

## Should-have

| ID | Story | Est. | Dependencies |
|----|-------|------|--------------|
| S33-10 | Presentation evidence (Phase G comms PNGs — extends S32-10) | 1.5d | S33-06 |
| S33-11 | C2 sign-off Check 17 + refresh 14–16 | 1d | S33-06, S33-10 |
| S33-13 | Closeout ≥1086; prune `stack/sprint32/*` | 0.5d | S33-02+ |

## Nice-to-have

| ID | Story | Est. |
|----|-------|------|
| S33-12 | CI/local gate refresh (S32-12 carryover) | 0.25d |

---

## Wave plan (Unity track)

Aligns with `production/agentic/sprint-33-parallel-kickoff-2026-06-19.md` but **sequences Unity after sim Wave 1**.

| Wave | Stories | Track | Notes |
|------|---------|-------|-------|
| Day-1 | S33-01 | DevOps | ≥1046 baseline; ReplayGolden 6/6 |
| Sim Wave 1 | S33-04 | Simulation | Datalink share gate — **blocks S33-06 start** |
| Data Wave 2 | S33-03 | Data | Kill-chain rules — **blocks S33-06 start** (rules context) |
| **Unity Wave** | **S33-06** | Unity | Starts only after S33-04 + S33-03 merge |
| QA Wave | S33-11 | QA | After S33-06; headless proxy mandatory |
| **Closeout** | **S33-10**, S33-13 | Unity + DevOps | S33-10 = evidence package; S33-13 hygiene |

```bash
# Dispatch order (Unity-relevant)
/dev-story dispatch S33-01          # Day-1
# wait: S33-04 + S33-03 merge
/dev-story dispatch S33-06          # Unity Wave
/dev-story dispatch S33-11          # QA (headless)
/dev-story dispatch S33-10 S33-13   # Closeout evidence + hygiene
```

**Do not** start S33-06 in parallel with S33-04 — sim gate semantics must land first.

---

## Lean review mode (merge authority)

Per S32-10/S32-11 precedent and ADR-010 headless-first:

| Gate type | Blocking? | S33 pattern |
|-----------|-----------|-------------|
| `dotnet test` headless filters | **Yes** — Buildkite merge authority | `PlatformImport\|PlatformCatalogViewer\|PlatformComms` on S33-06; ≥38/38 on S33-10 |
| ReplayGolden 6/6 | **Yes** | Unchanged on Unity merges |
| ZERO touch `DelegationBridge.cs` | **Yes** | `git diff HEAD -- .../DelegationBridge.cs` empty |
| Live Unity Editor PNG capture | **No** — advisory | Protocol placeholder `*-s33-*.png` acceptable |
| `Invoke-C2PlayModeSignoffBatch.ps1` | **Advisory** | `-Scenario platform-comms -SkipBuild` documents intent; headless proxy satisfies merge |

### PASS WITH NOTES (no live Editor required)

When Unity 6.3 Editor is unavailable (headless Linux CI/agent host):

1. Headless regression **PASS** on mandated filters.
2. Protocol placeholder PNGs in `production/qa/evidence/*-s33-*.png` (labeled per S27-10/S31-07/S32-10 pattern).
3. Evidence doc `production/qa/sprint-33-presentation-evidence-*.md` maps S32 → S33 replacements.
4. Verdict: **APPROVED WITH CONDITIONS (lean PASS WITH NOTES)** — same as S32-10/S32-11.
5. Live Editor re-capture optional polish before Production → Polish gate.

**S33-11** inherits S32-11 headless **33/33** baseline; Check 17 comms fittings satisfied by headless `PlatformComms` tests when Editor absent.

---

## Story detail

### S33-06 — Platform Editor Phase G comms/datalink Unity

| Field | Value |
|-------|-------|
| **Graphite** | `stack/sprint33/platform-phase-g-comms` |
| **Req trace** | Req 21 Comms/LinkCatalog Phase A*; ADR-011 |
| **GitNexus HIGH** | `PlatformCatalogViewerHost`, `PlatformImportPanelHost`, `PlatformWorkbookWriteBridge` |
| **CRITICAL extend-only** | `CatalogWriteGate` |

**Deliverables:**

- `PlatformCatalogViewerHost` comms fitting rows (`LinkId`, `Role`, `SatcomCapable`)
- Import staging diff surfaces comms field deltas (aligned with S33-03 rules context)
- Headless propose→acknowledge→approve→readback tests
- Writes via `PlatformImportPanelHost` → `PlatformWorkbookWriteBridge` only

**Acceptance criteria:**

- [ ] Viewer shows comms fittings per platform
- [ ] Staging diff surfaces comms field deltas
- [ ] Headless propose→approve tests PASS
- [ ] Writes via write bridge only; no write-gate bypass
- [ ] No new migrations; ZERO touch `DelegationBridge.cs`

### S33-10 — Presentation evidence (extends S32-10)

| Field | Value |
|-------|-------|
| **Graphite** | `stack/sprint33/presentation-evidence` |
| **Req trace** | Req 20 C2 presentation; Req 21 Platform Editor |
| **Depends on** | S33-06 feature complete |

**Deliverables (extension only):**

| S32-10 artifact | S33-10 extension |
|-----------------|----------------|
| `platform-catalog-damage-s32-viewer-columns.png` | `platform-catalog-comms-s33-viewer-columns.png` (or equivalent) |
| `platform-import-staging-s32-baltic-diff.png` | `platform-import-staging-s33-comms-diff.png` |
| `sprint-32-presentation-evidence-2026-06-19.md` | `sprint-33-presentation-evidence-*.md` with S32→S33 map |

**Acceptance criteria:**

- [ ] `production/qa/evidence/*-s33-*.png` (protocol placeholder OK in lean mode)
- [ ] Evidence doc with S32 replacement map
- [ ] Headless filter ≥38/38 PASS (`PlatformImport|PlatformCatalogViewer|PlatformComms|C2TopBar`)

---

## Graphite

```
stack/sprint33/full-sln-gate
  → [wait S33-04 + S33-03]
  → platform-phase-g-comms
  → c2-signoff-upgrade
  → presentation-evidence
  → closeout
```

Presentation evidence stacks **after** S33-06 implementation and **before/with** closeout — not in Wave 1 parallel with sim.

---

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"

# S33-06 merge gate
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|PlatformCatalogViewer|PlatformComms" -v minimal
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs  # empty
npx gitnexus impact PlatformCatalogViewerHost

# S33-10 closeout evidence gate
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|PlatformCatalogViewer|PlatformComms|C2TopBar" -v minimal
# target ≥38/38 PASS

# S33-11 advisory signoff (headless Linux OK)
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario platform-comms -SkipBuild  # advisory
```

---

## Phase G rationale

S32 closed damage workbook Unity (Phase F). Req 21 Phase A* comms/`LinkCatalog` schema landed in S22 — S33 closes the comms workbook Unity gap. **Schema-only in Unity;** `CommsState` datalink behavior is S33-04 sim ownership. Kill-chain detect-only rules (S33-03) provide validation context for staging diffs without auto-repair.

---

## Risks

| Risk | Mitigation |
|------|------------|
| Starting S33-06 before S33-04 | Wave plan blocks Unity until sim gate merges |
| Rebuilding S32 evidence infra | S33-10 explicitly extends S32-10 pipeline |
| Phase G scope creep into sim | Lock S33-06 to viewer + staging diff; grep for `DatalinkSidePictureMerger` in Unity adapter = zero |
| Editor unavailable at closeout | Lean PASS WITH NOTES — headless ≥38/38 + protocol PNGs (S32 precedent) |

---

## References

- Sprint plan: `production/sprints/sprint-33-kill-chain-intelligence-comms-integration.md`
- Parallel kickoff: `production/agentic/sprint-33-parallel-kickoff-2026-06-19.md`
- S32 evidence: `production/qa/sprint-32-presentation-evidence-2026-06-19.md`
- S32 C2 sign-off: `production/qa/sprint-32-c2-signoff-2026-06-19.md`
- Stories: `production/epics/sprint-33-platform-editor-phase-g/story-033-06-platform-phase-g-comms.md`, `story-033-10-presentation-evidence.md`
- ADR-010: `docs/architecture/adr-010-headless-first-command-driven-ui.md`