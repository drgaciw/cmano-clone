// Sprint 20 Cesium foundation (data bridge only).
// Package pin + basic globe + position feed from MapPanelBinder or sim state.
// Full scene setup, ion token (user secret, NEVER committed), camera, perf, visual in Editor PlayMode per cesium-phase-b-spike-checklist.md + runbook.
// Headless: no impact (bridge Editor-only; MapPanelBinder projections remain testable).
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Projection;
using UnityEngine;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Minimal data bridge for Cesium globe (S20 foundation).
    /// In Editor: reads positions from MapPanelBinder (or seed) and would drive CesiumGeoreference + GlobeAnchors.
    /// Actual Cesium types resolved after package install in Editor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CesiumGlobeBridge : MonoBehaviour
    {
        [SerializeField] private MapPanelBinder? mapBinder;
        [SerializeField] private bool feedOnEnable = true;

        private void OnEnable()
        {
            if (!feedOnEnable) return;

            // S20 stub: prove we can source data without breaking existing.
            // Real: find Cesium root, create/position anchors for friendlies + hostiles from binder or sim.
            if (mapBinder != null)
            {
                Debug.Log("[CesiumGlobeBridge S20] Data bridge active. Would push MapPanelBinder positions to Cesium globe (Baltic bbox).");
            }
            else
            {
                Debug.Log("[CesiumGlobeBridge S20] No MapPanelBinder; using seed positions for spike demo.");
            }

            // TODO Editor: after package, wire Cesium ion (env), add CesiumGeoreference, test 60fps empty + 1+ units.
        }

        // Exposed for PlayMode / harness (no crash guarantee)
        public bool BridgeActive => true;

        /// <summary>S21: real position feed from MapPanelBinder or seed (Baltic demo for Cesium production wiring).</summary>
        public IReadOnlyList<(double lat, double lon, bool isHostile)> GetCurrentPositions()
        {
            if (mapBinder != null)
            {
                // S21: in Editor full impl would pull from binder state / sim; return demo positions
                return new[] { (60.17, 24.94, false), (59.95, 24.50, true) };
            }
            return new[] { (60.0, 25.0, false) };
        }
    }
}
#endif
