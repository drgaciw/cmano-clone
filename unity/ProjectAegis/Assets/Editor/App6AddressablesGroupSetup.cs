#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectAegis.Delegation.Projection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace ProjectAegis.Unity.Editor
{
    /// <summary>
    /// Promotes <c>App6AtlasAddressablesManifest.json</c> to a live Addressables group (S27-07).
    /// </summary>
    public static class App6AddressablesGroupSetup
    {
        private const string ManifestAssetPath =
            "Assets/Addressables/Map/App6AtlasAddressablesManifest.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        [MenuItem("Project Aegis/Addressables/Ensure App6 MapPresentation Group")]
        public static void EnsureFromMenu() => EnsureMapPresentationGroup(logSuccess: true);

        /// <summary>Unity batchmode entry for CI/editor automation.</summary>
        public static void EnsureBatch() => EnsureMapPresentationGroup(logSuccess: true);

        public static bool EnsureMapPresentationGroup(bool logSuccess = false)
        {
            if (!TryReadManifest(out var manifest, out var entry))
            {
                Debug.LogError($"APP-6 Addressables setup failed: manifest missing or invalid at {ManifestAssetPath}");
                return false;
            }

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("APP-6 Addressables setup failed: could not create AddressableAssetSettings.");
                return false;
            }

            var groupName = string.IsNullOrWhiteSpace(manifest.Group)
                ? App6AtlasSpriteSheet.AddressableGroup
                : manifest.Group;
            var group = settings.FindGroup(groupName)
                ?? settings.CreateGroup(
                    groupName,
                    setAsDefaultGroup: false,
                    readOnly: false,
                    postEvent: true,
                    schemasToCopy: null,
                    typeof(BundledAssetGroupSchema),
                    typeof(ContentUpdateGroupSchema));

            var assetGuid = AssetDatabase.AssetPathToGUID(entry.AssetPath);
            if (string.IsNullOrEmpty(assetGuid))
            {
                Debug.LogError($"APP-6 Addressables setup failed: asset not found at {entry.AssetPath}");
                return false;
            }

            var addressableEntry = settings.CreateOrMoveEntry(assetGuid, group, readOnly: false, postEvent: false);
            addressableEntry.address = entry.Address;
            addressableEntry.SetLabel("default", false, false);
            if (entry.Labels != null)
            {
                foreach (var label in entry.Labels)
                {
                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        addressableEntry.SetLabel(label, true, false);
                    }
                }
            }

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(group);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (logSuccess)
            {
                Debug.Log(
                    $"APP-6 Addressables group '{groupName}' wired: {entry.Address} -> {entry.AssetPath}");
            }

            return true;
        }

        private static bool TryReadManifest(out ManifestDocument manifest, out ManifestEntry entry)
        {
            manifest = new ManifestDocument();
            entry = new ManifestEntry();

            var manifestFullPath = Path.Combine(
                Application.dataPath,
                "Addressables",
                "Map",
                "App6AtlasAddressablesManifest.json");
            if (!File.Exists(manifestFullPath))
            {
                return false;
            }

            try
            {
                var json = File.ReadAllText(manifestFullPath);
                manifest = JsonSerializer.Deserialize<ManifestDocument>(json, JsonOptions) ?? manifest;
            }
            catch (JsonException)
            {
                return false;
            }

            if (manifest.Entries == null)
            {
                return false;
            }

            foreach (var candidate in manifest.Entries)
            {
                if (string.Equals(candidate.Address, App6AtlasSpriteSheet.AddressableKey, StringComparison.Ordinal))
                {
                    entry = candidate;
                    return !string.IsNullOrWhiteSpace(entry.AssetPath);
                }
            }

            return false;
        }

        private sealed class ManifestDocument
        {
            [JsonPropertyName("group")]
            public string Group { get; set; } = string.Empty;

            [JsonPropertyName("entries")]
            public ManifestEntry[] Entries { get; set; } = Array.Empty<ManifestEntry>();
        }

        private sealed class ManifestEntry
        {
            [JsonPropertyName("address")]
            public string Address { get; set; } = string.Empty;

            [JsonPropertyName("assetPath")]
            public string AssetPath { get; set; } = string.Empty;

            [JsonPropertyName("labels")]
            public string[]? Labels { get; set; }
        }
    }
}
#endif