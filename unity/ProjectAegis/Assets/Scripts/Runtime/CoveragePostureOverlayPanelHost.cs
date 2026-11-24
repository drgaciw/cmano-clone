// DRG-192: Passive map-canvas overlay showing scale-aware mission-package posture & gap change explanations (ADR-010 §2-3, ADR-007, ADR-001).
#if UNITY_5_3_OR_NEWER
using System;
using System.Text;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class CoveragePostureOverlayPanelHost : MonoBehaviour
    {
        public const int DefaultOverlaySortingOrder = 8;

        private static readonly string[] PostureSeverityClasses =
        {
            "coverage-posture--nominal",
            "coverage-posture--degraded",
            "coverage-posture--critical",
            "coverage-posture--unknown",
        };

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private MapScaleHudPanelHost? mapScaleHud;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private VisualElement? _root;
        private VisualElement? _card;
        private Label? _scaleBadge;
        private Label? _authorityBadge;
        private Label? _summaryLabel;
        private Label? _layersLabel;
        private Label? _gapsLabel;
        private VisualElement? _diffBox;
        private Label? _diffHeadline;
        private Label? _diffDetail;

        private readonly StringBuilder _sb = new StringBuilder(256);

        private CoordinationSnapshot? _lastCoordination;
        private CoordinationSnapshot? _baselineCoordination;
        private CoordinationSnapshot? _lastBaselineUsed;
        private MapScaleState? _explicitScale;
        private MapScaleState? _lastScaleUsed;
        private CoveragePostureDiff? _simulatedDiff;
        private bool _lastInspecting;
        private bool _lastShowCoverage;
        private bool _lastShowPanel;
        private bool _dirty = true;
        private bool _wired;

        /// <summary>Latest projected layered coverage posture snapshot.</summary>
        public CoveragePostureSnapshot LastPosture { get; private set; } = CoveragePostureSnapshot.Empty;

        /// <summary>Latest posture delta explanation (baseline-vs-current or hypothetical).</summary>
        public CoveragePostureDiff LastDiff { get; private set; } = CoveragePostureDiff.Empty;

        /// <summary>Latest evaluated map-scale appropriateness decision.</summary>
        public CoverageMapScaleDecision LastScaleDecision { get; private set; } =
            CoverageMapScaleEvaluator.Evaluate(null);

        private void Reset()
        {
            if (bridgeHost == null)
            {
                bridgeHost = GetComponent<DelegationBridgeHost>();
            }

            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            _document.sortingOrder = DefaultOverlaySortingOrder;
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            if (_document.sortingOrder <= 0 || _document.sortingOrder >= 10)
            {
                _document.sortingOrder = DefaultOverlaySortingOrder;
            }

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
            _dirty = true;
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

        /// <summary>Applies an explicit map scale state for headless/test scale-tier evaluation.</summary>
        public void ApplyScale(MapScaleState? scaleState)
        {
            _explicitScale = scaleState;
            _dirty = true;
            Refresh();
        }

        /// <summary>Captures the current coordination snapshot as the posture diff baseline.</summary>
        public void CaptureBaseline()
        {
            _baselineCoordination = bridgeHost == null ? null : bridgeHost.LastCoordination;
            _simulatedDiff = null;
            _dirty = true;
            Refresh();
        }

        /// <summary>Sets an explicit baseline coordination snapshot for posture comparison.</summary>
        public void SetBaseline(CoordinationSnapshot? baseline)
        {
            _baselineCoordination = baseline;
            _simulatedDiff = null;
            _dirty = true;
            Refresh();
        }

        /// <summary>Clears any captured posture baseline.</summary>
        public void ClearBaseline()
        {
            _baselineCoordination = null;
            _simulatedDiff = null;
            _dirty = true;
            Refresh();
        }

        /// <summary>Runs a non-mutating hypothetical element loss or reassignment projection.</summary>
        public void SimulateElementChange(
            string groupId,
            string unitId,
            HypotheticalElementChangeKind changeKind,
            string? targetGroupId = null)
        {
            var source = bridgeHost == null ? CoordinationSnapshot.Empty : bridgeHost.LastCoordination;
            _simulatedDiff = CoveragePostureProjection.SimulateElementChange(
                source,
                groupId,
                unitId,
                changeKind,
                targetGroupId);
            _dirty = true;
            Refresh();
        }

        /// <summary>Clears any active hypothetical what-if diff.</summary>
        public void ClearSimulation()
        {
            _simulatedDiff = null;
            _dirty = true;
            Refresh();
        }

        /// <summary>Re-evaluates posture and scale overlay only when inputs changed.</summary>
        public void Refresh()
        {
            if (!_wired)
            {
                return;
            }

            if (!showPanel || bridgeHost == null)
            {
                _lastShowPanel = showPanel;
                if (_root != null)
                {
                    _root.style.display = DisplayStyle.None;
                }

                return;
            }

            var coordination = bridgeHost.LastCoordination;
            var isInspecting = bridgeHost.CommandTimeline.IsInspecting;
            var showCoverage = bridgeHost.ShowCommandCoverage;
            var liveScale = _explicitScale ?? (mapScaleHud != null ? mapScaleHud.LastState : null);

            if (!_dirty
                && ReferenceEquals(coordination, _lastCoordination)
                && ReferenceEquals(_baselineCoordination, _lastBaselineUsed)
                && Equals(liveScale, _lastScaleUsed)
                && _lastInspecting == isInspecting
                && _lastShowCoverage == showCoverage
                && _lastShowPanel == showPanel)
            {
                return;
            }

            _dirty = false;
            _lastCoordination = coordination;
            _lastBaselineUsed = _baselineCoordination;
            _lastScaleUsed = liveScale;
            _lastInspecting = isInspecting;
            _lastShowCoverage = showCoverage;
            _lastShowPanel = showPanel;

            var scaleDecision = CoverageMapScaleEvaluator.Evaluate(liveScale, isInspecting || !showCoverage);
            LastScaleDecision = scaleDecision;

            if (!scaleDecision.ShowSummaryBanner)
            {
                LastPosture = CoveragePostureSnapshot.Empty;
                LastDiff = CoveragePostureDiff.Empty;
                if (_root != null)
                {
                    _root.style.display = DisplayStyle.None;
                }

                return;
            }

            if (_root != null)
            {
                _root.style.display = DisplayStyle.Flex;
            }

            var posture = CoveragePostureProjection.Project(coordination);
            LastPosture = posture;

            if (_baselineCoordination == null && coordination.Groups.Count > 0)
            {
                _baselineCoordination = coordination;
                _lastBaselineUsed = coordination;
            }

            var diff = _simulatedDiff
                ?? (_baselineCoordination != null
                    ? CoveragePostureProjection.Compare(_baselineCoordination, coordination)
                    : CoveragePostureDiff.Empty);
            LastDiff = diff;

            ApplyVisuals(posture, diff, scaleDecision);
        }

        private void TryWireElements()
        {
            if (_document == null)
            {
                return;
            }

            var docRoot = _document.rootVisualElement;
            if (docRoot == null)
            {
                return;
            }

            docRoot.pickingMode = PickingMode.Ignore;
            _root = docRoot.Q<VisualElement>("coverage-posture-overlay-root") ?? docRoot;
            _card = _root.Q<VisualElement>("coverage-posture-card");
            _scaleBadge = _root.Q<Label>("coverage-posture-scale-badge");
            _authorityBadge = _root.Q<Label>("coverage-posture-authority-badge");
            _summaryLabel = _root.Q<Label>("coverage-posture-summary-label");
            _layersLabel = _root.Q<Label>("coverage-posture-layers-label");
            _gapsLabel = _root.Q<Label>("coverage-posture-gaps-label");
            _diffBox = _root.Q<VisualElement>("coverage-posture-diff-box");
            _diffHeadline = _root.Q<Label>("coverage-posture-diff-headline");
            _diffDetail = _root.Q<Label>("coverage-posture-diff-detail");

            ApplyIgnorePicking(_root);

            if (panelStyles != null && !_root.styleSheets.Contains(panelStyles))
            {
                _root.styleSheets.Add(panelStyles);
            }

            _wired = _summaryLabel != null;
        }

        private void ApplyVisuals(
            CoveragePostureSnapshot posture,
            CoveragePostureDiff diff,
            CoverageMapScaleDecision scale)
        {
            if (_scaleBadge != null)
            {
                _scaleBadge.text = scale.SummaryLabel;
            }

            if (_authorityBadge != null)
            {
                _authorityBadge.text = "ADVISORY · IsOrder=false · WRA=false";
            }

            if (_summaryLabel != null)
            {
                _summaryLabel.text = posture.CompactSummary;
            }

            if (_layersLabel != null)
            {
                _sb.Clear();
                for (var g = 0; g < posture.Groups.Count; g++)
                {
                    var group = posture.Groups[g];
                    if (g > 0) _sb.Append('\n');
                    _sb.Append(group.SummaryLine);
                    for (var l = 0; l < group.Layers.Count; l++)
                    {
                        _sb.Append(" | ").Append(group.Layers[l].Detail);
                    }
                }

                _layersLabel.text = _sb.Length == 0 ? "No layered defense elements authored." : _sb.ToString();
            }

            if (_gapsLabel != null)
            {
                _sb.Clear();
                for (var g = 0; g < posture.Groups.Count; g++)
                {
                    var group = posture.Groups[g];
                    for (var i = 0; i < group.UnprotectedGaps.Count; i++)
                    {
                        if (_sb.Length > 0) _sb.Append('\n');
                        _sb.Append(group.UnprotectedGaps[i].Detail);
                    }
                }

                _gapsLabel.text = _sb.Length == 0 ? "Gaps: none" : _sb.ToString();
            }

            var hasDelta = diff.IsHypothetical || diff.OverallDelta != CoveragePostureDeltaKind.Unchanged;
            if (_diffBox != null)
            {
                _diffBox.style.display = hasDelta ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_diffHeadline != null)
            {
                _diffHeadline.text = diff.IsHypothetical
                    ? "[WHAT-IF] " + diff.Headline
                    : diff.Headline;
            }

            if (_diffDetail != null)
            {
                _sb.Clear();
                for (var i = 0; i < diff.ExplanationLines.Count; i++)
                {
                    if (i > 0) _sb.Append('\n');
                    _sb.Append(diff.ExplanationLines[i]);
                }

                _diffDetail.text = _sb.ToString();
            }

            if (_card != null)
            {
                ApplySeverityClass(_card, ResolveOverallSeverityClass(posture));
            }
        }

        private static string ResolveOverallSeverityClass(CoveragePostureSnapshot posture)
        {
            var hasCritical = false;
            var hasDegraded = false;
            var hasNominal = false;
            for (var i = 0; i < posture.Groups.Count; i++)
            {
                switch (posture.Groups[i].Severity)
                {
                    case CoveragePostureSeverity.Critical:
                        hasCritical = true;
                        break;
                    case CoveragePostureSeverity.Degraded:
                        hasDegraded = true;
                        break;
                    case CoveragePostureSeverity.Nominal:
                        hasNominal = true;
                        break;
                }
            }

            if (hasCritical) return "coverage-posture--critical";
            if (hasDegraded) return "coverage-posture--degraded";
            if (hasNominal) return "coverage-posture--nominal";
            return "coverage-posture--unknown";
        }

        private static void ApplySeverityClass(VisualElement element, string activeClass)
        {
            for (var i = 0; i < PostureSeverityClasses.Length; i++)
            {
                element.RemoveFromClassList(PostureSeverityClasses[i]);
            }

            element.AddToClassList(activeClass);
        }

        private static void ApplyIgnorePicking(VisualElement root)
        {
            root.pickingMode = PickingMode.Ignore;
            foreach (var child in root.Children())
            {
                ApplyIgnorePicking(child);
            }
        }
    }
}
#endif
