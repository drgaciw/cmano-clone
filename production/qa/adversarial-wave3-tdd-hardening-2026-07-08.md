# Adversarial TDD Hardening — Wave 3 (2026-07-08)

**Branch:** `test/adversarial-wave3-tdd`  
**Worktree:** `.worktrees/adv-w3-tdd`  
**Base:** `main@9dbafb0` (Wave 3 docs honesty)  
**Mode:** Parallel adversarial tracks A3-a / A3-b / A3-c → characterization pins  
**Scope:** **Tests only** — zero production `.cs`, zero goldens/hash, zero `DelegationBridge`

## Audit domains (mapped to Wave 3 honesty)

| Track | Domain | Wave 3 claim hardened |
|-------|--------|------------------------|
| **A3-a** | Near-future spine (doc 09) | 4 archetypes; Medium swarm 500; PlanSpawns plan-only; no `PlatformTechnologyLevel`; `GameTechnologyLevel` |
| **A3-b** | Speculative / S54 demotion (doc 10 / 10b) | No `OrbitalDewPlatform` / `KesslerRiskMeter` / `EscalationTier` types; catalog metadata only; `BlackProjectRequired` gate gap |
| **A3-c** | Docs contract + platform write-gate (doc 21) | FR-08/FR-19 + mapping honesty; tracker 10b Phase N; ClosedXML IO type; ProposePlatform ≠ live |

## Pins implemented (green)

| Test | Assembly | Hardens |
|------|----------|---------|
| `near_future_archetype_catalog_has_exactly_four_rows_with_canonical_ids` | Data.Tests | Catalog spine size + IDs |
| `swarm_tier_limits_medium_max_entities_is_500` | Data.Tests | MEDIUM cap honesty |
| `plan_spawns_returns_plan_records_only_not_world_entities` | Data.Tests | Plan-only (not full DOTS spawn) |
| `data_assembly_has_no_type_named_PlatformTechnologyLevel` | Data.Tests | Naming honesty (GTL field) |
| `catalog_platform_binding_exposes_GameTechnologyLevel_property` | Data.Tests | GTL field existence |
| `src_assemblies_have_no_OrbitalDewPlatform_KesslerRiskMeter_or_EscalationTier_types` | Sim.Tests | Tracker **10b** demotion |
| `speculative_platform_catalog_includes_orbital_dew_demo_as_metadata_only` | Sim.Tests | Metadata ≠ full DEW runtime |
| `speculative_gate_returns_BlackProjectRequired_when_tl_ok_but_black_mode_off` | Sim.Tests | Gate gap vs TL-only tests |
| `mvp_resolver_aborts_black_project_required_with_log_code` | Sim.Tests | Resolver path + log code |
| `doc_09_and_10_reverse_ref_FR_08_and_have_implementation_mapping` | Data.Tests | Wave 3 FR-08 + mapping footer |
| `doc_21_reverse_ref_FR_19_and_mapping_not_all_new` | Data.Tests | FR-19 + Shipped mapping |
| `tracker_10b_is_phase_n_not_on_main_not_implemented_s54` | Data.Tests | Hard demote 10b cell |
| `platform_workbook_io_closedxml_type_exists` | Data.Tests | ClosedXML IO shipped |
| `ProposePlatformBatch_without_ApproveBatch_not_readable_as_live_catalog` | Data.Tests | Write-gate staging ≠ live (platform) |

**New/updated files:**

- `src/ProjectAegis.Data.Tests/Catalog/NearFutureHonestyPinsTests.cs` (new)
- `src/ProjectAegis.Sim.Tests/Scenario/SpeculativeHonestyPinsTests.cs` (new)
- `src/ProjectAegis.Data.Tests/Architecture/Wave3RequirementsHonestyContractTests.cs` (new)
- `src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGatePlatformApproveTests.cs` (+1 Fact)

## Verify (targeted)

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~NearFutureHonestyPins|FullyQualifiedName~Wave3RequirementsHonesty|FullyQualifiedName~ProposePlatformBatch_without_Approve" -v minimal
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "FullyQualifiedName~SpeculativeHonestyPins" -v minimal
```

Expected: all new Facts green (14 pins). Parallel-track filters reported **0 failures** during implement.

## Invariants

- No production `src/**` edits outside `*.Tests`
- No golden / production hash edits
- `DelegationBridge` zero-touch
- Adversarial Wave 2 worktree (`.worktrees/adv-w2-tdd`) remains separate

## Backlog (not this pass)

1. CLI `scenario_near_future_spawn` missing-path exit code pin  
2. Baltic `NF_SPAWN` harness log string pin (UA)  
3. Hypersonic gate e2e through `MvpEngagementResolver` (boolean already unit-pinned)  
4. Commit/merge this branch only on user instruction  

## Sign-off

- Parallel agents A3-a / A3-b / A3-c + orchestrator integrate  
- Date: 2026-07-08  
