# Bug Report

## Summary
**Title**: `CombatDomainValidator.Validate` unconditionally denies every `CombatDomain.Mine` engagement, permanently overriding ADR-009's `MineAspectDomainValidator` regardless of `combatDomainsEnabled`
**ID**: BUG-mine-domain-legacy-gate-blocks-all-launches
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Fixed (this loop) — TDD test + minimal production fix committed
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (automated TDD bug-hunt loop, qa-r2-02-roe-engage)

## Classification
- **Category**: Gameplay (Rules of Engagement / weapon release authorization — combat domain gating)
- **System**: Engagement resolution — `ProjectAegis.Sim.Engage.MvpEngagementResolver`, `ProjectAegis.Sim.Engage.CombatDomainValidator` (req-18 legacy gate), `ProjectAegis.Sim.Engage.MineAspectDomainValidator` / `DomainValidatorRegistry` (ADR-009 pluggable domain validators)
- **Frequency**: Always, for any engage intent whose `EngageContext.CombatDomain == CombatDomain.Mine`, regardless of `combatDomainsEnabled`, `MineAspectInEnvelope`, range, ammo, or policy — there is no code path through `MvpEngagementResolver.Resolve` that can ever launch a Mine-domain engagement.
- **Regression**: No — since-inception design/logic defect. `CombatDomainValidator`'s `CombatDomain.Mine => EngagementAbortReason.DomainNoSolution` arm predates ADR-009 (req-18 stub, written when Mine was simply unimplemented) and was never revisited when ADR-009 later added a real, geometry-based `MineAspectDomainValidator` — unlike `Facility`, which ADR-009 added with no competing legacy arm at all.

## Environment
- **Build**: worktree `qa-r2-02-roe-engage`, branch head at `main` @ `e2e1342`
- **Platform**: headless `dotnet test` (no Unity Editor available in this sandbox); `ProjectAegis.Sim.Tests` substitutes for PlayMode QA
- **Scene/Level**: N/A — plain .NET sim layer (`ProjectAegis.Sim.Tests`)
- **Game State**: Any unit engaging a target whose combat domain is `Mine`, with `combatDomainsEnabled: true` and a `DomainValidatorRegistry` containing `MineAspectDomainValidator` (the shipped `DomainValidatorRegistry.MvpStubs` registry)

## Reproduction Steps
**Preconditions**: A `MvpEngagementResolver` constructed with `combatDomainsEnabled: true` and `domainValidators: DomainValidatorRegistry.MvpStubs` (the production default set). An `EngageContext` for a Mine-domain target: in envelope range, `MineAspectInEnvelope: true`, fire-control track present, ammunition available, no other abort conditions.

1. Build a `MvpEngagementResolver` with `combatDomainsEnabled: true` and `domainValidators: DomainValidatorRegistry.MvpStubs`.
2. Set up an `EngageContext` with `CombatDomain: CombatDomain.Mine`, `MineAspectInEnvelope: true`, range inside the weapon envelope, magazine rounds available.
3. Call `resolver.Resolve(request)`.

**Expected Result**: The `MineAspectDomainValidator` (ADR-009) allows the action (envelope contains range, aspect in envelope), and the engagement proceeds through the rest of the pipeline (air-ready, EMCON, magazine, DLZ, etc.) to a normal `Launched == true` result — exactly as Air/Surface/Facility engagements do when their aspect validators allow.

**Actual Result** (before fix): `result.Launched == false`, `result.AbortReason == EngagementAbortReason.DomainNoSolution`. The ADR-009 `MineAspectDomainValidator` correctly returns `Allow`, but `MvpEngagementResolver.Resolve` *unconditionally* (not gated by `combatDomainsEnabled`) also calls the older, req-18 `CombatDomainValidator.Validate(ctx.CombatDomain, in ctx)` later in the same method, whose switch hardcodes `CombatDomain.Mine => EngagementAbortReason.DomainNoSolution` with no condition at all. This second, legacy check always fires after the new validator's `Allow`, silently discarding it. Every other domain has either no competing legacy arm (`Facility`), an unconditional allow (`Air`, `Surface`), or only a *conditional* legacy restriction (`Subsurface`, `Land`) that can be satisfied — only `Mine` is permanently, unconditionally blocked. This means enabling ADR-009 combat domains for Mine (as the `MineAspectDomainValidator` and its full aspect/range test coverage imply is intended) has no observable effect: Mine engagements can never succeed.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Sim/Engage/CombatDomainValidator.cs` (root cause — fixed symbol; `Validate`, the `CombatDomain.Mine` switch arm)
  - `src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs` (`Resolve`, line ~142 — calls `CombatDomainValidator.Validate` unconditionally, after the flag-gated ADR-009 `DomainValidatorRegistry.Validate` call)
  - `src/ProjectAegis.Sim/Engage/MineAspectDomainValidator.cs` (the ADR-009 validator whose `Allow` verdict was being silently overridden)
  - `src/ProjectAegis.Sim/Engage/DomainValidatorRegistry.cs` (registers `MineAspectDomainValidator` in `MvpStubs`)
- **Related systems**: ADR-009 (`docs/architecture/adr-009-combat-domain-validators.md`) explicitly lists mines as a "clear extension point ... without forking engage core" — i.e., mine engagement is meant to become real, gated behavior, not to remain permanently stubbed. `baltic-patrol-mine-transit-hazard` scenario fixture (`data/scenarios/baltic-patrol-mine-transit-hazard.policy.json`, exercised by `BalticReplayHarnessMineTransitHazardTests`) sets `CombatDomain: Mine` and `combatDomainsEnabled: true`, but models a *passive* mine-transit-hazard mechanic (ship damage from transiting a mined area) rather than an active weapon engage, and is explicitly excluded from the 6-scenario `ReplayGoldenRegressionCatalog` — so it was not exercising, and would not have caught, this launch-path defect.
- **Possible root cause**: `CombatDomainValidator` (req-18) was written when Mine domain support did not exist yet, and `CombatDomain.Mine => EngagementAbortReason.DomainNoSolution` was a reasonable placeholder at the time. ADR-009 later added the real `MineAspectDomainValidator` as the intended gate for Mine, but the old placeholder arm in `CombatDomainValidator` was never removed, and because `MvpEngagementResolver.Resolve` runs `CombatDomainValidator.Validate` unconditionally (regardless of `combatDomainsEnabled`) *after* the flag-gated ADR-009 check, the stale arm always wins.

## Evidence
- **New failing test (red)**: `ProjectAegis.Sim.Tests.Engage.DomainValidatorRegistryTests.CombatDomainsEnabled_true_mine_aspect_allowed_launches_successfully` (`src/ProjectAegis.Sim.Tests/Engage/DomainValidatorRegistryTests.cs`).
  - Confirmed **RED** before the fix: `Assert.True() Failure — Expected: True / Actual: False` (`result.Launched` was `false`, aborted with `DomainNoSolution`).
  - Confirmed **GREEN** after the fix: `result.Launched == true`, magazine correctly decremented.
- **Regression run (green)**: `ProjectAegis.Sim.Tests` 286/286 (285 baseline + 1 new); `ProjectAegis.Delegation.Tests` 253/253 (unchanged); `ProjectAegis.Delegation.UnityAdapter.Tests` 261/261 (unchanged, includes `BalticReplayHarnessMineTransitHazardTests` — mine-transit-hazard fingerprint/determinism tests unaffected since that fixture never issues a real Mine-domain *engage* intent); `ProjectAegis.Data.Tests` 477/477 (unchanged); `ProjectAegis.Data.Excel.Tests` 5/5 (unchanged); `ProjectAegis.MissionEditor.Cli.Tests` 72/72 excluding one pre-existing, environment-caused failure (`BranchIntegrationPhase0SmokeTests.Phase0_smoke_script_exists_and_passes_quick_mode`, which invokes a bash script unavailable in this sandbox and was independently confirmed to fail identically on the unmodified `main` tip before any change in this loop — unrelated to this fix).

## Impact / Blast Radius (manual analysis — GitNexus MCP/CLI not reachable inside this isolated worktree; `.gitnexus/run.cjs` absent)
`CombatDomainValidator.Validate` is a `public static` method with exactly one production call site: `MvpEngagementResolver.Resolve` (`MvpEngagementResolver.cs:142`, unconditional — runs regardless of `combatDomainsEnabled`). Grep confirms only two direct unit-test call sites of the static method (`CombatDomainValidatorTests.cs`, testing `MountOffline` and `Subsurface`/`ContactIdentified` — neither touches the `Mine` arm), so no existing test coverage depended on the old unconditional-deny behavior. `EngageContext.CombatDomain` defaults to `Air`; grep of `data/scenarios/*.policy.json` shows only two fixtures set a non-Air domain: `baltic-patrol-combat-domains-facility-hot-tick.policy.json` (Facility, unaffected by this change) and `baltic-patrol-mine-transit-hazard.policy.json` (Mine, test-only, explicitly excluded from `ReplayGoldenRegressionCatalog`, and models passive damage rather than active engage). None of the 6 golden replay scenarios use `CombatDomain.Mine`. Risk assessed **LOW**: the change only activates a previously-unreachable-in-practice success path (Mine engagements succeeding when their aspect validator allows) and does not alter behavior for any other domain, for `combatDomainsEnabled: false` callers relying on Air's default-allow behavior, or for any scenario in the regression/golden catalog.

## Related Issues
- Sibling round-1 bugs `BUG-roe-holdfire-silently-overridden.md` and `BUG-wra-range-abort-reason-mismapped.md` (also in `MvpEngagementResolver.cs`) both involve a legacy/duplicate code path silently overriding or mismapping the result of a supposedly-authoritative newer mechanism (policy snapshot resolution, and `FireAbortReason` mapping, respectively). This bug is the same *class* of defect — a stale legacy gate silently overriding a newer, correct mechanism — but in a different subsystem (combat-domain gating vs. policy snapshot / abort-reason mapping) and a different symbol (`CombatDomainValidator.Validate` vs. `MvpEngagementResolver.Resolve`'s `PolicyContext` construction / `MapPolicyDenial`), so it is not a duplicate of either.
- Not addressed in this pass (flagged for follow-up, out of minimal-fix scope): the `docs/reports/baltic-headless-slice-gate-2026-07-04.md` "policy-engage-unification" contract gap (WeaponsTight denials surfacing as raw `PolicyDenial` rows instead of `Engagement|...` abort rows) was investigated and found to already be resolved via `SimulationSession.SurfaceRoePolicyDeniedEngagements` — not a live defect, and out of scope for `ProjectAegis.Sim/Engage` and `ProjectAegis.Sim/Policy` regardless (the fix lives entirely in `ProjectAegis.Delegation`).

## Notes
The fix is a one-line behavior change plus a removed dead arm: `CombatDomain.Mine` no longer has a legacy-specific case in `CombatDomainValidator.Validate` and falls through to the same `_ => null` (allow) default that `Facility` already used. This makes `MineAspectDomainValidator` (ADR-009, gated by `combatDomainsEnabled`) the sole authority for Mine-domain engage gating, consistent with how Facility already works, and consistent with ADR-009's stated intent that mines are a "clear extension point" for real domain validation rather than a permanent stub. `combatDomainsEnabled` still defaults to `false` everywhere in production scenario JSON except the two test-only fixtures noted above, so default MVP behavior (Air-domain-only, `combatDomainsEnabled: false`) is completely unaffected by this change.
