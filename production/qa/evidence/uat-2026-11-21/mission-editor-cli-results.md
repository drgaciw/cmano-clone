# Mission Editor CLI UAT results (partial, stopped at coordinator request)

> Defect IDs are prefixed `CLI-` to keep them apart from the Play Mode defects (D1–D19) in `uat-playmode-2026-11-21.md`.

Branch uat/ui-function-matrix-r1, built Debug net11.0. All mutating runs used copies under a scratch directory outside the repo. Nothing written to data/, assets/ or unity/.
Test suite: `dotnet test src/ProjectAegis.MissionEditor.Cli.Tests -v q` -> Passed 132, Failed 0, Skipped 0, Total 132 (42 s). (README says "~43 tests": doc drift.)

Inventory: 62 dispatched verbs (Program.cs switch; event_update is alias of event_add). The CLI has no subcommands; options enumerated from PrintUsage (Program.cs:968) and docs/engineering/mission-editor-cli.md.
Counts: PASS 40, DEFECT 13 (see CLI-1-CLI-13; some verbs counted in both), NOT RUN 9 (verb-level list at bottom).
Round-trip: scenario_validate x2 on same file gives identical output (md5 match, new.json). scenario_export/export_brief/publish did not modify the input (sha256 unchanged). Undo restored the pre-delete fileHash exactly (975b3a0e...). Full validate -> save -> validate hash comparison for a mutated file: only partly done (UNKNOWN beyond the above).

## Matrix (command | expected | actual | verdict)

| Verb / case | Expected | Actual | Verdict |
|---|---|---|---|
| (no args) / unknown verb | usage, exit 1 | usage, exit 1 | PASS |
| scenario_validate x4 example scenarios | per findings | baltic-patrol, event-no-fire, strike-package exit 0; ferry-redeploy exit 1 | PASS (but see CLI-8) |
| scenario_validate missing file | JSON NOT_FOUND, exit 1 (docs) | `{"error":"file not found"}` exit 2 | DEFECT CLI-2 |
| scenario_validate malformed JSON | clear error, nonzero | unhandled JsonException, exit 134 | DEFECT CLI-1 |
| scenario_validate missing --path / --bogus | stderr msg, exit 1 | as expected | PASS |
| scenario_validate on non-scenario JSON ({} , slice-b fixture, baltic-escort.mission.json) | reject as not a scenario | exit 1 with TL_BRANCH_MISSING only; two different files give same reportHash | DEFECT CLI-9 (S4) |
| scenario_create (new / existing / no --out) | exit 0 / FILE_EXISTS 1 / 1 | as expected | PASS |
| scenario_create --seed abc | error | silently seed 42, exit 0 | DEFECT CLI-10 (S4) |
| scenario_create --out w/nodir/x.json | UNKNOWN (undocumented) | creates dir+file | PASS (noted) |
| mission_add_patrol (ok, stale version -> 3 CONFLICT, <3 wp, no unit, dup id, bad wp, missing/NaN edit-version) | per docs | all as documented | PASS |
| mission_add_strike / add_ferry / add_support (3 wp) | ok | ok | PASS |
| mission_add_support --role Bogus | INVALID_ROLE | zone check first; with 3 wp not re-run (UNKNOWN); update_support Bogus -> INVALID_ROLE | PASS (update) / UNKNOWN (add) |
| mission_update_patrol/strike/ferry/support (ok, unknown id, bad role, <3 wp, stale) | per docs | as expected (U1-U5, S3-S5) | PASS |
| mission_list (+--type, --status Assigned, missing file) | filtered list / NOT_FOUND | as expected | PASS |
| mission_list --status Bogus | error | exit 0, empty list | DEFECT CLI-11 (S4) |
| mission_clone / mission_add_from_template / mission_delete (ok, unknown source/template/id, stale) | per docs | as expected | PASS |
| scenario_undo (ok, stale, missing) | per docs | ok -> editVersion 12, remainingUndoDepth 11; stale exit 3; missing exit 1. Sidecar `<file>.undo-stack.json` (24 KB) written beside scenario | PASS |
| scenario_export (ok / blocked / missing / malformed) | 0 / 1 / 1 / 1 | as expected, malformed gives EXPORT_ERROR JSON | PASS |
| scenario_export_brief (--out, default out, blocked, missing) | 0 / 0 / 1 / 1 | blocked ok; missing file exit 2 non-envelope | DEFECT CLI-2 (same cause class) |
| scenario_publish (ok, blocked, missing) | 0/1/1 | as expected (FILE_NOT_FOUND, VALIDATION_BLOCKED) | PASS |
| scenario_simulate_sample (default 32, --ticks 8 twice, -5, abc, missing, malformed) | deterministic | same seed/ticks gives identical worldHash (7873a0e2... for 8 ticks, c956e8ea... for 32) | PASS |
| scenario_simulate_sample --ticks -5 / abc | error | silently clamps to 1 / defaults to 32, exit 0 | DEFECT CLI-10 (S4) |
| scenario_simulate_sample malformed JSON | clear error | exit 134 unhandled | DEFECT CLI-1 |
| scenario_simulate_sample missing file | exit 1 | exit 2 | DEFECT CLI-2 |
| scenario_diff_summary (diff, identical, missing, no --after, malformed) | per docs | as expected | PASS |
| scenario_comms_status / scenario_cyber_status (ok, unknown policy exit 1) | per docs | as expected | PASS |
| scenario_comms_status / cyber_status / near_future_spawn missing flag or file | exit 1 | exit 2 | DEFECT CLI-3 (S4) |
| scenario_near_future_spawn (ok) | 0 | 0 | PASS |
| scenario_event_trace (ok) | trace | ok | PASS |
| scenario_event_trace --event nope; --path missing | error | exit 0, empty trace (missing: stderr warning, exit 0); malformed exit 2 | DEFECT CLI-4 |
| scenario_ai_scaffold (default, --brief) | 0 | 0 | PASS |
| scenario_migrate_preview | deterministic output | idmap suffix differs every run (successor-74b1c54b / b132f03c / 644f3aaf) | DEFECT CLI-5 |
| scenario_migrate_preview / scenario_umpire_snapshot --path missing | error | exit 0, silently uses built-in default | DEFECT CLI-6 |
| scenario_umpire_snapshot (default, --path) | 0 | 0 | PASS |
| orbat_upsert_unit (ok, lat 99, lon 200, missing flags) | per code | LAT/LON_OUT_OF_RANGE exit 1 | PASS |
| orbat_upsert_unit --lat abc | parse error | generic "requires ..." usage message (misleading) | DEFECT CLI-10 (S4) |
| orbat_move_unit / orbat_clone_unit (ok, unknown id) | UNIT_NOT_FOUND / CLONE_FAILED | as expected | PASS |
| reference_point_upsert point / circle+radius / polygon | ok | ok | PASS |
| reference_point_upsert --type bogus; circle --radius-nm -3; circle w/o radius; polygon with 2 vertices | rejected | all accepted exit 0, saved | DEFECT CLI-7 |
| event_add / event_update / event_delete (ok, missing id EVENT_NOT_FOUND, missing trigger, missing file) | per docs | as expected | PASS |
| event_add --trigger Bogus / --condition Bogus | rejected | accepted, saved, validate passed (EVENT_DEAD_TRIGGER only on others) | DEFECT CLI-7 |
| side_list / side_upsert / side_delete (ok, SIDE_NOT_FOUND, missing file) | per docs | as expected | PASS |
| side_upsert --roe Bogus / --emcon Bogus | rejected | accepted, saved, scenario_validate passed | DEFECT CLI-7 |
| timeline_list / timeline_upsert / timeline_delete (ok, --tick -4 INVALID_TICK, missing flags, missing file) | per docs | as expected | PASS |
| timeline_upsert --mission nope (nonexistent mission) | rejected | accepted, orphan entry saved | DEFECT CLI-8b (S3/S4) |
| aar_explain (no args, missing file, bad jsonl, empty file, with/without --enabled) | exit 1 JSON | MISSING_EVIDENCE / NOT_FOUND / EVIDENCE_PARSE all exit 1 | PASS |
| aar_explain valid evidence + --enabled true | structured result | NOT RUN (no valid evidence file produced) | NOT RUN |
| mission_plan_suggest (keyword, fake provider valid-draft, missing --intent, empty intent, bad fixture, live provider, fake w/o fixture) | per README | all as documented (exit 0/1, PROVIDER_NOT_CONFIGURED, INVALID_SCRIPT) | PASS |
| catalog_entity_map | 0 | 0 | PASS |
| catalog_platform_browse --db copy --max-records 3 | 0 | 0 | PASS |
| catalog_platform_browse / catalog_intelligence_run / catalog_dependency_graph / catalog_link_report / catalog_mount_loadout_quarantine_triage `--db w/missing.db` | NOT_FOUND exit 1 | exit 0 ok:true, and a 332 KB sqlite file was CREATED at the missing path | DEFECT CLI-12 |
| catalog_platform_browse --db <non-sqlite file> | clear error | unhandled SqliteException, exit 134 | DEFECT CLI-1 (class) |
| catalog_platform_browse --max-records abc | error | exit 0 (ignored) | DEFECT CLI-10 (S4) |
| catalog_intelligence_run / dependency_graph / kill_chain_report / link_report (--db copy) and --help on the last three | 0 | 0 | PASS |
| catalog_release_diff (no flags -> usage exit 1; unknown version -> exit 1) | per docs | exit 1, but unknown-version error is a stderr .NET message with ArgumentException param text, not JSON envelope | PASS (note) |
| catalog_mount_loadout_quarantine_triage --entity bogus | rejected | exit 0 ok | DEFECT CLI-13 (S4) |
| catalog_mount_loadout_quarantine_triage (no --db) | exit 1 | exit 1 | PASS |

## Defects

- CLI-1 (S2 candidate, qa-lead to rule): malformed input crashes with unhandled exception, exit 134 (SIGABRT), no JSON envelope. Repro: `echo '{bad json' > bad.json; MissionEditor.Cli scenario_validate --path bad.json` (also scenario_simulate_sample --path bad.json; catalog_platform_browse --db <non-sqlite file>). Expected: JSON error, exit 1. Actual: stack trace, exit 134. Suspect: ScenarioValidateCommand.cs ~line 17 (`ScenarioDocumentJsonLoader.LoadFromFile` uncaught) and the RunScenarioValidate/RunSimulateSample/RunCatalogPlatformBrowse wrappers in Program.cs (exact lines UNKNOWN, not inspected). Contrast: scenario_export, scenario_diff_summary, aar_explain handle the same input cleanly.
- CLI-2 (S3): missing file returns exit 2 and non-envelope `{"error":"file not found","path":...}`; docs (mission-editor-cli.md Exit codes) say 1 and `code:NOT_FOUND`. Repro: `scenario_validate --path missing.json`; same for scenario_export_brief, scenario_simulate_sample. Suspect: ScenarioValidateCommand.cs:11-15 (`return 2`).
- CLI-3 (S4): missing required flag returns exit 2 for scenario_comms_status, scenario_cyber_status (docs: 1; others return 1); scenario_near_future_spawn missing file returns exit 2. Repro: `scenario_comms_status` -> exit 2. Suspect: ScenarioCommsStatusCommand.cs / ScenarioCyberStatusCommand.cs / ScenarioNearFutureSpawnCommand.cs (lines UNKNOWN).
- CLI-4 (S3): scenario_event_trace returns exit 0 success-shaped trace for unknown event id and for a nonexistent --path (warning on stderr only). Repro: `scenario_event_trace --path event-no-fire.scenario.json --event nope`, `scenario_event_trace --path missing.json`. Expected: error exit 1. Suspect: ScenarioEventTraceCommand.cs (lines UNKNOWN); malformed JSON there returns exit 2 with JSON on stderr.
- CLI-5 (S2 candidate): scenario_migrate_preview output is non-deterministic across runs (violates determinism invariant). Repro: run `scenario_migrate_preview` 3 times; idmap `legacy-patrol-ship->successor-<hex>` differs each time. Cause confirmed: string.GetHashCode() is randomized per process. Suspect: src/ProjectAegis.Data/Scenario/Authoring/ScenarioDbMigrationPreview.cs:54. Note: the pre/post hashes in the same output also differ from the idmap-containing run (MG1 vs MG3 post hash f08260fb... same, so only the idmap is unstable; UNKNOWN whether any hash includes it).
- CLI-6 (S4): scenario_migrate_preview / scenario_umpire_snapshot with nonexistent --path exit 0 and silently use a built-in default scenario. Repro: `scenario_umpire_snapshot --path missing.json`. Suspect: RunScenarioMigratePreview / RunScenarioUmpireSnapshot in Program.cs (lines UNKNOWN).
- CLI-7 (S3): authoring verbs accept invalid enum-like values and persist them: `reference_point_upsert --type bogus`, `--type circle --radius-nm -3`, circle without radius, polygon with 2 vertices; `event_add --trigger Bogus` / `--condition Bogus`; `side_upsert --roe Bogus` / `--emcon Bogus`. Each exits 0; a following scenario_validate still passes. Expected: INVALID_* error like mission_update_support's INVALID_ROLE. Suspect: ReferencePointUpsertCommand.cs, EventCommands.cs, SideCommands.cs (lines UNKNOWN); validator gap in ScenarioValidationEngine for reference points/sides.
- CLI-8 (S3, example data): shipped example data/scenarios/examples/ferry-redeploy.scenario.json fails scenario_validate (exit 1): MISSION_ROLE_NOT_EXECUTED (Ferry not executed by baltic-replay-harness), TL_BRANCH_SNAPSHOT_MISMATCH (tlBranch TL-2 vs snapshot TL-0), TL_RELEASE_TRAIN_NOT_FOUND. Also validation/doctrine-inheritance.json fails with MISSION_ROLE_NOT_EXECUTED (Support AEW); that one is used by tests asserting on findings, so it may be intentional (UNKNOWN). Repro: `scenario_validate --path data/scenarios/examples/ferry-redeploy.scenario.json`. Mission_add_ferry/support verbs also produce scenarios that cannot export (new.json failed validate after adding Support).
- CLI-8b (S4): `timeline_upsert --mission nope --tick 10` on a scenario without mission "nope" succeeds and saves an orphan entry. Suspect: TimelineCommands.cs (upsert, lines UNKNOWN).
- CLI-9 (S4): scenario_validate on non-scenario JSON (`{}`, data/scenarios/slice-b-combat-acceptance.json, baltic-escort.mission.json) is treated as an empty scenario; reports only TL_BRANCH_MISSING; slice-b and baltic-escort give the same reportHash 6fe426c0.... Expected: "not a scenario document" error. Suspect: ScenarioDocumentJsonLoader (ProjectAegis.Data) lenient deserialization.
- CLI-10 (S4): unparseable numeric flags silently defaulted or reported with misleading text: `scenario_create --seed abc` (seed 42), `scenario_simulate_sample --ticks abc` (32) / `--ticks -5` (clamped to 1), `catalog_platform_browse --max-records abc` (ignored), `orbat_upsert_unit --lat abc` (generic "requires ..." message), `mission_add_patrol --edit-version abc` (same generic message). Cause: CliArgParser.GetIntFlag etc. return defaults on parse failure (documented in README, but undesirable). Suspect: CliArgParser.cs.
- CLI-11 (S4): `mission_list --status Bogus` returns exit 0 with empty list. Suspect: MissionListCommand.cs.
- CLI-12 (S3): read-only catalog verbs given a nonexistent --db create a new ~332 KB SQLite DB at that path and report ok:true. Repro: `catalog_intelligence_run --db /tmp/x/missing.db` (also platform_browse, dependency_graph, link_report, mount_loadout_quarantine_triage); file exists afterwards. Expected: NOT_FOUND exit 1, no file created. Suspect: catalog reader open path (likely CatalogReaderFactory / SqliteCatalogReader in ProjectAegis.Data; UNKNOWN exact line) plus the Program.cs wrappers. Only scratch paths were used; repo catalog DBs untouched by me (git showed them modified: that is UAT D19, caused by the CLI test suite itself).
- CLI-13 (S4): `catalog_mount_loadout_quarantine_triage --db <copy> --entity bogus` exit 0. Suspect: MountLoadoutQuarantineTriageCommand.cs.
- Doc drift (S4, not counted): README says Event/Side/Timeline commands are not wired (they are); README says ~43 tests (actual 132); simulate/validate/publish print multi-line pretty JSON though the convention says one JSON line; scenario_simulate_sample prints extra `SEED=.. HASH=..` lines before the JSON.

## NOT RUN
catalog_write_propose, catalog_write_approve, catalog_import_markdown, platform_export_xlsx, platform_import_xlsx, platform_diff_xlsx, platform_design_propose, osint_search, osint_staging_review, gauntlet_oracle_eval (also NOT RUN: aar_explain with valid evidence, catalog_release_diff against a DB with real release versions). That is 10 verbs fully NOT RUN, 1 more partial.
