# Gate: Baltic Headless Vertical Slice

**Original date:** 2026-06-01
**Last refreshed:** 2026-07-04
**Scope:** Pre-Production headless loop (plan → fight → replay without C2 UI)
**Epic:** [baltic-headless-slice](../../production/epics/baltic-headless-slice/EPIC.md) — **Complete**
**Verified against:** branch `fix-scenario-publish-cli-wiring` @ `17d426c`
**Verdict:** original gate **PASS** (unchanged); lineage **AT RISK** (2026-07-04 refresh — 2 spine contract tests failing, see below)

---

## Summary

The Baltic headless vertical slice gate closed on **2026-06-01** with three merged PRs (#17–#19) and four acceptance criteria met. Those four criteria still hold on the `baltic-patrol` spine: the AC-4 CLI, the golden replay suite, the PlayMode smoke proxy, and the pinned world hash are all green today.

However, a **full `dotnet test ProjectAegis.sln` run on 2026-07-04 fails 2 of 1,310 tests** — both in `BalticReplayHarnessPolicyEngageTests`, the policy-engage-unification slice that sits directly on the Baltic replay harness. The failures are **inherited from `main`** (the behavior-changing commits are ancestors of `main`; this feature branch's only harness edit is additive `fire_order` plumbing). Determinism itself is intact — the failing scenario is reproducible run-to-run — but the harness no longer surfaces the engagement-abort representation the contract expects. This is a **contract/representation regression, not a determinism regression**, and it downgrades the lineage from HEALTHY to **AT RISK** until resolved.

**Related gates:** [vertical-slice gate (2026-06-02)](../../production/vertical-slice/gate-2026-06-02.md) **PROCEED** · [S48 release gate](../../production/gate-checks/s48-release-gate-2026-06-20.md) · [project dashboard](project-dashboard.md)

---

## Original gate evidence (2026-06-01)

| Check | Result |
|-------|--------|
| `dotnet test ProjectAegis.sln` | **105 passed** |
| PlayMode + replay fingerprint filter | **3 passed** |
| Replay CLI seed 42 / `baltic-patrol` / 4 ticks | `Launched` + `MagazineChange` rows |
| PRs #17–#19 on `main` | Merged |

### Epic acceptance

All four epic-level criteria in `production/epics/baltic-headless-slice/EPIC.md` met at close:

1. Solution tests green on `main`.
2. Fixed seed + scenario id → identical `DecisionLog.ComputeFingerprint()` across runs.
3. `Launched` and stable abort codes in engagement log.
4. CLI: `dotnet run --project src/ProjectAegis.Delegation.Demo -- --seed 42 --scenario baltic-patrol --ticks 4`.

---

## Lineage refresh (2026-07-04)

Live verification on `fix-scenario-publish-cli-wiring` @ `17d426c`. Working tree carries only doc/state changes (roadmap notes, `AGENTS.md`, cursor hook state) — no source edits pending.

### Build & test — actually run, not asserted

| Check | Result |
|-------|--------|
| `dotnet build ProjectAegis.sln` | **Build succeeded. 0 Warning(s), 0 Error(s)** |
| `dotnet test ProjectAegis.sln` | **1,308 / 1,310 pass — 2 FAIL** |
| `ReplayGoldenSuiteTests` | **6/6 PASS** |
| `PlayModeSmokeHarnessTests` (C2 proxy) | **18/18 PASS** |
| AC-4 CLI (`baltic-patrol`, seed 42, 4 ticks) | **`Launched` + `MagazineChange` + `EngagementOutcome\|Kill`** ✓ |
| Pinned world hash `17144800277401907079` | Present in v2 goldens (35 `replay-golden-baltic-*.txt` fixtures) |

**Per-assembly:**

| Assembly | Result |
|----------|--------|
| `ProjectAegis.Sim.Tests` | 281 / 281 ✓ |
| `ProjectAegis.Delegation.Tests` | 249 / 249 ✓ |
| `ProjectAegis.Data.Tests` | 453 / 453 ✓ |
| `ProjectAegis.MissionEditor.Cli.Tests` | 63 / 63 ✓ |
| `ProjectAegis.Data.Excel.Tests` | 5 / 5 ✓ |
| **`ProjectAegis.Delegation.UnityAdapter.Tests`** | **257 / 259 — 2 FAIL** |

> Note: the 1,310 total is higher than the ~1,232 recorded in prior sprint closeouts because this branch adds the scenario-editor / mission-editor suites (`MissionEditor.Cli.Tests`, additional `Data.Tests`). Earlier "1232/0f" figures reflect a different, pre-scenario-editor tree.

### ❌ Failing tests (blocking for a HEALTHY lineage verdict)

Both in [`src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessPolicyEngageTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessPolicyEngageTests.cs) — the *policy-engage-unification* slice:

1. **`Restricted_engagement_scenario_fingerprint_is_deterministic`**
   - `Assert.That(a.Fingerprint, Is.EqualTo(b.Fingerprint))` → **passes** (run-to-run determinism holds).
   - `Assert.That(a.Fingerprint, Does.Contain("Engagement|"))` → **fails** — no engagement row is emitted.
2. **`Friendly_weapons_tight_surfaces_policy_abort_in_engagement_log`**
   - Expects the fingerprint to contain a canonical `AbortReasonCatalog` code (`ROE_WEAPONS_TIGHT`, `OUT_OF_ENVELOPE`, or `DLZ_OUT`) → **fails**.

**Observed fingerprint** for `restricted-engagement` (seed 7, 4 ticks):

```
PolicyUpdate|1|0|0|1|roe|WeaponsFree|WeaponsTight
ModeChange|2|0|0||Planning|Executing
AgentDecision|3|1|1|a1|Engage|Engage:1:High|3.1331311245863662
PolicyDenial|4|1|u1|WeaponsTight|Engage
… (repeats for ticks 2–4) …
```

**Root cause (observed).** Under `restricted-engagement`, ROE resolves to `WeaponsTight` and every engage intent is denied at the **policy layer** (`PolicyDenial|…WeaponsTight|Engage`) *before* reaching the engagement resolver. The harness therefore emits no `Engagement|` row and no canonical `AbortReasonCatalog` abort code — only the raw `PolicyDenial` row. The policy-engage-unification contract (resolver denials should surface in the harness fingerprint *as engagement aborts*) is not met.

**Attribution.** Not introduced by this branch. The failing test source is byte-identical to `main`, and the behavior-changing commits (`08aaa03` contact-triggered dual-side ASuW/AAA, `cf15e38` abort-reason manifest / `BLACK_PROJECT_MODE`) are ancestors of `main`. This branch's only edit to `BalticReplayHarness.cs` (`7b0f376`) is additive `fire_order` plumbing that does not touch fingerprint, ROE, or engagement resolution. **The regression is present on `main`.**

**Secondary smell.** `AgentDecision` rows embed **raw `double` scores** in the fingerprint (`Engage:1:High|3.1331311245863662`). Run-to-run equality holds today, but raw floats in a determinism fingerprint are a culture/format/platform hazard and should be quantized or formatted invariantly.

### What is still green

- `baltic-patrol` spine (WeaponsFree default): AC-4 CLI launches, fires, kills, logs magazine change — unchanged.
- Golden replay suite (6/6) and PlayMode C2 proxy (18/18).
- Pinned world hash `17144800277401907079` intact across the 35 Baltic goldens.
- All of Sim, Delegation core, Data, Data.Excel, and MissionEditor.Cli suites.

### Direct successor epics (all complete)

| Epic | Delivers beyond Baltic gate |
|------|----------------------------|
| [sensor-headless-slice](../../production/epics/sensor-headless-slice/EPIC.md) | `ContactChange` order log + `ObservedState` |
| Baltic v2 / v3 program (S57–S80) | OOB/theater expansion, difficulty bands, mission events, C2 scenario UX ([scope boundary](../../production/baltic-v3-scope-boundary-2026-06-25.md), [manifest](../../production/playtests/baltic-v3-scenario-manifest.yaml)) |
| Agentic Mission Editor (doc-11) | Scenario authoring, validation, publish CLI, export gate ([req 11](../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md), ADRs [013](../architecture/adr-013-cmo-scenario-import-policy.md)–[017](../architecture/adr-017-editor-topology-client-vs-scenario-lab.md)) |

### Regression catalog

Pinned goldens: `tests/regression/replay-golden-baltic-*.txt` (35 fixtures) — see [tests/regression/README.md](../../tests/regression/README.md).

```bash
dotnet test ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln --filter "FullyQualifiedName~ReplayGoldenSuiteTests|FullyQualifiedName~PlayModeSmokeHarnessTests"
dotnet run --project src/ProjectAegis.Delegation.Demo -- --seed 42 --scenario baltic-patrol --ticks 4
# Reproduce the failure:
dotnet test ProjectAegis.sln --filter "FullyQualifiedName~BalticReplayHarnessPolicyEngageTests"
```

---

## GitNexus index note

Canonical index `cmano-clone` reports **20,496 symbols / 38,203 relationships / 300 execution flows** (last full analyze `@28c582d`, 2026-06-25). This **predates** the scenario-editor / mission-editor CLI work committed 2026-07-01…07-04, so the graph does not yet contain the new `ScenarioValidate*`/`ScenarioPublish*` symbols. Run `node .gitnexus/run.cjs analyze` before relying on `impact`/`detect_changes` for scenario-editor edits.

Standing CRITICAL hubs (unchanged): `CatalogWriteGate`, `PatrolCandidateEngagePolicy`, `DelegationBridge`, `BalticReplayHarness`. `impact` before touching any of these.

---

## CONCERNS

| # | Concern | Severity |
|---|---------|----------|
| 1 | **`BalticReplayHarnessPolicyEngageTests` × 2 failing** — restricted-engagement denials surface as `PolicyDenial` rows, not as `Engagement\|` rows with canonical `AbortReasonCatalog` codes. Present on `main`. | **BLOCKING** (spine contract) |
| 2 | Raw `double` scores in `AgentDecision` fingerprint rows — determinism hazard across culture/platform even though run-to-run equality holds today. | Advisory |
| 3 | GitNexus index stale vs. scenario-editor commits (`@28c582d` @ 2026-06-25). | Advisory |
| 4 | Live Unity Editor screenshots — headless proxy clears merge gate; live re-capture advisory. | Advisory |
| 5 | Full GDD MVP breadth still Partial per [implementation tracker 2026-07-04](../../Game-Requirements/implementation-tracker-2026-07-04.md). | Program breadth |

---

## Recommended next

1. **Fix the policy-engage-unification contract** — decide whether a `WeaponsTight` denial should emit an `Engagement|…|ROE_WEAPONS_TIGHT` abort row (so the harness log/fingerprint surfaces the doctrinal abort), or whether the two tests should assert against the `PolicyDenial` representation. Either way, land the fix on `main` — this is a real red on the trunk, not a branch artifact.
2. **Quantize float scores** in the decision-log fingerprint (invariant formatting / fixed decimals) to remove the raw-`double` determinism hazard.
3. **Reindex GitNexus** (`node .gitnexus/run.cjs analyze`) so scenario-editor symbols are covered before further doc-11 work.
4. **`/replay-verify`** before any merge touching `BalticReplayHarness`, `restricted-engagement`, or golden fixtures.
5. **`gitnexus impact`** on `DecisionLog`, `DelegationOrchestrator`, `PatrolCandidateEngagePolicy`, `CatalogWriteGate` before orchestrator/policy edits.

---

*Original gate: producer agent, 2026-06-01. Refreshed 2026-07-04 from live `dotnet build` + `dotnet test ProjectAegis.sln` (1,308/1,310 pass, 2 fail), targeted golden/PlayMode filter (24/24), AC-4 CLI run, and GitNexus index/context. All numbers in the 2026-07-04 refresh are from executed runs on `17d426c`, not carried forward.*
