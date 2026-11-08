// Plotted-course polylines (CMD-38 / CMD-30.7) — separate from rings/edges and Track C VFX.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Draws projected course segments on a dedicated canvas layer. Must not share
    /// elements with <see cref="MapCanvasOverlayRenderer"/> or Track C VFX.
    /// Presentation-only: no DecisionLog writes, no bridge hotpath.
    /// </summary>
    public sealed class MapCanvasCourseOverlayRenderer
    {
        public const string LayerName = "map-overlay-course-layer";
        public const string SegmentBaseClass = "map-overlay-course";

        private const float LayoutEpsilon = 0.5f;

        private sealed class SegmentEntry
        {
            public VisualElement Element = null!;
            public MapCanvasEdgeShape? Applied;
        }

        private readonly VisualElement _canvas;
        private readonly VisualElement _layer;
        private readonly Dictionary<string, SegmentEntry> _segments = new(StringComparer.Ordinal);
        private readonly List<string> _stale = new();
        private float _layoutWidth;
        private float _layoutHeight;

        public MapCanvasCourseOverlayRenderer(VisualElement canvas)
        {
            if (canvas == null)
            {
                throw new ArgumentNullException(nameof(canvas));
            }

            _canvas = canvas;
            _layer = new VisualElement { name = LayerName, pickingMode = PickingMode.Ignore };
            _layer.AddToClassList("map-overlay-layer");
            _layer.AddToClassList("map-overlay-course-layer");
            InsertAfterStaticOverlays(canvas, _layer);
            canvas.RegisterCallback<GeometryChangedEvent>(OnCanvasGeometryChanged);
        }

        /// <summary>Live pooled course-segment count (diagnostics / tests).</summary>
        public int SegmentCount => _segments.Count;

        public void Sync(IReadOnlyList<MapCanvasEdgeShape>? segments)
        {
            CaptureLayoutSize();
            SyncSegments(segments ?? Array.Empty<MapCanvasEdgeShape>());
        }

        public void Clear()
        {
            _layer.Clear();
            _segments.Clear();
        }

        private static void InsertAfterStaticOverlays(VisualElement canvas, VisualElement courseLayer)
        {
            var edge = canvas.Q<VisualElement>("map-overlay-edge-layer");
            if (edge != null)
            {
                canvas.Insert(canvas.IndexOf(edge) + 1, courseLayer);
                return;
            }

            canvas.Add(courseLayer);
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
            foreach (var entry in _segments.Values)
            {
                if (entry.Applied != null)
                {
                    ApplyPixels(entry.Element, entry.Applied);
                }
            }
        }

        private void SyncSegments(IReadOnlyList<MapCanvasEdgeShape> segments)
        {
            for (var i = 0; i < segments.Count; i++)
            {
                var shape = segments[i];
                if (!_segments.TryGetValue(shape.Key, out var entry))
                {
                    entry = new SegmentEntry { Element = CreateElement(shape.Key) };
                    _segments[shape.Key] = entry;
                    _layer.Add(entry.Element);
                }

                if (entry.Applied != shape)
                {
                    Apply(entry.Element, entry.Applied, shape);
                    entry.Applied = shape;
                }
                else
                {
                    ApplyPixels(entry.Element, shape);
                }
            }

            PruneStale(segments);
        }

        private void PruneStale(IReadOnlyList<MapCanvasEdgeShape> live)
        {
            _stale.Clear();
            foreach (var kvp in _segments)
            {
                if (!ContainsKey(live, kvp.Key))
                {
                    _stale.Add(kvp.Key);
                }
            }

            foreach (var key in _stale)
            {
                _segments[key].Element.RemoveFromHierarchy();
                _segments.Remove(key);
            }
        }

        private static bool ContainsKey(IReadOnlyList<MapCanvasEdgeShape> shapes, string key)
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

        private static VisualElement CreateElement(string key)
        {
            var element = new VisualElement
            {
                userData = key,
                pickingMode = PickingMode.Ignore,
            };
            element.AddToClassList(SegmentBaseClass);
            return element;
        }

        private void Apply(VisualElement element, MapCanvasEdgeShape? previous, MapCanvasEdgeShape shape)
        {
            element.style.position = Position.Absolute;
            element.style.height = 2;
            element.style.transformOrigin = new TransformOrigin(Length.Percent(0f), Length.Percent(50f), 0);
            ApplyPixels(element, shape);

            if (previous != null)
            {
                element.RemoveFromClassList(previous.StyleClass);
            }

            element.AddToClassList(shape.StyleClass);
        }

        private void ApplyPixels(VisualElement element, MapCanvasEdgeShape shape)
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

            var px = MapCanvasOverlayGeometry.LayoutEdgePixels(shape, width, height);
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

        private bool TryGetLayoutSize(out float width, out float height)
        {
            width = _layoutWidth > LayoutEpsilon ? _layoutWidth : _canvas.resolvedStyle.width;
            height = _layoutHeight > LayoutEpsilon ? _layoutHeight : _canvas.resolvedStyle.height;
            return width > LayoutEpsilon && height > LayoutEpsilon;
        }
    }
}
#endif
