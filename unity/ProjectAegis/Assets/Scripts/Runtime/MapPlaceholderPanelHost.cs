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

        // --- T4 Symbology (req 20 rev 2 §Map and Symbology) ---------------------------------------
        // Icon size ladder + label declutter are driven by the pure functions in
        // ProjectAegis.Delegation.Projection (MapIconSizeLadder / MapLabelDeclutterProjection).
        // No camera/zoom system exists yet on this placeholder map, so `zoomBand` is a serialized
        // stand-in until a real camera controller drives it.
        [SerializeField] private MapZoomBand zoomBand = MapZoomBand.Tactical;
        [SerializeField] private float labelCollisionRadiusNormalized = 0.05f;
        [SerializeField] private int maxDirectLabels = 24;
        [SerializeField] private int maxLeaderLineLabels = 12;

        private const string DomainModifierName = "domain-modifier";
        private const string LabelHiddenClass = "map-symbol__label--hidden";
        private const string LabelLeaderLineClass = "map-symbol__label--leader-line";
        // --------------------------------------------------------------------------------------------

        private UIDocument _document = null!;
        private VisualElement? _rootPanel;
        private Label? _theaterLabel;
        private VisualElement? _canvas;
        private VisualElement? _planningDimOverlay;
        private MapPanelState _panelState = new("—", Array.Empty<MapSymbolDisplayRow>());
        private C2PlanningChromeState _planningChrome = new(false, false, SimulationPhase.Planning);
        private bool _wired;

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
            _canvas = _rootPanel.Q<VisualElement>(CanvasName);
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
            ApplyPlanningChrome();
            _rootPanel!.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
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

        private void RebuildSymbols()
        {
            _canvas!.Clear();

            // T4 Symbology: icon size ladder (req 20 rev 2) — pure function of zoom band only.
            var iconSizePx = MapIconSizeLadder.ResolveIconSizePx(zoomBand);
            var declutter = ResolveLabelDeclutter();

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

                // Icon/frame shape is always shown regardless of declutter — only the text label is
                // decluttered (a11y §5: affiliation must remain shape-readable at every zoom band).
                if (row.UsesAtlasFrame && !string.IsNullOrEmpty(row.AtlasFrameClass))
                {
                    var frame = new VisualElement
                    {
                        style = { width = iconSizePx, height = iconSizePx },
                    };
                    frame.AddToClassList(row.AtlasFrameClass);
                    container.Add(frame);
                }
                else if (!string.IsNullOrEmpty(row.Glyph))
                {
                    var glyphLabel = new Label(row.Glyph) { style = { fontSize = iconSizePx } };
                    container.Add(glyphLabel);
                }

                // Domain modifier (Air/Surface/Subsurface/Land/Mine/Facility) — secondary cue layered
                // on the frame, never a substitute for the affiliation shape above.
                if (!string.IsNullOrEmpty(row.DomainModifierClass))
                {
                    var domainBadge = new VisualElement { name = DomainModifierName };
                    domainBadge.AddToClassList(row.DomainModifierClass);
                    container.Add(domainBadge);
                }

                container.Add(BuildLabel(row, declutter));
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

        /// <summary>
        /// Build the text label element for a symbol row, applying the zoom-band visibility rule
        /// ("labels appear from regional zoom") and the declutter outcome (shown / leader-line / hidden)
        /// for the current frame. Ghost rows (comms-degraded duplicates) are exempt from declutter —
        /// they already carry their own lag-offset visual language.
        /// </summary>
        private Label BuildLabel(MapSymbolDisplayRow row, IReadOnlyDictionary<string, MapLabelDeclutterOutcome> declutter)
        {
            var label = new Label(row.Label);
            if (!MapIconSizeLadder.AreLabelsVisible(zoomBand))
            {
                label.AddToClassList(LabelHiddenClass);
                return label;
            }

            if (row.IsGhost || !declutter.TryGetValue(row.SymbolId, out var outcome))
            {
                return label;
            }

            switch (outcome)
            {
                case MapLabelDeclutterOutcome.Hidden:
                    label.AddToClassList(LabelHiddenClass);
                    break;
                case MapLabelDeclutterOutcome.LeaderLine:
                    label.AddToClassList(LabelLeaderLineClass);
                    break;
                case MapLabelDeclutterOutcome.Shown:
                default:
                    break;
            }

            return label;
        }

        /// <summary>
        /// Resolve declutter outcomes for the current frame's non-ghost labels via the pure
        /// <see cref="MapLabelDeclutterProjection"/> (req 20 rev 2: "selected &gt; engaged &gt; hostile &gt;
        /// friendly" with leader lines before hiding). "Engaged" priority is reserved for when doc 14
        /// engagement state reaches map symbols; today only Selected / Hostile / Friendly are derivable
        /// from <see cref="MapSymbolDisplayRow"/>.
        /// </summary>
        private Dictionary<string, MapLabelDeclutterOutcome> ResolveLabelDeclutter()
        {
            var candidates = new List<MapLabelCandidate>(_panelState.Symbols.Count);
            foreach (var row in _panelState.Symbols)
            {
                if (row.IsGhost)
                {
                    continue;
                }

                candidates.Add(new MapLabelCandidate(row.SymbolId, ResolveLabelPriority(row), row.NormalizedX, row.NormalizedY));
            }

            var results = MapLabelDeclutterProjection.Resolve(
                candidates,
                labelCollisionRadiusNormalized,
                maxDirectLabels,
                maxLeaderLineLabels);

            var byId = new Dictionary<string, MapLabelDeclutterOutcome>(results.Count, StringComparer.Ordinal);
            foreach (var result in results)
            {
                byId[result.SymbolId] = result.Outcome;
            }

            return byId;
        }

        private static MapLabelPriority ResolveLabelPriority(MapSymbolDisplayRow row)
        {
            if (row.IsSelected)
            {
                return MapLabelPriority.Selected;
            }

            if (row.StyleClass.Contains("map-symbol--hostile", StringComparison.Ordinal)
                || row.StyleClass.Contains("map-symbol--suspect", StringComparison.Ordinal))
            {
                return MapLabelPriority.Hostile;
            }

            if (row.StyleClass.Contains("map-symbol--friendly", StringComparison.Ordinal))
            {
                return MapLabelPriority.Friendly;
            }

            return MapLabelPriority.Other;
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