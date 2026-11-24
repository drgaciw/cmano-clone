// Combat message log strip — UI Toolkit panel bound to DelegationBridgeHost.
// S104-02 / S105 A2: row selection by index, sequenceId, unit focus + category class map.
// CMD-23: collapsible body via C2ChromeCollapseState + prefs bag on bridge host.
// DRG-408 / DRG-273: policy settings drawer, alert ACK/dismiss banner, row attribution & non-mutating DecisionLog deep-link.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MessageLogPanelHost : MonoBehaviour
    {
        private const string RootName = "message-log-root";
        private static readonly string ListName = KeyboardDiscoverySurfaces.MessageLogListElementName;
        private const string OnboardingHintName = "message-log-onboarding-hint";
        private const string CollapseToggleName = "message-log-collapse-toggle";
        private const string PolicyToggleName = "message-log-policy-toggle";
        private const string PolicyDrawerName = "message-log-policy-drawer";
        private const string AlertBannerName = "message-log-alert-banner";
        private const string DeepLinkDrawerName = "message-log-deeplink-drawer";
        private const string DeepLinkHeaderName = "message-log-deeplink-header";
        private const string DeepLinkAttributionName = "message-log-deeplink-attribution";
        private const string DeepLinkDetailName = "message-log-deeplink-detail";
        private const string DeepLinkTelemetryName = "message-log-deeplink-telemetry";
        private const string TitleClass = "message-log-title";
        private const string CollapseToggleClass = "message-log-collapse-toggle";
        private const string CollapsedClass = "message-log-panel--collapsed";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private int maxRows = 12;

        private UIDocument _document = null!;
        private VisualElement? _panelRoot;
        private ListView? _messageList;
        private VisualElement? _onboardingHint;
        private Button? _collapseToggle;
        private Button? _policyToggle;
        private VisualElement? _policyDrawer;
        private VisualElement? _alertBanner;
        private VisualElement? _deepLinkDrawer;
        private Label? _deepLinkHeader;
        private Label? _deepLinkAttribution;
        private Label? _deepLinkDetail;
        private Label? _deepLinkTelemetry;
        private MessageLogPanelState _panelState = new(Array.Empty<MessageLogDisplayRow>());
        private DecisionLogDeepLinkPresentation _deepLinkPresentation = DecisionLogDeepLinkPresentation.Empty;
        private bool _policyDrawerOpen;
        private string _lastPolicySignature = string.Empty;
        private string _lastAlertSignature = string.Empty;
        private bool _wired;

        /// <summary>Last selected order-log sequence id (null if none).</summary>
        public ulong? SelectedSequenceId { get; private set; }

        /// <summary>Last selected unit id from message log row (null if none).</summary>
        public string? SelectedUnitId { get; private set; }

        /// <summary>Last selected row index in the current panel (null if none).</summary>
        public int? SelectedRowIndex { get; private set; }

        /// <summary>Current bound panel rows (for tests).</summary>
        public MessageLogPanelState PanelState => _panelState;

        /// <summary>DRG-273: last bound non-mutating DecisionLog deep-link detail.</summary>
        public DecisionLogDeepLinkPresentation LastDeepLinkPresentation => _deepLinkPresentation;

        /// <summary>DRG-408: whether the category policy drawer is currently expanded.</summary>
        public bool IsPolicyDrawerOpen => _policyDrawerOpen;

        /// <summary>Last applied chrome collapse presentation (CMD-23).</summary>
        public C2ChromeCollapsePresentation LastChromePresentation { get; private set; } =
            C2ChromeCollapsePresentation.Empty;

        private void Reset()
        {
            if (bridgeHost == null)
            {
                bridgeHost = GetComponent<DelegationBridgeHost>();
            }

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
            Refresh();
        }

        private void LateUpdate()
        {
            if (!_wired)
            {
                TryWireElements();
            }

            if (!showPanel)
            {
                ApplyMasterVisibility();
                return;
            }

            if (bridgeHost == null)
            {
                // Null-safe chrome: expanded defaults under showPanel master visibility.
                ApplyMasterVisibility();
                ApplyChromeCollapse();
                return;
            }

            Refresh();
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var panel = root.Q<VisualElement>(RootName) ?? root;
            _panelRoot = panel;
            _messageList = panel.Q<ListView>(ListName);
            _onboardingHint = panel.Q(OnboardingHintName);
            _policyToggle = panel.Q<Button>(PolicyToggleName);
            _policyDrawer = panel.Q<VisualElement>(PolicyDrawerName);
            _alertBanner = panel.Q<VisualElement>(AlertBannerName);
            _deepLinkDrawer = panel.Q<VisualElement>(DeepLinkDrawerName);
            _deepLinkHeader = panel.Q<Label>(DeepLinkHeaderName);
            _deepLinkAttribution = panel.Q<Label>(DeepLinkAttributionName);
            _deepLinkDetail = panel.Q<Label>(DeepLinkDetailName);
            _deepLinkTelemetry = panel.Q<Label>(DeepLinkTelemetryName);

            EnsureCollapseToggle(panel);
            if (_policyToggle != null)
            {
                _policyToggle.clicked -= OnPolicyToggleClicked;
                _policyToggle.clicked += OnPolicyToggleClicked;
            }

            _wired = _messageList != null;

            if (_messageList != null)
            {
                // DRG-270: Tab order reaches message-log rows; USS uses var(--focus-ring).
                _messageList.focusable = true;
                _messageList.makeItem = () =>
                {
                    var label = new Label();
                    label.focusable = true;
                    label.AddToClassList(MessageLogCategoryClassMap.BaseRowClass);
                    label.RegisterCallback<KeyDownEvent>(OnMessageRowKeyDown);
                    return label;
                };
                _messageList.bindItem = (element, index) =>
                {
                    if (element is not Label label || index < 0 || index >= _panelState.Rows.Count)
                    {
                        return;
                    }

                    var row = _panelState.Rows[index];
                    label.text = MessageLogPanelBinder.FormatAttributedDisplayLine(
                        row,
                        bridgeHost != null ? bridgeHost.NotificationPolicy : null);
                    label.userData = index;
                    label.ClearClassList();
                    label.AddToClassList(MessageLogCategoryClassMap.BaseRowClass);
                    label.AddToClassList(MessageLogCategoryClassMap.SelectableRowClass);
                    if (!string.IsNullOrEmpty(row.CategoryCssClass))
                    {
                        label.AddToClassList(row.CategoryCssClass);
                    }

                    var fuelBand = MessageLogFuelBandBinder.Bind(row);
                    foreach (var knownCue in FuelBandCueClasses.All)
                    {
                        label.RemoveFromClassList(knownCue);
                    }

                    if (!string.IsNullOrEmpty(fuelBand.CueClass))
                    {
                        label.AddToClassList(fuelBand.CueClass);
                    }
                };
                _messageList.selectionType = SelectionType.Single;
                _messageList.selectionChanged -= OnMessageSelectionChanged;
                _messageList.selectionChanged += OnMessageSelectionChanged;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            ApplyMasterVisibility();
            ApplyChromeCollapse();
        }

        private void EnsureCollapseToggle(VisualElement panel)
        {
            _collapseToggle = panel.Q<Button>(CollapseToggleName);
            if (_collapseToggle == null)
            {
                _collapseToggle = new Button
                {
                    name = CollapseToggleName,
                    text = C2ChromeCollapseProjection.CollapseMessageLogLabel,
                };
                _collapseToggle.AddToClassList(CollapseToggleClass);
                _collapseToggle.focusable = true;

                // Prefer a title-row strip: insert after title label when present.
                var title = panel.Q<Label>(className: TitleClass);
                if (title != null)
                {
                    var titleIndex = panel.IndexOf(title);
                    if (titleIndex >= 0)
                    {
                        panel.Insert(titleIndex + 1, _collapseToggle);
                    }
                    else
                    {
                        panel.Insert(0, _collapseToggle);
                    }
                }
                else
                {
                    panel.Insert(0, _collapseToggle);
                }
            }

            _collapseToggle.clicked -= OnCollapseToggleClicked;
            _collapseToggle.clicked += OnCollapseToggleClicked;
        }

        private void OnCollapseToggleClicked()
        {
            // Null-safe: no bridge chrome API → no-op (showPanel remains master visibility).
            if (bridgeHost == null)
            {
                return;
            }

            bridgeHost.ToggleMessageLogCollapsed();
            ApplyChromeCollapse();
        }

        private void OnPolicyToggleClicked()
        {
            _policyDrawerOpen = !_policyDrawerOpen;
            _lastPolicySignature = string.Empty;
            RefreshPolicyDrawer();
        }

        private void OnMessageSelectionChanged(IEnumerable<object> _)
        {
            if (_messageList == null)
            {
                return;
            }

            ApplySelectionIndex(_messageList.selectedIndex);
        }

        private void OnMessageRowKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Space))
            {
                return;
            }

            if (evt.currentTarget is Label { userData: int index })
            {
                if (_messageList != null)
                {
                    _messageList.SetSelection(index);
                }

                TrySelectRow(index);
                evt.StopPropagation();
            }
        }

        /// <summary>Headless-friendly selection entry (also used by host selection callback).</summary>
        public bool TrySelectRow(int index)
        {
            return ApplySelection(MessageLogPanelSelection.SelectRow(_panelState, index));
        }

        /// <summary>Select by order-log sequence id (CMD-05 deep-link).</summary>
        public bool TrySelectBySequenceId(ulong sequenceId)
        {
            return ApplySelection(MessageLogPanelSelection.SelectBySequenceId(_panelState, sequenceId));
        }

        /// <summary>Unit focus: latest message-log row for unitId, then bridge SelectUnit.</summary>
        public bool TrySelectByUnitId(string unitId)
        {
            return ApplySelection(MessageLogPanelSelection.SelectByUnitId(_panelState, unitId));
        }

        private bool ApplySelection(MessageLogSelection? selection)
        {
            if (selection == null)
            {
                SelectedSequenceId = null;
                SelectedUnitId = null;
                SelectedRowIndex = null;
                _deepLinkPresentation = DecisionLogDeepLinkPresentation.Empty;
                bridgeHost?.SelectMessageLogDeepLink(null);
                ApplyDeepLinkDrawer();
                return false;
            }

            SelectedSequenceId = selection.SequenceId;
            SelectedUnitId = selection.UnitId;
            SelectedRowIndex = selection.RowIndex;

            RefreshDeepLinkSelection();

            // DRG-268: Fog-safe selection — only fall back to raw SelectUnit if no deep-link detail existed and not fog-suppressed.
            if (bridgeHost != null
                && !bridgeHost.LastMessageLogExplainLink.HasExplain
                && !string.IsNullOrEmpty(selection.UnitId))
            {
                bridgeHost.SelectUnit(selection.UnitId);
            }

            return true;
        }

        private void ApplySelectionIndex(int index)
        {
            TrySelectRow(index);
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null || _messageList == null)
            {
                ApplyMasterVisibility();
                ApplyChromeCollapse();
                return;
            }

            // UAT D38 / DRG-408: filter Log == false categories before taking the COMMS-pinned tail window.
            var lines = MessageLogRowWindow.Select(
                bridgeHost.LastMessageLog,
                maxRows,
                bridgeHost.NotificationPolicy);

            _panelState = MessageLogPanelBinder.Bind(
                lines,
                bridgeHost.NotificationPolicy,
                MessageLogBindingSource.LiveOrderLogView);

            // Skip list rebuild when collapsed (body hidden) to avoid work.
            if (!bridgeHost.ChromeCollapse.MessageLogCollapsed)
            {
                _messageList.itemsSource = _panelState.Rows.ToList();
                _messageList.Rebuild();
            }

            RefreshAlertBanner();
            RefreshPolicyDrawer();
            ApplyDeepLinkDrawer();
            ApplyMasterVisibility();
            ApplyChromeCollapse();
        }

        private void RefreshDeepLinkSelection()
        {
            if (bridgeHost == null || !SelectedSequenceId.HasValue)
            {
                _deepLinkPresentation = DecisionLogDeepLinkPresentation.Empty;
                bridgeHost?.SelectMessageLogDeepLink(null);
                ApplyDeepLinkDrawer();
                return;
            }

            var detail = bridgeHost.InspectMessageLogDecision(SelectedSequenceId.Value);
            _deepLinkPresentation = DecisionLogDeepLinkBinder.Bind(detail);
            bridgeHost.SelectMessageLogDeepLink(detail);
            ApplyDeepLinkDrawer();
        }

        private void ApplyDeepLinkDrawer()
        {
            if (_deepLinkDrawer == null)
            {
                return;
            }

            var collapsed = bridgeHost != null && bridgeHost.ChromeCollapse.MessageLogCollapsed;
            var visible = showPanel && !collapsed && _deepLinkPresentation.Visible;
            _deepLinkDrawer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (_deepLinkHeader != null)
            {
                _deepLinkHeader.text = _deepLinkPresentation.HeaderLine;
            }

            if (_deepLinkAttribution != null)
            {
                _deepLinkAttribution.text = _deepLinkPresentation.AttributionLine;
            }

            if (_deepLinkDetail != null)
            {
                _deepLinkDetail.text = _deepLinkPresentation.Found
                    ? _deepLinkPresentation.RationaleLine
                    : _deepLinkPresentation.UnavailableText;
            }

            if (_deepLinkTelemetry != null)
            {
                var telemetry = _deepLinkPresentation.TelemetryLine;
                if (_deepLinkPresentation.FieldLines.Count > 0)
                {
                    var joinedFields = string.Join(" · ", _deepLinkPresentation.FieldLines);
                    telemetry = string.IsNullOrEmpty(telemetry) ? joinedFields : telemetry + " | " + joinedFields;
                }

                _deepLinkTelemetry.text = telemetry;
            }
        }

        private void RefreshAlertBanner()
        {
            if (_alertBanner == null || bridgeHost == null)
            {
                return;
            }

            var isReplay = bridgeHost.IsReplayViewerAttached;
            var rows = MessageLogAlertBannerProjection.Project(
                bridgeHost.UnresolvedMessageAlerts,
                bridgeHost.ActiveMessageAlerts,
                isReplay);
            var sig = isReplay + ":" + string.Join(",", rows.Select(r => $"{r.SequenceId}:{r.ShowAck}:{r.CanDismiss}:{r.Prefix}"));
            if (string.Equals(sig, _lastAlertSignature, StringComparison.Ordinal))
            {
                var collapsed = bridgeHost.ChromeCollapse.MessageLogCollapsed;
                _alertBanner.style.display = !collapsed && showPanel && rows.Count > 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                return;
            }

            _lastAlertSignature = sig;
            _alertBanner.Clear();

            if (rows.Count == 0)
            {
                _alertBanner.style.display = DisplayStyle.None;
                return;
            }

            _alertBanner.style.display = DisplayStyle.Flex;
            for (var i = 0; i < rows.Count; i++)
            {
                var alert = rows[i];
                var seqId = alert.SequenceId;
                var row = new VisualElement();
                row.AddToClassList("message-log-alert-row");

                var label = new Label($"{alert.Prefix} {alert.Category}: {alert.Text}");
                label.AddToClassList("message-log-alert-text");
                row.Add(label);

                if (alert.ShowAck)
                {
                    var ackBtn = new Button(() =>
                    {
                        if (bridgeHost != null && !bridgeHost.IsReplayViewerAttached)
                        {
                            bridgeHost.TryAcknowledgeMessageAlert(seqId);
                            RefreshAlertBanner();
                        }
                    })
                    {
                        text = "ACK",
                    };
                    ackBtn.SetEnabled(alert.CanAck);
                    row.Add(ackBtn);
                }

                var dismissBtn = new Button(() =>
                {
                    if (bridgeHost != null && !bridgeHost.IsReplayViewerAttached)
                    {
                        bridgeHost.TryDismissMessageAlert(seqId);
                        RefreshAlertBanner();
                    }
                })
                {
                    text = "DISMISS",
                };
                dismissBtn.SetEnabled(alert.CanDismiss);
                row.Add(dismissBtn);

                _alertBanner.Add(row);
            }
        }

        private void RefreshPolicyDrawer()
        {
            if (_policyDrawer == null || bridgeHost == null)
            {
                return;
            }

            var collapsed = bridgeHost.ChromeCollapse.MessageLogCollapsed;
            var visible = showPanel && !collapsed && _policyDrawerOpen;
            _policyDrawer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
            {
                return;
            }

            var isReplay = bridgeHost.IsReplayViewerAttached;
            var rows = MessageCategoryPolicyBinder.Bind(bridgeHost.NotificationPolicy);
            var sig = isReplay + ":" + string.Join(";", rows.Select(r => $"{r.Category}:{r.Log}{r.Pause}{r.MapBalloon}{r.DropToRealTime}"));
            if (string.Equals(sig, _lastPolicySignature, StringComparison.Ordinal))
            {
                return;
            }

            _lastPolicySignature = sig;
            _policyDrawer.Clear();

            var resetBtn = new Button(() =>
            {
                if (bridgeHost != null && !bridgeHost.IsReplayViewerAttached)
                {
                    bridgeHost.ResetMessageCategoryPolicyDefaults();
                    _lastPolicySignature = string.Empty;
                    Refresh();
                }
            })
            {
                text = isReplay ? "READ-ONLY (REPLAY)" : "Reset Defaults",
            };
            resetBtn.SetEnabled(!isReplay);
            _policyDrawer.Add(resetBtn);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var category = row.Category;
                var line = new VisualElement();
                line.AddToClassList("message-log-policy-row");

                var catLabel = new Label(category);
                catLabel.AddToClassList("message-log-policy-category");
                line.Add(catLabel);

                line.Add(MakeRuleToggle("Log", row.Log, isReplay, v =>
                {
                    var current = bridgeHost.NotificationPolicy.GetRule(category);
                    bridgeHost.SetMessageCategoryRule(category, current with { Log = v });
                }));
                line.Add(MakeRuleToggle("Pause", row.Pause, isReplay, v =>
                {
                    var current = bridgeHost.NotificationPolicy.GetRule(category);
                    bridgeHost.SetMessageCategoryRule(category, current with { Pause = v });
                }));
                line.Add(MakeRuleToggle("Balloon", row.MapBalloon, isReplay, v =>
                {
                    var current = bridgeHost.NotificationPolicy.GetRule(category);
                    bridgeHost.SetMessageCategoryRule(category, current with { MapBalloon = v });
                }));
                line.Add(MakeRuleToggle("1x", row.DropToRealTime, isReplay, v =>
                {
                    var current = bridgeHost.NotificationPolicy.GetRule(category);
                    bridgeHost.SetMessageCategoryRule(category, current with { DropToRealTime = v });
                }));

                _policyDrawer.Add(line);
            }
        }

        private Toggle MakeRuleToggle(string label, bool initialValue, bool isReplay, Action<bool> onChanged)
        {
            var toggle = new Toggle(label) { value = initialValue };
            toggle.SetEnabled(!isReplay);
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (bridgeHost == null || bridgeHost.IsReplayViewerAttached)
                {
                    return;
                }

                onChanged(evt.newValue);
                _lastPolicySignature = string.Empty;
                Refresh();
            });
            return toggle;
        }

        private void ApplyMasterVisibility()
        {
            if (_panelRoot != null)
            {
                _panelRoot.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// CMD-23: collapse body to title strip; affordance labels from pure apply-state.
        /// Null-safe when bridge has no chrome API — falls back to expanded body under showPanel.
        /// </summary>
        private void ApplyChromeCollapse()
        {
            var state = bridgeHost != null
                ? bridgeHost.ChromeCollapse
                : C2ChromeCollapseState.Expanded;
            LastChromePresentation = C2ChromeCollapseApplyState.Apply(state);

            var collapsed = showPanel && LastChromePresentation.MessageLogCollapsed;

            if (_messageList != null)
            {
                _messageList.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_onboardingHint != null)
            {
                _onboardingHint.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_policyDrawer != null)
            {
                _policyDrawer.style.display = !collapsed && showPanel && _policyDrawerOpen
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_alertBanner != null)
            {
                _alertBanner.style.display = !collapsed && showPanel && _alertBanner.childCount > 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_deepLinkDrawer != null && collapsed)
            {
                _deepLinkDrawer.style.display = DisplayStyle.None;
            }

            if (_collapseToggle != null)
            {
                _collapseToggle.text = LastChromePresentation.MessageLogAffordanceLabel;
                _collapseToggle.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_panelRoot != null)
            {
                if (collapsed)
                {
                    _panelRoot.AddToClassList(CollapsedClass);
                }
                else
                {
                    _panelRoot.RemoveFromClassList(CollapsedClass);
                }
            }
        }
    }
}
#endif
