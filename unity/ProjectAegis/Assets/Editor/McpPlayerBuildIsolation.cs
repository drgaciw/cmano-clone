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

        public void OnPreprocessBuild(BuildReport report)
        {
            var named = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
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

        public void OnPostprocessBuild(BuildReport report)
        {
            var named = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
            var key = named.TargetName;
            if (!HadDefineByTarget.TryGetValue(key, out var had) || !had)
            {
                HadDefineByTarget.Remove(key);
                return;
            }

            HadDefineByTarget.Remove(key);

            var defines = PlayerSettings.GetScriptingDefineSymbols(named)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim())
                .Where(d => d.Length > 0)
                .ToList();

            if (defines.Any(d => string.Equals(d, ReadyDefine, StringComparison.Ordinal)))
            {
                return;
            }

            defines.Add(ReadyDefine);
            PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", defines));
            Debug.Log(
                $"[McpPlayerBuildIsolation] Restored {ReadyDefine} on {key} after player build.");
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

            return stripped;
        }
    }
}
#endif
