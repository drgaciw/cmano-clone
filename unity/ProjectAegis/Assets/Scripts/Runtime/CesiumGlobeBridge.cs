// Sprint 20 Cesium foundation (data bridge only).
// Package pin + basic globe + position feed from MapPanelBinder or sim state.
// Full scene setup, ion token (user secret, NEVER committed), camera, perf, visual in Editor PlayMode per cesium-phase-b-spike-checklist.md + runbook.
// Headless: no impact (bridge Editor-only; MapPanelBinder projections remain testable).
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectAegis.Delegation.Projection;
using UnityEngine;

#if CESIUM_FOR_UNITY
using CesiumForUnity;
#endif

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Real Cesium runtime foundation (S20-03). Data bridge + actual CesiumGeoreference/GlobeAnchor creation in Editor when package active.
    /// Positions sourced from MapPanelBinder (via MapPlaceholderPanelHost.LastMapSymbols which Binder consumes) or sim projections per kickoff.
    /// Baltic bbox demo for 1 friendly + 1 hostile. Ion token: Editor Inspector (never in repo / committed).
    /// Headless / dotnet: unaffected (Editor-only #if + Unity Assets not in sln compile).
    /// Full visual/perf/selection verified local Editor (see cesium-s20-local-editor-evidence.md + CESIUM-SPIKE-SETUP.md).
    /// S25-14: APP-6 billboard glyphs/frames via <see cref="CesiumBillboardProjection"/> + <see cref="App6Sidc"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CesiumGlobeBridge : MonoBehaviour
    {
        [SerializeField] private MapPlaceholderPanelHost? mapHost;
        [SerializeField] private bool feedOnEnable = true;

        private void OnEnable()
        {
            if (!feedOnEnable) return;

            var markers = GetBillboardMarkers();
            Debug.Log($"[CesiumGlobeBridge] Data bridge active ({markers.Count} APP-6 billboard marker(s); Baltic bbox).");

#if CESIUM_FOR_UNITY
            CreateCesiumAnchors();
#endif
        }

        // Exposed for PlayMode / harness (no crash guarantee)
        public bool BridgeActive => true;

        /// <summary>
        /// S25-14: APP-6 billboard markers resolved from map symbols or Baltic demo seed.
        /// Read-only projection; does not mutate sim/catalog (ADR-010).
        /// </summary>
        public IReadOnlyList<CesiumBillboardMarker> GetBillboardMarkers()
        {
            var symbols = mapHost?.CurrentMapSymbols;
            if (symbols is { Count: > 0 })
            {
                return CesiumBillboardProjection.Project(symbols);
            }

            return mapHost != null
                ? CesiumBillboardProjection.ProjectDemoPair()
                : CesiumBillboardProjection.ProjectSeed();
        }

        /// <summary>S21: real position feed from MapPanelBinder or seed (Baltic demo for Cesium production wiring).
        /// S25-14: affiliation derived from APP-6 billboard markers.</summary>
        public IReadOnlyList<(double lat, double lon, bool isHostile)> GetCurrentPositions()
        {
            return GetBillboardMarkers()
                .Select(m => (m.Latitude, m.Longitude, string.Equals(m.Affiliation, "Hostile", StringComparison.Ordinal)))
                .ToArray();
        }

#if CESIUM_FOR_UNITY
        private void CreateCesiumAnchors()
        {
            // Find or create the georeference root (globe origin). In full spike scene per CESIUM-SPIKE-SETUP, one is placed in hierarchy.
            var georef = FindFirstObjectByType<CesiumGeoreference>();
            if (georef == null)
            {
                var georefGO = new GameObject("CesiumGeoreference");
                georef = georefGO.AddComponent<CesiumGeoreference>();
                // Reasonable origin near Baltic for spike (user can retune in Editor inspector).
                georef.latitude = 60.0;
                georef.longitude = 24.8;
                georef.height = 1000000.0; // start high for overview
            }

            var markers = GetBillboardMarkers();
            foreach (var marker in markers)
            {
                Debug.Log(
                    $"[CesiumGlobeBridge] Billboard {marker.SymbolId}: frame={marker.UssFrameId} glyph={marker.UnicodeGlyph} sidc={marker.Sidc}");

                var anchorGO = new GameObject($"Cesium_{marker.SymbolId}_{marker.UssFrameId}");
                var anchor = anchorGO.AddComponent<CesiumGlobeAnchor>();
                anchor.latitude = marker.Latitude;
                anchor.longitude = marker.Longitude;
                anchor.height = 200.0; // meters above surface for billboard visibility

                // Visual marker (primitive for spike; production replaces with USS atlas billboard prefab).
                // S24-08 depth/occlusion: 25 km sphere scale keeps billboards visible at Baltic overview altitude.
                // S25-14: color by affiliation; name encodes APP-6 glyph + USS frame id for Editor inspection.
                var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = $"{marker.Affiliation}_{marker.UnicodeGlyph}_Visual";
                visual.transform.SetParent(anchorGO.transform, false);
                visual.transform.localScale = Vector3.one * 25000f;
                var rend = visual.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.color = AffiliationColor(marker.Affiliation, marker.UssFrameId);
                    rend.material = mat;
                }

                anchorGO.transform.SetParent(georef.transform, false);
            }

            Debug.Log($"[CesiumGlobeBridge] Created {markers.Count} CesiumGlobeAnchor(s) with APP-6 billboards under CesiumGeoreference (package active).");
        }

        private static Color AffiliationColor(string affiliation, string ussFrameId)
        {
            if (string.Equals(ussFrameId, App6Sidc.FallbackFrame, StringComparison.Ordinal))
            {
                return new Color(0.85f, 0.75f, 0.2f);
            }

            return affiliation switch
            {
                "Hostile" => Color.red,
                "Friendly" => Color.green,
                _ => new Color(0.7f, 0.7f, 0.7f),
            };
        }
#endif
    }
}
#endif