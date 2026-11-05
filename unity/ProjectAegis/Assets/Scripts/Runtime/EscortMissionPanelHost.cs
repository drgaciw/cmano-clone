#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Text;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>Thin UI Toolkit view: immutable mission projections in, validated commands out.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class EscortMissionPanelHost : MonoBehaviour
    {
        [SerializeField] private EscortMissionHost missionHost = null!;
        [SerializeField] private VisualTreeAsset panelAsset = null!;
        [SerializeField] private StyleSheet panelStyles = null!;
        private UIDocument _document = null!;
        private VisualElement? _root;
        private VisualElement? _map;
        private readonly Dictionary<string, Label> _labels = new();
        private readonly Dictionary<string, Button> _buttons = new();
        private readonly Dictionary<string, Action> _commandHandlers = new();
        private readonly Dictionary<string, VisualElement> _markers = new();
        private readonly StringBuilder _text = new();
        private float _nextRefresh;
        private string? _focusedSymbol;

        private static readonly string[] Commands =
        {
            "begin", "pause", "resume", "speed-slower", "speed-faster", "radar-active", "radar-passive",
            "investigate", "engage", "posture-manual", "posture-cautious", "posture-balanced",
            "posture-assertive", "hold", "proceed", "alerts-all", "alerts-critical", "alerts-manual",
            "acknowledge-alerts", "restart",
        };

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            if (panelAsset != null)
            {
                _document.visualTreeAsset = panelAsset;
            }
        }

        private void OnEnable() => Wire();

        private void OnDisable()
        {
            foreach (var pair in _buttons)
            {
                pair.Value.clicked -= _commandHandlers[pair.Key];
            }

            var focus = _root?.Q<Button>("escort-focus-alert");
            if (focus != null)
            {
                focus.clicked -= FocusContact;
            }

            foreach (var marker in _markers.Values)
            {
                marker.RemoveFromHierarchy();
            }

            _root = null;
            _labels.Clear();
            _buttons.Clear();
            _commandHandlers.Clear();
            _markers.Clear();
        }

        private void Wire()
        {
            var root = _document.rootVisualElement;
            _root = root?.Q("escort-screen");
            if (_root == null)
            {
                return;
            }

            if (panelStyles != null && !_root.styleSheets.Contains(panelStyles))
            {
                _root.styleSheets.Add(panelStyles);
            }

            _map = _root.Q("escort-map");
            foreach (var command in Commands)
            {
                var button = _root.Q<Button>("command-" + command);
                if (button == null || _buttons.ContainsKey(command))
                {
                    continue;
                }

                var id = command;
                Action handler = () => Submit(id);
                button.clicked += handler;
                _buttons.Add(id, button);
                _commandHandlers.Add(id, handler);
            }

            var focus = _root.Q<Button>("escort-focus-alert");
            if (focus != null)
            {
                focus.clicked -= FocusContact;
                focus.clicked += FocusContact;
            }
        }

        private void Submit(string command)
        {
            if (missionHost == null)
            {
                return;
            }

            missionHost.IssueCommand(command);
            if (command == "restart")
            {
                _focusedSymbol = null;
            }

            Refresh();
        }

        private void LateUpdate()
        {
            if (_root == null)
            {
                Wire();
            }

            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.unscaledTime + 0.2f;
            Refresh();
        }

        private void SetLabel(string name, string value)
        {
            if (_root == null)
            {
                return;
            }

            if (!_labels.TryGetValue(name, out var label))
            {
                label = _root.Q<Label>(name);
                if (label == null)
                {
                    return;
                }

                _labels[name] = label;
            }

            if (label.text != value)
            {
                label.text = value;
            }
        }

        private void Refresh()
        {
            if (_root == null || missionHost == null || missionHost.Mission == null)
            {
                return;
            }

            var view = missionHost.Presentation;
            SetLabel("escort-title", view.Title);
            SetLabel("escort-clock", $"{TimeSpan.FromSeconds(missionHost.Mission.SimTime):mm\\:ss} · {missionHost.PresentationHost.LiveCompressionLabel}");
            SetLabel("escort-briefing", view.Briefing);
            SetLabel("escort-guidance", view.Guidance);
            SetLabel("escort-objective", view.Objective);
            SetLabel("escort-status", $"{view.Status} · Score {view.Score} · {view.RoundsRemaining} rounds · {TimeSpan.FromSeconds(Math.Max(0, view.RemainingSeconds)):mm\\:ss} remaining");
            SetLabel("escort-posture", view.Posture);
            SetLabel("escort-radar", $"{view.Radar} · Contact: {view.ContactState}");
            SetLabel("escort-command-result", missionHost.LastCommandMessage);
            SetLabel("escort-debrief-summary", view.IsComplete ? view.Debrief : "Your decisions and their observed consequences appear here.");
            SetLabel("escort-debrief-title", view.IsComplete ? "AFTER-ACTION REVIEW" : "DECISIVE MOMENTS");
            var progress = _root.Q<ProgressBar>("escort-progress");
            if (progress != null)
            {
                progress.value = (float)(view.Progress * 100);
                progress.title = $"Convoy transit · {view.Progress:P0}";
            }

            var exit = _root.Q("escort-exit");
            var exitLabel = _root.Q("escort-exit-label");
            if (exit != null)
            {
                exit.style.left = Length.Percent((float)missionHost.Mission.ExitNormalizedX * 100);
                exit.style.right = StyleKeyword.Auto;
            }

            if (exitLabel != null)
            {
                exitLabel.style.left = Length.Percent((float)missionHost.Mission.ExitNormalizedX * 100 + 1);
                exitLabel.style.right = StyleKeyword.Auto;
            }

            _text.Clear();
            foreach (var alert in view.Alerts)
            {
                _text.AppendLine(alert);
            }

            SetLabel("escort-alerts", _text.Length == 0 ? "No outstanding alerts." : _text.ToString());
            _text.Clear();
            foreach (var moment in view.Moments)
            {
                _text.Append(TimeSpan.FromSeconds(moment.SimTime).ToString(@"mm\:ss"));
                _text.Append("  ").AppendLine(moment.Title).AppendLine(moment.Detail).AppendLine();
            }

            SetLabel("escort-moments", _text.ToString());
            foreach (var pair in _buttons)
            {
                var isBegin = pair.Key == "begin";
                var enabled = pair.Key == "restart" || (!view.IsComplete && (!isBegin || missionHost.Mission.SimTime == 0));
                pair.Value.SetEnabled(enabled);
            }

            RenderMap(missionHost.Mission.MapSymbols);
        }

        private void RenderMap(IReadOnlyList<MapSymbolEntry> symbols)
        {
            if (_map == null)
            {
                return;
            }

            foreach (var marker in _markers.Values)
            {
                marker.style.display = DisplayStyle.None;
            }

            foreach (var symbol in symbols)
            {
                if (!_markers.TryGetValue(symbol.SymbolId, out var marker))
                {
                    marker = new VisualElement { focusable = true };
                    marker.AddToClassList("escort-marker");
                    var glyph = new Label { name = "symbol-glyph" };
                    glyph.AddToClassList("escort-marker-glyph");
                    marker.Add(glyph);
                    var caption = new Label { name = "symbol-caption" };
                    caption.AddToClassList("escort-marker-caption");
                    marker.Add(caption);
                    var id = symbol.SymbolId;
                    marker.RegisterCallback<ClickEvent>(_ => SelectSymbol(id));
                    marker.RegisterCallback<KeyDownEvent>(evt =>
                    {
                        if (evt.keyCode is KeyCode.Return or KeyCode.Space)
                        {
                            SelectSymbol(id);
                            evt.StopPropagation();
                        }
                    });
                    _markers.Add(id, marker);
                    _map.Add(marker);
                }

                marker.style.display = DisplayStyle.Flex;
                marker.style.left = Length.Percent(symbol.NormalizedX * 100);
                marker.style.top = Length.Percent((1 - symbol.NormalizedY) * 100);
                marker.Q<Label>("symbol-glyph").text = symbol.ShapeGlyph;
                marker.Q<Label>("symbol-caption").text = symbol.Label;
                marker.tooltip = symbol.IsDestroyed ? $"{symbol.Label}: confirmed destroyed" : symbol.Label;
                marker.EnableInClassList("escort-marker--convoy", symbol.SymbolId == "blue-convoy");
                marker.EnableInClassList("escort-marker--hostile", symbol.Affiliation == "Hostile");
                marker.EnableInClassList("escort-marker--destroyed", symbol.IsDestroyed);
                marker.EnableInClassList("escort-marker--focused", symbol.SymbolId == _focusedSymbol);
            }
        }

        private void SelectSymbol(string symbolId)
        {
            _focusedSymbol = symbolId;
            var symbols = missionHost.Mission.MapSymbols;
            foreach (var symbol in symbols)
            {
                if (symbol.SymbolId != symbolId)
                {
                    continue;
                }

                SetLabel("escort-selection", $"{symbol.Label} · {(symbol.IsDestroyed ? "Confirmed destroyed" : symbol.Affiliation)}. {missionHost.Presentation.Guidance}");
                break;
            }

            RenderMap(symbols);
        }

        private void FocusContact()
        {
            foreach (var symbol in missionHost.Mission.MapSymbols)
            {
                if (symbol.Affiliation is "Hostile" or "Unknown")
                {
                    SelectSymbol(symbol.SymbolId);
                    return;
                }
            }

            SetLabel("escort-selection", "No detected hostile contact to focus. Investigate a contact before requesting fire control.");
        }
    }
}
#endif
