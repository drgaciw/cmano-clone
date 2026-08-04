// Product globe status chrome (ADR-007 Phase B / CMD-06 · CMD-13).
// Pure UI Toolkit — no Cesium package dependency; safe for CI / default smoke.
// Active only when DelegationBridgeHost.UseGlobeMap is true.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Headless-backed product globe status strip for useGlobeMap scenes.
    /// Binds <see cref="GlobeMapApplyState"/> status line; works without com.cesium.unity.
    /// Theater quick-jump / bookmarks are presentation-only (no sim mutation).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class GlobeMapProductHost : MonoBehaviour
    {
        private const string RootName = "globe-map-product-root";
        private const string StatusName = "globe-status-line";
        private const string BookmarksEmptyName = "globe-bookmarks-empty";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _statusLine;
        private Label? _bookmarksEmpty;
        private VisualElement? _root;
        private GlobeMapPresentation _presentation = GlobeMapPresentation.Empty;
        private GlobeViewState _viewState = GlobeViewProjection.DefaultBalticTheater();
        private bool _wired;

        /// <summary>Last applied globe presentation (status + bookmarks).</summary>
        public GlobeMapPresentation LastPresentation => _presentation;

        /// <summary>Current product view state (camera / bookmarks / mode). Presentation-only.</summary>
        public GlobeViewState ViewState => _viewState;

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

        /// <summary>Force refresh from current bridge map symbols + view state.</summary>
        public void Refresh()
        {
            if (bridgeHost != null && !bridgeHost.UseGlobeMap)
            {
                if (_root != null)
                {
                    _root.style.display = DisplayStyle.None;
                }

                return;
            }

            if (_root != null)
            {
                _root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }

            IReadOnlyList<MapSymbolEntry> symbols =
                bridgeHost?.LastMapSymbols ?? Array.Empty<MapSymbolEntry>();

            IReadOnlyList<CesiumBillboardMarker> markers = symbols.Count > 0
                ? CesiumBillboardProjection.ProjectWithCamera(symbols, _viewState.Camera)
                : CesiumBillboardProjection.ProjectDemoPair();

            _presentation = GlobeMapApplyState.Apply(_viewState, markers);

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

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            _root = root.Q<VisualElement>(RootName);
            _statusLine = root.Q<Label>(StatusName);
            _bookmarksEmpty = root.Q<Label>(BookmarksEmptyName);
            _wired = _root != null && _statusLine != null;
        }
    }
}
#endif
