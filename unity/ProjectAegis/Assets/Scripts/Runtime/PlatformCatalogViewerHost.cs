// S27-08/15: ADR-011 Phase C read-only platform catalog browse with search/filter + detail pane (no write-gate bypass).
#if UNITY_5_3_OR_NEWER
using System.Linq;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class PlatformCatalogViewerHost : MonoBehaviour
    {
        private const string RootName = "platform-catalog-root";
        private const string SearchName = "platform-catalog-search";
        private const string ListName = "platform-catalog-list";
        private const string DetailLatName = "platform-catalog-detail-lat";
        private const string DetailLonName = "platform-catalog-detail-lon";
        private const string DetailRadiusName = "platform-catalog-detail-radius";
        private const string DetailHpName = "platform-catalog-detail-hp";
        private const string DetailSpeedName = "platform-catalog-detail-speed";

        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private ListView? _platformList;
        private TextField? _searchField;
        private Label? _detailLat;
        private Label? _detailLon;
        private Label? _detailRadius;
        private Label? _detailHp;
        private Label? _detailSpeed;
        private IReadOnlyList<CatalogPlatformBrowseRow> _allRows = Array.Empty<CatalogPlatformBrowseRow>();
        private List<CatalogPlatformBrowseRow> _filteredRows = new();
        private List<string> _displayItems = new();
        private bool _wired;

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
            RefreshList();
            ApplyPanelVisibility();
        }

        public void BindRows(IReadOnlyList<CatalogPlatformBrowseRow> rows)
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            _allRows = rows;
            TryWireElements();
            RefreshList();
            ApplyPanelVisibility();
        }

        public void BindReader(ICatalogReader reader) =>
            BindRows(CatalogPlatformBrowseProjection.FromReader(reader));

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            if (_platformList == null)
            {
                _platformList = root.Q<ListView>(ListName);
                if (_platformList != null)
                {
                    _platformList.makeItem = () => new Label();
                    _platformList.bindItem = (element, index) =>
                    {
                        if (element is Label label && index >= 0 && index < _displayItems.Count)
                        {
                            label.text = _displayItems[index];
                        }
                    };
                    _platformList.selectionChanged += OnSelectionChanged;
                }
            }

            if (_searchField == null)
            {
                _searchField = root.Q<TextField>(SearchName);
                if (_searchField != null)
                {
                    _searchField.RegisterValueChangedCallback(_ => RefreshList());
                }
            }

            _detailLat ??= root.Q<Label>(DetailLatName);
            _detailLon ??= root.Q<Label>(DetailLonName);
            _detailRadius ??= root.Q<Label>(DetailRadiusName);
            _detailHp ??= root.Q<Label>(DetailHpName);
            _detailSpeed ??= root.Q<Label>(DetailSpeedName);

            _wired = _platformList != null;
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            if (_platformList == null)
            {
                return;
            }

            var index = _platformList.selectedIndex;
            var row = index >= 0 && index < _filteredRows.Count
                ? _filteredRows[index]
                : null;
            BindDetail(row);
        }

        private void BindDetail(CatalogPlatformBrowseRow? row)
        {
            var detail = PlatformCatalogDetailProjection.Format(row);
            if (_detailLat != null)
            {
                _detailLat.text = detail.LatLabel;
            }

            if (_detailLon != null)
            {
                _detailLon.text = detail.LonLabel;
            }

            if (_detailRadius != null)
            {
                _detailRadius.text = detail.CombatRadiusLabel;
            }

            if (_detailHp != null)
            {
                _detailHp.text = detail.MaxHpLabel;
            }

            if (_detailSpeed != null)
            {
                _detailSpeed.text = detail.MaxSpeedLabel;
            }
        }

        private void RefreshList()
        {
            _filteredRows = PlatformCatalogFilterProjection.Apply(_allRows, _searchField?.value).ToList();
            _displayItems = _filteredRows
                .Select(r => $"{r.PlatformId} hp={r.MaxHp} speed={r.MaxSpeedKnots} mounts={r.MountCount} sensors={r.SensorCount}")
                .ToList();

            if (_platformList != null)
            {
                _platformList.itemsSource = _displayItems;
                _platformList.ClearSelection();
                _platformList.Rebuild();
            }

            BindDetail(null);
        }

        private void ApplyPanelVisibility()
        {
            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
#endif