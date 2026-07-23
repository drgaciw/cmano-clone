// S27-08/15 + S28-07: ADR-011 Phase C read-only platform catalog browse with search/filter + detail pane
// and read-only export/diff triggers (no write-gate bypass; import/write deferred to CLI).
// S36-07 Phase H link surfacing (read-only): FK links shown for selected platform (UI + data read via comms).
// S38-04: residual C2/Platform Editor polish (filters/tooltips/density S37-13/06 carry). All per polish-scope-boundary-2026-06-19.md + sprint-38 + qa-plan-s38; lean; no new scope.
// PE-UX-W2: section bar, graph ListView bind, mounts/sensors detail, Export/Diff status, demoted scenario Lat/Lon.
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
        private enum CatalogSection
        {
            Identity,
            Damage,
            Fits,
            Comms,
            Links,
            Graph,
        }

        private const string RootName = "platform-catalog-root";
        private const string ExportButtonName = "platform-catalog-export";
        private const string DiffButtonName = "platform-catalog-diff";
        private const string ActionStatusName = "platform-catalog-action-status";
        private const string SearchName = "platform-catalog-search";
        private const string ListName = "platform-catalog-list";
        private const string DetailIdName = "platform-catalog-detail-id";
        private const string DetailLatName = "platform-catalog-detail-lat";
        private const string DetailLonName = "platform-catalog-detail-lon";
        private const string DetailRadiusName = "platform-catalog-detail-radius";
        private const string DetailHpName = "platform-catalog-detail-hp";
        private const string DetailResilienceName = "platform-catalog-detail-resilience";
        private const string DetailWithdrawName = "platform-catalog-detail-withdraw";
        private const string DetailFlagsName = "platform-catalog-detail-flags";
        private const string DetailSpeedName = "platform-catalog-detail-speed";
        private const string DetailMountsName = "platform-catalog-detail-mounts";
        private const string DetailSensorsName = "platform-catalog-detail-sensors";
        private const string DetailPaneName = "platform-catalog-detail";
        private const string CommsPaneName = "platform-catalog-comms";
        private const string LinksPaneName = "platform-catalog-links";
        private const string GraphPaneName = "platform-catalog-graph";
        private const string CommsListName = "platform-catalog-comms-list";
        private const string LinksListName = "platform-catalog-links-list";
        private const string GraphListName = "platform-catalog-graph-list";
        private const string GraphSearchName = "platform-catalog-graph-search";
        private const string SectionBarName = "platform-catalog-section-bar";
        private const string SectionActiveClass = "platform-catalog-section-button--active";

        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private string databasePathForExport = string.Empty;
        [SerializeField] private string exportOutFileName = "platform-catalog-export.platform.txt";
        [SerializeField] private C2AccessibilityScalePercent accessibilityScale = C2AccessibilityScalePercent.OneHundred;
        [SerializeField] private bool reducedMotion = true;

        /// <summary>PE-UX-W5: curator asks to jump from a comms row into Links for the selected platform.</summary>
        public event System.Action<string?>? CommsLinkNavigateRequested;

        private UIDocument _document = null!;
        private Button? _exportButton;
        private Button? _diffButton;
        private Label? _actionStatus;
        private ListView? _platformList;
        private TextField? _searchField;
        private TextField? _graphSearchField;
        private VisualElement? _sectionBar;
        private string _graphSearch = string.Empty;
        private string? _selectedPlatformId;
        private string? _boundDatabasePath;
        private string _boundSnapshotId = CatalogValidationDefaults.BalticSnapshotId;
        private Label? _detailId;
        private Label? _detailLat;
        private Label? _detailLon;
        private Label? _detailRadius;
        private Label? _detailHp;
        private Label? _detailResilience;
        private Label? _detailWithdraw;
        private Label? _detailFlags;
        private Label? _detailSpeed;
        private Label? _detailMounts;
        private Label? _detailSensors;
        private VisualElement? _detailPane;
        private VisualElement? _commsPane;
        private VisualElement? _linksPane;
        private VisualElement? _graphPane;
        private ListView? _commsList;
        private ListView? _linksList;
        private ListView? _graphList;
        private Button? _sectionIdentity;
        private Button? _sectionDamage;
        private Button? _sectionFits;
        private Button? _sectionComms;
        private Button? _sectionLinks;
        private Button? _sectionGraph;
        private CatalogSection _activeSection = CatalogSection.Identity;
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

        /// <summary>Edge count for shell health strip (read-only).</summary>
        public int DependencyEdgeCount => _allGraphEdges.Count;

        public string? SelectedPlatformId => _selectedPlatformId;

        /// <summary>PE-UX-W4: shell toggles visibility without disabling the host (state preserved).</summary>
        public void SetShellVisible(bool visible)
        {
            showPanel = visible;
            ApplyPanelVisibility();
        }

        /// <summary>PE-UX-W5: select platform and open Links section (comms → link jump).</summary>
        public void NavigateToLinksSection(string? platformId)
        {
            SelectPlatform(platformId);
            _activeSection = CatalogSection.Links;
            ApplySectionVisibility();
            BindLinks(platformId);
            BindGraph(platformId);
        }

        public void SelectPlatform(string? platformId)
        {
            _selectedPlatformId = platformId;
            if (_platformList == null || string.IsNullOrWhiteSpace(platformId))
            {
                return;
            }

            var index = _filteredRows.FindIndex(r =>
                string.Equals(r.PlatformId, platformId, System.StringComparison.Ordinal));
            if (index >= 0)
            {
                _platformList.SetSelection(index);
            }
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            ApplyAccessibility(root);

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
                            // S37-05 + S38-04 + S39-03 residual polish (filters, tooltips, density from S37-13/S37-06 carry; deeper tooltip UX).
                            // Richer tooltip for info density (includes full formatted row + graph note). Cite boundary.
                            // Cite: production/sprints/sprint-39-deeper-polish-c2-platform-hygiene.md + qa-plan-sprint-39-2026-06-20.md + polish-scope-boundary-2026-06-19.md + S37/S38.
                            // Headless primary; lean PNG evidence; C2 proxy/Graph* / frame no regression; ZERO DelegationBridge; extend-only. C2 18/18+ maintained conceptually.
                            var rowInfo = index < _filteredRows.Count ? PlatformCatalogListProjection.FormatRow(_filteredRows[index]) : _displayItems[index];
                            label.tooltip = $"ID/row (hp=/res=/speed= for density): {rowInfo} | graph/FK in links+graph panes (read-only; S39-03 polish per polish-scope-boundary-2026-06-19.md)";
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
                    // S39-03: tooltip for density filter UX (search on ID + formatted row content); extend-only per polish-scope-boundary-2026-06-19.md; C2/Platform polish track.
                    _searchField.tooltip = "Filter platforms by ID or row (e.g. hp=100, speed=). Supports density/search polish (S39-03).";
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

            _actionStatus ??= root.Q<Label>(ActionStatusName);
            _detailId ??= root.Q<Label>(DetailIdName);
            _detailLat ??= root.Q<Label>(DetailLatName);
            _detailLon ??= root.Q<Label>(DetailLonName);
            _detailRadius ??= root.Q<Label>(DetailRadiusName);
            _detailHp ??= root.Q<Label>(DetailHpName);
            _detailResilience ??= root.Q<Label>(DetailResilienceName);
            _detailWithdraw ??= root.Q<Label>(DetailWithdrawName);
            _detailFlags ??= root.Q<Label>(DetailFlagsName);
            _detailSpeed ??= root.Q<Label>(DetailSpeedName);
            _detailMounts ??= root.Q<Label>(DetailMountsName);
            _detailSensors ??= root.Q<Label>(DetailSensorsName);
            _detailPane ??= root.Q(DetailPaneName);
            _commsPane ??= root.Q(CommsPaneName);
            _linksPane ??= root.Q(LinksPaneName);
            _graphPane ??= root.Q(GraphPaneName);

            WireSectionButton(ref _sectionIdentity, "platform-catalog-section-identity", CatalogSection.Identity);
            WireSectionButton(ref _sectionDamage, "platform-catalog-section-damage", CatalogSection.Damage);
            WireSectionButton(ref _sectionFits, "platform-catalog-section-fits", CatalogSection.Fits);
            WireSectionButton(ref _sectionComms, "platform-catalog-section-comms", CatalogSection.Comms);
            WireSectionButton(ref _sectionLinks, "platform-catalog-section-links", CatalogSection.Links);
            WireSectionButton(ref _sectionGraph, "platform-catalog-section-graph", CatalogSection.Graph);

            if (_commsList == null)
            {
                _commsList = root.Q<ListView>(CommsListName);
                if (_commsList != null)
                {
                    _commsList.selectionType = SelectionType.Single;
                    _commsList.makeItem = () => new Label();
                    _commsList.bindItem = (element, index) =>
                    {
                        if (element is Label label && index >= 0 && index < _commsDisplayItems.Count)
                        {
                            label.text = _commsDisplayItems[index];
                        }
                    };
                    _commsList.selectionChanged += _ =>
                    {
                        // PE-UX-W5: any comms row click jumps to Links for the selected platform.
                        CommsLinkNavigateRequested?.Invoke(_selectedPlatformId);
                        NavigateToLinksSection(_selectedPlatformId);
                    };
                }
            }

            if (_graphSearchField == null)
            {
                _graphSearchField = root.Q<TextField>(GraphSearchName);
                if (_graphSearchField != null)
                {
                    _graphSearchField.RegisterValueChangedCallback(evt =>
                    {
                        _graphSearch = evt.newValue ?? string.Empty;
                        BindGraph(_selectedPlatformId);
                    });
                }
            }

            if (_sectionBar == null)
            {
                _sectionBar = root.Q(SectionBarName);
                if (_sectionBar != null)
                {
                    _sectionBar.focusable = true;
                    _sectionBar.RegisterCallback<KeyDownEvent>(OnSectionBarKeyDown);
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

            if (_graphList == null)
            {
                _graphList = root.Q<ListView>(GraphListName);
                if (_graphList != null)
                {
                    _graphList.makeItem = () => new Label();
                    _graphList.bindItem = (element, index) =>
                    {
                        if (element is Label label && index >= 0 && index < _graphDisplayItems.Count)
                        {
                            label.text = _graphDisplayItems[index];
                        }
                    };
                }
            }

            ApplySectionVisibility();
            _wired = _platformList != null;
        }

        private void OnSectionBarKeyDown(KeyDownEvent evt)
        {
            var order = new[]
            {
                CatalogSection.Identity,
                CatalogSection.Damage,
                CatalogSection.Fits,
                CatalogSection.Comms,
                CatalogSection.Links,
                CatalogSection.Graph,
            };
            var index = System.Array.IndexOf(order, _activeSection);
            if (evt.keyCode == KeyCode.RightArrow)
            {
                index = (index + 1) % order.Length;
                _activeSection = order[index];
                ApplySectionVisibility();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.LeftArrow)
            {
                index = (index - 1 + order.Length) % order.Length;
                _activeSection = order[index];
                ApplySectionVisibility();
                evt.StopPropagation();
            }
        }

        private void ApplyAccessibility(VisualElement root)
        {
            var settings = new C2AccessibilitySettings(accessibilityScale, reducedMotion);
            root.EnableInClassList("aegis-scale-100", settings.ScalePercent == C2AccessibilityScalePercent.OneHundred);
            root.EnableInClassList("aegis-scale-125", settings.ScalePercent == C2AccessibilityScalePercent.OneTwentyFive);
            root.EnableInClassList("aegis-scale-150", settings.ScalePercent == C2AccessibilityScalePercent.OneFifty);
            root.EnableInClassList("reduced-motion", settings.ReducedMotion);
        }

        private void WireSectionButton(ref Button? field, string name, CatalogSection section)
        {
            if (field != null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            field = root?.Q<Button>(name);
            if (field == null)
            {
                return;
            }

            // Capture section in local for stable handler identity per button instance.
            var target = section;
            field.clicked += () =>
            {
                _activeSection = target;
                ApplySectionVisibility();
            };
        }

        private void ApplySectionVisibility()
        {
            SetPaneVisible(_detailPane, _activeSection is CatalogSection.Identity or CatalogSection.Damage or CatalogSection.Fits);
            SetPaneVisible(_commsPane, _activeSection == CatalogSection.Comms);
            SetPaneVisible(_linksPane, _activeSection == CatalogSection.Links);
            SetPaneVisible(_graphPane, _activeSection == CatalogSection.Graph);

            ApplyDetailLineVisibility();

            SetSectionActive(_sectionIdentity, _activeSection == CatalogSection.Identity);
            SetSectionActive(_sectionDamage, _activeSection == CatalogSection.Damage);
            SetSectionActive(_sectionFits, _activeSection == CatalogSection.Fits);
            SetSectionActive(_sectionComms, _activeSection == CatalogSection.Comms);
            SetSectionActive(_sectionLinks, _activeSection == CatalogSection.Links);
            SetSectionActive(_sectionGraph, _activeSection == CatalogSection.Graph);
        }

        private void ApplyDetailLineVisibility()
        {
            // Identity: id/radius/speed + demoted scenario lat/lon
            // Damage: hp/resilience/withdraw/flags
            // Fits: mounts/sensors
            var showIdentity = _activeSection == CatalogSection.Identity;
            var showDamage = _activeSection == CatalogSection.Damage;
            var showFits = _activeSection == CatalogSection.Fits;
            var showDetailBundle = showIdentity || showDamage || showFits;

            SetLabelVisible(_detailId, showIdentity);
            SetLabelVisible(_detailRadius, showIdentity);
            SetLabelVisible(_detailSpeed, showIdentity);
            SetLabelVisible(_detailLat, showIdentity);
            SetLabelVisible(_detailLon, showIdentity);
            SetLabelVisible(_detailHp, showDamage);
            SetLabelVisible(_detailResilience, showDamage);
            SetLabelVisible(_detailWithdraw, showDamage);
            SetLabelVisible(_detailFlags, showDamage);
            SetLabelVisible(_detailMounts, showFits);
            SetLabelVisible(_detailSensors, showFits);

            if (_detailPane != null && showDetailBundle)
            {
                _detailPane.style.display = DisplayStyle.Flex;
            }
        }

        private static void SetPaneVisible(VisualElement? pane, bool visible)
        {
            if (pane != null)
            {
                pane.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private static void SetLabelVisible(Label? label, bool visible)
        {
            if (label != null)
            {
                label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private static void SetSectionActive(Button? button, bool active)
        {
            button?.EnableInClassList(SectionActiveClass, active);
        }

        private void SetActionStatus(string message)
        {
            if (_actionStatus != null)
            {
                _actionStatus.text = message;
            }
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
                SetActionStatus("EXPORT: database path required (or use CLI platform_export_xlsx)");
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
                SetActionStatus($"EXPORT: ok → {outPath}");
                Debug.Log(
                    $"[PlatformCatalogViewerHost] Export ok snapshot={_boundSnapshotId} out={outPath} " +
                    "(canonical text; use CLI platform_export_xlsx for .xlsx).");
            }
            catch (Exception ex)
            {
                SetActionStatus($"EXPORT: failed — {ex.Message}");
                Debug.LogWarning($"[PlatformCatalogViewerHost] Export failed: {ex.Message}");
            }
        }

        private void OnDiffClicked()
        {
            var databasePath = ResolveDatabasePath();
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                SetActionStatus("DIFF: database path required");
                Debug.LogWarning("[PlatformCatalogViewerHost] Diff skipped: no database path bound.");
                return;
            }

            try
            {
                var changes = PlatformCatalogExportBridge.DiffUneditedRoundTrip(
                    databasePath,
                    _boundSnapshotId,
                    clockTicks: 0);
                SetActionStatus($"DIFF: {changes.Count} change(s) (read-only empty-diff check)");
                Debug.Log(
                    $"[PlatformCatalogViewerHost] Diff ok snapshot={_boundSnapshotId} diffCount={changes.Count} " +
                    "(read-only; propose/approve remains CLI/Phase D).");
            }
            catch (Exception ex)
            {
                SetActionStatus($"DIFF: failed — {ex.Message}");
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
            _selectedPlatformId = row?.PlatformId;
            BindDetail(row);
            BindComms(row?.PlatformId);
            BindLinks(row?.PlatformId);
            BindGraph(row?.PlatformId);  // PE-UX-W5 graph projection
        }

        private void BindDetail(CatalogPlatformBrowseRow? row)
        {
            var detail = PlatformCatalogDetailProjection.Format(row);
            if (_detailId != null)
            {
                _detailId.text = detail.PlatformIdLabel;
            }

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

            if (_detailMounts != null)
            {
                _detailMounts.text = detail.MountsLabel;
            }

            if (_detailSensors != null)
            {
                _detailSensors.text = detail.SensorsLabel;
            }

            ApplyDetailLineVisibility();
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

        // PE-UX-W5: graph lines via PlatformCatalogGraphProjection (focus + search beyond display cap).
        private void BindGraph(string? platformId)
        {
            _selectedPlatformId = platformId;
            _graphDisplayItems = PlatformCatalogGraphProjection
                .FormatLines(_allGraphEdges, focusPlatformId: platformId, search: _graphSearch)
                .ToList();

            if (_graphList != null)
            {
                _graphList.itemsSource = _graphDisplayItems;
                _graphList.Rebuild();
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