// CMD-24 Phase A Air Operations readiness panel — UI Toolkit bound to DelegationBridgeHost.
// Phase N (LOG-08 timers / launch / abort FSM) is intentionally out of scope.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AirOpsPanelHost : MonoBehaviour
    {
        private const string RootName = "air-ops-root";
        private const string HeaderName = "air-ops-header";
        private const string EmptyName = "air-ops-empty";
        private const string ListName = "air-ops-list";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _headerLine;
        private Label? _emptyLine;
        private ListView? _assetList;
        private AirOpsPresentation _presentation = AirOpsPresentation.Empty;
        private bool _wired;

        /// <summary>Last applied Air Ops presentation (CMD-24 Phase A).</summary>
        public AirOpsPresentation LastPresentation => _presentation;

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

            // Prefer host-maintained list; rebuild from Session + OOB when needed.
            bridgeHost.RefreshAirOps();
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
            _headerLine = panel.Q<Label>(HeaderName);
            _emptyLine = panel.Q<Label>(EmptyName);
            _assetList = panel.Q<ListView>(ListName);

            if (_assetList != null)
            {
                _assetList.makeItem = () => new Label();
                _assetList.bindItem = (element, index) =>
                {
                    if (element is Label label && index >= 0 && index < _presentation.Lines.Count)
                    {
                        label.text = _presentation.Lines[index];
                    }
                };
                _assetList.selectionType = SelectionType.Single;
                _assetList.selectionChanged -= OnAssetSelectionChanged;
                _assetList.selectionChanged += OnAssetSelectionChanged;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            _wired = _headerLine != null || _assetList != null;
        }

        private void OnAssetSelectionChanged(IEnumerable<object> _)
        {
            if (_assetList == null || bridgeHost == null)
            {
                return;
            }

            var index = _assetList.selectedIndex;
            if (index < 0 || index >= _presentation.Rows.Count)
            {
                return;
            }

            bridgeHost.SelectUnit(_presentation.Rows[index].UnitId);
        }

        /// <summary>Apply presentation via headless apply-state (tests / direct bind).</summary>
        public void ApplyPresentation(AirOpsPresentation presentation)
        {
            _presentation = presentation ?? AirOpsPresentation.Empty;
            ApplyPresentationToUi();
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null)
            {
                return;
            }

            _presentation = AirOpsApplyState.Apply(
                bridgeHost.LastAirOps,
                bridgeHost.HasAirOpsReadinessData);

            ApplyPresentationToUi();

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ApplyPresentationToUi()
        {
            if (_headerLine != null)
            {
                _headerLine.text = _presentation.HeaderLine;
            }

            if (_emptyLine != null)
            {
                _emptyLine.text = _presentation.EmptyStateLine ?? string.Empty;
                _emptyLine.style.display = string.IsNullOrEmpty(_presentation.EmptyStateLine)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }

            if (_assetList != null)
            {
                _assetList.itemsSource = _presentation.Lines as System.Collections.IList
                    ?? new List<string>(_presentation.Lines);
                _assetList.Rebuild();
            }
        }
    }
}
#endif
