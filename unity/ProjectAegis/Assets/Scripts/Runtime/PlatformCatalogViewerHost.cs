// S27-08/15 + S28-07: ADR-011 Phase C read-only platform catalog browse with search/filter + detail pane
// and read-only export/diff triggers (no write-gate bypass; import/write deferred to CLI).
// S36-07 Phase H link surfacing (read-only): FK links shown for selected platform (UI + data read via comms).
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class PlatformCatalogViewerHost : MonoBehaviour
    {
        private const string RootName = "platform-catalog-root";
        private const string ExportButtonName = "platform-catalog-export";
        private const string DiffButtonName = "platform-catalog-diff";
        private const string SearchName = "platform-catalog-search";
        private const string ListName = "platform-catalog-list";
        private const string DetailLatName = "platform-catalog-detail-lat";
        private const string DetailLonName = "platform-catalog-detail-lon";
        private const string DetailRadiusName = "platform-catalog-detail-radius";
        private const string DetailHpName = "platform-catalog-detail-hp";
        private const string DetailResilienceName = "platform-catalog-detail-resilience";
        private const string DetailWithdrawName = "platform-catalog-detail-withdraw";
        private const string DetailFlagsName = "platform-catalog-detail-flags";
        private const string DetailSpeedName = "platform-catalog-detail-speed";
        private const string CommsListName = "platform-catalog-comms-list";
        private const string LinksListName = "platform-catalog-links-list";

        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private string databasePathForExport = string.Empty;
        [SerializeField] private string exportOutFileName = "platform-catalog-export.platform.txt";

        private UIDocument _document = null!;
        private Button? _exportButton;
        private Button? _diffButton;
        private ListView? _platformList;
        private TextField? _searchField;
        private string? _boundDatabasePath;
        private string _boundSnapshotId = CatalogValidationDefaults.BalticSnapshotId;
        private Label? _detailLat;
        private Label? _detailLon;
        private Label? _detailRadius;
        private Label? _detailHp;
        private Label? _detailResilience;
        private Label? _detailWithdraw;
        private Label? _detailFlags;
        private Label? _detailSpeed;
        private ListView? _commsList;
        private ListView? _linksList;
        private IReadOnlyList<CatalogPlatformBrowseRow> _allRows = Array.Empty<CatalogPlatformBrowseRow>();
        private IReadOnlyList<CatalogCommsBinding> _allComms = Array.Empty<CatalogCommsBinding>();
        private IReadOnlyList<CatalogLinkEntry> _allLinks = Array.Empty<CatalogLinkEntry>();
        private IReadOnlyDictionary<string, string> _linkDisplayNames = new Dictionary<string, string>();
        // S37-05: full graph surfacing (FK + dependency chains) for interactive display
        private IReadOnlyList<CatalogDependencyEdge> _allGraphEdges = Array.Empty<CatalogDependencyEdge>();
        private List<string> _graphDisplayItems = new();
        private List<CatalogPlatformBrowseRow> _filteredRows = new();
        private List<string> _displayItems = new();
        private List<string> _commsDisplayItems = new();
        private List<string> _linksDisplayItems = new();
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

        public void BindRows(
            IReadOnlyList<CatalogPlatformBrowseRow> rows,
            IReadOnlyList<CatalogCommsBinding>? comms = null,
            IReadOnlyList<CatalogLinkEntry>? links = null,
            IReadOnlyList<CatalogDependencyEdge>? graphEdges = null)
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            _allRows = rows;
            _allComms = comms ?? Array.Empty<CatalogCommsBinding>();
            _allLinks = links ?? Array.Empty<CatalogLinkEntry>();
            _linkDisplayNames = CatalogLinkListProjection.BuildDisplayNameLookup(_allLinks);
            // S37-05: FK/full graph display
            _allGraphEdges = graphEdges ?? Array.Empty<CatalogDependencyEdge>();
            TryWireElements();
            RefreshList();
            ApplyPanelVisibility();
        }

        public void BindReader(ICatalogReader reader) =>
            BindRows(
                CatalogPlatformBrowseProjection.FromReader(reader),
                reader.GetSortedComms(),
                CatalogLinkListProjection.FromReader(reader),
                reader.GetSortedDependencyEdges());  // S37-05 full graph surfacing

        public void BindExportContext(string databasePath, string? snapshotId = null)
        {
            _boundDatabasePath = databasePath;
            _boundSnapshotId = string.IsNullOrWhiteSpace(snapshotId)
                ? CatalogValidationDefaults.BalticSnapshotId
                : snapshotId;
            TryWireElements();
        }

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
                            // S37-05: tooltips for FK/graph surfacing (interactive display)
                            label.tooltip = $"Platform row: {_displayItems[index]} — graph/FK details in links+graph panes (read-only)";
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

            if (_exportButton == null)
            {
                _exportButton = root.Q<Button>(ExportButtonName);
                if (_exportButton != null)
                {
                    _exportButton.clicked -= OnExportClicked;
                    _exportButton.clicked += OnExportClicked;
                }
            }

            if (_diffButton == null)
            {
                _diffButton = root.Q<Button>(DiffButtonName);
                if (_diffButton != null)
                {
                    _diffButton.clicked -= OnDiffClicked;
                    _diffButton.clicked += OnDiffClicked;
                }
            }

            _detailLat ??= root.Q<Label>(DetailLatName);
            _detailLon ??= root.Q<Label>(DetailLonName);
            _detailRadius ??= root.Q<Label>(DetailRadiusName);
            _detailHp ??= root.Q<Label>(DetailHpName);
            _detailResilience ??= root.Q<Label>(DetailResilienceName);
            _detailWithdraw ??= root.Q<Label>(DetailWithdrawName);
            _detailFlags ??= root.Q<Label>(DetailFlagsName);
            _detailSpeed ??= root.Q<Label>(DetailSpeedName);

            if (_commsList == null)
            {
                _commsList = root.Q<ListView>(CommsListName);
                if (_commsList != null)
                {
                    _commsList.makeItem = () => new Label();
                    _commsList.bindItem = (element, index) =>
                    {
                        if (element is Label label && index >= 0 && index < _commsDisplayItems.Count)
                        {
                            label.text = _commsDisplayItems[index];
                        }
                    };
                }
            }

            if (_linksList == null)
            {
                _linksList = root.Q<ListView>(LinksListName);
                if (_linksList != null)
                {
                    _linksList.makeItem = () => new Label();
                    _linksList.bindItem = (element, index) =>
                    {
                        if (element is Label label && index >= 0 && index < _linksDisplayItems.Count)
                        {
                            label.text = _linksDisplayItems[index];
                        }
                    };
                }
            }

            _wired = _platformList != null;
        }

        private string? ResolveDatabasePath()
        {
            if (!string.IsNullOrWhiteSpace(_boundDatabasePath))
            {
                return _boundDatabasePath;
            }

            return string.IsNullOrWhiteSpace(databasePathForExport) ? null : databasePathForExport;
        }

        private void OnExportClicked()
        {
            var databasePath = ResolveDatabasePath();
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                Debug.LogWarning("[PlatformCatalogViewerHost] Export skipped: no database path bound (use CLI platform_export_xlsx).");
                return;
            }

            try
            {
                var outPath = Path.Combine(Application.persistentDataPath, exportOutFileName);
                PlatformCatalogExportBridge.ExportToFile(
                    databasePath,
                    _boundSnapshotId,
                    outPath,
                    clockTicks: 0);
                Debug.Log(
                    $"[PlatformCatalogViewerHost] Export ok snapshot={_boundSnapshotId} out={outPath} " +
                    "(canonical text; use CLI platform_export_xlsx for .xlsx).");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlatformCatalogViewerHost] Export failed: {ex.Message}");
            }
        }

        private void OnDiffClicked()
        {
            var databasePath = ResolveDatabasePath();
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                Debug.LogWarning("[PlatformCatalogViewerHost] Diff skipped: no database path bound.");
                return;
            }

            try
            {
                var changes = PlatformCatalogExportBridge.DiffUneditedRoundTrip(
                    databasePath,
                    _boundSnapshotId,
                    clockTicks: 0);
                Debug.Log(
                    $"[PlatformCatalogViewerHost] Diff ok snapshot={_boundSnapshotId} diffCount={changes.Count} " +
                    "(read-only; propose/approve remains CLI/Phase D).");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlatformCatalogViewerHost] Diff failed: {ex.Message}");
            }
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
            BindComms(row?.PlatformId);
            BindLinks(row?.PlatformId);
            BindGraph(row?.PlatformId);  // S37-05 full graph + FK
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

            if (_detailResilience != null)
            {
                _detailResilience.text = detail.ResilienceLabel;
            }

            if (_detailWithdraw != null)
            {
                _detailWithdraw.text = detail.WithdrawThresholdLabel;
            }

            if (_detailFlags != null)
            {
                _detailFlags.text = detail.CriticalFlagsLabel;
            }

            if (_detailSpeed != null)
            {
                _detailSpeed.text = detail.MaxSpeedLabel;
            }
        }

        private void BindComms(string? platformId)
        {
            var fittings = string.IsNullOrWhiteSpace(platformId)
                ? Array.Empty<CatalogCommsBinding>()
                : CatalogPlatformCommsProjection.ForPlatform(_allComms, platformId);
            _commsDisplayItems = PlatformCommsListProjection
                .FormatRows(fittings, _linkDisplayNames)
                .ToList();

            if (_commsList != null)
            {
                _commsList.itemsSource = _commsDisplayItems;
                _commsList.Rebuild();
            }
        }

        private void BindLinks(string? platformId)
        {
            // S36-07 Phase H link surfacing (read-only): when platform selected, show only its FK links from comms bindings;
            // global LinkCatalog when null (refresh/clear). Uses existing data read via _allLinks + _allComms (from ICatalogReader).
            IReadOnlyList<CatalogLinkEntry> toShow;
            if (string.IsNullOrWhiteSpace(platformId))
            {
                toShow = _allLinks;
            }
            else
            {
                var usedLinkIds = _allComms
                    .Where(c => string.Equals(c.PlatformId, platformId, StringComparison.Ordinal))
                    .Select(c => c.LinkId)
                    .ToHashSet(StringComparer.Ordinal);
                toShow = _allLinks
                    .Where(l => usedLinkIds.Contains(l.LinkId))
                    .ToList();
            }

            _linksDisplayItems = PlatformLinkListProjection.FormatRows(toShow).ToList();

            if (_linksList != null)
            {
                _linksList.itemsSource = _linksDisplayItems;
                _linksList.Rebuild();
            }
        }

        // S37-05: Platform Editor graph surfacing — interactive FK/full graph (beyond S36 Phase H read-only), tooltips, export polish
        private void BindGraph(string? platformId)
        {
            _graphDisplayItems.Clear();
            var edges = string.IsNullOrWhiteSpace(platformId)
                ? _allGraphEdges.Take(20).ToList() // cap for display
                : _allGraphEdges.Where(e => string.Equals(e.PlatformId, platformId, StringComparison.Ordinal)).ToList();

            foreach (var e in edges)
            {
                // S37-05: format inline (mirrors CatalogDependencyGraphCommand for display)
                string line = e.Kind switch
                {
                    CatalogDependencyEdgeKind.PlatformToSensor => $"sensor:{e.PlatformId}:{e.SensorId}",
                    CatalogDependencyEdgeKind.PlatformToMountToWeapon => $"weapon:{e.PlatformId}:{e.MountId}:{e.WeaponId}",
                    CatalogDependencyEdgeKind.PlatformToLink => $"link:{e.PlatformId}:{e.LinkId}:{e.CommsFittingId}",
                    _ => $"edge:{e.PlatformId}"
                };
                _graphDisplayItems.Add(line);
            }
            if (_graphDisplayItems.Count == 0 && !string.IsNullOrWhiteSpace(platformId))
            {
                _graphDisplayItems.Add($"(no graph edges for {platformId})");
            }
        }

        private void RefreshList()
        {
            _filteredRows = PlatformCatalogFilterProjection.Apply(_allRows, _searchField?.value).ToList();
            _displayItems = _filteredRows
                .Select(PlatformCatalogListProjection.FormatRow)
                .ToList();

            if (_platformList != null)
            {
                _platformList.itemsSource = _displayItems;
                _platformList.ClearSelection();
                _platformList.Rebuild();
            }

            BindDetail(null);
            BindComms(null);
            BindLinks(null);
            BindGraph(null);  // S37-05
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