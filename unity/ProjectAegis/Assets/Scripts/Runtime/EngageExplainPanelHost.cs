// DRG-67: Engage Explain panel — plain-language fire-refusal explanation (CMD-11).
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class EngageExplainPanelHost : MonoBehaviour
    {
        private const string RootName = "engage-explain-root";
        private const string StatusName = "engage-explain-status";
        private const string ReasonName = "engage-explain-reason";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _statusLabel;
        private Label? _reasonLabel;
        private EngageExplain _last = EngageExplain.Empty;
        private bool _wired;
        private CombatPresentationFrame? _lastFrame;
        private string? _lastSelection;
        private string? _lastUnit;
        private string? _lastContact;

        /// <summary>Correlated event explanation from the shared Slice B frame.</summary>
        public CombatDetailPresentation LastCombatDetail { get; private set; } = CombatDetailPresentation.Empty;

        /// <summary>Last projected explain state (DRG-67).</summary>
        public EngageExplain LastExplain => _last;

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
            _statusLabel = panel.Q<Label>(StatusName);
            _reasonLabel = panel.Q<Label>(ReasonName);

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            _wired = _statusLabel != null || _reasonLabel != null;
            _lastFrame = null; // A rebuilt UIDocument needs binding even while simulation is paused.
        }

        private void Refresh()
        {
            if (!_wired)
            {
                return;
            }

            if (bridgeHost == null || bridgeHost.Bridge == null) return;
            var frame = bridgeHost.DisplayCombatFrame;
            var selected = bridgeHost.DisplayCombatKey;
            if (ReferenceEquals(frame, _lastFrame) && selected == _lastSelection
                && bridgeHost.SelectedUnitId == _lastUnit && bridgeHost.SelectedContactId == _lastContact) return;
            _lastFrame = frame;
            _lastSelection = selected;
            _lastUnit = bridgeHost.SelectedUnitId;
            _lastContact = bridgeHost.SelectedContactId;
            LastCombatDetail = bridgeHost.ProjectReviewCombatDetail();
            _last = new EngageExplain(LastCombatDetail.StatusLine, null,
                string.Join("\n", LastCombatDetail.WeaponLine, LastCombatDetail.HardConstraintsLine,
                    LastCombatDetail.PolicyLine, LastCombatDetail.ConfidenceLine, LastCombatDetail.FiringSolutionLine,
                    LastCombatDetail.NextActionLine, LastCombatDetail.CorrelationLine),
                LastCombatDetail.StatusLine.Contains("AuthorizationRefused"));

            if (_statusLabel != null)
            {
                _statusLabel.text = _last.StatusLine;
            }

            if (_reasonLabel != null)
            {
                _reasonLabel.text = _last.ReasonPlain;
            }

            var rootEl = _document.rootVisualElement?.Q(RootName);
            if (rootEl != null)
            {
                rootEl.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>Apply a projected explain state directly (headless / test path).</summary>
        public void Apply(EngageExplain explain)
        {
            _last = explain ?? EngageExplain.Empty;
            if (_wired)
            {
                if (_statusLabel != null) _statusLabel.text = _last.StatusLine;
                if (_reasonLabel != null) _reasonLabel.text = _last.ReasonPlain;
            }
        }
    }
}
#endif
