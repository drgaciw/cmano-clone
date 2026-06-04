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
        private const string StatusName = "status-line";
        private const string MagazineName = "magazine-line";
        private const string EmconName = "emcon-line";
        private const string DoctrineName = "doctrine-line";
        private const string FuelName = "fuel-line";
        private const string EngageName = "engage-line";
        private const string AttackOptionsName = "attack-options-line";
        private const string ContactName = "contact-line";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _unitIdLine;
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

            var panel = root.Q<VisualElement>(RootName) ?? root;
            _unitIdLine = panel.Q<Label>(UnitIdName);
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

            if (bridgeHost.TrySelectAttackOption(optionId, out _))
            {
                Refresh();
            }
        }
    }
}
#endif