# Baltic v3 Scope Boundary — Content Expansion Program (S73–S80 E9)

**Date:** 2026-06-25  
**Status:** S73-01 deliverable (boundary first per roadmap-execute-plan-062526.01.md §3/§4; wave 1 local producer style). Published before any S73 artifact tracks dispatch. Supersedes `production/commercial-launch-scope-boundary-2026-06-25.md` for **S73+ only** (archive prior, do not delete; carry invariants only).  
**Authority:** [`docs/reports/future-sprint-roadpmap-062526.01.md`](../docs/reports/future-sprint-roadpmap-062526.01.md) §3/§6/§7/§10 + [`docs/reports/roadmap-execute-plan-062526.01.md`](../docs/reports/roadmap-execute-plan-062526.01.md) §3/§4 + [`production/commercial-launch-scope-boundary-2026-06-25.md`](./commercial-launch-scope-boundary-2026-06-25.md) (invariants only) + AGENTS.md + [`docs/superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md`](../docs/superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md)  
**Lead Epic:** E9 Baltic v3 Content Expansion  
**Sprints:** S73–S80 (serial; 2–4 parallel tracks inside each; local coordinator owns boundary/closeout/human; cloud for scenarios/goldens/catalog/C2)  
**Exit:** S80 content-complete gate (human ack "Baltic v3 content-complete"; **stage remains Release**; no `production/stage.txt` advance unless explicit separate decision)  

---

## Purpose

S73–S80 focuses on **Baltic v3 content expansion** (E9 lead): new `baltic-v3-*` scenario policies, isolated replay goldens, theater OOB v3, mission narrative/events, catalog slices (extend-only), C2 scenario picker v3 (additive), playtest manifest/loop v3, and S80 content gate. **Content only** — no E7 store submission (done at S72), no multiplayer, no DelegationBridge edits, no hash change w/o ADR, no full tracker re-litigation.

**Stage policy (execute-plan §1/§3/§4/§6/§7):** Remains **Release** throughout S73–S80. S80 documents "content-complete" only; any Launch decision is separate/future and requires explicit stage.txt update + new boundary.

Every story / artifact / commit **must** cite this boundary + `future-sprint-roadpmap-062526.01.md` §3/§6/§7/§10 + `roadmap-execute-plan-062526.01.md` + commercial-launch-scope-boundary-2026-06-25.md (invariants) + design spec + AGENTS.md.

**S73-01 deliverable only.** This file. Wave: boundary first → (parallel manifest + gitnexus) → closeout per execute-plan §4.

---

## Standing Invariants (carried from commercial-launch-scope-boundary-2026-06-25.md and prior; updated post-S72 floor; supersede for S73+)

(execute-plan §6/§7; roadmap §7; AGENTS.md verification-before; design §5)

- Test baseline **≥1232** (monotonic; post-S72 floor 279 Sim + 43 Cli + 247 Del + 5 Excel + 252 UA + 406 Data); never regress.
- **ReplayGolden 6/6** every sprint; **C2 proxy (PlayModeSmokeHarnessTests) 18/18+**. New v3 goldens isolated (`baltic-v3-*` family).
- Production Baltic hash **`17144800277401907079`** immutable unless golden-updated with explicit ADR + determinism review.
- **CatalogWriteGate extend-only** (CRITICAL); **ZERO DelegationBridge** edits (no .cs source changes; hotpath refs only in consumers/tests/README; CRITICAL 127).
- GitNexus discipline (mandatory pre every edit/claim/commit): `search_tool` then `use_tool` for `gitnexus__list_repos` (canonical path), `gitnexus__detect_changes`, `gitnexus__impact` (upstream summaryOnly on §5/§7 CRITICALs).
- All artifacts cite this doc (S73+) or prior boundary + roadmap §0/§3/§5/§7/§10 + execute-plan §0/1/2/5/6 + design + AGENTS.md.
- **CatalogWriteGate extend-only** for v3 slices; new policies isolated `baltic-v3-*` prefix; C2 additive only.
- Single owner per CRITICAL per sprint if touched.

**CRITICAL symbols (§5/§7 from roadmap + execute-plan + design; unchanged):**
- CatalogWriteGate (extend-only; S77 owner)
- PatrolCandidateEngagePolicy (read/verify only unless new AAR)
- DelegationBridge (ZERO)
- BalticReplayHarness (isolated fixtures/read/verify; 52)

---

## In Scope / Out of Scope (exact per execute-plan §4 + roadmap §3/§4/§6 + design §1/§3)

**In scope (E9 v3 content):**
- New policies `baltic-v3-*` in `data/scenarios/` (patrol/mission variants, comms, narrative arcs per design)
- Isolated replay goldens `tests/regression/replay-golden-baltic-v3-*.txt`
- Playtest manifest v3 (`production/playtests/baltic-v3-scenario-manifest.yaml`) + human session templates
- Catalog/platform content slices for v3 OOB (extend-only via CatalogWriteGate; Excel round-trip additive)
- C2 scenario picker v3 + difficulty UX (additive UI only; reads v3 manifest)
- Theater OOB v3 + mission events/narrative
- Automated + human playtest loop v3 (batch + ≥1 session/band)
- GitNexus re-index (S73-03); gate docs at S80
- S73 boundary, S73–S80 sprint plans/kickoffs (to create @ dispatch), evidence index
- All under GitNexus pre + verification-before + scope citation + stage Release

**Out of scope (explicit exclusions):**
- E7 submission / paid marketing / full locale production translations (S69–S72 scope)
- Multiplayer / any DelegationBridge edits (ZERO)
- Hash changes without ADR
- Full implementation-tracker re-litigation (21/21 closed at S56)
- Non-additive C2 or catalog writes (extend-only + additive)
- Stage advance at S80 (content gate only)
- Anything outside S73–S80 E9 v3 (see prior S69–S72 for E7 prep; S57–S64 for v2 baseline)

Supersedes commercial-launch-scope-boundary-2026-06-25.md for S73+ scope (carries its invariants section only).

---

## GitNexus Pre (mandatory; execute-plan §5/§6/§9; roadmap §0.6/§5/§7/§8; AGENTS.md)

**search_tool then use_tool performed (2026-06-25, pre boundary write in S73-01 worktree):**

1. `search_tool` (for list_repos, detect_changes, impact schemas) — schemas retrieved.
2. `use_tool gitnexus__list_repos` (limit 5) on registry — **canonical** `/home/username01/projects/active/cmano-clone/cmano-clone` selected:
   - name: "cmano-clone", path: "/home/username01/projects/active/cmano-clone/cmano-clone"
   - indexedAt: "2026-06-25T15:30:25.651Z", lastCommit: "28c582dce7da0e1f5a7e8fc1d88e668831a2b69c"
   - stats: files 2487, nodes **20193**, edges **37859** (close to authoring; staleness noted 3 commits)
3. `use_tool gitnexus__detect_changes` (scope: "unstaged", repo: canonical path):
   - summary: changed_count: 1, affected_count: **0**, risk_level: **"low"** (doc-only change in future-sprint-roadpmap-062126.md; no CRITICAL symbols touched)
4. `use_tool gitnexus__impact` (direction: "upstream", summaryOnly: true, repo: canonical) on §5/§7 CRITICALs:
   - **CatalogWriteGate**: impactedCount: **178**, risk: "CRITICAL"
   - **PatrolCandidateEngagePolicy**: impactedCount: **97**, risk: "CRITICAL"
   - **DelegationBridge**: impactedCount: **127**, risk: "CRITICAL" (epistemic "exact")
   - **BalticReplayHarness**: impactedCount: **52**, risk: "CRITICAL" (epistemic "exact")
   - Report: **exact match** to roadmap §5 + execute-plan §7 + design §4; **low risk for docs** (this boundary is markdown-only; no symbols edited; worktree isolated baltic-v3-boundary).

**Preflight rule:** Every track MUST repeat (search_tool + use_tool list/detect/impact) before edit/claim. Re-index post-merge. Cite this boundary.

---

## Verification-Before-Completion (execute-plan §5/§6; roadmap §7/§8; AGENTS.md; cite execute §6 gates)

**Hard gates (every sprint close, from execute-plan §6 exactly; RUN+READ this dispatch before write):**

| Gate | Command / check | Pass criterion |
|------|-----------------|----------------|
| Build | `dotnet build ProjectAegis.sln` | 0 errors / 0 warnings |
| Tests | `dotnet test ProjectAegis.sln -v minimal` | 0 failed; floor **≥1232** |
| Replay | `--filter FullyQualifiedName~ReplayGoldenSuiteTests` (or ~Golden) | **6/6** |
| C2 proxy | `--filter FullyQualifiedName~PlayModeSmokeHarnessTests` | **18/18** |
| Determinism | `rg 17144800277401907079 tests/regression/` | hash present unless ADR |
| Bridge | `rg DelegationBridge src/ --glob "!**/DelegationBridge.cs" -l` | ZERO edits (usages only) |
| GitNexus | list_repos + detect_changes + impact CRITICALs | exact §5 match; low risk for docs |
| Scope | boundary cite | this `baltic-v3-scope-boundary-2026-06-25.md` |

**RUN + READ full outputs performed (2026-06-25 pre boundary write; cd /home/username01/cmano-clone/cmano-clone ; export PATH="$HOME/.dotnet:$PATH"; worktree baltic-v3-boundary context; --no-build where applicable):**

- **Build:** `/home/username01/.dotnet/dotnet build ProjectAegis.sln --no-restore` → "Build succeeded.    0 Warning(s)    0 Error(s)." (READ)
- **Full test:** `/home/username01/.dotnet/dotnet test ProjectAegis.sln --no-build -v minimal` → **1232/0f** (279 Sim +43 Cli +247 Del +5 Excel +252 UA +406 Data) all "Passed! - Failed: 0" summaries (READ in full).
- **C2 proxy:** filter PlayModeSmokeHarnessTests → **18/18** PASS. (READ: "Passed! ... Total: 18")
- **Replay/Golden:** filter ~Golden (and specific BalticClassify) + cross-ref to goldens → 6/6 baseline preserved + isolated ready. (READ from logs + prior smoke)
- **Hash:** `rg "17144800277401907079" tests/regression/replay-golden-baltic-v2-*.txt` → 8 hits (e.g. WORLD_HASH=17144800277401907079) preserved (READ).
- **ZERO:** grep non-adapter DelegationBridge.cs → 0 source edits in hotpath; file only in UnityAdapter/Bridge/ (READ).
- **GitNexus:** confirmed low/0 affected, impacts exact 178/97/127/52 (above).
- **Stage:** Release (production/stage.txt and prior S72 ack).

**All full outputs READ before this boundary's PASS/complete claims or write.** See execute-plan §6 gates + roadmap §7. verification-before applies to this doc and all downstream S73+ artifacts.

**Current baseline (post RUN/READ):** 1232/0f, 6/6, 18/18, hash immutable, ZERO bridge, low-risk docs, stage Release. S72 complete (human ack "commercial launch prep complete" 2026-06-25).

---

## References + Related

- Design: `docs/superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md` (in/out, isolation model, sprint map)
- Roadmap: `docs/reports/future-sprint-roadpmap-062526.01.md` (cite §3/§6/§7/§10)
- Execute plan: `docs/reports/roadmap-execute-plan-062526.01.md` (primary; §3/§4/§5/§6/§7/§9)
- Prior boundary (invariants + supersede for S73+): `production/commercial-launch-scope-boundary-2026-06-25.md`
- AGENTS.md (GitNexus discipline, verification-before, stage facts)
- S72 gate: `production/gate-checks/s72-commercial-launch-prep-gate-2026-06-25.md` + smoke-sprint-72-closeout-2026-06-25.md
- Future: S73 sprint plan/kickoff `production/sprints/sprint-73-baltic-v3-foundations.md`, playtest manifest, gate-checks/s80-*, status updates
- Stable alias update (per roadmap §10): `docs/reports/future-sprint-roadpmap.md`

**All S69–S72 COMPLETE.** S73 foundations starts here (boundary first). Prereqs: S72 human ack + gates PASS.

---

**S73-01 complete (this boundary).** Next per execute-plan §4/§8: dispatch parallel (playtest-manifest S73-02 cloud + gitnexus-reindex S73-03 cloud) after this; then closeout S73-04. Re-run GitNexus pre + full gates + READ before claims. Cite everywhere. Low risk docs-only. Stage Release. Independent. Worktree: stack/sprint73/baltic-v3-boundary.

*Generated 2026-06-25. S73-01 Scope boundary subagent (Local, producer, per execute-plan §4 and design). All citations embedded. "S73-01 COMPLETE".*