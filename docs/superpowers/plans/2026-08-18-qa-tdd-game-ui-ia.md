# QA TDD Game UI IA Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Land headless `UiIa` oracles for C2 selection sync, COMMS triad, planning gates, and PanelSettings-null, then wire them into `qa-gauntlet-ui`.

**Architecture:** New NUnit classes in `ProjectAegis.Delegation.UnityAdapter.Tests` named `*UiIa*`. Binder/projection tests use seeded `BalticReplayHarness`; source contracts read Unity Runtime `.cs` / USS. Product edits are presentation-only (`C2TopBarPanelHost`, `C2MenuPanelHost` + USS). Skill packaging is last.

**Tech Stack:** .NET 8.0.400, NUnit 4, Unity Runtime C# (`#if UNITY_5_3_OR_NEWER` hosts), `qa-gauntlet-ui` skill Markdown.

**Spec:** `docs/superpowers/specs/2026-08-18-qa-tdd-game-ui-ia-design.md`

## Global Constraints

- Zero-touch `src/ProjectAegis.Delegation.UnityAdapter/Baltic/DelegationBridge.cs` (and any `DelegationBridge.cs`).
- Zero CatalogWriteGate write-path edits.
- Baltic v2 hash `17144800277401907079` unchanged.
- New IA test **types** must include `UiIa` in the class name. Do not add new IA assertions only into pre-existing `*ContractTests` files.
- No UI Toolkit instantiation in headless tests.
- Do not mix binder/projection asserts and source-contract asserts in the same `[Test]` method.
- GitNexus `impact({target, direction:"upstream"})` before editing an existing C# symbol; `detect_changes()` before each commit.
- `export PATH="$HOME/.dotnet:$PATH"` before every `dotnet` command. Run from `/home/username01/cmano-clone`.
- Play Mode signoffs ×5 and Demo ladder are out of scope.
- Manual UAT stays on `/team-qa` / `/smoke-check`.

## File map

| File | Responsibility |
|------|----------------|
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaSourceReader.cs` | Shared repo-root + file read for source contracts |
| `.../UiIaSelectionSyncOracleTests.cs` | Selection binder oracle + host source contracts |
| `.../UiIaCommsTriadOracleTests.cs` | COMMS triad source contracts |
| `.../UiIaPlanningGateOracleTests.cs` | Planning gate source contracts (menu + locked hosts) |
| `.../UiIaPanelSettingsOracleTests.cs` | PanelSettings bootstrap / scene-builder / no-Tick |
| `unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs` | Bind comms label from `LastCommsState` |
| `unity/ProjectAegis/Assets/Scripts/Runtime/C2MenuPanelHost.cs` | Planning read-only class + click guards |
| `unity/ProjectAegis/Assets/UI/C2Menu/C2MenuPanel.uss` | `.c2-menu-panel--planning-readonly` |
| `.claude/skills/qa-gauntlet-ui/SKILL.md` | `FullyQualifiedName~UiIa` + AAR IA row |
| `.claude/skills/team-qa-gauntlet/SKILL.md` | UI-mode summary mentions IA oracles |

---

### Task 1: Branch, spec status, shared reader

**Files:**
- Modify: `docs/superpowers/specs/2026-08-18-qa-tdd-game-ui-ia-design.md` (Status line only)
- Create: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaSourceReader.cs`
- Already on disk: this plan file

**Interfaces:**
- Consumes: none
- Produces: `internal static class UiIaSourceReader` with `RequireRepoRoot()`, `ReadRuntime(string fileName)`, `ReadUnder(params string[] relativeParts)`

- [ ] **Step 1: Create the Graphite branch**

```bash
cd /home/username01/cmano-clone
gt create qa/ui-ia-tdd -m "test(ui): start UiIa oracles for C2 information architecture"
```

Expected: current branch is `qa/ui-ia-tdd` (or Graphite equivalent). If the working tree is dirty with unrelated files, do **not** stage them.

- [ ] **Step 2: Mark the spec approved**

In `docs/superpowers/specs/2026-08-18-qa-tdd-game-ui-ia-design.md`, change:

`**Status:** Draft (design approved in chat; pending spec review)`

to:

`**Status:** Approved (user, 2026-08-18)`

- [ ] **Step 3: Write `UiIaSourceReader.cs`**

Create `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaSourceReader.cs`:

```csharp
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>Shared repo-root file reads for UiIa source contracts (no UI Toolkit).</summary>
internal static class UiIaSourceReader
{
    public static string RequireRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "unity", "ProjectAegis")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        Assert.Fail("Could not locate repo root (missing unity/ProjectAegis).");
        return string.Empty;
    }

    public static string ReadRuntime(string fileName)
    {
        var path = Path.Combine(
            RequireRepoRoot(),
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            fileName);
        Assert.That(File.Exists(path), Is.True, path);
        return File.ReadAllText(path);
    }

    public static string ReadUnder(params string[] relativeParts)
    {
        var parts = new List<string> { RequireRepoRoot() };
        parts.AddRange(relativeParts);
        var path = Path.Combine(parts.ToArray());
        Assert.That(File.Exists(path), Is.True, path);
        return File.ReadAllText(path);
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add docs/superpowers/specs/2026-08-18-qa-tdd-game-ui-ia-design.md \
  docs/superpowers/plans/2026-08-18-qa-tdd-game-ui-ia.md \
  src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaSourceReader.cs
git commit -m "$(cat <<'EOF'
docs(qa): approve UiIa spec and add source-contract reader

EOF
)"
```

---

### Task 2: Selection sync oracles (`UiIa`)

**Files:**
- Create: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaSelectionSyncOracleTests.cs`
- Test: same file (NUnit)

**Interfaces:**
- Consumes: `UiIaSourceReader.ReadRuntime`, `BalticReplayHarness.Run`, `MapPanelBinder.Bind`, `OobTreePanelBinder.Bind`, `ContactSummaryProjection.Project`, `SensorC2PanelBinder.Bind`, `C2SelectionResolver`, `MapPictureProjection`
- Produces: class `UiIaSelectionSyncOracleTests` (must match `FullyQualifiedName~UiIa`)

This family is mostly characterization of existing binds. Write the tests first. If they pass on first run, **do not weaken assertions**. If they fail, fix **hosts only** (not `DelegationBridge`).

- [ ] **Step 1: Write the failing / characterizing tests**

Create `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaSelectionSyncOracleTests.cs`:

```csharp
namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class UiIaSelectionSyncOracleTests
{
    [Test]
    public void Classify_binders_share_one_hostile_contact_id_across_map_oob_summary_and_sensor()
    {
        var result = BalticReplayHarness.Run(7, "baltic-patrol-classify", ticks: 10, mvpEngagement: false);
        var oob = new[] { new OobTreeEntry("u1", true) };
        var defaultUnit = C2SelectionResolver.ResolveDefaultFriendlyUnit(oob);
        var symbols = MapPictureProjection.Project(oob, result.SensorC2.Contacts, layoutSeed: 7);

        var mapDefault = MapPanelBinder.Bind(symbols, "baltic-patrol-classify", defaultUnit, null);
        var oobDefault = OobTreePanelBinder.Bind(oob, defaultUnit);
        Assert.That(mapDefault.Symbols.Single(s => s.SymbolId == defaultUnit).IsSelected, Is.True);
        Assert.That(oobDefault.UnitRows.Single(r => r.UnitId == defaultUnit).IsSelected, Is.True);

        var hostile = symbols.First(s => s.Affiliation == "Hostile");
        Assert.That(
            C2SelectionResolver.TryResolveHostileContactFromSymbol(hostile.SymbolId, symbols, out var contactId),
            Is.True);

        var summary = ContactSummaryProjection.Project(contactId, result.SensorC2.Contacts);
        Assert.That(summary, Is.Not.Null);
        Assert.That(summary!.DisplayLine, Does.Contain("CONTACT"));

        var mapContact = MapPanelBinder.Bind(symbols, "baltic-patrol-classify", null, contactId);
        Assert.That(mapContact.Symbols.Single(s => s.SymbolId == contactId).IsSelected, Is.True);

        var sensor = SensorC2PanelBinder.Bind(result.SensorC2);
        Assert.That(sensor.ContactRows.Any(r => r.ContactId == contactId), Is.True);
    }

    [Test]
    public void Selection_hosts_bind_bridge_ids_and_do_not_keep_a_second_selection_store()
    {
        var map = UiIaSourceReader.ReadRuntime("MapPlaceholderPanelHost.cs");
        Assert.That(map, Does.Contain("SelectedContactId"));
        Assert.That(map, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(map, Does.Not.Contain("DelegationBridge.Tick"));

        var oob = UiIaSourceReader.ReadRuntime("OobTreePanelHost.cs");
        Assert.That(oob, Does.Contain("OobTreePanelBinder.Bind"));
        Assert.That(oob, Does.Contain("SelectedUnitId"));
        Assert.That(oob, Does.Not.Contain("private string? _selected"));

        var contact = UiIaSourceReader.ReadRuntime("ContactDetailPanelHost.cs");
        Assert.That(contact, Does.Contain("bridgeHost.SelectedContactId"));
        Assert.That(contact, Does.Not.Contain("private string? _selected"));

        var unit = UiIaSourceReader.ReadRuntime("RightUnitPanelHost.cs");
        Assert.That(unit, Does.Contain("UnitDetailPanelBinder.Bind"));
        Assert.That(unit, Does.Contain("LastUnitDetail"));
        Assert.That(unit, Does.Not.Contain("private string? _selected"));

        var sensor = UiIaSourceReader.ReadRuntime("SensorC2PanelHost.cs");
        Assert.That(sensor, Does.Contain("LastSensorC2"));
        Assert.That(sensor, Does.Contain("SensorC2Bridge.BindPanel"));
        Assert.That(sensor, Does.Not.Contain("private string? _selected"));

        var drawer = UiIaSourceReader.ReadRuntime("C2LeftDrawerPanelHost.cs");
        Assert.That(drawer, Does.Contain("SelectContact"));
        Assert.That(drawer, Does.Contain("SelectedUnitId"));
        Assert.That(drawer, Does.Not.Contain("CatalogWriteGate"));
    }
}
```

- [ ] **Step 2: Run tests**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter FullyQualifiedName~UiIaSelectionSyncOracleTests -v n --nologo
```

Expected: **2 passed**. If a host assertion fails, GitNexus `impact` on that host class (upstream) then add the missing bind token — do not invent a second selection field.

- [ ] **Step 3: Commit**

```bash
git add src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaSelectionSyncOracleTests.cs
git commit -m "$(cat <<'EOF'
test(ui): add UiIa selection-sync oracles

EOF
)"
```

---

### Task 3: COMMS triad — RED then GREEN on top bar

**Files:**
- Create: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaCommsTriadOracleTests.cs`
- Modify: `unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs` (`Refresh` only; overlay `LastCommsState`)

**Interfaces:**
- Consumes: `UiIaSourceReader.ReadRuntime`; `DelegationBridgeHost.LastCommsState` (`CommsStateSnapshot.TopBarLabel`); `C2TopBarApplyState.ResolveCommsCssClass`
- Produces: class `UiIaCommsTriadOracleTests`; top bar comms text sourced from `LastCommsState` not a host-local `CommsStateProjection.Project`

Today `C2TopBarPanelHost.Refresh` applies `bridgeHost.LastTopBar` and never mentions `LastCommsState`. The spec locks the triad to map + top bar + feed. This task makes the top-bar test fail, then overlays `LastCommsState`.

- [ ] **Step 1: GitNexus impact (before any host edit)**

`impact({target: "C2TopBarPanelHost", direction: "upstream"})`

Expected: presentation/Unity Runtime callers only. HIGH/CRITICAL → stop. Record risk in the commit body.

- [ ] **Step 2: Write the failing tests**

Create `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaCommsTriadOracleTests.cs`:

```csharp
namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using NUnit.Framework;

[TestFixture]
public sealed class UiIaCommsTriadOracleTests
{
    [Test]
    public void Map_host_binds_LastCommsState_and_does_not_reproject()
    {
        var text = UiIaSourceReader.ReadRuntime("MapPlaceholderPanelHost.cs");
        Assert.That(text, Does.Contain("LastCommsState"));
        Assert.That(text, Does.Not.Contain("CommsStateProjection.Project"));
        Assert.That(text, Does.Not.Contain("DelegationBridge.Tick"));
    }

    [Test]
    public void Top_bar_binds_LastCommsState_and_does_not_reproject()
    {
        var text = UiIaSourceReader.ReadRuntime("C2TopBarPanelHost.cs");
        Assert.That(text, Does.Contain("LastCommsState"));
        Assert.That(text, Does.Contain("TopBarLabel"));
        Assert.That(text, Does.Not.Contain("CommsStateProjection.Project"));
        Assert.That(text, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(text, Does.Not.Contain("CatalogWriteGate"));
    }

    [Test]
    public void Bridge_host_exposes_LastCommsState_on_the_presentation_feed()
    {
        var text = UiIaSourceReader.ReadRuntime("DelegationBridgeHost.cs");
        Assert.That(text, Does.Contain("LastCommsState"));
        Assert.That(text, Does.Contain("CommsStateProjection.Project"));
        Assert.That(text, Does.Not.Contain("CatalogWriteGate"));
    }
}
```

- [ ] **Step 3: Run to verify RED**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter FullyQualifiedName~UiIaCommsTriadOracleTests -v n --nologo
```

Expected: `Top_bar_binds_LastCommsState_and_does_not_reproject` **FAIL** (missing `LastCommsState` / `TopBarLabel` in `C2TopBarPanelHost.cs`). Map and bridge-host tests **PASS**.

- [ ] **Step 4: Minimal host fix**

In `C2TopBarPanelHost.Refresh`, after applying `LastTopBar` and the live phase overlay, overlay comms from `LastCommsState`. Replace the `_presentation = C2TopBarApplyState.Apply(state) with { PhaseLabel = ... }` block with:

```csharp
            var state = bridgeHost.LastTopBar;
            var applied = C2TopBarApplyState.Apply(state) with
            {
                PhaseLabel = $"PHASE: {bridgeHost.Phase}",
            };
            var comms = bridgeHost.LastCommsState;
            if (comms != null)
            {
                applied = applied with
                {
                    CommsLabel = comms.TopBarLabel,
                    CommsCssClass = C2TopBarApplyState.ResolveCommsCssClass(comms.TopBarLabel),
                };
            }

            _presentation = applied;
```

Do **not** edit `DelegationBridgeHost` Tick / project methods. Do **not** add `CommsStateProjection.Project` to the top bar.

- [ ] **Step 5: Run to verify GREEN**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter FullyQualifiedName~UiIaCommsTriadOracleTests -v n --nologo
```

Expected: **3 passed**.

- [ ] **Step 6: Commit**

```bash
git add src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaCommsTriadOracleTests.cs \
  unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs
git commit -m "$(cat <<'EOF'
fix(ui): bind C2 top-bar COMMS from LastCommsState

EOF
)"
```

---

### Task 4: Planning-phase gate — RED then GREEN on C2 menu

**Files:**
- Create: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaPlanningGateOracleTests.cs`
- Modify: `unity/ProjectAegis/Assets/Scripts/Runtime/C2MenuPanelHost.cs`
- Modify: `unity/ProjectAegis/Assets/UI/C2Menu/C2MenuPanel.uss`

**Interfaces:**
- Consumes: `C2PlanningChromeProjection.Project(SimulationPhase)`; `DelegationBridgeHost.Phase`; USS class `c2-menu-panel--planning-readonly`
- Produces: class `UiIaPlanningGateOracleTests`; menu host applies planning class and ignores layer/unit clicks while `IsDrawerReadOnly`

Locked hosts from spec: MapPlaceholder and left drawer already have planning contracts in `C2PlanningChromeTests`. This `UiIa` file **re-asserts** those tokens (allowed — new type name) and adds the missing menu host.

- [ ] **Step 1: GitNexus impact**

`impact({target: "C2MenuPanelHost", direction: "upstream"})`

Expected: Unity Runtime / smoke builder only. HIGH/CRITICAL → stop.

- [ ] **Step 2: Write the failing tests**

Create `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaPlanningGateOracleTests.cs`:

```csharp
namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using NUnit.Framework;

[TestFixture]
public sealed class UiIaPlanningGateOracleTests
{
    [Test]
    public void Map_placeholder_binds_planning_dim_class()
    {
        var host = UiIaSourceReader.ReadRuntime("MapPlaceholderPanelHost.cs");
        Assert.That(host, Does.Contain("C2PlanningChromeProjection.Project"));
        Assert.That(host, Does.Contain("map-placeholder-panel--planning-dimmed"));
    }

    [Test]
    public void Left_drawer_binds_planning_readonly_class()
    {
        var host = UiIaSourceReader.ReadRuntime("C2LeftDrawerPanelHost.cs");
        Assert.That(host, Does.Contain("C2PlanningChromeProjection.Project"));
        Assert.That(host, Does.Contain("IsDrawerReadOnly"));
        Assert.That(host, Does.Contain("c2-drawer-panel--planning-readonly"));
    }

    [Test]
    public void Menu_host_applies_planning_readonly_and_guards_clicks()
    {
        var host = UiIaSourceReader.ReadRuntime("C2MenuPanelHost.cs");
        Assert.That(host, Does.Contain("C2PlanningChromeProjection.Project"));
        Assert.That(host, Does.Contain("IsDrawerReadOnly"));
        Assert.That(host, Does.Contain("c2-menu-panel--planning-readonly"));
        Assert.That(host, Does.Contain("OnLayerRowClicked"));
        Assert.That(host, Does.Contain("OnCycleUnitClicked"));
        Assert.That(host, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(host, Does.Not.Contain("CatalogWriteGate"));

        var uss = UiIaSourceReader.ReadUnder(
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "C2Menu",
            "C2MenuPanel.uss");
        Assert.That(uss, Does.Contain(".c2-menu-panel--planning-readonly"));
    }
}
```

- [ ] **Step 3: Run to verify RED**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter FullyQualifiedName~UiIaPlanningGateOracleTests -v n --nologo
```

Expected: map + drawer **PASS**; `Menu_host_applies_planning_readonly_and_guards_clicks` **FAIL**.

- [ ] **Step 4: USS**

Append to `unity/ProjectAegis/Assets/UI/C2Menu/C2MenuPanel.uss`:

```css
.c2-menu-panel--planning-readonly {
    opacity: 0.72;
}
```

- [ ] **Step 5: Host — apply class on refresh; guard clicks**

In `C2MenuPanelHost.cs`:

1. At the start of `Refresh(bool force)`, after the `if (!_wired) return;` block, call `ApplyPlanningChrome();` (including on the early dirty-flag return — phase can change without layer dirty).

2. Add:

```csharp
        private void ApplyPlanningChrome()
        {
            var panel = _document.rootVisualElement?.Q(RootName) ?? _document.rootVisualElement;
            if (panel == null)
            {
                return;
            }

            var readOnly = bridgeHost != null
                && C2PlanningChromeProjection.Project(bridgeHost.Phase).IsDrawerReadOnly;
            panel.EnableInClassList("c2-menu-panel--planning-readonly", readOnly);
        }

        private bool IsPlanningReadOnly =>
            bridgeHost != null
            && C2PlanningChromeProjection.Project(bridgeHost.Phase).IsDrawerReadOnly;
```

3. First lines of `OnLayerRowClicked` and `OnCycleUnitClicked`:

```csharp
            if (IsPlanningReadOnly)
            {
                SetStatus("PLANNING_READONLY");
                return;
            }
```

`C2PlanningChromeProjection` is already in `ProjectAegis.Delegation.Projection` (file already `using` that namespace). Do not add a second projection type.

- [ ] **Step 6: Run to verify GREEN**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter FullyQualifiedName~UiIaPlanningGateOracleTests -v n --nologo
```

Expected: **3 passed**.

- [ ] **Step 7: Commit**

```bash
git add src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaPlanningGateOracleTests.cs \
  unity/ProjectAegis/Assets/Scripts/Runtime/C2MenuPanelHost.cs \
  unity/ProjectAegis/Assets/UI/C2Menu/C2MenuPanel.uss
git commit -m "$(cat <<'EOF'
fix(ui): gate C2 menu while planning chrome is read-only

EOF
)"
```

---

### Task 5: PanelSettings-null oracle

**Files:**
- Create: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaPanelSettingsOracleTests.cs`

**Interfaces:**
- Consumes: `UiDocumentPanelSettingsBootstrap` source; `DelegationSmokeSceneBuilder` source; participating hosts call `EnsureDocument`
- Produces: class `UiIaPanelSettingsOracleTests`

Product intent (existing bootstrap): null `panelSettings` is **assigned** shared settings so Game view is not an empty skybox. Oracle: bootstrap exists, assigns, does **not** Tick / BeginExecution. Do not change bootstrap behavior.

- [ ] **Step 1: Write tests**

Create `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaPanelSettingsOracleTests.cs`:

```csharp
namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using NUnit.Framework;

[TestFixture]
public sealed class UiIaPanelSettingsOracleTests
{
    [Test]
    public void Bootstrap_assigns_PanelSettings_and_does_not_tick_sim()
    {
        var text = UiIaSourceReader.ReadRuntime("UiDocumentPanelSettingsBootstrap.cs");
        Assert.That(text, Does.Contain("EnsureDocument"));
        Assert.That(text, Does.Contain("SharedRuntimeSettings"));
        Assert.That(text, Does.Contain("panelSettings"));
        Assert.That(text, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(text, Does.Not.Contain("BeginExecution"));
        Assert.That(text, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(text, Does.Not.Contain("SimulationSession"));
    }

    [Test]
    public void Scene_builder_has_FixPanelSettings_path()
    {
        var text = UiIaSourceReader.ReadUnder(
            "unity",
            "ProjectAegis",
            "Assets",
            "Editor",
            "DelegationSmokeSceneBuilder.cs");
        Assert.That(text, Does.Contain("FixPanelSettings"));
        Assert.That(text, Does.Contain("EnsurePanelSettingsAsset"));
        Assert.That(text, Does.Contain("DefaultPanelSettingsAssetPath"));
        Assert.That(text, Does.Not.Contain("CatalogWriteGate"));
    }

    [Test]
    public void Participating_hosts_call_EnsureDocument()
    {
        string[] hosts =
        {
            "MapPlaceholderPanelHost.cs",
            "OobTreePanelHost.cs",
            "ContactDetailPanelHost.cs",
            "RightUnitPanelHost.cs",
            "SensorC2PanelHost.cs",
            "C2LeftDrawerPanelHost.cs",
            "C2TopBarPanelHost.cs",
            "C2MenuPanelHost.cs",
        };

        foreach (var host in hosts)
        {
            var text = UiIaSourceReader.ReadRuntime(host);
            Assert.That(text, Does.Contain("UiDocumentPanelSettingsBootstrap.EnsureDocument"), host);
        }
    }
}
```

- [ ] **Step 2: Run**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter FullyQualifiedName~UiIaPanelSettingsOracleTests -v n --nologo
```

Expected: **3 passed**. If `OobTreePanelHost` (or another) lacks `EnsureDocument`, GitNexus `impact` on that host then add `UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);` next to the existing `GetComponent<UIDocument>()` / `Awake` — same pattern as `C2TopBarPanelHost.Awake`.

- [ ] **Step 3: Commit**

```bash
git add src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/UiIaPanelSettingsOracleTests.cs
# plus any host EnsureDocument one-liners if Step 2 required them
git commit -m "$(cat <<'EOF'
test(ui): add UiIa PanelSettings-null bootstrap oracles

EOF
)"
```

---

### Task 6: Gauntlet-ui packaging

**Files:**
- Modify: `.claude/skills/qa-gauntlet-ui/SKILL.md`
- Modify: `.claude/skills/team-qa-gauntlet/SKILL.md`

**Interfaces:**
- Consumes: all `*UiIa*` test types from Tasks 2–5
- Produces: filter token `FullyQualifiedName~UiIa`; AAR row **IA oracles**

- [ ] **Step 1: Extend the hard-coded UI suite filter**

In `.claude/skills/qa-gauntlet-ui/SKILL.md`, change the `dotnet test` `--filter` string to append `|FullyQualifiedName~UiIa` (keep every existing clause). The quoted filter must become:

```text
FullyQualifiedName~PlayModeSmoke|FullyQualifiedName~Presentation|FullyQualifiedName~C2|FullyQualifiedName~MapPlaceholder|FullyQualifiedName~MapCanvas|FullyQualifiedName~UnityCsharpScriptHygiene|FullyQualifiedName~Panel|FullyQualifiedName~MessageLog|FullyQualifiedName~SensorC2|FullyQualifiedName~UiIa
```

Note: `Presentation` already matches these test files; `UiIa` is still required so the dedicated IA row cannot silently miss a class that is renamed out of `Presentation`.

- [ ] **Step 2: AAR template row**

In the same skill, add a table row under the existing Headless UI row:

```markdown
| IA oracles (`UiIa`) | N/N |
```

Keep `track: game-ui`. Add one sentence under Success / package: IA oracles are mandatory; layout/visuals and manual UAT are not covered.

- [ ] **Step 3: Team orchestrator summary**

In `.claude/skills/team-qa-gauntlet/SKILL.md`, under `--mode ui` / `ui-smoke` summary, add item: `UiIa` IA oracles (selection, COMMS, planning, PanelSettings) via `/qa-gauntlet-ui`. Do **not** rewrite ladder prose.

- [ ] **Step 4: Prove the filter matches**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter FullyQualifiedName~UiIa -v n --nologo
```

Expected: **all UiIa tests passed**, **0 failed** (11 tests if every family landed: 2+3+3+3).

Then run the **full** updated UI-style filter (same string as the skill) and confirm 0 failures. Count may exceed 118; that is allowed.

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~PlayModeSmoke|FullyQualifiedName~Presentation|FullyQualifiedName~C2|FullyQualifiedName~MapPlaceholder|FullyQualifiedName~MapCanvas|FullyQualifiedName~UnityCsharpScriptHygiene|FullyQualifiedName~Panel|FullyQualifiedName~MessageLog|FullyQualifiedName~SensorC2|FullyQualifiedName~UiIa" \
  -v minimal --nologo
```

Expected: **0 failed**. Also:

```bash
grep -r "17144800277401907079" tests/ data/ | head
git diff --name-only | grep DelegationBridge.cs || true
```

Expected: hash hits > 0; no `DelegationBridge.cs` in the diff.

- [ ] **Step 5: Commit**

```bash
git add .claude/skills/qa-gauntlet-ui/SKILL.md .claude/skills/team-qa-gauntlet/SKILL.md
git commit -m "$(cat <<'EOF'
docs(qa): require UiIa oracles on gauntlet-ui track

EOF
)"
```

---

## Self-review (plan vs spec)

| Spec item | Task |
|-----------|------|
| D1 contract-first headless | Tasks 2–5 |
| D2 two oracle styles, no UITK | Tasks 2–5 (separate `[Test]` methods) |
| D3 participating hosts only | Tasks 2 and 5 host lists |
| D4 Wave6 binders only as bind paths | Task 2 uses existing binders; no extra Wave6 hosts |
| D5 `UiIa` type names + filter | All test classes + Task 6 |
| D6 skill last | Task 6 |
| D7 no second UAT loop | Task 6 non-coverage sentence |
| D8 zero DelegationBridge / WriteGate / hash | Global constraints + Task 6 grep |
| COMMS triad map + top bar + feed | Task 3 |
| Planning MapPlaceholder, drawer, menu | Task 4 |
| PanelSettings bootstrap / no Tick | Task 5 |
| Failure routing `/qa-gauntlet-remediation` | Already in skill; Task 6 does not add a human loop |
