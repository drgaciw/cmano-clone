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

## Known-affected existing corpus entries

**Correction (2026-07-29, PR #367 review):** the original version of this report claimed "no `targetClass: Air` was found anywhere in the pre-existing (tier 1-4) corpus." **That was wrong** — this session's own tier-4 drafting had already shipped two `"targetClass": "Air"` triggers (both named `aaw-intercept-roe`), missed during the tier-4/tier-5 review passes and only caught by an external automated PR reviewer (`chatgpt-codex-connector[bot]`) after the PR was marked ready for review. Both are **now fixed** (see below) — logged here so the miss is part of the record, not just the fix.

**Fixed this pass** (`targetClass: "Air"` → `"Any"`, both observers have exactly one relevant `detection[]` entry so the fix is behavior-safe; both scenarios re-batched at their tier-4 24-tick budget/seeds 42,7,123 and `gauntlet.expect` regenerated from the corrected real behavior; `gauntlet_oracle_eval` re-confirmed `allPassed: true` for the full 8-policy tier-4 set after the fix):

- `data/scenarios/gauntlet-20260727-1455-t4-s2.policy.json` — trigger `aaw-intercept-roe` (observer `f-230-norfolk-type-23-duke` vs `mig-31-foxhound`). Pre-fix the AAW group never escalated off `WeaponsTight`, so the scenario's own stated intent ("BLUE AAW group ... intercepts RED's two-ship raid") never actually happened — denials dropped from a flat 76-84 to 4 post-fix, kills became achievable for that group as the scenario always claimed.
- `data/scenarios/gauntlet-forge-20260727-1455-t4-c3.policy.json` — trigger `aaw-intercept-roe` (observer `f-219-sachsen-type-f124-2006` vs `su-27sm-sm3-flanker-j-2013`). Same pattern; denials dropped from a flat 96 to 24 post-fix.

**Still open — `"targetClass": "Subsurface"` instances (harmless coincidence, not fixed):**

- `data/scenarios/gauntlet-20260727-1455-t3-s1.policy.json` — trigger `asw-contact-roe`
- `data/scenarios/gauntlet-forge-20260727-1455-t4-c3.policy.json` — trigger `asw-prosecution-roe` (the sibling ASW trigger in the same file whose `Air` trigger was just fixed above)
- `data/scenarios/gauntlet-20260727-1455-t4-s2.policy.json` and `gauntlet-20260727-1455-t4-s3.policy.json`

In every one of these, the affected observer happens to have exactly one relevant `detection[]` entry, so "match anything" (the actual `Any` fallback) and "match only subsurface contacts" (the intended filter) produce identical practical behavior. These were left as-is rather than "fixed" to `Any` explicitly, since they aren't currently misbehaving and changing them is cosmetic-only (no behavior change) — flagged here for a future audit pass, not urgent.

**Process note:** this miss is exactly why an external, independent review pass caught something this session's own multi-agent review didn't — three drafting agents found the general defect class, but none happened to check the two already-shipped tier-4 files for the same pattern. Worth remembering for future gauntlet runs: a defect class found in new content should always trigger a grep-style sweep of the existing corpus for the same pattern, not just a narrative note that "no other cases were found" without actually searching.

## Corpus-wide sweep (2026-07-29, prompted by the process note above)

Grepped every `data/scenarios/*.policy.json` for `mission.triggers[].targetClass` in `{Air, Subsurface}`. Beyond the gauntlet-corpus instances above, this also surfaced 6 golden-backed fixtures (`baltic-v3-classify`, `baltic-v3-comms-challenged`, `baltic-v3-mission-band-b`, `baltic-v3-mission-roe-band-c`, `baltic-v3-patrol-comms`, `baltic-v3-patrol`), each with two `"targetClass": "Air"` triggers (`blue-aaa-air`, `red-aaa-air`).

**These are NOT further instances of the bug — they are the correct, working reference usage**, and initially flagging them as suspect without checking would have been its own false-alarm mistake. Their observers (`ucav-blue`, `ucav-red`) hold detection entries against **both** a non-`ucav`-prefixed target (`hostile-1`/`u1`, correctly classified `Surface`) **and** a `ucav`-prefixed target (`ucav-red`/`ucav-blue`, correctly classified `Air`) — confirmed via `data/scenarios/baltic-v3-patrol.policy.json` and cross-referenced against `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessV3UcavTests.cs`, whose own assertions (`Does.Contain("blue-aaa-air|AAA")`) depend on the `Air` filter genuinely discriminating between the two. `MissionContactTargetClassifier.Classify` works exactly as designed here, because these fixtures follow the one narrow naming convention (`ucav-` prefix) it recognizes. **Not touched, and correctly so** — golden-backed fixtures are off-limits regardless, and in this case there is nothing to fix.

This sharpens the actual defect: `Classify()` isn't malfunctioning in the abstract — it implements a real, working, but extremely narrow and undocumented convention (`ucav-` prefix) that these 6 hand-authored fixtures happen to follow and the entire catalog-driven gauntlet corpus (~110 real platform ids, none prefixed `ucav-`) does not and structurally cannot. Any future scenario built from real catalog platforms will hit the same silent trap these two gauntlet files did, unless the classifier is generalized to use actual catalog domain data (see Suggested Fix) rather than an id-prefix convention invented for one early test suite.

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
