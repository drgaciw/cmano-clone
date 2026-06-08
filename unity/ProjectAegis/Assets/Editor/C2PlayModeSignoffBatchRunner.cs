#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using ProjectAegis.Unity.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectAegis.Unity.Editor
{
    /// <summary>
    /// Batchmode Play Mode sign-off for S19-01 check 1 (no console errors on Play start).
    /// Survives Enter Play Mode domain reload via SessionState + InitializeOnLoad.
    /// Entry: -executeMethod ProjectAegis.Unity.Editor.C2PlayModeSignoffBatchRunner.RunBatch
    /// Classify: -executeMethod ProjectAegis.Unity.Editor.C2PlayModeSignoffBatchRunner.RunClassifyBatch
    /// </summary>
    [InitializeOnLoad]
    public static class C2PlayModeSignoffBatchRunner
    {
        private const int TargetSimTicks = 120;
        private const double MaxRealSeconds = 8.0;
        private const double PlayModeStartTimeoutSeconds = 60.0;
        private const double SimTickSeconds = 1.0 / 60.0;

        private const string SessionPending = "C2Signoff.Pending";
        private const string SessionScenario = "C2Signoff.Scenario";
        private const string SessionErrorCount = "C2Signoff.ErrorCount";

        private static readonly List<string> Errors = new();

        private static string _scenarioPolicyId = "baltic-patrol-comms";
        private static bool _armed;
        private static bool _running;
        private static bool _collectingLogs;
        private static bool _finishPending;
        private static double _startRealtime;
        private static double _runRequestedAt;

        static C2PlayModeSignoffBatchRunner()
        {
            Application.logMessageReceived += OnLogMessage;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
            ResumeAfterDomainReloadIfNeeded();
        }

        public static void RunBatch() => BeginRun("baltic-patrol-comms");

        public static void RunClassifyBatch() => BeginRun("baltic-patrol-classify");

        private static void BeginRun(string scenarioPolicyId)
        {
            Errors.Clear();
            SessionState.SetBool(SessionPending, true);
            SessionState.SetString(SessionScenario, scenarioPolicyId);
            SessionState.SetInt(SessionErrorCount, 0);

            _scenarioPolicyId = scenarioPolicyId;
            _armed = true;
            _running = false;
            _collectingLogs = false;
            _finishPending = false;

            try
            {
                EnsureScene(scenarioPolicyId);
                OpenScene();

                _runRequestedAt = EditorApplication.timeSinceStartup;
                Debug.Log($"C2PlayModeSignoffBatchRunner starting Play Mode scenario={scenarioPolicyId}");
                EditorApplication.EnterPlaymode();
            }
            catch (Exception ex)
            {
                RecordError($"C2PlayModeSignoffBatchRunner setup failed: {ex}");
                CleanupAndExit(1);
            }
        }

        private static void ResumeAfterDomainReloadIfNeeded()
        {
            if (!SessionState.GetBool(SessionPending, false))
            {
                return;
            }

            _scenarioPolicyId = SessionState.GetString(SessionScenario, "baltic-patrol-comms");
            _armed = true;

            if (EditorApplication.isPlaying)
            {
                _running = true;
                _collectingLogs = true;
                _startRealtime = EditorApplication.timeSinceStartup;
                Debug.Log("C2PlayModeSignoffBatchRunner resumed after domain reload; collecting console errors");
            }
        }

        private static void EnsureScene(string scenarioPolicyId)
        {
            var scenePath = DelegationSmokeSceneBuilder.ScenePath;
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var absoluteScenePath = Path.Combine(projectRoot, scenePath.Replace('/', Path.DirectorySeparatorChar));
            var exists = File.Exists(absoluteScenePath);

            if (!exists || scenarioPolicyId != "baltic-patrol-comms")
            {
                DelegationSmokeSceneBuilder.Build(scenarioPolicyId, exitBatchModeWhenDone: false);
            }
        }

        private static void OpenScene()
        {
            var scene = EditorSceneManager.OpenScene(DelegationSmokeSceneBuilder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open scene {DelegationSmokeSceneBuilder.ScenePath}");
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(SessionPending, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _running = true;
                _collectingLogs = true;
                _startRealtime = EditorApplication.timeSinceStartup;
                Debug.Log("C2PlayModeSignoffBatchRunner entered Play Mode; collecting console errors");
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                _collectingLogs = false;
            }
            else if (state == PlayModeStateChange.EnteredEditMode && _finishPending)
            {
                CleanupAndExit(Errors.Count > 0 ? 1 : 0);
            }
        }

        private static void OnEditorUpdate()
        {
            if (!SessionState.GetBool(SessionPending, false) || !_armed)
            {
                return;
            }

            if (!_running && !EditorApplication.isPlaying)
            {
                var waitSeconds = EditorApplication.timeSinceStartup - _runRequestedAt;
                if (waitSeconds > PlayModeStartTimeoutSeconds)
                {
                    RecordError($"Play Mode did not start within {PlayModeStartTimeoutSeconds:0}s");
                    _finishPending = true;
                    CleanupAndExit(1);
                }

                return;
            }

            if (!_running || !EditorApplication.isPlaying)
            {
                return;
            }

            var simTicks = EstimateSimTicks();
            var elapsed = EditorApplication.timeSinceStartup - _startRealtime;
            if (simTicks < TargetSimTicks && elapsed < MaxRealSeconds)
            {
                return;
            }

            _running = false;
            _finishPending = true;
            Debug.Log(
                $"C2PlayModeSignoffBatchRunner complete: simTicks={simTicks} elapsed={elapsed:0.00}s scenario={_scenarioPolicyId}");
            EditorApplication.ExitPlaymode();
        }

        private static int EstimateSimTicks()
        {
            var simHost = UnityEngine.Object.FindFirstObjectByType<SimplePlayModeSimHost>();
            if (simHost == null)
            {
                return 0;
            }

            return Mathf.Max(0, Mathf.RoundToInt((float)(simHost.SimTime / SimTickSeconds)));
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (!_collectingLogs)
            {
                return;
            }

            if (IsIgnorableBatchNoise(condition, stackTrace))
            {
                return;
            }

            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            var message = string.IsNullOrWhiteSpace(stackTrace)
                ? $"[{type}] {condition}"
                : $"[{type}] {condition}\n{stackTrace}";
            RecordError(message);
        }

        private static bool IsIgnorableBatchNoise(string condition, string stackTrace)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return true;
            }

            if (condition.Contains("Mesh Deformation Systems disabled", StringComparison.Ordinal)
                || condition.Contains("No SRP present", StringComparison.Ordinal)
                || condition.Contains("McpManagerClientHub", StringComparison.Ordinal)
                || condition.Contains("Connection not available and auto-reconnect disabled", StringComparison.Ordinal)
                || condition.Contains("Start Indexing on Editor startup", StringComparison.Ordinal))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(stackTrace))
            {
                if (stackTrace.Contains("UnityEditor.Search", StringComparison.Ordinal)
                    || stackTrace.Contains("com.IvanMurzak.Unity.MCP", StringComparison.Ordinal)
                    || stackTrace.Contains("UnityEditor.Search.SearchInit", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RecordError(string message)
        {
            if (!Errors.Contains(message))
            {
                Errors.Add(message);
                SessionState.SetInt(SessionErrorCount, Errors.Count);
            }

            Debug.LogError($"SIGNOFF_ERROR: {message}");
        }

        private static void CleanupAndExit(int exitCode)
        {
            _finishPending = false;
            _armed = false;
            _collectingLogs = false;
            _running = false;
            SessionState.EraseBool(SessionPending);
            SessionState.EraseString(SessionScenario);
            SessionState.EraseInt(SessionErrorCount);

            if (exitCode == 0)
            {
                Debug.Log(
                    $"C2PlayModeSignoffBatchRunner PASS: no console errors during Play Mode (scenario={_scenarioPolicyId})");
            }
            else
            {
                Debug.LogError(
                    $"C2PlayModeSignoffBatchRunner FAIL: {Errors.Count} console error(s) during Play Mode (scenario={_scenarioPolicyId})");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
        }
    }
}
#endif