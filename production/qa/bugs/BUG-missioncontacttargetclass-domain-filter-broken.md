# Bug Report

## Summary
**Title**: `mission.triggers[].targetClass` domain filtering is broken for every real catalog platform — `Subsurface` silently falls back to `Any`, `Air` silently never matches anything
**ID**: BUG-missioncontacttargetclass-domain-filter-broken
**Severity**: S2-Major (silent incorrect behavior — no error, no warning, a domain filter either does nothing or is permanently false; not a crash/data-corruption/CRITICAL-impact issue)
**Priority**: P2
**Status**: Open — quarantined, not fixed (GitNexus MCP tools were unavailable this session; per CLAUDE.md's "never edit a function/class without impact analysis first" this defect's fix surface — `MissionContactTargetClass`/`MissionContactTargetClassifier` — was not touched)
**Reported**: 2026-07-28
**Reporter**: QA Gauntlet run `gauntlet-20260727-1455`, Tier 5. Independently found and confirmed by **three separate `military-simulation-architect` agents** (drafting forge candidates `t5-c3`, `t5-c4`, and the tier-5 main-ladder scenarios) while designing `mission.triggers` conditions — each traced the actual runtime classification code before authoring a `targetClass` value, rather than assuming a schema field behaves as its name suggests.

## Classification
- **Category**: Sim code (`src/ProjectAegis.Sim/Scenario/MissionContactTargetClass.cs`, `src/ProjectAegis.Sim/Scenario/ScenarioPolicyJsonLoader.cs`)
- **System**: `mission.triggers[].targetClass` domain-gated ROE escalation
- **Frequency**: Always, deterministically, for `targetClass: "Air"` or `"Subsurface"` against any real catalog platform id in this corpus (none start with `"ucav"`)
- **Regression**: Unknown when introduced. At least tiers 3 and 4 of this run's corpus (`gauntlet-20260727-1455-t3-s1`'s `asw-contact-roe` trigger, `gauntlet-forge-20260727-1455-t4-c3`'s `asw-prosecution-roe` trigger, and `gauntlet-20260727-1455-t4-s2`/`t4-s3`) already authored `"targetClass": "Subsurface"` before this was noticed.

## The defect

`MissionContactTargetClass` (`src/ProjectAegis.Sim/Scenario/MissionContactTargetClass.cs`) defines only three members:

```csharp
public enum MissionContactTargetClass { Any = 0, Surface = 1, Air = 2 }

public static class MissionContactTargetClassifier
{
    public static MissionContactTargetClass Classify(string targetId) =>
        targetId.StartsWith("ucav", StringComparison.Ordinal)
            ? MissionContactTargetClass.Air
            : MissionContactTargetClass.Surface;

    public static bool Matches(MissionContactTargetClass required, string targetId) =>
        required switch
        {
            MissionContactTargetClass.Any => true,
            MissionContactTargetClass.Surface => Classify(targetId) == MissionContactTargetClass.Surface,
            MissionContactTargetClass.Air => Classify(targetId) == MissionContactTargetClass.Air,
            _ => false,
        };
}
```

Two independent, compounding gaps:

1. **There is no `Subsurface` enum member at all.** `ScenarioPolicyJsonLoader.ParseMissionContactTargetClass` (`ScenarioPolicyJsonLoader.cs:362-365`) parses the JSON string via `Enum.TryParse<MissionContactTargetClass>(label, ignoreCase: true, out var parsed)` and **silently falls back to `Any`** whenever the label doesn't match a real member — `"Subsurface"` always hits this fallback. A trigger authored with `"targetClass": "Subsurface"` therefore actually fires on the observer's **first `Detected` transition against any contact**, not specifically a subsurface one.
2. **`Classify()` treats every real platform id as `Surface`.** It returns `Air` only when the target id literally starts with the string `"ucav"` — true of zero platform ids anywhere in `assets/data/catalog/baltic_patrol.db` (fighters, bombers, submarines, frigates, corvettes — none use a `ucav-` prefix). So `"targetClass": "Air"` is a **syntactically valid enum value that can never match any real target**: `Matches(Air, realId)` is `Classify(realId) == Air` → `Surface == Air` → always `false`. A trigger declared this way will **never fire**, silently, regardless of how reliably its underlying `detection[]` entry resolves.

These are qualitatively different failure modes — `Subsurface` degrades gracefully (falls back to "match anything," which is usually harmless when the observer has exactly one relevant detection entry, as happened to be true everywhere it was used) — but `Air` fails **closed** (permanently false), which is a much more dangerous shape: an author who declares `"targetClass": "Air"` believing it is real and functional (it *is* a real enum member, unlike `"Subsurface"`) gets a trigger that looks correct, passes JSON validation, and simply never activates.

## Reproduction — the caught case

`production/qa/gauntlet/gauntlet-20260727-1455/forge/candidates/gauntlet-forge-20260727-1455-t5-c4.policy.json`'s `aaw-defensive-intercept-baseline` trigger was originally authored:

```json
{
  "id": "aaw-defensive-intercept-baseline",
  "observerId": "f-219-sachsen-type-f124-2006",
  "targetClass": "Air",
  "roe": "WeaponsFree",
  "unitIds": ["f-219-sachsen-type-f124-2006", "jas-39e-gripen-ng-2021", "eurofighter-typhoon"]
}
```

with the observer holding a `detection[]` entry against `tu-160-blackjack` at `basePd: 1.0` (deterministic hit every run). Despite the detection resolving with certainty, `Matches(Air, "tu-160-blackjack")` is always `false` (`tu-160-blackjack` doesn't start with `"ucav"`), so this trigger, as originally authored, would have **never fired** — contradicting its own name ("baseline", implying always-on) and the candidate author's belief that using a real enum member made it safe. **This was caught and fixed before the candidate was finalized this session** (`targetClass` corrected to `"Any"`, which is safe here since the observer has exactly one detection entry) — logged here as the reproduction case, not as an open instance.

## Known-affected existing corpus entries (not fixed — flagging for audit)

All of the following use `"targetClass": "Subsurface"` and, per the code above, silently run as `Any` instead:

- `data/scenarios/gauntlet-20260727-1455-t3-s1.policy.json` — trigger `asw-contact-roe`
- `data/scenarios/gauntlet-forge-20260727-1455-t4-c3.policy.json` — trigger `asw-prosecution-roe`
- `data/scenarios/gauntlet-20260727-1455-t4-s2.policy.json` and `gauntlet-20260727-1455-t4-s3.policy.json` (per this session's own tier-4 drafting)

In every one of these, the affected observer happens to have exactly one relevant `detection[]` entry, so "match anything" and "match only subsurface contacts" produce identical practical behavior — these are **not currently misbehaving**, but they are relying on a coincidence, not a correct domain filter. No `"targetClass": "Air"` was found anywhere in the pre-existing (tier 1-4) corpus, so the fail-closed variant of this bug is believed to be novel to this session's tier-5 drafting (and was caught before landing).

## Why it matters

1. Any scenario author relying on `targetClass` to genuinely distinguish "this trigger should only escalate ROE on a submarine contact, not a surface one" is silently getting no such guarantee — every trigger in this corpus so far has only "worked" because each observer's `detection[]` array happened to contain exactly one entry.
2. The `Air` case is worse: it doesn't degrade to "always matches," it degrades to "never matches," which is a much easier way to accidentally ship a scenario whose entire escalation branch is dead.
3. Both gaps are silent — no exception, no log line, no validation error. `dotnet run ... scenario_validate` and the gauntlet policy schema checks used by this project do not catch this class of defect at all.

## Suggested fix (not implemented — quarantined pending GitNexus impact analysis)

1. Add a real `Subsurface` member to `MissionContactTargetClass` and teach `Classify()` to determine domain from the actual platform's catalog `domain` field (already available via `ICatalogReader`/`BalticV3SideRegistry` elsewhere in the codebase) rather than an id-prefix heuristic that only recognizes a hypothetical `"ucav"` naming convention no real platform uses.
2. Consider making `ScenarioPolicyJsonLoader.ParseMissionContactTargetClass` fail loudly (throw or emit a validation finding) on an unparseable `targetClass` string instead of silently defaulting to `Any` — the current behavior actively hides authoring mistakes.
3. Audit the corpus (`data/scenarios/*.policy.json`) for every `"targetClass": "Air"` or `"Subsurface"` once the fix lands, and re-verify each trigger's intended behavior still holds (most should be unaffected per the single-detection-entry coincidence above, but this should be confirmed, not assumed).

This is a separate, distinct defect from `production/qa/bugs/BUG-scenario-contacts-shadowed-by-detection.md` (which concerns the `contacts[]`/`ScenarioContactSimulator` mechanism being unreachable) — both are silent-gap findings in scenario-authoring mechanics discovered during this run's tier 4/5 drafting, but they live in different code paths and have different fixes.

## Related Issues
- `production/qa/bugs/BUG-scenario-contacts-shadowed-by-detection.md` — a related but distinct silent-gap finding from the same run.
