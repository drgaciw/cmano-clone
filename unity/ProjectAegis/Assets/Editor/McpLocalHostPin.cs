#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ProjectAegis.Unity.Editor
{
    /// <summary>
    /// Explicit (menu-only) pin of Unity-MCP to local Custom mode on
    /// <c>http://localhost:8080</c>. Does <b>not</b> run on Editor load —
    /// prior auto-mutation of MCP UserSettings was rejected as adversarial.
    /// </summary>
    /// <remarks>
    /// Unity-MCP ≥0.86 defaults to Cloud mode and a path-hashed port in
    /// 20000–29999. Project Aegis client configs (<c>.cursor/mcp.json</c>,
    /// <c>.mcp.json</c>) stay on <c>:8080</c>; run this menu (or
    /// <c>tools/pin-unity-mcp-8080</c>) once per clone so the plugin matches.
    /// </remarks>
    internal static class McpLocalHostPin
    {
        public const string ConfigFileName = "AI-Game-Developer-Config.json";
        public const string PinnedHost = "http://localhost:8080";
        public const string ConnectionModeCustom = "Custom";

        private static readonly Regex ConnectionModeRegex = new(
            "\"connectionMode\"\\s*:\\s*\"[^\"]*\"",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex HostRegex = new(
            "\"host\"\\s*:\\s*\"[^\"]*\"",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex KeepServerRunningRegex = new(
            "\"keepServerRunning\"\\s*:\\s*(true|false)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex KeepConnectedRegex = new(
            "\"keepConnected\"\\s*:\\s*(true|false)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex AuthOptionRegex = new(
            "\"authOption\"\\s*:\\s*\"[^\"]*\"",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// Writes or patches only the pin fields in
        /// <c>UserSettings/AI-Game-Developer-Config.json</c>.
        /// </summary>
        /// <returns>Absolute path written.</returns>
        public static string ApplyPin(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new ArgumentException("projectRoot is required.", nameof(projectRoot));
            }

            var userSettings = Path.Combine(projectRoot, "UserSettings");
            Directory.CreateDirectory(userSettings);
            var path = Path.Combine(userSettings, ConfigFileName);

            string json;
            if (File.Exists(path))
            {
                json = File.ReadAllText(path);
                json = UpsertString(json, ConnectionModeRegex, "connectionMode", ConnectionModeCustom);
                json = UpsertString(json, HostRegex, "host", PinnedHost);
                json = UpsertBool(json, KeepServerRunningRegex, "keepServerRunning", true);
                json = UpsertBool(json, KeepConnectedRegex, "keepConnected", true);
                json = UpsertString(json, AuthOptionRegex, "authOption", "none");
            }
            else
            {
                json =
                    "{\n" +
                    $"  \"connectionMode\": \"{ConnectionModeCustom}\",\n" +
                    $"  \"host\": \"{PinnedHost}\",\n" +
                    "  \"keepServerRunning\": true,\n" +
                    "  \"keepConnected\": true,\n" +
                    "  \"authOption\": \"none\",\n" +
                    "  \"transportMethod\": \"streamableHttp\",\n" +
                    "  \"logLevel\": \"Warning\",\n" +
                    "  \"timeoutMs\": 10000\n" +
                    "}\n";
            }

            File.WriteAllText(path, json);
            return path;
        }

        [MenuItem("Project Aegis/MCP/Pin Local Host :8080")]
        private static void PinFromMenu()
        {
            try
            {
                var path = ApplyPin(Directory.GetParent(Application.dataPath)!.FullName);
                Debug.Log(
                    $"[McpLocalHostPin] Pinned Unity-MCP to Custom + {PinnedHost} at {path}. " +
                    "Restart Cursor/Claude MCP if already connected, then verify with curl :8080 / ping.");
                EditorUtility.DisplayDialog(
                    "Unity-MCP pin",
                    $"Pinned to Custom mode at {PinnedHost}.\n\nWrote:\n{path}",
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpLocalHostPin] Failed: {ex.Message}");
                EditorUtility.DisplayDialog("Unity-MCP pin failed", ex.Message, "OK");
            }
        }

        private static string UpsertString(string json, Regex regex, string key, string value)
        {
            var replacement = $"\"{key}\": \"{value}\"";
            if (regex.IsMatch(json))
            {
                return regex.Replace(json, replacement, 1);
            }

            return InsertBeforeClosingBrace(json, replacement);
        }

        private static string UpsertBool(string json, Regex regex, string key, bool value)
        {
            var literal = value ? "true" : "false";
            var replacement = $"\"{key}\": {literal}";
            if (regex.IsMatch(json))
            {
                return regex.Replace(json, replacement, 1);
            }

            return InsertBeforeClosingBrace(json, replacement);
        }

        private static string InsertBeforeClosingBrace(string json, string property)
        {
            var idx = json.LastIndexOf('}');
            if (idx < 0)
            {
                throw new InvalidOperationException("AI-Game-Developer-Config.json is not valid JSON object.");
            }

            var before = json.Substring(0, idx).TrimEnd();
            if (!before.EndsWith("{", StringComparison.Ordinal) && !before.EndsWith(",", StringComparison.Ordinal))
            {
                before += ",";
            }

            return before + "\n  " + property + "\n" + json.Substring(idx);
        }
    }
}
#endif
