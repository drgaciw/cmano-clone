// DRG-167 — engagement battle graphic. Reads the combat frame; does not tick or enqueue orders.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Thin view for shooter→target battle graphics. Presentation only (ADR-010 §2–3, ADR-007, ADR-001).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class BattleGraphicPanelHost : MonoBehaviour
    {
        private const string RootName = "battle-graphic-root";
        private const string SummaryName = "battle-graphic-summary";
        private const string LineName = "battle-graphic-line";
        private const string TrackName = "battle-graphic-track";
        private const string TrailName = "battle-graphic-trail";
        private const string DetailName = "battle-graphic-detail";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private VisualElement? _root;
        private Label? _summary;
        private Label? _line;
        private Label? _track;
        private Label? _trail;
        private Label? _detail;
        private Button? _tactical;
        private Button? _operational;
        private Button? _theater;
        private bool _wired;
        private CombatZoomBand _zoom = CombatZoomBand.Tactical;
        private string? _selectedKey;
        private BattleGraphicState _state = BattleGraphicState.Empty;
        private CombatPresentationFrame? _frame;
        private IReadOnlyList<MapSymbolEntry>? _symbols;

        /// <summary>Last applied battle graphic (headless-readable after Refresh).</summary>
        public BattleGraphicState LastBattleGraphic => _state;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
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
            Refresh();
        }

        private void LateUpdate()
        {
            if (!showPanel || bridgeHost == null)
            {
                if (_root != null)
                {
                    _root.style.display = DisplayStyle.None;
                }

                return;
            }

            if (!_wired)
            {
                TryWireElements();
            }

            var frame = bridgeHost.DisplayCombatFrame;
            var symbols = bridgeHost.DisplayCombatSymbols;
            if (_wired
                && ReferenceEquals(frame, _frame)
                && ReferenceEquals(symbols, _symbols))
            {
                return;
            }

            Refresh();
        }

        /// <summary>Inspect one engagement key. Presentation selection only — not a sim order.</summary>
        public void Inspect(string? key)
        {
            _selectedKey = key;
            _frame = null;
            Refresh();
        }

        /// <summary>Changes graphic density without writing simulation state.</summary>
        public void SetZoom(CombatZoomBand zoom)
        {
            _zoom = zoom;
            _frame = null;
            Refresh();
        }

        /// <summary>Apply the current combat-frame projection onto the engagement labels.</summary>
        public void Refresh()
        {
            if (bridgeHost == null)
            {
                _state = BattleGraphicState.Empty;
            }
            else
            {
                var symbols = bridgeHost.DisplayCombatSymbols;
                _frame = bridgeHost.DisplayCombatFrame;
                _symbols = symbols;
                _state = bridgeHost.ProjectBattleGraphic(symbols, _zoom, _selectedKey);
            }

            ApplyRows();
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            _root = root.Q<VisualElement>(RootName) ?? root;
            _summary = _root.Q<Label>(SummaryName);
            _line = _root.Q<Label>(LineName);
            _track = _root.Q<Label>(TrackName);
            _trail = _root.Q<Label>(TrailName);
            _detail = _root.Q<Label>(DetailName);
            _tactical = _root.Q<Button>("battle-graphic-zoom-tactical");
            _operational = _root.Q<Button>("battle-graphic-zoom-operational");
            _theater = _root.Q<Button>("battle-graphic-zoom-theater");

            if (panelStyles != null && !_root.styleSheets.Contains(panelStyles))
            {
                _root.styleSheets.Add(panelStyles);
            }

            if (_tactical != null)
            {
                _tactical.clicked -= OnTactical;
                _tactical.clicked += OnTactical;
            }

            if (_operational != null)
            {
                _operational.clicked -= OnOperational;
                _operational.clicked += OnOperational;
            }

            if (_theater != null)
            {
                _theater.clicked -= OnTheater;
                _theater.clicked += OnTheater;
            }

            _wired = _summary != null && _line != null && _track != null && _trail != null && _detail != null;
        }

        private void OnTactical() => SetZoom(CombatZoomBand.Tactical);

        private void OnOperational() => SetZoom(CombatZoomBand.Operational);

        private void OnTheater() => SetZoom(CombatZoomBand.Theater);

        private void ApplyRows()
        {
            if (_root != null)
            {
                _root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }

            var rows = BattleGraphicPanelBinder.BindRows(_state);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var label = LabelFor(row.ElementName);
                if (label == null)
                {
                    continue;
                }

                label.text = row.Text;
                label.tooltip = row.Tooltip;
                ApplyCue(label, row.CueClass);
            }
        }

        private Label? LabelFor(string elementName) =>
            elementName switch
            {
                SummaryName => _summary,
                LineName => _line,
                TrackName => _track,
                TrailName => _trail,
                DetailName => _detail,
                _ => null,
            };

        private static void ApplyCue(VisualElement element, string cueClass)
        {
            var all = BattleGraphicCueClasses.All;
            for (var i = 0; i < all.Count; i++)
            {
                element.RemoveFromClassList(all[i]);
            }

            if (!string.IsNullOrEmpty(cueClass))
            {
                element.AddToClassList(cueClass);
            }
        }
    }
}
#endif
