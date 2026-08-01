// Cesium Globe Host - initializes Cesium globe with ion token and georeference.
// Optional: requires com.cesium.unity package (see docs/engineering/cesium-unity-package-pin.md).
// Ion token: Editor Inspector / user secret / env ONLY — NEVER commit tokens to the repo.
// Env names (presence only; values never logged): CESIUM_ION_TOKEN, ProjectAegis_CesiumIonToken
// Default CI smoke (DelegationSmoke) does NOT include this host; CesiumSpike scene only.
// Wave 4: tile streaming gate + optional Cesium3DTileset when token + package present.
#if UNITY_5_3_OR_NEWER && CESIUM_FOR_UNITY
using System;
using System.Reflection;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using CesiumForUnity;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Ion token + World Terrain tileset activation hook for CesiumSpike (Editor visual gate).
    /// Token value is never logged or written to disk by this host.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CesiumGlobeHost : MonoBehaviour
    {
        /// <summary>Primary env var for ion token (user secret / CI — never commit).</summary>
        public const string IonTokenEnvVar = "CESIUM_ION_TOKEN";

        /// <summary>Alternate env var for Editor user secret naming.</summary>
        public const string IonTokenEnvVarAlt = "ProjectAegis_CesiumIonToken";

        [SerializeField] private string ionAccessToken = string.Empty;

        [SerializeField] private int ionAssetId = GlobeTileStreamingConfig.CesiumWorldTerrainAssetId;

        [SerializeField] private float maximumScreenSpaceError =
            (float)GlobeTileStreamingConfig.DefaultMaximumScreenSpaceError;

        [SerializeField] private bool preloadAncestors = true;

        [SerializeField] private bool preloadSiblings = true;

        [SerializeField] private bool enableFrustumCulling = true;

        private bool _warnedMissingToken;
        private GlobeIonGateState _gateState =
            GlobeIonGateProjection.Project(hasToken: false, packageAvailable: true);
        private GlobeTileStreamingConfig _streamingConfig = GlobeTileStreamingConfig.Default;
        private bool _tilesetCreated;

        /// <summary>
        /// True when a non-empty token is available from Inspector or environment.
        /// Does not expose the token value.
        /// </summary>
        public bool IsIonTokenPresent => ResolveTokenLength() > 0;

        /// <summary>Last projected gate (presence only; no token value).</summary>
        public GlobeIonGateState GateState => _gateState;

        /// <summary>True when token present and package available (this assembly requires CESIUM_FOR_UNITY).</summary>
        public bool StreamingActive => _gateState.StreamingActive;

        /// <summary>Bindable status line for Toolkit / bridge (ACTIVE / NO_ION_TOKEN / PACKAGE_MISSING).</summary>
        public string TileStreamingStatusLine => _gateState.StatusLine;

        /// <summary>Streaming config last applied (asset id / SSE / preload flags).</summary>
        public GlobeTileStreamingConfig StreamingConfig => _streamingConfig;

        private void Awake()
        {
            RefreshGateAndMaybeOpenStreaming();
        }

        /// <summary>
        /// Re-resolve token presence + open streaming gate when possible.
        /// Safe to call from Editor tests; never logs token value.
        /// </summary>
        public void RefreshGateAndMaybeOpenStreaming()
        {
            _streamingConfig = BuildConfig();
            var hasToken = IsIonTokenPresent;
            // This type only exists when CESIUM_FOR_UNITY is active (asmdef defineConstraints).
            const bool packageAvailable = true;
            _gateState = GlobeIonGateProjection.Project(hasToken, packageAvailable, _streamingConfig);

            if (!hasToken)
            {
                if (!_warnedMissingToken)
                {
                    Debug.LogWarning(
                        "[CesiumGlobeHost] No ion token configured; tile streaming gate CLOSED " +
                        "(StreamingActive=false). Set via Inspector or environment variables " +
                        "CESIUM_ION_TOKEN / ProjectAegis_CesiumIonToken " +
                        "(Editor user secret; see cesium-unity-package-pin.md + " +
                        "cesium-ion-visual-gate-2026-08-01.md — NEVER commit tokens).");
                    _warnedMissingToken = true;
                }

                return;
            }

            // Token present + CESIUM_FOR_UNITY: gate OPEN.
            Debug.Log(
                "[CesiumGlobeHost] tile streaming gate OPEN " +
                $"(asset={_streamingConfig.IonAssetId} sse={_streamingConfig.MaximumScreenSpaceError:0.##}; " +
                "token value not logged).");

            ApplyIonTokenWithoutLogging();
            TryCreateWorldTerrainTileset();
        }

        /// <summary>Length of resolved token for presence checks only.</summary>
        private int ResolveTokenLength()
        {
            if (!string.IsNullOrWhiteSpace(ionAccessToken))
            {
                return ionAccessToken.Trim().Length;
            }

            var env = Environment.GetEnvironmentVariable(IonTokenEnvVar);
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env.Trim().Length;
            }

            env = Environment.GetEnvironmentVariable(IonTokenEnvVarAlt);
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env.Trim().Length;
            }

            return 0;
        }

        /// <summary>Resolve token string for Cesium runtime only — never log or serialize out.</summary>
        private string ResolveTokenForRuntime()
        {
            if (!string.IsNullOrWhiteSpace(ionAccessToken))
            {
                return ionAccessToken.Trim();
            }

            var env = Environment.GetEnvironmentVariable(IonTokenEnvVar);
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env.Trim();
            }

            env = Environment.GetEnvironmentVariable(IonTokenEnvVarAlt);
            return string.IsNullOrWhiteSpace(env) ? string.Empty : env.Trim();
        }

        private GlobeTileStreamingConfig BuildConfig() =>
            new(
                IonAssetId: ionAssetId > 0 ? ionAssetId : GlobeTileStreamingConfig.CesiumWorldTerrainAssetId,
                MaximumScreenSpaceError: maximumScreenSpaceError > 0
                    ? maximumScreenSpaceError
                    : GlobeTileStreamingConfig.DefaultMaximumScreenSpaceError,
                PreloadAncestors: preloadAncestors,
                PreloadSiblings: preloadSiblings,
                EnableFrustumCulling: enableFrustumCulling,
                BalticBbox: GlobeTileStreamingConfig.DefaultBalticBbox);

        private void ApplyIonTokenWithoutLogging()
        {
            var token = ResolveTokenForRuntime();
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            // Cesium for Unity pin variance: default token lives on CesiumRuntimeSettings and/or ion server.
            // Resolve types by name so this host compiles across minor API renames; never log the value.
            var settingsType = Type.GetType("CesiumForUnity.CesiumRuntimeSettings, CesiumForUnity")
                               ?? Type.GetType("CesiumForUnity.CesiumRuntimeSettings");
            if (settingsType != null)
            {
                TrySetStaticStringProperty(settingsType, "defaultIonAccessToken", token);
                TrySetStaticStringProperty(settingsType, "defaultIonAccessTokenWebAndDesktop", token);
            }
        }

        private void TryCreateWorldTerrainTileset()
        {
            if (_tilesetCreated)
            {
                return;
            }

            // Actual Cesium3DTileset creation when types are available (this #if branch).
            try
            {
                var existing = FindFirstObjectByType<Cesium3DTileset>();
                Cesium3DTileset tileset;
                if (existing != null)
                {
                    tileset = existing;
                }
                else
                {
                    var tilesetGo = new GameObject("Cesium3DTileset_WorldTerrain");
                    tileset = tilesetGo.AddComponent<Cesium3DTileset>();
                    tilesetGo.transform.SetParent(transform, false);
                }

                // Well-known Cesium3DTileset fields — set via reflection for pin resilience.
                TrySetInstanceProperty(tileset, "ionAssetID", (long)_streamingConfig.IonAssetId);
                TrySetInstanceProperty(tileset, "ionAssetId", (long)_streamingConfig.IonAssetId);
                TrySetInstanceProperty(tileset, "maximumScreenSpaceError", (float)_streamingConfig.MaximumScreenSpaceError);
                TrySetInstanceProperty(tileset, "preloadAncestors", _streamingConfig.PreloadAncestors);
                TrySetInstanceProperty(tileset, "preloadSiblings", _streamingConfig.PreloadSiblings);
                TrySetInstanceProperty(tileset, "enableFrustumCulling", _streamingConfig.EnableFrustumCulling);

                _tilesetCreated = true;
                Debug.Log(
                    "[CesiumGlobeHost] Cesium3DTileset configured for ion asset " +
                    $"{_streamingConfig.IonAssetId} (World Terrain default when id=1). " +
                    "Tiles load on Play with valid ion account.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "[CesiumGlobeHost] Cesium3DTileset creation/config skipped " +
                    $"(types or API unavailable): {ex.GetType().Name}. Gate status remains OPEN; status-only mode.");
            }
        }

        private static void TrySetStaticStringProperty(Type type, string propertyName, string value)
        {
            try
            {
                var prop = type.GetProperty(
                    propertyName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
                {
                    prop.SetValue(null, value);
                    return;
                }

                var field = type.GetField(
                    propertyName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (field != null && field.FieldType == typeof(string))
                {
                    field.SetValue(null, value);
                }
            }
            catch
            {
                // Pin variance — gate still OPEN; user can set token in Cesium ion window.
            }
        }

        private static void TrySetInstanceProperty(object target, string propertyName, object value)
        {
            try
            {
                var type = target.GetType();
                var prop = type.GetProperty(
                    propertyName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite)
                {
                    var converted = Convert.ChangeType(value, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                    prop.SetValue(target, converted);
                    return;
                }

                var field = type.GetField(
                    propertyName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    var converted = Convert.ChangeType(value, Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType);
                    field.SetValue(target, converted);
                }
            }
            catch
            {
                // Optional property missing on this pin.
            }
        }
    }
}
#endif
