// DRG-67: Map Scale HUD panel — scale bar (NM) and camera altitude readout (CMD-20).
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MapScaleHudPanelHost : MonoBehaviour
    {
        private const string RootName = "map-scale-hud-root";
        private const string ScaleBarName = "map-scale-bar";
        private const string AltitudeName = "map-scale-altitude";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _scaleBarLabel;
        private Label? _altLabel;
        private MapScaleState? _state;
        private bool _wired;

        /// <summary>Last map scale state applied (DRG-67).</summary>
        public MapScaleState? LastState => _state;

        private void Reset()
        {
            if (bridgeHost == null)
            {
                bridgeHost = GetComponent<DelegationBridgeHost>();
            }

            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (panelAsset != null)
            {
                _document.visualTreeAsset = panelAsset;
            }
        }

        private void OnEnable()
        {
            TryWireElements();
            Refresh();
        }

        private void LateUpdate()
        {
            if (!showPanel || bridgeHost == null)
            {
                return;
            }

            if (!_wired)
            {
                TryWireElements();
            }

            Refresh();
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var panel = root.Q<VisualElement>(RootName) ?? root;
            _scaleBarLabel = panel.Q<Label>(ScaleBarName);
            _altLabel = panel.Q<Label>(AltitudeName);

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            _wired = _scaleBarLabel != null || _altLabel != null;
        }

        private void Refresh()
        {
            if (!_wired || _state == null)
            {
                return;
            }

            if (_scaleBarLabel != null)
            {
                _scaleBarLabel.text = _state.ScaleBarLabel;
            }

            if (_altLabel != null)
            {
                _altLabel.text = _state.CameraAltitudeLabel;
            }

            var rootEl = _document.rootVisualElement?.Q(RootName);
            if (rootEl != null)
            {
                rootEl.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>Apply map scale state directly (headless / test path).</summary>
        public void Apply(MapScaleState state)
        {
            _state = state;
            if (_wired)
            {
                Refresh();
            }
        }
    }
}
#endif
