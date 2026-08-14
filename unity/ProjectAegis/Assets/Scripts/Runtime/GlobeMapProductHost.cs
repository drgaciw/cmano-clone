// Product globe status chrome (ADR-007 Phase B / CMD-06 · CMD-13).
// Pure UI Toolkit — no Cesium package dependency; safe for CI / default smoke.
// Active only when DelegationBridgeHost.UseGlobeMap is true.
// DRG-161: envelope rings + datalink edges bind via GlobeOverlayProjection (globe-only).
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Headless-backed product globe status strip for useGlobeMap scenes.
    /// Binds <see cref="GlobeMapApplyState"/> status line; works without com.cesium.unity.
    /// Theater quick-jump / bookmarks are presentation-only (no sim mutation).
    /// DRG-161: draws projected envelope rings and datalink edges on the globe overlay layer.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class GlobeMapProductHost : MonoBehaviour
    {
        private const string RootName = "globe-map-product-root";
        private const string StatusName = "globe-status-line";
        private const string BookmarksEmptyName = "globe-bookmarks-empty";
        private const string OverlayLayerName = "globe-overlay-layer";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _statusLine;
        private Label? _bookmarksEmpty;
        private VisualElement? _root;
        private GlobeOverlayVisualLayer? _overlayLayer;
        private GlobeMapPresentation _presentation = GlobeMapPresentation.Empty;
        private GlobeViewState _viewState = GlobeViewProjection.DefaultBalticTheater();
        private bool _wired;

        /// <summary>Last applied globe presentation (status + bookmarks + overlays).</summary>
        public GlobeMapPresentation LastPresentation => _presentation;

        /// <summary>Current product view state (camera / bookmarks / mode). Presentation-only.</summary>
        public GlobeViewState ViewState => _viewState;

        /// <summary>Last projected WGS84 envelope ring markers (CMD-21/34).</summary>
        public IReadOnlyList<GlobeEnvelopeRingMarker> LastEnvelopeRings { get; private set; } =
            Array.Empty<GlobeEnvelopeRingMarker>();

        /// <summary>Last projected WGS84 datalink edge markers (CMD-32).</summary>
        public IReadOnlyList<GlobeDatalinkEdgeMarker> LastDatalinkEdges { get; private set; } =
            Array.Empty<GlobeDatalinkEdgeMarker>();

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            EnsureProgrammaticTree();
        }

        private void OnEnable()
        {
            EnsureProgrammaticTree();
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

        /// <summary>
        /// Apply a pure view-state update (e.g. theater quick-jump). Does not touch sim.
        /// </summary>
        public void ApplyViewState(GlobeViewState view)
        {
            _viewState = view ?? throw new ArgumentNullException(nameof(view));
            Refresh();
        }

        /// <summary>Quick-jump to a bookmark id (presentation only).</summary>
        public void QuickJump(string bookmarkId)
        {
            _viewState = GlobeViewProjection.WithQuickJump(_viewState, bookmarkId);
            Refresh();
        }

        /// <summary>Force refresh from current bridge map symbols + view state + overlays.</summary>
        public void Refresh()
        {
            if (bridgeHost != null && !bridgeHost.UseGlobeMap)
            {
                if (_root != null)
                {
                    _root.style.display = DisplayStyle.None;
                }

                if (_overlayLayer != null)
                {
                    _overlayLayer.style.display = DisplayStyle.None;
                }

                return;
            }

            if (_root != null)
            {
                _root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_overlayLayer != null)
            {
                _overlayLayer.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }

            IReadOnlyList<MapSymbolEntry> symbols =
                bridgeHost?.LastMapSymbols ?? Array.Empty<MapSymbolEntry>();

            IReadOnlyList<CesiumBillboardMarker> markers = symbols.Count > 0
                ? CesiumBillboardProjection.ProjectWithCamera(symbols, _viewState.Camera)
                : CesiumBillboardProjection.ProjectDemoPair();

            var (ringEntries, edgeEntries) = ProjectOverlayEntries(symbols);
            LastEnvelopeRings = GlobeOverlayProjection.ProjectRings(ringEntries, symbols);
            LastDatalinkEdges = GlobeOverlayProjection.ProjectEdges(edgeEntries, symbols);

            _presentation = GlobeMapApplyState.Apply(
                _viewState,
                markers,
                LastEnvelopeRings,
                LastDatalinkEdges);

            if (_statusLine != null)
            {
                _statusLine.text = _presentation.StatusLine;
            }

            if (_bookmarksEmpty != null)
            {
                var empty = _presentation.Bookmarks.IsEmpty;
                _bookmarksEmpty.style.display = empty ? DisplayStyle.Flex : DisplayStyle.None;
                _bookmarksEmpty.text = empty
                    ? _presentation.Bookmarks.EmptyStateLine
                    : string.Empty;
            }

            _overlayLayer?.Bind(_viewState.Camera, LastEnvelopeRings, LastDatalinkEdges);
        }

        private (IReadOnlyList<EnvelopeRingEntry> Rings, IReadOnlyList<DatalinkEdgeEntry> Edges)
            ProjectOverlayEntries(IReadOnlyList<MapSymbolEntry> symbols)
        {
            var catalog = bridgeHost?.CatalogReader;
            var selectedUnitId = bridgeHost?.SelectedUnitId;
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
                var friendlyIds = CollectAliveFriendlyUnitIds(bridgeHost?.LastOobTree);
                var links = catalog.GetSortedLinks() ?? Array.Empty<CatalogLinkEntry>();
                var commsSnapshot = ResolveCommsSnapshot();
                edges = DatalinkUnitPairFeed.ProjectEdges(
                    friendlyIds,
                    links,
                    commsSnapshot: commsSnapshot);
            }

            return (rings, edges);
        }

        private CommsStateSnapshot? ResolveCommsSnapshot()
        {
            var log = bridgeHost?.Bridge?.Orchestrator?.DecisionLog;
            return log is null ? null : CommsStateProjection.Project(log);
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

        private void EnsureProgrammaticTree()
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            EnsureOverlayLayer(root);

            var panel = root.Q<VisualElement>(RootName);
            if (panel != null)
            {
                return;
            }

            panel = new VisualElement { name = RootName };
            panel.style.position = Position.Absolute;
            panel.style.left = 8;
            panel.style.bottom = 8;
            panel.style.paddingLeft = 8;
            panel.style.paddingRight = 8;
            panel.style.paddingTop = 4;
            panel.style.paddingBottom = 4;
            panel.style.backgroundColor = new Color(0.05f, 0.08f, 0.12f, 0.75f);

            var status = new Label("GLOBE") { name = StatusName };
            status.style.color = Color.white;
            status.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(status);

            var empty = new Label { name = BookmarksEmptyName };
            empty.style.color = new Color(0.75f, 0.78f, 0.82f, 1f);
            empty.style.display = DisplayStyle.None;
            panel.Add(empty);

            root.Add(panel);
        }

        private void EnsureOverlayLayer(VisualElement root)
        {
            _overlayLayer = root.Q<GlobeOverlayVisualLayer>(OverlayLayerName);
            if (_overlayLayer != null)
            {
                return;
            }

            _overlayLayer = new GlobeOverlayVisualLayer { name = OverlayLayerName };
            root.Insert(0, _overlayLayer);
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            EnsureOverlayLayer(root);
            _root = root.Q<VisualElement>(RootName);
            _statusLine = root.Q<Label>(StatusName);
            _bookmarksEmpty = root.Q<Label>(BookmarksEmptyName);
            _wired = _root != null && _statusLine != null && _overlayLayer != null;
        }
    }
}
#endif
