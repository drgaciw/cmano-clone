# Project Aegis — Requirements Audit Findings Register

**Scope:** requirements corpus `Game-Requirements/` (docs 01–22, hubs, tracker, traceability) vs trunk `81831e76` (2026-08-29)
**Audit date:** 2026-09-01/02 · **Method:** fresh GitNexus index (46,075 nodes / 92,607 edges / 300 flows; `cypher`, `context`, `impact`), nine read-only reviewers, `rg`/`git` evidence · **Verdict:** **CONCERNS**
**Long-form review:** `Game-Requirements/reviews/requirements-corpus-review-gitnexus-2026-09-02.md` · **Companion draft:** `Game-Requirements/drafts/24-Human-On-The-Loop-Authority.md`

**Severity:** **P0** contradicts code, CI or an accepted ADR now · **P1** shipped behaviour with no spec, or unverifiable claim · **P2** hygiene

## Summary

| Category | P0 | P1 | P2 | Total |
|---|---|---|---|---|
| A. Governance claims not honoured by code | 4 | 0 | 0 | 4 |
| B. Shipped capabilities with no requirement | 6 | 9 | 0 | 15 |
| C. Hub, status and architecture rot | 6 | 3 | 2 | 11 |
| D. Per-document defects | 12 | 10 | 3 | 25 |
| E. Traceability and process | 2 | 6 | 3 | 11 |
| F. Tooling | 0 | 2 | 2 | 4 |
| **Total** | **30** | **30** | **10** | **70** |

Key numbers behind the register: ~700 commits since the tracker's last update (2026-07-09); 71 of 80 Linear DRG ids since then appear in no requirement, tracker or traceability doc; 24 of 621 code identifiers cited by the docs no longer exist; test floors are stated in five contradicting generations (≥1204, ≥1232, ≥1599, ≥1638; measured 1924).

---

## A. Governance claims not honoured by code

| ID | Sev | Docs | Finding | Evidence | Recommendation |
|---|---|---|---|---|---|
| A-01 | P0 | 04 | "Full autonomous lethal engagement requires explicit player opt-in per mission phase" is graded **Shipped**; no such gate exists | `AutonomyGate.Evaluate` returns `ExecuteNow` for `SemiAutonomous`/`FullAutonomous` immediately after the ROE check; zero hits for any opt-in/phase flag in `src/` | Implement HOL-04 (policy field `engage.lethalAutonomyOptIn`, new `AutonomyGateTests` cases) or de-scope with an ADR amendment; change the doc 04 mapping row either way |
| A-02 | P0 | 21, 06 | `balanceCritical` approval gate asserted as done (PLE-3.3 `[x]`, Scope, OQ3; DBI-2.4) | `rg -i 'balance.?critical' src/` = 0; only the >10-record threshold exists in `CatalogWriteGate` | Untick PLE-3.3, mark GAP with a story, or add a `BalanceCritical` column + importer test; fix DBI-2.4 in the same pass |
| A-03 | P0 | 10 | "TL-5 requires `BLACK_PROJECT_MODE`" is not enforced | `SpeculativeEngageGate` gates TL and black-project independently on per-weapon `requiresBlackProject`; `ScenarioSpeculativeSettings(blackProjectMode:false, maxTechnologyLevel:5)` is valid | Add a `ScenarioValidationEngine` rule + test, or reword to per-platform semantics |
| A-04 | P0 | 22 | SWARM-31 §1 "US/NATO catalog platforms expose CEC" gate is unimplemented while the footer says "landed" | `CecCapable` exists only on `CatalogSwarmPlatform`; no nationality/affiliation logic in `Sim/Cec/*` | Downgrade §1 to Partial or implement a `Nationality` → US/NATO + allied opt-in rule; tick boxes 2–5 with test names |

## B. Shipped capabilities with no requirement text

| ID | Sev | Should live in | Finding | Evidence | Recommendation |
|---|---|---|---|---|---|
| B-01 | P0 | new 24 (HOL) | Human-on-the-loop approval queue, escalation/approval-required gate ledger and authority disposition shipped; only doc 04's one-line `HUMAN_IN_LOOP` exists | `PendingApprovalQueue`, `PendingApprovalPanelHost` (DRG-66/67), `EscalationGateProjection` + codes `HOLD_FIRE`/`WEAPONS_TIGHT`/`HIGHER_HQ` (DRG-228), `C2AuthorityProjector` (DRG-209); tests `PendingApprovalQueueTests`, `EscalationGateProjectionTests`, `C2AuthorityProjectorTests` | Land doc 24 (draft written) with HOL-01…10 |
| B-02 | P0 | new 23 (KCX) | Targetability composition, sensor-to-shooter chain and track custody — the "why can't I fire on this track" chain — have zero corpus hits | `TargetabilityAccept*` (DRG-219), `SensorToShooter*` (DRG-207), `TrackCustody*` (DRG-222) with projection tests | Write doc 23 KCX-01…07 |
| B-03 | P0 | new 24 / 07 / 08 | Agent-callable C2 skill contract (propose ≠ authorize) lives outside the corpus | `SkillCatalog`, `SkillEnvelope(Validator)`, `SkillIds` (`c2.track.assess`, `c2.datalink.reason`, `c2.pairing.recommend`, `c2.explain`), `C2CommandIssuance` (DRG-196); contract in `production/docs/skills/agent-c2-skill-contract/` | HOL-05; link the contract from doc 07 INF-7 and doc 08 §3 |
| B-04 | P0 | 15 / new 23 | Contact provenance and identity/classification ledgers shipped; CMD-17/29.x still "Open" | `ContactProvenance*` (DRG-206), `IdentityClass*` (DRG-225) | SEN-09…12 / KCX-02, KCX-06 |
| B-05 | P0 | new 25 (C2N) | C2 nodes, mission packages, network health, task-group gaps, mission-command intent — an entire C2 structure layer with no owning doc | `C2Node*`, `MissionPackage*` (DRG-213), `C2NetworkHealth*` (DRG-214), `TaskGroupCoord*` (DRG-223), `MissionIntent*` (DRG-229) | Write doc 25 C2N-01…04 |
| B-06 | P0 | 07 / new 26 (VER) | The QA gauntlet, CI gate and oracle apparatus has no FR/AC; doc 07 scopes it as "studio process" | `tools/qa-gauntlet` (223 symbols: ladder, stress axes, 16 saboteur mutants, forge), `GauntletOracleEvaluator`, `gauntlet_oracle_eval`, `.buildkite/pipeline.yml`, `gauntlet-oracle.yml`, 11 `UiIa*` oracles | Write doc 26 VER-01…07; re-grade INF-6.x |
| B-07 | P1 | 17 | Combat event stream, engage-explain positive path, after-action ledger | `CombatEvents` (DRG-211), `EngageExplainContract` (DRG-215), `AfterAction` (DRG-218); ADR-019 uncited | RPL-29/30 amendments |
| B-08 | P1 | 14 / new 24 | Threat assessment & weapon recommendation, scarcity ranking, withheld-order next action | `ThreatAssessmentProjection` (DRG-212), `ResourceRankProjection` (DRG-217), `EngageNextActionProjection` (DRG-226) | HOL-06/07, KCX-07, ENG-09…12 |
| B-09 | P1 | 13 / 18 / new 24 | Collateral / CDE advisory; "collateral" appears in no doc | `CdeAssessProjection` (DRG-220), `CdeAssessProjectionTests` | HOL-08, DOM-13 |
| B-10 | P1 | 20 | Live Play Mode picture: kinematic mover, attention toast + auto-pause clock interrupt, combat VFX, datalink edges, planning-dim gate; CMD-31…39 allocated outside the doc | `PlayModeKinematicMover`, `WatchAttentionQueue`, `WatchAutoPauseGate`, `CombatVfxProjection` (#569, S115/116); drafts `docs/superpowers/specs/2026-08-17-cmd-38/39-*` "pending owner approval" | Append CMD-31…43 to doc 20; add §Alerting and Interruption |
| B-11 | P1 | 16 | Ordnance bands (Shotgun/Winchester), engage gates and employment ledger; doc 16 knows only bingo/joker | `OrdnanceStateBands`, `Logistics{Bingo,Shotgun,Winchester}EngageGate`, `OrdnanceStateChangeRecord`, `EmploymentLedger*` (DRG-224) | LOG-12/13 |
| B-12 | P1 | 18 | Own-unit degradation / damage-control advisory | `PlatformDegrade*` (DRG-227) | DOM-12 |
| B-13 | P1 | 11 / new 27 (LIB) | Campaign and scenario library, package loader | `CampaignDocument*`, `CampaignLibrary*`, `ScenarioLibraryProjection/Lister`, `ScenarioPackageLoader`, `ScenarioLibraryPanelHost` (CMD-27) | Write doc 27 LIB-01…04 |
| B-14 | P1 | 21 | Platform Design Assistant and unified platform editor shell | `platform_design_propose`, `PlatformDesignAssistant*`, `PlatformRelativeScaler` (DRG-73); `PlatformEditorShellHost` (#338) | PLE-5.x / PLE-7.x |
| B-15 | P1 | 05 / 07 / 20 | Infrastructure with no requirement: balance telemetry sinks, Hindsight retain hook, `SimBenchmark`, C2 accessibility scale, Cesium globe bridge, Editor batch sign-off runner | `BalanceTelemetry*`, `HindsightOrderLogHook`, `Sim.Benchmark`, `C2AccessibilitySettings`, `CesiumGlobeHost`, `C2PlayModeSignoffBatchRunner` | Telemetry/privacy and sidecar I/O policy in 07/08; presentation constraints in 01/20 |

## C. Hub, status and architecture rot

| ID | Sev | Files | Finding | Evidence | Recommendation |
|---|---|---|---|---|---|
| C-01 | P0 | 01, 03, 04, 06, 07, 09, 10, `architecture.md`, tracker | DOTS/ECS/Burst/BlobAsset presented as live architecture after ADR-005 was superseded (2026-07-07) | doc 01 L163, 03 L95/114, 04 L89, 06 L209/214, 07 L224, 09 L56/263/287/295, 10 L306; `architecture.md` L9/196/205; tracker rows 08/09; 4 agent defs + skill `ecs-data-optimization` | One sweep PR to doc 08 §4 wording; ADR index row → Superseded |
| C-02 | P0 | 00, 01, 03, 04, 06, 07 guides, CI scripts | Test floors stated in five contradicting generations | ≥1204/17 (`tools/buildkite/dotnet-ci.sh`, `verify-ci-local.ps1` headers); ≥1232/18 (`00-Master-Index.md:87`, doc 01, `ci-and-branch-protection.md`); ≥1599 (tracker); ≥1638/≥20 (AGENTS.md); measured 1924/21 (2026-08-17) | Single source (AGENTS.md §Invariants); other files reference it; VER-06 |
| C-03 | P0 | 00, GR index | Both hubs stamped 2026-08-09 describe the S81–S88 train that closed 2026-07-09 | tracker + `post-editor-status-truth-2026-07-09.md`; sprint files to S121; `sprint-status.yaml` stuck at S104 | Dated pointer to the live status source; fix or retire `sprint-status.yaml` |
| C-04 | P0 | tracker | Tracker is the cited status source in every doc header yet is ~687 commits stale and self-contradicting (UA engage "CLOSED" vs "Open on branch") | `implementation-tracker-2026-07-04.md` last commit 2026-07-09; 70 files hard-link the dated filename | Stable alias `implementation-tracker.md` + "post-2026-07-09 delta" section |
| C-05 | P0 | CLAUDE.md, AGENTS.md, tracker | GitNexus counts hand-typed in three generations, none current | 29,391/55,622 (07-27); 24,418/47,032 (07-09); on-disk 46,075/92,607 @ HEAD | Reference `.gitnexus/meta.json`; session-start freshness check |
| C-06 | P0 | 13–20 | Docs still "Draft — ready for design review" and the index still advertises the 2026-05-29 CONCERNS verdicts, although W0–W4 reviews were APPROVED 2026-07-08 | `production/qa/requirements-corpus-w{0..4}-design-review-2026-07-08.md` | Record supersession in the index; set `Status: Approved (W2 2026-07-08; re-verified <date>)` |
| C-07 | P1 | 01/02/03/04/05/06/13 | Doc stamps older than last commit for 10 of 22 docs | git last-touch 2026-08-09 for 01/14–22, 07-27 for 11 vs stamps 07-08/07-09 | Pre-commit hook: staged `requirements/*.md` must bump `Last Updated` |
| C-08 | P1 | 00-Master-Index, `directory-structure.md`, `technical-preferences.md` | CLAUDE.md includes carry the oldest floors (1213/1215) and an unpopulated studio config ("[TO BE CONFIGURED]") | `.claude/docs/technical-preferences.md` every field unset | Populate or stop including; move S36–S39 sign-offs to `production/qa/` |
| C-09 | P1 | `architecture-traceability-index.md`, README, `unity-ci.yml`, VERSION.md | Unity/package pin drift | 6000.3.14f1 vs ProjectVersion 6000.3.22f1; manifest Addressables 2.9.1/Burst 1.8.30 vs VERSION.md 2.3.16/1.8.29 | Pin once, assert from one file |
| C-10 | P2 | tracker | Tracker cites Graphite PR #237 "draft, +9 commits" and a branch base from 2026-07-04 | PRs now at #597 | Regenerate tracker |
| C-11 | P2 | GR index | Roadmap link points at `future-sprint-roadpmap-07042026.md`; three newer roadmaps exist | `docs/reports/` | Retarget to the stable alias |

## D. Per-document defects

| ID | Sev | Doc | Finding | Evidence | Recommendation |
|---|---|---|---|---|---|
| D-01 | P0 | 01 | Release law weaker than CI (≥1232/18-18 vs ≥1638/≥20-20); OV-SC-G5 says FR-01…FR-19 while FR-20 exists | `RequirementsHubContractTests` pins FR-20 | Floors by reference; fix G5; `OV-NFR-nn` IDs |
| D-02 | P0 | 02 | Time-compression bands contradict docs 01/03, the GDD and code; "Loop policy — Shipped" over-claims fog/rebrief | `SimClock.MaxAccelerationFactor = 256`, `TimeCompressionPolicy` | Align bands; split Loop policy into shipped vs residual |
| D-03 | P0 | 04 | `AGD-12/13/14/20` use `AgentIntent` (doc 17 canon is `AgentDecision`); no Formulas/Knobs (thresholds 1.0/1.25/1.5, budget 20, EW 0.9) | `AttentionTierName.cs`, `Attention*Projection.cs` | Adopt `AGD-nn`; add knobs; mapping rows for approval/attention/explain/swarm/Hindsight |
| D-05 | P0 | 05 | MCP OSINT surface over-claimed; DSA-3.1 `ai_proposed` / DSA-2.3 `proposedTL` do not exist | `mcp-tools.json` lists 5 `osint_*`; `Program.cs` dispatches `osint_search` only; code uses `interpreted_value` + `provisional` | Implement dispatches or downgrade; bidirectional manifest test; rewrite DSA-3.1/2.3 |
| D-06 | P0 | 06 | TL-branch snapshots and CMO markdown corpus marked deferred but shipped; `CatalogSchemaVersion = "010"` vs 17 migrations; AC list 0/40 checked | migration `010_tl_snapshot_branch.sql`, `CatalogTlTier`, `CatalogReleaseTrainResolver`, `CmoMarkdownImporter`, nightly scripts | Re-baseline §7/DBI-7.5; Schema Versioning section; checkbox pass |
| D-07 | P0 | 07 | Burst/DOTS wording; no CI gate or gauntlet requirement; INF-4.3 holds only headless (steps 2/4b harness-only) | ADR-018/021 | See B-06; scope INF-4.3 |
| D-08 | P0 | 08 | `architecture.md` disagrees with doc 08 (engine line, ADR-005 "Accepted", 6000.3.14f1); ARCH-0.1 overstates interactive/headless step parity; ADR list stops at 008 of 022 | `MissionRuntime.Tick`, `DatalinkSidePictureMerger.Merge` only in `BalticReplayHarness.RunCore` | Reconcile; parity amendment + divergence test; enforce ARCH-NFR-2 |
| D-09 | P0 | 09 | Phase N targets written against DOTS; two unreconciled swarm models; counter-swarm "Phase N" though shipped under doc 22 | `SwarmTierLimits` 50/500/5000 vs `SwarmPerformanceCaps` 40/12 | Managed-sim wording; reconcile with doc 22; `NFT-nn` IDs |
| D-10 | P0 | 11 | Unity status wrong: map authoring window, editor shell, live-edit panel, library panel exist while the doc says residual/not built; four P0 AME items have no code and no AC; verb counts ~28/~40 vs 58 cases / 50 MCP tools; export/publish gating was broken until `3cf67cb4` despite the "shipped" claim | `ScenarioMapAuthoringWindow`, `ScenarioEditorShellHost` (#338), `LiveEditPanelHost` (CMD-35); AME-2.1/2.2/5.2–5.4/7.4 | Re-baseline from the Unity tree; flag unimplemented P0s; generate verb table in CI |
| D-11 | P0 | 12 | Identifier drift and missing vocabulary | `LogisticsAbortReason` (no type), Comms State vs `CommsState`, Blocker/Advisory vs `ValidationSeverity`, ~17 manifest codes missing; none of the DRG-179…230 terms; "escalation" overloaded | Fix rows; add 15 families; generate abort codes from `abort_reason_manifest.json`; `GLO-nn` |
| D-12 | P0 | 13 | `MaxSalvo` semantics conflict (per-shot cap vs cumulative budget); WRA range denials log as `OUT_OF_ENVELOPE`; 6-tier/5-value inheritance spec vs shipped 3-tier + boolean | `PolicyEvaluator.cs:43`; `engagement-pipeline.md` gate 5; `ResolvedUnitPolicy.RoeInheritedFromMission` | Define salvo window + test; keep `WRA_RANGE`; ADR-002 amendment |
| D-13 | P0 | 14 | Abort catalog: 4 phantom codes, ~20 shipped codes missing, manifest version bump unenforced; stale "2 UA failures" residual; 6-stage diagram vs 22-gate chain | `abort_reason_manifest.json` v1 / 31 codes; `EngagementAbortReason` 28 members; UA 3/3 green since 07-09 | Regenerate from manifest; version-bump AC + codegen test; `ENG-09…12` |
| D-14 | P0 | 16 | LOG-08…11 graded "Phase N" though air/boat ops shipped 2026-08-01; `LogisticsAbortReason` never existed | `AirOpsFsm`, `BoatOpsFsm`, `ScenarioSeaState`, 4 Unity hosts, `SimulationSession.AirOps/BoatOps` | Re-grade with residual list; LOG-12 ordnance bands; abort-family table |
| D-15 | P0 | 20 | ≥15 CMD ids "Open" though shipped; CMD-31…39 undefined; code cites a non-existent "§Alerting and Interruption"; approval UX unspecified; evidence paragraphs now false | `AlertSeverity.cs` comment; `UnitOrderToolbarHost`, `ContactDetailPanelHost`, `MapLayerStack*`, ops hosts; PlayModeSmoke "18/18" vs 21/21 | Re-baseline statuses; add the two sections; CMD-40…43; NFR block |
| D-16 | P1 | 10 | `OrbitalDewPlatform`/`KesslerRiskMeter`/`EscalationTier` pinned absent by a test the doc does not cite; engage gate never reads `speculative_platforms.json` | `SpeculativeHonestyPinsTests` | `SPEC-nn`; cite the pin; state data sources |
| D-17 | P1 | 15 | EW status inverted: ECCM, IR/Visual modalities, swarm ISR, CEC composite tracks shipped; ESM and deception carry P0 with zero code; `sourceSensorIds[]` vs shipped `ContactProvenance` | `EccmFactor`, `SensorModality`, `IrVisualDetection` | Swap statuses; `SEN-09…12`; Pd formula + knobs |
| D-18 | P1 | 17 | MCP table asserts Met/Partial for five non-existent verbs; RPL-20 claims CSV columns the exporter lacks; `SwarmOrderLog` is a second writer violating RPL-01 | `replay_list/export/verify`, `aar_generate`, `metrics_batch` = 0 hits | Replace table; RPL-01 exception + fold-in AC; `RPL-29…32` |
| D-19 | P1 | 18 | `EngageIntent`/`ASW_NO_SOLUTION` do not exist (code `EngageRequest`, `DomainNoSolution`) so AC-3 cannot pass; ADR-009 contradicts DOM-04; no formulas though `CombatDamageLevel`, `mineHazard.*` are tunable | — | `DOM-AC-nn`; damage formulas; `DOM-11…14` |
| D-20 | P1 | 19 | Link-state YAML unimplemented (`CommsStateChangeRecord` has 6 plain fields); 3 of 4 `CYBER_*` codes never emitted; "JADC2 node damage Phase N" stale; share-lag path shipped but called Phase N | `DatalinkShareLagResolver`, ADR-018 | Document shipped schema; `COM-06/07`, `CYB-06/07` |
| D-21 | P1 | 21 | Workbook contract diverges from `PlatformWorkbookExporter` (`SensorCatalog`/`WeaponCatalog` absent, `Swarms` untabled, Lat/Lon contradiction); DRG-110 described as future though landed | exporter header `PlatformId, LatDeg, LonDeg, CombatRadiusNm` | Regenerate table + CI diff test; PLE-5.x/7.x |
| D-22 | P1 | 22 | Status self-contradictory (Draft / HOLD / unchecked boxes vs "landed"); landed numbers never written back; slice ids unmapped | PRs #420–#436 merged | Reconcile status; NFR/knob table; mapping table |
| D-23 | P1 | 11 / 06 / 21 | TL-branch scope conflict (11 ships `TL_*` rules; 06/21 say deferred); `dbRef` vs `dbSnapshotId` precedence undocumented; no snapshot-change → scenario-migration link | `ScenarioDbBinding(DbSnapshotId, DbRef?)` | One binding spec in doc 06; snapshot-change requirement |
| D-24 | P2 | 13–20 | No Formulas / Tuning knobs / Edge cases sections in any of the eight docs; key math (Pd chain, Pk chain, DLZ bands, constant fuel burn, `DefaultMaxSalvo = 8`) unowned | GDDs/ADR-020/code only | Minimal formula + knob tables per doc |
| D-25 | P2 | 03, 04, 09, 10, 18, 19, 20 | No requirement/AC IDs, so tests and tracker cannot trace to text | — | `MODE/AGD/NFT/SPEC/DOM-AC/COM/CMD-AC-nn` |

## E. Traceability and process

| ID | Sev | Finding | Evidence | Recommendation |
|---|---|---|---|---|
| E-01 | P0 | 89 % of ticketed work since 2026-07-09 is invisible to the corpus | 71 of 80 DRG ids in git cited by no requirement/tracker/traceability doc; only 13 of 684 commits touched `Game-Requirements/` | Linear rule: every code-landing DRG names a requirement anchor; tracker delta section |
| E-02 | P0 | `research-traceability.md` is 100 % dead-linked | 33/33 doc links use `../requirements/` from inside `Game-Requirements/`; linked from both hubs | `sed 's|(\.\./requirements/|(requirements/|g'`; CI link check |
| E-03 | P1 | No requirements change log; the dated trackers stopped playing that role on 2026-07-09 | — | `Game-Requirements/CHANGELOG.md` |
| E-04 | P1 | No `/design-review` recorded for any requirement change after 2026-07-09 despite five substantive edits | boat ops (16), CMD-16…29 (20), AME-11.x (11), SWARM-01…31 (22), DRG-47 (09/10); index rule L99 | Record verdicts; re-stamp status |
| E-05 | P1 | CMO manual matrix never re-run since May; registry lacks docs 21/22; undefined "Draft" coverage value; rows contradict shipped code | `cmo-manual-traceability.md` "May 29, 2026" | Regenerate from `docs/manual/generate.py TOC_RAW`; owner per section |
| E-06 | P1 | Twelve CMO-parity areas have no requirement owner | weather, mine warfare, satellites/space, aerial refuelling, cargo/amphib, base construction, land-combat depth, audio, menus/options, INI/log files, Tacview, custom overlays | Reserve stub docs 28–33 with Phase N decisions |
| E-07 | P1 | Traceability split across four unconnected registers with colliding IDs | Notion REQ-nn (no edges), Linear DRG (no requirement links), `tr-registry.yaml` (47 rows "not re-assessed"), ~20 in-doc prefixes; RTM `C1–C5` collides with review `C1–C7` | One status vocabulary; rename RTM rows; index pending drafts |
| E-08 | P1 | Corpus pin tests enforce the stale text the P0 fixes must change | `Wave4RtmIndexHonestyPinsTests` pins literal `1232`, `implementation-tracker-2026-07-04.md`, "S81 / editor active"; `RequirementsHubContractTests` requires every `requirements/*.md` in doc 01's index | Re-pin in the same PR as the hub fixes |
| E-09 | P2 | Reverse phantoms: ids used by code/specs that no requirement defines | `CMD-31…39`, `AGC-01…04`; forward refs DRG-141…194 in doc comments | Append to docs 20/24 |
| E-10 | P2 | Prior-review items still open without a closure record | C2 `PassthroughRoeFilter`, C6 logistics abort family, spoof-visible question (19→20), ADR-015 decision due 2026-09-01 unchecked | Closure table in the index |
| E-11 | P2 | `production/sprint-status.yaml` 17 sprints stale; `/sprint-status` reports S104 | last entry sprint 104 | Keep current or delete |

## F. Tooling

| ID | Sev | Finding | Evidence | Recommendation |
|---|---|---|---|---|
| F-01 | P1 | GitNexus MCP server fails at session start; on-disk index in the main checkout ~700 commits stale | `.mcp.json` pins `gitnexus@1.6.9`, global CLI 1.6.5, `node_modules/gitnexus` missing; `.gitnexus/meta.json` @ `c119d41d` (2026-06-15) | Align versions; `npm i`; re-index; session-start freshness check |
| F-02 | P1 | Keyword `query` degraded: FTS indexes exist but the extension is not loaded | `CALL SHOW_INDEXES()` → `extension_loaded: false` | Install/load the FTS extension; fall back to `cypher CONTAINS` |
| F-03 | P2 | `analyze` rewrites CLAUDE.md/AGENTS.md and six skill files as a side effect | `<!-- gitnexus:start -->` blocks; tool-name drift `impact(...)` → `gitnexus_impact(...)` | Decide whether to accept the refresh; otherwise revert after re-index |
| F-04 | P2 | No doc→code edges in the graph; traceability is not queryable | 17,371 markdown `Section` nodes, zero edges to symbols | Adopt a per-FR code marker convention |

---

## Proposed new requirement documents (from the findings)

| Doc | Title | Closes |
|---|---|---|
| 23 | Kill-Chain Explainability & Targetability (`KCX`) | B-02, B-04, B-08 (next action) |
| 24 | Human-on-the-Loop Authority, Approvals & Agent Recommendations (`HOL`) — **draft written** | A-01, B-01, B-03, B-08, B-09 |
| 25 | C2 Nodes, Mission Packages & Mission Command (`C2N`) | B-05 |
| 26 | Verification, CI Gates & QA Gauntlet (`VER`) | B-06, C-02, E-08 |
| 27 | Scenario Library, Campaigns & Packages (`LIB`) | B-13, D-10 (AME-2.1) |
| 28–33 | Stubs: Environment & Weather; Space & Satellites; Aerial Refuelling, Cargo & Amphibious Ops; Land Combat & Base Construction; Player Environment; Audio | E-06 |

## Suggested order of work

1. **Governance decisions** (A-01…A-04): implement or de-scope, each with an ADR note.
2. **Hub day** (C-01…C-05, E-02, E-08): floors by reference, DOTS sweep, tracker alias + delta section, link fix, re-pin tests.
3. **Honesty pass** (D-01…D-25) keyed to `git log --since=2026-07-09`; record `/design-review` verdicts (E-04).
4. **New docs 23–27** and the ID-level amendments (B-01…B-15).
5. **Guards** (E-01, E-03, E-05, E-07, F-01…F-04): Linear anchor rule, CI link check, change log, matrix regeneration, GitNexus freshness.
