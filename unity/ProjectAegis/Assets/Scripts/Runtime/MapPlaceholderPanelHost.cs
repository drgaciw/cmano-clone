// Tactical map placeholder (ADR-007 Phase A) — UI Toolkit canvas.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Orchestration;
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
        private const string PlanningDimmedClass = "map-placeholder-panel--planning-dimmed";
        private const string PlanningDimOverlayHiddenClass = "map-planning-dim-overlay--hidden";

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
        private VisualElement? _canvas;
        private VisualElement? _planningDimOverlay;
        private MapPanelState _panelState = new("—", Array.Empty<MapSymbolDisplayRow>());
        private C2PlanningChromeState _planningChrome = new(false, false, SimulationPhase.Planning);
        private bool _wired;

        private MapSymbolPool? _symbolPool;
        private bool _refreshedOnce;
        private IReadOnlyList<MapSymbolEntry>? _dirtySymbolsRef;
        private string? _dirtySelectedUnit;
        private string? _dirtySelectedContact;
        private SimulationPhase _dirtyPhase;
        private bool _dirtyShowPanel;

        private IC2PresentationFeed? PresentationFeed => bridgeHost;

        /// <summary>True while <see cref="SimulationPhase.Planning"/> chrome dims the map (S30-07).</summary>
        public bool IsDimmed => _planningChrome.IsMapDimmed;

        /// <summary>Read-only map symbols from presentation feed (Cesium APP-6 billboard wiring, ADR-010).</summary>
        public IReadOnlyList<MapSymbolEntry> CurrentMapSymbols =>
            PresentationFeed?.LastMapSymbols ?? Array.Empty<MapSymbolEntry>();

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (panelAsset != null)
            {
                _document.visualTreeAsset = panelAsset;
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

            _rootPanel = root.Q<VisualElement>(RootName) ?? root;
            _theaterLabel = _rootPanel.Q<Label>(TheaterName);
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
            ApplyPlanningChrome();
            _rootPanel!.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            CaptureDirtyState();
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
                || showPanel != _dirtyShowPanel;
        }

        private void CaptureDirtyState()
        {
            var feed = PresentationFeed;
            _dirtySymbolsRef = feed?.LastMapSymbols;
            _dirtySelectedUnit = feed?.SelectedUnitId;
            _dirtySelectedContact = feed?.SelectedContactId;
            _dirtyPhase = bridgeHost.Phase;
            _dirtyShowPanel = showPanel;
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