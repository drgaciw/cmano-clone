# Scenario Editor UAT — 2026-07-20

## Scope
UI Toolkit Scenario Map Authoring (`ScenarioMapAuthoringWindow` + `ScenarioMapAuthoringPanel.uxml`) and headless authoring surface (`MapAuthoringSurface` / bus / session).

## Method
1. Parallel explore agents: widget matrix vs handlers/tests; domain-logic audit
2. Existing structural + surface suite (baseline green)
3. TDD on P0 defects (failing tests first → minimal fix)

## Widget / workflow coverage (summary)

| Area | Status |
|------|--------|
| Session Open/Save/Rebuild/Refresh Findings | Wired; structural UXML gates; session logic via integration tests |
| Domain nav (Aircraft/Ships/Subs/Ground) | Wired + menu entries; catalog non-empty tests; host invalidates stage on domain change |
| Catalog row → form fill | Wired; stage invalidated on preset change |
| Begin / Commit / Place+Commit / Cancel | Surface tests; Place+Commit happy path added |
| RP polygon begin+commit | Surface geometry validity tests |
| Status labels / lists | Display-bound; structural names include session-status + platform-list-scroll |

## Defects fixed (TDD)

| ID | Issue | Fix |
|----|--------|-----|
| UAT-P0-1 | Domain/catalog change left stale TentativeUnit | `InvalidateStagedGesturesForFormOrDomainChange` + host `SelectDomain` / `ApplyPlatformPreset` |
| UAT-P0-2 | Place silently overwrote existing unit id | `PlaceOrbatUnit` (no replace); bus `PlaceUnit` uses it |
| UAT-P0-3 | Failed commit cleared gesture | Keep `TentativeUnit`/`TentativeRp` when `result.Ok == false` |
| UAT-P0-4 | Empty side/platform + bad lat/lon accepted | Validation in `UpsertOrbatUnitCore` |

## Tests
- `ProjectAegis.Data.Tests` Scenario filter: **151 passed**
- `ProjectAegis.Delegation.UnityAdapter.Tests` Authoring: **35 passed**
- New: `ScenarioOrbatPlaceValidationTests`, MapAuthoringSurface UAT cases (retry, domain policy, place-commit, duplicate place)

## Residual / not in this pass
- Live UITK click simulation in Editor (license/MCP optional)
- Map click-to-place (form lat/lon only by design)
- Unit list row click → SelectUnit (surface API exists; no list buttons yet)
- Export workflow (N/A)
- Other C2 panel hosts dirty-flag (out of scenario editor scope)

## How to open
`Project Aegis → Scenario Map Authoring` or agent flag `Temp/aegis-open-scenario-map-authoring.flag`
