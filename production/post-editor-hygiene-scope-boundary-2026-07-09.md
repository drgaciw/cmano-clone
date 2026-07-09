# Post-Editor Hygiene Scope Boundary — Engineering Hygiene Program (S89–S92)

**Date:** 2026-07-09  
**Status:** S89-01 deliverable (boundary first per `roadmap-execute-plan-07092026.md` §3/§4). Published before S89 artifact tracks dispatch. Supersedes `production/scenario-editor-scope-boundary-2026-07-04.md` for **S89+ only** (archive prior, do not delete; carry invariants only).  
**Authority:** [`docs/reports/future-sprint-roadpmap-07092026.md`](../docs/reports/future-sprint-roadpmap-07092026.md) §3/§6/§7/§10 + [`docs/reports/roadmap-execute-plan-07092026.md`](../docs/reports/roadmap-execute-plan-07092026.md) §3/§4 + [`production/scenario-editor-scope-boundary-2026-07-04.md`](./scenario-editor-scope-boundary-2026-07-04.md) (invariants only) + [`production/platform-editor-completion-scope-boundary-2026-07-09.md`](./platform-editor-completion-scope-boundary-2026-07-09.md) + AGENTS.md + [`docs/reports/tech-stack-agent-skill-recommendations-2026-07-08.md`](../docs/reports/tech-stack-agent-skill-recommendations-2026-07-08.md)  
**Lead Epic:** Post-editor engineering hygiene + asset spec production  
**Sprints:** S89–S92 (serial; 2–4 parallel tracks inside each; local coordinator owns boundary/closeout/human; cloud for docs/tests/hygiene)  
**Exit:** S92 hygiene gate (human ack **"post-editor hygiene program complete"**; **stage remains Release**; no `production/stage.txt` advance unless explicit separate decision)

---

## Purpose

S89–S92 focuses on **post-editor engineering hygiene** after S81–S88 scenario editor, Mission Editor Phase 2, and Platform Editor completion: invariant floor updates (1599/20/20), agent/skill P0 doc sync, asset manifest ASSET-001…003 production specs, UA engage-test hygiene, and program gate. **Hygiene + specs only** — no Launch stage advance, no store submission, no Baltic reopen, no DelegationBridge edits, no CatalogWriteGate write-path rewrites.

**Stage policy:** Remains **Release** throughout S89–S92. S92 documents hygiene-complete only; any Launch decision is separate/future.

Every story / artifact / commit **must** cite this boundary + `future-sprint-roadpmap-07092026.md` + `roadmap-execute-plan-07092026.md` + prior editor boundaries (invariants) + AGENTS.md.

---

## Standing Invariants (updated post-PE floor; supersede for S89+)

| Gate | Command / check | Pass criterion |
|------|-----------------|----------------|
| Build | `dotnet build ProjectAegis.sln` | 0 errors (warnings allowed if pre-existing nonblock) |
| Tests | `dotnet test ProjectAegis.sln -v minimal` | **≥1599 / 0 failed** (monotonic; floor 311 Sim + 260 Del + 286 UA + 24 Excel + 616 Data + 102 Cli) |
| Replay | `--filter ReplayGoldenSuiteTests` | **6/6** |
| C2 proxy | `--filter PlayModeSmokeHarnessTests` | **≥20/20** |
| Hash | `rg -l '17144800277401907079' tests/ data/` | preserved (18 paths) |
| Bridge | No production hotpath edits to `DelegationBridge.cs` | **ZERO** |
| Catalog | `CatalogWriteGate` | **extend-only** — no rewrite of existing write paths |

**GitNexus discipline (mandatory pre every edit/claim/commit):** `list_repos` (canonical `/home/username01/cmano-clone`), `detect_changes`, `impact` (upstream summaryOnly on §5 CRITICALs).

**CRITICAL symbols (§5 post-analyze @ `223a5fe` 2026-07-09):**

| Symbol | Upstream impact | Risk |
|--------|-----------------|------|
| `ScenarioDocumentEditor` | 233 | **CRITICAL** |
| `CatalogWriteGate` | 186 | **CRITICAL** |
| `DelegationBridge` | 145 | **CRITICAL** |
| `PatrolCandidateEngagePolicy` | 113 | **CRITICAL** |
| `BalticReplayHarness` | 54 | **CRITICAL** |

---

## In Scope / Out of Scope

**In scope (S89–S92 hygiene):**

| Sprint | Focus |
|--------|-------|
| **S89** | AGENTS/tracker floor bump to 1599/20/20; UA engage-test hygiene (document/waive/fix if regress); GitNexus freshness hygiene |
| **S90** | Agent/skill P0 sync per tech-stack recs A1–A3 / B1–B3 (Input Manager truth, VERSION.md packages, Claude-Agent-Setup MCP honesty) — **docs only** |
| **S91** | Asset production specs ASSET-001…003 from `design/assets/asset-manifest.md` |
| **S92** | Program gate + human ack; optional architecture-review TR note stub |

**Out of scope (explicit exclusions):**

- Launch stage advance (`production/stage.txt` → Launch)
- E7 store submission / paid marketing / production i18n execution
- Mission Editor Phase 2.4+ mining/layers GUI; visual event graph
- WYSIWYG platform editor; Office.js live add-in
- Baltic v2/v3 reopen; production hash change without ADR
- `DelegationBridge` edits; `CatalogWriteGate` write-path rewrites
- Addressables bulk import / full asset pipeline (beyond ASSET-001…003 specs)
- Full implementation-tracker re-litigation (21/21 closed at S56)

---

## GitNexus Pre (mandatory; 2026-07-09)

**Performed pre boundary publish:**

| Tool | Result |
|------|--------|
| `node .gitnexus/run.cjs analyze` | **24,418** nodes / **47,032** edges / **424** clusters / **300** flows @ `223a5fe` — ✅ up-to-date |
| `impact` CatalogWriteGate | **186 CRITICAL** |
| `impact` DelegationBridge | **145 CRITICAL** |
| `impact` PatrolCandidateEngagePolicy | **113 CRITICAL** |
| `impact` BalticReplayHarness | **54 CRITICAL** |
| `impact` ScenarioDocumentEditor | **233 CRITICAL** |

---

## Verification-Before-Completion (2026-07-09 RUN+READ)

Evidence: [`production/qa/evidence/gates-post-editor-hygiene-2026-07-09.log`](./qa/evidence/gates-post-editor-hygiene-2026-07-09.log)

| Gate | Result |
|------|--------|
| Build | **0e/0w** |
| Full test | **1599/0f** |
| ReplayGolden | **6/6** |
| C2 proxy | **20/20** |
| Hash | **18** paths preserved |
| UA engage filter | **3/3** (green at gate time) |
| Stage | **Release** |

**All gates PASS.** Low risk for docs-only S89-01 boundary.

---

## Exit Criteria (S92)

- [ ] S89–S91 closeouts PASS per sprint-status.yaml
- [ ] AGENTS.md + tracker cite **≥1599** / **≥20/20** floors
- [ ] ASSET-001…003 spec files refined under `design/assets/specs/`
- [ ] S90 agent/skill P0 doc sync complete (tech-stack recs A1–A3 / B1–B3)
- [ ] S92 gate artifact + human ack **"post-editor hygiene program complete"**
- [ ] Stage remains **Release**
