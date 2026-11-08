// Mission list left-drawer tab — UI Toolkit panel bound to DelegationBridgeHost.
#if UNITY_5_3_OR_NEWER
using System;
using ProjectAegis.Delegation.C2Nodes;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MissionListPanelHost : MonoBehaviour
    {
        private const string RootName = "mission-list-root";
        private const string ListName = "mission-list";
        private const string PackageListName = "mp-package-list";
        private const string ElementListName = "mp-element-list";
        private const string ActivePackageName = "mp-active-package-line";
        private const string SummaryName = "mp-summary-line";
        private const string NextActionName = "mp-next-action-line";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private ListView? _missionList;
        private ListView? _packageList;
        private ListView? _elementList;
        private Label? _activePackageLine;
        private Label? _summaryLine;
        private Label? _nextActionLine;
        private MissionListPanelState _panelState = new(Array.Empty<MissionListDisplayRow>());
        private MissionPackagePresentation _packagePresentation = MissionPackagePresentation.Empty;
        private MissionPackageSnapshot? _lastPackageSnapshot;
        private string? _lastUnitId;
        private string? _lastPackageFingerprint;
        private bool _wired;

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

            _missionList = root.Q<ListView>(ListName);
            _packageList = root.Q<ListView>(PackageListName);
            _elementList = root.Q<ListView>(ElementListName);
            _activePackageLine = root.Q<Label>(ActivePackageName);
            _summaryLine = root.Q<Label>(SummaryName);
            _nextActionLine = root.Q<Label>(NextActionName);
            if (_missionList != null)
            {
                _missionList.makeItem = () => new Label();
                _missionList.bindItem = (element, index) =>
                {
                    if (element is Label label && index >= 0 && index < _panelState.MissionRows.Count)
                    {
                        label.text = _panelState.MissionRows[index].DisplayLine;
                    }
                };
            }

            if (_packageList != null)
            {
                _packageList.makeItem = () => new Label();
                _packageList.bindItem = (element, index) =>
                {
                    if (element is Label label
                        && index >= 0
                        && index < _packagePresentation.Packages.Count)
                    {
                        label.text = _packagePresentation.Packages[index].SummaryLine;
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
                        && index < _packagePresentation.Elements.Count)
                    {
                        label.text = _packagePresentation.Elements[index].DisplayLine;
                    }
                };
            }

            _wired = _missionList != null;
        }

        private void Refresh()
        {
            if (bridgeHost == null || _missionList == null)
            {
                return;
            }

            _panelState = MissionListPanelBinder.Bind(bridgeHost.LastMissionList);
            _missionList.itemsSource = _panelState.MissionRows.ToList();
            _missionList.Rebuild();
            RefreshPackageChrome();

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void RefreshPackageChrome()
        {
            if (_packageList == null || _elementList == null)
            {
                return;
            }

            var unitId = bridgeHost.SelectedUnitId;
            var snapshot = bridgeHost.LastMissionPackage;
            var fingerprint = MissionPackageProjection.ComputeFingerprint(snapshot);
            if (ReferenceEquals(snapshot, _lastPackageSnapshot)
                && string.Equals(unitId, _lastUnitId, StringComparison.Ordinal)
                && string.Equals(fingerprint, _lastPackageFingerprint, StringComparison.Ordinal))
            {
                return;
            }

            _lastPackageSnapshot = snapshot;
            _lastUnitId = unitId;
            _lastPackageFingerprint = fingerprint;
            _packagePresentation = MissionPackagePresenter.Build(snapshot, unitId);
            var labels = MissionPackagePanelBinder.Bind(_packagePresentation);
            if (_activePackageLine != null)
            {
                _activePackageLine.text = labels.ActivePackageLine;
            }

            if (_summaryLine != null)
            {
                _summaryLine.text = labels.SummaryLine;
            }

            if (_nextActionLine != null)
            {
                _nextActionLine.text = labels.NextActionLine;
            }

            _packageList.itemsSource = labels.PackageLines.ToList();
            _packageList.Rebuild();
            _elementList.itemsSource = labels.ElementLines.ToList();
            _elementList.Rebuild();
        }
    }
}
#endif
