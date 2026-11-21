#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ProjectAegis.Unity.Editor
{
    /// <summary>
    /// Ensures Unity MCP runtime (gated by <c>UNITY_MCP_READY</c>) is not compiled into
    /// player builds. Editor MCP may re-add the define via NuGet resolver for Editor work;
    /// every player build temporarily strips it and restores afterward.
    /// </summary>
    internal sealed class McpPlayerBuildIsolation :
        IPreprocessBuildWithReport,
        IPostprocessBuildWithReport
    {
        public const string ReadyDefine = "UNITY_MCP_READY";

        private static readonly Dictionary<string, bool> HadDefineByTarget = new(StringComparer.Ordinal);

        public int callbackOrder => -1000;

        /// <summary>
        /// Resolves the named target for a build, including the dedicated-server subtarget.
        /// A Standalone group build with the Server subtarget is a separate define scope, so
        /// keying on platform group alone would leave server builds with the define.
        /// </summary>
        internal static IReadOnlyList<NamedBuildTarget> ResolveNamedTargets(BuildSummary summary)
        {
            var primary = NamedBuildTarget.FromBuildTargetGroup(summary.platformGroup);

            // BuildSummary does not expose the dedicated-server subtarget, so any Standalone-group
            // build strips both the Standalone and Server define scopes (NamedBuildTarget.Server).
            return summary.platformGroup == BuildTargetGroup.Standalone
                ? new[] { primary, NamedBuildTarget.Server }
                : new[] { primary };
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            foreach (var named in ResolveNamedTargets(report.summary))
            {
                var key = named.TargetName;
                var defines = PlayerSettings.GetScriptingDefineSymbols(named)
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim())
                    .Where(d => d.Length > 0)
                    .ToList();

                var had = defines.RemoveAll(d =>
                    string.Equals(d, ReadyDefine, StringComparison.Ordinal)) > 0;
                HadDefineByTarget[key] = had;

                if (had)
                {
                    PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", defines));
                    Debug.Log(
                        $"[McpPlayerBuildIsolation] Stripped {ReadyDefine} from {key} for player build " +
                        "(Editor-only MCP isolation).");
                }
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            var targets = ResolveNamedTargets(report.summary);
            var key = targets[0].TargetName;

            // Post-build gate: MCP assemblies must not appear in player output.
            var leaked = report.GetFiles()
                .Where(f => f.path.IndexOf("ivanmurzak", StringComparison.OrdinalIgnoreCase) >= 0
                    || f.path.IndexOf("UnityMcp", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(f => f.path)
                .ToList();
            if (leaked.Count > 0)
            {
                Debug.LogError(
                    $"[McpPlayerBuildIsolation] MCP assemblies found in {key} player output: " +
                    string.Join(", ", leaked));
            }

            foreach (var named in targets)
            {
                var targetKey = named.TargetName;
                if (!HadDefineByTarget.TryGetValue(targetKey, out var had) || !had)
                {
                    HadDefineByTarget.Remove(targetKey);
                    continue;
                }

                HadDefineByTarget.Remove(targetKey);

                var defines = PlayerSettings.GetScriptingDefineSymbols(named)
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim())
                    .Where(d => d.Length > 0)
                    .ToList();

                if (defines.Any(d => string.Equals(d, ReadyDefine, StringComparison.Ordinal)))
                {
                    continue;
                }

                defines.Add(ReadyDefine);
                PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", defines));
                Debug.Log(
                    $"[McpPlayerBuildIsolation] Restored {ReadyDefine} on {targetKey} after player build.");
            }
        }

        /// <summary>
        /// Removes <see cref="ReadyDefine"/> from every NamedBuildTarget currently listed in
        /// ProjectSettings (used by tests/structural gates and optional menu cleanup).
        /// </summary>
        public static int StripReadyDefineFromAllPlayerTargets()
        {
            var stripped = 0;
            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown)
                {
                    continue;
                }

                NamedBuildTarget named;
                try
                {
                    named = NamedBuildTarget.FromBuildTargetGroup(group);
                }
                catch
                {
                    continue;
                }

                var defines = PlayerSettings.GetScriptingDefineSymbols(named)
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim())
                    .Where(d => d.Length > 0)
                    .ToList();

                if (defines.RemoveAll(d =>
                        string.Equals(d, ReadyDefine, StringComparison.Ordinal)) <= 0)
                {
                    continue;
                }

                PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", defines));
                stripped++;
            }

            // Dedicated server is a subtarget, not a BuildTargetGroup, so the loop above never sees it.
            var server = NamedBuildTarget.Server;
            var serverDefines = PlayerSettings.GetScriptingDefineSymbols(server)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim())
                .Where(d => d.Length > 0)
                .ToList();
            if (serverDefines.RemoveAll(d =>
                    string.Equals(d, ReadyDefine, StringComparison.Ordinal)) > 0)
            {
                PlayerSettings.SetScriptingDefineSymbols(server, string.Join(";", serverDefines));
                stripped++;
            }

            return stripped;
        }
    }
}
#endif
