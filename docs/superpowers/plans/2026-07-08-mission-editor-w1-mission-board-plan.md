# ME-W1 Mission Board (P2.2 / AME-3.4) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.  
> **Parent program:** `docs/superpowers/plans/2026-07-08-mission-editor-phase2-completion-plan.md`  
> **Story:** `production/epics/mission-editor-phase2-completion/story-me-002-w1-mission-board.md`  
> **Worktree:** Prefer `.worktrees/me-w1/` (or per-track worktrees after Data lands).  
> **Collaboration:** No commits without user instruction unless user says execute+commit.

**Goal:** Ship a Mission Board that lists/filters missions, clones them, and creates Patrol/Strike/Support/Ferry from templates — all via `ScenarioDocumentEditor` + `ScenarioEditCommandBus`, with CLI/MCP parity and a headless presenter (optional Unity panel deferred).

**Architecture:** Pure query/template helpers over existing `ScenarioMissionDto` (no second schema). Mutations go through the command bus (editVersion, undo, live validate). Presenter is a thin view model in `UnityAdapter.Authoring`.

**Tech Stack:** .NET 8, ProjectAegis.Data, MissionEditor.Cli, UnityAdapter (no UnityEngine), xUnit (Data/Cli), NUnit (UA), Graphite, GitNexus.

---

## Standing invariants

| Invariant | Rule |
|-----------|------|
| Test floor | ≥1462 monotonic |
| ReplayGolden | 6/6 |
| Hash | `17144800277401907079` |
| PlayModeSmoke | ≥18 |
| `DelegationBridge.cs` | ZERO production hotpath |
| CatalogWriteGate | Extend-only |
| Stage | Release |

**Hub ownership:** Single owner for `ScenarioDocumentEditor.cs` / bus for Tasks 1–3. CLI and UA must not edit Editor concurrently.

**Pre-edit:**

```bash
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
```

---

## Reality check (codebase @ plan authoring)

| Fact | Implication |
|------|-------------|
| `Add*Mission` / `Update*Mission` / `TryRemoveMission` exist | Reuse — do not reimplement CRUD |
| Bus has `Attach*FromSelection` + `Mutate` pipeline | Add `CloneMission` / `AddFromTemplate` on bus |
| `ScenarioMissionDto` has **no** `SideId` or `Status` | Derive board fields: side from first ORBAT unit; status = `Assigned` if units.Count>0 else `Unassigned` |
| No `CloneMission` yet | Task 2 adds it |
| MCP list in `tools/mission-editor/mcp-tools.json` + `McpToolsManifestTests.RequiredCliVerbs` | Must add new verbs |

---

## File map

### Create (Data)

| Path | Responsibility |
|------|----------------|
| `src/ProjectAegis.Data/Scenario/Authoring/MissionBoardQuery.cs` | Filter/sort mission board rows from document DTO |
| `src/ProjectAegis.Data/Scenario/Authoring/MissionBoardRow.cs` | Row view DTO (id, type, sideId, status, unitCount, summary) |
| `src/ProjectAegis.Data/Scenario/Authoring/MissionTemplateCatalog.cs` | Built-in templates → mission construction args |
| `src/ProjectAegis.Data.Tests/Scenario/MissionBoardQueryTests.cs` | Filter/sort tests |
| `src/ProjectAegis.Data.Tests/Scenario/MissionTemplateCatalogTests.cs` | Template materialization |
| `src/ProjectAegis.Data.Tests/Scenario/ScenarioMissionCloneBusTests.cs` | Clone + template via bus |

### Modify (Data)

| Path | Change |
|------|--------|
| `ScenarioDocumentEditor.cs` | `CloneMission(sourceId, newId)`, optional `AddMissionFromDto` private helper |
| `ScenarioEditCommandBus.cs` | `CloneMission`, `AddFromTemplate`, `DeleteMission`, `ListBoard` (optional thin wrap) |

### Create (CLI)

| Path | Responsibility |
|------|----------------|
| `MissionListCommand.cs` | `mission_list` |
| `MissionCloneCommand.cs` | `mission_clone` |
| `MissionAddFromTemplateCommand.cs` | `mission_add_from_template` |
| `MissionListCliTests.cs` (or `MissionBoardCliTests.cs`) | OK + CONFLICT |

### Modify (CLI)

| Path | Change |
|------|--------|
| `Program.cs` | Wire 3 verbs + help |
| `tools/mission-editor/mcp-tools.json` | 3 tools + inputSchema |
| `McpToolsManifestTests.cs` | Add 3 names to `RequiredCliVerbs` |

### Create (UnityAdapter)

| Path | Responsibility |
|------|----------------|
| `Authoring/MissionBoardPresenter.cs` | Headless list + select + clone/template actions via session bus |
| `Authoring/MissionBoardPresenterTests.cs` | NUnit |

### Explicit non-touch

- `DelegationBridge.cs`
- Mission Board Unity UI panel (optional later; not required for W1 exit)
- Event graph (ME-W2)
- Mining/cargo types

---

## Locked API names

```csharp
// Query
public sealed class MissionBoardRow
{
    public string Id { get; init; } = "";
    public string Type { get; init; } = "";           // Patrol|Strike|Ferry|Support
    public string? SideId { get; init; }              // derived from first unit side, else null
    public string Status { get; init; } = "Unassigned"; // Assigned|Unassigned
    public int UnitCount { get; init; }
    public string SummaryLine { get; init; } = "";
}

public static class MissionBoardQuery
{
    public static IReadOnlyList<MissionBoardRow> List(
        ScenarioDocumentDto document,
        string? typeFilter = null,      // null = all; case-insensitive type match
        string? sideFilter = null,      // null = all; match row.SideId
        string? statusFilter = null);   // null = all; Assigned|Unassigned
}

// Templates
public sealed class MissionTemplateSpec
{
    public string TemplateId { get; init; } = "";
    public string Type { get; init; } = "";
    public string DisplayName { get; init; } = "";
}

public static class MissionTemplateCatalog
{
    public static IReadOnlyList<MissionTemplateSpec> All { get; }
    public static ScenarioMissionDto Materialize(string templateId, string newMissionId);
}

// Editor
public void CloneMission(string sourceMissionId, string newMissionId);

// Bus
public ScenarioMutationResult CloneMission(int expectedEditVersion, string sourceId, string newId, bool save);
public ScenarioMutationResult AddFromTemplate(int expectedEditVersion, string templateId, string newMissionId, bool save);
public ScenarioMutationResult DeleteMission(int expectedEditVersion, string missionId, bool save);
```

**Built-in templates (fixed ids for tests):**

| TemplateId | Type | Materialized defaults |
|------------|------|------------------------|
| `tpl-patrol-empty` | Patrol | 3-waypoint Baltic box; no units |
| `tpl-strike-empty` | Strike | empty targets; no units |
| `tpl-ferry-empty` | Ferry | destination `base-1`; no units |
| `tpl-support-tanker` | Support | role Tanker; 3-waypoint station; no units |

---

### Task 1: MissionBoardQuery (TDD)

**Files:**

- Create: `src/ProjectAegis.Data/Scenario/Authoring/MissionBoardRow.cs`
- Create: `src/ProjectAegis.Data/Scenario/Authoring/MissionBoardQuery.cs`
- Create: `src/ProjectAegis.Data.Tests/Scenario/MissionBoardQueryTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class MissionBoardQueryTests
{
    private static ScenarioDocumentDto DocWithMissions()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
        {
            Id = "u1", SideId = "blue", PlatformId = "ffg", Lat = 57, Lon = 20,
        });
        editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
        {
            Id = "u2", SideId = "red", PlatformId = "ddg", Lat = 58, Lon = 21,
        });
        editor.AddPatrolMission("patrol-1", ["u1"],
        [
            new ScenarioWaypointDto { Lat = 57, Lon = 20 },
            new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
            new ScenarioWaypointDto { Lat = 57.2, Lon = 20 },
        ]);
        editor.AddStrikeMission("strike-1", ["u2"], ["hostile-1"]);
        editor.AddFerryMission("ferry-empty", [], "base-1");
        return editor.ToDto();
    }

    [Fact]
    public void List_returns_all_missions_sorted_by_id_ordinal()
    {
        var rows = MissionBoardQuery.List(DocWithMissions());
        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { "ferry-empty", "patrol-1", "strike-1" }, rows.Select(r => r.Id).ToArray());
    }

    [Fact]
    public void List_filter_by_type_patrol()
    {
        var rows = MissionBoardQuery.List(DocWithMissions(), typeFilter: "Patrol");
        Assert.Single(rows);
        Assert.Equal("patrol-1", rows[0].Id);
        Assert.Equal("blue", rows[0].SideId);
        Assert.Equal("Assigned", rows[0].Status);
        Assert.Equal(1, rows[0].UnitCount);
    }

    [Fact]
    public void List_filter_by_side_red()
    {
        var rows = MissionBoardQuery.List(DocWithMissions(), sideFilter: "red");
        Assert.Single(rows);
        Assert.Equal("strike-1", rows[0].Id);
    }

    [Fact]
    public void List_filter_unassigned_status()
    {
        var rows = MissionBoardQuery.List(DocWithMissions(), statusFilter: "Unassigned");
        Assert.Single(rows);
        Assert.Equal("ferry-empty", rows[0].Id);
        Assert.Null(rows[0].SideId);
    }

    [Fact]
    public void Summary_line_includes_type_and_id()
    {
        var row = MissionBoardQuery.List(DocWithMissions(), typeFilter: "Strike").Single();
        Assert.Contains("strike-1", row.SummaryLine, StringComparison.Ordinal);
        Assert.Contains("Strike", row.SummaryLine, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Run — expect FAIL**

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter MissionBoardQueryTests -v minimal
```

Expected: missing types `MissionBoardQuery` / `MissionBoardRow`.

- [ ] **Step 3: Implement**

`MissionBoardRow.cs` — sealed class with properties above.

`MissionBoardQuery.cs`:

```csharp
namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>Pure Mission Board list/filter over canonical scenario document (AME-3.4).</summary>
public static class MissionBoardQuery
{
    public static IReadOnlyList<MissionBoardRow> List(
        ScenarioDocumentDto document,
        string? typeFilter = null,
        string? sideFilter = null,
        string? statusFilter = null)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));

        var unitSide = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in document.Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>())
        {
            if (!string.IsNullOrWhiteSpace(u.Id))
                unitSide[u.Id] = u.SideId ?? "";
        }

        IEnumerable<MissionBoardRow> rows = document.Missions.Select(m => ToRow(m, unitSide));

        if (!string.IsNullOrWhiteSpace(typeFilter))
            rows = rows.Where(r => string.Equals(r.Type, typeFilter, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(sideFilter))
            rows = rows.Where(r => string.Equals(r.SideId, sideFilter, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(statusFilter))
            rows = rows.Where(r => string.Equals(r.Status, statusFilter, StringComparison.OrdinalIgnoreCase));

        return rows.OrderBy(r => r.Id, StringComparer.Ordinal).ToArray();
    }

    private static MissionBoardRow ToRow(ScenarioMissionDto m, IReadOnlyDictionary<string, string> unitSide)
    {
        var units = m.AssignedUnitIds ?? Array.Empty<string>();
        string? side = null;
        foreach (var id in units)
        {
            if (unitSide.TryGetValue(id, out var s) && !string.IsNullOrEmpty(s))
            {
                side = s;
                break;
            }
        }

        var status = units.Count > 0 ? "Assigned" : "Unassigned";
        var type = m.Type ?? "";
        return new MissionBoardRow
        {
            Id = m.Id,
            Type = type,
            SideId = side,
            Status = status,
            UnitCount = units.Count,
            SummaryLine = $"{type} {m.Id} | units={units.Count} | {status}",
        };
    }
}
```

- [ ] **Step 4: Run — expect PASS**

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter MissionBoardQueryTests -v minimal
```

- [ ] **Step 5: Commit** (if instructed)

```bash
git commit -m "feat(editor): MissionBoardQuery list/filter for AME-3.4"
```

---

### Task 2: CloneMission on editor (TDD)

**Files:**

- Modify: `src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentEditor.cs`
- Create: `src/ProjectAegis.Data.Tests/Scenario/ScenarioMissionCloneEditorTests.cs`

- [ ] **Step 1: Failing tests**

```csharp
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class ScenarioMissionCloneEditorTests
{
    [Fact]
    public void CloneMission_copies_fields_under_new_id()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.AddPatrolMission("patrol-1", ["u1"],
        [
            new ScenarioWaypointDto { Lat = 57, Lon = 20 },
            new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
            new ScenarioWaypointDto { Lat = 57.2, Lon = 20 },
        ]);
        editor.CloneMission("patrol-1", "patrol-1-copy");

        Assert.Equal(2, editor.Missions.Count);
        var copy = editor.Missions.Single(m => m.Id == "patrol-1-copy");
        Assert.Equal("Patrol", copy.Type);
        Assert.Equal(new[] { "u1" }, copy.AssignedUnitIds);
        Assert.Equal(3, copy.PatrolZone.Count);
    }

    [Fact]
    public void CloneMission_duplicate_new_id_throws()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.AddStrikeMission("s1", ["u1"], ["t1"]);
        Assert.Throws<InvalidOperationException>(() => editor.CloneMission("s1", "s1"));
    }

    [Fact]
    public void CloneMission_missing_source_throws()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        Assert.Throws<InvalidOperationException>(() => editor.CloneMission("nope", "x"));
    }

    [Fact]
    public void CloneMission_strike_and_ferry_and_support()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.AddStrikeMission("s1", ["u1"], ["t1"]);
        editor.AddFerryMission("f1", ["u1"], "base-a");
        editor.AddSupportMission("sup1", ["u1"], "Tanker",
        [
            new ScenarioWaypointDto { Lat = 1, Lon = 1 },
            new ScenarioWaypointDto { Lat = 2, Lon = 2 },
            new ScenarioWaypointDto { Lat = 3, Lon = 3 },
        ]);
        editor.CloneMission("s1", "s2");
        editor.CloneMission("f1", "f2");
        editor.CloneMission("sup1", "sup2");
        Assert.Equal("Strike", editor.Missions.Single(m => m.Id == "s2").Type);
        Assert.Equal("base-a", editor.Missions.Single(m => m.Id == "f2").FerryDestinationBaseId);
        Assert.Equal("Tanker", editor.Missions.Single(m => m.Id == "sup2").SupportRole);
    }
}
```

- [ ] **Step 2: Run — FAIL (method missing)**

- [ ] **Step 3: Implement on `ScenarioDocumentEditor`**

```csharp
/// <summary>Deep-copies a mission under a new id (Mission Board clone). Does not CommitMutation.</summary>
public void CloneMission(string sourceMissionId, string newMissionId)
{
    if (string.IsNullOrWhiteSpace(newMissionId))
        throw new InvalidOperationException("New mission id is required.");
    if (Missions.Any(m => string.Equals(m.Id, newMissionId, StringComparison.OrdinalIgnoreCase)))
        throw new InvalidOperationException($"Mission id '{newMissionId}' already exists.");

    var src = Missions.FirstOrDefault(m =>
        string.Equals(m.Id, sourceMissionId, StringComparison.OrdinalIgnoreCase));
    if (src is null)
        throw new InvalidOperationException($"Mission id '{sourceMissionId}' was not found.");

    Missions.Add(new ScenarioMissionDto
    {
        Id = newMissionId,
        Type = src.Type,
        AssignedUnitIds = src.AssignedUnitIds.ToArray(),
        TargetIds = src.TargetIds.ToArray(),
        FerryDestinationBaseId = src.FerryDestinationBaseId,
        PatrolZone = src.PatrolZone
            .Select(w => new ScenarioWaypointDto { Lat = w.Lat, Lon = w.Lon })
            .ToArray(),
        StationGeometry = src.StationGeometry?
            .Select(w => new ScenarioWaypointDto { Lat = w.Lat, Lon = w.Lon })
            .ToArray(),
        SupportRole = src.SupportRole,
        RoeOverride = src.RoeOverride,
        EmconOverride = src.EmconOverride,
    });
}
```

- [ ] **Step 4: PASS + commit**

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter ScenarioMissionCloneEditorTests -v minimal
git commit -m "feat(editor): CloneMission for Mission Board"
```

---

### Task 3: MissionTemplateCatalog + editor add from template (TDD)

**Files:**

- Create: `MissionTemplateCatalog.cs`, `MissionTemplateSpec.cs` (can be same file)
- Modify: `ScenarioDocumentEditor.cs` — `AddMissionFromTemplate(string templateId, string newMissionId)`
- Tests: `MissionTemplateCatalogTests.cs`

- [ ] **Step 1: Tests**

```csharp
[Fact]
public void All_contains_four_builtin_templates()
{
    Assert.Equal(4, MissionTemplateCatalog.All.Count);
    Assert.Contains(MissionTemplateCatalog.All, t => t.TemplateId == "tpl-patrol-empty");
}

[Fact]
public void Materialize_patrol_has_three_waypoints_and_no_units()
{
    var m = MissionTemplateCatalog.Materialize("tpl-patrol-empty", "p-new");
    Assert.Equal("p-new", m.Id);
    Assert.Equal("Patrol", m.Type);
    Assert.Empty(m.AssignedUnitIds);
    Assert.Equal(3, m.PatrolZone.Count);
}

[Fact]
public void Materialize_unknown_throws()
{
    Assert.Throws<InvalidOperationException>(() =>
        MissionTemplateCatalog.Materialize("nope", "x"));
}

[Fact]
public void Editor_AddMissionFromTemplate_adds_and_rejects_duplicate()
{
    var editor = ScenarioDocumentEditor.CreateNew();
    editor.AddMissionFromTemplate("tpl-strike-empty", "s-new");
    Assert.Single(editor.Missions);
    Assert.Equal("Strike", editor.Missions[0].Type);
    Assert.Throws<InvalidOperationException>(() =>
        editor.AddMissionFromTemplate("tpl-strike-empty", "s-new"));
}
```

- [ ] **Step 2: Implement catalog**

```csharp
namespace ProjectAegis.Data.Scenario.Authoring;

public sealed class MissionTemplateSpec
{
    public string TemplateId { get; init; } = "";
    public string Type { get; init; } = "";
    public string DisplayName { get; init; } = "";
}

/// <summary>Built-in mission templates for Mission Board wizard (AME-3.4).</summary>
public static class MissionTemplateCatalog
{
    private static readonly ScenarioWaypointDto[] BalticBox =
    [
        new() { Lat = 57.0, Lon = 20.0 },
        new() { Lat = 57.1, Lon = 20.1 },
        new() { Lat = 57.2, Lon = 20.0 },
    ];

    public static IReadOnlyList<MissionTemplateSpec> All { get; } =
    [
        new() { TemplateId = "tpl-patrol-empty", Type = "Patrol", DisplayName = "Empty Patrol" },
        new() { TemplateId = "tpl-strike-empty", Type = "Strike", DisplayName = "Empty Strike" },
        new() { TemplateId = "tpl-ferry-empty", Type = "Ferry", DisplayName = "Empty Ferry" },
        new() { TemplateId = "tpl-support-tanker", Type = "Support", DisplayName = "Tanker Support" },
    ];

    public static ScenarioMissionDto Materialize(string templateId, string newMissionId)
    {
        if (string.IsNullOrWhiteSpace(newMissionId))
            throw new InvalidOperationException("Mission id is required.");

        return templateId switch
        {
            "tpl-patrol-empty" => new ScenarioMissionDto
            {
                Id = newMissionId, Type = "Patrol",
                AssignedUnitIds = Array.Empty<string>(),
                PatrolZone = BalticBox.ToArray(),
            },
            "tpl-strike-empty" => new ScenarioMissionDto
            {
                Id = newMissionId, Type = "Strike",
                AssignedUnitIds = Array.Empty<string>(),
                TargetIds = Array.Empty<string>(),
            },
            "tpl-ferry-empty" => new ScenarioMissionDto
            {
                Id = newMissionId, Type = "Ferry",
                AssignedUnitIds = Array.Empty<string>(),
                FerryDestinationBaseId = "base-1",
            },
            "tpl-support-tanker" => new ScenarioMissionDto
            {
                Id = newMissionId, Type = "Support",
                SupportRole = "Tanker",
                AssignedUnitIds = Array.Empty<string>(),
                PatrolZone = BalticBox.ToArray(),
                StationGeometry = BalticBox.ToArray(),
            },
            _ => throw new InvalidOperationException($"Unknown template id '{templateId}'."),
        };
    }
}
```

Editor method:

```csharp
public void AddMissionFromTemplate(string templateId, string newMissionId)
{
    if (Missions.Any(m => string.Equals(m.Id, newMissionId, StringComparison.OrdinalIgnoreCase)))
        throw new InvalidOperationException($"Mission id '{newMissionId}' already exists.");
    Missions.Add(MissionTemplateCatalog.Materialize(templateId, newMissionId));
}
```

- [ ] **Step 3: PASS + commit**

```bash
git commit -m "feat(editor): MissionTemplateCatalog and AddMissionFromTemplate"
```

---

### Task 4: Bus methods — clone / template / delete (TDD)

**Files:**

- Modify: `ScenarioEditCommandBus.cs`
- Create: `ScenarioMissionCloneBusTests.cs`

- [ ] **Step 1: Tests**

```csharp
[Fact]
public void Bus_CloneMission_bumps_edit_version_and_validates()
{
    var path = Path.Combine(Path.GetTempPath(), $"aegis-clone-{Guid.NewGuid():N}.json");
    try
    {
        ScenarioDocumentEditor.CreateNew().Save(path);
        using var session = ScenarioAuthoringSession.Open(path);
        session.Bus.AttachPatrolFromSelection(1, "p1", ["u1"],
        [
            new ScenarioWaypointDto { Lat = 57, Lon = 20 },
            new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
            new ScenarioWaypointDto { Lat = 57.2, Lon = 20 },
        ], save: true);

        var r = session.Bus.CloneMission(session.EditVersion, "p1", "p2", save: true);
        Assert.True(r.Ok);
        Assert.Equal(3, r.EditVersion); // create=1, attach=2, clone=3 if create starts at 1 and each commit +1
        // Adjust assertion to actual: after CreateNew Save editVersion=1; Attach bumps to 2; Clone to 3
        Assert.Equal(2, session.Editor.Missions.Count);
        Assert.NotNull(r.Report);
    }
    finally { if (File.Exists(path)) File.Delete(path); }
}

[Fact]
public void Bus_CloneMission_stale_edit_version_conflict()
{
    var path = Path.Combine(Path.GetTempPath(), $"aegis-clone-c-{Guid.NewGuid():N}.json");
    try
    {
        ScenarioDocumentEditor.CreateNew().Save(path);
        using var session = ScenarioAuthoringSession.Open(path);
        session.Bus.AttachPatrolFromSelection(1, "p1", ["u1"],
        [
            new ScenarioWaypointDto { Lat = 57, Lon = 20 },
            new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
            new ScenarioWaypointDto { Lat = 57.2, Lon = 20 },
        ], save: true);

        var r = session.Bus.CloneMission(99, "p1", "p2", save: true);
        Assert.False(r.Ok);
        Assert.Equal(ScenarioEditVersionGuard.ConflictCode, r.ErrorCode);
        Assert.Single(session.Editor.Missions);
    }
    finally { if (File.Exists(path)) File.Delete(path); }
}

[Fact]
public void Bus_AddFromTemplate_and_DeleteMission()
{
    var path = Path.Combine(Path.GetTempPath(), $"aegis-tpl-{Guid.NewGuid():N}.json");
    try
    {
        ScenarioDocumentEditor.CreateNew().Save(path);
        using var session = ScenarioAuthoringSession.Open(path);
        var add = session.Bus.AddFromTemplate(1, "tpl-patrol-empty", "p-tpl", save: true);
        Assert.True(add.Ok);
        Assert.Single(session.Editor.Missions);

        var del = session.Bus.DeleteMission(session.EditVersion, "p-tpl", save: true);
        Assert.True(del.Ok);
        Assert.Empty(session.Editor.Missions);
    }
    finally { if (File.Exists(path)) File.Delete(path); }
}

[Fact]
public void Bus_AddFromTemplate_live_findings_include_mission_no_units_for_empty_patrol()
{
    var path = Path.Combine(Path.GetTempPath(), $"aegis-tpl-v-{Guid.NewGuid():N}.json");
    try
    {
        ScenarioDocumentEditor.CreateNew().Save(path);
        using var session = ScenarioAuthoringSession.Open(path);
        var r = session.Bus.AddFromTemplate(1, "tpl-patrol-empty", "p-empty", save: true);
        Assert.True(r.Ok);
        Assert.Contains(r.Report!.Findings, f => f.Code == "MISSION_NO_UNITS");
    }
    finally { if (File.Exists(path)) File.Delete(path); }
}
```

**Note:** After `CreateNew().Save`, `EditVersion` is **1**. First `Attach`/`AddFromTemplate` with expected `1` → becomes `2`. Clone uses `session.EditVersion` after prior ops.

- [ ] **Step 2: Implement bus methods**

```csharp
public ScenarioMutationResult CloneMission(int expectedEditVersion, string sourceId, string newId, bool save)
    => Mutate(expectedEditVersion, save, e => e.CloneMission(sourceId, newId));

public ScenarioMutationResult AddFromTemplate(int expectedEditVersion, string templateId, string newMissionId, bool save)
    => Mutate(expectedEditVersion, save, e => e.AddMissionFromTemplate(templateId, newMissionId));

public ScenarioMutationResult DeleteMission(int expectedEditVersion, string missionId, bool save)
    => Mutate(expectedEditVersion, save, e =>
    {
        if (!e.TryRemoveMission(missionId))
            throw new InvalidOperationException($"Mission id '{missionId}' was not found.");
    });
```

- [ ] **Step 3: PASS + commit**

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "MissionBoardQueryTests|ScenarioMissionClone|MissionTemplate" -v minimal
git commit -m "feat(editor): bus CloneMission AddFromTemplate DeleteMission"
```

---

### Task 5: CLI + MCP parity (TDD)

**Files:**

- Create: `MissionListCommand.cs`, `MissionCloneCommand.cs`, `MissionAddFromTemplateCommand.cs`
- Create: `MissionBoardCliTests.cs`
- Modify: `Program.cs`, `mcp-tools.json`, `McpToolsManifestTests.cs`

**Verbs:**

| CLI | Args |
|-----|------|
| `mission_list` | `--path` optional `--type` `--side` `--status` (read-only, no editVersion) |
| `mission_clone` | `--path --edit-version --source --id` |
| `mission_add_from_template` | `--path --edit-version --template --id` |

- [ ] **Step 1: CLI tests** (pattern from `OrbatReferencePointCliTests` / `MissionAddPatrolCommand`)

```csharp
[Fact]
public void Mission_list_returns_rows_json_ok()
{
    // create temp scenario with one patrol via editor, Run MissionListCommand, assert exit 0 and body contains mission id
}

[Fact]
public void Mission_clone_ok_bumps_edit_version()
{
    // add patrol editVersion 1→2 via existing add command or editor
    // clone with edit-version current → 2 missions, editVersion +1
}

[Fact]
public void Mission_clone_stale_returns_conflict()
{
    // edit-version 99 → CONFLICT, no second mission
}

[Fact]
public void Mission_add_from_template_ok()
{
    // template tpl-patrol-empty → mission present
}
```

- [ ] **Step 2: Implement commands**

`MissionListCommand` — load DTO (no mutation), `MissionBoardQuery.List`, `WriteOk` with `{ ok, missions: [ { id, type, sideId, status, unitCount, summaryLine } ] }`.

`MissionCloneCommand` — same as other mutators: Load → RequireEditVersion → CaptureUndo → CloneMission → PersistUndo → Commit → Save.

`MissionAddFromTemplateCommand` — same with `AddMissionFromTemplate`.

- [ ] **Step 3: Wire Program.cs**

```csharp
case "mission_list": return RunMissionList(args.Skip(1).ToArray());
case "mission_clone": return RunMissionClone(args.Skip(1).ToArray());
case "mission_add_from_template": return RunMissionAddFromTemplate(args.Skip(1).ToArray());
```

Parse flags with `CliArgParser` like other missions.

- [ ] **Step 4: MCP manifest**

Add three tool entries (copy `mission_delete` structure). Add names to `RequiredCliVerbs`.

- [ ] **Step 5: PASS**

```bash
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "MissionBoardCliTests|McpToolsManifestTests" -v minimal
git commit -m "feat(editor): CLI/MCP mission_list clone add_from_template"
```

---

### Task 6: Headless MissionBoardPresenter (TDD)

**Files:**

- Create: `src/ProjectAegis.Delegation.UnityAdapter/Authoring/MissionBoardPresenter.cs`
- Create: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Authoring/MissionBoardPresenterTests.cs` (**NUnit**)

- [ ] **Step 1: Tests**

```csharp
[Test]
public void Refresh_lists_missions_from_session()
{
    // Open session, AddFromTemplate, presenter.Refresh(), assert Rows.Count == 1
}

[Test]
public void Filter_type_patrol_only()
{
    // two types, SetTypeFilter("Patrol"), Refresh, single row
}

[Test]
public void Clone_selected_updates_rows_and_findings()
{
    // select id, CloneSelected("new-id"), rows include both; findings presenter refreshed if wired
}

[Test]
public void Add_from_template_refresh_findings()
{
    // AddFromTemplate via presenter → LastFindings codes non-null
}
```

- [ ] **Step 2: Implement**

```csharp
namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;

public sealed class MissionBoardPresenter
{
    private readonly ScenarioAuthoringSession _session;
    private readonly LiveFindingsPresenter _findings;

    public MissionBoardPresenter(ScenarioAuthoringSession session, LiveFindingsPresenter findings)
    {
        _session = session;
        _findings = findings;
    }

    public string? TypeFilter { get; set; }
    public string? SideFilter { get; set; }
    public string? StatusFilter { get; set; }
    public string? SelectedMissionId { get; private set; }
    public IReadOnlyList<MissionBoardRow> Rows { get; private set; } = Array.Empty<MissionBoardRow>();

    public void Refresh()
    {
        Rows = MissionBoardQuery.List(_session.Editor.ToDto(), TypeFilter, SideFilter, StatusFilter);
    }

    public void Select(string? missionId) => SelectedMissionId = missionId;

    public ScenarioMutationResult? CloneSelected(string newMissionId, bool save = true)
    {
        if (string.IsNullOrWhiteSpace(SelectedMissionId)) return null;
        var r = _session.Bus.CloneMission(_session.EditVersion, SelectedMissionId, newMissionId, save);
        if (r.Ok)
        {
            Refresh();
            _findings.RefreshImmediate();
        }
        return r;
    }

    public ScenarioMutationResult AddFromTemplate(string templateId, string newMissionId, bool save = true)
    {
        var r = _session.Bus.AddFromTemplate(_session.EditVersion, templateId, newMissionId, save);
        if (r.Ok)
        {
            Refresh();
            _findings.RefreshImmediate();
        }
        return r;
    }
}
```

- [ ] **Step 3: PASS**

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter MissionBoardPresenterTests -v minimal
git commit -m "feat(editor): headless MissionBoardPresenter"
```

---

### Task 7: Optional Unity panel (DEFER by default)

**Files (only if capacity / Unity present):**

- `unity/ProjectAegis/Assets/Editor/ScenarioMissionBoardWindow.cs` — IMGUI list + clone/template buttons calling presenter (same pattern as `ScenarioMapAuthoringWindow`).

**W1 exit does NOT require this.** If skipped: note in story ME-002 completion notes “Unity board panel deferred; headless presenter + CLI ship AME-3.4 backend.”

---

### Task 8: Integration + story/docs + gate

**Files:**

- Create: `src/ProjectAegis.Data.Tests/Scenario/MissionBoardIntegrationTests.cs`
- Modify: `story-me-002-w1-mission-board.md` status
- Optional: doc 11 AME-3.4 maturity → Partial+ / In progress

- [ ] **Step 1: Integration test**

```csharp
[Fact]
public void Board_list_clone_template_cli_roundtrip_parity()
{
    // Session: template patrol → clone → query filter Patrol count 2
    // LiveValidate after template has MISSION_NO_UNITS
    // Reload file: missions persist
}
```

- [ ] **Step 2: Full verification**

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/... --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/... --filter ReplayGolden
# hash + bridge grep
```

- [ ] **Step 3: Commit closeout**

```bash
git commit -m "test(editor): Mission Board integration; mark ME-W1 story complete"
```

---

## Parallelization after Task 4

| Track | Tasks | Depends on |
|-------|-------|------------|
| Data (serial owner) | 1 → 2 → 3 → 4 | — |
| CLI | 5 | Task 4 merged |
| UA presenter | 6 | Task 4 merged |
| Integration | 8 | 5+6 |

Dispatch CLI and UA **in parallel** after Data lands on the integration branch.

---

## Spec coverage (story ME-002)

| Acceptance | Task |
|------------|------|
| Filter/sort by type/side/status | Task 1 |
| Clone + editVersion + undo | Tasks 2, 4 |
| Template creates 4 types | Tasks 3, 4 |
| CLI/MCP parity | Task 5 |
| Headless MissionBoardPresenter | Task 6 |
| Live findings after mutations | Tasks 4, 6 |
| Optional Unity panel | Task 7 deferred |

**Placeholder scan:** None intentional. Task 7 is explicit defer.

**Type consistency:** `CloneMission`, `AddFromTemplate`, `DeleteMission`, `MissionBoardQuery.List`, template ids `tpl-*` fixed.

---

## Execution handoff

Plan complete and saved to `docs/superpowers/plans/2026-07-08-mission-editor-w1-mission-board-plan.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — fresh subagent per task + review  
2. **Inline Execution** — this session with checkpoints  

Also create worktree `.worktrees/me-w1/` before coding.

**Which approach?**
