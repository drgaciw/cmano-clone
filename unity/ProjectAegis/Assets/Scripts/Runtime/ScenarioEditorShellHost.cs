// SE-UX: Unified Scenario Editor product chrome —
// top bar · Map | Mission Board (+ Events P2.3) · drawer/inspector · findings dock · Play/Sample gate.
// Hosts remain thin binders over ScenarioEditorShellProjection; mutations stay on ScenarioEditCommandBus.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ScenarioEditorShellHost : MonoBehaviour
    {
        private const string RootName = "scenario-editor-shell-root";
        private const string ModeTitleName = "scenario-editor-shell-mode-title";
        private const string StatusName = "scenario-editor-shell-status";
        private const string FindingsSummaryName = "scenario-editor-shell-findings-summary";
        private const string PlayGateName = "scenario-editor-shell-play-gate";
        private const string TabMapName = "scenario-editor-shell-tab-map";
        private const string TabMissionsName = "scenario-editor-shell-tab-missions";
        private const string TabEventsName = "scenario-editor-shell-tab-events";
        private const string TabsName = "scenario-editor-shell-tabs";
        private const string MapSlotName = "scenario-editor-shell-map-slot";
        private const string MissionBoardSlotName = "scenario-editor-shell-mission-board-slot";
        private const string SlotHiddenClass = "scenario-editor-shell-slot--hidden";
        private const string MapRootClass = "scenario-editor-shell--map";
        private const string MissionBoardRootClass = "scenario-editor-shell--mission-board";
        private const string ScenarioTitleName = "scenario-editor-shell-scenario-title";
        private const string DirtyName = "scenario-editor-shell-dirty";
        private const string EditVersionName = "scenario-editor-shell-edit-version";
        private const string InspectorBodyName = "scenario-editor-shell-inspector-body";
        private const string FindingsListName = "scenario-editor-shell-findings-list";
        private const string FilterAllName = "scenario-editor-shell-filter-all";
        private const string FilterErrorsName = "scenario-editor-shell-filter-errors";
        private const string FilterWarningsName = "scenario-editor-shell-filter-warnings";
        private const string BtnLoadName = "scenario-editor-shell-btn-load";
        private const string BtnSaveName = "scenario-editor-shell-btn-save";
        private const string BtnUndoName = "scenario-editor-shell-btn-undo";
        private const string BtnRedoName = "scenario-editor-shell-btn-redo";
        private const string BtnPlayName = "scenario-editor-shell-btn-play";
        private const string BtnSampleName = "scenario-editor-shell-btn-sample";
        private const string DrawerTitleName = "scenario-editor-shell-drawer-title";

        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private C2AccessibilityScalePercent accessibilityScale = C2AccessibilityScalePercent.OneHundred;
        [SerializeField] private bool reducedMotion = true;

        private UIDocument _document = null!;
        private Label? _modeTitle;
        private Label? _status;
        private Label? _findingsSummary;
        private Label? _playGate;
        private Label? _scenarioTitle;
        private Label? _dirty;
        private Label? _editVersion;
        private Label? _inspectorBody;
        private Label? _drawerTitle;
        private Button? _tabMap;
        private Button? _tabMissions;
        private Button? _tabEvents;
        private Button? _filterAll;
        private Button? _filterErrors;
        private Button? _filterWarnings;
        private Button? _btnLoad;
        private Button? _btnSave;
        private Button? _btnUndo;
        private Button? _btnRedo;
        private Button? _btnPlay;
        private Button? _btnSample;
        private VisualElement? _mapSlot;
        private VisualElement? _missionBoardSlot;
        private VisualElement? _tabs;
        private ScrollView? _findingsList;
        private ScenarioEditorShellState _state = ScenarioEditorShellProjection.Bind();
        private bool _wired;

        /// <summary>Current shell projection state (test / curator sync).</summary>
        public ScenarioEditorShellState State => _state;

        /// <summary>Raised when Load is clicked (curator / session owns IO).</summary>
        public event Action? LoadRequested;

        /// <summary>Raised when Save is clicked (allowed with errors).</summary>
        public event Action? SaveRequested;

        public event Action? UndoRequested;
        public event Action? RedoRequested;

        /// <summary>Raised when Play is clicked and projection allows it.</summary>
        public event Action? PlayRequested;

        /// <summary>Raised when Sample is clicked and projection allows it.</summary>
        public event Action? SampleRequested;

        /// <summary>Raised when a findings row is activated (entity jump already applied to state).</summary>
        public event Action<ScenarioEditorFindingRow>? FindingActivated;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (panelAsset != null)
            {
                _document.visualTreeAsset = panelAsset;
            }

            if (panelStyles != null && _document.rootVisualElement != null)
            {
                _document.rootVisualElement.styleSheets.Add(panelStyles);
            }
        }

        private void OnEnable()
        {
            TryWireElements();
            ApplyState(_state);
            ApplyAccessibility();
            ApplyPanelVisibility();
        }

        /// <summary>Switch Map | Mission Board without destroying child slot content.</summary>
        public void SetMode(ScenarioEditorShellMode mode)
        {
            _state = ScenarioEditorShellProjection.WithMode(_state, mode);
            ApplyState(_state);
        }

        public void SetSelectedEntityId(string? entityId)
        {
            _state = ScenarioEditorShellProjection.WithSelection(_state, entityId);
            ApplyState(_state);
        }

        public void SetStatusSummary(string statusSummary)
        {
            _state = ScenarioEditorShellProjection.WithStatus(_state, statusSummary);
            ApplyState(_state);
        }

        /// <summary>Refresh live findings counts and Play/Sample gate (error-severity only).</summary>
        public void SetFindings(int errorCount, int warningCount)
        {
            _state = ScenarioEditorShellProjection.WithFindings(_state, errorCount, warningCount);
            ApplyState(_state);
        }

        /// <summary>Bind top-bar temporal/file chrome (title, dirty, editVersion, session, undo stack).</summary>
        public void SetTopBar(
            string scenarioTitle,
            bool isDirty,
            int editVersion,
            bool sessionOpen,
            bool undoEnabled = false,
            bool redoEnabled = false)
        {
            _state = ScenarioEditorShellProjection.WithTopBar(
                _state,
                scenarioTitle,
                isDirty,
                editVersion,
                sessionOpen,
                undoEnabled,
                redoEnabled);
            ApplyState(_state);
        }

        /// <summary>Replace findings dock rows and optional filter; updates counts + Play gate.</summary>
        public void SetFindingRows(
            IReadOnlyList<ScenarioEditorFindingRow> rows,
            ScenarioEditorFindingsFilter? filter = null)
        {
            _state = ScenarioEditorShellProjection.WithFindingRows(_state, rows, filter);
            ApplyState(_state);
        }

        public void SetFindingsFilter(ScenarioEditorFindingsFilter filter)
        {
            _state = ScenarioEditorShellProjection.WithFindingsFilter(_state, filter);
            ApplyState(_state);
        }

        /// <summary>Jump selection (and mode when mission-preferring) from a findings row.</summary>
        public void ActivateFinding(ScenarioEditorFindingRow row)
        {
            _state = ScenarioEditorShellProjection.JumpToFinding(_state, row);
            ApplyState(_state);
            FindingActivated?.Invoke(row);
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            _modeTitle ??= root.Q<Label>(ModeTitleName);
            _status ??= root.Q<Label>(StatusName);
            _findingsSummary ??= root.Q<Label>(FindingsSummaryName);
            _playGate ??= root.Q<Label>(PlayGateName);
            _scenarioTitle ??= root.Q<Label>(ScenarioTitleName);
            _dirty ??= root.Q<Label>(DirtyName);
            _editVersion ??= root.Q<Label>(EditVersionName);
            _inspectorBody ??= root.Q<Label>(InspectorBodyName);
            _drawerTitle ??= root.Q<Label>(DrawerTitleName);
            _mapSlot ??= root.Q(MapSlotName);
            _missionBoardSlot ??= root.Q(MissionBoardSlotName);
            _findingsList ??= root.Q<ScrollView>(FindingsListName);

            if (_tabMap == null)
            {
                _tabMap = root.Q<Button>(TabMapName);
                if (_tabMap != null)
                {
                    _tabMap.clicked += () => SetMode(ScenarioEditorShellMode.Map);
                }
            }

            if (_tabMissions == null)
            {
                _tabMissions = root.Q<Button>(TabMissionsName);
                if (_tabMissions != null)
                {
                    _tabMissions.clicked += () => SetMode(ScenarioEditorShellMode.MissionBoard);
                }
            }

            _tabEvents ??= root.Q<Button>(TabEventsName);
            if (_tabEvents != null)
            {
                _tabEvents.SetEnabled(false);
            }

            WireFilter(_filterAll, FilterAllName, ScenarioEditorFindingsFilter.All, b => _filterAll = b);
            WireFilter(_filterErrors, FilterErrorsName, ScenarioEditorFindingsFilter.Errors, b => _filterErrors = b);
            WireFilter(_filterWarnings, FilterWarningsName, ScenarioEditorFindingsFilter.Warnings, b => _filterWarnings = b);

            WireAction(ref _btnLoad, BtnLoadName, () => LoadRequested?.Invoke());
            WireAction(ref _btnSave, BtnSaveName, () =>
            {
                if (_state.SaveEnabled)
                {
                    SaveRequested?.Invoke();
                }
            });
            WireAction(ref _btnUndo, BtnUndoName, () =>
            {
                if (_state.UndoEnabled)
                {
                    UndoRequested?.Invoke();
                }
            });
            WireAction(ref _btnRedo, BtnRedoName, () =>
            {
                if (_state.RedoEnabled)
                {
                    RedoRequested?.Invoke();
                }
            });
            WireAction(ref _btnPlay, BtnPlayName, () =>
            {
                if (_state.PlayEnabled)
                {
                    PlayRequested?.Invoke();
                }
            });
            WireAction(ref _btnSample, BtnSampleName, () =>
            {
                if (_state.SampleEnabled)
                {
                    SampleRequested?.Invoke();
                }
            });

            if (_tabs == null)
            {
                _tabs = root.Q(TabsName);
                if (_tabs != null)
                {
                    _tabs.RegisterCallback<KeyDownEvent>(OnTabsKeyDown);
                }
            }

            _wired = root.Q(RootName) != null;
        }

        private void WireFilter(
            Button? existing,
            string name,
            ScenarioEditorFindingsFilter filter,
            Action<Button?> assign)
        {
            if (existing != null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            var button = root?.Q<Button>(name);
            if (button != null)
            {
                button.clicked += () => SetFindingsFilter(filter);
            }

            assign(button);
        }

        private void WireAction(ref Button? field, string name, Action onClick)
        {
            if (field != null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            field = root?.Q<Button>(name);
            if (field != null)
            {
                field.clicked += onClick;
            }
        }

        private void OnTabsKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is KeyCode.RightArrow or KeyCode.LeftArrow
                || (evt.keyCode == KeyCode.Tab && evt.ctrlKey))
            {
                _state = ScenarioEditorShellProjection.CycleMode(_state);
                ApplyState(_state);
                evt.StopPropagation();
            }
        }

        private void ApplyState(ScenarioEditorShellState state)
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            root.EnableInClassList(MapRootClass, state.Mode == ScenarioEditorShellMode.Map);
            root.EnableInClassList(MissionBoardRootClass, state.Mode == ScenarioEditorShellMode.MissionBoard);

            if (_modeTitle != null)
            {
                _modeTitle.text = state.ModeTitle;
            }

            if (_status != null)
            {
                _status.text = state.StatusSummary;
            }

            if (_scenarioTitle != null)
            {
                _scenarioTitle.text = state.ScenarioTitle;
            }

            if (_dirty != null)
            {
                _dirty.text = state.DirtyLabel;
                _dirty.EnableInClassList("scenario-editor-shell-dirty--unsaved", state.IsDirty);
            }

            if (_editVersion != null)
            {
                _editVersion.text = $"v{state.EditVersion}";
            }

            if (_findingsSummary != null)
            {
                _findingsSummary.text = state.FindingsSummaryText;
            }

            if (_playGate != null)
            {
                _playGate.text = state.PlayBlockReason;
                _playGate.style.display = state.PlayBlocked ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_inspectorBody != null)
            {
                _inspectorBody.text = state.SelectionInspectorText;
            }

            if (_drawerTitle != null)
            {
                _drawerTitle.text = state.Mode == ScenarioEditorShellMode.Map
                    ? "ORBAT / Zones"
                    : "Mission Filters";
            }

            _tabMap?.EnableInClassList("scenario-editor-shell-tab--active", state.MapTabActive);
            _tabMissions?.EnableInClassList("scenario-editor-shell-tab--active", state.MissionBoardTabActive);
            _tabEvents?.EnableInClassList("scenario-editor-shell-tab--disabled", !state.EventsTabEnabled);

            ApplyActionButton(_btnLoad, state.LoadEnabled);
            ApplyActionButton(_btnSave, state.SaveEnabled);
            ApplyActionButton(_btnUndo, state.UndoEnabled);
            ApplyActionButton(_btnRedo, state.RedoEnabled);
            ApplyActionButton(_btnPlay, state.PlayEnabled);
            ApplyActionButton(_btnSample, state.SampleEnabled);

            ApplyFilterChip(_filterAll, ScenarioEditorFindingsFilter.All, state.FindingsFilter);
            ApplyFilterChip(_filterErrors, ScenarioEditorFindingsFilter.Errors, state.FindingsFilter);
            ApplyFilterChip(_filterWarnings, ScenarioEditorFindingsFilter.Warnings, state.FindingsFilter);

            _mapSlot?.EnableInClassList(SlotHiddenClass, !state.MapContentVisible);
            _missionBoardSlot?.EnableInClassList(SlotHiddenClass, !state.MissionBoardContentVisible);

            RebuildFindingsList(state);
        }

        private static void ApplyActionButton(Button? button, bool enabled)
        {
            if (button == null)
            {
                return;
            }

            button.SetEnabled(enabled);
            button.EnableInClassList("scenario-editor-shell-action--disabled", !enabled);
        }

        private static void ApplyFilterChip(
            Button? button,
            ScenarioEditorFindingsFilter chip,
            ScenarioEditorFindingsFilter active)
        {
            if (button == null)
            {
                return;
            }

            button.EnableInClassList(
                "scenario-editor-shell-findings-filter--active",
                chip == active);
        }

        private void RebuildFindingsList(ScenarioEditorShellState state)
        {
            if (_findingsList == null)
            {
                return;
            }

            _findingsList.Clear();
            foreach (var row in state.VisibleFindingRows)
            {
                var rowEl = new VisualElement();
                rowEl.AddToClassList("scenario-editor-shell-finding-row");
                rowEl.AddToClassList(SeverityRowModifier(row.Severity));
                rowEl.focusable = true;

                var tag = new Label($"[{row.SeverityTag}]");
                tag.AddToClassList("scenario-editor-shell-finding-tag");
                tag.AddToClassList(SeverityTagModifier(row.Severity));

                var label = new Label($"{row.Code} — {row.Message}");
                label.AddToClassList("scenario-editor-shell-finding-label");

                rowEl.Add(tag);
                rowEl.Add(label);

                var captured = row;
                rowEl.RegisterCallback<ClickEvent>(_ => ActivateFinding(captured));
                rowEl.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Space)
                    {
                        ActivateFinding(captured);
                        evt.StopPropagation();
                    }
                });

                _findingsList.Add(rowEl);
            }
        }

        private static string SeverityRowModifier(ScenarioEditorFindingSeverity severity) =>
            severity switch
            {
                ScenarioEditorFindingSeverity.Error => "scenario-editor-shell-finding-row--error",
                ScenarioEditorFindingSeverity.Warning => "scenario-editor-shell-finding-row--warning",
                _ => "scenario-editor-shell-finding-row--info",
            };

        private static string SeverityTagModifier(ScenarioEditorFindingSeverity severity) =>
            severity switch
            {
                ScenarioEditorFindingSeverity.Error => "scenario-editor-shell-finding-tag--error",
                ScenarioEditorFindingSeverity.Warning => "scenario-editor-shell-finding-tag--warning",
                _ => "scenario-editor-shell-finding-tag--info",
            };

        private void ApplyAccessibility()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var settings = new C2AccessibilitySettings(accessibilityScale, reducedMotion);
            root.EnableInClassList("aegis-scale-100", settings.ScalePercent == C2AccessibilityScalePercent.OneHundred);
            root.EnableInClassList("aegis-scale-125", settings.ScalePercent == C2AccessibilityScalePercent.OneTwentyFive);
            root.EnableInClassList("aegis-scale-150", settings.ScalePercent == C2AccessibilityScalePercent.OneFifty);
            root.EnableInClassList("reduced-motion", settings.ReducedMotion);
        }

        private void ApplyPanelVisibility()
        {
            var root = _document.rootVisualElement;
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
#endif
