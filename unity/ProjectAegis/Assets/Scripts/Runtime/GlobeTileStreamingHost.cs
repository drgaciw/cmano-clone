// Tile streaming ion visual gate status chrome (Wave 4 Lane C).
// Pure UI Toolkit — no Cesium package dependency; safe for CI / default smoke.
// Shows honest ACTIVE / NO_ION_TOKEN / PACKAGE_MISSING from GlobeIonGateProjection.
// Token values are never read into serialized fields or logs.
#if UNITY_5_3_OR_NEWER
using System;
using System.Reflection;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Toolkit status strip for Cesium tile-streaming gate.
    /// Works without com.cesium.unity — reports PACKAGE_MISSING when Cesium assembly absent.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class GlobeTileStreamingHost : MonoBehaviour
    {
        private const string RootName = "globe-tile-streaming-root";
        private const string StatusName = "globe-tile-status-line";
        private const string ConfigName = "globe-tile-config-line";

        /// <summary>Primary env var for ion token presence (never stores value).</summary>
        public const string IonTokenEnvVar = "CESIUM_ION_TOKEN";

        /// <summary>Alternate env var for Editor user secret naming.</summary>
        public const string IonTokenEnvVarAlt = "ProjectAegis_CesiumIonToken";

        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _statusLine;
        private Label? _configLine;
        private VisualElement? _root;
        private GlobeTileStreamingPresentation _presentation = GlobeTileStreamingPresentation.Empty;
        private bool _wired;

        /// <summary>Last applied tile-streaming presentation (status + config summary).</summary>
        public GlobeTileStreamingPresentation LastPresentation => _presentation;

        /// <summary>True when env or CesiumGlobeHost reports a non-empty token (value not exposed).</summary>
        public bool IsIonTokenPresent => ResolveHasTokenConfigured();

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            EnsureProgrammaticTree();
        }

        private void OnEnable()
        {
            EnsureProgrammaticTree();
            TryWireElements();
            Refresh();
        }

        private void LateUpdate()
        {
            if (!showPanel)
            {
                if (_root != null)
                {
                    _root.style.display = DisplayStyle.None;
                }

                return;
            }

            if (!_wired)
            {
                TryWireElements();
            }

            Refresh();
        }

        /// <summary>Force refresh gate status from env / host / package presence.</summary>
        public void Refresh()
        {
            if (_root != null)
            {
                _root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }

            var hasToken = ResolveHasTokenConfigured();
            var packageAvailable = ResolvePackageAvailable();
            _presentation = GlobeTileStreamingApplyState.ProjectAndApply(
                hasToken,
                packageAvailable,
                GlobeTileStreamingConfig.Default);

            if (_statusLine != null)
            {
                _statusLine.text = _presentation.StatusLine;
            }

            if (_configLine != null)
            {
                _configLine.text = _presentation.ConfigSummary;
            }
        }

        private static bool ResolveHasTokenConfigured()
        {
            // Prefer CesiumGlobeHost.IsIonTokenPresent when the Cesium assembly is loaded.
            try
            {
                var hostType = Type.GetType(
                    "ProjectAegis.Unity.Runtime.CesiumGlobeHost, ProjectAegis.Unity.Runtime.Cesium");
                if (hostType != null)
                {
                    var find = typeof(UnityEngine.Object)
                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m =>
                            m.Name == "FindFirstObjectByType" &&
                            m.IsGenericMethodDefinition &&
                            m.GetParameters().Length == 0);
                    if (find != null)
                    {
                        var generic = find.MakeGenericMethod(hostType);
                        var host = generic.Invoke(null, null);
                        if (host != null)
                        {
                            var prop = hostType.GetProperty("IsIonTokenPresent", BindingFlags.Public | BindingFlags.Instance);
                            if (prop != null && prop.PropertyType == typeof(bool))
                            {
                                return (bool)prop.GetValue(host)!;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fall through to env presence.
            }

            var env = Environment.GetEnvironmentVariable(IonTokenEnvVar)
                      ?? Environment.GetEnvironmentVariable(IonTokenEnvVarAlt);
            return !string.IsNullOrWhiteSpace(env);
        }

        private static bool ResolvePackageAvailable()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = asm.GetName().Name;
                if (string.Equals(name, "ProjectAegis.Unity.Runtime.Cesium", StringComparison.Ordinal) ||
                    string.Equals(name, "CesiumForUnity", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureProgrammaticTree()
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var panel = root.Q<VisualElement>(RootName);
            if (panel != null)
            {
                return;
            }

            panel = new VisualElement { name = RootName };
            panel.style.position = Position.Absolute;
            panel.style.left = 8;
            panel.style.bottom = 48;
            panel.style.paddingLeft = 8;
            panel.style.paddingRight = 8;
            panel.style.paddingTop = 4;
            panel.style.paddingBottom = 4;
            panel.style.backgroundColor = new Color(0.05f, 0.1f, 0.14f, 0.8f);

            var status = new Label("GLOBE TILES") { name = StatusName };
            status.style.color = Color.white;
            status.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(status);

            var config = new Label { name = ConfigName };
            config.style.color = new Color(0.7f, 0.75f, 0.8f, 1f);
            panel.Add(config);

            root.Add(panel);
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            _root = root.Q<VisualElement>(RootName);
            _statusLine = root.Q<Label>(StatusName);
            _configLine = root.Q<Label>(ConfigName);
            _wired = _root != null && _statusLine != null;
        }
    }
}
#endif
