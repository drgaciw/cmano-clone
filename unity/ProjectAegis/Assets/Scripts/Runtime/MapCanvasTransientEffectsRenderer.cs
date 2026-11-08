// Transient CMO-style combat VFX layer (Track C) — separate from static rings/edges.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Draws projected <see cref="CombatVfxFrame"/> fire lines and impact markers on a
    /// dedicated canvas layer. Must not share elements with <see cref="MapCanvasOverlayRenderer"/>.
    /// Presentation-only: no DecisionLog writes, no bridge hotpath, no RNG.
    /// </summary>
    public sealed class MapCanvasTransientEffectsRenderer
    {
        public const string LineLayerName = "map-combat-vfx-line-layer";
        public const string MarkerLayerName = "map-combat-vfx-marker-layer";
        public const string LineBaseClass = "map-combat-vfx-fireline";
        public const string MarkerBaseClass = "map-combat-vfx-impact";

        private const float LayoutEpsilon = 0.5f;
        private const float MarkerSizePx = 8f;

        private sealed class LineEntry
        {
            public VisualElement Element = null!;
            public CombatVfxFireLine? Applied;
        }

        private sealed class MarkerEntry
        {
            public VisualElement Element = null!;
            public CombatVfxImpactMarker? Applied;
        }

        private readonly VisualElement _canvas;
        private readonly VisualElement _lineLayer;
        private readonly VisualElement _markerLayer;
        private readonly Dictionary<string, LineEntry> _lines = new(StringComparer.Ordinal);
        private readonly Dictionary<string, MarkerEntry> _markers = new(StringComparer.Ordinal);
        private readonly List<string> _stale = new();
        private float _layoutWidth;
        private float _layoutHeight;

        public MapCanvasTransientEffectsRenderer(VisualElement canvas)
        {
            if (canvas == null)
            {
                throw new ArgumentNullException(nameof(canvas));
            }

            _canvas = canvas;
            _lineLayer = new VisualElement { name = LineLayerName, pickingMode = PickingMode.Ignore };
            _lineLayer.AddToClassList("map-overlay-layer");
            _lineLayer.AddToClassList("map-combat-vfx-layer");
            _markerLayer = new VisualElement { name = MarkerLayerName, pickingMode = PickingMode.Ignore };
            _markerLayer.AddToClassList("map-overlay-layer");
            _markerLayer.AddToClassList("map-combat-vfx-layer");
            InsertAfterStaticOverlays(canvas, _lineLayer, _markerLayer);
            canvas.RegisterCallback<GeometryChangedEvent>(OnCanvasGeometryChanged);
        }

        /// <summary>Live pooled fire-line count (diagnostics / tests).</summary>
        public int FireLineCount => _lines.Count;

        /// <summary>Live pooled impact-marker count (diagnostics / tests).</summary>
        public int ImpactMarkerCount => _markers.Count;

        /// <summary>Reconciles the transient layer to the projected VFX frame.</summary>
        public void Sync(CombatVfxFrame? frame)
        {
            CaptureLayoutSize();
            var live = frame ?? CombatVfxFrame.Empty;
            SyncLines(live.FireLines ?? Array.Empty<CombatVfxFireLine>());
            SyncMarkers(live.ImpactMarkers ?? Array.Empty<CombatVfxImpactMarker>());
        }

        /// <summary>Detach all transient VFX elements.</summary>
        public void Clear()
        {
            _lineLayer.Clear();
            _markerLayer.Clear();
            _lines.Clear();
            _markers.Clear();
        }

        private static void InsertAfterStaticOverlays(
            VisualElement canvas,
            VisualElement lineLayer,
            VisualElement markerLayer)
        {
            var edge = canvas.Q<VisualElement>("map-overlay-edge-layer");
            if (edge != null)
            {
                var edgeIndex = canvas.IndexOf(edge);
                canvas.Insert(edgeIndex + 1, lineLayer);
                canvas.Insert(edgeIndex + 2, markerLayer);
                return;
            }

            canvas.Add(lineLayer);
            canvas.Add(markerLayer);
        }

        private void OnCanvasGeometryChanged(GeometryChangedEvent evt)
        {
            var width = evt.newRect.width;
            var height = evt.newRect.height;
            if (Math.Abs(width - _layoutWidth) < LayoutEpsilon
                && Math.Abs(height - _layoutHeight) < LayoutEpsilon)
            {
                return;
            }

            _layoutWidth = width;
            _layoutHeight = height;
            RelayoutApplied();
        }

        private void CaptureLayoutSize()
        {
            _layoutWidth = _canvas.resolvedStyle.width;
            _layoutHeight = _canvas.resolvedStyle.height;
        }

        private void RelayoutApplied()
        {
            foreach (var entry in _lines.Values)
            {
                if (entry.Applied != null)
                {
                    ApplyLinePixels(entry.Element, entry.Applied);
                }
            }

            foreach (var entry in _markers.Values)
            {
                if (entry.Applied != null)
                {
                    ApplyMarkerPixels(entry.Element, entry.Applied);
                }
            }
        }

        private void SyncLines(IReadOnlyList<CombatVfxFireLine> lines)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                var shape = lines[i];
                if (!_lines.TryGetValue(shape.Key, out var entry))
                {
                    entry = new LineEntry { Element = CreateLineElement(shape.Key) };
                    _lines[shape.Key] = entry;
                    _lineLayer.Add(entry.Element);
                }

                if (entry.Applied != shape)
                {
                    ApplyLine(entry.Element, entry.Applied, shape);
                    entry.Applied = shape;
                }
                else
                {
                    ApplyLinePixels(entry.Element, shape);
                }
            }

            PruneLineStale(lines);
        }

        private void SyncMarkers(IReadOnlyList<CombatVfxImpactMarker> markers)
        {
            for (var i = 0; i < markers.Count; i++)
            {
                var shape = markers[i];
                if (!_markers.TryGetValue(shape.Key, out var entry))
                {
                    entry = new MarkerEntry { Element = CreateMarkerElement(shape.Key) };
                    _markers[shape.Key] = entry;
                    _markerLayer.Add(entry.Element);
                }

                if (entry.Applied != shape)
                {
                    ApplyMarker(entry.Element, entry.Applied, shape);
                    entry.Applied = shape;
                }
                else
                {
                    ApplyMarkerPixels(entry.Element, shape);
                }
            }

            PruneMarkerStale(markers);
        }

        private void PruneLineStale(IReadOnlyList<CombatVfxFireLine> live)
        {
            _stale.Clear();
            foreach (var kvp in _lines)
            {
                if (!ContainsLineKey(live, kvp.Key))
                {
                    _stale.Add(kvp.Key);
                }
            }

            foreach (var key in _stale)
            {
                _lines[key].Element.RemoveFromHierarchy();
                _lines.Remove(key);
            }
        }

        private void PruneMarkerStale(IReadOnlyList<CombatVfxImpactMarker> live)
        {
            _stale.Clear();
            foreach (var kvp in _markers)
            {
                if (!ContainsMarkerKey(live, kvp.Key))
                {
                    _stale.Add(kvp.Key);
                }
            }

            foreach (var key in _stale)
            {
                _markers[key].Element.RemoveFromHierarchy();
                _markers.Remove(key);
            }
        }

        private static bool ContainsLineKey(IReadOnlyList<CombatVfxFireLine> shapes, string key)
        {
            for (var i = 0; i < shapes.Count; i++)
            {
                if (string.Equals(shapes[i].Key, key, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsMarkerKey(IReadOnlyList<CombatVfxImpactMarker> shapes, string key)
        {
            for (var i = 0; i < shapes.Count; i++)
            {
                if (string.Equals(shapes[i].Key, key, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static VisualElement CreateLineElement(string key)
        {
            var element = new VisualElement
            {
                userData = key,
                pickingMode = PickingMode.Ignore,
            };
            element.AddToClassList(LineBaseClass);
            return element;
        }

        private static VisualElement CreateMarkerElement(string key)
        {
            var element = new VisualElement
            {
                userData = key,
                pickingMode = PickingMode.Ignore,
            };
            element.AddToClassList(MarkerBaseClass);
            return element;
        }

        private void ApplyLine(VisualElement element, CombatVfxFireLine? previous, CombatVfxFireLine shape)
        {
            element.style.position = Position.Absolute;
            element.style.height = 2;
            element.style.transformOrigin = new TransformOrigin(Length.Percent(0f), Length.Percent(50f), 0);
            ApplyLineFallbackColor(element);
            ApplyLinePixels(element, shape);

            if (previous != null)
            {
                element.RemoveFromClassList(previous.StyleClass);
            }

            element.AddToClassList(shape.StyleClass);
        }

        private void ApplyMarker(VisualElement element, CombatVfxImpactMarker? previous, CombatVfxImpactMarker shape)
        {
            element.style.position = Position.Absolute;
            element.style.width = MarkerSizePx;
            element.style.height = MarkerSizePx;
            ApplyMarkerFallbackColor(element, shape.StyleClass);
            ApplyMarkerPixels(element, shape);

            if (previous != null)
            {
                element.RemoveFromClassList(previous.StyleClass);
            }

            element.AddToClassList(shape.StyleClass);
        }

        private void ApplyLinePixels(VisualElement element, CombatVfxFireLine shape)
        {
            if (!TryGetLayoutSize(out var width, out var height))
            {
                var dx = shape.ToX - shape.FromX;
                var dy = shape.ToY - shape.FromY;
                var length = Mathf.Sqrt((dx * dx) + (dy * dy));
                if (length <= 1e-6f)
                {
                    element.style.display = DisplayStyle.None;
                    return;
                }

                element.style.display = DisplayStyle.Flex;
                element.style.left = Length.Percent(shape.FromX * 100f);
                element.style.top = Length.Percent(shape.FromY * 100f);
                element.style.width = Length.Percent((float)length * 100f);
                element.style.rotate = new Rotate(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
                return;
            }

            var edge = new MapCanvasEdgeShape(
                shape.Key,
                shape.FromX,
                shape.FromY,
                shape.ToX,
                shape.ToY,
                Status: "vfx",
                shape.StyleClass);
            var px = MapCanvasOverlayGeometry.LayoutEdgePixels(edge, width, height);
            if (px.Hidden)
            {
                element.style.display = DisplayStyle.None;
                return;
            }

            element.style.display = DisplayStyle.Flex;
            element.style.left = px.Left;
            element.style.top = px.Top;
            element.style.width = px.Length;
            element.style.rotate = new Rotate(px.AngleDeg);
        }

        private void ApplyMarkerPixels(VisualElement element, CombatVfxImpactMarker shape)
        {
            if (!TryGetLayoutSize(out var width, out var height))
            {
                element.style.left = Length.Percent(shape.X * 100f);
                element.style.top = Length.Percent(shape.Y * 100f);
                element.style.marginLeft = -MarkerSizePx * 0.5f;
                element.style.marginTop = -MarkerSizePx * 0.5f;
                return;
            }

            element.style.left = (shape.X * width) - (MarkerSizePx * 0.5f);
            element.style.top = (shape.Y * height) - (MarkerSizePx * 0.5f);
            element.style.marginLeft = 0;
            element.style.marginTop = 0;
        }

        private bool TryGetLayoutSize(out float width, out float height)
        {
            width = _layoutWidth > LayoutEpsilon ? _layoutWidth : _canvas.resolvedStyle.width;
            height = _layoutHeight > LayoutEpsilon ? _layoutHeight : _canvas.resolvedStyle.height;
            return width > LayoutEpsilon && height > LayoutEpsilon;
        }

        private static void ApplyLineFallbackColor(VisualElement element)
        {
            element.style.backgroundColor = new Color(1f, 180f / 255f, 160f / 255f, 0.85f);
        }

        private static void ApplyMarkerFallbackColor(VisualElement element, string styleClass)
        {
            var color = styleClass switch
            {
                CombatVfxProjection.StyleImpactKill => new Color(232f / 255f, 93f / 255f, 93f / 255f, 0.95f),
                CombatVfxProjection.StyleImpactHit => new Color(1f, 180f / 255f, 160f / 255f, 0.95f),
                CombatVfxProjection.StyleImpactMiss => new Color(140f / 255f, 155f / 255f, 170f / 255f, 0.9f),
                CombatVfxProjection.StyleImpactIntercept => new Color(100f / 255f, 200f / 255f, 140f / 255f, 0.95f),
                _ => new Color(200f / 255f, 210f / 255f, 220f / 255f, 0.9f),
            };
            element.style.backgroundColor = color;
            var radius = styleClass == CombatVfxProjection.StyleImpactMiss ? 0f : Length.Percent(50f);
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }
    }
}
#endif
