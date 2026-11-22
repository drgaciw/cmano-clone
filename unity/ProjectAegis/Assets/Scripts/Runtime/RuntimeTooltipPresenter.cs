// DRG-351: Runtime UITK tooltip presenter for Play Mode (ADR-010 §2–3 presentation-only).
// Unity's built-in VisualElement.tooltip renders only in Editor IMGUI hosts; this presenter
// renders an in-panel tooltip label on hover or keyboard focus and hides on leave/blur.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Presentation-only runtime tooltip presenter for UI Toolkit panels in Play Mode.
    /// Reads <see cref="VisualElement.tooltip"/> on hovered or focused controls; never mutates sim state.
    /// </summary>
    public sealed class RuntimeTooltipPresenter
    {
        /// <summary>UXML element name for the runtime tooltip popup label.</summary>
        public const string TooltipElementName = "runtime-tooltip-popup";

        /// <summary>USS class applied to the runtime tooltip popup label.</summary>
        public const string TooltipCssClass = "unit-detail-runtime-tooltip";

        private readonly List<VisualElement> _observedTargets = new();
        private VisualElement? _root;
        private Label? _tooltipLabel;
        private VisualElement? _activeTarget;

        /// <summary>Underlying tooltip label attached to the panel root.</summary>
        public Label? TooltipElement => _tooltipLabel;

        /// <summary>True while the in-panel tooltip popup is displayed.</summary>
        public bool IsVisible => _tooltipLabel != null && _tooltipLabel.style.display == DisplayStyle.Flex;

        /// <summary>Currently displayed tooltip text, or empty string when hidden.</summary>
        public string CurrentText => IsVisible && _tooltipLabel != null ? _tooltipLabel.text : string.Empty;

        /// <summary>Attach to a panel root, creating or reusing the runtime tooltip label.</summary>
        public void Attach(VisualElement containerRoot)
        {
            if (containerRoot == null)
            {
                return;
            }

            if (ReferenceEquals(_root, containerRoot) && _tooltipLabel != null)
            {
                return;
            }

            Detach();
            _root = containerRoot;

            var existing = containerRoot.Q<Label>(TooltipElementName);
            if (existing != null)
            {
                _tooltipLabel = existing;
            }
            else
            {
                _tooltipLabel = new Label
                {
                    name = TooltipElementName,
                    text = string.Empty,
                };
                _tooltipLabel.AddToClassList(TooltipCssClass);
                containerRoot.Add(_tooltipLabel);
            }

            _tooltipLabel.pickingMode = PickingMode.Ignore;
            _tooltipLabel.focusable = false;
            _tooltipLabel.style.whiteSpace = WhiteSpace.Normal;
            _tooltipLabel.style.display = DisplayStyle.None;

            _root.RegisterCallback<PointerOverEvent>(OnPointerOver);
            _root.RegisterCallback<PointerOutEvent>(OnPointerOut);
            _root.RegisterCallback<FocusInEvent>(OnFocusIn);
            _root.RegisterCallback<FocusOutEvent>(OnFocusOut);
        }

        /// <summary>
        /// Observe a specific control for non-bubbling hover/focus events
        /// (<see cref="PointerEnterEvent"/>, <see cref="MouseEnterEvent"/>, <see cref="FocusEvent"/>).
        /// </summary>
        public void Observe(VisualElement? target)
        {
            if (target == null || _observedTargets.Contains(target))
            {
                return;
            }

            target.RegisterCallback<PointerEnterEvent>(OnTargetPointerEnter);
            target.RegisterCallback<PointerLeaveEvent>(OnTargetPointerLeave);
            target.RegisterCallback<MouseEnterEvent>(OnTargetMouseEnter);
            target.RegisterCallback<MouseLeaveEvent>(OnTargetMouseLeave);
            target.RegisterCallback<FocusEvent>(OnTargetFocus);
            target.RegisterCallback<BlurEvent>(OnTargetBlur);
            _observedTargets.Add(target);
        }

        /// <summary>Re-read the active control's tooltip after a panel refresh and update or hide.</summary>
        public void RefreshActive()
        {
            if (_activeTarget == null)
            {
                return;
            }

            if (_activeTarget.style.display == DisplayStyle.None)
            {
                Hide();
                return;
            }

            ShowFrom(_activeTarget);
        }

        /// <summary>Show tooltip text resolved from <paramref name="target"/> or its nearest ancestor.</summary>
        public void ShowFrom(VisualElement? target)
        {
            if (_tooltipLabel == null || _root == null || target == null)
            {
                Hide();
                return;
            }

            if (TryResolveTooltip(target, out var owner, out var text))
            {
                _activeTarget = owner;
                _tooltipLabel.text = text;
                _tooltipLabel.style.display = DisplayStyle.Flex;
                return;
            }

            Hide();
        }

        /// <summary>Hide the runtime tooltip popup.</summary>
        public void Hide()
        {
            _activeTarget = null;
            if (_tooltipLabel != null)
            {
                _tooltipLabel.text = string.Empty;
                _tooltipLabel.style.display = DisplayStyle.None;
            }
        }

        /// <summary>Unregister all UITK event callbacks and hide the popup.</summary>
        public void Detach()
        {
            Hide();

            for (var i = 0; i < _observedTargets.Count; i++)
            {
                var target = _observedTargets[i];
                target.UnregisterCallback<PointerEnterEvent>(OnTargetPointerEnter);
                target.UnregisterCallback<PointerLeaveEvent>(OnTargetPointerLeave);
                target.UnregisterCallback<MouseEnterEvent>(OnTargetMouseEnter);
                target.UnregisterCallback<MouseLeaveEvent>(OnTargetMouseLeave);
                target.UnregisterCallback<FocusEvent>(OnTargetFocus);
                target.UnregisterCallback<BlurEvent>(OnTargetBlur);
            }

            _observedTargets.Clear();

            if (_root != null)
            {
                _root.UnregisterCallback<PointerOverEvent>(OnPointerOver);
                _root.UnregisterCallback<PointerOutEvent>(OnPointerOut);
                _root.UnregisterCallback<FocusInEvent>(OnFocusIn);
                _root.UnregisterCallback<FocusOutEvent>(OnFocusOut);
                _root = null;
            }
        }

        private bool TryResolveTooltip(VisualElement start, out VisualElement? owner, out string text)
        {
            var current = start;
            while (current != null)
            {
                if (!ReferenceEquals(current, _tooltipLabel) && !string.IsNullOrWhiteSpace(current.tooltip))
                {
                    owner = current;
                    text = current.tooltip;
                    return true;
                }

                if (ReferenceEquals(current, _root))
                {
                    break;
                }

                current = current.parent;
            }

            owner = null;
            text = string.Empty;
            return false;
        }

        private void OnPointerOver(PointerOverEvent evt) => ShowFrom(evt.target as VisualElement);

        private void OnPointerOut(PointerOutEvent evt) => Hide();

        private void OnFocusIn(FocusInEvent evt) => ShowFrom(evt.target as VisualElement);

        private void OnFocusOut(FocusOutEvent evt) => Hide();

        private void OnTargetPointerEnter(PointerEnterEvent evt) => ShowFrom(evt.currentTarget as VisualElement);

        private void OnTargetPointerLeave(PointerLeaveEvent evt) => Hide();

        private void OnTargetMouseEnter(MouseEnterEvent evt) => ShowFrom(evt.currentTarget as VisualElement);

        private void OnTargetMouseLeave(MouseLeaveEvent evt) => Hide();

        private void OnTargetFocus(FocusEvent evt) => ShowFrom(evt.currentTarget as VisualElement);

        private void OnTargetBlur(BlurEvent evt) => Hide();
    }
}
#endif
