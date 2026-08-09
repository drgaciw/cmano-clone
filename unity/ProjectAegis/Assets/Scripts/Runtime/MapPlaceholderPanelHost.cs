// Tactical map placeholder (ADR-007 Phase A) — UI Toolkit canvas.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Sim.Scenario;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MapPlaceholderPanelHost : MonoBehaviour
    {
        private const string RootName = "map-placeholder-root";
        private const string TheaterName = "theater-label";
        private const string CanvasName = "map-canvas";
        private const string PlanningDimOverlayName = "planning-dim-overlay";
        private const string EnvelopeRingCountName = "envelope-ring-count";
        private const string DatalinkEdgeCountName = "datalink-edge-count";
        private const string DoctrineOverlayCountName = "doctrine-overlay-count";
        private const string LayerCountName = "layer-count";
        private const string PlanningDimmedClass = "map-placeholder-panel--planning-dimmed";
        private const string PlanningDimOverlayHiddenClass = "map-planning-dim-overlay--hidden";

        /// <summary>Repo-relative Specced production USS for ASSET-009 (not Approved).</summary>
        public const string SpeccedProductionUssRelativePath =
            ProjectAegis.Delegation.Projection.SpeccedC2PanelStylePaths.Asset009MapPlaceholderUss;

        /// <summary>Repo-relative Specced production USS for ASSET-010 comms degrade modifiers.</summary>
        public const string SpeccedCommsDegradeUssRelativePath =
            ProjectAegis.Delegation.Projection.SpeccedC2PanelStylePaths.Asset010MapCommsDegradeUss;

        public const string UnityUssRelativePath =
            ProjectAegis.Delegation.Projection.SpeccedC2PanelStylePaths.UnityMapPlaceholderUss;

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private bool useApp6AtlasFrames = true;
        [SerializeField] private bool preferAddressablesAtlas = true;
        [SerializeField] private string app6AtlasManifestRelativePath =
            "Addressables/Map/App6AtlasAddressablesManifest.json";

        private UIDocument _document = null!;
        private VisualElement? _rootPanel;
        private Label? _theaterLabel;
        private Label? _envelopeRingCountLabel;
        private Label? _datalinkEdgeCountLabel;
        private Label? _doctrineOverlayCountLabel;
        private Label? _layerCountLabel;
        private VisualElement? _canvas;
        private VisualElement? _planningDimOverlay;
        private MapPanelState _panelState = new("—", Array.Empty<MapSymbolDisplayRow>());
        private C2PlanningChromeState _planningChrome = new(false, false, SimulationPhase.Planning);
        private MapLayerStackState _layerStack = MapLayerStackState.WithDefaults();
        private readonly MapLayerStackStore _layerStore = new();
        private bool _wired;

        private MapSymbolPool? _symbolPool;
        private bool _refreshedOnce;
        private IReadOnlyList<MapSymbolEntry>? _dirtySymbolsRef;
        private string? _dirtySelectedUnit;
        private string? _dirtySelectedContact;
        private SimulationPhase _dirtyPhase;
        private bool _dirtyShowPanel;
        private int _dirtyLayerVisibleCount = -1;

        private IC2PresentationFeed? PresentationFeed => bridgeHost;

        /// <summary>True while <see cref="SimulationPhase.Planning"/> chrome dims the map (S30-07).</summary>
        public bool IsDimmed => _planningChrome.IsMapDimmed;

        /// <summary>Read-only map symbols from presentation feed (Cesium APP-6 billboard wiring, ADR-010).</summary>
        public IReadOnlyList<MapSymbolEntry> CurrentMapSymbols =>
            PresentationFeed?.LastMapSymbols ?? Array.Empty<MapSymbolEntry>();

        /// <summary>Last projected selected-unit envelope ring count (CMD-21/34).</summary>
        public int LastEnvelopeRingCount { get; private set; }

        /// <summary>Last projected datalink edge count (CMD-32).</summary>
        public int LastDatalinkEdgeCount { get; private set; }

        /// <summary>Last projected doctrine map overlay row count (CMD-33).</summary>
        public int LastDoctrineOverlayCount { get; private set; }

        /// <summary>Last basemap layer visible count (CMD-28.2, UI-local).</summary>
        public int LastLayerVisibleCount { get; private set; }

        /// <summary>Last basemap layer total count (CMD-28.2, UI-local).</summary>
        public int LastLayerTotalCount { get; private set; }

        /// <summary>Last layer summary label from <see cref="MapLayerStackApplyState"/>.</summary>
        public string LastLayerSummaryLabel { get; private set; } = "LAYERS: 0/0";

        /// <summary>Current UI-local basemap layer stack (not sim state).</summary>
        public MapLayerStackState LayerStack => _layerStack;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            if (panelAsset != null)
            {
                _document.visualTreeAsset = panelAsset;
            }

            // Restore UI-local layer visibility bag (in-memory; no DecisionLog).
            _layerStack = _layerStore.Restore(MapLayerStackState.WithDefaults());
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

            _rootPanel = root.Q<VisualElement>(RootName) ?? root;
            _theaterLabel = _rootPanel.Q<Label>(TheaterName);
            // Optional overlay count labels (CMD-21/32/33/34) — null-safe; scene rebuild not required.
            _envelopeRingCountLabel = _rootPanel.Q<Label>(EnvelopeRingCountName);
            _datalinkEdgeCountLabel = _rootPanel.Q<Label>(DatalinkEdgeCountName);
            _doctrineOverlayCountLabel = _rootPanel.Q<Label>(DoctrineOverlayCountName);
            // Optional basemap layer HUD (CMD-28.2) — null-safe Q.
            _layerCountLabel = _rootPanel.Q<Label>(LayerCountName);
            var canvas = _rootPanel.Q<VisualElement>(CanvasName);
            if (!ReferenceEquals(canvas, _canvas))
            {
                _canvas = canvas;
                _symbolPool = _canvas != null ? new MapSymbolPool(_canvas) : null;
                _refreshedOnce = false;
            }

            _planningDimOverlay = _rootPanel.Q<VisualElement>(PlanningDimOverlayName);
            if (panelStyles != null && !_rootPanel.styleSheets.Contains(panelStyles))
            {
                _rootPanel.styleSheets.Add(panelStyles);
            }

            _wired = _theaterLabel != null && _canvas != null;
        }

        private void Refresh()
        {
            if (!_wired || PresentationFeed == null || bridgeHost.Bridge == null || _canvas == null)
            {
                return;
            }

            // Dirty-flag: skip the whole rebind/rebuild while nothing that affects the map changed.
            if (!IsDirty())
            {
                return;
            }

            var comms = CommsStateProjection.Project(bridgeHost.Bridge.Orchestrator.DecisionLog);
            var commsDisplay = bridgeHost.Bridge.Orchestrator.ScenarioPolicy?.CommsDisplay
                ?? ScenarioCommsDisplaySettings.Default;
            var atlas = ResolveAtlasCatalog();
            _panelState = MapPanelBinder.Bind(
                PresentationFeed.LastMapSymbols,
                bridgeHost.ScenarioPolicyId,
                PresentationFeed.SelectedUnitId,
                PresentationFeed.SelectedContactId,
                comms.State,
                commsDisplay,
                atlas);
            _theaterLabel!.text = $"THEATER: {_panelState.TheaterLabel}";
            _symbolPool!.Sync(_panelState.Symbols, OnSymbolClicked);
            ApplyOverlayCounts();
            ApplyLayerStackHud();
            ApplyPlanningChrome();
            _rootPanel!.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            CaptureDirtyState();
        }

        /// <summary>
        /// Projects selected-unit envelope rings (catalog ranges), datalink unit-pair edges,
        /// and doctrine map overlay rows; surfaces overlay counts (CMD-21/32/33/34).
        /// </summary>
        private void ApplyOverlayCounts()
        {
            var catalog = bridgeHost != null ? bridgeHost.CatalogReader : null;
            var selectedUnitId = PresentationFeed?.SelectedUnitId;
            var (sensorNm, weaponNm) = CatalogEnvelopeRangeResolver.ResolveSelectedUnitRanges(
                catalog,
                selectedUnitId,
                CatalogWeaponIds.MvpDefault);

            var rings = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes(
                selectedUnitId,
                sensorNm,
                weaponNm);

            IReadOnlyList<DatalinkEdgeEntry> edges = Array.Empty<DatalinkEdgeEntry>();
            if (catalog is not null)
            {
                var friendlyIds = CollectAliveFriendlyUnitIds(PresentationFeed?.LastOobTree);
                var links = catalog.GetSortedLinks() ?? Array.Empty<CatalogLinkEntry>();
                edges = DatalinkUnitPairFeed.ProjectEdges(friendlyIds, links);
            }

            var doctrineOverlay = ProjectDoctrineOverlay();
            var presentation = MapPanelApplyState.Apply(_panelState, rings, edges, doctrineOverlay);
            LastEnvelopeRingCount = presentation.EnvelopeRingCount;
            LastDatalinkEdgeCount = presentation.DatalinkEdgeCount;
            LastDoctrineOverlayCount = presentation.DoctrineOverlayCount;

            if (_envelopeRingCountLabel != null)
            {
                _envelopeRingCountLabel.text = $"ENVELOPES: {LastEnvelopeRingCount}";
            }

            if (_datalinkEdgeCountLabel != null)
            {
                _datalinkEdgeCountLabel.text = $"DATALINKS: {LastDatalinkEdgeCount}";
            }

            if (_doctrineOverlayCountLabel != null)
            {
                _doctrineOverlayCountLabel.text = $"DOCTRINE: {LastDoctrineOverlayCount}";
            }
        }

        /// <summary>
        /// Refresh basemap layer HUD from UI-local stack (CMD-28.2). Not sim state.
        /// </summary>
        private void ApplyLayerStackHud()
        {
            var presentation = MapLayerStackApplyState.Apply(_layerStack);
            LastLayerVisibleCount = presentation.VisibleCount;
            LastLayerTotalCount = presentation.TotalCount;
            LastLayerSummaryLabel = presentation.SummaryLabel;

            if (_layerCountLabel != null)
            {
                _layerCountLabel.text = presentation.SummaryLabel;
            }
        }

        /// <summary>
        /// Toggle a basemap layer (UI-local). Captures visibility into the in-memory store.
        /// Does not touch DecisionLog or sim state.
        /// </summary>
        public void ToggleLayer(MapLayerId id)
        {
            _layerStack = _layerStack.Toggle(id);
            _layerStore.Capture(_layerStack);
            _dirtyLayerVisibleCount = -1; // force refresh on next tick
            if (_wired)
            {
                ApplyLayerStackHud();
            }
        }

        /// <summary>Replace layer stack from a host-supplied state (tests / prefs restore).</summary>
        public void SetLayerStack(MapLayerStackState state)
        {
            _layerStack = state ?? MapLayerStackState.WithDefaults();
            _layerStore.Capture(_layerStack);
            _dirtyLayerVisibleCount = -1;
            if (_wired)
            {
                ApplyLayerStackHud();
            }
        }

        private IReadOnlyList<DoctrineMapOverlayEntry> ProjectDoctrineOverlay()
        {
            var oob = PresentationFeed?.LastOobTree;
            if (oob is null || oob.Count == 0)
            {
                return Array.Empty<DoctrineMapOverlayEntry>();
            }

            var unitIds = new List<TargetId>();
            foreach (var entry in oob)
            {
                if (entry is null || !entry.IsAlive || string.IsNullOrWhiteSpace(entry.UnitId))
                {
                    continue;
                }

                unitIds.Add(new TargetId(entry.UnitId));
            }

            if (unitIds.Count == 0)
            {
                return Array.Empty<DoctrineMapOverlayEntry>();
            }

            var policy = bridgeHost?.Bridge?.Orchestrator?.ScenarioPolicy;
            var inheritance = DoctrineInheritanceProjection.ProjectAllUnits(unitIds, policy, isFriendly: true);
            return DoctrineMapOverlayProjection.Project(inheritance, PresentationFeed?.LastMapSymbols);
        }

        private static IReadOnlyList<string> CollectAliveFriendlyUnitIds(IReadOnlyList<OobTreeEntry>? oob)
        {
            if (oob is null || oob.Count == 0)
            {
                return Array.Empty<string>();
            }

            var ids = new List<string>(oob.Count);
            foreach (var entry in oob)
            {
                if (entry is null || !entry.IsAlive || string.IsNullOrWhiteSpace(entry.UnitId))
                {
                    continue;
                }

                ids.Add(entry.UnitId);
            }

            return ids;
        }

        private bool IsDirty()
        {
            var feed = PresentationFeed;
            if (feed == null)
            {
                return false;
            }

            return !_refreshedOnce
                || !ReferenceEquals(feed.LastMapSymbols, _dirtySymbolsRef)
                || feed.SelectedUnitId != _dirtySelectedUnit
                || feed.SelectedContactId != _dirtySelectedContact
                || bridgeHost.Phase != _dirtyPhase
                || showPanel != _dirtyShowPanel
                || _layerStack.VisibleCount != _dirtyLayerVisibleCount;
        }

        private void CaptureDirtyState()
        {
            var feed = PresentationFeed;
            _dirtySymbolsRef = feed?.LastMapSymbols;
            _dirtySelectedUnit = feed?.SelectedUnitId;
            _dirtySelectedContact = feed?.SelectedContactId;
            _dirtyPhase = bridgeHost.Phase;
            _dirtyShowPanel = showPanel;
            _dirtyLayerVisibleCount = _layerStack.VisibleCount;
            _refreshedOnce = true;
        }

        private void ApplyPlanningChrome()
        {
            if (bridgeHost == null || _rootPanel == null)
            {
                return;
            }

            _planningChrome = C2PlanningChromeProjection.Project(bridgeHost.Phase);
            if (_planningChrome.IsMapDimmed)
            {
                _rootPanel.AddToClassList(PlanningDimmedClass);
            }
            else
            {
                _rootPanel.RemoveFromClassList(PlanningDimmedClass);
            }

            if (_planningDimOverlay == null)
            {
                return;
            }

            if (_planningChrome.IsMapDimmed)
            {
                _planningDimOverlay.RemoveFromClassList(PlanningDimOverlayHiddenClass);
            }
            else
            {
                _planningDimOverlay.AddToClassList(PlanningDimOverlayHiddenClass);
            }
        }

        private IApp6AtlasAvailability ResolveAtlasCatalog()
        {
            if (!useApp6AtlasFrames)
            {
                return App6AtlasCatalog.Unavailable;
            }

            if (preferAddressablesAtlas
                && TryResolveAddressablesAtlas(out var addressablesAtlas))
            {
                return addressablesAtlas;
            }

            return App6AtlasCatalog.Default;
        }

        private bool TryResolveAddressablesAtlas(out IApp6AtlasAvailability atlas)
        {
            atlas = App6AtlasCatalog.Unavailable;
            if (string.IsNullOrWhiteSpace(app6AtlasManifestRelativePath))
            {
                return false;
            }

            var manifestPath = System.IO.Path.Combine(Application.dataPath, app6AtlasManifestRelativePath);
            return App6AddressablesCatalog.TryResolveFromManifest(
                manifestPath,
                Application.dataPath,
                out var catalog,
                out _)
                && catalog.IsLoaded
                && (atlas = catalog).IsLoaded;
        }

        private void OnSymbolClicked(string symbolId)
        {
            if (PresentationFeed == null)
            {
                return;
            }

            var symbols = PresentationFeed.LastMapSymbols;
            if (C2SelectionResolver.TryResolveFriendlyUnitFromSymbol(symbolId, symbols, out var unitId))
            {
                PresentationFeed.SelectUnit(unitId);
                return;
            }

            if (C2SelectionResolver.TryResolveHostileContactFromSymbol(symbolId, symbols, out var contactId))
            {
                PresentationFeed.SelectContact(contactId);
            }
        }
    }
}
#endif
