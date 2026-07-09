// Doc-20 right unit detail panel — UI Toolkit bound to DelegationBridgeHost.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class RightUnitPanelHost : MonoBehaviour
    {
        private const string RootName = "unit-detail-root";
        private const string UnitIdName = "unit-id-line";
        private const string OrderStateName = "order-state-line";
        private const string StatusName = "status-line";
        private const string MagazineName = "magazine-line";
        private const string EmconName = "emcon-line";
        private const string DoctrineName = "doctrine-line";
        private const string FuelName = "fuel-line";
        private const string EngageName = "engage-line";
        private const string AttackOptionsName = "attack-options-line";
        private const string ContactName = "contact-line";

        // Track T2 (req 20 §Order lifecycle) — Phase 0 .order-state--* USS classes; must match
        // unity/ProjectAegis/Assets/UI/AegisTokens.uss exactly so ClearOrderStateClasses stays correct.
        private static readonly string[] OrderStateCssClasses =
        {
            "order-state--accepted",
            "order-state--queued",
            "order-state--executing",
            "order-state--completed",
            "order-state--denied",
            "order-state--aborted",
        };

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _unitIdLine;
        private Label? _orderStateLine;
        private Label? _statusLine;
        private Label? _magazineLine;
        private Label? _emconLine;
        private Label? _doctrineLine;
        private Label? _fuelLine;
        private Label? _engageLine;
        private Label? _attackOptionsLine;
        private readonly Dictionary<string, Button> _attackButtons = new();
        private Label? _contactLine;
        private bool _wired;
        private bool _attackHandlersRegistered;

        // Track T2 weapons-release confirmation gate (req 20 §Order lifecycle; GDD
        // command-and-control-ui.md — Enter confirms, Esc cancels; ADR-010: cancel emits no intent).
        // Bind pending intent to the unit that opened the gate so a mid-gate selection change cannot
        // re-resolve positive-control / fire from a different unit (Codex P1).
        private string? _pendingAttackOptionId;
        private string? _pendingAttackUnitId;
        private bool _pendingPositiveControlRequired;

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

            RegisterConfirmationGateHandler();
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

            var panel = root.Q<VisualElement>(RootName) ?? root;
            _unitIdLine = panel.Q<Label>(UnitIdName);
            _orderStateLine = panel.Q<Label>(OrderStateName);
            _statusLine = panel.Q<Label>(StatusName);
            _magazineLine = panel.Q<Label>(MagazineName);
            _emconLine = panel.Q<Label>(EmconName);
            _doctrineLine = panel.Q<Label>(DoctrineName);
            _fuelLine = panel.Q<Label>(FuelName);
            _engageLine = panel.Q<Label>(EngageName);
            _attackOptionsLine = panel.Q<Label>(AttackOptionsName);
            _contactLine = panel.Q<Label>(ContactName);
            _attackButtons.Clear();
            RegisterAttackButton(panel.Q<Button>("attack-fire-single"), "fire-single");
            RegisterAttackButton(panel.Q<Button>("attack-fire-salvo"), "fire-salvo");
            RegisterAttackButton(panel.Q<Button>("attack-hold-fire"), "hold-fire");
            _wired = _unitIdLine != null && _statusLine != null && _magazineLine != null &&
                     _emconLine != null && _doctrineLine != null;

            WireAttackMenuHandlers();

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }
        }

        private void RegisterAttackButton(Button? button, string optionId)
        {
            if (button == null)
            {
                return;
            }

            _attackButtons[optionId] = button;
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null)
            {
                return;
            }

            var state = UnitDetailPanelBinder.Bind(
                bridgeHost.LastUnitDetail,
                bridgeHost.Presentation.ResolveContactLine());
            _unitIdLine!.text = state.UnitIdLine;
            RefreshOrderStateChip();
            _statusLine!.text = state.StatusLine;
            _magazineLine!.text = state.MagazineLine;
            _emconLine!.text = state.EmconLine;
            _doctrineLine!.text = state.DoctrineLine;
            if (_fuelLine != null)
            {
                _fuelLine.text = state.FuelLine;
            }

            if (_engageLine != null)
            {
                _engageLine.text = state.EngagePreviewLine;
            }

            if (_attackOptionsLine != null)
            {
                _attackOptionsLine.text = state.AttackOptionsLine;
            }

            if (_contactLine != null)
            {
                _contactLine.text = state.ContactLine;
            }

            RefreshAttackMenuButtons(state.AttackMenu);

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void RefreshAttackMenuButtons(IReadOnlyList<EngageAttackOptions.AttackOption> menu)
        {
            foreach (var (optionId, button) in _attackButtons)
            {
                var option = AttackMenuPanelBinder.FindOption(menu, optionId);
                if (option == null)
                {
                    button.style.display = DisplayStyle.None;
                    continue;
                }

                button.style.display = DisplayStyle.Flex;
                button.text = option.Label;
                button.SetEnabled(option.Enabled);
                button.tooltip = option.Enabled
                    ? option.Label
                    : option.DisabledReason ?? "Blocked";
            }
        }

        private void WireAttackMenuHandlers()
        {
            if (_attackHandlersRegistered)
            {
                return;
            }

            foreach (var (optionId, button) in _attackButtons)
            {
                var capturedId = optionId;
                button.clicked += () => OnAttackOptionClicked(capturedId);
            }

            _attackHandlersRegistered = true;
        }

        private void OnAttackOptionClicked(string optionId)
        {
            if (bridgeHost == null)
            {
                return;
            }

            // Track T2 weapons-release confirmation gate: fire intents pause for Enter/Esc when the
            // selected unit's ROE requires positive control (req 20 §Order lifecycle). ADR-010: no
            // bridge call happens until the pure gate decision (WeaponsReleaseConfirmationGate)
            // resolves — cancel never reaches the bridge.
            var unitId = bridgeHost.SelectedUnitId;
            if (IsWeaponsReleaseOption(optionId) &&
                !string.IsNullOrEmpty(unitId) &&
                ResolvePositiveControlRequired(unitId))
            {
                _pendingAttackOptionId = optionId;
                _pendingAttackUnitId = unitId;
                _pendingPositiveControlRequired = true;
                Refresh();
                return;
            }

            if (bridgeHost.TrySelectAttackOption(optionId, out _))
            {
                Refresh();
            }
        }

        private static bool IsWeaponsReleaseOption(string optionId) =>
            optionId is "fire-single" or "fire-salvo";

        private void RefreshOrderStateChip()
        {
            if (_orderStateLine == null || bridgeHost == null)
            {
                return;
            }

            // Cancel a pending fire confirm if the operator re-selected away from the bound unit.
            if (_pendingAttackOptionId != null &&
                !string.Equals(bridgeHost.SelectedUnitId, _pendingAttackUnitId, StringComparison.Ordinal))
            {
                ClearPendingAttackConfirmation();
            }

            var unitId = bridgeHost.SelectedUnitId;
            var latest = string.IsNullOrEmpty(unitId)
                ? null
                : OrderLifecycleProjection.ProjectLatestForUnit(
                    bridgeHost.Bridge.Orchestrator.DecisionLog,
                    unitId,
                    bridgeHost.LastLifecycleStates);

            if (latest == null)
            {
                _orderStateLine.text = "ORDER: —";
                ClearOrderStateClasses(_orderStateLine);
                return;
            }

            var chip = OrderStateChipBinder.Bind(latest.Value.State);
            _orderStateLine.text = $"ORDER: {chip.Text}";
            ClearOrderStateClasses(_orderStateLine);
            if (!string.IsNullOrEmpty(chip.CssClass))
            {
                _orderStateLine.AddToClassList(chip.CssClass);
            }
        }

        private static void ClearOrderStateClasses(Label label)
        {
            foreach (var cssClass in OrderStateCssClasses)
            {
                label.RemoveFromClassList(cssClass);
            }
        }

        private void RegisterConfirmationGateHandler()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            root.focusable = true;
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (_pendingAttackOptionId == null)
            {
                return;
            }

            if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
            {
                ResolvePendingConfirmation(
                    WeaponsReleaseConfirmationGate.GateAction.Confirm);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                ResolvePendingConfirmation(
                    WeaponsReleaseConfirmationGate.GateAction.Cancel);
                evt.StopPropagation();
            }
        }

        private void ResolvePendingConfirmation(
            WeaponsReleaseConfirmationGate.GateAction action)
        {
            var optionId = _pendingAttackOptionId;
            var unitId = _pendingAttackUnitId;
            var positiveControlRequired = _pendingPositiveControlRequired;
            ClearPendingAttackConfirmation();
            if (optionId == null || bridgeHost == null || string.IsNullOrEmpty(unitId))
            {
                return;
            }

            // Ensure the bridge fires from the unit that opened the gate, not whatever is currently
            // selected (selection may have drifted; we cancel on drift in RefreshOrderStateChip, but
            // bind here as a second line of defense).
            if (!string.Equals(bridgeHost.SelectedUnitId, unitId, StringComparison.Ordinal))
            {
                bridgeHost.SelectUnit(unitId);
            }

            if (!WeaponsReleaseConfirmationGate.ShouldEmit(
                    positiveControlRequired, action))
            {
                // Esc: ADR-010 — no bridge call, no logged intent.
                Refresh();
                return;
            }

            if (bridgeHost.TrySelectAttackOption(optionId, out _))
            {
                Refresh();
            }
        }

        private void ClearPendingAttackConfirmation()
        {
            _pendingAttackOptionId = null;
            _pendingAttackUnitId = null;
            _pendingPositiveControlRequired = false;
        }

        private bool ResolvePositiveControlRequired(string? unitId)
        {
            if (string.IsNullOrEmpty(unitId) || bridgeHost == null)
            {
                return false;
            }

            var policy = bridgeHost.Bridge.Orchestrator.ScenarioPolicy;
            if (policy == null)
            {
                return false;
            }

            // Req 20 P0 / TR-c2-006 residual (T3): policy projection flag, not a raw WeaponsTight check.
            var resolved = policy.ResolveUnitPolicy(unitId, isFriendly: true);
            return PositiveControlRequiredProjection.IsRequired(resolved.Effective);
        }
    }
}
#endif