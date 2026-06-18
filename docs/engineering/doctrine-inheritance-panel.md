# Doctrine inheritance panel — developer reference

Developer reference for the **doctrine inheritance panel** projection
(`src/ProjectAegis.Delegation/Projection/DoctrineInheritance*.cs`) — the read-only view model that
surfaces a unit's **effective ROE / salvo policy** and **where that policy was inherited from**
(scenario default → mission → unit override) so the C2 UI and MCP can render the req-13 *visual
inheritance chain*. Like every binder in the `Projection/` namespace it is a pure, deterministic
projection ([ADR-010](../architecture/adr-010-headless-first-command-driven-ui.md)): it depends only
on `ProjectAegis.Sim` policy types, has **no `UnityEngine` dependency**, and **never mutates policy**.
The actual override write path is a separate command,
[`DoctrineOverrideCommand`](#applying-an-override-doctrineoverridecommand), which goes through the
delegation orchestrator's snapshot + decision-log contracts. Requirement coverage is
[req-13 — Policy Inheritance Model](../../Game-Requirements/requirements/13-Doctrine-ROE-EMCON-WRA.md)
(P0 *Unit/mission override with inheritance — visual inheritance chain*).

| Question | Answer |
|----------|--------|
| What does it do? | Projects a unit's resolved ROE, max-salvo, inheritance **source**, and override state into UI-ready strings, plus the selectable ROE options for the override dropdown. |
| Where does the code live? | `src/ProjectAegis.Delegation/Projection/DoctrineInheritanceProjection.cs`, `DoctrineInheritancePanelState.cs`, `DoctrineInheritancePanelBinder.cs`. The write path is `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DoctrineOverrideCommand.cs`. |
| Does it change policy or touch Unity? | **No.** It is presentation-only (ADR-010): pure C#, deterministic, read-only. It reads `ScenarioPolicyProfile`; it never writes. |
| Where does the inheritance precedence come from? | `ScenarioPolicyProfile.ResolveUnitPolicy` in `ProjectAegis.Sim` — the projection only **labels** what the sim resolved. |
| How do I apply an override? | `DoctrineOverrideCommand.TryApply(orchestrator, unitId, roeLabel, simTime)` — captures a policy snapshot and appends a `PolicyUpdateRecord`. |
| Is it wired into Unity yet? | The projection/binder/command are headless and fully tested; Unity binds them through the adapter seam (`PlayModeSmokeHarnessTests` exercises the override command). |

## Why it exists

`req-13` requires that ROE/WRA/EMCON doctrine be **inherited** and that the UI and MCP expose the
**effective policy after inheritance**, not just local overrides — with visible provenance
(`inherited | user | agent | event | scenario_template`). Per ADR-010 the simulation core stays
headless and authoritative, so the inheritance chain is resolved in `ProjectAegis.Sim`
(`ScenarioPolicyProfile.ResolveUnitPolicy`) and merely **projected** into display strings here. This
keeps three properties:

- **Single source of truth** — precedence lives in the sim's `ResolveUnitPolicy`, not in the UI. The
  panel cannot disagree with what the engine will actually enforce.
- **Deterministic & testable headless** — the projection is a pure function of
  `(unitId, ScenarioPolicyProfile, isFriendly)`; `ProjectAllUnits` sorts by `EntityId`
  (`StringComparer.Ordinal`) so rendered order is stable across runs and platforms.
- **Read/write separation** — rendering the chain (projection) and changing it
  (`DoctrineOverrideCommand` → snapshot + decision log) are distinct paths. The panel never writes.

## Components

| Type | Kind | Role |
|------|------|------|
| `DoctrineInheritanceProjection` | static | `ProjectUnit` / `ProjectAllUnits`: resolve a unit's policy and build a `DoctrineInheritanceEntry`. |
| `DoctrineInheritanceEntry` | record | Projected view model: effective ROE/salvo labels, inheritance source, and override flags. |
| `DoctrineInheritancePanelBinder` | static | `Bind`: turn an entry into final panel strings + ROE dropdown options + the `CanOverride` gate. |
| `DoctrineInheritancePanelState` | record | UI-ready lines (`UnitIdLine`, `RoeLine`, `SalvoLine`, `SourceLine`, `OverrideLine`), `CanOverride`, and `RoeOptions`. |
| `RoeLevelOption` | record | One selectable ROE level for the override dropdown: `Label`, `Value`, `IsCurrent`. |
| `DoctrineOverrideCommand` | static | Headless write path (UnityAdapter): apply a new ROE, snapshot it, and log a `PolicyUpdateRecord`. |
| `ScenarioPolicyProfile.ResolveUnitPolicy` | sim | Authoritative inheritance resolution → `ResolvedUnitPolicy` (effective policy + `RoeInheritedFromMission`). |

## Flow

```
ScenarioPolicyProfile (sim)                      DoctrineInheritanceProjection.ProjectUnit(unitId, policy, isFriendly)
        │  ResolveUnitPolicy(unitKey, isFriendly)        │  policy == null → "—" / "OVERRIDE: UNAVAILABLE" placeholder
        ▼                                                ▼
ResolvedUnitPolicy { Effective, RoeInheritedFromMission }
        │                                                │  source = Unit Override | Mission | Scenario Default
        ▼                                                ▼
DoctrineInheritanceEntry { RoeLabel, SalvoLabel, Source, IsInheritedFromMission, HasLocalOverride, OverrideButtonLabel }
        │  DoctrineInheritancePanelBinder.Bind(entry)
        ▼
DoctrineInheritancePanelState { UnitIdLine, RoeLine, SalvoLine, SourceLine, OverrideLine, CanOverride, RoeOptions[] }
        │  (UI render — read only)
        ▼
user picks a level  ──►  DoctrineOverrideCommand.TryApply(orchestrator, unitId, roeLabel, simTime)
                                │  PolicySnapshots.Capture + DecisionLog.AppendPolicyUpdate(PolicyUpdateRecord)
                                ▼
                         orchestrator policy state (next ResolveUnitPolicy reflects the override)
```

The projection stops at `DoctrineInheritancePanelState`. The arrow into the orchestrator is the
**separate** write command — nothing in the projection or binder mutates state.

## Inheritance precedence (resolved in `ScenarioPolicyProfile.ResolveUnitPolicy`)

Most specific wins. The projection labels the winner via `SourceLine`:

| Precedence | Condition | `SourceLine` | `IsInheritedFromMission` |
|-----------|-----------|--------------|--------------------------|
| 1 (highest) | `unitId` is in `UnitOverrides` | `SOURCE: Unit Override` | `false` |
| 2 | `MissionRoe` set **and** `unitId` ∈ `MissionUnitIds` | `SOURCE: Mission` | `true` |
| 3 (default) | otherwise | `SOURCE: Scenario Default` | `false` |

A `null` policy yields the unavailable placeholder (`ROE: —`, `SOURCE: —`, `OVERRIDE: UNAVAILABLE`).
Because a **unit override flips `RoeInheritedFromMission` to `false`**, `IsInheritedFromMission` and
`HasLocalOverride` are effectively mutually exclusive: an override always supersedes mission
inheritance.

### Override-control gating (`DoctrineInheritancePanelBinder`)

```
CanOverride = !entry.IsInheritedFromMission || entry.HasLocalOverride
```

In practice this **disables** the override controls only while a unit is *currently inheriting its
ROE from the mission* (no local override). Scenario-default units and units that already carry a unit
override keep override controls enabled. This implements the req-13 rule that mission-assigned
doctrine is mission-controlled and is shown — but not silently overridable at the unit level — until
an explicit override exists.

`RoeOptions` always lists the three levels (`HoldFire`, `WeaponsTight`, `WeaponsFree`); the one whose
name appears in the current ROE label (case-insensitive) is marked `IsCurrent`. The unavailable
(`null` entry) state returns an empty option list and `CanOverride = false`.

## Label reference

| Field | Format | Example |
|-------|--------|---------|
| `UnitIdLine` | `UNIT: {id}` / `UNIT: —` | `UNIT: u1` |
| `RoeLine` | `ROE: {RoeLevel}` / `ROE: —` | `ROE: WeaponsTight` |
| `SalvoLine` | `SALVO: {MaxSalvo}` / `SALVO: —` | `SALVO: 2` |
| `SourceLine` | `SOURCE: {Unit Override\|Mission\|Scenario Default}` / `SOURCE: —` | `SOURCE: Mission` |
| `OverrideLine` | `OVERRIDE: {ACTIVE\|NONE\|UNAVAILABLE}` | `OVERRIDE: ACTIVE` |

## Usage

```csharp
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;

// Read side: project the inheritance chain for the selected unit, then bind to panel strings.
DoctrineInheritanceEntry? entry = DoctrineInheritanceProjection.ProjectUnit(
    new TargetId("su-27-flanker"),
    scenario.PolicyProfile,   // ScenarioPolicyProfile, may be null pre-load
    isFriendly: true);

DoctrineInheritancePanelState panel = DoctrineInheritancePanelBinder.Bind(entry);
// panel.SourceLine == "SOURCE: Mission", panel.CanOverride == false, etc.

// Whole roster (deterministic, ordinal-sorted by unit id):
IReadOnlyList<DoctrineInheritanceEntry> all =
    DoctrineInheritanceProjection.ProjectAllUnits(unitIds, scenario.PolicyProfile, isFriendly: true);
```

### Applying an override (`DoctrineOverrideCommand`)

```csharp
using ProjectAegis.Delegation.UnityAdapter.Bridge;

bool applied = DoctrineOverrideCommand.TryApply(
    orchestrator,
    new TargetId("su-27-flanker"),
    roeLevelLabel: "WeaponsTight",   // parsed case-insensitively into RoeLevel
    simTime: currentSimTime);
```

`TryApply` returns `false` (and writes nothing) when:

- the `orchestrator` is `null`, or the label does not parse to a `RoeLevel`; or
- the requested ROE equals the unit's current effective ROE (**idempotent** — no duplicate
  `PolicyUpdateRecord`).

On success it captures a policy snapshot (`PolicySnapshots.Capture`) and appends a
`PolicyUpdateRecord(field: "roe", previous, new, snapshotId, simTick)` to the decision log, so the
override carries provenance and is replayable. Salvo (`MaxSalvo`) is preserved from the current
effective policy — only ROE changes through this command.

## Tests

| Test | Asserts |
|------|---------|
| `DoctrineInheritanceProjectionTests` | Mission-assigned unit shows `SOURCE: Mission` + inherited flag; unit override wins over mission; `null` policy → unavailable placeholder; non-mission unit → `SOURCE: Scenario Default`. |
| `DoctrineInheritancePanelBinderTests` | Entry maps to ROE/salvo/source lines; mission-inherited-without-override disables controls; `null` entry → unavailable state with empty options; scenario-default unit enables controls with 3 ROE options. |
| `DoctrineOverrideCommandTests` | Override changes policy and logs a `PolicyUpdateRecord` with previous/new ROE + snapshot id; idempotent when ROE unchanged; rejects unknown label and `null` orchestrator. |
| `PlayModeSmokeHarnessTests` (UnityAdapter) | Exercises the override command through the headless Play Mode seam. |

Run them with:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter DoctrineInheritance
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter DoctrineOverride
```

## Common pitfalls

- **The panel never changes policy.** Projection + binder are read-only. To mutate ROE you must call
  `DoctrineOverrideCommand.TryApply` (or another orchestrator command); binding alone does nothing.
- **`CanOverride` gates mission-inherited units.** A unit whose ROE comes from the mission
  (`SOURCE: Mission`, no local override) returns `CanOverride = false`. That is by design — render the
  controls as disabled rather than treating it as an error.
- **Source precedence is fixed in the sim, not the UI.** Don't re-derive precedence in UI code; call
  `ScenarioPolicyProfile.ResolveUnitPolicy` (via the projection) so the panel always agrees with what
  the engine enforces.
- **Overrides are idempotent.** Re-applying the current ROE returns `false` and logs nothing; don't
  treat a `false` result as a failure when the value already matches.
- **Labels are display contracts.** Tests assert exact strings (`"SOURCE: Mission"`,
  `"OVERRIDE: UNAVAILABLE"`, the em dash `—` placeholders). Changing a label format is a breaking UI
  change — update the projection tests deliberately.
- **`ProjectAllUnits` is ordinal-sorted.** Order is stable and culture-invariant; don't re-sort with a
  culture-sensitive comparer or you reintroduce non-determinism.

## Related

- [ADR-010 — Headless-first / command-driven UI](../architecture/adr-010-headless-first-command-driven-ui.md)
- Requirement [13 — Doctrine, ROE, EMCON, WRA](../../Game-Requirements/requirements/13-Doctrine-ROE-EMCON-WRA.md) (Policy Inheritance Model)
- [Balance telemetry & drift detection — developer reference](balance-telemetry-drift.md)
- Code: `src/ProjectAegis.Delegation/Projection/DoctrineInheritance*.cs`; write path:
  `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DoctrineOverrideCommand.cs`;
  tests: `src/ProjectAegis.Delegation.Tests/Projection/`,
  `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DoctrineOverrideCommandTests.cs`
