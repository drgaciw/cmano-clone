# Baltic v3 Content Expansion (S73–S80) — Design Spec

**Date:** 2026-06-25  
**Project:** Project Aegis (cmano-clone)  
**Status:** Approved for planning and S73 dispatch  
**Authority:** [`future-sprint-roadpmap-062526.01.md`](../../reports/future-sprint-roadpmap-062526.01.md), user decisions 2026-06-25  
**Prior programs:** S57–S64 Baltic v2 (reference pattern), S69–S72 E7 prep (COMPLETE)

---

## 1. Goal

Expand Baltic theater content to a **v3 corpus** — new scenarios, theater OOB, mission narrative, catalog slices, C2 UX, and playtest loop v3 — without regressing shippable v2 baseline or release-train / E7 prep artifacts.

**Program exit:** S80 Baltic v3 **content-complete** gate (automated + human playtest sign-off). **Not** E7 store submission (prep done at S72). **Not** automatic Launch stage advance.

**In scope (S73–S80):**

- New policies under `baltic-v3-*` prefix in `data/scenarios/`
- Isolated replay goldens in `tests/regression/replay-golden-baltic-v3-*.txt`
- Playtest manifest v3 + human session template
- Catalog/platform content for v3 OOB (extend-only `CatalogWriteGate`)
- C2 scenario picker v3 (additive UI)
- Optional v3 corpus promotion to production at S80 (explicit human decision only)

**Out of scope:**

- E7 store submission, paid marketing, production locale translation
- Multiplayer, global campaigns
- `DelegationBridge` edits
- Production hash `17144800277401907079` change without golden ADR
- Full implementation-tracker row re-litigation (21/21 closed at S56)

---

## 2. Architecture

### Program shape

| Dimension | Value |
|-----------|-------|
| Sprints | **8** serial (S73 → S80) |
| Parallelism | 2–4 tracks **inside** each sprint |
| Lead epic | **E9** Baltic v3 content (S74–S80); S73 foundations |
| Stage | **Release** throughout |
| Boundary | `production/baltic-v3-scope-boundary-2026-06-25.md` (@ S73-01) |

### Content isolation model

```
baltic-v2-*  (frozen shippable baseline — S57–S66)
     │
     │  read / reference
     ▼
baltic-v3-*  (isolated development — S74–S79)
     │
     │  optional promotion @ S80 + ADR
     ▼
production corpus + UnifiedReleaseTrainManifest (if decided)
```

v3 work must not mutate v2 goldens or production Baltic hash without ADR. New goldens start isolated; ReplayGolden suite may grow monotonically when promoted.

### Agent dispatch

Same model as S57–S64 and S65–S72:

- **Local:** boundary publish, closeout, playtest capture, S80 gate, human ack
- **Cloud:** scenario code, replay goldens, catalog ingest, C2 UI (additive)
- **Worktrees:** `.worktrees/stack/sprint{N}/{track-slug}/`
- **Merge:** Graphite `gt submit` → closeout `gt restack` → gate re-run

---

## 3. Sprint map (committed)

| Sprint | Theme | Primary deliverables |
|--------|-------|---------------------|
| **S73** | Foundations | `baltic-v3-scope-boundary-2026-06-25.md`, playtest manifest v3, GitNexus re-index |
| **S74** | Scenario wave 2 | `baltic-v3-*` policies, isolated replay goldens |
| **S75** | Theater v3 | Extended OOB, optional regional slice spike |
| **S76** | Mission / narrative | Contact-window arcs, mission-event policies |
| **S77** | Catalog + platform | Unit/loadout slices, Excel round-trip |
| **S78** | C2 UX v3 | Scenario picker bands, tooltips |
| **S79** | Playtest loop v3 | Automated batch + human session template |
| **S80** | Gate | `s80-baltic-v3-content-gate-*.md`, human ack |

Detailed track tables: [`roadmap-execute-plan-062526.01.md`](../../reports/roadmap-execute-plan-062526.01.md) §4.

---

## 4. GitNexus & hot symbols

Mandatory preflight every track: `list_repos` (canonical path), `detect_changes`, `impact` upstream on CRITICALs.

| Symbol | Risk | S73–S80 rule |
|--------|------|--------------|
| `DelegationBridge` | CRITICAL (127) | ZERO touch |
| `CatalogWriteGate` | CRITICAL (178) | Extend-only; single owner (S77) |
| `PatrolCandidateEngagePolicy` | CRITICAL (97) | S73 E1 only if new AAR topic; single owner |
| `BalticReplayHarness` | CRITICAL (52) | Isolated fixtures; verify at S80 |
| `UnifiedReleaseTrainManifest` | MED | Read until S80 promotion decision |

Expected impact counts unchanged from prior boundaries (178/97/127/52 exact).

Re-index at S73 open and after each sprint merge.

---

## 5. Standing invariants

Every sprint **fails** if any regresses:

1. Test floor **≥1232** (monotonic)
2. ReplayGolden **6/6** minimum (may increase with isolated v3 goldens pre-promotion)
3. C2 proxy **18/18+**
4. Production hash **17144800277401907079** unless golden ADR
5. ZERO `DelegationBridge` source edits
6. `CatalogWriteGate` extend-only
7. GitNexus `impact()` before symbol edits; `detect_changes()` before commit
8. Every artifact cites `baltic-v3-scope-boundary-2026-06-25.md` + epic ID

---

## 6. Testing & verification

**Per sprint close (automated):**

```bash
cd /home/username01/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~PlayModeSmokeHarnessTests"
```

**S79–S80 additionally:**

- Automated playtest batch against full v3 manifest
- ≥1 human session per difficulty band (artifacts in `production/playtests/human/`)
- Fun-hypothesis refresh documented

**S80 gate:** Full RUN+READ verification-before; compile evidence from S73–S79 closeouts.

---

## 7. Risks & mitigations

| Risk | Mitigation |
|------|------------|
| v3 breaks production hash | `baltic-v3-*` prefix; ADR before promotion |
| Catalog contention S77 | Single `CatalogWriteGate` owner; stagger from S74 |
| Playtest without UX bands | Lock C2 picker v3 at S78 before S79 human sessions |
| Scope creep to E7 submission | Boundary out-of-scope list; checklist v3 already complete |
| GitNexus stale index | Re-index S73-03 + post-merge |

---

## 8. Related artifacts

| Artifact | Path |
|----------|------|
| Roadmap | `docs/reports/future-sprint-roadpmap-062526.01.md` |
| Execute plan | `docs/reports/roadmap-execute-plan-062526.01.md` |
| v2 reference | `production/baltic-v2-scope-boundary-2026-06-22.md` |
| v2 manifest | `production/playtests/baltic-v2-scenario-manifest.yaml` |
| E7 prep (archived) | `production/commercial-launch-scope-boundary-2026-06-25.md` |
| S72 gate | `production/gate-checks/s72-commercial-launch-prep-gate-2026-06-25.md` |

---

## 9. Approval & next steps

**Approved:** 2026-06-25 (user: proceed after roadmap review)

**Next steps (do not skip):**

1. Publish `production/baltic-v3-scope-boundary-2026-06-25.md` (S73-01)
2. Create `production/sprints/sprint-73-baltic-v3-foundations.md` + kickoff
3. `/qa-plan sprint 73`
4. Dispatch S73 tracks per [`roadmap-execute-plan-062526.01.md`](../../reports/roadmap-execute-plan-062526.01.md) §8

**Implementation planning:** Use superpowers `writing-plans` only for individual sprint stories after `/sprint-plan` — the execute plan is the program-level orchestration doc.
