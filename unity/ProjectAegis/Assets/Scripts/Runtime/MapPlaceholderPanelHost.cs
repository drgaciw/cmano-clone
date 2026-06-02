// Tactical map placeholder (ADR-007 Phase A) — UI Toolkit canvas.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
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

        private UIDocument _document = null!;
        private VisualElement? _rootPanel;
        private Label? _theaterLabel;
        private VisualElement? _canvas;
        private MapPanelState _panelState = new("—", Array.Empty<MapSymbolDisplayRow>());
        private bool _wired;

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
            if (!_wired || bridgeHost == null || _canvas == null)
            {
                return;
            }

            _panelState = MapPanelBinder.Bind(bridgeHost.LastMapSymbols, bridgeHost.ScenarioPolicyId);
            _theaterLabel!.text = $"THEATER: {_panelState.TheaterLabel}";
            RebuildSymbols();
            _rootPanel!.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RebuildSymbols()
        {
            _canvas!.Clear();
            foreach (var row in _panelState.Symbols)
            {
                var label = new Label($"{row.Glyph} {row.Label}")
                {
                    style =
                    {
                        position = Position.Absolute,
                        left = Length.Percent(row.NormalizedX * 100f),
                        top = Length.Percent(row.NormalizedY * 100f),
                    },
                };
                label.AddToClassList("map-symbol");
                label.AddToClassList(row.StyleClass);
                _canvas.Add(label);
            }
        }
    }
}
#endif