// DRG-255: Supervised advice accept/dismiss host (ADR-010 §2–3, ADR-001).
// Thin UI Toolkit client: projects SupervisedAdviceCardState and routes player decisions
// strictly through DelegationBridgeHost facade methods.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SupervisedAdvicePanelHost : MonoBehaviour
    {
        public const int DefaultSortingOrder = 18;

        private const string RootName = "supervised-advice-root";
        private const string HeaderName = "supervised-advice-header";
        private const string RationaleName = "supervised-advice-rationale";
        private const string AuthorityName = "supervised-advice-authority";
        private const string EvidenceListName = "supervised-advice-evidence-list";
        private const string OptInToggleName = "supervised-advice-optin-toggle";
        private const string AcceptButtonName = "supervised-advice-accept";
        private const string DismissButtonName = "supervised-advice-dismiss";
        private const string StatusName = "supervised-advice-status";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private string proposedCommandId = "engage";
        [SerializeField] private string attackOptionId = "fire-single";
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private VisualElement? _panelRoot;
        private Label? _headerLine;
        private Label? _rationaleLine;
        private Label? _authorityLine;
        private ListView? _evidenceList;
        private Toggle? _optInToggle;
        private Button? _acceptButton;
        private Button? _dismissButton;
        private Label? _statusLine;

        private IReadOnlyList<SupervisedAdviceEvidenceLine> _evidenceRows = Array.Empty<SupervisedAdviceEvidenceLine>();
        private AdviceFrame? _lastAdvice;
        private string? _lastUnitId;
        private string? _lastDismissedContactId;
        private bool _lastOptIn;
        private bool _lastShowPanel;
        private bool _wired;
        private bool _handlersRegistered;
        private string _lastStatusText = string.Empty;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            if (_document != null)
            {
                _document.sortingOrder = DefaultSortingOrder;
            }

            if (panelAsset != null && _document != null)
            {
                _document.visualTreeAsset = panelAsset;
            }
        }

        private void OnEnable()
        {
            TryWireElements();
            Refresh(force: true);
        }

        private void OnDisable()
        {
            UnregisterHandlers();
            _wired = false;
        }

        private void LateUpdate()
        {
            if (bridgeHost == null)
            {
                return;
            }

            if (!_wired)
            {
                TryWireElements();
            }

            Refresh(force: false);
        }

        private void TryWireElements()
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
            _panelRoot = panel;
            _headerLine = panel.Q<Label>(HeaderName);
            _rationaleLine = panel.Q<Label>(RationaleName);
            _authorityLine = panel.Q<Label>(AuthorityName);
            _evidenceList = panel.Q<ListView>(EvidenceListName);
            _optInToggle = panel.Q<Toggle>(OptInToggleName);
            _acceptButton = panel.Q<Button>(AcceptButtonName);
            _dismissButton = panel.Q<Button>(DismissButtonName);
            _statusLine = panel.Q<Label>(StatusName);

            if (_evidenceList != null)
            {
                _evidenceList.makeItem = () =>
                {
                    var label = new Label { name = "supervised-advice-evidence-row" };
                    label.AddToClassList("supervised-advice-evidence-row");
                    return label;
                };
                _evidenceList.bindItem = (element, index) =>
                {
                    if (element is Label label && index >= 0 && index < _evidenceRows.Count)
                    {
                        label.text = _evidenceRows[index].FormattedPointer;
                    }
                };
                _evidenceList.selectionType = SelectionType.None;
            }

            RegisterHandlers();
            _wired = _acceptButton != null || _dismissButton != null || _evidenceList != null;
        }

        private void RegisterHandlers()
        {
            if (_handlersRegistered || (_acceptButton == null && _dismissButton == null && _optInToggle == null))
            {
                return;
            }

            if (_acceptButton != null)
            {
                _acceptButton.clicked += OnAcceptClicked;
            }

            if (_dismissButton != null)
            {
                _dismissButton.clicked += OnDismissClicked;
            }

            if (_optInToggle != null)
            {
                _optInToggle.RegisterValueChangedCallback(OnOptInChanged);
            }

            _handlersRegistered = true;
        }

        private void UnregisterHandlers()
        {
            if (!_handlersRegistered)
            {
                return;
            }

            if (_acceptButton != null)
            {
                _acceptButton.clicked -= OnAcceptClicked;
            }

            if (_dismissButton != null)
            {
                _dismissButton.clicked -= OnDismissClicked;
            }

            if (_optInToggle != null)
            {
                _optInToggle.UnregisterValueChangedCallback(OnOptInChanged);
            }

            _handlersRegistered = false;
        }

        private void OnOptInChanged(ChangeEvent<bool> _)
        {
            Refresh(force: true);
        }

        private void OnAcceptClicked()
        {
            if (bridgeHost == null)
            {
                SetStatus("NO_BRIDGE");
                return;
            }

            var optIn = _optInToggle != null && _optInToggle.value;
            var result = bridgeHost.TryAcceptSupervisedAdvice(proposedCommandId, attackOptionId, optIn);
            SetStatus(result.Accepted
                ? $"ENQUEUED seq={result.PlayerOrderSequenceId} unit={result.UnitId}"
                : $"BLOCKED: {result.FailureReason}");
            Refresh(force: true);
        }

        private void OnDismissClicked()
        {
            if (bridgeHost == null)
            {
                return;
            }

            bridgeHost.DismissSupervisedAdvice();
            SetStatus("DISMISSED (no order-log change)");
            Refresh(force: true);
        }

        private void Refresh(bool force)
        {
            if (!_wired || bridgeHost == null)
            {
                return;
            }

            var advice = bridgeHost.LastAdvice;
            var unitId = bridgeHost.SupervisedAdviceUnitId;
            var optIn = _optInToggle != null && _optInToggle.value;
            var dismissed = bridgeHost.DismissedSupervisedAdviceContactId;

            if (!force
                && ReferenceEquals(advice, _lastAdvice)
                && string.Equals(unitId, _lastUnitId, StringComparison.Ordinal)
                && string.Equals(dismissed, _lastDismissedContactId, StringComparison.Ordinal)
                && optIn == _lastOptIn
                && showPanel == _lastShowPanel)
            {
                return;
            }

            _lastAdvice = advice;
            _lastUnitId = unitId;
            _lastDismissedContactId = dismissed;
            _lastOptIn = optIn;
            _lastShowPanel = showPanel;

            var card = bridgeHost.ProjectSupervisedAdviceCard(proposedCommandId, attackOptionId, optIn);
            var visible = showPanel && card.CardVisible;
            if (_panelRoot != null)
            {
                _panelRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_headerLine != null)
            {
                _headerLine.text = visible
                    ? $"SUPERVISED ADVICE: {card.UnitId} -> {card.ContactId} ({card.ResolvedAutonomy})"
                    : "SUPERVISED ADVICE";
            }

            if (_rationaleLine != null)
            {
                _rationaleLine.text = visible ? card.Rationale : string.Empty;
            }

            if (_authorityLine != null)
            {
                _authorityLine.text = visible ? card.AuthoritySummary : string.Empty;
            }

            _evidenceRows = card.EvidenceLines;
            if (_evidenceList != null)
            {
                _evidenceList.itemsSource = (System.Collections.IList)_evidenceRows;
                _evidenceList.Rebuild();
            }

            if (_optInToggle != null)
            {
                _optInToggle.style.display = card.RequiresFullAutonomousOptIn ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_acceptButton != null)
            {
                _acceptButton.SetEnabled(visible && card.CanAccept);
                _acceptButton.tooltip = card.BlockReason ?? "Enqueue supervised player order";
            }

            if (_dismissButton != null)
            {
                _dismissButton.SetEnabled(visible);
            }
        }

        private void SetStatus(string status)
        {
            _lastStatusText = status;
            if (_statusLine != null)
            {
                _statusLine.text = status;
            }
        }

        public bool IsWired => _wired;
        public string StatusText => _lastStatusText;
    }
}
#endif
