#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>Thin map/history view of the shared combat frame. Creates no simulation facts or orders.</summary>
    public sealed class CombatMapView : IDisposable
    {
        private readonly VisualElement _canvas;
        private readonly VisualElement _layer;
        private readonly VisualElement _controls;
        private readonly ScrollView _history;
        private readonly Label _summary;
        private readonly Action<string?> _select;
        private CombatPresentationFrame? _frame;
        private IReadOnlyList<MapSymbolEntry>? _symbols;
        private string? _key;
        private CombatZoomBand _zoom = CombatZoomBand.Tactical;
        private CombatMapPresentation _presentation = CombatMapPresentation.Empty;
        private readonly List<Button> _historyButtons = new();
        private readonly List<EffectSlot> _effectSlots = new();
        private int _historyOffset;

        private sealed class EffectSlot
        {
            public Button Label = null!;
            public readonly List<VisualElement> Segments = new();
        }

        /// <summary>Creates bounded, keyboard-accessible inspection controls on the existing map surface.</summary>
        public CombatMapView(VisualElement canvas, VisualElement panel, Action<string?> select)
        {
            _canvas = canvas;
            _select = select;
            _layer = new VisualElement { name = "combat-event-layer", pickingMode = PickingMode.Ignore };
            _layer.style.position = Position.Absolute;
            _layer.style.left = _layer.style.top = _layer.style.right = _layer.style.bottom = 0;
            canvas.Add(_layer);
            _controls = new VisualElement { name = "combat-inspection-controls" };
            _controls.style.backgroundColor = new Color(0.035f, 0.055f, 0.075f, 0.94f);
            _controls.style.paddingLeft = _controls.style.paddingRight = 8;
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            foreach (CombatZoomBand band in Enum.GetValues(typeof(CombatZoomBand)))
            {
                var value = band;
                toolbar.Add(new Button(() => SetZoom(value)) { text = value.ToString(), tooltip = "Combat display density" });
            }
            toolbar.Add(new Button(() => Select(null)) { text = "Follow selection", tooltip = "Clear history inspection" });
            toolbar.Add(new Button(() => { _historyOffset += 200; Rebuild(); }) { text = "Earlier events" });
            toolbar.Add(new Button(() => { _historyOffset = 0; Rebuild(); }) { text = "Latest events" });
            _controls.Add(toolbar);
            _summary = new Label { name = "combat-event-summary" };
            _summary.style.color = Color.white;
            _summary.style.whiteSpace = WhiteSpace.Normal;
            _controls.Add(_summary);
            var foldout = new Foldout { text = "Combat event history — select to inspect", value = false };
            _history = new ScrollView { name = "combat-event-history" };
            _history.style.maxHeight = 140;
            foldout.Add(_history);
            _controls.Add(foldout);
            panel.Add(_controls);
            canvas.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        /// <summary>Current projected effect count, bounded by the presenter.</summary>
        public int EffectCount => _presentation.Effects.Count;

        /// <summary>Rebuilds on simulation-frame, symbols or inspection changes only.</summary>
        public void Bind(CombatPresentationFrame frame, IReadOnlyList<MapSymbolEntry> symbols, string? selectedKey)
        {
            if (ReferenceEquals(_frame, frame) && ReferenceEquals(_symbols, symbols) && _key == selectedKey) return;
            _frame = frame;
            _symbols = symbols;
            _key = selectedKey;
            Rebuild();
        }

        /// <summary>Changes visual density without changing simulation state or camera authority.</summary>
        public void SetZoom(CombatZoomBand zoom)
        {
            _zoom = zoom;
            Rebuild();
        }

        private void Select(string? key)
        {
            _key = key;
            _select(key);
            Rebuild();
        }

        private void Rebuild()
        {
            if (_frame == null || _symbols == null) return;
            var combatSymbols = CombatMapSymbolBridge.Build(_symbols, _frame.Contacts.Contacts);
            _presentation = CombatMapPresenter.Build(_frame.Events, combatSymbols, _frame.SimTime, _zoom, _key);
            _summary.text = $"Combat: {_zoom} | {_presentation.Effects.Count} visible | {_presentation.EventLines.Count} event facts";
            _historyOffset = Math.Min(_historyOffset, Math.Max(0, _presentation.EventLines.Count - 1));
            var first = Math.Max(0, _presentation.EventLines.Count - 200 - _historyOffset);
            var end = Math.Min(_presentation.EventLines.Count, first + 200);
            for (var i = first; i < end; i++)
            {
                var row = _presentation.EventLines[i];
                var index = i - first;
                if (index == _historyButtons.Count)
                {
                    var created = new Button();
                    created.clicked += () => Select(created.userData as string);
                    created.style.whiteSpace = WhiteSpace.Normal;
                    created.style.color = Color.white;
                    _historyButtons.Add(created);
                    _history.Add(created);
                }
                var button = _historyButtons[index];
                button.userData = row.Key;
                button.text = row.Text;
                button.tooltip = row.Text;
                button.style.display = DisplayStyle.Flex;
            }
            for (var i = end - first; i < _historyButtons.Count; i++) _historyButtons[i].style.display = DisplayStyle.None;
            RenderEffects();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) => RenderEffects();

        private void RenderEffects()
        {
            var width = _canvas.resolvedStyle.width;
            var height = _canvas.resolvedStyle.height;
            if (float.IsNaN(width) || float.IsNaN(height) || width <= 0 || height <= 0) return;
            for (var rowIndex = 0; rowIndex < _presentation.Effects.Count; rowIndex++)
            {
                var row = _presentation.Effects[rowIndex];
                if (rowIndex == _effectSlots.Count)
                {
                    var created = new Button { name = "combat-effect-label" };
                    created.clicked += () => Select(created.userData as string);
                    created.style.position = Position.Absolute;
                    created.style.maxWidth = 340;
                    created.style.whiteSpace = WhiteSpace.Normal;
                    created.style.fontSize = 12;
                    created.style.color = Color.white;
                    created.style.backgroundColor = new Color(0.035f, 0.055f, 0.075f, 0.94f);
                    _layer.Add(created);
                    _effectSlots.Add(new EffectSlot { Label = created });
                }
                var slot = _effectSlots[rowIndex];
                var usedSegments = 0;
                var from = new Vector2(row.FromX * width, row.FromY * height);
                var to = new Vector2(row.ToX * width, row.ToY * height);
                var delta = to - from;
                var length = delta.magnitude;
                var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                var key = row.CorrelationKeys.Count > 0 ? row.CorrelationKeys[0] : row.Key;
                if (row.LinePattern != "none" && length > 0)
                {
                    var segments = row.LinePattern == "dash" ? 12 : row.LinePattern == "dot" ? 24 : 1;
                    for (var i = 0; i < segments; i++)
                    {
                        var start = from + delta * ((float)i / segments);
                        var segmentLength = length / segments * (segments == 1 ? 1f : row.LinePattern == "dot" ? 0.18f : 0.6f);
                        ApplySegment(slot, usedSegments++, start, segmentLength, angle, row.LinePattern == "double-solid" ? -2 : 0);
                        if (row.LinePattern == "double-solid") ApplySegment(slot, usedSegments++, start, segmentLength, angle, 2);
                    }
                }
                for (var i = usedSegments; i < slot.Segments.Count; i++) slot.Segments[i].style.display = DisplayStyle.None;
                var label = slot.Label;
                label.userData = key;
                label.text = row.Label;
                label.tooltip = row.CorrelationKeys.Count > 1 ? row.Label + " — inspect first engagement; all members are in history." : row.Label;
                label.style.display = DisplayStyle.Flex;
                label.style.left = Mathf.Clamp((from.x + to.x) / 2, 0, Mathf.Max(0, width - 340));
                label.style.top = Mathf.Clamp((from.y + to.y) / 2, 0, Mathf.Max(0, height - 50));
            }
            for (var i = _presentation.Effects.Count; i < _effectSlots.Count; i++)
            {
                _effectSlots[i].Label.style.display = DisplayStyle.None;
                foreach (var segment in _effectSlots[i].Segments) segment.style.display = DisplayStyle.None;
            }
        }

        private void ApplySegment(EffectSlot slot, int index, Vector2 start, float length, float angle, float offset)
        {
            if (index == slot.Segments.Count)
            {
                var created = new VisualElement { pickingMode = PickingMode.Ignore };
                created.style.position = Position.Absolute;
                created.style.height = 2;
                created.style.backgroundColor = Color.white;
                created.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50), 0);
                _layer.Insert(0, created);
                slot.Segments.Add(created);
            }
            var segment = slot.Segments[index];
            segment.style.display = DisplayStyle.Flex;
            segment.style.left = start.x;
            segment.style.top = start.y + offset;
            segment.style.width = length;
            segment.style.rotate = new Rotate(angle);
        }

        /// <summary>Detaches controls and callbacks when the document is replaced or disabled.</summary>
        public void Dispose()
        {
            _canvas.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _layer.RemoveFromHierarchy();
            _controls.RemoveFromHierarchy();
        }
    }
}
#endif
