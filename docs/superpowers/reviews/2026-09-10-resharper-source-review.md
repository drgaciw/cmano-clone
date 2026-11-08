# Final product-source review

Date: 2026-09-10
Scope: completed product/test diff after all planned source patches; read-only correctness and protected-invariant review
Runtime closeout state: all final documentation, Release, plugin-publish, and InspectCode gates subsequently passed; see the dated closeout.

## Verdict: PASS

No concrete behavior, contract, presentation-boundary, or protected-invariant regression was found in the final source diff. The completed runtime/analyzer evidence is linked in the closeout.

## Behavior and contract findings

- The previously reviewed switch-arm reductions preserve the current selected value for every input. Guards are neither reordered nor skipped. `AlertSeverityMap` retains its distinct `WEAPON_LAUNCH` arm and decision comment. Future fallback maintainability is optional and is not a present behavior defect.
- `C2AuthorityProjector.ParseRoeLabel` still handles canonical `WeaponsFree`, HOLD/TIGHT compatibility spellings, and the tested null/unknown fallback. Removing the redundant FREE substring arm does not alter current output.
- Numeric cast cleanup retains required signed-to-`uint` normalization before widening. Nullable pattern changes retain original positive/negative polarity. Tuple-array corrections retain nullable element types and restore the unreported Air Ops arrays.
- `ReplayIntegrityTimeline` still executes each mutation once with the same arguments. Removed terminal boolean checks and empty conditional wrappers do not suppress side effects.
- Hindsight request construction retains request method, URI, JSON body, authentication, and send order. Establishing request ownership before content creation improves exception-path disposal without changing successful requests.
- `ScenarioDocumentEditor` snapshots now use the existing deep undo-snapshot seam; known rollback restores all captured state, while unknown IDs report failure without mutation. The two focused tests pin both paths.
- Explicit default arguments that encode security roles, authoring roles, parse fallback, simulation tick cadence, modes, and golden/fixture policy remain explicit. Only the seven reviewed redundant defaults were removed.
- Unused-member cleanup retains calls with side effects and removes storage/parameters shown to have no reader. `CecNodeRegistration.IsSwarm` remains in the public positional record for construction, deconstruction, and equality compatibility; only its unread private runtime copy was removed. Its ReSharper source exception is separate from the original inventory.
- Documentation changes are runtime-neutral except `AdjudicationWorkspace.ComputeDiff`, where separate sealed-record null guards preserve rejection behavior and now report the correct `before` or `after` `ArgumentNullException.ParamName`. The red test failed for the old behavior and both focused adjudication tests subsequently passed.

## Nullability follow-on review

The return annotations match implementations:

- `UnitDetailProjection.ProjectSelected` constructs and returns `UnitDetailEntry` on every path.
- `DoctrineInheritanceProjection.ProjectUnit` constructs an unavailable placeholder when policy is absent and returns a concrete entry on every path.
- `UnitDetailBridge.BuildSelected(TargetId, ISimWorldSnapshot, DecisionLog, ...)` validates both nullable inputs and directly returns `ProjectSelected`; strengthening this first overload to `UnitDetailEntry` is correct. The second overload remains nullable because its result flows through the shared nullable `EnrichAttackMenu` contract, also used by nullable `BuildPrimary`; this review does not infer a registry-lookup failure or change that contract.

The fresh analyzer exposed **11** redundant caller null-forgiving tokens, not 12: five UnitDetail tests, five DoctrineInheritance tests, and one PlayMode doctrine test. Their removal is annotation-driven and does not alter evaluation. The additional first-overload `UnitDetailBridge.BuildSelected` return annotation is outside the original 2,326 inventory, has exact LOW/2 impact evidence, and adds no exception or behavior change.

The strengthened public UnityAdapter annotation required a `netstandard2.1` plugin refresh. Publish/copy verification subsequently passed for all 14 DLLs.

## Metadata identity migration

The retained `pkg.ExportDocument.Metadata?.EditVersion` access remains required because explicitly null metadata is accepted by the JSON load boundary and is not normalized during export transformation. Removing the preceding redundant `ExportDocument?` qualifier shifted this unchanged finding and changed its context identity:

- original ledger identity: fingerprint `296A863CC626C4DCCB60DD9A9578B1300FB7FBE398954CA73B71D95CAF39EEC6`, occurrence 2
- active SARIF identity: fingerprint `AE4E1755BEBF51B44C7D2567C2EFA576B5DB4FE2B8EFF348816B106FD96B6894`, single active occurrence

`metadata-nullability-identity-migration.json` records the one-to-one provenance. The ledger preserves accepted-exception status, rationale, owner, review condition, and original identity; `nullability-contracts-final.sarif.comparison.json` reports 0 new, 0 ambiguous, and 0 reintroduced findings. No general identity algorithm was loosened.

## Documentation test correction

The first documentation scan reached the intended original-inventory accounting (**1,363 fixed / 956 accepted / 7 deferred / 0 open**) but correctly reported one new finding: the new ParamName regression test explicitly passed the default `"umpire"` role to `AdjudicationWorkspace`. Removing only that redundant constructor argument preserves the test's purpose and runtime setup; role behavior is irrelevant to the two asserted null-parameter names. The newly added test method is absent from the baseline GitNexus index, so its direct method lookup returned not found; `documentation-test-correction-impact.json` records the exact enclosing `McpMissionToolCliTests` class impact as LOW/0. Final documentation verification subsequently confirmed zero new findings, and all 3,226 tests passed.

## Current verified batch evidence

The latest completed nullability batch provides useful intermediate proof:

- `nullability-contracts-final-build.log`: build succeeded with 0 warnings and 0 errors.
- `nullability-contracts-final-tests.log`: 3,225 tests passed, 0 failed.
- `nullability-contracts-final-smoke.log`: 24/24 passed.
- `nullability-contracts-final-replay.log`: 17/17 passed.
- `nullability-contracts-final.sarif.comparison.json`: 1,281 fixed, 956 accepted, 7 deferred, 82 open, and zero new/ambiguous/reintroduced.

The 82 open entries at that intermediate checkpoint were all resolved by the subsequent documentation batch. The final Release, plugin, and analyzer results are recorded in the closeout.

## Protected invariants

- `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`: zero diff.
- `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs`: zero diff; existing concrete write paths untouched.
- `unity/ProjectAegis`: zero diff, including all asmdefs, scenes, prefabs, and runtime/editor hosts.
- `tests/regression`: zero diff. The locked Baltic v2 hash `17144800277401907079` remains present; final verification found 26 matching lines across tests and data.
- Replay source tests only lose unused imports; no golden value or assertion changes.
- No `*.db3` file appears in the diff.
- `git diff --check` reports no whitespace/error output.

## Final closeout

The root completed and read the final Debug/Release gates, plugin refresh, pinned CLI scan, and protected-path checks. No source change followed verification. See [closeout](2026-09-10-resharper-closeout.md). No commit was requested or made.
