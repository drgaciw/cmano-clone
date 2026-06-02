// Doc-20 top bar — sim time, phase, compression, score strip.
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class C2TopBarPanelHost : MonoBehaviour
    {
        private const string RootName = "c2-topbar-root";
        private const string SimTimeName = "sim-time-label";
        private const string PhaseName = "phase-label";
        private const string CompressionName = "compression-label";
        private const string ModeName = "mode-label";
        private const string CommsName = "comms-label";
        private const string ScoreName = "score-label";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _simTime;
        private Label? _phase;
        private Label? _compression;
        private Label? _mode;
        private Label? _comms;
        private Label? _score;
        private bool _wired;

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
            _simTime = panel.Q<Label>(SimTimeName);
            _phase = panel.Q<Label>(PhaseName);
            _compression = panel.Q<Label>(CompressionName);
            _mode = panel.Q<Label>(ModeName);
            _comms = panel.Q<Label>(CommsName);
            _score = panel.Q<Label>(ScoreName);
            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            _wired = _simTime != null && _phase != null && _compression != null && _mode != null && _score != null;
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null)
            {
                return;
            }

            var state = bridgeHost.LastTopBar;
            _simTime!.text = state.SimTimeLabel;
            _phase!.text = state.PhaseLabel;
            _compression!.text = state.CompressionLabel;
            _mode!.text = state.ModeLabel;
            if (_comms != null)
            {
                _comms.text = state.CommsLabel;
                _comms.ClearClassList();
                _comms.AddToClassList("c2-topbar-item");
                _comms.AddToClassList("c2-topbar-item--comms");
                if (state.CommsLabel.Contains("DEGRADED", System.StringComparison.Ordinal))
                {
                    _comms.AddToClassList("c2-topbar-item--comms-degraded");
                }
                else if (state.CommsLabel.Contains("DENIED", System.StringComparison.Ordinal))
                {
                    _comms.AddToClassList("c2-topbar-item--comms-denied");
                }
                else
                {
                    _comms.AddToClassList("c2-topbar-item--comms-nominal");
                }
            }

            _score!.text = state.ScoreLabel;

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
#endif