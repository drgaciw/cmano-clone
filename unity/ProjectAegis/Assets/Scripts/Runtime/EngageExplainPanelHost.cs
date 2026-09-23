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
        private const string AuthorityName = "engage-explain-authority";
        private const string DomainAbortName = "engage-explain-domain-abort";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _statusLabel;
        private Label? _reasonLabel;
        private Label? _authorityLabel;
        private Label? _dlzLine;
        private Label? _domainAbortLabel;
        private EngageExplain _last = EngageExplain.Empty;
        private DlzLiveSurfaceState _dlzSurface = DlzLiveSurfaceState.Empty;
        private DomainAbortExplainState _domainAbort = DomainAbortExplainState.Empty;
        private string? _lastDomainAbortFingerprint;
        private bool _wired;
        private CombatPresentationFrame? _lastFrame;
        private string? _lastSelection;
        private string? _lastUnit;
        private string? _lastContact;

        /// <summary>Correlated event explanation from the shared Slice B frame.</summary>
        public CombatDetailPresentation LastCombatDetail { get; private set; } = CombatDetailPresentation.Empty;

        /// <summary>Last projected explain state (DRG-67).</summary>
        public EngageExplain LastExplain => _last;

        /// <summary>Last applied weapon-panel DLZ row (DRG-266).</summary>
        public DlzLiveSurfaceState LastDlzSurface => _dlzSurface;

        /// <summary>Last bound domain-abort line (DRG-265). Read-only presentation chrome.</summary>
        public DomainAbortExplainState LastDomainAbort => _domainAbort;

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
            _authorityLabel = panel.Q<Label>(AuthorityName);
            _dlzLine = panel.Q<Label>("dlz-line");
            if (_dlzLine == null)
            {
                _dlzLine = new Label { name = "dlz-line" };
                _dlzLine.AddToClassList("engage-explain-dlz");
                _dlzLine.AddToClassList(DlzCueClasses.Unknown);
                panel.Insert(1, _dlzLine);
            }

            _domainAbortLabel = panel.Q<Label>(DomainAbortName);
            if (_domainAbortLabel == null)
            {
                _domainAbortLabel = new Label { name = DomainAbortName };
                _domainAbortLabel.AddToClassList("engage-explain-domain-abort");
                _domainAbortLabel.AddToClassList(DomainAbortCueClasses.Unknown);
                panel.Add(_domainAbortLabel);
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            _wired = _statusLabel != null || _reasonLabel != null || _authorityLabel != null
                || _domainAbortLabel != null;
            _lastFrame = null; // A rebuilt UIDocument needs binding even while simulation is paused.
            _lastDomainAbortFingerprint = null;
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
            var preview = bridgeHost.ProjectSelectedEngagePreview();
            _dlzSurface = DlzLiveSurfaceBinder.BindFromEngagePreview(preview);
            ApplyDomainAbort(DomainAbortExplainBinder.Bind(
                EngageExplainProjection.Project(preview),
                preview,
                LastCombatDetail));

            if (_statusLabel != null)
            {
                _statusLabel.text = _last.StatusLine;
            }

            if (_reasonLabel != null)
            {
                _reasonLabel.text = _last.ReasonPlain;
            }

            if (_authorityLabel != null)
            {
                var contactId = bridgeHost.SelectedContactId;
                bridgeHost.LastSliceAContacts.Authorities.TryGetValue(
                    contactId ?? string.Empty,
                    out var authority);
                _authorityLabel.text = C2AuthorityPresenter.FormatSummaryLine(
                    C2AuthorityPresenter.Build(contactId, authority));
            }

            foreach (var row in DlzLiveSurfacePanelBinder.BindRows(_dlzSurface))
            {
                ApplyDlzSurfaceRow(_dlzLine, row.Text, row.CueClass);
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
            ApplyDomainAbort(DomainAbortExplainBinder.Bind(_last));
            if (_wired)
            {
                if (_statusLabel != null) _statusLabel.text = _last.StatusLine;
                if (_reasonLabel != null) _reasonLabel.text = _last.ReasonPlain;
                if (_authorityLabel != null) _authorityLabel.text = string.Empty;
            }
        }

        private void ApplyDomainAbort(DomainAbortExplainState next)
        {
            var unchanged = string.Equals(
                next.Fingerprint,
                _lastDomainAbortFingerprint,
                StringComparison.Ordinal);
            _domainAbort = next;
            if (unchanged)
            {
                return;
            }

            _lastDomainAbortFingerprint = next.Fingerprint;
            foreach (var row in DomainAbortExplainBinder.BindRows(next))
            {
                ApplyDomainAbortRow(_domainAbortLabel, row.Text, row.CueClass, next.DeclutterToken);
            }
        }

        private static void ApplyDomainAbortRow(Label? label, string text, string cueClass, string declutterToken)
        {
            if (label == null)
            {
                return;
            }

            label.text = text;
            label.tooltip = declutterToken;
            foreach (var knownCue in DomainAbortCueClasses.All)
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
            if (label == null)
            {
                return;
            }

            label.text = text;
            foreach (var knownCue in DlzCueClasses.All)
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
