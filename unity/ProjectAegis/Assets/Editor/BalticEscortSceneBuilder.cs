#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectAegis.Unity.Runtime;

namespace ProjectAegis.Unity.Editor
{
    /// <summary>Builds the player mission independently of the legacy smoke-test scene.</summary>
    public static class BalticEscortSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/BalticEscort.unity";
        private const string UiPath = "Assets/UI/EscortMission/";

        [MenuItem("Project Aegis/Play Baltic Escort")]
        public static void PlayFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Build();
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Project Aegis/Build Baltic Escort Scene")]
        public static void BuildFromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Build();
            }
        }

        /// <summary>Builds a mission scene after the caller has handled unsaved Editor work.</summary>
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.11f);

            var simulation = new GameObject("Baltic Escort Mission");
            simulation.AddComponent<DelegationBridgeHost>();
            var mission = simulation.AddComponent<EscortMissionHost>();

            var ui = new GameObject("Escort Command Post");
            var document = ui.AddComponent<UIDocument>();
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "EscortMissionPanel.uxml");
            var styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(UiPath + "EscortMissionPanel.uss");
            if (tree == null || styles == null)
            {
                throw new InvalidOperationException("Escort mission UI assets failed to import. See the Console for UXML/USS diagnostics.");
            }

            var settingsPath = UiDocumentPanelSettingsBootstrap.DefaultPanelSettingsAssetPath;
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(settingsPath);
            if (panelSettings == null)
            {
                panelSettings = UiDocumentPanelSettingsBootstrap.CreateDefaultPanelSettings();
                panelSettings.name = "C2RuntimePanelSettings";
                AssetDatabase.CreateAsset(panelSettings, settingsPath);
            }

            document.panelSettings = panelSettings;
            document.visualTreeAsset = tree;
            var panel = ui.AddComponent<EscortMissionPanelHost>();
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("missionHost").objectReferenceValue = mission;
            serialized.FindProperty("panelAsset").objectReferenceValue = tree;
            serialized.FindProperty("panelStyles").objectReferenceValue = styles;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save Baltic escort scene.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Baltic escort player mission saved: " + ScenePath);
        }

        /// <summary>Batch scene generation and player entry registration; fails on import/setup errors.</summary>
        public static void BuildBatch()
        {
            try
            {
                Build();
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
#endif
