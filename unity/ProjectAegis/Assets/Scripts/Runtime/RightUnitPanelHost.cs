// Doc-20 right unit detail panel — UI Toolkit bound to DelegationBridgeHost.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class RightUnitPanelHost : MonoBehaviour
    {
        private const string RootName = "unit-detail-root";
        private const string UnitIdName = "unit-id-line";
        private const string StatusName = "status-line";
        private const string CommsName = "comms-line";
        private const string MagazineName = "magazine-line";
        private const string EmconName = "emcon-line";
        private const string DoctrineName = "doctrine-line";
        private const string FuelName = "fuel-line";
        private const string DlzName = "dlz-line";
        private const string WraSalvoName = "wra-salvo-line";
        private const string EngageName = "engage-line";
        private const string AttackOptionsName = "attack-options-line";
        private const string ContactName = "contact-line";

        /// <summary>Repo-relative Specced production USS for ASSET-008 (not Approved).</summary>
        public const string SpeccedProductionUssRelativePath =
            ProjectAegis.Delegation.Projection.SpeccedC2PanelStylePaths.Asset008RightUnitDetailUss;

        /// <summary>Unity-side USS path constant for host style-path parity (S106).</summary>
        public const string UnityUssRelativePath =
            ProjectAegis.Delegation.Projection.SpeccedC2PanelStylePaths.UnityRightUnitDetailUss;

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _unitIdLine;
        private Label? _statusLine;
        private Label? _commsLine;
        private Label? _magazineLine;
        private Label? _emconLine;
        private Label? _doctrineLine;
        private Label? _fuelLine;
        private Label? _dlzLine;
        private Label? _wraSalvoLine;
        private Label? _engageLine;
        private Label? _attackOptionsLine;
        private readonly Dictionary<string, Button> _attackButtons = new();
        private Label? _contactLine;
        private bool _wired;
        private bool _attackHandlersRegistered;
        private string? _commandFeedback;
        private string? _feedbackUnitId;
        private string? _feedbackBaseAttackOptionsLine;
        private string? _attackOptionsDisplay;
        private UnitDetailPresentation _presentation = UnitDetailPresentation.Empty;
        private UnitDetailFuelBandPresentation _fuelBandPresentation = new("FUEL: —", null, FuelBandCueClasses.Unknown, null);
        private DlzLiveSurfaceState _dlzSurface = DlzLiveSurfaceState.Empty;
        private WraSalvoRemainingState _wraSalvoSurface = WraSalvoRemainingState.Empty;
        private AttackOptionsPreviewState _attackPreview = AttackOptionsPreviewState.Empty;

        /// <summary>Last applied unit-detail presentation (S107 apply-state).</summary>
        public UnitDetailPresentation LastPresentation => _presentation;

        /// <summary>Last applied weapon-panel DLZ row (DRG-266).</summary>
        public DlzLiveSurfaceState LastDlzSurface => _dlzSurface;

        /// <summary>Last applied weapon-panel WRA salvo row (DRG-259).</summary>
        public WraSalvoRemainingState LastWraSalvoSurface => _wraSalvoSurface;

        /// <summary>Last applied attack-options preview (DRG-262). Presentation only.</summary>
        public AttackOptionsPreviewState LastAttackOptionsPreview => _attackPreview;

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

            if (panelStyles != null && _document.rootVisualElement != null)
            {
                _document.rootVisualElement.styleSheets.Add(panelStyles);
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
            _unitIdLine = panel.Q<Label>(UnitIdName);
            _statusLine = panel.Q<Label>(StatusName);
            _commsLine = panel.Q<Label>(CommsName);
            _magazineLine = panel.Q<Label>(MagazineName);
            _emconLine = panel.Q<Label>(EmconName);
            _doctrineLine = panel.Q<Label>(DoctrineName);
            _fuelLine = panel.Q<Label>(FuelName);
            _dlzLine = panel.Q<Label>(DlzName);
            _wraSalvoLine = panel.Q<Label>(WraSalvoName);
            _engageLine = panel.Q<Label>(EngageName);
            _attackOptionsLine = panel.Q<Label>(AttackOptionsName);
            if (_attackOptionsLine != null)
            {
                _attackOptionsLine.style.whiteSpace = WhiteSpace.Normal;
            }
            _contactLine = panel.Q<Label>(ContactName);
            _attackButtons.Clear();
            RegisterAttackButton(panel.Q<Button>("attack-fire-single"), "fire-single");
            RegisterAttackButton(panel.Q<Button>("attack-fire-salvo"), "fire-salvo");
            RegisterAttackButton(panel.Q<Button>("attack-hold-fire"), "hold-fire");
            _wired = _unitIdLine != null && _statusLine != null && _magazineLine != null &&
                     _emconLine != null && _doctrineLine != null;

            WireAttackMenuHandlers();

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }
        }

        private void RegisterAttackButton(Button? button, string optionId)
        {
            if (button == null)
            {
                return;
            }

            _attackButtons[optionId] = button;
        }

        /// <summary>Apply panel state via shipped binder + apply-state path (S107).</summary>
        public void ApplyPanelState(UnitDetailPanelState? state)
        {
            _presentation = UnitDetailApplyState.Apply(state);
            _attackPreview = AttackOptionsPreviewBinder.Bind(state?.AttackMenu);
            ApplyPresentationToLabels();

            // Mirror Refresh(): without this, direct-apply callers keep whatever
            // fire/hold buttons the previous bridge Refresh() left displayed and
            // enabled. A null/empty menu correctly hides them.
            RefreshAttackMenuButtons();
        }

        private void Refresh()
        {
            var selectedUnitId = bridgeHost == null ? null : bridgeHost.SelectedUnitId;
            if (_commandFeedback != null &&
                !string.Equals(_feedbackUnitId, selectedUnitId, System.StringComparison.Ordinal))
            {
                _commandFeedback = null;
                _feedbackUnitId = null;
                _feedbackBaseAttackOptionsLine = null;
                _attackOptionsDisplay = null;
            }

            if (!_wired || bridgeHost == null)
            {
                return;
            }

            var state = UnitDetailPanelBinder.Bind(
                bridgeHost.LastUnitDetail,
                bridgeHost.Presentation.ResolveContactLine());
            _presentation = UnitDetailApplyState.Apply(state);
            _attackPreview = AttackOptionsPreviewBinder.Bind(state.AttackMenu);
            _dlzSurface = DlzLiveSurfaceBinder.BindFromEngagePreview(
                bridgeHost.ProjectSelectedEngagePreview());
            _wraSalvoSurface = bridgeHost.ProjectSelectedWraSalvoRemaining();
            ApplyPresentationToLabels();
            RefreshAttackMenuButtons();

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ApplyPresentationToLabels()
        {
            if (_unitIdLine != null)
            {
                _unitIdLine.text = _presentation.UnitIdLine;
            }

            if (_statusLine != null)
            {
                _statusLine.text = _presentation.StatusLine;
            }

            // CMD-17: null-safe — older UXML without comms-line still wires.
            if (_commsLine != null)
            {
                _commsLine.text = _presentation.CommsLine;
            }

            if (_magazineLine != null)
            {
                _magazineLine.text = _presentation.MagazineLine;
            }

            if (_emconLine != null)
            {
                _emconLine.text = _presentation.EmconLine;
            }

            if (_doctrineLine != null)
            {
                _doctrineLine.text = _presentation.DoctrineLine;
            }

            if (_fuelLine != null)
            {
                _fuelBandPresentation = UnitDetailFuelBandBinder.Bind(_presentation);
                _fuelLine.text = string.IsNullOrEmpty(_fuelBandPresentation.DeclutterToken)
                    ? _fuelBandPresentation.FuelLineText
                    : $"{_fuelBandPresentation.FuelLineText} {_fuelBandPresentation.DeclutterToken}";
                ApplyFuelBandCueClass(_fuelLine, _fuelBandPresentation.CueClass);
            }

            foreach (var row in DlzLiveSurfacePanelBinder.BindRows(_dlzSurface))
            {
                ApplyDlzSurfaceRow(_dlzLine, row.Text, row.CueClass);
            }

            foreach (var row in WraSalvoRemainingPanelBinder.BindRows(_wraSalvoSurface))
            {
                var wraSalvoText = string.IsNullOrEmpty(_wraSalvoSurface.DeclutterToken)
                    ? row.Text
                    : $"{row.Text} {_wraSalvoSurface.DeclutterToken}";
                ApplyWraSalvoSurfaceRow(_wraSalvoLine, wraSalvoText, row.CueClass);
            }

            if (_engageLine != null)
            {
                _engageLine.text = _presentation.EngagePreviewLine;
            }

            if (_attackOptionsLine != null)
            {
                ApplyAttackOptionsCue(_attackOptionsLine, _attackPreview.CueClass);
                var previewLine = _attackPreview.PreviewLine;
                if (string.IsNullOrEmpty(_commandFeedback))
                {
                    _attackOptionsDisplay = previewLine;
                }
                else if (!string.Equals(
                             _feedbackBaseAttackOptionsLine,
                             previewLine,
                             System.StringComparison.Ordinal))
                {
                    _feedbackBaseAttackOptionsLine = previewLine;
                    _attackOptionsDisplay = $"{previewLine}\n{_commandFeedback}";
                }

                if (!string.Equals(_attackOptionsLine.text, _attackOptionsDisplay, System.StringComparison.Ordinal))
                {
                    _attackOptionsLine.text = _attackOptionsDisplay;
                }
            }

            if (_contactLine != null)
            {
                _contactLine.text = _presentation.ContactLine;
            }
        }

        private void RefreshAttackMenuButtons()
        {
            foreach (var (optionId, button) in _attackButtons)
            {
                var row = AttackOptionsPreviewBinder.FindRow(_attackPreview, optionId);
                if (row == null)
                {
                    button.style.display = DisplayStyle.None;
                    continue;
                }

                button.style.display = DisplayStyle.Flex;
                var salvoBlocked = optionId == "fire-salvo" && _wraSalvoSurface.IsExhausted;
                var enabled = row.Enabled && !salvoBlocked;
                string text;
                string tooltip;
                if (!row.Enabled)
                {
                    text = row.ButtonText;
                    tooltip = row.AbortReason ?? AttackOptionsPreviewBinder.MissingAbortToken;
                }
                else if (salvoBlocked)
                {
                    var reason = _wraSalvoSurface.AbortReasonCode ?? "WRA salvo exhausted";
                    text = $"{row.Label} — {reason}";
                    tooltip = reason;
                }
                else
                {
                    text = row.Label;
                    tooltip = row.Label;
                }

                button.text = text;
                button.tooltip = tooltip;
                button.SetEnabled(enabled);
                ApplyAttackOptionsCue(
                    button,
                    enabled ? AttackOptionsCueClasses.Ready : AttackOptionsCueClasses.Blocked);
            }
        }

        private void WireAttackMenuHandlers()
        {
            if (_attackHandlersRegistered)
            {
                return;
            }

            foreach (var (optionId, button) in _attackButtons)
            {
                var capturedId = optionId;
                button.clicked += () => OnAttackOptionClicked(capturedId);
            }

            if (_attackButtons.Count > 0)
            {
                _attackHandlersRegistered = true;
            }
        }

        private void OnAttackOptionClicked(string optionId)
        {
            if (bridgeHost == null)
            {
                SetCommandFeedback(optionId, false, "NO_BRIDGE");
                return;
            }

            var queued = bridgeHost.TrySelectAttackOption(optionId, out var failureReason);
            SetCommandFeedback(optionId, queued, failureReason);
            Refresh();
        }

        private void SetCommandFeedback(string optionId, bool queued, string? failureReason)
        {
            _feedbackUnitId = bridgeHost == null ? null : bridgeHost.SelectedUnitId;
            if (queued)
            {
                _commandFeedback = $"QUEUED: {ResolveAttackOptionLabel(optionId)}";
            }
            else
            {
                var reason = string.IsNullOrEmpty(failureReason) ? "ISSUE_FAILED" : failureReason;
                _commandFeedback = $"DENIED: {DescribeFailure(reason)} ({reason})";
            }

            _feedbackBaseAttackOptionsLine = null;
            ApplyPresentationToLabels();
        }

        private string ResolveAttackOptionLabel(string optionId)
        {
            var row = AttackOptionsPreviewBinder.FindRow(_attackPreview, optionId);
            if (row != null)
            {
                return row.Label;
            }

            return optionId switch
            {
                "fire-single" => "Fire 1 round",
                "fire-salvo" => "Fire salvo",
                "hold-fire" => "Hold fire",
                _ => optionId,
            };
        }

        private static void ApplyAttackOptionsCue(VisualElement element, string cueClass)
        {
            foreach (var knownCue in AttackOptionsCueClasses.All)
            {
                element.RemoveFromClassList(knownCue);
            }

            if (!string.IsNullOrEmpty(cueClass))
            {
                element.AddToClassList(cueClass);
            }
        }

        private static string DescribeFailure(string reason)
        {
            if (reason == "UNKNOWN_OPTION") return "Unknown command";
            if (reason == "NO_SELECTION") return "No unit selected";
            if (reason == "UNKNOWN_UNIT") return "Unknown unit";
            if (reason == "NO_BRIDGE") return "Command service unavailable";
            if (reason == "ISSUE_FAILED") return "Command was not queued";

            var words = reason.Replace('_', ' ').ToLowerInvariant();
            return words.Length == 0 ? "Command blocked" : char.ToUpperInvariant(words[0]) + words.Substring(1);
        }

        private static void ApplyFuelBandCueClass(Label label, string cueClass)
        {
            foreach (var knownCue in FuelBandCueClasses.All)
            {
                label.RemoveFromClassList(knownCue);
            }

            if (!string.IsNullOrEmpty(cueClass))
            {
                label.AddToClassList(cueClass);
            }
        }

        private static void ApplyDlzSurfaceRow(Label? label, string text, string cueClass)
        {
            ApplyCueSurfaceRow(label, text, cueClass, DlzCueClasses.All);
        }

        private static void ApplyWraSalvoSurfaceRow(Label? label, string text, string cueClass)
        {
            ApplyCueSurfaceRow(label, text, cueClass, WraSalvoCueClasses.All);
        }

        private static void ApplyCueSurfaceRow(Label? label, string text, string cueClass, IReadOnlyList<string> knownCues)
        {
            if (label == null)
            {
                return;
            }

            label.text = text;
            foreach (var knownCue in knownCues)
            {
                label.RemoveFromClassList(knownCue);
            }

            if (!string.IsNullOrEmpty(cueClass))
            {
                label.AddToClassList(cueClass);
            }
        }
    }
}
#endif
