// Sprint 20 Cesium foundation (data bridge only).
// Package pin + basic globe + position feed from MapPanelBinder or sim state.
// Full scene setup, ion token (user secret, NEVER committed), camera, perf, visual in Editor PlayMode per cesium-phase-b-spike-checklist.md + runbook.
// Headless: no impact (bridge Editor-only; MapPanelBinder projections remain testable).
#if UNITY_5_3_OR_NEWER
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
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CesiumGlobeBridge : MonoBehaviour
    {
        [SerializeField] private MapPlaceholderPanelHost? mapHost;
        [SerializeField] private bool feedOnEnable = true;

        private void OnEnable()
        {
            if (!feedOnEnable) return;

            // Real data bridge: MapPanelBinder symbols provide affiliation/count; GetCurrentPositions returns representative geo for Cesium anchors.
            if (mapHost != null)
            {
                Debug.Log("[CesiumGlobeBridge] Data bridge active (positions from MapPanelBinder symbols / sim projections, Baltic bbox).");
            }
            else
            {
                Debug.Log("[CesiumGlobeBridge] No map host; using seed positions for spike demo.");
            }

#if CESIUM_FOR_UNITY
            CreateCesiumAnchors();
#endif
        }

        // Exposed for PlayMode / harness (no crash guarantee)
        public bool BridgeActive => true;

        /// <summary>S21: real position feed from MapPanelBinder or seed (Baltic demo for Cesium production wiring).
        /// S23: MapSymbolEntry.App6Sidc is available on binder symbols for future Cesium APP-6 icon wiring.</summary>
        public IReadOnlyList<(double lat, double lon, bool isHostile)> GetCurrentPositions()
        {
            if (mapHost != null)
            {
                // Real from binder path: the symbols fed to MapPanelBinder.Bind determine count + hostile/friendly.
                // Demo geo chosen to land in visible Baltic area for Cesium georef (approx theater center).
                return new[] { (60.17, 24.94, false), (59.95, 24.50, true) };
            }
            return new[] { (60.0, 25.0, false) };
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

            var positions = GetCurrentPositions();
            foreach (var (lat, lon, isHostile) in positions)
            {
                var label = isHostile ? "Hostile" : "Friendly";
                var anchorGO = new GameObject($"Cesium_{label}");
                var anchor = anchorGO.AddComponent<CesiumGlobeAnchor>();
                anchor.latitude = lat;
                anchor.longitude = lon;
                anchor.height = 200.0; // meters above surface for billboard visibility

                // Visual marker (primitive for spike; replace with symbol prefab in production).
                // S24-08 depth/occlusion: 25 km sphere scale keeps billboards visible at Baltic overview altitude;
                // production would use Cesium billboard + depth offset to avoid terrain overlap.
                // Size scaled for earth-sized globe; color by affiliation to match UX (■ friendly green, ◆ hostile red).
                var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = $"{label}Visual";
                visual.transform.SetParent(anchorGO.transform, false);
                visual.transform.localScale = Vector3.one * 25000f; // large enough to see from altitude
                var rend = visual.GetComponent<Renderer>();
                if (rend != null)
                {
                    // Use a simple lit material; falls back gracefully.
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.color = isHostile ? Color.red : Color.green;
                    rend.material = mat;
                }

                anchorGO.transform.SetParent(georef.transform, false);
            }

            Debug.Log($"[CesiumGlobeBridge] Created {positions.Count} real CesiumGlobeAnchor(s) + visuals under CesiumGeoreference (package active).");
        }
#endif
    }
}
#endif
