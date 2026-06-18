// Tactical map placeholder (ADR-007 Phase A) — UI Toolkit canvas.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
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
        private MapPanelState _panelState = new("—", Array.Empty<MapSymbolDisplayRow>());
        private bool _wired;

        private IC2PresentationFeed? PresentationFeed => bridgeHost;

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
            _canvas = _rootPanel.Q<VisualElement>(CanvasName);
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
            RebuildSymbols();
            _rootPanel!.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
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

        private void RebuildSymbols()
        {
            _canvas!.Clear();
            foreach (var row in _panelState.Symbols)
            {
                var container = new VisualElement
                {
                    style =
                    {
                        position = Position.Absolute,
                        left = Length.Percent(row.NormalizedX * 100f),
                        top = Length.Percent(row.NormalizedY * 100f),
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                    },
                };
                container.AddToClassList("map-symbol");
                foreach (var styleClass in row.StyleClass.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    container.AddToClassList(styleClass);
                }

                if (row.UsesAtlasFrame && !string.IsNullOrEmpty(row.AtlasFrameClass))
                {
                    var frame = new VisualElement();
                    frame.AddToClassList(row.AtlasFrameClass);
                    container.Add(frame);
                }
                else if (!string.IsNullOrEmpty(row.Glyph))
                {
                    container.Add(new Label(row.Glyph));
                }

                container.Add(new Label(row.Label));
                container.userData = row.SymbolId;
                if (!row.IsGhost)
                {
                    container.RegisterCallback<ClickEvent>(_ => OnSymbolClicked(row.SymbolId));
                }
                else
                {
                    container.pickingMode = PickingMode.Ignore;
                }

                _canvas.Add(container);
            }
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