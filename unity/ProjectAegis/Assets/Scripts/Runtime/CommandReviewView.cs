#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.AfterAction;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>Thin, pooled command-review UI. Reads projections and forwards explicit player intent.</summary>
    public sealed class CommandReviewView : IDisposable
    {
        private readonly DelegationBridgeHost _host;
        private readonly Foldout _root;
        private readonly ScrollView _body;
        private readonly Label _status;
        private readonly Label _liveTime;
        private readonly VisualElement _decisions;
        private readonly DropdownField _group = new("Task group");
        private readonly Label _pageLabel;
        private readonly ScrollView _events;
        private readonly TextField _platform = new("Platform");
        private readonly TextField _target = new("Target");
        private readonly TextField _family = new("Weapon family");
        private readonly TextField _outcome = new("Outcome");
        private readonly List<Button> _buttons = new();
        private readonly List<Button> _statusButtons = new();
        private readonly Foldout _statusHistory;
        private readonly Foldout _timeline;
        private readonly Dictionary<string, (Foldout Root, List<Label> Rows)> _sections = new();
        private int _page;
        private object? _boundFrame;
        private object? _boundDisplay;
        private object? _boundSections;
        private IReadOnlyList<AfterActionLedgerEntry> _rows = Array.Empty<AfterActionLedgerEntry>();

        /// <summary>Attaches to the existing map panel without requiring scene or prefab changes.</summary>
        public CommandReviewView(VisualElement parent, DelegationBridgeHost host)
        {
            _host = host;
            _root = new Foldout { name = "command-review", text = "Command, learn and adapt", value = false };
            _root.style.backgroundColor = new Color(.035f, .055f, .075f, .96f);
            _root.style.color = Color.white;
            _root.style.flexShrink = 1;
            _root.style.minHeight = 24;
            _root.style.maxHeight = Length.Percent(45);
            _root.contentContainer.style.flexShrink = 1;
            _root.contentContainer.style.minHeight = 0;
            _body = new ScrollView();
            _body.style.maxHeight = 320;
            _body.style.flexShrink = 1;
            _body.style.minHeight = 0;
            _root.Add(_body);
            _status = new Label { name = "command-review-status" };
            _status.style.whiteSpace = WhiteSpace.Normal;
            _body.Add(_status);
            var problems = new Toggle("Show only degraded own units");
            problems.RegisterValueChangedCallback(evt => { if (_host != null) { _host.SetCommandReviewProblemsOnly(evt.newValue); Bind(); } });
            _body.Add(problems);
            var coverage = new Toggle("Show authored coverage and gaps") { value = host.ShowCommandCoverage };
            coverage.RegisterValueChangedCallback(evt => { if (_host != null) _host.ShowCommandCoverage = evt.newValue; });
            _body.Add(coverage);
            var timeline = new Foldout { text = "Combat timeline and after-action review", value = true };
            _timeline = timeline;
            foreach (var field in new[] { _platform, _target, _family, _outcome })
            {
                field.tooltip = "Exact value from the event; leave empty to show all";
                field.RegisterValueChangedCallback(_ => { _page = 0; RefreshTimeline(); });
                timeline.Add(field);
            }
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.Add(new Button(() => { _host.ReturnToLiveCombat(); Bind(); }) { text = "Return to live" });
            toolbar.Add(new Button(() => { _page = Math.Max(0, _page - 1); RefreshTimeline(); }) { text = "Previous" });
            toolbar.Add(new Button(() => { _page++; RefreshTimeline(); }) { text = "Next" });
            timeline.Add(toolbar);
            _pageLabel = new Label();
            timeline.Add(_pageLabel);
            _events = new ScrollView { name = "command-review-events" };
            _events.style.maxHeight = 170;
            timeline.Add(_events);
            _body.Add(timeline);
            _liveTime = new Label();
            _liveTime.style.whiteSpace = WhiteSpace.Normal;
            _body.Add(_liveTime);
            _decisions = new VisualElement { name = "command-review-decisions" };
            var result = new Label();
            result.style.whiteSpace = WhiteSpace.Normal;
            foreach (var item in new[] {
                (AdviceSkillIds.DatalinkAssessment, "Assess datalink"),
                (AdviceSkillIds.ResourceRecommendation, "Recommend resources"),
                (AdviceSkillIds.MissionPackageExplanation, "Explain mission package") })
            {
                var skill = item.Item1;
                _decisions.Add(new Button(() => { result.text = _host.InvokeAdviceSkill(skill); Bind(); }) { text = item.Item2 });
            }
            _decisions.Add(_group);
            foreach (var decision in new[] { CoordinationDecision.Hold, CoordinationDecision.Withdraw, CoordinationDecision.Reattack })
            {
                var choice = decision;
                _decisions.Add(new Button(() => result.text = _host.SubmitGroupDecision(_group.value, choice))
                { text = choice == CoordinationDecision.Reattack ? "Review reattack constraints" : "Issue group " + choice });
            }
            _decisions.Add(result);
            _body.Add(_decisions);
            _statusHistory = new Foldout { text = "Recent damage/comms — open related combat timeline", value = false };
            _body.Add(_statusHistory);
            parent.Add(_root);
        }

        /// <summary>Called on changed snapshot/inspection only; reuses timeline buttons and section labels.</summary>
        public void Bind()
        {
            if (_host == null) return;
            if (ReferenceEquals(_boundFrame, _host.LastCombatFrame)
                && ReferenceEquals(_boundDisplay, _host.CommandTimeline.DisplayFrame)
                && ReferenceEquals(_boundSections, _host.LastCommandReviewSections)) return;
            _boundFrame = _host.LastCombatFrame;
            _boundDisplay = _host.CommandTimeline.DisplayFrame;
            _boundSections = _host.LastCommandReviewSections;
            _status.text = _host.CommandTimeline.StatusLine;
            _liveTime.text = $"CURRENT decision support — t={_host.LastCombatFrame.SimTime:0.###}. Sections below show live evidence.";
            _decisions.SetEnabled(!_host.CommandTimeline.IsInspecting);
            var groups = new List<string>();
            foreach (var group in _host.LastCoordination.Groups) groups.Add(group.Coordination.GroupId);
            var selected = _group.value;
            _group.choices = groups;
            _group.SetValueWithoutNotify(groups.Contains(selected) ? selected : groups.Count > 0 ? groups[0] : string.Empty);
            var statusRows = _host.CommandStatusHistory;
            var statusCount = Math.Min(100, statusRows.Count);
            for (var i = 0; i < statusCount; i++)
            {
                if (i == _statusButtons.Count)
                {
                    var button = new Button();
                    button.style.whiteSpace = WhiteSpace.Normal;
                    button.clicked += () => {
                        if (button.userData is StatusCorrelationRow change)
                        {
                            _platform.SetValueWithoutNotify(string.Empty);
                            _target.SetValueWithoutNotify(change.UnitId);
                            _family.SetValueWithoutNotify(string.Empty);
                            _outcome.SetValueWithoutNotify(string.Empty);
                            _page = 0;
                            _timeline.value = true;
                            RefreshTimeline();
                        }
                    };
                    _statusButtons.Add(button);
                    _statusHistory.Add(button);
                }
                var row = statusRows[statusRows.Count - 1 - i];
                _statusButtons[i].userData = row;
                _statusButtons[i].text = $"#{row.SequenceId} t={row.SimTime:0.###} {row.UnitId} {row.Kind}: {row.Detail}";
                _statusButtons[i].tooltip = "Show combat events targeting this unit. A status change need not have a combat cause.";
                _statusButtons[i].style.display = DisplayStyle.Flex;
            }
            for (var i = statusCount; i < _statusButtons.Count; i++) _statusButtons[i].style.display = DisplayStyle.None;
            _statusHistory.text = $"Damage/comms — latest {statusCount} of {statusRows.Count}; open related combat timeline";
            RefreshTimeline();
            foreach (var section in _host.LastCommandReviewSections)
            {
                if (!_sections.TryGetValue(section.Id, out var pool))
                {
                    var root = new Foldout { text = section.Title, value = false };
                    pool = (root, new List<Label>());
                    _sections.Add(section.Id, pool);
                    _body.Add(root);
                }
                pool.Root.text = section.Title;
                for (var i = 0; i < section.Lines.Count; i++)
                {
                    if (i == pool.Rows.Count)
                    {
                        var label = new Label();
                        label.style.whiteSpace = WhiteSpace.Normal;
                        label.style.color = Color.white;
                        pool.Rows.Add(label);
                        pool.Root.Add(label);
                    }
                    pool.Rows[i].text = section.Lines[i];
                    pool.Rows[i].style.display = DisplayStyle.Flex;
                }
                for (var i = section.Lines.Count; i < pool.Rows.Count; i++) pool.Rows[i].style.display = DisplayStyle.None;
            }
        }

        private static string? Optional(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private void RefreshTimeline()
        {
            if (_host == null) return;
            _rows = _host.CommandTimeline.Filter(new AfterActionLedgerFilter(
                Optional(_platform.value), Optional(_target.value), Optional(_family.value), Optional(_outcome.value)));
            _page = Math.Min(_page, Math.Max(0, (_rows.Count - 1) / 100));
            var start = _page * 100;
            var count = Math.Min(100, _rows.Count - start);
            _pageLabel.text = $"{_rows.Count} events | page {_page + 1} | select an event to inspect map and explanation";
            for (var i = 0; i < count; i++)
            {
                if (i == _buttons.Count)
                {
                    var button = new Button();
                    button.clicked += () =>
                    {
                        if (_host != null && button.userData is AfterActionLedgerEntry entry)
                        {
                            _host.InspectTimelineEvent(entry);
                            Bind();
                        }
                    };
                    button.style.whiteSpace = WhiteSpace.Normal;
                    _buttons.Add(button);
                    _events.Add(button);
                }
                var row = _rows[start + i];
                _buttons[i].userData = row;
                _buttons[i].text = $"t={row.SimTime:0.###} | {row.ShooterId} → {row.TargetId} | {row.WeaponFamilyId} | {row.Phase}: {row.Outcome}";
                _buttons[i].tooltip = row.ExplanationRef;
                _buttons[i].style.display = DisplayStyle.Flex;
            }
            for (var i = count; i < _buttons.Count; i++) _buttons[i].style.display = DisplayStyle.None;
        }

        /// <summary>Removes owned controls when the document is replaced.</summary>
        public void Dispose() => _root.RemoveFromHierarchy();
    }
}
#endif
