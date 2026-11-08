#if UNITY_5_3_OR_NEWER
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Ensures every <see cref="UIDocument"/> has a <see cref="PanelSettings"/> so UI Toolkit
    /// can paint to the Game view. DelegationSmoke historically serialized
    /// <c>m_PanelSettings: {fileID: 0}</c>, which yields a null <see cref="UIDocument.rootVisualElement"/>
    /// and an empty skybox-only Play Mode view despite hosts + UXML being present.
    /// </summary>
    public static class UiDocumentPanelSettingsBootstrap
    {
        public const string DefaultPanelSettingsAssetPath = "Assets/UI/C2RuntimePanelSettings.asset";

        private static PanelSettings? _sharedRuntimeSettings;

        /// <summary>
        /// Shared PanelSettings used when no asset is assigned (Play Mode / headless bootstrap).
        /// </summary>
        public static PanelSettings SharedRuntimeSettings
        {
            get
            {
                if (_sharedRuntimeSettings == null)
                {
                    _sharedRuntimeSettings = CreateDefaultPanelSettings();
                }

                return _sharedRuntimeSettings;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            EnsureAllDocumentsInLoadedScenes();
        }

        /// <summary>
        /// Assigns <see cref="SharedRuntimeSettings"/> to every loaded UIDocument with a null
        /// panelSettings. Safe to call multiple times (idempotent).
        /// </summary>
        public static int EnsureAllDocumentsInLoadedScenes()
        {
            var fixedCount = 0;
#if UNITY_2023_1_OR_NEWER
            var documents = UnityEngine.Object.FindObjectsByType<UIDocument>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
#else
            var documents = UnityEngine.Object.FindObjectsOfType<UIDocument>(includeInactive: true);
#endif
            foreach (var document in documents)
            {
                if (EnsureDocument(document))
                {
                    fixedCount++;
                }
            }

            if (fixedCount > 0)
            {
                Debug.Log(
                    $"UiDocumentPanelSettingsBootstrap: assigned PanelSettings to {fixedCount} UIDocument(s).");
            }

            return fixedCount;
        }

        /// <summary>
        /// Ensures a single document has panel settings. Returns true if assignment was needed.
        /// </summary>
        public static bool EnsureDocument(UIDocument? document)
        {
            if (document == null || document.panelSettings != null)
            {
                return false;
            }

            document.panelSettings = SharedRuntimeSettings;
            return true;
        }

        /// <summary>
        /// Creates a Screen Space Overlay PanelSettings suitable for C2 HUD panels.
        /// </summary>
        public static PanelSettings CreateDefaultPanelSettings()
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "C2RuntimePanelSettings";
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.sortingOrder = 0;
            settings.clearColor = false;
            return settings;
        }
    }
}
#endif
