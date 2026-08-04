#if UNITY_EDITOR
using System;
using ProjectAegis.Unity.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Editor
{
    /// <summary>
    /// Builds the Play Mode smoke scene per PLAYMODE-SMOKE.md (batchmode or menu).
    /// Also builds optional CesiumSpike scene (useGlobeMap) — separate path; never forced into smoke CI.
    /// </summary>
    public static class DelegationSmokeSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/DelegationSmoke.unity";

        /// <summary>Optional product globe spike scene (ADR-007 Phase B). Not used by default CI smoke.</summary>
        public const string CesiumSpikeScenePath = "Assets/Scenes/CesiumSpike.unity";

        [MenuItem("Project Aegis/Build DelegationSmoke Scene (comms QA)")]
        public static void BuildFromMenuComms() => Build("baltic-patrol-comms");

        [MenuItem("Project Aegis/Build DelegationSmoke Scene (classify QA)")]
        public static void BuildFromMenuClassify() => Build("baltic-patrol-classify");

        /// <summary>Unity batchmode entry: -executeMethod ProjectAegis.Unity.Editor.DelegationSmokeSceneBuilder.BuildBatch</summary>
        public static void BuildBatch() => Build("baltic-patrol-comms");

        public static void Build(string scenarioPolicyId, bool exitBatchModeWhenDone = true)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            // Default GameObjects include AudioListener; strip it when the audio built-in module
            // is disabled (Console: "AudioListener component deleted: Component belongs to a disabled built-in package").
            StripAudioListenersFromOpenScene();

            var root = new GameObject("DelegationSmoke");
            var bridge = root.AddComponent<DelegationBridgeHost>();
            var sim = root.AddComponent<SimplePlayModeSimHost>();

            SetInt(bridge, "globalSeed", 42);
            SetBool(bridge, "enableMvpEngagement", true);
            SetString(bridge, "scenarioPolicyId", scenarioPolicyId);
            // CI-safe default: placeholder map, not Cesium product globe.
            SetBool(bridge, "useGlobeMap", false);
            SetObjectReference(sim, "bridgeHost", bridge);

            CreatePanelHost<C2TopBarPanelHost>(
                "C2TopBar",
                bridge,
                "Assets/UI/TopBar/C2TopBarPanel.uxml",
                "Assets/UI/TopBar/C2TopBarPanel.uss");
            CreatePanelHost<C2LeftDrawerPanelHost>(
                "C2LeftDrawer",
                bridge,
                "Assets/UI/C2LeftDrawer/C2LeftDrawerPanel.uxml",
                "Assets/UI/C2LeftDrawer/C2LeftDrawerPanel.uss");
            CreatePanelHost<MapPlaceholderPanelHost>(
                "MapPlaceholder",
                bridge,
                "Assets/UI/MapPlaceholder/MapPlaceholderPanel.uxml",
                "Assets/UI/MapPlaceholder/MapPlaceholderPanel.uss");
            CreatePanelHost<RightUnitPanelHost>(
                "RightUnitDetail",
                bridge,
                "Assets/UI/UnitDetail/UnitDetailPanel.uxml",
                "Assets/UI/UnitDetail/UnitDetailPanel.uss");
            CreatePanelHost<MessageLogPanelHost>(
                "MessageLog",
                bridge,
                "Assets/UI/MessageLog/MessageLogPanel.uxml",
                "Assets/UI/MessageLog/MessageLogPanel.uss");
            CreatePanelHost<DoctrineInheritancePanelHost>(
                "DoctrineInheritance",
                bridge,
                "Assets/UI/DoctrineInheritance/DoctrineInheritancePanel.uxml",
                "Assets/UI/DoctrineInheritance/DoctrineInheritancePanel.uss");
            CreatePanelHost<PlatformCatalogViewerHost>(
                "PlatformCatalog",
                bridge,
                "Assets/UI/PlatformCatalog/PlatformCatalogPanel.uxml",
                "Assets/UI/PlatformCatalog/PlatformCatalogPanel.uss");
            CreatePanelHost<PlatformImportPanelHost>(
                "PlatformImport",
                bridge,
                "Assets/UI/PlatformImport/PlatformImportPanel.uxml",
                "Assets/UI/PlatformImport/PlatformImportPanel.uss");

            // UI maturity Wave1 hosts (CMD-16 / CMD-29 / CMD-37)
            CreatePanelHost<UnitOrderToolbarHost>(
                "UnitOrderToolbar",
                bridge,
                "Assets/UI/UnitOrderToolbar/UnitOrderToolbarPanel.uxml",
                "Assets/UI/UnitOrderToolbar/UnitOrderToolbarPanel.uss");
            CreatePanelHost<ContactDetailPanelHost>(
                "ContactDetail",
                bridge,
                "Assets/UI/ContactDetail/ContactDetailPanel.uxml",
                "Assets/UI/ContactDetail/ContactDetailPanel.uss");
            CreatePanelHost<AgentRosterPanelHost>(
                "AgentRoster",
                bridge,
                "Assets/UI/AgentRoster/AgentRosterPanel.uxml",
                "Assets/UI/AgentRoster/AgentRosterPanel.uss");

            // UI maturity Wave2 hosts (CMD-24 / CMD-27 / CMD-35)
            CreatePanelHost<AirOpsPanelHost>(
                "AirOps",
                bridge,
                "Assets/UI/AirOps/AirOpsPanel.uxml",
                "Assets/UI/AirOps/AirOpsPanel.uss");
            CreatePanelHost<ScenarioLibraryPanelHost>(
                "ScenarioLibrary",
                bridge,
                "Assets/UI/ScenarioLibrary/ScenarioLibraryPanel.uxml",
                "Assets/UI/ScenarioLibrary/ScenarioLibraryPanel.uss");
            CreatePanelHost<LiveEditPanelHost>(
                "LiveEdit",
                bridge,
                "Assets/UI/LiveEdit/LiveEditPanel.uxml",
                "Assets/UI/LiveEdit/LiveEditPanel.uss");

            // UI maturity Wave4 residual hosts (CMD-25 / magazine / deck)
            CreatePanelHost<BoatOpsPanelHost>(
                "BoatOps",
                bridge,
                "Assets/UI/BoatOps/BoatOpsPanel.uxml",
                "Assets/UI/BoatOps/BoatOpsPanel.uss");
            CreatePanelHost<MagazineLoadoutPanelHost>(
                "MagazineLoadout",
                bridge,
                "Assets/UI/MagazineLoadout/MagazineLoadoutPanel.uxml",
                "Assets/UI/MagazineLoadout/MagazineLoadoutPanel.uss");
            CreatePanelHost<DeckHangarPanelHost>(
                "DeckHangar",
                bridge,
                "Assets/UI/DeckHangar/DeckHangarPanel.uxml",
                "Assets/UI/DeckHangar/DeckHangarPanel.uss");

            var scenesDir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"Failed to save scene at {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"DelegationSmoke scene saved: {ScenePath} scenario={scenarioPolicyId}");
            if (Application.isBatchMode && exitBatchModeWhenDone)
            {
                EditorApplication.Exit(0);
            }
        }

        /// <summary>
        /// Builds <see cref="CesiumSpikeScenePath"/> with useGlobeMap=true + product globe hosts.
        /// Separate from DelegationSmoke — does not break CI smoke (useGlobeMap stays false there).
        /// Cesium package components are added via reflection when ProjectAegis.Unity.Runtime.Cesium is present.
        /// Ion tokens must never be written into the scene by this builder.
        /// </summary>
        [MenuItem("Project Aegis/Build CesiumSpike Scene")]
        public static void BuildCesiumSpikeSceneFromMenu() => BuildCesiumSpikeScene(exitBatchModeWhenDone: false);

        /// <summary>
        /// Batch: -executeMethod ProjectAegis.Unity.Editor.DelegationSmokeSceneBuilder.BuildCesiumSpikeSceneBatch
        /// </summary>
        public static void BuildCesiumSpikeSceneBatch() => BuildCesiumSpikeScene(exitBatchModeWhenDone: true);

        public static void BuildCesiumSpikeScene(bool exitBatchModeWhenDone = true)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            StripAudioListenersFromOpenScene();

            var root = new GameObject("CesiumSpike");
            var bridge = root.AddComponent<DelegationBridgeHost>();
            var sim = root.AddComponent<SimplePlayModeSimHost>();

            SetInt(bridge, "globalSeed", 42);
            SetBool(bridge, "enableMvpEngagement", true);
            SetString(bridge, "scenarioPolicyId", "baltic-patrol-comms");
            // Product globe flag ON for this scene only (not DelegationSmoke).
            SetBool(bridge, "useGlobeMap", true);
            SetObjectReference(sim, "bridgeHost", bridge);

            // Placeholder map still present so CesiumGlobeBridge can feed symbols when package active.
            var mapHost = CreatePanelHost<MapPlaceholderPanelHost>(
                "MapPlaceholder",
                bridge,
                "Assets/UI/MapPlaceholder/MapPlaceholderPanel.uxml",
                "Assets/UI/MapPlaceholder/MapPlaceholderPanel.uss");

            // Product globe status chrome — pure Toolkit, no Cesium package required (CI-safe type).
            var productGo = new GameObject("GlobeMapProduct");
            var productDoc = productGo.AddComponent<UIDocument>();
            productDoc.panelSettings = EnsurePanelSettingsAsset();
            var productHost = productGo.AddComponent<GlobeMapProductHost>();
            SetObjectReference(productHost, "bridgeHost", bridge);

            // Wave4 tile streaming gate status — pure Toolkit; works without com.cesium.unity.
            // Never writes ion tokens; host reports NO_ION_TOKEN / PACKAGE_MISSING honestly.
            var tileGo = new GameObject("GlobeTileStreaming");
            var tileDoc = tileGo.AddComponent<UIDocument>();
            tileDoc.panelSettings = EnsurePanelSettingsAsset();
            tileGo.AddComponent<GlobeTileStreamingHost>();

            // Cesium types live in ProjectAegis.Unity.Runtime.Cesium (CESIUM_FOR_UNITY gated).
            // Add via reflection so this Editor assembly never hard-depends on the Cesium package.
            var cesiumBridgeGo = new GameObject("CesiumGlobeBridge");
            var cesiumBridge = TryAddComponentByTypeName(
                cesiumBridgeGo,
                "ProjectAegis.Unity.Runtime.CesiumGlobeBridge");
            if (cesiumBridge != null && mapHost != null)
            {
                SetObjectReference(cesiumBridge, "mapHost", mapHost);
            }
            else if (cesiumBridge == null)
            {
                Debug.Log(
                    "DelegationSmokeSceneBuilder.BuildCesiumSpikeScene: CesiumGlobeBridge type not found " +
                    "(com.cesium.unity / CESIUM_FOR_UNITY inactive). GO left as slot.");
            }

            var hostGo = new GameObject("CesiumGlobeHost");
            var cesiumHost = TryAddComponentByTypeName(
                hostGo,
                "ProjectAegis.Unity.Runtime.CesiumGlobeHost");
            if (cesiumHost == null)
            {
                hostGo.name = "CesiumGlobeHost (requires com.cesium.unity)";
                Debug.Log(
                    "DelegationSmokeSceneBuilder.BuildCesiumSpikeScene: CesiumGlobeHost type not found; " +
                    "GlobeMapProductHost + useGlobeMap=true still wired. Ion token never written.");
            }
            // Intentionally do NOT set ionAccessToken — user secret via Inspector or env
            // CESIUM_ION_TOKEN / ProjectAegis_CesiumIonToken only (NEVER commit tokens).

            var scenesDir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            if (!EditorSceneManager.SaveScene(scene, CesiumSpikeScenePath))
            {
                Debug.LogError($"Failed to save scene at {CesiumSpikeScenePath}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"CesiumSpike scene saved: {CesiumSpikeScenePath} useGlobeMap=true " +
                "(ion token not written; set via Inspector or CESIUM_ION_TOKEN env only).");
            if (Application.isBatchMode && exitBatchModeWhenDone)
            {
                EditorApplication.Exit(0);
            }
        }

        /// <summary>
        /// Adds missing UI maturity hosts to the open scene without a full rebuild.
        /// Skips any GameObject whose name already exists.
        /// </summary>
        [MenuItem("Project Aegis/Ensure UI Maturity Hosts (open scene)")]
        public static void EnsureUiMaturityHostsOnOpenScene()
        {
            var bridge = Object.FindFirstObjectByType<DelegationBridgeHost>();
            if (bridge == null)
            {
                Debug.LogError(
                    "DelegationSmokeSceneBuilder.EnsureUiMaturityHosts: no DelegationBridgeHost in open scene.");
                return;
            }

            var added = 0;
            added += EnsurePanelHostIfMissing<UnitOrderToolbarHost>(
                "UnitOrderToolbar",
                bridge,
                "Assets/UI/UnitOrderToolbar/UnitOrderToolbarPanel.uxml",
                "Assets/UI/UnitOrderToolbar/UnitOrderToolbarPanel.uss");
            added += EnsurePanelHostIfMissing<ContactDetailPanelHost>(
                "ContactDetail",
                bridge,
                "Assets/UI/ContactDetail/ContactDetailPanel.uxml",
                "Assets/UI/ContactDetail/ContactDetailPanel.uss");
            added += EnsurePanelHostIfMissing<AgentRosterPanelHost>(
                "AgentRoster",
                bridge,
                "Assets/UI/AgentRoster/AgentRosterPanel.uxml",
                "Assets/UI/AgentRoster/AgentRosterPanel.uss");
            added += EnsurePanelHostIfMissing<AirOpsPanelHost>(
                "AirOps",
                bridge,
                "Assets/UI/AirOps/AirOpsPanel.uxml",
                "Assets/UI/AirOps/AirOpsPanel.uss");
            added += EnsurePanelHostIfMissing<ScenarioLibraryPanelHost>(
                "ScenarioLibrary",
                bridge,
                "Assets/UI/ScenarioLibrary/ScenarioLibraryPanel.uxml",
                "Assets/UI/ScenarioLibrary/ScenarioLibraryPanel.uss");
            added += EnsurePanelHostIfMissing<LiveEditPanelHost>(
                "LiveEdit",
                bridge,
                "Assets/UI/LiveEdit/LiveEditPanel.uxml",
                "Assets/UI/LiveEdit/LiveEditPanel.uss");
            added += EnsurePanelHostIfMissing<BoatOpsPanelHost>(
                "BoatOps",
                bridge,
                "Assets/UI/BoatOps/BoatOpsPanel.uxml",
                "Assets/UI/BoatOps/BoatOpsPanel.uss");
            added += EnsurePanelHostIfMissing<MagazineLoadoutPanelHost>(
                "MagazineLoadout",
                bridge,
                "Assets/UI/MagazineLoadout/MagazineLoadoutPanel.uxml",
                "Assets/UI/MagazineLoadout/MagazineLoadoutPanel.uss");
            added += EnsurePanelHostIfMissing<DeckHangarPanelHost>(
                "DeckHangar",
                bridge,
                "Assets/UI/DeckHangar/DeckHangarPanel.uxml",
                "Assets/UI/DeckHangar/DeckHangarPanel.uss");

            if (added > 0)
            {
                EditorSceneManager.MarkAllScenesDirty();
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log(
                $"DelegationSmokeSceneBuilder: Ensure UI Maturity Hosts added {added} host(s) to open scene.");
        }

        private static int EnsurePanelHostIfMissing<T>(
            string objectName,
            DelegationBridgeHost bridge,
            string uxmlPath,
            string ussPath)
            where T : MonoBehaviour
        {
            var existing = GameObject.Find(objectName);
            if (existing != null)
            {
                return 0;
            }

            CreatePanelHost<T>(objectName, bridge, uxmlPath, ussPath);
            return 1;
        }

        private static T CreatePanelHost<T>(
            string objectName,
            DelegationBridgeHost bridge,
            string uxmlPath,
            string ussPath)
            where T : MonoBehaviour
        {
            var go = new GameObject(objectName);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = EnsurePanelSettingsAsset();
            var host = go.AddComponent<T>();
            var hostSo = new SerializedObject(host);
            var bridgeProp = hostSo.FindProperty("bridgeHost");
            if (bridgeProp != null)
            {
                bridgeProp.objectReferenceValue = bridge;
                hostSo.ApplyModifiedPropertiesWithoutUndo();
            }

            SetObjectReference(host, "panelAsset", AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath));
            SetObjectReference(host, "panelStyles", AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath));
            return host;
        }

        /// <summary>
        /// Menu: assign shared PanelSettings to every UIDocument in the open scene (fixes empty Game view).
        /// Batch: -executeMethod ProjectAegis.Unity.Editor.DelegationSmokeSceneBuilder.FixPanelSettingsBatch
        /// </summary>
        [MenuItem("Project Aegis/Fix UIDocument PanelSettings (open scene)")]
        public static void FixPanelSettingsOnOpenScene()
        {
            var settings = EnsurePanelSettingsAsset();
            var documents = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var fixedCount = 0;
            foreach (var document in documents)
            {
                if (document.panelSettings == null)
                {
                    document.panelSettings = settings;
                    EditorUtility.SetDirty(document);
                    fixedCount++;
                }
            }

            if (fixedCount > 0)
            {
                EditorSceneManager.MarkAllScenesDirty();
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log(
                $"DelegationSmokeSceneBuilder: PanelSettings assigned on {fixedCount}/{documents.Length} UIDocument(s) path={UiDocumentPanelSettingsBootstrap.DefaultPanelSettingsAssetPath}");
        }

        public static void FixPanelSettingsBatch()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"Failed to open {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            FixPanelSettingsOnOpenScene();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void StripAudioListenersFromOpenScene()
        {
            // Use type name so editor scripts still compile when the audio built-in module is disabled.
            foreach (var component in Object.FindObjectsByType<Component>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (component != null && component.GetType().Name == "AudioListener")
                {
                    Object.DestroyImmediate(component);
                }
            }
        }

        /// <summary>
        /// Batch/menu: remove AudioListeners from the open scene (audio module disabled projects).
        /// </summary>
        [MenuItem("Project Aegis/Strip AudioListeners (open scene)")]
        public static void StripAudioListenersMenu()
        {
            StripAudioListenersFromOpenScene();
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("DelegationSmokeSceneBuilder: stripped AudioListeners from open scene.");
        }

        public static void StripAudioListenersBatch()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"Failed to open {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            StripAudioListenersMenu();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static PanelSettings EnsurePanelSettingsAsset()
        {
            var path = UiDocumentPanelSettingsBootstrap.DefaultPanelSettingsAssetPath;
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (existing != null)
            {
                return existing;
            }

            var dir = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                // Assets/UI is expected; create only the leaf if missing
                if (!AssetDatabase.IsValidFolder("Assets/UI"))
                {
                    AssetDatabase.CreateFolder("Assets", "UI");
                }
            }

            var created = UiDocumentPanelSettingsBootstrap.CreateDefaultPanelSettings();
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created PanelSettings asset at {path}");
            return created;
        }

        /// <summary>
        /// Add a MonoBehaviour by full type name without a compile-time reference
        /// (Cesium assembly is CESIUM_FOR_UNITY-gated and not referenced by this Editor asmdef).
        /// </summary>
        private static Component? TryAddComponentByTypeName(GameObject go, string fullTypeName)
        {
            Type? type = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullTypeName, throwOnError: false);
                if (type != null)
                {
                    break;
                }
            }

            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                return null;
            }

            return go.AddComponent(type);
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(propertyName).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(propertyName).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(propertyName).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectReference(Object target, string propertyName, Object? value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(propertyName).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
