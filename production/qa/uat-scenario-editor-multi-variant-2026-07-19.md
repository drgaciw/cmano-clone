# Scenario Editor UAT — multi-variant (2026-07-19)

**Orchestration:** 3 parallel agents (validate variants · lifecycle mutations · automated Scenario filter tests) + TDD fixes on findings.

**Product surface under test:** headless Mission Editor CLI (`ProjectAegis.MissionEditor.Cli`) — primary scenario editor path (Unity Scenario Map Authoring remains Edit Mode IMGUI, separate from CLI).

## Variations exercised

| Fixture / track | Role |
|-----------------|------|
| `assets/.../validation/golden_clean.json` | Happy path golden |
| `assets/.../validation/doctrine-inheritance.json` | Doctrine resolution |
| `assets/.../validation/golden_strike_unreachable.json` | Intentional negative (STRIKE_UNREACHABLE) |
| `data/scenarios/examples/strike-package.scenario.json` | TL-0 example full verb chain |
| `data/scenarios/examples/{baltic-patrol,event-no-fire,ferry-redeploy}.scenario.json` | TL-2 vs TL-0 catalog gap |
| Lifecycle Track A | `scenario_create` empty → full mutate path |
| Lifecycle Track B | golden_clean copy → full mutate path |
| Policy verbs | `baltic-patrol-comms`, `baltic-patrol-classify` comms/cyber status |
| AI | `scenario_ai_scaffold` NL brief |

## Automated suite (Scenario filter)

| Suite | Passed | Failed |
|-------|-------:|-------:|
| MissionEditor.Cli.Tests ~Scenario | 41 | 0 |
| Data.Tests ~Scenario (pre-fix baseline) | 148 | 0 |
| UnityAdapter.Tests ~Scenario | 5 | 0 |

Post-fix Cli Scenario filter: **41/41 passed** (includes new Program dispatch test).

## Lifecycle mutation path

| Capability | Result |
|------------|--------|
| create / orbat / mission / side / RP / event | PASS |
| mission_clone / mission_add_from_template | PASS |
| scenario_validate after mutations | PASS (`passed=true`) |
| scenario_undo + stale CONFLICT (exit 3) | PASS |
| patrol ≥3 waypoints domain rule | PASS (2-wp correctly rejected) |

## Bugs found → fixed (TDD)

| Bug | Severity | Fix |
|-----|----------|-----|
| `scenario_diff_summary` command class existed but **Program.cs switch missing** (Unknown command) | Medium | Wired case + `RunScenarioDiffSummary`; process-level test `scenario_diff_summary_program_dispatch_returns_ok_json` |
| `scenario_export` returned **exit 0** when `allowed=false` | Medium | Non-zero exit 1 while still emitting JSON body |
| `scenario_publish` not blocked by failed validation | Medium | `VALIDATION_BLOCKED` exit 1 when export gate fails; publish test fixture includes `tlBranch` |

## Known residual (not code bugs)

1. **TL-2 example scenarios** (`baltic-patrol`, `event-no-fire`, `ferry-redeploy`) fail validate against TL-0 catalog (`TL_BRANCH_SNAPSHOT_MISMATCH`, `TL_RELEASE_TRAIN_NOT_FOUND`). Fix is **fixture/catalog alignment**, not CLI crash.
2. Policy JSON files rejected as scenarios (`TL_BRANCH_MISSING`) — expected format gate.
3. Unity **Scenario Map Authoring** window is Edit Mode IMGUI — not covered by headless multi-variant CLI UAT.

## Evidence logs

- `production/qa/uat-scenario-validate-variants-2026-07-19.log`
- `production/qa/uat-scenario-lifecycle-2026-07-19.log`
- `production/qa/uat-scenario-tests-2026-07-19.log`
- `production/qa/tdd-scenario-cli-after-fix-2026-07-19.log`

## Operator smoke (after pull)

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -c Release -- scenario_validate --path assets/data/scenarios/validation/golden_clean.json
dotnet run --project src/ProjectAegis.MissionEditor.Cli -c Release -- scenario_diff_summary --before assets/data/scenarios/validation/golden_clean.json --after assets/data/scenarios/validation/golden_strike_unreachable.json
dotnet run --project src/ProjectAegis.MissionEditor.Cli -c Release -- scenario_export --path assets/data/scenarios/validation/golden_strike_unreachable.json; echo exit:$?  # expect 1
```
