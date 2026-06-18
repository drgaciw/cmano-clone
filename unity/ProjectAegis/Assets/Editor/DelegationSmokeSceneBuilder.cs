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
            go.AddComponent<UIDocument>();
            var host = go.AddComponent<T>();
            SetObjectReference(host, "bridgeHost", bridge);
            SetObjectReference(host, "panelAsset", AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath));
            SetObjectReference(host, "panelStyles", AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath));
            return host;
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
