// DRG-181 sensor-to-shooter chain panel — text bind of SensorToShooterApplyState (ADR-010).
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.SensorToShooter;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Thin UI Toolkit host for the selected contact's sensor→shooter chain.
    /// Labels receive <see cref="SensorToShooterPresentation"/> strings without re-formatting.
    /// </summary>
    /// <remarks>
    /// <see cref="DelegationBridgeHost"/> does not expose a <see cref="SensorToShooterSnapshot"/>
    /// (or fire-control / shooter sources). Live refresh therefore either:
    /// 1. applies an injected snapshot via <see cref="BindSnapshot"/> (test / serialized hook), or
    /// 2. projects from the existing public <c>Bridge.Orchestrator.DecisionLog</c> +
    ///    <c>CurrentSimTick</c> + <c>CatalogReader</c> surface (FC/shooter links may be incomplete).
    /// Does not edit DelegationBridge / DelegationBridgeHost.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SensorToShooterPanelHost : MonoBehaviour
    {
        private const string RootName = "sensor-to-shooter-root";
        private const string ContactIdName = "contact-id-line";
        private const string CompleteName = "complete-line";
        private const string PrimaryCauseName = "primary-cause-line";
        private const string SensorLinkName = "sensor-link-line";
        private const string TrackLinkName = "track-link-line";
        private const string TargetabilityLinkName = "targetability-link-line";
        private const string ShooterLinkName = "shooter-link-line";
        private const string ExplainLinkName = "explain-link-line";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _contactIdLine;
        private Label? _completeLine;
        private Label? _primaryCauseLine;
        private Label? _sensorLinkLine;
        private Label? _trackLinkLine;
        private Label? _targetabilityLinkLine;
        private Label? _shooterLinkLine;
        private Label? _explainLinkLine;
        private bool _wired;
        private SensorToShooterPresentation _presentation = SensorToShooterPresentation.Empty;
        private SensorToShooterSnapshot? _injectedSnapshot;

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
            TryWireElements();
            Refresh();
        }

        private void LateUpdate()
        {
            if (!showPanel)
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
            _contactIdLine = panel.Q<Label>(ContactIdName);
            _completeLine = panel.Q<Label>(CompleteName);
            _primaryCauseLine = panel.Q<Label>(PrimaryCauseName);
            _sensorLinkLine = panel.Q<Label>(SensorLinkName);
            _trackLinkLine = panel.Q<Label>(TrackLinkName);
            _targetabilityLinkLine = panel.Q<Label>(TargetabilityLinkName);
            _shooterLinkLine = panel.Q<Label>(ShooterLinkName);
            _explainLinkLine = panel.Q<Label>(ExplainLinkName);
            _wired = _contactIdLine != null && _completeLine != null && _explainLinkLine != null;

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }
        }

        /// <summary>Apply presentation via headless apply-state (tests / direct bind). Does not re-format.</summary>
        public void Apply(SensorToShooterPresentation presentation)
        {
            _presentation = presentation ?? SensorToShooterPresentation.Empty;
            ApplyPresentationToLabels();
        }

        /// <summary>
        /// Test / serialized hook: bind a snapshot when the feed does not publish one.
        /// Subsequent <see cref="Refresh"/> maps it through <see cref="SensorToShooterApplyState"/>.
        /// </summary>
        public void BindSnapshot(SensorToShooterSnapshot? snapshot)
        {
            _injectedSnapshot = snapshot;
        }

        private void Refresh()
        {
            if (!_wired)
            {
                return;
            }

            var contactId = bridgeHost != null ? bridgeHost.SelectedContactId : null;
            if (_injectedSnapshot != null)
            {
                _presentation = SensorToShooterApplyState.Apply(_injectedSnapshot, contactId);
            }
            else if (bridgeHost != null && bridgeHost.Bridge != null)
            {
                // Best-effort live bind from existing public DecisionLog surface (no DelegationBridgeHost edits).
                var snapshot = SensorToShooterProjection.Project(
                    bridgeHost.Bridge.Orchestrator.DecisionLog,
                    bridgeHost.CurrentSimTick,
                    catalog: bridgeHost.CatalogReader);
                _presentation = SensorToShooterApplyState.Apply(snapshot, contactId);
            }

            ApplyPresentationToLabels();

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                var hasContact = bridgeHost != null && !string.IsNullOrEmpty(bridgeHost.SelectedContactId);
                var hasInjected = _injectedSnapshot != null;
                root.style.display = showPanel && (hasContact || hasInjected)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private void ApplyPresentationToLabels()
        {
            if (_contactIdLine != null)
            {
                _contactIdLine.text = _presentation.ContactIdLine;
            }

            if (_completeLine != null)
            {
                _completeLine.text = _presentation.CompleteLine;
            }

            if (_primaryCauseLine != null)
            {
                _primaryCauseLine.text = _presentation.PrimaryCauseLine;
            }

            if (_sensorLinkLine != null)
            {
                _sensorLinkLine.text = _presentation.SensorLinkLine;
            }

            if (_trackLinkLine != null)
            {
                _trackLinkLine.text = _presentation.TrackLinkLine;
            }

            if (_targetabilityLinkLine != null)
            {
                _targetabilityLinkLine.text = _presentation.TargetabilityLinkLine;
            }

            if (_shooterLinkLine != null)
            {
                _shooterLinkLine.text = _presentation.ShooterLinkLine;
            }

            if (_explainLinkLine != null)
            {
                _explainLinkLine.text = _presentation.ExplainLinkLine;
            }
        }
    }
}
#endif
