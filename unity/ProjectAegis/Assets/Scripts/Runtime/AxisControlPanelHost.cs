// DRG-67: Axis Control panel — per-axis Manual/Automatic control readout (CMD-19).
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AxisControlPanelHost : MonoBehaviour
    {
        private const string RootName = "axis-control-root";
        private const string BadgeName = "axis-control-badge";
        private const string AltitudeName = "axis-control-altitude";
        private const string ThrottleName = "axis-control-throttle";
        private const string EmconName = "axis-control-emcon";
        private const string SensorsName = "axis-control-sensors";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _badgeLabel;
        private Label? _altLabel;
        private Label? _thrLabel;
        private Label? _emcLabel;
        private Label? _snsLabel;
        private AxisControlState? _state;
        private bool _wired;

        /// <summary>Last axis control state applied (DRG-67).</summary>
        public AxisControlState? LastState => _state;

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
            _badgeLabel = panel.Q<Label>(BadgeName);
            _altLabel = panel.Q<Label>(AltitudeName);
            _thrLabel = panel.Q<Label>(ThrottleName);
            _emcLabel = panel.Q<Label>(EmconName);
            _snsLabel = panel.Q<Label>(SensorsName);

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            _wired = _badgeLabel != null || _altLabel != null;
        }

        private void Refresh()
        {
            if (!_wired || _state == null)
            {
                return;
            }

            if (_badgeLabel != null)
            {
                _badgeLabel.text = AxisControlProjection.FormatBadge(_state);
            }

            if (_altLabel != null)
            {
                _altLabel.text = _state.AltitudeAxis.ToString();
            }

            if (_thrLabel != null)
            {
                _thrLabel.text = _state.ThrottleAxis.ToString();
            }

            if (_emcLabel != null)
            {
                _emcLabel.text = _state.EmconAxis.ToString();
            }

            if (_snsLabel != null)
            {
                _snsLabel.text = _state.SensorsAxis.ToString();
            }

            var rootEl = _document.rootVisualElement?.Q(RootName);
            if (rootEl != null)
            {
                rootEl.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>Apply axis state directly (headless / test path).</summary>
        public void Apply(AxisControlState state)
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
