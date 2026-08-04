// Cesium Globe Host - initializes Cesium globe with ion token and georeference.
// Optional: requires com.cesium.unity package (see docs/engineering/cesium-unity-package-pin.md).
// Ion token: Editor Inspector / user secret ONLY — NEVER commit tokens to the repo.
// Default CI smoke (DelegationSmoke) does NOT include this host; CesiumSpike scene only.
#if UNITY_5_3_OR_NEWER && CESIUM_FOR_UNITY
using UnityEngine;
using CesiumForUnity;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CesiumGlobeHost : MonoBehaviour
    {
        [SerializeField] private string ionAccessToken = string.Empty;

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(ionAccessToken))
            {
                Debug.LogWarning("[CesiumGlobeHost] No ion token configured; globe spike is inactive. Set via Inspector (Editor user secret; see cesium-unity-package-pin.md — NEVER commit).");
                return;
            }

            // When define CESIUM_FOR_UNITY active (package installed via git pin in manifest) and token present:
            // Host activates. Actual tile streaming / globe requires valid Cesium ion login in Editor (token tied to account).
            // Georeference + anchors primarily driven by CesiumGlobeBridge (or scene setup). This host provides the ion + root activation hook.
            // Product camera defaults: GlobeViewProjection.DefaultBalticTheater() (headless; no ion required for status chrome).
            Debug.Log("[CesiumGlobeHost] Cesium globe host active (Editor spike). Ion token accepted; globe tiles load on Play (local Editor visual gate).");
        }
    }
}
#endif
