// Tactical map placeholder (ADR-007 Phase A) — UI Toolkit canvas.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
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
        // stand-in until a real camera controller drives it. Actual VisualElement construction for
        // icon size / domain modifier / label declutter lives in MapSymbolPool.Apply — this host only
        // resolves the per-frame inputs (icon size px, label visibility, per-symbol declutter map).
        [SerializeField] private MapZoomBand zoomBand = MapZoomBand.Tactical;
        [SerializeField] private float labelCollisionRadiusNormalized = 0.05f;
        [SerializeField] private int maxDirectLabels = 24;
        [SerializeField] private int maxLeaderLineLabels = 12;
        // --------------------------------------------------------------------------------------------

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

        // req20-rev2 Track T1 (TR-c2-005): drag-box marquee + shift-click multi-select. Pointer/marquee
        // handling only — symbol rendering stays in MapSymbolPool (T4 territory, untouched here).
        private const string MarqueeBoxClass = "map-marquee-box";
        private Vector2? _dragStartNormalized;
        private Vector2 _dragCurrentNormalized;
        private bool _isDragging;
        /// <summary>
        /// Frame through which symbol clicks are swallowed after a marquee resolve. Using a frame
        /// watermark (not a sticky bool) ensures an empty-canvas marquee does not permanently
        /// suppress the next ordinary symbol click.
        /// </summary>
        private int _suppressSymbolClickThroughFrame = -1;
        private bool _pointerDownShiftKey;
        private VisualElement? _marqueeBox;

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
                _marqueeBox = null;
                WireMarqueeInput();
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

            // T4 Symbology: icon size ladder (req 20 rev 2) — pure function of zoom band only.
            var iconSizePx = MapIconSizeLadder.ResolveIconSizePx(zoomBand);
            var labelsVisible = MapIconSizeLadder.AreLabelsVisible(zoomBand);
            var declutter = ResolveLabelDeclutter();
            _symbolPool!.Sync(_panelState.Symbols, OnSymbolClicked, iconSizePx, labelsVisible, declutter);

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

            // A marquee drag just resolved on this pointer gesture — swallow the synthesized click
            // so releasing over a symbol doesn't also fire a conflicting single-select on top of the
            // marquee result (req 20 §Selection, TR-c2-005). Frame-scoped so empty-canvas marquees
            // do not leave a sticky suppress that eats the next ordinary click.
            if (Time.frameCount <= _suppressSymbolClickThroughFrame)
            {
                return;
            }

            var symbols = PresentationFeed.LastMapSymbols;
            if (C2SelectionResolver.TryResolveFriendlyUnitFromSymbol(symbolId, symbols, out var unitId))
            {
                if (_pointerDownShiftKey)
                {
                    PresentationFeed.ToggleUnit(unitId);
                }
                else
                {
                    PresentationFeed.SelectUnit(unitId);
                }

                return;
            }

            if (C2SelectionResolver.TryResolveHostileContactFromSymbol(symbolId, symbols, out var contactId))
            {
                PresentationFeed.SelectContact(contactId);
            }
        }

        // req20-rev2 Track T1 (TR-c2-005): drag-box marquee + shift-click multi-select. The rect→ids
        // math itself lives in the pure, headless-testable SelectionBoxResolver
        // (src/ProjectAegis.Delegation.UnityAdapter/Presentation/SelectionBoxResolver.cs) — this host
        // only feeds pointer coordinates in and applies the resulting id list.

        private void WireMarqueeInput()
        {
            if (_canvas == null)
            {
                return;
            }

            _canvas.RegisterCallback<PointerDownEvent>(OnCanvasPointerDown);
            _canvas.RegisterCallback<PointerMoveEvent>(OnCanvasPointerMove);
            _canvas.RegisterCallback<PointerUpEvent>(OnCanvasPointerUp);
            _canvas.RegisterCallback<PointerLeaveEvent>(OnCanvasPointerLeave);
        }

        private void OnCanvasPointerDown(PointerDownEvent evt)
        {
            if (_canvas == null || evt.button != 0)
            {
                return;
            }

            _pointerDownShiftKey = evt.shiftKey;
            _dragStartNormalized = ToNormalized(evt.localPosition);
            _dragCurrentNormalized = _dragStartNormalized.Value;
            _isDragging = false;
        }

        private void OnCanvasPointerMove(PointerMoveEvent evt)
        {
            if (_canvas == null || _dragStartNormalized == null)
            {
                return;
            }

            _dragCurrentNormalized = ToNormalized(evt.localPosition);
            var rect = CurrentDragRect();

            if (!_isDragging && rect.IsDrag)
            {
                _isDragging = true;
            }

            if (_isDragging)
            {
                UpdateMarqueeVisual(rect);
            }
        }

        private void OnCanvasPointerUp(PointerUpEvent evt)
        {
            if (_canvas == null || _dragStartNormalized == null)
            {
                ResetDragState();
                return;
            }

            if (_isDragging && PresentationFeed != null)
            {
                var rect = CurrentDragRect();
                var symbols = PresentationFeed.LastMapSymbols;
                var boxIds = SelectionBoxResolver.Resolve(rect, symbols);

                if (_pointerDownShiftKey)
                {
                    PresentationFeed.AddUnits(boxIds);
                }
                else
                {
                    PresentationFeed.SelectUnits(boxIds);
                }

                // Swallow only ClickEvents raised for this pointer-up's frame (same-frame symbol
                // click synthesis). Next frame's clicks are free again.
                _suppressSymbolClickThroughFrame = Time.frameCount;
            }

            ResetDragState();
        }

        private void OnCanvasPointerLeave(PointerLeaveEvent evt) => ResetDragState();

        private NormalizedRect CurrentDragRect() =>
            NormalizedRect.FromCorners(
                _dragStartNormalized!.Value.x,
                _dragStartNormalized.Value.y,
                _dragCurrentNormalized.x,
                _dragCurrentNormalized.y);

        private void ResetDragState()
        {
            _dragStartNormalized = null;
            _isDragging = false;
            HideMarqueeVisual();
        }

        private Vector2 ToNormalized(Vector2 localPosition)
        {
            var width = Mathf.Max(1f, _canvas!.resolvedStyle.width);
            var height = Mathf.Max(1f, _canvas.resolvedStyle.height);
            return new Vector2(
                Mathf.Clamp01(localPosition.x / width),
                Mathf.Clamp01(localPosition.y / height));
        }

        private void UpdateMarqueeVisual(NormalizedRect rect)
        {
            if (_canvas == null)
            {
                return;
            }

            if (_marqueeBox == null)
            {
                _marqueeBox = new VisualElement { pickingMode = PickingMode.Ignore };
                _marqueeBox.AddToClassList(MarqueeBoxClass);
                // Inline style only (no new USS rule) — this stays a minimal, self-contained pointer/
                // marquee change; symbol/theme styling remains T4's territory in MapPlaceholderPanel.uss.
                _marqueeBox.style.position = Position.Absolute;
                _marqueeBox.style.borderTopWidth = 1f;
                _marqueeBox.style.borderBottomWidth = 1f;
                _marqueeBox.style.borderLeftWidth = 1f;
                _marqueeBox.style.borderRightWidth = 1f;
                var borderColor = new Color(1f, 1f, 1f, 0.85f);
                _marqueeBox.style.borderTopColor = borderColor;
                _marqueeBox.style.borderBottomColor = borderColor;
                _marqueeBox.style.borderLeftColor = borderColor;
                _marqueeBox.style.borderRightColor = borderColor;
                _marqueeBox.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
                _canvas.Add(_marqueeBox);
            }

            _marqueeBox.style.left = Length.Percent(rect.MinX * 100f);
            _marqueeBox.style.top = Length.Percent(rect.MinY * 100f);
            _marqueeBox.style.width = Length.Percent((rect.MaxX - rect.MinX) * 100f);
            _marqueeBox.style.height = Length.Percent((rect.MaxY - rect.MinY) * 100f);
            _marqueeBox.BringToFront();
        }

        private void HideMarqueeVisual()
        {
            _marqueeBox?.RemoveFromHierarchy();
            _marqueeBox = null;
        }
    }
}
#endif
