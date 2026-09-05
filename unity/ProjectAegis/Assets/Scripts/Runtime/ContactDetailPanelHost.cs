// CMD-29 contact detail panel — belief/inference view distinct from own-unit RightUnitPanelHost.
#if UNITY_5_3_OR_NEWER
using System;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ContactDetailPanelHost : MonoBehaviour
    {
        private const string RootName = "contact-detail-root";
        private const string ContactIdName = "contact-id-line";
        private const string TargetIdName = "target-id-line";
        private const string LifecycleName = "lifecycle-line";
        private const string ClassificationName = "classification-line";
        private const string ConfidenceName = "confidence-line";
        private const string ProvenanceName = "provenance-line";
        private const string WraName = "wra-line";
        private const string BdaName = "bda-line";
        private const string StalenessName = "staleness-line";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _contactIdLine;
        private Label? _targetIdLine;
        private Label? _lifecycleLine;
        private Label? _classificationLine;
        private Label? _confidenceLine;
        private Label? _provenanceLine;
        private Label? _wraLine;
        private Label? _bdaLine;
        private Label? _stalenessLine;
        private Label? _killChainLine;
        private Label? _sensorShooterLine;
        private Label? _authorityLine;
        private Label? _nextActionLine;
        private VisualElement? _panel;
        private SliceAContactFrame? _lastFrame;
        private string? _lastContactId;
        private SliceAContactPresentation _sliceA = SliceAContactPresentation.Empty;
        private bool _wired;
        private ContactDetailPresentation _presentation = ContactDetailPresentation.Empty;

        /// <summary>Last applied contact-detail presentation (CMD-29).</summary>
        public ContactDetailPresentation LastPresentation => _presentation;

        /// <summary>Last applied read-only sense-and-target explanation.</summary>
        public SliceAContactPresentation LastSliceAPresentation => _sliceA;

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
            _wired = false;
            _lastFrame = null;
            TryWireElements();
            Refresh();
        }

        private void LateUpdate()
        {
            if (!_wired)
            {
                TryWireElements();
            }

            Refresh();
        }

        private void TryWireElements()
        {
            if (_document == null) return;
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var panel = root.Q<VisualElement>(RootName) ?? root;
            _panel = panel;
            _contactIdLine = panel.Q<Label>(ContactIdName);
            _targetIdLine = panel.Q<Label>(TargetIdName);
            _lifecycleLine = panel.Q<Label>(LifecycleName);
            _classificationLine = panel.Q<Label>(ClassificationName);
            _confidenceLine = panel.Q<Label>(ConfidenceName);
            _provenanceLine = panel.Q<Label>(ProvenanceName);
            _wraLine = panel.Q<Label>(WraName);
            _bdaLine = panel.Q<Label>(BdaName);
            _stalenessLine = panel.Q<Label>(StalenessName);
            _killChainLine = panel.Q<Label>("kill-chain-line");
            _sensorShooterLine = panel.Q<Label>("sensor-shooter-line");
            _authorityLine = panel.Q<Label>("authority-line");
            _nextActionLine = panel.Q<Label>("next-action-line");
            _wired = _contactIdLine != null && _targetIdLine != null && _classificationLine != null;

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }
        }

        /// <summary>Apply presentation via headless apply-state (tests / direct bind).</summary>
        public void ApplyPresentation(ContactDetailPresentation presentation)
        {
            _presentation = presentation ?? ContactDetailPresentation.Empty;
            _sliceA = SliceAContactPresentation.Empty;
            _lastFrame = null;
            ApplyPresentationToLabels();
        }

        private void Refresh()
        {
            if (!_wired)
            {
                return;
            }

            var contactId = bridgeHost == null ? null : bridgeHost.SelectedContactId;
            if (_panel != null)
                _panel.style.display = showPanel && !string.IsNullOrEmpty(contactId)
                    ? DisplayStyle.Flex : DisplayStyle.None;

            var frame = bridgeHost == null ? SliceAContactFrame.Empty : bridgeHost.LastSliceAContacts;
            if (ReferenceEquals(frame, _lastFrame) && string.Equals(contactId, _lastContactId, StringComparison.Ordinal))
                return;

            _lastFrame = frame;
            _lastContactId = contactId;
            if (string.IsNullOrEmpty(contactId))
            {
                _presentation = ContactDetailPresentation.Empty;
            }
            else
            {
                _presentation = ContactDetailApplyState.ProjectAndApply(
                    contactId,
                    frame.Contacts,
                    frame.SimTick);
            }

            frame.Authorities.TryGetValue(contactId ?? string.Empty, out var authority);
            _sliceA = SliceAContactPresenter.Build(contactId, frame.KillChain, frame.Provenance,
                frame.EligibilityAvailable ? frame.Chains : null, authority);
            ApplyPresentationToLabels();
        }

        private void ApplyPresentationToLabels()
        {
            if (_contactIdLine != null)
            {
                _contactIdLine.text = _presentation.ContactIdLine;
            }

            if (_targetIdLine != null)
            {
                _targetIdLine.text = _presentation.TargetIdLine;
            }

            if (_lifecycleLine != null)
            {
                _lifecycleLine.text = _presentation.LifecycleLine;
            }

            if (_classificationLine != null)
            {
                _classificationLine.text = _presentation.ClassificationLine;
            }

            if (_confidenceLine != null)
            {
                _confidenceLine.text = _presentation.ConfidenceLine;
            }

            if (_provenanceLine != null)
            {
                _provenanceLine.text = ReferenceEquals(_sliceA, SliceAContactPresentation.Empty)
                    ? _presentation.DetectionProvenanceLine : _sliceA.ProvenanceLine;
            }

            if (_wraLine != null)
            {
                _wraLine.text = _presentation.WraLine;
            }

            if (_bdaLine != null)
            {
                _bdaLine.text = _presentation.BdaLine;
            }

            if (_stalenessLine != null)
            {
                _stalenessLine.text = ReferenceEquals(_sliceA, SliceAContactPresentation.Empty)
                    ? _presentation.StalenessLine : _sliceA.FreshnessLine;
            }
            if (_killChainLine != null) _killChainLine.text = _sliceA.PhaseLine;
            if (_sensorShooterLine != null) _sensorShooterLine.text = _sliceA.ChainLine;
            if (_authorityLine != null) _authorityLine.text = _sliceA.AuthorityLine;
            if (_nextActionLine != null) _nextActionLine.text = _sliceA.NextActionLine;
        }
    }
}
#endif
