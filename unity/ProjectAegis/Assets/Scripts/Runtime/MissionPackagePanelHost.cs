// DRG-189: Mission-package / composable C2-node inspection chrome — presentation-only.
#if UNITY_5_3_OR_NEWER
using System;
using ProjectAegis.Delegation.C2Nodes;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MissionPackagePanelHost : MonoBehaviour
    {
        private const string RootName = "mission-package-root";
        private const string AdvisoryName = "mp-advisory-line";
        private const string ActivePackageName = "mp-active-package-line";
        private const string SummaryName = "mp-summary-line";
        private const string PackageListName = "mp-package-list";
        private const string ElementListName = "mp-element-list";
        private const string NextActionName = "mp-next-action-line";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private VisualElement? _panel;
        private Label? _advisoryLine;
        private Label? _activePackageLine;
        private Label? _summaryLine;
        private ListView? _packageList;
        private ListView? _elementList;
        private Label? _nextActionLine;
        private MissionPackageSnapshot? _lastSnapshot;
        private string? _lastUnitId;
        private string? _lastFingerprint;
        private MissionPackagePresentation _presentation = MissionPackagePresentation.Empty;
        private bool _wired;

        /// <summary>Last applied mission-package presentation (DRG-189).</summary>
        public MissionPackagePresentation LastPresentation => _presentation;

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
            _lastSnapshot = null;
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
            _advisoryLine = panel.Q<Label>(AdvisoryName);
            _activePackageLine = panel.Q<Label>(ActivePackageName);
            _summaryLine = panel.Q<Label>(SummaryName);
            _packageList = panel.Q<ListView>(PackageListName);
            _elementList = panel.Q<ListView>(ElementListName);
            _nextActionLine = panel.Q<Label>(NextActionName);
            if (_packageList != null)
            {
                _packageList.makeItem = () => new Label();
                _packageList.bindItem = (element, index) =>
                {
                    if (element is Label label
                        && index >= 0
                        && index < _presentation.Packages.Count)
                    {
                        label.text = _presentation.Packages[index].SummaryLine;
                    }
                };
            }

            if (_elementList != null)
            {
                _elementList.makeItem = () => new Label();
                _elementList.bindItem = (element, index) =>
                {
                    if (element is Label label
                        && index >= 0
                        && index < _presentation.Elements.Count)
                    {
                        label.text = _presentation.Elements[index].DisplayLine;
                    }
                };
            }

            _wired = _activePackageLine != null && _packageList != null && _elementList != null;

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }
        }

        /// <summary>Apply projected package state directly (headless / test path).</summary>
        public void Apply(MissionPackagePresentation presentation)
        {
            _presentation = presentation ?? MissionPackagePresentation.Empty;
            _lastSnapshot = null;
            _lastFingerprint = _presentation.Fingerprint;
            ApplyPresentationToLabels();
        }

        private void Refresh()
        {
            if (!_wired)
            {
                return;
            }

            var unitId = bridgeHost == null ? null : bridgeHost.SelectedUnitId;
            if (_panel != null)
            {
                _panel.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }

            var snapshot = bridgeHost == null
                ? MissionPackageSnapshot.Empty
                : bridgeHost.LastMissionPackage;
            var fingerprint = MissionPackageProjection.ComputeFingerprint(snapshot);
            if (ReferenceEquals(snapshot, _lastSnapshot)
                && string.Equals(unitId, _lastUnitId, StringComparison.Ordinal)
                && string.Equals(fingerprint, _lastFingerprint, StringComparison.Ordinal))
            {
                return;
            }

            _lastSnapshot = snapshot;
            _lastUnitId = unitId;
            _lastFingerprint = fingerprint;
            _presentation = MissionPackagePresenter.Build(snapshot, unitId);
            ApplyPresentationToLabels();
        }

        private void ApplyPresentationToLabels()
        {
            var labels = MissionPackagePanelBinder.Bind(_presentation);
            if (_advisoryLine != null)
            {
                _advisoryLine.text = labels.AdvisoryBadge;
            }

            if (_activePackageLine != null)
            {
                _activePackageLine.text = labels.ActivePackageLine;
            }

            if (_summaryLine != null)
            {
                _summaryLine.text = labels.SummaryLine;
            }

            if (_packageList != null)
            {
                _packageList.itemsSource = labels.PackageLines.ToList();
                _packageList.Rebuild();
            }

            if (_elementList != null)
            {
                _elementList.itemsSource = labels.ElementLines.ToList();
                _elementList.Rebuild();
            }

            if (_nextActionLine != null)
            {
                _nextActionLine.text = labels.NextActionLine;
            }
        }
    }
}
#endif
