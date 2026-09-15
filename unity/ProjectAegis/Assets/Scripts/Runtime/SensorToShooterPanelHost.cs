// DRG-181: Sensor-to-shooter chain inspection chrome — presentation-only, no fire orders.
#if UNITY_5_3_OR_NEWER
using System;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SensorToShooterPanelHost : MonoBehaviour
    {
        private const string RootName = "sensor-to-shooter-root";
        private const string ContactIdName = "sts-contact-line";
        private const string TargetIdName = "sts-target-line";
        private const string ObserverIdName = "sts-observer-line";
        private const string StatusName = "sts-status-line";
        private const string SensorName = "sts-sensor-line";
        private const string TrackName = "sts-track-line";
        private const string TargetabilityName = "sts-targetability-line";
        private const string ShooterName = "sts-shooter-line";
        private const string NextActionName = "sts-next-action-line";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private VisualElement? _panel;
        private Label? _contactIdLine;
        private Label? _targetIdLine;
        private Label? _observerIdLine;
        private Label? _statusLine;
        private Label? _sensorLine;
        private Label? _trackLine;
        private Label? _targetabilityLine;
        private Label? _shooterLine;
        private Label? _nextActionLine;
        private SliceAContactFrame? _lastFrame;
        private string? _lastContactId;
        private string? _lastFingerprint;
        private SensorToShooterPresentation _presentation = SensorToShooterPresentation.Empty;
        private bool _wired;

        /// <summary>Last applied sensor-to-shooter presentation (DRG-181).</summary>
        public SensorToShooterPresentation LastPresentation => _presentation;

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
            _lastFingerprint = null;
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
            if (_document == null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var panel = root.Q<VisualElement>(RootName) ?? root;
            _panel = panel;
            _contactIdLine = panel.Q<Label>(ContactIdName);
            _targetIdLine = panel.Q<Label>(TargetIdName);
            _observerIdLine = panel.Q<Label>(ObserverIdName);
            _statusLine = panel.Q<Label>(StatusName);
            _sensorLine = panel.Q<Label>(SensorName);
            _trackLine = panel.Q<Label>(TrackName);
            _targetabilityLine = panel.Q<Label>(TargetabilityName);
            _shooterLine = panel.Q<Label>(ShooterName);
            _nextActionLine = panel.Q<Label>(NextActionName);
            _wired = _statusLine != null && _sensorLine != null;

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }
        }

        /// <summary>Apply projected chain state directly (headless / test path).</summary>
        public void Apply(SensorToShooterPresentation presentation)
        {
            _presentation = presentation ?? SensorToShooterPresentation.Empty;
            _lastFrame = null;
            _lastFingerprint = _presentation.Fingerprint;
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
            {
                _panel.style.display = showPanel && !string.IsNullOrEmpty(contactId)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            var frame = bridgeHost == null ? SliceAContactFrame.Empty : bridgeHost.LastSliceAContacts;
            var fingerprint = frame.EligibilityAvailable && frame.Chains.Chains.Count > 0
                ? SensorToShooterProjection.ComputeFingerprint(frame.Chains)
                : "sts:empty";
            if (ReferenceEquals(frame, _lastFrame)
                && string.Equals(contactId, _lastContactId, StringComparison.Ordinal)
                && string.Equals(fingerprint, _lastFingerprint, StringComparison.Ordinal))
            {
                return;
            }

            _lastFrame = frame;
            _lastContactId = contactId;
            _lastFingerprint = fingerprint;
            _presentation = SensorToShooterPresenter.Build(
                contactId,
                frame.EligibilityAvailable ? frame.Chains : null,
                frame.EligibilityAvailable);
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

            if (_observerIdLine != null)
            {
                _observerIdLine.text = _presentation.ObserverIdLine;
            }

            if (_statusLine != null)
            {
                _statusLine.text = _presentation.StatusLine;
            }

            if (_sensorLine != null)
            {
                _sensorLine.text = _presentation.SensorLine;
            }

            if (_trackLine != null)
            {
                _trackLine.text = _presentation.TrackLine;
            }

            if (_targetabilityLine != null)
            {
                _targetabilityLine.text = _presentation.TargetabilityLine;
            }

            if (_shooterLine != null)
            {
                _shooterLine.text = _presentation.ShooterLine;
            }

            if (_nextActionLine != null)
            {
                _nextActionLine.text = _presentation.NextActionLine;
            }
        }
    }
}
#endif
