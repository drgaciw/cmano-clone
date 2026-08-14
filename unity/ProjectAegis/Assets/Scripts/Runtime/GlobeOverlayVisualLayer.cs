// Globe overlay visual bind (DRG-161 / S121-CESIUM) — UI Toolkit only, no Cesium package.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Draws projected envelope rings and datalink edges on the product globe host.
    /// Uses headless <see cref="GlobeOverlayScreenProjection"/> — presentation-only.
    /// </summary>
    internal sealed class GlobeOverlayVisualLayer : VisualElement
    {
        private const string LayerName = "globe-overlay-layer";

        private IReadOnlyList<GlobeEnvelopeRingMarker> _rings = System.Array.Empty<GlobeEnvelopeRingMarker>();
        private IReadOnlyList<GlobeDatalinkEdgeMarker> _edges = System.Array.Empty<GlobeDatalinkEdgeMarker>();
        private GlobeCameraState _camera = GlobeViewProjection.DefaultBalticTheater().Camera;

        public GlobeOverlayVisualLayer()
        {
            name = LayerName;
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.right = 0;
            style.top = 0;
            style.bottom = 0;
            generateVisualContent += OnGenerateVisualContent;
        }

        public void Bind(
            GlobeCameraState camera,
            IReadOnlyList<GlobeEnvelopeRingMarker> rings,
            IReadOnlyList<GlobeDatalinkEdgeMarker> edges)
        {
            _camera = camera ?? throw new System.ArgumentNullException(nameof(camera));
            _rings = rings ?? System.Array.Empty<GlobeEnvelopeRingMarker>();
            _edges = edges ?? System.Array.Empty<GlobeDatalinkEdgeMarker>();
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            if (painter == null)
            {
                return;
            }

            var width = contentRect.width;
            var height = contentRect.height;
            if (width <= 1f || height <= 1f)
            {
                return;
            }

            foreach (var edge in _edges)
            {
                if (edge is null)
                {
                    continue;
                }

                var from = GlobeOverlayScreenProjection.Project(edge.FromLatitude, edge.FromLongitude, _camera);
                var to = GlobeOverlayScreenProjection.Project(edge.ToLatitude, edge.ToLongitude, _camera);
                painter.strokeColor = ResolveEdgeColor(edge.Status);
                painter.lineWidth = 2f;
                painter.BeginPath();
                painter.MoveTo(ToLocal(from, width, height));
                painter.LineTo(ToLocal(to, width, height));
                painter.Stroke();
            }

            foreach (var ring in _rings)
            {
                if (ring is null || ring.Polyline.Count < 2)
                {
                    continue;
                }

                var projected = GlobeOverlayScreenProjection.ProjectPolyline(ring.Polyline, _camera);
                if (projected.Count < 2)
                {
                    continue;
                }

                painter.strokeColor = ResolveRingColor(ring.RingKind);
                painter.lineWidth = ring.IsSelectedUnit ? 2.5f : 1.5f;
                painter.BeginPath();
                var first = ToLocal(projected[0], width, height);
                painter.MoveTo(first);
                for (var i = 1; i < projected.Count; i++)
                {
                    painter.LineTo(ToLocal(projected[i], width, height));
                }

                painter.Stroke();
            }
        }

        private static Vector2 ToLocal(GlobeViewportPoint point, float width, float height) =>
            new((float)(point.X * width), (float)((1.0 - point.Y) * height));

        private static Color ResolveRingColor(string ringKind) =>
            string.Equals(ringKind, TacticalOverlayProjection.RingKindWeapon, System.StringComparison.Ordinal)
                ? new Color(1f, 0.45f, 0.2f, 0.85f)
                : new Color(0.35f, 0.75f, 1f, 0.85f);

        private static Color ResolveEdgeColor(string status) =>
            status switch
            {
                DatalinkPictureProjection.StatusDegraded => new Color(1f, 0.85f, 0.2f, 0.9f),
                DatalinkPictureProjection.StatusDown => new Color(0.9f, 0.25f, 0.25f, 0.9f),
                _ => new Color(0.4f, 0.95f, 0.55f, 0.9f),
            };
    }
}
#endif
