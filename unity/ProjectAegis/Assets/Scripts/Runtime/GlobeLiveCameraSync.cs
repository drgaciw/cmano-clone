// Live globe camera readback for GlobeMapProductHost (DRG-161 review).
// Optional Cesium path only — no package dependency for CI / default smoke.
#if UNITY_5_3_OR_NEWER
using System;
using System.Reflection;
using ProjectAegis.Delegation.Projection;
using UnityEngine;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Reads the live Unity/Cesium camera into <see cref="GlobeCameraState"/> when available.
    /// Returns false on headless CI (no Cesium package / no scene camera).
    /// </summary>
    internal static class GlobeLiveCameraSync
    {
        /// <summary>
        /// Try to read the active scene camera pose as product globe view state.
        /// Does not mutate sim or bookmarks — camera pose only.
        /// </summary>
        public static bool TryReadLiveCamera(out GlobeCameraState camera)
        {
#if CESIUM_FOR_UNITY
            if (TryReadCesiumCamera(out camera))
            {
                return true;
            }
#endif
            return TryReadUnityCameraFallback(out camera);
        }

#if CESIUM_FOR_UNITY
        private static bool TryReadCesiumCamera(out GlobeCameraState camera)
        {
            camera = default!;
            var georef = UnityEngine.Object.FindFirstObjectByType(
                Type.GetType("CesiumForUnity.CesiumGeoreference, CesiumForUnity"));
            if (georef == null)
            {
                return false;
            }

            var unityCamera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (unityCamera == null)
            {
                return false;
            }

            if (!TryTransformUnityToLongitudeLatitudeHeight(georef, unityCamera.transform.position, out var lat, out var lon, out var height))
            {
                return false;
            }

            var heading = unityCamera.transform.eulerAngles.y;
            var pitch = NormalizePitch(unityCamera.transform.eulerAngles.x);
            camera = new GlobeCameraState(lat, lon, height, heading, pitch);
            return true;
        }

        private static bool TryTransformUnityToLongitudeLatitudeHeight(
            object georeference,
            Vector3 unityPosition,
            out double latitude,
            out double longitude,
            out double height)
        {
            latitude = 0;
            longitude = 0;
            height = 0;
            var type = georeference.GetType();

            // Pin-resilient: try common Cesium for Unity conversion method names.
            foreach (var methodName in new[]
                     {
                         "TransformUnityPositionToLongitudeLatitudeHeight",
                         "TransformUnityDirectionToEarthCenteredEarthFixed",
                     })
            {
                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                if (method == null)
                {
                    continue;
                }

                try
                {
                    var result = method.Invoke(georeference, new object[] { unityPosition });
                    if (TryReadLongitudeLatitudeHeight(result, out latitude, out longitude, out height))
                    {
                        return true;
                    }
                }
                catch
                {
                    // API variance — fall through.
                }
            }

            // Fallback: georef origin + camera local offset (Baltic spike scenes).
            return TryReadGeoreferenceOrigin(georeference, unityPosition, out latitude, out longitude, out height);
        }

        private static bool TryReadGeoreferenceOrigin(
            object georeference,
            Vector3 unityPosition,
            out double latitude,
            out double longitude,
            out double height)
        {
            latitude = ReadDoubleProperty(georeference, "latitude");
            longitude = ReadDoubleProperty(georeference, "longitude");
            var originHeight = ReadDoubleProperty(georeference, "height");
            if (Math.Abs(latitude) < double.Epsilon && Math.Abs(longitude) < double.Epsilon)
            {
                return false;
            }

            // Rough offset: treat camera Y as altitude above georef when conversion API absent.
            height = originHeight + Math.Max(unityPosition.y, 0);
            return true;
        }

        private static bool TryReadLongitudeLatitudeHeight(
            object? result,
            out double latitude,
            out double longitude,
            out double height)
        {
            latitude = 0;
            longitude = 0;
            height = 0;
            if (result == null)
            {
                return false;
            }

            if (result is Vector3 v3)
            {
                longitude = v3.x;
                latitude = v3.y;
                height = v3.z;
                return true;
            }

            var type = result.GetType();
            longitude = ReadComponent(result, type, "x", "longitude", "Longitude");
            latitude = ReadComponent(result, type, "y", "latitude", "Latitude");
            height = ReadComponent(result, type, "z", "height", "Height");
            return Math.Abs(latitude) > double.Epsilon || Math.Abs(longitude) > double.Epsilon;
        }

        private static double ReadComponent(object target, Type type, params string[] names)
        {
            foreach (var name in names)
            {
                var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    var value = prop.GetValue(target);
                    if (value is double d)
                    {
                        return d;
                    }

                    if (value is float f)
                    {
                        return f;
                    }
                }

                var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    var value = field.GetValue(target);
                    if (value is double d)
                    {
                        return d;
                    }

                    if (value is float f)
                    {
                        return f;
                    }
                }
            }

            return 0;
        }
#endif

        private static bool TryReadUnityCameraFallback(out GlobeCameraState camera)
        {
            camera = default!;
            var unityCamera = Camera.main;
            if (unityCamera == null)
            {
                return false;
            }

            // Non-Cesium smoke: preserve theater lat/lon; adopt live heading/pitch/altitude scale.
            var defaultView = GlobeViewProjection.DefaultBalticTheater();
            var heading = unityCamera.transform.eulerAngles.y;
            var pitch = NormalizePitch(unityCamera.transform.eulerAngles.x);
            var altitude = defaultView.Camera.AltitudeMeters;
            camera = defaultView.Camera with
            {
                HeadingDeg = heading,
                PitchDeg = pitch,
                AltitudeMeters = altitude,
            };
            return true;
        }

        private static double ReadDoubleProperty(object target, string propertyName)
        {
            var prop = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null)
            {
                return 0;
            }

            var value = prop.GetValue(target);
            return value switch
            {
                double d => d,
                float f => f,
                _ => 0,
            };
        }

        private static double NormalizePitch(float eulerX)
        {
            // Unity camera euler X: map to globe pitch convention (-90 top-down, 0 horizon).
            var pitch = eulerX;
            if (pitch > 180f)
            {
                pitch -= 360f;
            }

            return Math.Clamp(-pitch, -90.0, 0.0);
        }
    }
}
#endif
