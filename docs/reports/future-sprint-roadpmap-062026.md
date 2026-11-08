# Future Sprint Roadmap — Project Aegis (cmano-clone)

> **Status:** Living document. Authored 2026-06-20 from project state (S38 complete, S39 planned)
> and a GitNexus read of the index @ `90c9a5f` (HEAD) — 17,290 symbols / 34,420 edges / 377 clusters / 300 flows.
> **Stage:** Polish (`production/stage.txt`). **Review mode:** lean (`production/review-mode.txt`).
> **Governing boundary:** `production/polish-scope-boundary-2026-06-19.md`.
>
> This roadmap is **direction, not a commitment**. Per `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md`,
> each sprint is still planned via `/sprint-plan` with user approval. Filename retains the original
> `roadpmap` spelling for link stability — rename to `future-sprint-roadmap.md` is offered separately.

---

## 1. Where we are

| Dimension | State | Evidence |
|---|---|---|
| Stage | **Polish** (sustaining) | `production/stage.txt`, `gate-checks/s38-polish-continuation-2026-06-20.md` |
| Last sprint | **S39 complete** (C2/Platform deeper polish, hygiene, closeout) | `retro-sprint-39-2026-06-20.md` |
| Next sprint | **S40 planned** — Horizon 2 Catalog/Import + perf P1 | `sprints/sprint-40-deeper-polish-catalog-import-perf.md` |
| Test baseline | **≥1215** headless, **ReplayGolden 6/6**, **C2 proxy 18/18** | `qa/smoke-sprint-39-closeout-2026-06-20.md` |
| Determinism | Baltic world hash **`17144800277401907079`** immutable; ZERO DelegationBridge changes | smoke + retro |
| Release readiness | **CONCERNS** — do not advance | gate-check S38 continuation |

**Why Release is gated (carried, not regressions):**
1. Not feature/content complete (post-MVP tracker rows still Partial).
2. Art bible is **lean** (C2 + Editor only); full 9-section + asset specs deferred.
3. Launch artifacts absent (release checklist, store, localization) — correct for phase, but required for the gate.
4. Multiple deeper-Polish carryovers remain (perf P1, evidence, hygiene, C2/Platform residuals).

---

## 2. Code-grounded landscape (GitNexus)

Top modules by symbol count and cohesion — these set where polish vs. hardening effort lands.

| Module | Symbols | Cohesion | Roadmap signal |
|---|---:|---:|---|
| `Catalog` | 501 | 70% | Largest surface (and growing). Import/quarantine/provenance flows (`CatalogJsonImporter`, `MountLoadoutQuarantineTriage`). High-value but high blast-radius — gate every edit with `impact()`. |
| `Platform` | 420 | 73% | Editor viewer + projections (`PlatformCatalogViewer`, `PlatformCatalogFilterProjection`). Primary deeper-polish target (density/tooltips/surfacing). |
| `Projection` | 297 | 75% | C2/Platform read-models. Polish edits should stay projection-side, not in Sim. |
| `Import` | 135 | **67%** | Markdown/catalog import (`CmoMarkdownImportProposer`). Cohesion slipped (was 72%) → emerging debt; watch alongside Osint. |
| `Osint` | 48 | **68%** | OSINT mapping (`OsintCatalogMapper`). Low cohesion → refactor candidate. |
| `Engage` / `Sensors` | 134 / 98 | 80% / 76% | Deterministic combat/detection spine. **Frozen for polish** except via golden-replay-gated fixtures. |
| `WriteGate` | 127 | 75% | `CatalogWriteGate` — extend-only invariant. Grew + cohesion slipped (was 81%); treat as contract, never relax. |
| `Decision` | 72 | **60%** | **Lowest cohesion in the codebase** → top hygiene/refactor candidate post-Polish. |
| `Telemetry` | 63 | **67%** | Low cohesion; instrumentation sprawl. Hygiene candidate. |

**Interpretation for the roadmap:**
- Deeper-Polish work concentrates in `Platform` + `Projection` + `Catalog` (read-model & editor surfaces) — exactly where S39 is aimed.
- The Sim spine (`Engage`, `Sensors`, `Baltic`, `Runtime`) is mature and gate-protected; touch only through fixtures.
- **Structural-debt cluster:** `Decision` (60%), `Import` (67%), `Telemetry` (67%), `Osint` (68%) are the clearest low-cohesion signals. `Import` and `WriteGate` cohesion *slipped* this index vs. the prior — early creep worth a dedicated hygiene track *after* the Polish boundary lifts.

---

## 3. Roadmap horizons

The roadmap runs on **two parallel tracks** that must not be conflated:

- **Track A — Deeper Polish (in-boundary):** S39 → S41. Strictly inside `polish-scope-boundary`. No new features.
- **Track B — Release Enablement (out-of-boundary):** requires an explicit **scope-expansion decision** before it can start. Sequenced here so the decision is informed, not so it is pre-approved.

```
 Polish sustain ──► S39 ──► S40 ──► S41 ──┐
 (Track A, in-boundary)                   │ exit criteria met?
                                          ▼
                          [SCOPE DECISION GATE] ──► Track B (Release Enablement) ──► Release gate
```

### Horizon 1 — S39 (planned): Deeper Polish: C2/Platform · Hygiene · Perf · Evidence · Replay
Already specced in `sprints/sprint-39-deeper-polish-c2-platform-hygiene.md`. Summary:
- C2 + Platform Editor residual polish (density/tooltip/surfacing) — `Platform`/`Projection` only.
- Hygiene/tests-layout + CI refinement (hybrid layout retained).
- Perf P1 follow-up + re-profile deltas vs S38; replay-golden maintenance.
- Evidence/PNG refresh + playtest session 11; dispatching-parallel-agents refinements.
- **Exit:** ≥1213 tests, Replay 6/6, proxy 18/18+, Baltic hash unchanged, all stories cite boundary.

### Horizon 2 — S40 (proposed): Deeper Polish continuation — Catalog/Import surfacing + perf P1 burn-down
Code-grounded focus from GitNexus hotspots:
- **Catalog/Import polish:** surface provenance/quarantine outcomes in the Editor read-models (`Projection`-side) — make `MountLoadoutQuarantineTriage` / `CatalogJsonImporter.WriteQuarantineRows` results legible without touching write paths. `impact()` required before any `Catalog` symbol edit.
- **Perf P1 burn-down:** convert S39 re-profile deltas into concrete fixes; appendix to `perf-profile-polish-baseline`.
- **Replay/determinism maintenance:** extend golden fixtures only; no production-hash change.
- **Evidence cadence:** playtest 12 + targeted PNGs.
- **Exit:** same gate set as S39; no new module dependencies introduced (verify via `detect_changes`).

### Horizon 3 — S41 (proposed): Polish hardening + Release-readiness pre-flight (still in-boundary)
- **Structural-debt spike (read-only first):** characterize `Decision` (60%) and `Telemetry` (67%) cohesion problems — produce a refactor ADR, *no code change in Polish*. Tees up Track B.
- **Determinism audit pass:** full `/determinism-audit` + `/replay-verify` sweep; keep GitNexus index re-baselined to HEAD (currently current @ `90c9a5f`).
- **Evidence/perf consolidation:** single Polish-exit evidence pack (perf, replay, proxy, playtest corpus).
- **Release pre-flight checklist (gap analysis only):** enumerate exactly what Track B requires; do not produce launch artifacts yet.
- **Exit:** a written **Polish-exit report** + the scope-expansion decision packet for the gate below.

---

## 4. The scope-expansion decision gate

Track B cannot start inside the current boundary. Before it does, the user/creative-director must decide:

- Lift/replace `polish-scope-boundary-2026-06-19.md`? With what new scope?
- Which post-MVP tracker rows move from Partial → committed?
- Budget for full art bible completion + launch artifacts?

**Inputs to that decision** are produced by S41 (Polish-exit report + gap analysis). Until then, Track B below is *planning only*.

---

## 5. Track B — Release Enablement (out-of-boundary, requires decision)

Sequenced, not scheduled. Each is a multi-sprint epic.

| Epic | What it covers | Code-grounded notes | Prereq |
|---|---|---|---|
| **B1 — Content completeness** | Close post-MVP tracker Partial rows | Likely concentrated in `Catalog`/`Platform`/`Scenario` data + `Engage` features | Scope decision |
| **B2 — Art bible full** | Expand lean bible to full 9-section + asset specs | `design/art/art-bible.md` | Scope decision |
| **B3 — Structural-debt refactor** | Refactor `Decision` (60%) + `Telemetry` (67%); audit `Osint` (68%) | Use `rename`/`impact` (call-graph aware); golden-replay must stay 6/6 | S41 ADR |
| **B4 — Performance scale-out** | Beyond P0/P1 — DOTS/ECS paths currently out of boundary | `Runtime`/`Sensors`/`Engage`; coordinate with determinism-engineer | B1 scope |
| **B5 — Launch artifacts** | Release checklist, store pages, localization pipeline | `localization-lead`, `release-manager` | B1+B2 |
| **B6 — Release gate** | `/gate-check` Polish→Release | All gates + full content + artifacts | B1–B5 |

---

## 6. Standing invariants (apply to every sprint, both tracks)

Per `CLAUDE.md` + observed gates — a sprint **fails** if any regress:

1. **Determinism:** controllers/sim are pure functions of (observed state, traits, seed). Baltic world hash stays `17144800277401907079` unless a fixture change is explicitly golden-updated.
2. **ReplayGolden 6/6** and **C2 proxy 18/18+** every sprint.
3. **CatalogWriteGate is extend-only**; **ZERO DelegationBridge** changes in Polish.
4. **Test baseline never regresses** (currently ≥1213).
5. **GitNexus discipline:** `impact()` before editing any symbol (warn on HIGH/CRITICAL); `detect_changes()` before commit; `rename` (not find/replace) for renames. **Re-index when stale** — currently current @ HEAD `90c9a5f`; refresh after merges with `node .gitnexus/run.cjs analyze`.
6. **Boundary citation:** every Polish story cites `polish-scope-boundary-2026-06-19.md`.

---

## 7. Risk register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Polish drifts into feature creep | Med | High | Boundary citation enforced per story; producer advisory gate |
| `Catalog`/`Platform` edits exceed blast radius | Med | High | Mandatory `impact()`; keep edits projection-side |
| Determinism/replay regression from "harmless" polish | Low | Critical | Golden-replay gate; determinism-audit each horizon |
| Decision/Telemetry debt compounds | Med | Med | S41 read-only spike → ADR; defer refactor to B3 |
| GitNexus index drift hides blast radius | Med | Med | Re-baseline to HEAD in S41 (and whenever >N commits behind) |
| Release pressure forces premature gate | Med | High | Scope-decision gate is explicit; gate-check stays CONCERNS until prereqs met |

---

## 8. Open questions for the user

1. **Horizon depth:** How many deeper-Polish sprints (S39–S41 proposed) before forcing the scope-expansion decision?
2. **Debt timing:** Refactor `Decision`/`Telemetry` as a Polish-adjacent hardening sprint, or strictly post-boundary (B3)?
3. **Filename:** Rename this file to `future-sprint-roadmap.md` (fix `roadpmap` typo)?

---

## 9. S42–S48 decomposition (Track B → sprint mapping)

> **Status:** Scope-expansion decision **APPROVED** (2026-06-20): [`production/gate-checks/scope-expansion-decision-2026-06-20.md`](../production/gate-checks/scope-expansion-decision-2026-06-20.md). **S42 blocked until S41 closeout** (S40 in progress). Track B boundary: [`production/release-enablement-scope-boundary-2026-06-20.md`](../production/release-enablement-scope-boundary-2026-06-20.md). **Agent execution plan (S40–S48):** [`s40-s48-local-cloud-agent-execution-plan-2026-06-20.md`](s40-s48-local-cloud-agent-execution-plan-2026-06-20.md).

| Sprint | Track B epic(s) | Primary goal | Sprint plan | Parallel kickoff | Est. calendar | Dispatch status |
|--------|-----------------|--------------|-------------|------------------|---------------|-----------------|
| **S42** | **B1** wave 1 + **B2** start | First committed tracker rows (Catalog/Platform/Scenario); art bible §1–4; expanded gate matrix | `production/sprints/sprint-42-release-kickoff-content-art-bible-w1.md` | `production/agentic/sprint-42-parallel-kickoff-2026-06-20.md` | ~10–12 days | **APPROVED** — ⛔ S41 closeout pending |
| **S43** | **B1** wave 2 + **B2** complete | Engage/features batch + Platform/scenario remainder; full 9-section art bible + asset specs | `production/sprints/sprint-43-content-wave2-art-bible-complete.md` | `production/agentic/sprint-43-parallel-kickoff-2026-06-20.md` | ~10–12 days | **APPROVED** — after S42 |
| **S44** | **B3** | Decision/Telemetry refactor + Osint audit; golden-replay gated (requires S41 ADR) | `production/sprints/sprint-44-structural-debt-refactor.md` | `production/agentic/sprint-44-parallel-kickoff-2026-06-20.md` | ~10–14 days | **APPROVED** — requires S41 ADR |
| **S45** | **B4** | Performance scale-out (Runtime/Sensors/Engage); determinism-engineer paired (requires B1 locked) | `production/sprints/sprint-45-performance-scale-out.md` | `production/agentic/sprint-45-parallel-kickoff-2026-06-20.md` | ~10–14 days | **APPROVED** — requires B1 lock |
| **S46** | **B5** | Release checklist, store pages, localization pipeline (requires B1+B2) | `production/sprints/sprint-46-launch-artifacts.md` | `production/agentic/sprint-46-parallel-kickoff-2026-06-20.md` | ~8–10 days | **APPROVED** — after S43 |
| **S47** | **B6** prep | Full gate-check dry run, consolidated evidence, CI/Buildkite sign-off | `production/sprints/sprint-47-release-dry-run.md` | `production/agentic/sprint-47-parallel-kickoff-2026-06-20.md` | ~5–7 days | **APPROVED** — after S46 |
| **S48** | **B6** | `/gate-check` Polish→Release; stage advance; program closeout (human verdict) | `production/sprints/sprint-48-release-gate.md` | `production/agentic/sprint-48-parallel-kickoff-2026-06-20.md` | ~3–5 days | **APPROVED** — requires S47 Go |

### Track A cross-reference (in-boundary, for program continuity)

| Sprint | Horizon | Sprint plan | Kickoff |
|--------|---------|-------------|---------|
| **S39** | §Horizon 1 | `production/sprints/sprint-39-deeper-polish-c2-platform-hygiene.md` | `production/agentic/sprint-39-parallel-kickoff-2026-06-20.md` | **COMPLETE** 2026-06-20 |
| **S40** | §Horizon 2 | `production/sprints/sprint-40-deeper-polish-catalog-import-perf.md` | `production/agentic/sprint-40-parallel-kickoff-2026-06-20.md` |
| **S41** | §Horizon 3 | `production/sprints/sprint-41-polish-hardening-release-preflight.md` | `production/agentic/sprint-41-parallel-kickoff-2026-06-20.md` |

### Program infrastructure (S39–S48)

| Artifact | Path |
|----------|------|
| Worktree manifest | `production/agentic/s39-s48-worktree-manifest.md` |
| Local/cloud routing | `production/agentic/local-cloud-agent-routing.md` |
| Scope gate template | `production/gate-checks/scope-expansion-decision-template-2026-06-20.md` |
| Program execution guide | `production/agentic/s39-s48-program-execution-guide.md` |
| Track B pre-flight checklist | `production/agentic/sprint-42-48-readiness-checklist.md` |

### Epic exit criteria (summary)

| Epic | Exit when |
|------|-----------|
| B1 | All scope-gate-committed tracker rows Partial→committed; S43 closeout |
| B2 | 9-section art bible + asset specs; S43 closeout |
| B3 | S41 ADR items addressed; cohesion targets met; S44 closeout |
| B4 | Perf scale-out targets met; determinism sign-off; S45 closeout |
| B5 | Checklist + store + i18n pipeline; S46 closeout |
| B6 | Gate-check APPROVED; stage Release; S48 closeout |

**Total program (S39–S48):** ~85–110 calendar days with parallel tracks inside each sprint.

---

*Grounded in: GitNexus index @ `90c9a5f` (HEAD); `gate-checks/s38-polish-continuation-2026-06-20.md`; `retro-sprint-38-2026-06-20.md`; `sprints/sprint-39-deeper-polish-c2-platform-hygiene.md`; `clusters` + `processes` resources. Update via `/sprint-plan` and re-run GitNexus analyze when the index falls behind HEAD.*
