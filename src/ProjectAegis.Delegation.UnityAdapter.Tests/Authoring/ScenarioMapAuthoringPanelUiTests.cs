using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Authoring;

/// <summary>
/// Structural gates for Scenario Map Authoring UI Toolkit assets + Editor host bootstrap.
/// Loads the shipped UXML/USS/C# paths from the repo (no mock reimplementation).
/// </summary>
[TestFixture]
public sealed class ScenarioMapAuthoringPanelUiTests
{
    private static readonly string[] RequiredElementNames =
    {
        "scenario-map-root",
        "scenario-map-scroll-root",
        "scenario-map-title",
        "scenario-map-help",
        "scenario-map-sticky-header",
        "scenario-session-bar",
        "scenario-path-field",
        "scenario-browse-button",
        "scenario-open-button",
        "scenario-load-ui-dev-button",
        "scenario-save-button",
        "scenario-rebuild-button",
        "scenario-refresh-findings-button",
        "scenario-session-body",
        "scenario-session-status",
        "scenario-session-path",
        "scenario-session-edit-version",
        "scenario-session-dirty",
        "scenario-units-list",
        "scenario-rps-list",
        "scenario-findings-list",
        "scenario-place-unit",
        "scenario-platform-domain-nav",
        "scenario-domain-aircraft",
        "scenario-domain-ships",
        "scenario-domain-subs",
        "scenario-domain-ground",
        "scenario-platform-domain-label",
        "scenario-platform-list-scroll",
        "scenario-platform-list",
        "scenario-place-unit-id",
        "scenario-place-side-id",
        "scenario-place-platform-id",
        "scenario-place-lat",
        "scenario-place-lon",
        "scenario-begin-place-unit",
        "scenario-commit-place-unit",
        "scenario-place-and-commit",
        "scenario-cancel-gesture",
        "scenario-reference-point",
        "scenario-rp-id",
        "scenario-rp-lat0",
        "scenario-rp-lon0",
        "scenario-rp-lat1",
        "scenario-rp-lon1",
        "scenario-rp-lat2",
        "scenario-rp-lon2",
        "scenario-upsert-rp",
        "scenario-status",
    };

    [Test]
    public void Scenario_map_authoring_panel_uxml_declares_stable_element_names()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var uxmlPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "ScenarioEditor",
            "ScenarioMapAuthoringPanel.uxml");
        Assert.That(File.Exists(uxmlPath), Is.True, $"Missing UXML: {uxmlPath}");

        var uxml = File.ReadAllText(uxmlPath);
        Assert.That(uxml, Does.Contain("editor-extension-mode=\"True\""));
        Assert.That(uxml, Does.Contain("ScenarioMapAuthoringPanel.uss"));

        foreach (var name in RequiredElementNames)
        {
            Assert.That(uxml, Does.Contain($"name=\"{name}\""), $"Missing UXML element: {name}");
        }
    }

    [Test]
    public void Scenario_map_authoring_panel_uss_imports_aegis_tokens_and_panel_classes()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var ussPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "ScenarioEditor",
            "ScenarioMapAuthoringPanel.uss");
        Assert.That(File.Exists(ussPath), Is.True, $"Missing USS: {ussPath}");

        var uss = File.ReadAllText(ussPath);
        Assert.That(uss, Does.Contain("@import url(\"../AegisTokens.uss\")"));
        Assert.That(uss, Does.Contain(".scenario-map-panel"));
        Assert.That(uss, Does.Contain(".scenario-map-sticky-header"));
        Assert.That(uss, Does.Contain(".scenario-map-title"));
        Assert.That(uss, Does.Contain(".scenario-map-status"));
        Assert.That(uss, Does.Contain("var(--surface-panel)"));
        Assert.That(uss, Does.Contain("var(--text-heading)"));
        // Body must not look dead when prep-browsing without a session.
        Assert.That(uss, Does.Not.Contain(".scenario-map-session-body:disabled"));
    }

    [Test]
    public void Scenario_map_authoring_window_create_gui_loads_shipped_uxml_path()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var hostPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Editor",
            "ScenarioMapAuthoringWindow.cs");
        Assert.That(File.Exists(hostPath), Is.True, $"Missing host: {hostPath}");

        var host = File.ReadAllText(hostPath);
        Assert.That(host, Does.Contain("CreateGUI()"));
        Assert.That(host, Does.Contain("RebuildUi()"));
        Assert.That(host, Does.Contain("using UnityEngine.UIElements;"));
        Assert.That(host, Does.Contain("Assets/UI/ScenarioEditor/ScenarioMapAuthoringPanel.uxml"));
        Assert.That(host, Does.Contain("Assets/UI/ScenarioEditor/ScenarioMapAuthoringPanel.uss"));
        Assert.That(host, Does.Contain("PanelUxmlAssetPath"));
        Assert.That(host, Does.Contain("CloneTree"));
        Assert.That(host, Does.Contain("Q<TextField>(\"scenario-path-field\")"));
        Assert.That(host, Does.Contain("Q<Button>(\"scenario-open-button\")"));
        Assert.That(host, Does.Contain("Q<VisualElement>(\"scenario-units-list\")"));
        Assert.That(host, Does.Contain("Q<VisualElement>(\"scenario-findings-list\")"));
        Assert.That(host, Does.Contain("Q<Button>(\"scenario-begin-place-unit\")"));
        Assert.That(host, Does.Contain("Q<Button>(\"scenario-upsert-rp\")"));
        Assert.That(host, Does.Contain("MenuItem(\"Project Aegis/Scenario Map Authoring\")"));
        Assert.That(host, Does.Contain("Project Aegis/Scenario Editor/Add Aircraft"));
        Assert.That(host, Does.Contain("Project Aegis/Scenario Editor/Add Ships"));
        Assert.That(host, Does.Contain("Project Aegis/Scenario Editor/Add Subs"));
        Assert.That(host, Does.Contain("Project Aegis/Scenario Editor/Add Ground Units"));
        Assert.That(host, Does.Contain("ScenarioPlatformDomainCatalog"));
        Assert.That(host, Does.Contain("SelectDomain"));
        Assert.That(host, Does.Contain("scenario-domain-aircraft"));
        Assert.That(host, Does.Contain("scenario-platform-list"));
        // Primary chrome is UI Toolkit, not IMGUI OnGUI layout.
        Assert.That(host, Does.Not.Contain("private void OnGUI()"));
        Assert.That(host, Does.Not.Contain("EditorGUILayout.BeginHorizontal"));
        // Still binds only to headless authoring surfaces (no DelegationBridge import/use).
        Assert.That(host, Does.Contain("ScenarioAuthoringSession"));
        Assert.That(host, Does.Contain("MapAuthoringSurface"));
        Assert.That(host, Does.Contain("LiveFindingsPresenter"));
        Assert.That(host, Does.Not.Contain("using ProjectAegis.Delegation.UnityAdapter.Bridge"));
        Assert.That(host, Does.Not.Contain("new DelegationBridge"));
        Assert.That(host, Does.Not.Contain("DelegationBridge."));
        // Host must invalidate staged place-unit before UXML reclones form defaults.
        Assert.That(host, Does.Contain("ScenarioMapAuthoringHostPolicy.InvalidateStagedGesturesForChromeRebuild"));
        // Dead-button fix: catalog/form stay enabled without session; write chrome gated; clicks via Button.clicked.
        Assert.That(host, Does.Contain("ShouldEnableCatalogAndFormChrome"));
        Assert.That(host, Does.Contain("ShouldEnableSessionWriteActions"));
        Assert.That(host, Does.Contain("NoSessionMessage"));
        Assert.That(host, Does.Contain("WireButton"));
        Assert.That(host, Does.Contain("button.clicked"));
        Assert.That(host, Does.Contain("TryAutoOpenSeededScenario"));
        Assert.That(host, Does.Contain("TryLoadUiDevScenario"));
        Assert.That(host, Does.Contain("scenario-load-ui-dev-button"));
        Assert.That(host, Does.Contain("ResolveDefaultScenarioPath"));
        Assert.That(host, Does.Contain("Open Baltic UI Dev Scenario"));
        Assert.That(host, Does.Contain("russia-vs-nato-baltic-ui-dev.json"));
        // Host still uses plugin Invalidate* APIs (present on older DLLs too).
        Assert.That(host, Does.Contain("InvalidateStagedGesturesForFormOrDomainChange"));
    }

    [Test]
    public void Project_console_quiet_bootstrap_is_non_destructive()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var bootstrapPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Editor",
            "ProjectConsoleQuietBootstrap.cs");
        Assert.That(File.Exists(bootstrapPath), Is.True, $"Missing bootstrap: {bootstrapPath}");

        var bootstrap = File.ReadAllText(bootstrapPath);
        Assert.That(bootstrap, Does.Not.Contain("DestroyImmediate"));
        Assert.That(bootstrap, Does.Not.Contain("StripAudioListeners"));
        Assert.That(bootstrap, Does.Not.Contain("File.WriteAllText"));
        Assert.That(bootstrap, Does.Not.Contain("[InitializeOnLoad]"));
        Assert.That(bootstrap, Does.Not.Contain("InitializeOnLoadMethod"));
        Assert.That(bootstrap, Does.Not.Contain("sceneOpened"));
        Assert.That(bootstrap, Does.Not.Contain("playModeStateChanged"));
        Assert.That(bootstrap, Does.Not.Contain("connectionMode"));
        Assert.That(bootstrap, Does.Not.Contain("keepServerRunning"));

        var smokePath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Editor",
            "DelegationSmokeSceneBuilder.cs");
        var smoke = File.ReadAllText(smokePath);
        Assert.That(smoke, Does.Contain("StripAudioListenersFromOpenScene"));
        Assert.That(smoke, Does.Contain("Strip AudioListeners (open scene)"));
        Assert.That(smoke, Does.Contain("StripAudioListenersBatch"));
    }

    [Test]
    public void Player_scripting_defines_do_not_enable_unity_mcp_ready()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var settingsPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "ProjectSettings",
            "ProjectSettings.asset");
        Assert.That(File.Exists(settingsPath), Is.True);

        var settings = File.ReadAllText(settingsPath);
        Assert.That(
            settings,
            Does.Not.Contain("UNITY_MCP_READY"),
            "Player scripting defines must not enable UNITY_MCP_READY (Editor-only MCP isolation).");
        Assert.That(settings, Does.Contain("APP_UI_EDITOR_ONLY"));
        Assert.That(settings, Does.Contain("SENTIS_ANALYTICS_ENABLED"));

        var isolationPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Editor",
            "McpPlayerBuildIsolation.cs");
        Assert.That(File.Exists(isolationPath), Is.True, "Missing McpPlayerBuildIsolation.cs");
        var isolation = File.ReadAllText(isolationPath);
        Assert.That(isolation, Does.Contain("IPreprocessBuildWithReport"));
        Assert.That(isolation, Does.Contain("UNITY_MCP_READY"));
        Assert.That(isolation, Does.Contain("OnPreprocessBuild"));
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return null;
    }
}
