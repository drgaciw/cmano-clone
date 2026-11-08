// CMD-35 live editing findings contract panel — status line + finding codes + gated Commit.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Thin UI Toolkit host over <see cref="LiveEditPanelState"/>.
    /// Does not invent sim state — bind via <see cref="ApplyState"/> / <see cref="ApplyPresentation"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class LiveEditPanelHost : MonoBehaviour
    {
        private const string RootName = "live-edit-root";
        private const string StatusName = "live-edit-status";
        private const string FindingsListName = "live-edit-findings-list";
        private const string CommitName = "live-edit-commit";
        private const string CommitReasonName = "live-edit-commit-reason";
        private const string StatusBlockedClass = "live-edit-status--blocked";

        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _statusLine;
        private VisualElement? _findingsList;
        private Button? _commitButton;
        private Label? _commitReason;
        private LiveEditPanelState _state = LiveEditPanelState.Empty;
        private bool _wired;

        /// <summary>Last bound panel state (tests / curator).</summary>
        public LiveEditPanelState PanelState => _state;

        /// <summary>Raised when Commit is clicked and the gate allows it.</summary>
        public event Action? CommitRequested;

        private void Reset()
        {
            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
        }

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
            ApplyPanelVisibility();
        }

        private void OnDisable()
        {
            if (_commitButton != null)
            {
                _commitButton.clicked -= OnCommitClicked;
            }
        }

        /// <summary>Apply headless panel state (status + finding codes + commit gate).</summary>
        public void ApplyState(LiveEditPanelState state)
        {
            _state = state ?? LiveEditPanelState.Empty;
            if (!_wired)
            {
                TryWireElements();
            }

            if (_statusLine != null)
            {
                _statusLine.text = _state.StatusLine ?? string.Empty;
                if (_state.BlockingCount > 0 || !_state.LastMutationOk)
                {
                    _statusLine.AddToClassList(StatusBlockedClass);
                }
                else
                {
                    _statusLine.RemoveFromClassList(StatusBlockedClass);
                }
            }

            RebuildFindingsList(_state.Rows);

            if (_commitButton != null)
            {
                _commitButton.SetEnabled(_state.CommitEnabled);
                _commitButton.tooltip = _state.CommitEnabled
                    ? "Commit live edit"
                    : (_state.CommitDisabledReason ?? "Commit disabled");
            }

            if (_commitReason != null)
            {
                _commitReason.text = _state.CommitEnabled
                    ? string.Empty
                    : (_state.CommitDisabledReason ?? string.Empty);
            }
        }

        /// <summary>Apply presentation via binder (tests / session glue).</summary>
        public void ApplyPresentation(LiveEditSessionPresentation presentation) =>
            ApplyState(LiveEditPanelBinder.Bind(presentation));

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var panel = root.Q<VisualElement>(RootName) ?? root;
            _statusLine = panel.Q<Label>(StatusName);
            _findingsList = panel.Q<VisualElement>(FindingsListName);
            _commitButton = panel.Q<Button>(CommitName);
            _commitReason = panel.Q<Label>(CommitReasonName);
            _wired = _statusLine != null && _findingsList != null;

            if (_commitButton != null)
            {
                _commitButton.clicked -= OnCommitClicked;
                _commitButton.clicked += OnCommitClicked;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }
        }

        private void RebuildFindingsList(IReadOnlyList<LiveEditFindingDisplayRow> rows)
        {
            if (_findingsList == null)
            {
                return;
            }

            _findingsList.Clear();
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            foreach (var row in rows)
            {
                if (row is null)
                {
                    continue;
                }

                var label = new Label(row.DisplayLine ?? row.Code ?? string.Empty);
                label.AddToClassList("live-edit-finding-row");
                label.AddToClassList(SeverityClass(row.Severity));
                _findingsList.Add(label);
            }
        }

        private static string SeverityClass(string? severity)
        {
            if (string.Equals(severity, "Error", StringComparison.OrdinalIgnoreCase))
            {
                return "live-edit-finding-row--error";
            }

            if (string.Equals(severity, "Warning", StringComparison.OrdinalIgnoreCase))
            {
                return "live-edit-finding-row--warning";
            }

            return "live-edit-finding-row--info";
        }

        private void OnCommitClicked()
        {
            if (!_state.CommitEnabled)
            {
                return;
            }

            CommitRequested?.Invoke();
        }

        private void ApplyPanelVisibility()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
#endif
