// DRG-253: OSINT staging review host (ADR-006 / ADR-010 §2–3).
// Thin UI Toolkit client: reads OsintStagingBridge + OsintStagingProjection only;
// never opens databases or instantiates write gates directly.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class OsintStagingPanelHost : MonoBehaviour
    {
        public const int DefaultSortingOrder = 17;

        private const string RootName = "osint-staging-root";
        private const string ListName = "proposal-list";
        private const string LogOnlyListName = "logonly-list";
        private const string StatusName = "status-line";
        private const string DetailTitleName = "osint-detail-title";
        private const string DetailConfidenceName = "osint-detail-confidence";
        private const string DetailTrlName = "osint-detail-trl";
        private const string DetailCitationUrlName = "osint-detail-citation-url";
        private const string DetailCitationSnippetName = "osint-detail-citation-snippet";
        private const string ApproveName = "approve-selected";
        private const string RejectName = "reject-selected";
        private const string RefreshName = "refresh-button";

        [SerializeField] private UIDocument? document;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private string databasePathForProxy = "data/catalog.db";
        [SerializeField] private string fixturePathForFallback = "data/osint_facts.json";
        [SerializeField] private string reviewerActorType = "human";
        [SerializeField] private string reviewerActorId = "osint-staging-panel";

        private UIDocument _document = null!;
        private ListView? _proposalList;
        private ListView? _logOnlyList;
        private Label? _statusLine;
        private Label? _detailTitle;
        private Label? _detailConfidence;
        private Label? _detailTrl;
        private Label? _detailCitationUrl;
        private Label? _detailCitationSnippet;
        private Button? _approveButton;
        private Button? _rejectButton;
        private Button? _refreshButton;

        private OsintStagingFeed _feed = new OsintStagingFeed(
            Array.Empty<OsintStagingProposalItem>(),
            Array.Empty<OsintStagingProposalItem>(),
            "empty");
        private OsintStagingPanelState? _panelState;
        private string? _selectedItemId;
        private string? _boundDatabasePath;
        private string? _boundFixturePath;
        private long _clockTicks = 1000;
        private bool _wired;
        private bool _handlersRegistered;
        private string _lastAppliedFingerprint = string.Empty;

        private void Awake()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }

            _document = document;
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            if (_document != null)
            {
                _document.sortingOrder = DefaultSortingOrder;
                if (panelAsset != null && _document.visualTreeAsset != panelAsset)
                {
                    _document.visualTreeAsset = panelAsset;
                }
            }
        }

        private void OnEnable()
        {
            SetupUI();
            RefreshProposals();
        }

        private void LateUpdate()
        {
            if (!_wired)
            {
                SetupUI();
                if (_wired)
                {
                    RefreshProposals();
                }
            }
        }

        private void OnDisable()
        {
            UnregisterHandlers();
            _wired = false;
        }

        public void BindDatabasePath(string databasePath)
        {
            _boundDatabasePath = databasePath;
            if (_wired)
            {
                RefreshProposals();
            }
        }

        public void BindPaths(string databasePath, string? fixturePath)
        {
            _boundDatabasePath = databasePath;
            _boundFixturePath = fixturePath;
            if (_wired)
            {
                RefreshProposals();
            }
        }

        private void SetupUI()
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            if (_document != null)
            {
                _document.sortingOrder = DefaultSortingOrder;
            }

            if (panelAsset != null && _document != null)
            {
                UnregisterHandlers();
                if (_document.visualTreeAsset != panelAsset)
                {
                    _document.visualTreeAsset = panelAsset;
                }
            }
            else
            {
                UnregisterHandlers();
            }

            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null)
            {
                return;
            }

            if (panelStyles != null && !root.styleSheets.Contains(panelStyles))
            {
                root.styleSheets.Add(panelStyles);
            }

            var panel = root.Q<VisualElement>(RootName) ?? root;
            _proposalList = panel.Q<ListView>(ListName);
            _logOnlyList = panel.Q<ListView>(LogOnlyListName);
            _statusLine = panel.Q<Label>(StatusName);
            _detailTitle = panel.Q<Label>(DetailTitleName);
            _detailConfidence = panel.Q<Label>(DetailConfidenceName);
            _detailTrl = panel.Q<Label>(DetailTrlName);
            _detailCitationUrl = panel.Q<Label>(DetailCitationUrlName);
            _detailCitationSnippet = panel.Q<Label>(DetailCitationSnippetName);
            _approveButton = panel.Q<Button>(ApproveName);
            _rejectButton = panel.Q<Button>(RejectName);
            _refreshButton = panel.Q<Button>(RefreshName);

            ConfigureProposalList();
            ConfigureLogOnlyList();
            RegisterHandlers();

            _wired = _proposalList != null || _detailConfidence != null || _approveButton != null;
        }

        private void ConfigureProposalList()
        {
            if (_proposalList == null)
            {
                return;
            }

            _proposalList.makeItem = () =>
            {
                var label = new Label { name = "proposal-row" };
                label.AddToClassList("osint-proposal-row");
                return label;
            };
            _proposalList.bindItem = (e, i) =>
            {
                if (i < 0 || i >= _feed.Proposals.Count) return;
                if (e is Label label)
                {
                    label.text = OsintStagingProjection.FormatProposalRow(_feed.Proposals[i]);
                }
            };
            _proposalList.selectionType = SelectionType.Single;
            _proposalList.selectionChanged -= OnProposalSelectionChanged;
            _proposalList.selectionChanged += OnProposalSelectionChanged;
        }

        private void ConfigureLogOnlyList()
        {
            if (_logOnlyList == null)
            {
                return;
            }

            _logOnlyList.makeItem = () =>
            {
                var label = new Label { name = "logonly-row" };
                label.AddToClassList("osint-logonly-row");
                return label;
            };
            _logOnlyList.bindItem = (e, i) =>
            {
                if (i < 0 || i >= _feed.LogOnly.Count) return;
                if (e is Label label)
                {
                    label.text = OsintStagingProjection.FormatLogOnlyRow(_feed.LogOnly[i]);
                }
            };
            _logOnlyList.selectionType = SelectionType.Single;
            _logOnlyList.selectionChanged -= OnLogOnlySelectionChanged;
            _logOnlyList.selectionChanged += OnLogOnlySelectionChanged;
        }

        private void RegisterHandlers()
        {
            if (_handlersRegistered)
            {
                return;
            }

            if (_approveButton != null)
            {
                _approveButton.clicked += OnApproveSelected;
            }

            if (_rejectButton != null)
            {
                _rejectButton.clicked += OnRejectSelected;
            }

            if (_refreshButton != null)
            {
                _refreshButton.clicked += RefreshProposals;
            }

            _handlersRegistered = true;
        }

        private void UnregisterHandlers()
        {
            if (_proposalList != null)
            {
                _proposalList.selectionChanged -= OnProposalSelectionChanged;
            }

            if (_logOnlyList != null)
            {
                _logOnlyList.selectionChanged -= OnLogOnlySelectionChanged;
            }

            if (_handlersRegistered)
            {
                if (_approveButton != null)
                {
                    _approveButton.clicked -= OnApproveSelected;
                }

                if (_rejectButton != null)
                {
                    _rejectButton.clicked -= OnRejectSelected;
                }

                if (_refreshButton != null)
                {
                    _refreshButton.clicked -= RefreshProposals;
                }
            }

            _handlersRegistered = false;
        }

        private void OnProposalSelectionChanged(IEnumerable<object> _)
        {
            if (_proposalList == null) return;
            var idx = _proposalList.selectedIndex;
            if (idx >= 0 && idx < _feed.Proposals.Count)
            {
                _selectedItemId = _feed.Proposals[idx].ItemId;
                ProjectAndApply(forceStatus: false);
            }
        }

        private void OnLogOnlySelectionChanged(IEnumerable<object> _)
        {
            if (_logOnlyList == null) return;
            var idx = _logOnlyList.selectedIndex;
            if (idx >= 0 && idx < _feed.LogOnly.Count)
            {
                _selectedItemId = _feed.LogOnly[idx].ItemId;
                ProjectAndApply(forceStatus: false);
            }
        }

        public void RefreshProposals()
        {
            var dbPath = _boundDatabasePath ?? databasePathForProxy;
            var fixturePath = _boundFixturePath ?? fixturePathForFallback;
            _feed = OsintStagingBridge.LoadFeed(dbPath, fixturePath);

            if (_proposalList != null)
            {
                _proposalList.itemsSource = (System.Collections.IList)_feed.Proposals;
                _proposalList.Rebuild();
            }

            if (_logOnlyList != null)
            {
                _logOnlyList.itemsSource = (System.Collections.IList)_feed.LogOnly;
                _logOnlyList.Rebuild();
            }

            _lastAppliedFingerprint = string.Empty;
            ProjectAndApply(forceStatus: true);
        }

        private void ProjectAndApply(bool forceStatus)
        {
            var state = OsintStagingProjection.Project(_feed, _selectedItemId);
            _panelState = state;
            if (state.SelectedDetail != null)
            {
                _selectedItemId = state.SelectedDetail.ItemId;
            }

            if (!forceStatus && string.Equals(_lastAppliedFingerprint, state.Fingerprint, StringComparison.Ordinal))
            {
                return;
            }

            _lastAppliedFingerprint = state.Fingerprint;

            if (forceStatus && _statusLine != null)
            {
                _statusLine.text = state.StatusSummary;
            }

            var detail = state.SelectedDetail;
            if (detail == null)
            {
                if (_detailTitle != null) _detailTitle.text = "No proposal selected";
                if (_detailConfidence != null) _detailConfidence.text = "CONFIDENCE: —";
                if (_detailTrl != null) _detailTrl.text = "TRL —";
                if (_detailCitationUrl != null) _detailCitationUrl.text = "CITATION: —";
                if (_detailCitationSnippet != null) _detailCitationSnippet.text = "SNIPPET: —";
                if (_approveButton != null) _approveButton.SetEnabled(false);
                if (_rejectButton != null) _rejectButton.SetEnabled(false);
                return;
            }

            if (_detailTitle != null) _detailTitle.text = detail.Title;
            if (_detailConfidence != null) _detailConfidence.text = detail.ConfidenceText;
            if (_detailTrl != null) _detailTrl.text = detail.TrlAndStateText;
            if (_detailCitationUrl != null)
            {
                _detailCitationUrl.text = string.IsNullOrWhiteSpace(detail.CitationUrl)
                    ? "CITATION: (none)"
                    : "CITATION: " + detail.CitationUrl;
            }
            if (_detailCitationSnippet != null)
            {
                _detailCitationSnippet.text = string.IsNullOrWhiteSpace(detail.Snippet)
                    ? "SNIPPET: (none)"
                    : "SNIPPET: " + detail.Snippet;
            }

            if (_approveButton != null)
            {
                _approveButton.SetEnabled(detail.CanApprove);
                _approveButton.tooltip = detail.ApproveBlockedReason ?? "Approve and commit via write gate";
            }

            if (_rejectButton != null)
            {
                _rejectButton.SetEnabled(detail.CanReject);
            }
        }

        private void OnApproveSelected()
        {
            var selected = ResolveSelectedItem();
            if (selected == null)
            {
                return;
            }

            var dbPath = _boundDatabasePath ?? databasePathForProxy;
            var result = OsintStagingBridge.ApproveItem(
                dbPath,
                selected,
                reviewerActorType,
                reviewerActorId,
                ++_clockTicks);

            _selectedItemId = null;
            RefreshProposals();
            if (_statusLine != null)
            {
                if (result.Committed)
                {
                    _statusLine.text = $"{_panelState?.StatusSummary ?? "OSINT Staging"} | COMMITTED ({result.BatchId})";
                }
                else
                {
                    var err = result.Errors.Count > 0 ? string.Join(", ", result.Errors) : "APPROVE_BLOCKED";
                    _statusLine.text = $"{_panelState?.StatusSummary ?? "OSINT Staging"} | BLOCKED: {err}";
                }
            }
        }

        private void OnRejectSelected()
        {
            var selected = ResolveSelectedItem();
            if (selected == null || !selected.IsPendingBatch || selected.IsLogOnly)
            {
                return;
            }

            var dbPath = _boundDatabasePath ?? databasePathForProxy;
            var result = OsintStagingBridge.RejectBatch(
                dbPath,
                selected.BatchId,
                reviewerActorType,
                reviewerActorId,
                "rejected from review panel",
                ++_clockTicks);

            _selectedItemId = null;
            RefreshProposals();
            if (_statusLine != null)
            {
                if (result.Rejected)
                {
                    _statusLine.text = $"{_panelState?.StatusSummary ?? "OSINT Staging"} | REJECTED ({result.BatchId})";
                }
                else
                {
                    var err = result.Errors.Count > 0 ? string.Join(", ", result.Errors) : "REJECT_FAILED";
                    _statusLine.text = $"{_panelState?.StatusSummary ?? "OSINT Staging"} | REJECT_ERROR: {err}";
                }
            }
        }

        private OsintStagingProposalItem? ResolveSelectedItem()
        {
            if (_panelState?.SelectedDetail == null)
            {
                return null;
            }

            var id = _panelState.SelectedDetail.ItemId;
            for (var i = 0; i < _feed.Proposals.Count; i++)
            {
                if (string.Equals(_feed.Proposals[i].ItemId, id, StringComparison.Ordinal))
                {
                    return _feed.Proposals[i];
                }
            }

            for (var i = 0; i < _feed.LogOnly.Count; i++)
            {
                if (string.Equals(_feed.LogOnly[i].ItemId, id, StringComparison.Ordinal))
                {
                    return _feed.LogOnly[i];
                }
            }

            return null;
        }

        public bool IsWired => _wired;
        public string StatusText => _statusLine != null ? _statusLine.text : string.Empty;
        public int ProposalCount => _feed.Proposals.Count;
        public int LogOnlyCount => _feed.LogOnly.Count;
        public OsintStagingDetailState? SelectedDetail => _panelState?.SelectedDetail;
    }
}
#endif
