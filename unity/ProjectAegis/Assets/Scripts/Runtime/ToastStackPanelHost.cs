// C2 ToastStack — transient alert host bound to DelegationBridgeHost (req 20 §Alerting and
// Interruption, TR-c2-007/008, Track T3). UI Toolkit panel; PURE queue logic lives in
// ProjectAegis.Delegation.Projection.ToastStackModel so it is unit-testable headlessly (see
// AlertingIntegrationTests.cs / ToastStackModelTests.cs).
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToastStackPanelHost : MonoBehaviour
    {
        private const string RootName = "toast-stack-root";
        private const string ListName = "toast-list";
        private const string OverflowChipName = "toast-overflow-chip";
        private const string OverflowLabelName = "toast-overflow-label";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        [Tooltip("Toasts older than this are auto-evicted (seconds). Notable/Critical alerts stay logged " +
                 "regardless — this only trims the transient toast, not the message log.")]
        [SerializeField] private float toastLifetimeSeconds = 6f;

        [Tooltip("a11y: when true, adds the .reduced-motion class so ToastStackPanel.uss disables the " +
                 "enter/exit transition. Wire this to the project's accessibility settings when built.")]
        [SerializeField] private bool reducedMotion;

        private UIDocument _document = null!;
        private VisualElement? _panel;
        private VisualElement? _list;
        private VisualElement? _overflowChip;
        private Label? _overflowLabel;
        private bool _wired;

        private ToastStackModel _model = new(isReplaySuppressed: false);
        private ulong? _lastProcessedSequenceId;
        private readonly Dictionary<ulong, float> _spawnTimes = new();

        /// <summary>
        /// Last auto-pause command produced for a newly observed Critical alert, or null.
        /// BLOCKER (see <see cref="AutoPauseCommand"/> remarks): no pause-reason-stack API exists in this
        /// baseline to dispatch this onto. Exposed here only so a future pause/interrupt system — or a
        /// test — can observe that the correct command was produced; this host does not call any bridge
        /// or sim method with it.
        /// </summary>
        public AutoPauseCommand? LastAutoPauseCommand { get; private set; }

        private void Reset()
        {
            if (bridgeHost == null)
            {
                bridgeHost = GetComponent<DelegationBridgeHost>();
            }

            _document = GetComponent<UIDocument>();
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
            if (!showPanel || bridgeHost == null)
            {
                return;
            }

            if (!_wired)
            {
                TryWireElements();
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

            _panel = root.Q<VisualElement>(RootName) ?? root;
            _list = _panel.Q<VisualElement>(ListName);
            _overflowChip = _panel.Q<VisualElement>(OverflowChipName);
            _overflowLabel = _panel.Q<Label>(OverflowLabelName);
            _wired = _list != null;

            if (panelStyles != null && !_panel.styleSheets.Contains(panelStyles))
            {
                _panel.styleSheets.Add(panelStyles);
            }

            _panel.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            _panel.EnableInClassList("reduced-motion", reducedMotion);
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null || _list == null)
            {
                return;
            }

            var isReplay = bridgeHost.Bridge != null && bridgeHost.Bridge.AttachReplayViewer;
            if (_model.IsReplaySuppressed != isReplay)
            {
                // Mode changed (e.g. entered/left replay) — rebuild the model under the new
                // suppression policy. Seed the watermark to the current max sequence so historical
                // Notable/Critical lines are not re-toasted as "new" after the toggle.
                _model = new ToastStackModel(isReplaySuppressed: isReplay);
                _spawnTimes.Clear();
                SeedWatermark(bridgeHost.LastMessageLog);
            }

            EnqueueNewAlerts(bridgeHost.LastMessageLog, isReplay);
            EvictExpiredToasts();
            RenderVisibleToasts();
        }

        private void SeedWatermark(IReadOnlyList<MessageLogLine> lines)
        {
            _lastProcessedSequenceId = lines.Count > 0 ? lines[lines.Count - 1].SequenceId : 0UL;
        }

        private void EnqueueNewAlerts(IReadOnlyList<MessageLogLine> lines, bool isReplay)
        {
            // First run: seed watermark to the latest sequence so startup does not flood the stack
            // with the entire historical LastMessageLog as fresh toasts.
            if (!_lastProcessedSequenceId.HasValue)
            {
                SeedWatermark(lines);
                return;
            }

            var newCriticalAlerts = new List<AlertItem>();
            foreach (var line in lines)
            {
                if (line.SequenceId <= _lastProcessedSequenceId.Value)
                {
                    continue;
                }

                _lastProcessedSequenceId = line.SequenceId;

                var severity = AlertSeverityMap.ForCategory(line.Category);
                // req 20 default matrix: Critical -> toast + log + optional auto-pause; Notable -> toast
                // (flash) + log; Routine -> log only (never toasted).
                // Skip spawn-time bookkeeping during replay: ToastStackModel.Add is a no-op while
                // suppressed, and growing _spawnTimes would leak memory for the whole replay.
                if (severity != AlertSeverity.Routine && !isReplay)
                {
                    _model.Add(new ToastEntry(line.SequenceId, severity, line.Category, line.Text, line.UnitId));
                    _spawnTimes[line.SequenceId] = Time.unscaledTime;
                }

                if (severity == AlertSeverity.Critical)
                {
                    newCriticalAlerts.Add(new AlertItem(
                        line.SequenceId, line.SimTime, line.Category, line.Text, line.UnitId, severity));
                }
            }

            if (newCriticalAlerts.Count > 0)
            {
                var command = AutoPausePolicy.Evaluate(newCriticalAlerts, isReplay);
                if (command != null)
                {
                    LastAutoPauseCommand = command;
                    // BLOCKER: no pause-reason-stack API exists to dispatch `command` onto (see
                    // AutoPauseCommand remarks). Logged so the gap is visible in Editor/Player runs
                    // rather than silently dropped.
                    Debug.LogWarning(
                        $"[ToastStack] Critical alert '{command.Reason}' (seq {command.TriggerSequenceId}) " +
                        "requests auto-pause, but no pause-reason-stack API exists in this baseline to " +
                        "dispatch it onto. See AutoPauseCommand remarks / T3 track report.");
                }
            }
        }

        private void EvictExpiredToasts()
        {
            if (toastLifetimeSeconds <= 0f)
            {
                return;
            }

            List<ulong>? expired = null;
            foreach (var kvp in _spawnTimes)
            {
                if (Time.unscaledTime - kvp.Value >= toastLifetimeSeconds)
                {
                    (expired ??= new List<ulong>()).Add(kvp.Key);
                }
            }

            if (expired == null)
            {
                return;
            }

            foreach (var sequenceId in expired)
            {
                _model.Evict(sequenceId);
                _spawnTimes.Remove(sequenceId);
            }
        }

        private void RenderVisibleToasts()
        {
            if (_list == null)
            {
                return;
            }

            _list.Clear();
            foreach (var entry in _model.VisibleToasts)
            {
                _list.Add(BuildToastElement(entry));
            }

            var overflow = _model.OverflowCount;
            if (_overflowChip != null)
            {
                _overflowChip.style.display = overflow > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_overflowLabel != null && overflow > 0)
            {
                _overflowLabel.text = $"+{overflow} more";
            }
        }

        private VisualElement BuildToastElement(ToastEntry entry)
        {
            var item = new VisualElement();
            item.AddToClassList("toast-item");
            item.AddToClassList(SeverityStyleClass(entry.Severity));
            item.userData = entry.SequenceId;
            item.RegisterCallback<ClickEvent>(OnToastClicked);

            var icon = new Label(SeverityIconGlyph(entry.Severity));
            icon.AddToClassList("toast-item-icon");
            item.Add(icon);

            var body = new VisualElement();
            body.AddToClassList("toast-item-body");

            var category = new Label(SeverityLabel(entry.Severity));
            category.AddToClassList("toast-item-category");
            body.Add(category);

            var text = new Label(entry.Text);
            text.AddToClassList("toast-item-text");
            body.Add(text);

            item.Add(body);
            return item;
        }

        private void OnToastClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not VisualElement { userData: ulong sequenceId } || bridgeHost == null)
            {
                return;
            }

            var focusTarget = _model.ResolveFocusTarget(sequenceId);
            if (string.IsNullOrEmpty(focusTarget))
            {
                return;
            }

            // Toast click -> presentation-only focus (ADR-010: no sim mutation). Reuses the same
            // selection surface as every other C2 panel (OOB tree, contact list, map symbol).
            bridgeHost.SelectUnit(focusTarget);
        }

        // Reuses the canonical Phase 0 severity classes from AegisTokens.uss (.toast--critical/-notable/
        // -routine) rather than inventing parallel names — same classes MessageLogPanelHost applies to
        // log rows, so severity styling stays a single source of truth across both surfaces.
        private static string SeverityStyleClass(AlertSeverity severity) => severity switch
        {
            AlertSeverity.Critical => "toast--critical",
            AlertSeverity.Notable => "toast--notable",
            _ => "toast--routine",
        };

        // Colour is never the sole cue (a11y §5): every toast pairs its severity border/tint with this
        // glyph AND the text label from SeverityLabel below.
        private static string SeverityIconGlyph(AlertSeverity severity) => severity switch
        {
            AlertSeverity.Critical => "!",
            AlertSeverity.Notable => "i",
            _ => "-",
        };

        private static string SeverityLabel(AlertSeverity severity) => severity switch
        {
            AlertSeverity.Critical => "CRITICAL",
            AlertSeverity.Notable => "NOTABLE",
            _ => "ROUTINE",
        };
    }
}
#endif
