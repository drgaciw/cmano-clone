# Scenario Editor Phase 2.1 — Map + ORBAT + Zones + Live Findings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.  
> **Governing design:** `docs/superpowers/specs/2026-07-08-scenario-editor-phase2-design.md` (**Approved**).  
> **Worktree:** Prefer `.worktrees/scenario-editor-p2-1/` (gitignored). Do not implement on dirty unrelated trees.  
> **Collaboration:** User-driven — show drafts for multi-file code; no commits without user instruction unless user says execute+commit.

**Goal:** Ship the Phase 2.1 vertical slice: map-first ORBAT place/move/clone, reference-point geometries, selection inspector, attach mission from selection, and a live Findings panel that uses the same pure `ScenarioValidationEngine` as CLI — all committed through `ScenarioDocumentEditor` into canonical `scenario.json`.

**Architecture:** Approach A — shared Data core owns mutations; headless presentation/session facades live in `ProjectAegis.Delegation.UnityAdapter` (no `UnityEngine` dependency); optional thin Unity Editor host under `unity/ProjectAegis/Assets/Editor` binds gestures to the command bus. No `DelegationBridge` hotpath edits. No CatalogWriteGate scenario writes.

**Tech Stack:** .NET 8, `ProjectAegis.Data` authoring + validation, `ProjectAegis.MissionEditor.Cli` MCP verbs, `ProjectAegis.Delegation.UnityAdapter` presentation facades, xUnit tests, Graphite (`gt`), GitNexus impact before symbol edits.

---

## Standing invariants (every gate)

| Invariant | Rule |
|-----------|------|
| Test floor | ≥1232 solution tests, 0 new failures |
| ReplayGolden | 6/6 |
| Hash | `17144800277401907079` preserved |
| PlayModeSmoke | ≥18/18 (additive AC-8 / map tests OK) |
| `DelegationBridge.cs` | **ZERO** production hotpath edits |
| CatalogWriteGate | Extend-only; scenario edits are **file-based** |
| Baltic corpora | Frozen |
| Stage | Release |

**Pre-edit (any task that touches a public symbol):**

```bash
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only
```

Report blast radius; if CRITICAL, warn user before editing.

**End-of-slice gate (RUN+READ all):**

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
grep -r "17144800277401907079" tests/ data/ | head
# Confirm zero new DelegationBridge hotpath production edits in the slice diff
```

---

## File map (create / modify)

### Create (Data — canonical mutations)

| Path | Responsibility |
|------|----------------|
| `src/ProjectAegis.Data/Scenario/Authoring/ScenarioGeometryValidity.cs` | Pure geometry validity for RP types (polygon/circle/line/point) |
| `src/ProjectAegis.Data.Tests/Scenario/ScenarioOrbatReferencePointEditorTests.cs` | TDD for place/move/clone unit + upsert RP |
| `src/ProjectAegis.Data.Tests/Scenario/ScenarioGeometryValidityTests.cs` | Geometry validity unit tests |
| `src/ProjectAegis.Data.Tests/Scenario/ScenarioEditCommandBusTests.cs` | Command bus: editVersion, undo, live findings |

### Create (Data — command bus / session, engine-agnostic)

| Path | Responsibility |
|------|----------------|
| `src/ProjectAegis.Data/Scenario/Authoring/ScenarioEditCommandBus.cs` | Serializes mutations: require editVersion → undo capture → mutate → commit → optional save; returns findings snapshot |
| `src/ProjectAegis.Data/Scenario/Authoring/ScenarioAuthoringSession.cs` | Load path, dirty flag, save, derived `editorState` only |

### Create (UnityAdapter — headless presentation; no UnityEngine)

| Path | Responsibility |
|------|----------------|
| `src/ProjectAegis.Delegation.UnityAdapter/Authoring/MapAuthoringSurface.cs` | ORBAT glyphs + RP geometry view models; tentative overlay until gesture-end commit |
| `src/ProjectAegis.Delegation.UnityAdapter/Authoring/EditModeController.cs` | Play ↔ Edit FSM; routes commit intent to bus |
| `src/ProjectAegis.Delegation.UnityAdapter/Authoring/LiveFindingsPresenter.cs` | Debounce façade over `LiveValidate` / engine; jump-to entity ids |
| `src/ProjectAegis.Delegation.UnityAdapter/Authoring/SelectionInspectorModel.cs` | Selected unit / RP / mission summary DTO |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Authoring/MapAuthoringSurfaceTests.cs` | Surface + commit contract |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Authoring/EditModeControllerTests.cs` | FSM + dirty/play guard |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Authoring/LiveFindingsPresenterTests.cs` | Debounce + code parity |

### Create (CLI / MCP parity)

| Path | Responsibility |
|------|----------------|
| `src/ProjectAegis.MissionEditor.Cli/OrbatUpsertUnitCommand.cs` | Place or update unit lat/lon |
| `src/ProjectAegis.MissionEditor.Cli/OrbatMoveUnitCommand.cs` | Move existing unit |
| `src/ProjectAegis.MissionEditor.Cli/OrbatCloneUnitCommand.cs` | Clone unit with new id |
| `src/ProjectAegis.MissionEditor.Cli/ReferencePointUpsertCommand.cs` | Upsert RP geometry |
| `src/ProjectAegis.MissionEditor.Cli.Tests/OrbatReferencePointCliTests.cs` | CLI contract + CONFLICT |
| Modify: `src/ProjectAegis.MissionEditor.Cli/Program.cs` | Wire verbs |
| Modify: MCP tools manifest (path used by `McpToolsManifestTests` — keep in sync) |

### Optional (Unity Editor host — Approach A glue)

| Path | Responsibility |
|------|----------------|
| `unity/ProjectAegis/Assets/Editor/ScenarioMapAuthoringWindow.cs` | Thin EditorWindow: open scenario, show findings list, call session/bus (no business rules) |

### Docs (closeout)

| Path | Responsibility |
|------|----------------|
| `production/scenario-editor-phase2-1-scope-boundary-2026-07-08.md` | Scope boundary for P2.1 |
| `docs/superpowers/specs/2026-07-08-scenario-editor-phase2-design.md` | Status → Approved / Implemented notes |

### Explicit non-touch

- `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` (zero-touch)
- `CatalogWriteGate` write paths
- Baltic v2/v3 policy/golden fixtures
- Mission Board (P2.2), event graph (P2.3)

---

## Reality check (codebase as of plan authoring)

| Fact | Implication |
|------|-------------|
| `ScenarioDocumentEditor` already has missions + `LiveValidate()` + undo + `editVersion` | **Reuse** — do not reimplement |
| ORBAT / RP **DTOs exist** (`ScenarioOrbatUnitDto`, `ScenarioReferencePointDto`) and serialize via `ScenarioStableJsonWriter` | **No new schema** for P2.1 core |
| **No public** place/move/clone unit or upsert RP APIs yet | Task 1 is the foundation |
| `C2PresentationController` is **selection-only** runtime C2, not map authoring | Do **not** overload it; add `Authoring/` facades |
| UnityAdapter has **no UnityEngine** ref | Business logic + tests stay headless; Editor window is optional thin glue |
| Example `baltic-patrol.scenario.json` is missions-only (no orbat body) | Tests use temp files + optional new fixture under `data/scenarios/examples/` if needed |

---

## Task 0: Map / C2 reuse spike (read-only, 30–60 min)

**Files:** Read only — no production edits.

- [ ] **Step 1: Inventory presentation surfaces**

Read:

- `src/ProjectAegis.Delegation.UnityAdapter/Presentation/C2PresentationController.cs`
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Map/*`
- `unity/ProjectAegis/Assets/Scripts/Runtime/C2*.cs`
- `unity/ProjectAegis/Assets/Editor/*`

- [ ] **Step 2: Write spike note (3–8 bullets) into the scope boundary draft**

Record:

1. What can be reused (APP6 glyphs contracts, lat/lon conventions).  
2. What must **not** be reused (runtime selection bound to `DelegationBridge` / sim snapshot).  
3. Decision: **new `Authoring/` namespace** in UnityAdapter for map authoring (confirmed if spike agrees).

- [ ] **Step 3: Do not commit code** — spike only; proceed to Task 1.

---

### Task 1: Geometry validity helper (TDD)

**Files:**

- Create: `src/ProjectAegis.Data/Scenario/Authoring/ScenarioGeometryValidity.cs`
- Create: `src/ProjectAegis.Data.Tests/Scenario/ScenarioGeometryValidityTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class ScenarioGeometryValidityTests
{
    [Fact]
    public void Point_with_one_vertex_is_valid()
    {
        var rp = new ScenarioReferencePointDto
        {
            Id = "rp1",
            Type = "point",
            Geometry = [new ScenarioWaypointDto { Lat = 57, Lon = 20 }],
        };
        Assert.True(ScenarioGeometryValidity.IsValid(rp, out var reason));
        Assert.Null(reason);
    }

    [Fact]
    public void Polygon_with_fewer_than_three_vertices_is_invalid()
    {
        var rp = new ScenarioReferencePointDto
        {
            Id = "rp-poly",
            Type = "polygon",
            Geometry =
            [
                new ScenarioWaypointDto { Lat = 57, Lon = 20 },
                new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
            ],
        };
        Assert.False(ScenarioGeometryValidity.IsValid(rp, out var reason));
        Assert.Contains("polygon", reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Circle_requires_center_and_positive_radius_nm()
    {
        var bad = new ScenarioReferencePointDto
        {
            Id = "c1",
            Type = "circle",
            Geometry = [new ScenarioWaypointDto { Lat = 57, Lon = 20 }],
            RadiusNm = null,
        };
        Assert.False(ScenarioGeometryValidity.IsValid(bad, out _));

        var good = bad with { }; // if not record, rebuild:
        good = new ScenarioReferencePointDto
        {
            Id = "c1",
            Type = "circle",
            Geometry = [new ScenarioWaypointDto { Lat = 57, Lon = 20 }],
            RadiusNm = 5,
        };
        Assert.True(ScenarioGeometryValidity.IsValid(good, out _));
    }

    [Fact]
    public void Line_requires_at_least_two_vertices()
    {
        var rp = new ScenarioReferencePointDto
        {
            Id = "ln",
            Type = "line",
            Geometry = [new ScenarioWaypointDto { Lat = 57, Lon = 20 }],
        };
        Assert.False(ScenarioGeometryValidity.IsValid(rp, out _));
    }
}
```

Note: `ScenarioReferencePointDto` is a class with `init` — rebuild instances (no `with`).

- [ ] **Step 2: Run tests — expect FAIL**

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter ScenarioGeometryValidityTests -v minimal
```

Expected: compile error or missing type `ScenarioGeometryValidity`.

- [ ] **Step 3: Implement**

```csharp
namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>Pure geometry checks for reference points (map invalid-mark contract). Not a Validation Engine rule.</summary>
public static class ScenarioGeometryValidity
{
    public static bool IsValid(ScenarioReferencePointDto rp, out string? reason)
    {
        reason = null;
        if (rp is null)
        {
            reason = "reference point is null";
            return false;
        }

        var type = (rp.Type ?? "point").Trim().ToLowerInvariant();
        var n = rp.Geometry?.Count ?? 0;

        switch (type)
        {
            case "point":
                if (n < 1)
                {
                    reason = "point requires at least 1 vertex";
                    return false;
                }
                return true;
            case "line":
            case "corridor":
                if (n < 2)
                {
                    reason = $"{type} requires at least 2 vertices";
                    return false;
                }
                return true;
            case "polygon":
                if (n < 3)
                {
                    reason = "polygon requires at least 3 vertices";
                    return false;
                }
                return true;
            case "circle":
                if (n < 1)
                {
                    reason = "circle requires a center vertex";
                    return false;
                }
                if (rp.RadiusNm is null or <= 0)
                {
                    reason = "circle requires RadiusNm > 0";
                    return false;
                }
                return true;
            default:
                reason = $"unknown geometry type '{rp.Type}'";
                return false;
        }
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter ScenarioGeometryValidityTests -v minimal
```

- [ ] **Step 5: Commit** (only if user instructed)

```bash
git add src/ProjectAegis.Data/Scenario/Authoring/ScenarioGeometryValidity.cs \
        src/ProjectAegis.Data.Tests/Scenario/ScenarioGeometryValidityTests.cs
git commit -m "$(cat <<'EOF'
feat(editor): add ScenarioGeometryValidity for map RP types

P2.1 foundation for invalid-draw marking without Validation Engine fork.
EOF
)"
```

---

### Task 2: ScenarioDocumentEditor ORBAT + RP mutations (TDD)

**Files:**

- Modify: `src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentEditor.cs`
- Create: `src/ProjectAegis.Data.Tests/Scenario/ScenarioOrbatReferencePointEditorTests.cs`

**GitNexus:** `impact ScenarioDocumentEditor` upstream; warn if CRITICAL (expected).

- [ ] **Step 1: Write failing tests**

```csharp
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class ScenarioOrbatReferencePointEditorTests
{
    [Fact]
    public void UpsertUnit_places_unit_and_round_trips_json()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-orbat-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.Save(path);

            var loaded = ScenarioDocumentEditor.Load(path);
            loaded.RequireEditVersion(1);
            var undo = loaded.CaptureUndoSnapshot();
            loaded.UpsertOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "u-blue-1",
                SideId = "blue",
                PlatformId = "ffg-generic",
                Lat = 57.05,
                Lon = 20.15,
            });
            loaded.PersistUndoSnapshot(path, undo);
            loaded.CommitMutation();
            loaded.Save(path);

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, dto.Metadata.EditVersion);
            Assert.NotNull(dto.Orbat);
            Assert.Single(dto.Orbat!.Units);
            Assert.Equal("u-blue-1", dto.Orbat.Units[0].Id);
            Assert.Equal(57.05, dto.Orbat.Units[0].Lat);
            Assert.Equal(20.15, dto.Orbat.Units[0].Lon);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void MoveUnit_updates_lat_lon_preserves_other_fields()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
        {
            Id = "u1",
            SideId = "blue",
            PlatformId = "ffg",
            Lat = 57,
            Lon = 20,
            RoeOverride = "WeaponsHold",
        });
        editor.MoveOrbatUnit("u1", lat: 58.0, lon: 21.0);

        var u = editor.ToDto().Orbat!.Units.Single(x => x.Id == "u1");
        Assert.Equal(58.0, u.Lat);
        Assert.Equal(21.0, u.Lon);
        Assert.Equal("WeaponsHold", u.RoeOverride);
        Assert.Equal("blue", u.SideId);
    }

    [Fact]
    public void CloneUnit_creates_new_id_at_offset_position()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
        {
            Id = "u1",
            SideId = "blue",
            PlatformId = "ffg",
            Lat = 57,
            Lon = 20,
        });
        editor.CloneOrbatUnit("u1", newUnitId: "u1-clone", lat: 57.01, lon: 20.01);

        var units = editor.ToDto().Orbat!.Units;
        Assert.Equal(2, units.Count);
        Assert.Contains(units, u => u.Id == "u1-clone" && u.PlatformId == "ffg");
    }

    [Fact]
    public void CloneUnit_duplicate_id_throws()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
        {
            Id = "u1", SideId = "blue", PlatformId = "ffg", Lat = 57, Lon = 20,
        });
        Assert.Throws<InvalidOperationException>(() =>
            editor.CloneOrbatUnit("u1", newUnitId: "u1", lat: 57, lon: 20));
    }

    [Fact]
    public void UpsertReferencePoint_round_trips_polygon()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-rp-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.Save(path);
            var loaded = ScenarioDocumentEditor.Load(path);
            loaded.RequireEditVersion(1);
            loaded.UpsertReferencePoint(new ScenarioReferencePointDto
            {
                Id = "zone-patrol",
                Type = "polygon",
                Geometry =
                [
                    new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.2, Lon = 20.0 },
                ],
            });
            loaded.CommitMutation();
            loaded.Save(path);

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Single(dto.ReferencePoints);
            Assert.Equal("polygon", dto.ReferencePoints[0].Type);
            Assert.Equal(3, dto.ReferencePoints[0].Geometry.Count);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void RemoveReferencePoint_removes_by_id()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertReferencePoint(new ScenarioReferencePointDto
        {
            Id = "rp1",
            Type = "point",
            Geometry = [new ScenarioWaypointDto { Lat = 1, Lon = 2 }],
        });
        Assert.True(editor.TryRemoveReferencePoint("rp1"));
        Assert.Empty(editor.ToDto().ReferencePoints);
    }

    [Fact]
    public void MoveUnit_missing_throws()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        Assert.Throws<InvalidOperationException>(() => editor.MoveOrbatUnit("missing", 0, 0));
    }
}
```

- [ ] **Step 2: Run — expect FAIL** (methods missing)

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter ScenarioOrbatReferencePointEditorTests -v minimal
```

- [ ] **Step 3: Implement methods on `ScenarioDocumentEditor`**

Add after mission methods (before `Save`), preserving existing style:

```csharp
/// <summary>Inserts or replaces an ORBAT unit by id (map place / inspector apply).</summary>
public void UpsertOrbatUnit(ScenarioOrbatUnitDto unit)
{
    if (unit is null) throw new ArgumentNullException(nameof(unit));
    if (string.IsNullOrWhiteSpace(unit.Id))
        throw new InvalidOperationException("Unit id is required.");

    var units = (_orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>()).ToList();
    var idx = units.FindIndex(u => string.Equals(u.Id, unit.Id, StringComparison.OrdinalIgnoreCase));
    var copy = new ScenarioOrbatUnitDto
    {
        Id = unit.Id,
        SideId = unit.SideId,
        PlatformId = unit.PlatformId,
        Lat = unit.Lat,
        Lon = unit.Lon,
        ParentUnitId = unit.ParentUnitId,
        RoeOverride = unit.RoeOverride,
        EmconOverride = unit.EmconOverride,
    };
    if (idx >= 0) units[idx] = copy;
    else units.Add(copy);

    _orbat = new ScenarioOrbatDto
    {
        Units = units,
        Bases = _orbat?.Bases ?? Array.Empty<ScenarioOrbatBaseDto>(),
    };
}

/// <summary>Moves an existing ORBAT unit; preserves non-position fields.</summary>
public void MoveOrbatUnit(string unitId, double lat, double lon)
{
    var units = (_orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>()).ToList();
    var idx = units.FindIndex(u => string.Equals(u.Id, unitId, StringComparison.OrdinalIgnoreCase));
    if (idx < 0)
        throw new InvalidOperationException($"Unit id '{unitId}' was not found.");

    var u = units[idx];
    units[idx] = new ScenarioOrbatUnitDto
    {
        Id = u.Id,
        SideId = u.SideId,
        PlatformId = u.PlatformId,
        Lat = lat,
        Lon = lon,
        ParentUnitId = u.ParentUnitId,
        RoeOverride = u.RoeOverride,
        EmconOverride = u.EmconOverride,
    };
    _orbat = new ScenarioOrbatDto
    {
        Units = units,
        Bases = _orbat?.Bases ?? Array.Empty<ScenarioOrbatBaseDto>(),
    };
}

/// <summary>Clones an existing unit under a new id at the given position.</summary>
public void CloneOrbatUnit(string sourceUnitId, string newUnitId, double lat, double lon)
{
    if (string.IsNullOrWhiteSpace(newUnitId))
        throw new InvalidOperationException("New unit id is required.");

    var units = (_orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>()).ToList();
    if (units.Any(u => string.Equals(u.Id, newUnitId, StringComparison.OrdinalIgnoreCase)))
        throw new InvalidOperationException($"Unit id '{newUnitId}' already exists.");

    var src = units.FirstOrDefault(u =>
        string.Equals(u.Id, sourceUnitId, StringComparison.OrdinalIgnoreCase));
    if (src is null)
        throw new InvalidOperationException($"Unit id '{sourceUnitId}' was not found.");

    units.Add(new ScenarioOrbatUnitDto
    {
        Id = newUnitId,
        SideId = src.SideId,
        PlatformId = src.PlatformId,
        Lat = lat,
        Lon = lon,
        ParentUnitId = src.ParentUnitId,
        RoeOverride = src.RoeOverride,
        EmconOverride = src.EmconOverride,
    });
    _orbat = new ScenarioOrbatDto
    {
        Units = units,
        Bases = _orbat?.Bases ?? Array.Empty<ScenarioOrbatBaseDto>(),
    };
}

/// <summary>Inserts or replaces a reference point by id (map draw gesture-end).</summary>
public void UpsertReferencePoint(ScenarioReferencePointDto point)
{
    if (point is null) throw new ArgumentNullException(nameof(point));
    if (string.IsNullOrWhiteSpace(point.Id))
        throw new InvalidOperationException("Reference point id is required.");

    var list = _referencePoints.ToList();
    var idx = list.FindIndex(p => string.Equals(p.Id, point.Id, StringComparison.OrdinalIgnoreCase));
    var copy = new ScenarioReferencePointDto
    {
        Id = point.Id,
        Type = point.Type,
        Geometry = point.Geometry?
            .Select(w => new ScenarioWaypointDto { Lat = w.Lat, Lon = w.Lon })
            .ToArray() ?? Array.Empty<ScenarioWaypointDto>(),
        RadiusNm = point.RadiusNm,
    };
    if (idx >= 0) list[idx] = copy;
    else list.Add(copy);
    _referencePoints = list;
}

public bool TryRemoveReferencePoint(string referencePointId)
{
    var list = _referencePoints.ToList();
    var removed = list.RemoveAll(p =>
        string.Equals(p.Id, referencePointId, StringComparison.OrdinalIgnoreCase));
    if (removed == 0) return false;
    _referencePoints = list;
    return true;
}
```

Also ensure `RestoreFromDto` already restores ORBAT/RP via `RestoreCanonicalSections` (it does). No change required to `ToDto`.

- [ ] **Step 4: Run tests — expect PASS**

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "ScenarioOrbatReferencePointEditorTests|ScenarioDocumentEditorTests" -v minimal
```

- [ ] **Step 5: Commit** (if instructed)

```bash
git commit -m "feat(editor): ORBAT place/move/clone and reference-point upsert APIs"
```

---

### Task 3: ScenarioEditCommandBus + ScenarioAuthoringSession (TDD)

**Files:**

- Create: `src/ProjectAegis.Data/Scenario/Authoring/ScenarioEditCommandBus.cs`
- Create: `src/ProjectAegis.Data/Scenario/Authoring/ScenarioAuthoringSession.cs`
- Create: `src/ProjectAegis.Data.Tests/Scenario/ScenarioEditCommandBusTests.cs`

- [ ] **Step 1: Failing tests**

```csharp
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class ScenarioEditCommandBusTests
{
    [Fact]
    public void Apply_place_unit_bumps_edit_version_marks_dirty_and_validates()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-bus-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            Assert.False(session.IsDirty);
            Assert.Equal(1, session.EditVersion);

            var result = session.Bus.PlaceUnit(
                expectedEditVersion: 1,
                new ScenarioOrbatUnitDto
                {
                    Id = "u1",
                    SideId = "blue",
                    PlatformId = "ffg",
                    Lat = 57,
                    Lon = 20,
                },
                save: true);

            Assert.True(result.Ok);
            Assert.Equal(2, result.EditVersion);
            Assert.NotNull(result.Report);
            // Fresh save clears dirty if save:true
            Assert.False(session.IsDirty);

            var reloaded = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Single(reloaded.Orbat!.Units);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Apply_stale_edit_version_returns_conflict_without_write()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-bus-c-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);

            var result = session.Bus.PlaceUnit(
                expectedEditVersion: 99,
                new ScenarioOrbatUnitDto
                {
                    Id = "u1", SideId = "blue", PlatformId = "ffg", Lat = 1, Lon = 2,
                },
                save: true);

            Assert.False(result.Ok);
            Assert.Equal(ScenarioEditVersionGuard.ConflictCode, result.ErrorCode);
            Assert.Equal(1, ScenarioDocumentJsonLoader.LoadFromFile(path).Metadata.EditVersion);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Live_findings_include_mission_no_units_after_empty_patrol_attach()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-bus-v-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.AttachPatrolFromSelection(
                expectedEditVersion: 1,
                missionId: "patrol-empty",
                unitIds: Array.Empty<string>(),
                zone:
                [
                    new ScenarioWaypointDto { Lat = 57, Lon = 20 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                ],
                save: true);

            var codes = session.Bus.LastReport!.Findings.Select(f => f.Code).ToHashSet();
            Assert.Contains("MISSION_NO_UNITS", codes);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
```

- [ ] **Step 2: Implement session + bus**

`ScenarioAuthoringSession.cs` sketch:

```csharp
namespace ProjectAegis.Data.Scenario.Authoring;

using ProjectAegis.Data.Validation;

/// <summary>File-backed authoring session: dirty flag, path, command bus. editorState is derived-only.</summary>
public sealed class ScenarioAuthoringSession : IDisposable
{
    private ScenarioAuthoringSession(string path, ScenarioDocumentEditor editor)
    {
        Path = path;
        Editor = editor;
        Bus = new ScenarioEditCommandBus(this);
    }

    public string Path { get; }
    public ScenarioDocumentEditor Editor { get; }
    public ScenarioEditCommandBus Bus { get; }
    public bool IsDirty { get; internal set; }
    public int EditVersion => Editor.Metadata.EditVersion;

    /// <summary>Derived-only camera/layers; never fed to Validation Engine.</summary>
    public ScenarioEditorStateDto EditorState { get; set; } = new();

    public static ScenarioAuthoringSession Open(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Scenario not found", path);
        return new ScenarioAuthoringSession(path, ScenarioDocumentEditor.Load(path));
    }

    public void Save()
    {
        Editor.Save(Path);
        IsDirty = false;
    }

    public void Dispose() { /* no unmanaged resources */ }
}

public sealed class ScenarioMutationResult
{
    public bool Ok { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public int EditVersion { get; init; }
    public string? FileHash { get; init; }
    public ValidationReport? Report { get; init; }
}

public sealed class ScenarioEditCommandBus
{
    private readonly ScenarioAuthoringSession _session;

    public ScenarioEditCommandBus(ScenarioAuthoringSession session) => _session = session;

    public ValidationReport? LastReport { get; private set; }

    public ScenarioMutationResult PlaceUnit(int expectedEditVersion, ScenarioOrbatUnitDto unit, bool save)
        => Mutate(expectedEditVersion, save, e => e.UpsertOrbatUnit(unit));

    public ScenarioMutationResult MoveUnit(int expectedEditVersion, string unitId, double lat, double lon, bool save)
        => Mutate(expectedEditVersion, save, e => e.MoveOrbatUnit(unitId, lat, lon));

    public ScenarioMutationResult CloneUnit(int expectedEditVersion, string sourceId, string newId, double lat, double lon, bool save)
        => Mutate(expectedEditVersion, save, e => e.CloneOrbatUnit(sourceId, newId, lat, lon));

    public ScenarioMutationResult UpsertReferencePoint(int expectedEditVersion, ScenarioReferencePointDto rp, bool save)
        => Mutate(expectedEditVersion, save, e => e.UpsertReferencePoint(rp));

    public ScenarioMutationResult AttachPatrolFromSelection(
        int expectedEditVersion,
        string missionId,
        IReadOnlyList<string> unitIds,
        IReadOnlyList<ScenarioWaypointDto> zone,
        bool save)
        => Mutate(expectedEditVersion, save, e => e.AddPatrolMission(missionId, unitIds, zone));

    // AttachStrike / Ferry / Support: same pattern calling existing Add*Mission APIs.

    private ScenarioMutationResult Mutate(int expectedEditVersion, bool save, Action<ScenarioDocumentEditor> action)
    {
        var editor = _session.Editor;
        try
        {
            editor.RequireEditVersion(expectedEditVersion, _session.Path);
            var snap = editor.CaptureUndoSnapshot();
            action(editor);
            editor.PersistUndoSnapshot(_session.Path, snap);
            editor.CommitMutation();
            _session.IsDirty = true;
            if (save) _session.Save();
            LastReport = editor.LiveValidate();
            return new ScenarioMutationResult
            {
                Ok = true,
                EditVersion = editor.Metadata.EditVersion,
                FileHash = editor.ComputeFileHash(),
                Report = LastReport,
            };
        }
        catch (ScenarioEditConflictException ex)
        {
            return new ScenarioMutationResult
            {
                Ok = false,
                ErrorCode = ex.Code,
                ErrorMessage = ex.Message,
                EditVersion = ex.CurrentEditVersion,
                FileHash = ex.FileHash,
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ScenarioMutationResult
            {
                Ok = false,
                ErrorCode = "INVALID_OPERATION",
                ErrorMessage = ex.Message,
                EditVersion = editor.Metadata.EditVersion,
            };
        }
    }

    public ValidationReport RefreshFindings()
    {
        LastReport = _session.Editor.LiveValidate();
        return LastReport;
    }
}
```

- [ ] **Step 3: Run tests PASS**

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter ScenarioEditCommandBusTests -v minimal
```

- [ ] **Step 4: Commit** (if instructed)

---

### Task 4: Headless MapAuthoringSurface + SelectionInspector (TDD)

**Files:**

- Create: `src/ProjectAegis.Delegation.UnityAdapter/Authoring/MapAuthoringSurface.cs`
- Create: `src/ProjectAegis.Delegation.UnityAdapter/Authoring/SelectionInspectorModel.cs`
- Create: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Authoring/MapAuthoringSurfaceTests.cs`

- [ ] **Step 1: Tests — rebuild glyphs from DTO; tentative vs committed**

```csharp
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using NUnit.Framework; // or Xunit — match project (UnityAdapter.Tests uses NUnit for PlayModeSmoke; check csproj)

// Prefer the same test framework as sibling tests in the assembly.
```

Inspect `ProjectAegis.Delegation.UnityAdapter.Tests.csproj` and **match existing framework** (NUnit vs xUnit). Write:

1. `RebuildFromDocument_creates_glyph_per_orbat_unit`  
2. `BeginPlaceUnit_then_Cancel_leaves_document_unchanged`  
3. `BeginPlaceUnit_then_CommitGesture_calls_session_bus_and_adds_glyph`  
4. `DrawPolygon_invalid_until_three_vertices_sets_InvalidGeometry_flag`  
5. `SelectUnit_populates_SelectionInspectorModel`

- [ ] **Step 2: Implement surface**

Core types:

```csharp
namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

using ProjectAegis.Data.Scenario.Authoring;

public sealed class OrbatGlyphView
{
    public string UnitId { get; init; } = "";
    public string SideId { get; init; } = "";
    public string PlatformId { get; init; } = "";
    public double Lat { get; init; }
    public double Lon { get; init; }
}

public sealed class ReferencePointView
{
    public string Id { get; init; } = "";
    public string Type { get; init; } = "point";
    public IReadOnlyList<ScenarioWaypointDto> Geometry { get; init; } = Array.Empty<ScenarioWaypointDto>();
    public double? RadiusNm { get; init; }
    public bool IsGeometryValid { get; init; }
    public string? InvalidReason { get; init; }
}

/// <summary>Map presentation + gesture staging. Commits only via ScenarioAuthoringSession.Bus.</summary>
public sealed class MapAuthoringSurface
{
    private readonly ScenarioAuthoringSession _session;
    private ScenarioOrbatUnitDto? _tentativeUnit;
    private ScenarioReferencePointDto? _tentativeRp;

    public MapAuthoringSurface(ScenarioAuthoringSession session) => _session = session;

    public IReadOnlyList<OrbatGlyphView> Units { get; private set; } = Array.Empty<OrbatGlyphView>();
    public IReadOnlyList<ReferencePointView> ReferencePoints { get; private set; } = Array.Empty<ReferencePointView>();
    public SelectionInspectorModel Selection { get; } = new();

    public void RebuildFromDocument()
    {
        var dto = _session.Editor.ToDto();
        Units = (dto.Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>())
            .OrderBy(u => u.Id, StringComparer.Ordinal)
            .Select(u => new OrbatGlyphView
            {
                UnitId = u.Id,
                SideId = u.SideId,
                PlatformId = u.PlatformId,
                Lat = u.Lat,
                Lon = u.Lon,
            })
            .ToArray();

        ReferencePoints = dto.ReferencePoints
            .OrderBy(p => p.Id, StringComparer.Ordinal)
            .Select(p =>
            {
                var valid = ScenarioGeometryValidity.IsValid(p, out var reason);
                return new ReferencePointView
                {
                    Id = p.Id,
                    Type = p.Type,
                    Geometry = p.Geometry,
                    RadiusNm = p.RadiusNm,
                    IsGeometryValid = valid,
                    InvalidReason = reason,
                };
            })
            .ToArray();
    }

    public void BeginPlaceUnit(ScenarioOrbatUnitDto tentative) => _tentativeUnit = tentative;

    public void CancelGesture()
    {
        _tentativeUnit = null;
        _tentativeRp = null;
    }

    public ScenarioMutationResult? CommitPlaceUnit(bool save = true)
    {
        if (_tentativeUnit is null) return null;
        var r = _session.Bus.PlaceUnit(_session.EditVersion, _tentativeUnit, save);
        _tentativeUnit = null;
        if (r.Ok) RebuildFromDocument();
        return r;
    }

    public void BeginDrawReferencePoint(ScenarioReferencePointDto tentative) => _tentativeRp = tentative;

    public ScenarioMutationResult? CommitReferencePoint(bool save = true)
    {
        if (_tentativeRp is null) return null;
        // Invalid geometry still commits (visible + marked invalid) per design §10
        var r = _session.Bus.UpsertReferencePoint(_session.EditVersion, _tentativeRp, save);
        _tentativeRp = null;
        if (r.Ok) RebuildFromDocument();
        return r;
    }

    public void SelectUnit(string unitId)
    {
        var u = _session.Editor.ToDto().Orbat?.Units
            .FirstOrDefault(x => string.Equals(x.Id, unitId, StringComparison.OrdinalIgnoreCase));
        Selection.SetUnit(u);
    }
}

public sealed class SelectionInspectorModel
{
    public string? SelectedUnitId { get; private set; }
    public string? SelectedReferencePointId { get; private set; }
    public string SummaryLine { get; private set; } = "";

    public void SetUnit(ScenarioOrbatUnitDto? u)
    {
        SelectedReferencePointId = null;
        if (u is null)
        {
            SelectedUnitId = null;
            SummaryLine = "";
            return;
        }
        SelectedUnitId = u.Id;
        SummaryLine = $"{u.Id} | {u.SideId} | {u.PlatformId} @ {u.Lat:F3},{u.Lon:F3}";
    }

    public void SetReferencePoint(ScenarioReferencePointDto? rp)
    {
        SelectedUnitId = null;
        if (rp is null)
        {
            SelectedReferencePointId = null;
            SummaryLine = "";
            return;
        }
        SelectedReferencePointId = rp.Id;
        SummaryLine = $"{rp.Id} | {rp.Type} | verts={rp.Geometry.Count}";
    }
}
```

- [ ] **Step 3: Run UA authoring tests PASS**

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter MapAuthoringSurfaceTests -v minimal
```

---

### Task 5: EditModeController + LiveFindingsPresenter (TDD)

**Files:**

- Create: `src/ProjectAegis.Delegation.UnityAdapter/Authoring/EditModeController.cs`
- Create: `src/ProjectAegis.Delegation.UnityAdapter/Authoring/LiveFindingsPresenter.cs`
- Create: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Authoring/EditModeControllerTests.cs`
- Create: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Authoring/LiveFindingsPresenterTests.cs`

- [ ] **Step 1: EditModeController tests**

Behaviors:

1. Default mode = `Edit` when session open for authoring.  
2. `TryEnterPlay` with dirty+error-severity findings → blocked (or requires explicit confirm flag).  
3. `TryEnterPlay` with clean findings → allowed; does **not** pass `editorState` into any validate call.  
4. Mode switch does not mutate ORBAT.

```csharp
public enum ScenarioHostMode { Edit, Play }

public sealed class EditModeController
{
    private readonly ScenarioAuthoringSession _session;
    private readonly LiveFindingsPresenter _findings;

    public EditModeController(ScenarioAuthoringSession session, LiveFindingsPresenter findings)
    {
        _session = session;
        _findings = findings;
        Mode = ScenarioHostMode.Edit;
    }

    public ScenarioHostMode Mode { get; private set; }

    public bool TryEnterPlay(bool forceConfirmInvalid = false)
    {
        _findings.RefreshImmediate();
        var hasErrors = _findings.HasErrorSeverity;
        if (hasErrors && !forceConfirmInvalid)
            return false;
        Mode = ScenarioHostMode.Play;
        return true;
    }

    public void EnterEdit() => Mode = ScenarioHostMode.Edit;
}
```

- [ ] **Step 2: LiveFindingsPresenter tests**

Behaviors:

1. `RefreshImmediate` codes match `editor.LiveValidate()` codes (same engine).  
2. After place + attach empty patrol, contains `MISSION_NO_UNITS`.  
3. Debounce: schedule refresh; without flush, `LastCodes` may be stale; `Flush` updates.  
4. For unit tests, inject `TimeProvider` or use `debounceMs: 0` constructor overload to avoid real timers.

```csharp
public sealed class LiveFindingsPresenter
{
    private readonly ScenarioAuthoringSession _session;
    private readonly int _debounceMs;
    // For tests: pass debounceMs: 0 so ScheduleRefresh == immediate.

    public LiveFindingsPresenter(ScenarioAuthoringSession session, int debounceMs = 300)
    {
        _session = session;
        _debounceMs = debounceMs;
    }

    public IReadOnlyList<string> LastCodes { get; private set; } = Array.Empty<string>();
    public ValidationReport? LastReport { get; private set; }
    public bool HasErrorSeverity =>
        LastReport?.Findings.Any(f => f.Severity == ValidationSeverity.Error) == true;

    public void RefreshImmediate()
    {
        LastReport = _session.Bus.RefreshFindings();
        LastCodes = LastReport.Findings
            .Select(f => f.Code)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();
    }

    public void ScheduleRefresh()
    {
        if (_debounceMs <= 0)
        {
            RefreshImmediate();
            return;
        }
        // Production: System.Threading.Timer or host-driven tick.
        // P2.1 minimum: document that Unity host calls RefreshImmediate on gesture-end
        // and optionally ScheduleRefresh with debounce in EditorWindow Update.
        RefreshImmediate();
    }
}
```

**Design open question #2 (debounce 200–400 ms):** default **300 ms** in presenter; host may call immediate on gesture-end (preferred for tests).

- [ ] **Step 3: Run tests PASS**

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "EditModeControllerTests|LiveFindingsPresenterTests" -v minimal
```

---

### Task 6: Attach mission from selection (inspector path)

**Files:**

- Extend: `ScenarioEditCommandBus` with `AttachStrikeFromSelection`, `AttachFerryFromSelection`, `AttachSupportFromSelection` (Patrol already in Task 3)
- Extend: `SelectionInspectorModel` or thin `MissionAttachHelper`
- Tests: `ScenarioEditCommandBusTests` + surface test “selected units feed assignedUnitIds”

- [ ] **Step 1: Test attach patrol with selected unit ids**

```csharp
[Fact]
public void Attach_patrol_uses_selected_unit_ids()
{
    var path = Path.Combine(Path.GetTempPath(), $"aegis-attach-{Guid.NewGuid():N}.json");
    try
    {
        ScenarioDocumentEditor.CreateNew().Save(path);
        using var session = ScenarioAuthoringSession.Open(path);
        session.Bus.PlaceUnit(1, new ScenarioOrbatUnitDto
        {
            Id = "u1", SideId = "blue", PlatformId = "ffg", Lat = 57, Lon = 20,
        }, save: true);

        var zone = new[]
        {
            new ScenarioWaypointDto { Lat = 57, Lon = 20 },
            new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
            new ScenarioWaypointDto { Lat = 57.2, Lon = 20.0 },
        };
        var r = session.Bus.AttachPatrolFromSelection(
            session.EditVersion, "patrol-1", new[] { "u1" }, zone, save: true);
        Assert.True(r.Ok);
        Assert.Equal("Patrol", session.Editor.Missions.Single().Type);
        Assert.Equal(new[] { "u1" }, session.Editor.Missions.Single().AssignedUnitIds);
    }
    finally
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
```

- [ ] **Step 2: Prefer inspector dropdown** (design open Q3) — document in scope boundary: attach type enum `Patrol|Strike|Ferry|Support` chosen in inspector, not radial menu.

- [ ] **Step 3: Run PASS + commit** (if instructed)

---

### Task 7: CLI / MCP parity verbs

**Files:**

- Create: `OrbatUpsertUnitCommand.cs`, `OrbatMoveUnitCommand.cs`, `OrbatCloneUnitCommand.cs`, `ReferencePointUpsertCommand.cs` under `src/ProjectAegis.MissionEditor.Cli/`
- Create: `OrbatReferencePointCliTests.cs`
- Modify: `Program.cs` verb routing
- Modify: MCP tools JSON / manifest consumed by `McpToolsManifestTests`

**Pattern:** Copy `MissionAddPatrolCommand` structure (load → RequireEditVersion → CaptureUndo → mutate → PersistUndo → CommitMutation → Save → WriteOk / CONFLICT).

Example verb names (kebab CLI, snake MCP):

| CLI | MCP tool |
|-----|----------|
| `orbat_upsert_unit` | `orbat_upsert_unit` |
| `orbat_move_unit` | `orbat_move_unit` |
| `orbat_clone_unit` | `orbat_clone_unit` |
| `reference_point_upsert` | `reference_point_upsert` |

Args (minimum):

```
orbat_upsert_unit --scenario PATH --edit-version N --id u1 --side blue --platform ffg --lat 57 --lon 20
orbat_move_unit --scenario PATH --edit-version N --id u1 --lat 58 --lon 21
orbat_clone_unit --scenario PATH --edit-version N --source u1 --id u2 --lat 57.01 --lon 20.01
reference_point_upsert --scenario PATH --edit-version N --id zone-1 --type polygon --latlon 57,20 --latlon 57.1,20.1 --latlon 57.2,20
```

- [ ] **Step 1: Failing CLI tests for OK + CONFLICT** (mirror `MissionUpdateSupportCommandTests` / patrol add)
- [ ] **Step 2: Implement commands + Program.cs cases**
- [ ] **Step 3: Update MCP manifest; run `McpToolsManifestTests`**
- [ ] **Step 4:**

```bash
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "OrbatReferencePointCliTests|McpToolsManifestTests" -v minimal
```

---

### Task 8: Integration — place + polygon + findings + CLI parity

**Files:**

- Create: `src/ProjectAegis.Data.Tests/Scenario/ScenarioMapAuthoringIntegrationTests.cs`
- Optional fixture: `data/scenarios/examples/map-authoring-min.scenario.json` (only if useful; prefer temp files to avoid freezing bad fixtures)

- [ ] **Step 1: Integration test (design AC §15.1–15.3)**

```csharp
[Fact]
public void Place_unit_and_patrol_polygon_persist_and_cli_validate_codes_match()
{
    // 1) Session place unit + upsert polygon RP + attach patrol with unit
    // 2) Save
    // 3) Reload via ScenarioDocumentJsonLoader — assert orbat + referencePoints present
    // 4) ValidationReport from ScenarioValidationEngine.Validate(dto, catalog, config)
    // 5) Assert codes equal editor.LiveValidate() codes (OrderBy ordinal)
    // 6) Optionally shell-out is NOT required; pure engine parity is enough for headless gate
}
```

Add second fact: unreachable strike produces `STRIKE_UNREACHABLE` or `STRIKE_UNREACHABLE_FUEL` in findings **without** CLI process (design AC §15.2–15.3). Reuse Baltic catalog fixture + known-bad coordinates from existing `ScenarioValidationEngineTests`.

- [ ] **Step 2: Derived-only pin**

Assert validation is unchanged if `EditorState` dictionary is present (reuse / extend `DerivedOnlyInvariantTests` if needed).

- [ ] **Step 3: Run**

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "ScenarioMapAuthoringIntegrationTests|DerivedOnly" -v minimal
```

---

### Task 9: Optional Unity EditorWindow glue (Approach A host)

**Files:**

- Create: `unity/ProjectAegis/Assets/Editor/ScenarioMapAuthoringWindow.cs` (+ `.meta` via Unity or minimal hand meta if project convention allows)

**Scope (thin):**

1. Menu: `Project Aegis/Scenario Map Authoring`  
2. Object field / path to `scenario.json`  
3. Buttons: Open, Save, Refresh Findings  
4. Lists: units, RPs, findings codes (IMGUI or UIToolkit)  
5. Calls **only** `ScenarioAuthoringSession` / bus / surface — **no** validation rules in Editor script  

**If Unity Editor is not available in the agent environment:** mark Task 9 **DEFERRED with checklist** in scope boundary; headless Tasks 1–8 still ship the slice’s testable product. Host evidence can be “headless surface + bus” + optional manual Editor checklist (same pattern as AC-8 productionize).

- [ ] **Step 1:** Implement only if Editor present; else write `production/qa/scenario-editor-p2-1-editor-host-checklist.md` with manual steps.

---

### Task 10: Scope boundary, design status, full gate

**Files:**

- Create: `production/scenario-editor-phase2-1-scope-boundary-2026-07-08.md`
- Modify: design spec status line → **Approved** (and **P2.1 implemented** when gate green)

Scope boundary contents:

1. In scope = Tasks 1–8 (required), 9 optional  
2. Out = Mission Board, event graph, NL, CMO import, Lua  
3. Invariants table  
4. Spike conclusions from Task 0  
5. Gate command block + expected floors  

- [ ] **Step 1: Full verification (RUN+READ)**

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter ReplayGolden
grep -r "17144800277401907079" tests/ data/ | head
git diff --name-only | grep -i DelegationBridge || true
node .gitnexus/run.cjs detect_changes 2>/dev/null || true
```

- [ ] **Step 2: Confirm**

- 0 build errors  
- Test count ≥1232  
- ReplayGolden 6/6  
- PlayModeSmoke ≥18  
- Hash present  
- No `DelegationBridge` production edits  

- [ ] **Step 3: Human handoff**

Ask user for commit/merge preference (Graphite `gt create` / stack vs direct). No unsolicited push.

---

## Parallelization guide

| Wave | Can parallelize | Serial dependency |
|------|-----------------|-------------------|
| A | Task 0 spike + Task 1 geometry | — |
| B | Task 2 editor mutations | after A (geometry optional parallel) |
| C | Task 3 bus/session | after Task 2 |
| D | Task 4 surface + Task 5 controller/findings | after Task 3 |
| E | Task 6 attach + Task 7 CLI | after Task 3 (CLI only needs Task 2) |
| F | Task 8 integration | after D+E |
| G | Task 9 host + Task 10 gate | after F |

**File ownership:** single owner for `ScenarioDocumentEditor.cs` per sprint/wave (CRITICAL hub).

---

## Spec coverage checklist (self-review)

| Design requirement | Task |
|--------------------|------|
| Load/save + dirty | Task 3 session |
| Place/move/clone units → ORBAT | Tasks 2, 4, 7 |
| Draw/edit RP polygon/circle/line; invalid stays marked | Tasks 1, 2, 4 |
| Selection + inspect | Task 4 |
| Attach mission from selection | Task 6 |
| Live Findings = same engine codes | Tasks 3, 5, 8 |
| editVersion + undo | Task 3 (reuses Capture/Persist) |
| CLI/MCP parity | Task 7 |
| editorState derived-only | Tasks 3, 8 |
| AC place+polygon in saved JSON | Task 8 |
| STRIKE_UNREACHABLE in findings | Task 8 |
| CLI validate same codes | Task 8 (+ optional CLI process) |
| ZERO DelegationBridge / hash / ReplayGolden | Task 10 |
| Mission Board / event graph out | Explicit non-touch + Task 10 boundary |
| Debounce 200–400 ms | Task 5 (300 default; 0 for tests) |
| Inspector attach vs radial | Task 6 (inspector) |

**Placeholder scan:** None intentional. Task 9 may defer EditorWindow with written checklist — not a silent TBD.

**Type consistency:**

- Methods: `UpsertOrbatUnit`, `MoveOrbatUnit`, `CloneOrbatUnit`, `UpsertReferencePoint`, `TryRemoveReferencePoint`
- Bus: `PlaceUnit`, `MoveUnit`, `CloneUnit`, `UpsertReferencePoint`, `AttachPatrolFromSelection`, `RefreshFindings`
- Presenter: `RefreshImmediate`, `ScheduleRefresh`, `LastCodes`, `HasErrorSeverity`
- Geometry: `ScenarioGeometryValidity.IsValid`

---

## Execution handoff

Plan complete and saved to `docs/superpowers/plans/2026-07-08-scenario-editor-phase2-1-plan.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — fresh subagent per task, review between tasks (`superpowers:subagent-driven-development`)  
2. **Inline Execution** — this session with checkpoints (`superpowers:executing-plans`)

Also create worktree first via `using-git-worktrees` if implementing.

**Which approach?**
