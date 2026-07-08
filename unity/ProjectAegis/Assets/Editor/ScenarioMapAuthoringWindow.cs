#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using UnityEditor;
using UnityEngine;

namespace ProjectAegis.Unity.Editor
{
    /// <summary>
    /// Thin Edit Mode host for Phase 2.1 scenario map authoring (Task 9 / ME-W0).
    /// Binds IMGUI only to headless APIs:
    /// <see cref="ScenarioAuthoringSession"/>, <see cref="ScenarioEditCommandBus"/>,
    /// <see cref="MapAuthoringSurface"/>, <see cref="LiveFindingsPresenter"/>.
    /// No Validation Engine rules, no <c>DelegationBridge</c>, no business logic.
    /// </summary>
    public sealed class ScenarioMapAuthoringWindow : EditorWindow
    {
        private string _scenarioPath = string.Empty;
        private string _status = "No scenario open.";

        private ScenarioAuthoringSession? _session;
        private MapAuthoringSurface? _surface;
        private LiveFindingsPresenter? _findings;

        private Vector2 _unitsScroll;
        private Vector2 _rpsScroll;
        private Vector2 _findingsScroll;

        // Place-unit form (gesture input only — commit goes through MapAuthoringSurface).
        private string _placeUnitId = "u-new";
        private string _placeSideId = "blue";
        private string _placePlatformId = "ffg";
        private string _placeLat = "57.0";
        private string _placeLon = "20.0";

        // Optional 3-vertex polygon RP form.
        private string _rpId = "rp-zone";
        private string _rpLat0 = "57.0";
        private string _rpLon0 = "20.0";
        private string _rpLat1 = "57.1";
        private string _rpLon1 = "20.1";
        private string _rpLat2 = "57.0";
        private string _rpLon2 = "20.2";

        /// <summary>Opens the Scenario Map Authoring window from the menu.</summary>
        [MenuItem("Project Aegis/Scenario Map Authoring")]
        public static void OpenWindow()
        {
            var window = GetWindow<ScenarioMapAuthoringWindow>();
            window.titleContent = new GUIContent("Scenario Map Authoring");
            window.minSize = new Vector2(420f, 520f);
            window.Show();
        }

        private void OnDisable()
        {
            DisposeSession();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Scenario Map Authoring (thin host)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Binds UI to ScenarioAuthoringSession / Bus / MapAuthoringSurface / LiveFindingsPresenter only. " +
                "No Validation Engine rules and no DelegationBridge.",
                MessageType.Info);

            DrawPathAndSessionBar();
            EditorGUILayout.Space(6f);

            using (new EditorGUI.DisabledScope(_session == null))
            {
                DrawSessionStatus();
                EditorGUILayout.Space(4f);
                DrawLists();
                EditorGUILayout.Space(6f);
                DrawPlaceUnitSection();
                EditorGUILayout.Space(6f);
                DrawReferencePointSection();
            }

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox(_status, MessageType.None);
            }
        }

        private void DrawPathAndSessionBar()
        {
            EditorGUILayout.BeginHorizontal();
            _scenarioPath = EditorGUILayout.TextField("Scenario path", _scenarioPath);
            if (GUILayout.Button("Browse…", GUILayout.Width(72f)))
            {
                var picked = EditorUtility.OpenFilePanel(
                    "Open scenario.json",
                    string.IsNullOrEmpty(_scenarioPath)
                        ? Application.dataPath
                        : Path.GetDirectoryName(_scenarioPath) ?? Application.dataPath,
                    "json");
                if (!string.IsNullOrEmpty(picked))
                {
                    _scenarioPath = picked;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open"))
            {
                TryOpen();
            }

            using (new EditorGUI.DisabledScope(_session == null))
            {
                if (GUILayout.Button("Save"))
                {
                    TrySave();
                }

                if (GUILayout.Button("Rebuild"))
                {
                    TryRebuild();
                }

                if (GUILayout.Button("Refresh Findings"))
                {
                    TryRefreshFindings();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSessionStatus()
        {
            if (_session == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Path", _session.Path);
            EditorGUILayout.LabelField("Edit version", _session.EditVersion.ToString());
            EditorGUILayout.LabelField("Dirty", _session.IsDirty ? "yes" : "no");
        }

        private void DrawLists()
        {
            EditorGUILayout.LabelField("ORBAT unit ids", EditorStyles.boldLabel);
            _unitsScroll = EditorGUILayout.BeginScrollView(_unitsScroll, GUILayout.Height(90f));
            if (_surface == null || _surface.Units.Count == 0)
            {
                EditorGUILayout.LabelField("(none)");
            }
            else
            {
                for (var i = 0; i < _surface.Units.Count; i++)
                {
                    var u = _surface.Units[i];
                    EditorGUILayout.LabelField(
                        $"{u.UnitId}  side={u.SideId}  platform={u.PlatformId}  ({u.Lat:F4},{u.Lon:F4})");
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField("Reference point ids", EditorStyles.boldLabel);
            _rpsScroll = EditorGUILayout.BeginScrollView(_rpsScroll, GUILayout.Height(90f));
            if (_surface == null || _surface.ReferencePoints.Count == 0)
            {
                EditorGUILayout.LabelField("(none)");
            }
            else
            {
                for (var i = 0; i < _surface.ReferencePoints.Count; i++)
                {
                    var rp = _surface.ReferencePoints[i];
                    var valid = rp.IsGeometryValid ? "valid" : $"invalid:{rp.InvalidReason}";
                    EditorGUILayout.LabelField($"{rp.Id}  type={rp.Type}  {valid}");
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField("Findings codes", EditorStyles.boldLabel);
            _findingsScroll = EditorGUILayout.BeginScrollView(_findingsScroll, GUILayout.Height(90f));
            if (_findings == null || _findings.LastCodes.Count == 0)
            {
                EditorGUILayout.LabelField("(none — use Refresh Findings)");
            }
            else
            {
                for (var i = 0; i < _findings.LastCodes.Count; i++)
                {
                    EditorGUILayout.LabelField(_findings.LastCodes[i]);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPlaceUnitSection()
        {
            EditorGUILayout.LabelField("Place unit", EditorStyles.boldLabel);
            _placeUnitId = EditorGUILayout.TextField("Id", _placeUnitId);
            _placeSideId = EditorGUILayout.TextField("Side", _placeSideId);
            _placePlatformId = EditorGUILayout.TextField("Platform", _placePlatformId);
            _placeLat = EditorGUILayout.TextField("Lat", _placeLat);
            _placeLon = EditorGUILayout.TextField("Lon", _placeLon);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Begin Place Unit"))
            {
                TryBeginPlaceUnit();
            }

            if (GUILayout.Button("Commit Place Unit"))
            {
                TryCommitPlaceUnit();
            }

            if (GUILayout.Button("Cancel Gesture"))
            {
                TryCancelGesture();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawReferencePointSection()
        {
            EditorGUILayout.LabelField("Upsert RP polygon (3 vertices)", EditorStyles.boldLabel);
            _rpId = EditorGUILayout.TextField("RP id", _rpId);
            DrawLatLonRow("V0", ref _rpLat0, ref _rpLon0);
            DrawLatLonRow("V1", ref _rpLat1, ref _rpLon1);
            DrawLatLonRow("V2", ref _rpLat2, ref _rpLon2);

            if (GUILayout.Button("Begin + Commit RP Polygon"))
            {
                TryUpsertRpPolygon();
            }
        }

        private static void DrawLatLonRow(string label, ref string lat, ref string lon)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            lat = EditorGUILayout.TextField(lat);
            lon = EditorGUILayout.TextField(lon);
            EditorGUILayout.EndHorizontal();
        }

        private void TryOpen()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_scenarioPath))
                {
                    _status = "Enter a scenario.json path.";
                    return;
                }

                if (!File.Exists(_scenarioPath))
                {
                    _status = $"File not found: {_scenarioPath}";
                    return;
                }

                DisposeSession();
                _session = ScenarioAuthoringSession.Open(_scenarioPath);
                _surface = new MapAuthoringSurface(_session);
                _findings = new LiveFindingsPresenter(_session, debounceMs: 0);
                _surface.RebuildFromDocument();
                _findings.RefreshImmediate();
                _status = $"Opened {_session.Path} (editVersion={_session.EditVersion}).";
                Repaint();
            }
            catch (Exception ex)
            {
                DisposeSession();
                _status = $"Open failed: {ex.Message}";
            }
        }

        private void TrySave()
        {
            if (_session == null)
            {
                return;
            }

            try
            {
                _session.Save();
                _status = $"Saved {_session.Path} (editVersion={_session.EditVersion}).";
                Repaint();
            }
            catch (Exception ex)
            {
                _status = $"Save failed: {ex.Message}";
            }
        }

        private void TryRebuild()
        {
            if (_surface == null)
            {
                return;
            }

            try
            {
                _surface.RebuildFromDocument();
                _status = $"Rebuilt surface: {_surface.Units.Count} units, {_surface.ReferencePoints.Count} RPs.";
                Repaint();
            }
            catch (Exception ex)
            {
                _status = $"Rebuild failed: {ex.Message}";
            }
        }

        private void TryRefreshFindings()
        {
            if (_findings == null)
            {
                return;
            }

            try
            {
                _findings.RefreshImmediate();
                _status = $"Findings: {_findings.LastCodes.Count} code(s).";
                Repaint();
            }
            catch (Exception ex)
            {
                _status = $"Refresh findings failed: {ex.Message}";
            }
        }

        private void TryBeginPlaceUnit()
        {
            if (_surface == null)
            {
                return;
            }

            if (!TryParseLatLon(_placeLat, _placeLon, out var lat, out var lon, out var err))
            {
                _status = err;
                return;
            }

            try
            {
                _surface.BeginPlaceUnit(new ScenarioOrbatUnitDto
                {
                    Id = _placeUnitId.Trim(),
                    SideId = _placeSideId.Trim(),
                    PlatformId = _placePlatformId.Trim(),
                    Lat = lat,
                    Lon = lon,
                });
                _status = $"Place-unit gesture staged for '{_placeUnitId.Trim()}'. Commit to apply via bus.";
                Repaint();
            }
            catch (Exception ex)
            {
                _status = $"Begin place unit failed: {ex.Message}";
            }
        }

        private void TryCommitPlaceUnit()
        {
            if (_surface == null || _findings == null)
            {
                return;
            }

            try
            {
                var result = _surface.CommitPlaceUnit(save: true);
                if (result == null)
                {
                    _status = "No tentative unit staged. Use Begin Place Unit first.";
                    return;
                }

                _findings.RefreshImmediate();
                if (result.Ok)
                {
                    _status = $"Place unit committed (editVersion={result.EditVersion}).";
                }
                else
                {
                    _status = $"Place unit failed: {result.ErrorCode} — {result.ErrorMessage}";
                }

                Repaint();
            }
            catch (Exception ex)
            {
                _status = $"Commit place unit failed: {ex.Message}";
            }
        }

        private void TryCancelGesture()
        {
            if (_surface == null)
            {
                return;
            }

            _surface.CancelGesture();
            _status = "Gesture cancelled.";
            Repaint();
        }

        private void TryUpsertRpPolygon()
        {
            if (_surface == null || _findings == null)
            {
                return;
            }

            if (!TryParseLatLon(_rpLat0, _rpLon0, out var lat0, out var lon0, out var err0))
            {
                _status = "V0: " + err0;
                return;
            }

            if (!TryParseLatLon(_rpLat1, _rpLon1, out var lat1, out var lon1, out var err1))
            {
                _status = "V1: " + err1;
                return;
            }

            if (!TryParseLatLon(_rpLat2, _rpLon2, out var lat2, out var lon2, out var err2))
            {
                _status = "V2: " + err2;
                return;
            }

            try
            {
                var geometry = new List<ScenarioWaypointDto>
                {
                    new() { Lat = lat0, Lon = lon0 },
                    new() { Lat = lat1, Lon = lon1 },
                    new() { Lat = lat2, Lon = lon2 },
                };

                _surface.BeginDrawReferencePoint(new ScenarioReferencePointDto
                {
                    Id = _rpId.Trim(),
                    Type = "polygon",
                    Geometry = geometry,
                });

                var result = _surface.CommitReferencePoint(save: true);
                if (result == null)
                {
                    _status = "RP commit returned null (no tentative RP).";
                    return;
                }

                _findings.RefreshImmediate();
                if (result.Ok)
                {
                    _status = $"RP polygon '{_rpId.Trim()}' committed (editVersion={result.EditVersion}).";
                }
                else
                {
                    _status = $"RP commit failed: {result.ErrorCode} — {result.ErrorMessage}";
                }

                Repaint();
            }
            catch (Exception ex)
            {
                _status = $"RP upsert failed: {ex.Message}";
            }
        }

        private static bool TryParseLatLon(
            string latText,
            string lonText,
            out double lat,
            out double lon,
            out string error)
        {
            lat = 0;
            lon = 0;
            error = string.Empty;

            if (!double.TryParse(latText, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out lat))
            {
                error = $"Invalid lat '{latText}'.";
                return false;
            }

            if (!double.TryParse(lonText, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out lon))
            {
                error = $"Invalid lon '{lonText}'.";
                return false;
            }

            return true;
        }

        private void DisposeSession()
        {
            _findings = null;
            _surface = null;
            if (_session != null)
            {
                _session.Dispose();
                _session = null;
            }
        }
    }
}
#endif
