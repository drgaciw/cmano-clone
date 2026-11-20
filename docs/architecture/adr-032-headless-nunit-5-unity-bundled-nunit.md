# ADR-032: Headless .NET Tests Standardize on NUnit 5; Unity Edit/PlayMode Tests Keep Unity's Bundled NUnit

## Status

**Accepted** — 2026-11-20, under Decision's FINAL NUnit 5 ruling (drg deferred the framework choice to Decision). This file records that ruling. It does not flip the status of any earlier ADR.

## Date

2026-11-20

## Last Verified

2026-11-20. `packages-lock.json` resolves the Unity Editor test stack to `com.unity.test-framework` 1.6.0 (builtin) depending on `com.unity.ext.nunit` 2.0.3. Headless package pins on the migrated projects are NUnit 5.0.0 and NUnit3TestAdapter 6.3.1, with Microsoft.NET.Test.Sdk held at 18.11.0. Open PR #888 already claims the number ADR-031 for a different decision (one CI system) that is not on `main`; this record uses 032 so the two files do not share a number.

## Decision Makers

Decision bot, FINAL NUnit 5 (2026-11-20, about 6:09 AM CT). Owner / Technical Director (drg amtd) deferred the framework choice to Decision. Linear: [DRG-777](https://linear.app/drgamtd-workspace/issue/DRG-777).

## Summary

Headless .NET tests standardize on NUnit 5. Unity Edit/PlayMode tests keep Unity's bundled NUnit (com.unity.test-framework 1.6.0 / com.unity.ext.nunit 2.0.3).

## Engine Compatibility

| Field | Value |
|-------|-------|
| Engine | Unity 6.3 LTS client + .NET 11 headless |
| Unity test packages | `com.unity.test-framework` 1.6.0, `com.unity.ext.nunit` 2.0.3 (Editor-bundled; not the headless NUnit 5 package) |
| Headless packages | NUnit 5.0.0, NUnit3TestAdapter 6.3.1, Microsoft.NET.Test.Sdk 18.11.0 |
| Risk | LOW. Test-host and documentation only. No sim, catalog-write, or bridge change |

## ADR Dependencies

| Relationship | ADR / artifact |
|--------------|----------------|
| **Depends on** | None |
| **Enables** | One assertion and attribute style for headless `dotnet test` projects, including the out-of-solution `ProjectAegis.Ai.LiveEval` lane |
| **Conflicts with** | None. Does not retarget Unity Editor tests and does not change ADR-031's CI proposal |

## Context

Headless test projects in this repo were split between xUnit 2.9.3 and NUnit. DRG-777 moves the in-solution xUnit projects to NUnit 5.0.0 + NUnit3TestAdapter 6.3.1, one draft PR each. `ProjectAegis.Ai.LiveEval` is an xUnit project that lives outside `ProjectAegis.sln` and outside the `dotnet-ci` gate; it follows the same package pins in its own follow-up. The Unity Editor test assemblies under `unity/ProjectAegis` run through Unity Test Framework, which ships its own NUnit and is not the NuGet NUnit 5 package.

## Decision

1. Headless .NET tests standardize on NUnit 5 (package NUnit 5.0.0 with NUnit3TestAdapter 6.3.1). Microsoft.NET.Test.Sdk stays at 18.11.0.
2. Unity Edit/PlayMode tests keep Unity's bundled NUnit (com.unity.test-framework 1.6.0 / com.unity.ext.nunit 2.0.3). Do not replace that stack with the headless NuGet packages.
3. `ProjectAegis.Ai.LiveEval` is headless .NET, so it uses the same NUnit 5 pins. It stays outside the solution and outside `dotnet-ci`.

## Consequences

### Positive

- New headless tests use one attribute and constraint style (`[Test]` / `[TestCase]`, `Assert.That`).
- Unity Editor tests stay on the runner the Editor already loads.

### Negative

- Two NUnit lines remain: NuGet NUnit 5 for `dotnet test`, and Unity's bundled NUnit for Edit/PlayMode. They are not interchangeable.
- `dotnet-ci` does not execute `ProjectAegis.Ai.LiveEval`. That lane is local and on demand.

## Related

- DRG-777 and the per-project migrations (#887, #890, #891, #892, #893, #894, and the LiveEval follow-up)
- `unity/ProjectAegis/Packages/packages-lock.json` (`com.unity.test-framework` 1.6.0, `com.unity.ext.nunit` 2.0.3)
