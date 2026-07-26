#if UNITY_EDITOR
using ProjectAegis.Unity.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Editor
{
    /// <summary>
    /// Builds the Play Mode smoke scene per PLAYMODE-SMOKE.md (batchmode or menu).
    /// </summary>
    public static class DelegationSmokeSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/DelegationSmoke.unity";

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
            Debug.Log("DelegationSmokeSceneBuilder: stripped AudioListener components from open scene.");
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
